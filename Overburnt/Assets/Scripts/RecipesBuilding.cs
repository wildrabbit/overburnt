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

    internal void Reset()
    {
        foreach (var slot in Slots)
        {
            slot.Init(this);
        }
    }

    // Update is called once per frame
    public void UpdateGame(float dt)
    {
        foreach (var slot in Slots)
        {
            slot.UpdateSlot(dt);
        }
    }

    public SlotRecipesBuilding FindSlotAtWithRequirement(Vector2 pos, Item item, out Recipe recipeData)
    {
        recipeData = null;
        SlotRecipesBuilding slotAtPos = Slots.Find(x => x.ContainsPosition(pos));
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
