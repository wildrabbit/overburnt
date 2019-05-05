using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotRecipesBuilding : BaseSlot
{
    public List<RecipeBuildingData> AllowedRecipes;
    List<ItemID> _currentRequirements;
    List<RecipeBuildingData> AvailableRecipes;

    Recipe _currentRecipe;
    Item _currentItem;
    RecipeBuildingSlotInfo _recipeSlotData;

    public override Item SlotItem
    {
        get => _currentItem;
        set => _currentItem = value;
    }
    public bool MissingRecipe => _currentRecipe == null;

    protected override void DoInit()
    {
        AvailableRecipes = new List<RecipeBuildingData>();
    }


    public void ApplyOverrides(RecipeBuildingSlotInfo overrideSlotData)
    {
        _recipeSlotData = overrideSlotData;
    }

    protected override void DoLoad()
    {
        _currentRecipe = null;
        _currentItem = null;
        _currentRequirements = new List<ItemID>();
        _currentSpawnTime = 0;

        _slotStatus = SlotStatus.AwaitingRequirements;
        Percent.gameObject.SetActive(false);
        var color = SlotContents.color;
        color.a = 1;
        SlotContents.enabled = false;

        AvailableRecipes.Clear();
        foreach(var recipe in AllowedRecipes)
        {
            if(_recipeSlotData.AvailableRecipes.Contains(recipe.Recipe))
            {
                AvailableRecipes.Add(recipe);
            }
        }
    }

    protected override void DoUnload()
    {
        gameObject.SetActive(false);
    }

    internal Recipe FindRecipeUsingItem(Item item)
    {
        if (_slotStatus == SlotStatus.Busy)
        {
            return null;
        }

        bool pendingPickup = _slotStatus == SlotStatus.ItemReady;
        Recipe candidateRecipe = null;
        foreach (var recipe in AvailableRecipes)
        {
            Recipe recipeData = _gameController.GetRecipe(recipe.Recipe);
            if (!recipeData.Requirements.Contains(item.ItemID))
            {
                continue;
            }

            if (pendingPickup)
            {
                if (!IsCurrentItemRequirementForRecipe(recipeData))
                {
                    continue;
                }
                else
                {
                    candidateRecipe = recipeData;
                    break;
                }
            }
            else
            {
                if (candidateRecipe == null)
                {
                    candidateRecipe = recipeData;
                }
                else if (recipeData.IsShorterThan(candidateRecipe))
                {
                    candidateRecipe = recipeData;
                }
            }
        }
        return candidateRecipe;
    }

    public bool ExistsRecipeWithItem(Item item)
    {
        foreach(var recipe in AvailableRecipes)
        {
            Recipe recipeData = _gameController.GetRecipe(recipe.Recipe);
            if(recipeData.Requirements.Contains(item.ItemID))
            {
                return true;
            }
        }
        return false;
    }
    
    protected override void SetLogicReady()
    {
        base.SetLogicReady();
        _currentItem = _gameController.GetItem(_currentRecipe.OutputItem);
        _currentRequirements.Clear();
        _currentRecipe = null;
        _currentSpawnTime = 0;
    }

    protected override void SetViewReady()
    {
        base.SetViewReady();
        // if incomplete and there is any recipe here targetting the final item we can change the current recipe and show what's missing.
    }

    internal void AddRequirement(Item itemData)
    {
        _currentRequirements.Add(itemData.ItemID);
        bool allRequirements = true;
        foreach(var req in _currentRecipe.Requirements)
        {
            if(!_currentRequirements.Contains(req))
            {
                allRequirements = false;
                break;
            }
        }

        if(allRequirements)
        {
            StartCoroutine(PrepareBusy());
        }
    }

    internal bool NeedsItem(Item itemData)
    {
        if(_currentRecipe == null)
        {
            return false;
        }
        
        if(!_currentRecipe.Requirements.Contains(itemData.ItemID))
        {
            return false;
        }

        if(_currentRequirements.Contains(itemData.ItemID))
        {
            return false;
        }

        return true;

    }

    protected override void SetViewBusy()
    {
        base.SetViewBusy();
        SlotContents.sprite = _gameController.GetItem(_currentRecipe.OutputItem)?.Icon;

    }

    public bool SetActiveRecipe(Recipe recipeData, Item itemData)
    {
        if(_currentItem != null && _currentItem == itemData)
        {
            return false;
        }

        _currentRecipe = recipeData;
        _currentRequirements.Clear();
        SlotContents.sprite = null;

        bool pickupPending = _slotStatus == SlotStatus.ItemReady;
        _slotStatus = SlotStatus.AwaitingRequirements;
        
        if (_currentItem != null && IsCurrentItemRequirementForRecipe(_currentRecipe))
        {
            AddRequirement(_currentItem);
        }

        var buildingRecipeData = AvailableRecipes.Find(x => x.Recipe == recipeData.RecipeID);
        _currentSpawnTime = buildingRecipeData.Time;

        AddRequirement(itemData);
        return true;
    }

    public bool IsCurrentItemRequirementForRecipe(Recipe currentRecipe)
    {
        return currentRecipe.Requirements.Contains(_currentItem.ItemID);
    }

    public void OnDrag(Vector2 worldPos)
    {
        if (_currentItem.InteractionType != InteractionType.Drag)
        {
            return;
        }

        _gameController.DragItem(this, _currentItem, worldPos);
    }

    protected override void DoClearContents()
    {
        _currentRecipe = null;
        _currentItem = null;
        _currentRequirements.Clear();
        _currentSpawnTime = 0;
    }
}