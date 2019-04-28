using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using URandom = UnityEngine.Random;

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

public class RequestData
{
    public int TicketIdx;
    public GameObject ClientPrefab;
    public List<ItemID> Items;
    public float Timeout;
}

public class GameController : MonoBehaviour
{
    public List<Item> ItemDataList;

    internal Recipe GetRecipe(RecipeID recipeID)
    {
        return RecipeDataList.Find(x => x.RecipeID == recipeID);
    }

    public List<Recipe> RecipeDataList;

    public List<ResourceBuilding> ResourceBuildings;
    public List<RecipesBuilding> RecipeBuildings;

    public List<ClientSlot> ClientRequestSlots;

    public float GameTime;
    public int NumRequests;
    public int RequestNoise = 2;

    public float Chance2ItemRequest = 0.25f;
    public List<ItemID> OptionsItem1;
    public List<ItemID> OptionsItem2;
    public List<GameObject> ClientView;
    public float PatienceMin;
    public float PatienceMax;

    public int MinRevenue;
    public int GoodRevenue;
    public int GreatRevenue;

    Dictionary<ClientSlot, RequestData> _requestAllocations;

    float _elapsed;
    float _requestElapsed;
    int _requestIdx;
    bool _finished;
    int _revenue;

    float[] _requestTimes;
    Queue<RequestData> _stalledRequests; // Generated, but no free space

    public float TimeLeft => (GameTime - _elapsed);

    public int Earnings => _revenue;

    IItemGenerator _draggedItem;
    Item _itemData;
    int _numFailures;

    public event Action<int> OnEarningsChanged;


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

        _requestAllocations = new Dictionary<ClientSlot, RequestData>();

        _requestTimes = new float[NumRequests];
        float avgTime = GameTime / NumRequests;
        for(int i = 0; i < NumRequests; ++i)
        {
            _requestTimes[i] = Mathf.Clamp(i * avgTime * UnityEngine.Random.Range(-RequestNoise, RequestNoise), 0, GameTime);
        }
        _requestIdx = 0;
        _numFailures = 0;
        _revenue = 0;
        foreach(var slot in ClientRequestSlots)
        {
            slot.Clear();
        }
        _stalledRequests = new Queue<RequestData>();
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
            // requests check:
            if(_requestIdx < NumRequests)
            {
                _requestElapsed += dt;
                if(_requestElapsed >= _requestTimes[_requestIdx])
                {
                    RequestData reqData = GenerateRequest();
                    int slotIdx = ClientRequestSlots.FindIndex(x => !_requestAllocations.ContainsKey(x));
                    if(slotIdx < 0)
                    {
                        _stalledRequests.Enqueue(reqData);
                    }
                    else
                    {
                        _requestAllocations[ClientRequestSlots[slotIdx]] = reqData;
                        ClientRequestSlots[slotIdx].InitClient(this, reqData);
                    }
                    _requestIdx++;
                    _requestElapsed = 0;
                }
            }

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

    private RequestData GenerateRequest()
    {
        List<ItemID> items = new List<ItemID>();
        items.Add(OptionsItem1[URandom.Range(0, OptionsItem1.Count - 1)]);
        if(URandom.value < Chance2ItemRequest)
        {
            items.Add(OptionsItem2[URandom.Range(0, OptionsItem2.Count - 1)]);
        }
        RequestData reqData = new RequestData()
        {
            Timeout = URandom.Range(PatienceMin, PatienceMax),
            ClientPrefab = ClientView[URandom.Range(0, ClientView.Count - 1)],
            TicketIdx = _requestIdx,
            Items = items
        };
        return reqData;
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

        foreach(var slot in ClientRequestSlots)
        {
            if(!slot.Active)
            {
                continue;
            }

            if(!slot.ContainsPosition(pos))
            {
                continue;
            }

            if(slot.IsRequestedItem(itemData))
            {
                slot.GiveItem(itemData.ItemID);
            }
        }

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

    public void RequestFulfilled(ClientSlot clientSlot)
    {
        var data = _requestAllocations[clientSlot];
        float timeLeftPercent = 100 * (1 - clientSlot.TimeElapsed / data.Timeout);
        int revenue = 0;
        foreach (var itemID in data.Items)
        {
            Item itemData = GetItem(itemID);
            revenue += itemData.BaseRevenue; // TODO: Extra score
        }
        // TODO: Notify revenue (and then add?)
        _revenue += revenue;
        OnEarningsChanged?.Invoke(_revenue);
        Debug.Log($"Request {data.TicketIdx} succeeded. Revenue: {revenue} (Total: {_revenue})");
        _requestAllocations.Remove(clientSlot);

        if(_stalledRequests.Count > 0)
        {
            data = _stalledRequests.Dequeue();
            clientSlot.InitClient(this, data);
        }
    }

    internal void ClientRequestFailed(ClientSlot clientSlot)
    {
        var reqData = _requestAllocations[clientSlot];
        Debug.Log($"Request {reqData.TicketIdx} failed. Contents: {reqData.Items}");
        _numFailures++;
        _requestAllocations.Remove(clientSlot);
        if (_stalledRequests.Count > 0)
        {
            var data = _stalledRequests.Dequeue();
            clientSlot.InitClient(this, data);
        }

    }
}
