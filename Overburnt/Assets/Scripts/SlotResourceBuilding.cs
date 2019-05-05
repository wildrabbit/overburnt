using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IItemGenerator
{
    Item SlotItem { get; set; }
    bool ContainsPosition(Vector2 pos);
    void ResetSlot();
    void ClearContents();
}

public class SlotResourceBuilding : BaseSlot
{
    public ItemID ItemID;
    public float SpawnTime;
    public bool BeginReady;

    Item _itemData;

    public override Item SlotItem
    {
        get => _itemData;
        set => _itemData = value;
    }

    protected override void DoInit() {}

    protected override void DoLoad()
    {
        _currentSpawnTime = SpawnTime;
        _itemData = _gameController.GetItem(ItemID);
        SlotContents.sprite = _itemData?.Icon;

        if (BeginReady)
        {
            SetViewReady();
            SetLogicReady();
        }
        else
        {
            SetViewBusy();
            SetLogicBusy();
        }

    }

    protected override void DoUnload()
    {
    }

    protected override void DoClearContents()
    {
        StartCoroutine(PrepareBusy());
    }
}