using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum ItemID
{
    Iron,
    Wood,
    Bread,
    Stick,
    SpearPoint,
    Spear,
    IronSword,
    WoodenSword
}

public enum RecipeID
{
    IronSword,
    SpearFromStick,
    SpearFromPoint,
    WoodenStick,
    IronSpearPoint
}

public enum ItemType
{
    Iron,
    Wood,
    Bread,
    Stick,
    Spear,
    Sword
}

public enum ItemNature
{
    Raw,
    Intermediate,
    Prepared
}

public enum InteractionType
{
    Click,
    Drag
}

[Serializable]
public class Item
{
    [FormerlySerializedAs("NameID")]public string Name;
    public ItemID ItemID;
    public Sprite Icon;
    public ItemType Type;
    public ItemNature Nature;
    public InteractionType InteractionType;
    public int BaseRevenue;
    public int ClientWaitTimePercentRestored;
}

[Serializable]
public class Recipe
{
    public string Name;
    public RecipeID RecipeID;
    public List<ItemID> Requirements;
    public ItemID OutputItem;
    public Sprite Icon;

    internal bool IsShorterThan(Recipe recipeData)
    {
        return Requirements.Count < recipeData.Requirements.Count;
    }
}

[Serializable]
public class RecipeBuildingData
{
    public RecipeID Recipe;
    public float Time;
}


public class GameController : MonoBehaviour
{
    public List<Item> ItemDataList;

    internal Recipe GetRecipe(RecipeID recipeID)
    {
        return RecipeDataList.Find(x => x.RecipeID == recipeID);
    }

    public List<Recipe> RecipeDataList;

    [FormerlySerializedAs("Buildings")] public List<ResourceBuilding> ResourceBuildings;
    public List<RecipesBuilding> RecipeBuildings;
    public float GameTime;

    float _elapsed;
    bool _finished;

    public float TimeLeft => (GameTime - _elapsed);

    IItemGenerator _draggedItem;
    Item _itemData;

    // TODO: Queue

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Init!");
        Reset();    
    }

    private void Reset()
    {
        _elapsed = 0;
        _finished = false;
        foreach (var building in ResourceBuildings)
        {
            building.Reset();
        }

        foreach(var building in RecipeBuildings)
        {
            building.Reset();
        }
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        if (_finished)
        {
            if(Input.anyKey)
            {
                Debug.Log("Restart!");
                Reset();
            }
            return;
        }
        _elapsed += dt;
        if (_elapsed >= GameTime)
        {
            Debug.Log("Timeout!");
            _finished = true;
        }
        else
        {
            foreach(var resBuilding in ResourceBuildings)
            {
                resBuilding.UpdateGame(dt);
            }
            foreach (var recBuilding in RecipeBuildings)
            {
                recBuilding.UpdateGame(dt);
            }
        }
    }

    public Item GetItem(ItemID item)
    {
        return ItemDataList.Find(x => x.ItemID == item);
    }

    public void ItemClicked(IItemGenerator slotResourceBuilding, Item itemData)
    {
        throw new NotImplementedException();
    }

    public void DragItemStart(IItemGenerator slotResourceBuilding, Item itemData, Vector2 pos)
    {
        slotResourceBuilding.DoDragStart(pos);
    }

    public void DragItemEnd(IItemGenerator slotResourceBuilding, Item itemData, Vector2 pos)
    {
        slotResourceBuilding.DoDragEnd(pos);
        // Valid  resource generator?
        Recipe recipeData;
        SlotRecipesBuilding buildingSlot = FindRecipeBuildingSlotForItemAtPosition(pos, itemData, out recipeData);
        if(buildingSlot != null)
        {
            if(buildingSlot.MissingRecipe)
            {
                buildingSlot.SetActiveRecipe(recipeData, itemData);
            }
            else
            {
                buildingSlot.AddRequirement(itemData);
            }

            slotResourceBuilding.ClearContents();
            return;           
        }

        // TODO: Valid client drag?

        // Otherwise: 
        slotResourceBuilding.ResetSlot();

    }

    private SlotRecipesBuilding FindRecipeBuildingSlotForItemAtPosition(Vector2 pos, Item itemData, out Recipe recipeData)
    {
        recipeData = null;
        foreach(var building in RecipeBuildings)
        {
            SlotRecipesBuilding slot = building.FindSlotAtWithRequirement(pos, itemData, out recipeData);
            if(slot != null)
            {
                return slot;
            }
        }
        return null;
    }

    public void DragItem(IItemGenerator slotResourceBuilding, Item itemData, Vector2 pos)
    {
        slotResourceBuilding.DoDrag(pos);
    }
}
