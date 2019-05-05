using System.Collections.Generic;

[System.Serializable]
public class RecipeBuildingSlotInfo
{
    public SlotRecipesBuilding Slot;
    public List<RecipeID> AvailableRecipes; //Subset of the slot's recipes
}
