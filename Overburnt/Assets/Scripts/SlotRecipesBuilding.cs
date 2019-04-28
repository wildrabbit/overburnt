using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlotRecipesBuilding : MonoBehaviour, IItemGenerator, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_currentItem == null || _currentItem.InteractionType != InteractionType.Drag)
        {
            return;
        }

        if (!_dragging)
        {
            _dragging = true;
            _dragStartPosition = Slot.transform.position;
            Vector2 worldPos = eventData.pointerCurrentRaycast.worldPosition;
            Slot.transform.position = worldPos;
            _gameController.DragItemStart(this, _currentItem, worldPos);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_currentItem == null || _currentItem.InteractionType != InteractionType.Drag)
        {
            return;
        }

        if (_dragging)
        {
            _dragging = false;
            Slot.transform.position = eventData.pointerPressRaycast.worldPosition;
            _gameController.DragItemEnd(this, _currentItem, eventData.position);
        }
    }

    public List<RecipeBuildingData> AllowedRecipes;
    public SpriteRenderer SlotBG;
    public SpriteRenderer Slot;
    public Collider2D CollisionArea;
    public TMPro.TextMeshPro Percent;

    public List<ItemID> _currentRequirements;

    Recipe _currentRecipe;
    Item _currentItem;
    float _elapsed;
    float _currentSpawnTime;

    public RecipeSlotStatus SlotStatus;
    
    RecipesBuilding _parent;
    GameController _gameController;

    bool _dragging;
    Vector2 _dragStartPosition;

    public bool MissingRecipe => _currentRecipe == null;
   

    public void Init(RecipesBuilding parent)
    {
        _parent = parent;
        _gameController = parent.GameController;
        _elapsed = 0;
        _dragging = false;
        _dragStartPosition = Vector2.zero;

        _currentRecipe = null;
        _currentItem = null;
        _currentRequirements = new List<ItemID>();
        _currentSpawnTime = 0;

        Slot.enabled = false;
        SlotStatus = RecipeSlotStatus.AwaitingRequirements;
        Percent.gameObject.SetActive(false);
        var color = Slot.color;
        color.a = 1;
        Slot.enabled = false;        
    }

    internal Recipe FindRecipeUsingItem(Item item)
    {
        Recipe candidateRecipe = null;
        foreach (var recipe in AllowedRecipes)
        {
            Recipe recipeData = _gameController.GetRecipe(recipe.Recipe);
            if (!recipeData.Requirements.Contains(item.ItemID))
            {
                continue;
            }

            if (candidateRecipe == null)
            {
                candidateRecipe = recipeData;
            }
            else if(SlotStatus == RecipeSlotStatus.PickupPending && IsCurrentItemRequirementForRecipe(recipeData))
            {
                candidateRecipe = recipeData;
                break;
            }
            else if(recipeData.IsShorterThan(candidateRecipe))
            {
                candidateRecipe = recipeData;
            }
        }
        return candidateRecipe;
    }

    public bool ExistsRecipeWithItem(Item item)
    {
        foreach(var recipe in AllowedRecipes)
        {
            Recipe recipeData = _gameController.GetRecipe(recipe.Recipe);
            if(recipeData.Requirements.Contains(item.ItemID))
            {
                return true;
            }
        }
        return false;
    }

    public void UpdateSlot(float dt)
    {
        if (_dragging)
        {
            Vector2 mousePos = Input.mousePosition;
            OnDrag(Camera.main.ScreenToWorldPoint(mousePos));
        }

        if (SlotStatus == RecipeSlotStatus.Generating)
        {
            Percent.text = $"{(int)(100 * _elapsed / _currentSpawnTime)}%";
            if (Mathf.Approximately(_elapsed, _currentSpawnTime))
            {
                return;
            }
            _elapsed += dt;
            if (_elapsed >= _currentSpawnTime)
            {
                _elapsed = _currentSpawnTime;
                StartCoroutine(PrepareReady());
            }
        }
    }

    IEnumerator PrepareReady()
    {
        yield return new WaitForSeconds(0.1f);
        _currentItem = _gameController.GetItem(_currentRecipe.OutputItem);
        _currentRequirements.Clear();
        _currentRecipe = null;
        _currentSpawnTime = 0;
        Percent.gameObject.SetActive(false);
        _elapsed = 0;
        var color = Slot.color;
        color.a = 1;
        Slot.enabled = true;
        Slot.sprite = _currentItem.Icon;
        Slot.color = color;
        SlotStatus = RecipeSlotStatus.PickupPending;
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
            StartCoroutine(PrepareGenerating());
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

    IEnumerator PrepareGenerating()
    {
        yield return new WaitForSeconds(0.1f);
        if (_currentSpawnTime > 0)
        {
            Slot.enabled = true;
            Slot.sprite = _gameController.GetItem(_currentRecipe.OutputItem)?.Icon;
            Percent.gameObject.SetActive(true);
            _elapsed = 0;
            var color = Slot.color;
            color.a = 0.5f;
            Slot.enabled = true;
            Slot.color = color;
            SlotStatus = RecipeSlotStatus.Generating;
        }
        else yield return PrepareReady();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_currentItem.InteractionType != InteractionType.Click)
        {
            return;
        }
    }

    public void SetActiveRecipe(Recipe recipeData, Item itemData)
    {
        _currentRecipe = recipeData;
        _currentRequirements.Clear();
        Slot.sprite = null;
        SlotStatus = RecipeSlotStatus.AwaitingRequirements;

        if (_currentItem != null && IsCurrentItemRequirementForRecipe(_currentRecipe))
        {
            AddRequirement(_currentItem);
            _currentItem = null;
        }

        var buildingRecipeData = AllowedRecipes.Find(x => x.Recipe == recipeData.RecipeID);
        _currentSpawnTime = buildingRecipeData.Time;

        AddRequirement(itemData);
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

    public void DoDragStart(Vector2 pos)
    {
        if(!_dragging)
        {
            _dragging = true;
            Slot.sortingOrder += 100;
            _dragStartPosition = Slot.transform.position;
            Slot.transform.position = pos;
        }
    }

    public void DoDragEnd(Vector2 pos)
    {
        if(_dragging)
        {
            Slot.sortingOrder -= 100;
            _dragging = false;
            Slot.transform.position = pos;
        }
    }

    public void DoDrag(Vector2 pos)
    {
        if (_dragging)
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
        SlotStatus = RecipeSlotStatus.AwaitingRequirements;
        _currentRecipe = null;
        _currentItem = null;
        _currentRequirements.Clear();
        _currentSpawnTime = 0;
    }
}