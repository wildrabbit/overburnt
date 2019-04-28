using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResourceBuildingInfo
{
    public ResourceBuilding Building;
    public List<SlotResourceBuilding> Slots;
}

[System.Serializable]
public class RecipeBuildingSlotInfo
{
    public SlotRecipesBuilding Slot;
    public List<RecipeID> AvailableRecipes; //Subset of the slot's recipes
}

[System.Serializable]
public class RecipeBuildingInfo
{
    public RecipesBuilding Building;
    public List<RecipeBuildingSlotInfo> Slots;
}

public class LevelConfig : MonoBehaviour
{
    public string DescriptionStart;
    public string DescriptionEnd;
    public Sprite[] HintGraphics;

    public List<ResourceBuildingInfo> ActiveResourcesBuildings;    
    public List<RecipeBuildingInfo> ActiveRecipeBuildings;
    public List<ClientSlot> ActiveSlots;
    public List<DisposalFacility> Wastelands;

    public float LevelTimeSeconds;
    public int NumClients;
    public int DistributionNoise;
    public int MinRevenue;
    public int GoodRevenue;
    public int GreatRevenue;

    public List<GameObject> ClientViews;
    public List<ItemID> ClientRequestsItemPoolItem1;
    public List<ItemID> ClientRequestsItemPoolItem2;
    public float SpawnChanceSecondItem;
    public int PatienceMin;
    public int PatienceMax;

    public bool IgnoreFatigueThresholds;
}
