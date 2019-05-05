using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using UnityEngine.Serialization;

public enum SlotStatus
{
    AwaitingRequirements,
    Busy,
    ItemReady    
}

public abstract class BaseSlot : MonoBehaviour, IItemGenerator, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public SpriteRenderer SlotBG;
    public SpriteRenderer SlotContents;
    public Collider2D CollisionArea;
    public TextMeshPro Percent;
    public SlotStatus DefaultStatus;

    protected GameController _gameController;

    protected bool _draggingSlot;
    protected Vector2 _dragStartPosition;
    protected Vector2 _dragStartScale;

    protected SlotStatus _slotStatus;

    public abstract Item SlotItem { get; set; }

    protected float _elapsed;
    protected float _currentSpawnTime;

    public void Init(GameController gameController)
    {
        _gameController = gameController;
        DoInit();
    }

    public void Load()
    {
        gameObject.SetActive(true);
        _elapsed = 0;
        _draggingSlot = false;
        DoLoad();
    }

    public void Unload()
    {
        gameObject.SetActive(false);
        DoUnload();
    }

    public void UpdateSlot(float dt)
    {
        if(_draggingSlot)
        {
            Vector2 mousePos = Input.mousePosition;
            OnDrag(Camera.main.ScreenToWorldPoint(mousePos));
        }

        if (_slotStatus == SlotStatus.Busy && _elapsed < _currentSpawnTime)
        {
            Percent.text = $"{(int)(100 * _elapsed / _currentSpawnTime)}%";
            _elapsed += dt;
            if (_elapsed >= _currentSpawnTime)
            {
               StartCoroutine(PrepareReady());
            }
        }

        DoUpdate(dt);
    }

    private void OnDrag(Vector2 worldPos)
    {
        if(!CanDrag())
        {
            return;
        }

        SlotContents.transform.position = worldPos + _gameController.DragOffset;
        _gameController.DragItem(this, SlotItem, worldPos);
    }

    public bool CanDrag()
    {
        return SlotItem != null && SlotItem.InteractionType == InteractionType.Drag && _draggingSlot;
    }

    IEnumerator PrepareReady()
    {
        yield return new WaitForSeconds(0.1f);
        PrepareReadyFunc();
    }

    protected void PrepareReadyFunc()
    {
        SetLogicReady();
        SetViewReady();      
    }

    protected IEnumerator PrepareBusy()
    {
        yield return new WaitForSeconds(0.1f);
        if (_currentSpawnTime > 0)
        {
            SetLogicBusy();
            SetViewBusy();        
        }
        else
        {
            //yield return PrepareReady();
            PrepareReadyFunc();
        }
    }

    protected virtual void SetViewBusy()
    {
        Percent.gameObject.SetActive(true);
        var color = SlotContents.color;
        color.a = 0.5f;
        SlotContents.enabled = true;
        SlotContents.color = color;
    }

    protected virtual void SetLogicBusy()
    {
        _slotStatus = SlotStatus.Busy;
        _elapsed = 0;
    }

    protected virtual void SetViewReady()
    {
        Percent.gameObject.SetActive(false);
        SlotContents.sprite = SlotItem.Icon;
        var color = SlotContents.color;
        color.a = 1;
        SlotContents.color = color;
    }

    protected virtual void SetLogicReady()
    {
        _elapsed = 0;
        _slotStatus = SlotStatus.ItemReady;
    }


    protected abstract void DoInit();
    protected abstract void DoLoad();
    protected abstract void DoUnload();
    protected virtual void DoUpdate(float dt) {}
    protected abstract void DoClearContents();

    public void DoDrag(Vector2 pos)
    {
    }

    public void DoDragEnd(Vector2 pos)
    {
        throw new NotImplementedException();
    }

    public void DoDragStart(Vector2 pos)
    {
        throw new NotImplementedException();
    }

    public void ClearContents()
    {
        ResetSlot();
        SlotContents.sprite = null;
        _slotStatus = SlotStatus.AwaitingRequirements;
        DoClearContents();
    }


    public bool ContainsPosition(Vector2 pos)
    {
        return CollisionArea.OverlapPoint(pos);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!CanProcessClick())
        {
            return;
        }
        DoPointerClick();
    }

    protected virtual void DoPointerClick() {}

    protected bool CanProcessClick()
    {
        Item item = SlotItem;
        return item != null && item.InteractionType == InteractionType.Click && _slotStatus == SlotStatus.ItemReady;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!CanProcessPointerDown())
        {
            return;
        }

        SlotContents.sortingOrder += 100;
        _draggingSlot = true;
        _dragStartPosition = SlotContents.transform.position;
        _dragStartScale = SlotContents.transform.localScale;
        Vector2 worldPos = eventData.pointerCurrentRaycast.worldPosition;
        SlotContents.transform.localScale = _gameController.DragScale;
        SlotContents.transform.position = worldPos + _gameController.DragOffset;
        _gameController.DragItemStart(this, SlotItem, worldPos);
    }

    public bool CanProcessPointerDown()
    {
        Item itemContents = SlotItem;
        return itemContents != null && itemContents.InteractionType == InteractionType.Drag
            && !_draggingSlot && _slotStatus != SlotStatus.Busy;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanProcessPointerUp())
        {
            return;
        }

        if (_draggingSlot)
        {
            SlotContents.sortingOrder -= 100;
            _draggingSlot = false;
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            SlotContents.transform.position = worldPos + _gameController.DragOffset;

            _gameController.DragItemEnd(this, SlotItem, worldPos);
        }
    }

    public bool CanProcessPointerUp()
    {
        Item itemContents = SlotItem;
        return itemContents != null && itemContents.InteractionType == InteractionType.Drag && _draggingSlot;
    }

    public void ResetSlot()
    {
        SlotContents.transform.position = _dragStartPosition;
        SlotContents.transform.localScale = _dragStartScale;
    }
}
