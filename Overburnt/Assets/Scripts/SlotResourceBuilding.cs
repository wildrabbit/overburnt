using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IItemGenerator
{
    void DoDragStart(Vector2 pos);
    void DoDragEnd(Vector2 pos);
    void DoDrag(Vector2 pos);

    bool ContainsPosition(Vector2 pos);
    void ResetSlot();
    void ClearContents();
}

public class SlotResourceBuilding : MonoBehaviour, IItemGenerator, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public ItemID ItemID;
    public SpriteRenderer Slot;
    public Collider2D CollisionArea;
    public TMPro.TextMeshPro Percent;
    public float SpawnTime;
    public SlotStatus SlotStatus;
    public bool BeginReady;
    float _elapsed;
    ResourceBuilding _parent;
    GameController _gameController;
    Item _itemData;

    bool _draggingSlot;
    Vector2 _dragStartPosition;

    public void Init(ResourceBuilding parent)
    {
        _parent = parent;
        _gameController = parent.GameController;
        _elapsed = 0;
        _itemData = _gameController.GetItem(ItemID);
        _draggingSlot = false;
        Slot.sprite = _itemData?.Icon;

        if (BeginReady)
        {
            Percent.gameObject.SetActive(false);
            var color = Slot.color;
            color.a = 1;
            Slot.color = color;
            SlotStatus = SlotStatus.PickupPending;
        }
        else
        {
            Percent.gameObject.SetActive(true);
            var color = Slot.color;
            color.a = 0.5f;
            Slot.color = color;
            SlotStatus = SlotStatus.Generating;
        }
    }

    public void UpdateSlot(float dt)
    {
        if(_draggingSlot)
        {
            Vector2 mousePos = Input.mousePosition;
            OnDrag(Camera.main.ScreenToWorldPoint(mousePos));
        }

        if (SlotStatus == SlotStatus.Generating)
        {
            Percent.text = $"{(int)(100 * _elapsed / SpawnTime)}%";
            if (Mathf.Approximately(_elapsed, SpawnTime))
            {
                return;
            }
            _elapsed += dt;
            if (_elapsed >= SpawnTime)
            {
                _elapsed = SpawnTime;
                StartCoroutine(PrepareReady());
            }
        }
    }

    IEnumerator PrepareReady()
    {
        yield return new WaitForSeconds(0.1f);
        Percent.gameObject.SetActive(false);
        Slot.sprite = _itemData?.Icon;
        _elapsed = 0;
        var color = Slot.color;
        color.a = 1;
        Slot.color = color;
        SlotStatus = SlotStatus.PickupPending;
    }

    IEnumerator PrepareGenerating()
    {
        yield return new WaitForSeconds(0.1f);
        if (SpawnTime > 0)
        {
            Percent.gameObject.SetActive(true);
            _elapsed = 0;
            var color = Slot.color;
            color.a = 0.5f;
            Slot.enabled = true;
            Slot.color = color;
            SlotStatus = SlotStatus.Generating;
        }
        else yield return PrepareReady();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(_itemData.InteractionType != InteractionType.Click)
        {
            return;
        }

        if (SlotStatus == SlotStatus.PickupPending)
        {
            _gameController.ItemClicked(this, _itemData);
            // TODO: Move to "DoClick"
            Slot.enabled = false;
            StartCoroutine(PrepareGenerating());
        }
    }

    public void OnDrag(Vector2 worldPos)
    {
        _gameController.DragItem(this, _itemData, worldPos);
    }

    public void DoDragStart(Vector2 pos)
    {
    }

    public void DoDragEnd(Vector2 pos)
    {

    }

    public void DoDrag(Vector2 pos)
    {
        if(_draggingSlot)
        {
            Slot.transform.position = pos;
        }
    }

    public bool ContainsPosition(Vector2 pos)
    {
        return CollisionArea.OverlapPoint(pos);
    }

    public void ResetSlot()
    {
        Slot.transform.position = _dragStartPosition;
    }

    public void ClearContents()
    {
        ResetSlot();
        Slot.sprite = null;
        StartCoroutine(PrepareGenerating());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_itemData.InteractionType != InteractionType.Drag)
        {
            return;
        }

        if(!_draggingSlot)
        {
            Slot.sortingOrder += 100;
            _draggingSlot = true;
            _dragStartPosition = Slot.transform.position;
            Vector2 worldPos = eventData.pointerCurrentRaycast.worldPosition;
            Slot.transform.position = worldPos;
            _gameController.DragItemStart(this, _itemData, worldPos);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_itemData.InteractionType != InteractionType.Drag)
        {
            return;
        }

        if (_draggingSlot)
        {
            Slot.sortingOrder -= 100;
            _draggingSlot = false;
            Vector2 worldPos = eventData.pointerCurrentRaycast.worldPosition;
            Slot.transform.position = worldPos;
            _gameController.DragItemEnd(this, _itemData, worldPos);
        }
    }
}