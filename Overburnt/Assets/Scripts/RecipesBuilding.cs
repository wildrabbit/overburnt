using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum RecipeSlotStatus
{
    AwaitingRequirements,
    Incomplete,
    Generating,
    PickupPending,
 }


public class RecipesBuilding : MonoBehaviour
{
    public GameController GameController;

    public List<SlotRecipesBuilding> Slots;

    List<SlotRecipesBuilding> _activeSlots;

    public void Init(GameController gameController)
    {
        GameController = gameController;
        foreach(var slot in Slots)
        {
            slot.Init(this);
        }
        _activeSlots = new List<SlotRecipesBuilding>();
    }

    public void LoadBuilding(RecipeBuildingInfo info)
    {
        gameObject.SetActive(true);
        _activeSlots.Clear();
        foreach(var slot in Slots)
        {
            var slotInfo = info.Slots.Find(x => x.Slot == slot);
            if(slotInfo != null)
            {
                slot.Load(slotInfo);
                _activeSlots.Add(slot);
            }
            else
            {
                slot.Unload();
            }
        }
    }

    public void Unload()
    {
        gameObject.SetActive(false);
        foreach(var slot in _activeSlots)
        {
            slot.Unload();
        }
        _activeSlots.Clear();
    }

    // Update is called once per frame
    public void UpdateGame(float dt)
    {
        foreach (var slot in _activeSlots)
        {
            slot.UpdateSlot(dt);
        }
    }

    public SlotRecipesBuilding FindSlotAtWithRequirement(Vector2 pos, Item item, out Recipe recipeData)
    {
        recipeData = null;
        SlotRecipesBuilding slotAtPos = _activeSlots.Find(x => x.ContainsPosition(pos));
        if(slotAtPos != null)
        {
            recipeData = slotAtPos.FindRecipeUsingItem(item);
            if (recipeData == null)
            {
                slotAtPos = null;
            }
        }
        return slotAtPos;
    }
}
