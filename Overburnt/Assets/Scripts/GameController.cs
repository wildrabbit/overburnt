using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using URandom = UnityEngine.Random;

[Serializable]
public class FatigueRestoreThreshold
{
    public float Value;
    public float FatigueRecoveryPercent;
}

[Serializable]
public class EarningModifierThresholds
{
    public float TimeLeftThresholdValue;
    public float ExtraPercent;
}

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
    IronSpearPoint,
    WoodenSword
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

public enum Result
{
    Running,
    LostExhaustion,
    LostEarnings,
    WonBase,
    WonGood,
    WonGreat
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

    public override string ToString()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        var itemValues = Items.ConvertAll(x => x.ToString()).ToArray();

        builder.Append($"[Client request ID:{TicketIdx}, Items: [{String.Join(",", itemValues)}]");
        return builder.ToString();
    }
}

public class GameController : MonoBehaviour
{
    [Header("Config")]
    public List<Item> ItemDataList;
    public List<Recipe> RecipeDataList;
    public List<LevelConfig> LevelList;
    public List<ResourceBuilding> AllResourceBuildings;
    public List<RecipesBuilding> AllRecipeBuildings;
    public List<ClientSlot> AllClientSlots;
    public List<DisposalFacility> AllDisposalFacilities;


    public int LevelIdx;
    public LevelConfig CurrentLevel;

    List<ResourceBuilding> ActiveResourceBuildings;
    List<RecipesBuilding> ActiveRecipeBuildings;
    List<ClientSlot> ActiveClientRequestSlots;
    List<DisposalFacility> ActiveDisposalFacilities;


    [Header("Player progression stuff")]
    public float StartFatigue;
    public float MaxFatigue;
    public float DragDepletionRate;
    public float IdleToRestoreTime;
    public float FatigueRestoreRate;
    public List<FatigueRestoreThreshold> FatigueRestoreThresholds;
    public List<EarningModifierThresholds> EarningModifiers;

    [Header("Misc")]
    public float LevelResumeTime;

    bool _dragging;
    float _elapsedSinceLastDragFinished = -1;    
    float _fatigue;
    
    Dictionary<ClientSlot, RequestData> _requestAllocations;

    float _gameTimerElapsed;
    float _requestElapsed;
    int _requestIdx;
    bool _finished;
    int _revenue;

    float[] _requestTimes;
    Queue<RequestData> _stalledRequests; // Generated, but no free space

    public float TimeLeft => _finished ? 0 : (CurrentLevel.LevelTimeSeconds - _gameTimerElapsed);

    public int Earnings => _revenue;
    public float Fatigue => _fatigue;
    public int FatiguePercent => Mathf.RoundToInt(100 * _fatigue / MaxFatigue);

    public int TargetIncome
    {
        get
        {
            if(_revenue < CurrentLevel.MinRevenue)
            {
                return CurrentLevel.MinRevenue;                
            }
            else if(_revenue < CurrentLevel.GoodRevenue)
            {
                return CurrentLevel.GoodRevenue;
            }
            else if(_revenue < CurrentLevel.GreatRevenue)
            {
                return CurrentLevel.GreatRevenue;
            }
            return -1;
        }
    }
    IItemGenerator _draggedItem;
    Item _itemData;
    int _numFailures;
    private Result _result;

    public event Action<int> OnEarningsChanged;
    public event Action GameReset;
    public event Action<Result,int,int, float> GameFinished;
    public event Action<int> OnFatigueChanged;
    public event Action<float> GameBeaten;
    public event Action GameReady;

    public event Action<int, LevelConfig, float> GameStarted;

    public float SecondsToGameStart;
    bool _startReady;

    // Start is called before the first frame update
    void Start()
    {
        InitGame();
        ResetGame();
        InitLevel(LevelList[LevelIdx]);
    }

    void ResetGame()
    {
        StartFatigue = 0;
    }

    void InitGame()
    {
        _requestAllocations = new Dictionary<ClientSlot, RequestData>();

        ActiveClientRequestSlots = new List<ClientSlot>();
        foreach(var slot in AllClientSlots)
        {
            slot.Init(this);
        }
        ActiveResourceBuildings = new List<ResourceBuilding>();
        foreach(var resB in AllResourceBuildings)
        {
            resB.Init(this);
        }
        ActiveRecipeBuildings = new List<RecipesBuilding>();
        foreach(var recB in AllRecipeBuildings)
        {
            recB.Init(this);
        }

        ActiveDisposalFacilities = new List<DisposalFacility>();
        foreach(var wasteland in AllDisposalFacilities)
        {
            wasteland.Init(this);
        }
    }

    void InitLevel(LevelConfig config)
    {
        CurrentLevel = config;
        _result = Result.Running;
        _gameTimerElapsed = 0;
        _finished = false;

        ActiveResourceBuildings.Clear();
        foreach(var building in AllResourceBuildings)
        {
            ResourceBuildingInfo resInfo = config.ActiveResourcesBuildings.Find(x => x.Building == building);
            if (resInfo != null)
            {
                building.LoadBuilding(resInfo);
                ActiveResourceBuildings.Add(building);
            }
            else
            {
                building.Unload();
            }
        }

        ActiveRecipeBuildings.Clear();
        foreach (var building in AllRecipeBuildings)
        {
            RecipeBuildingInfo resInfo = config.ActiveRecipeBuildings.Find(x => x.Building == building);
            if (config.ActiveRecipeBuildings.Exists(x => x.Building == building))
            {
                building.LoadBuilding(resInfo);
                ActiveRecipeBuildings.Add(building);
            }
            else
            {
                building.Unload();
            }
        }

        _requestAllocations.Clear();
        ActiveClientRequestSlots.Clear();
        foreach(var slot in AllClientSlots)
        {
            slot.Clear();
            if (config.ActiveSlots.Contains(slot))
            {
                ActiveClientRequestSlots.Add(slot);
            }
        }

        ActiveDisposalFacilities.Clear();
        foreach (var wasteland in AllDisposalFacilities)
        {
            if (config.Wastelands.Contains(wasteland))
            {
                wasteland.Load();
                ActiveDisposalFacilities.Add(wasteland);
            }
            else
            {
                wasteland.UnLoad();
            }
        }
       
        int numClients = CurrentLevel.NumClients;
        _requestTimes = new float[numClients];
        float avgTime = CurrentLevel.LevelTimeSeconds / CurrentLevel.NumClients;
        for (int i = 0; i < numClients; ++i)
        {
            _requestTimes[i] = Mathf.Clamp(i * avgTime + UnityEngine.Random.Range(-CurrentLevel.DistributionNoise, CurrentLevel.DistributionNoise), 0, CurrentLevel.LevelTimeSeconds);
        }
        _requestIdx = 0;
        _requestElapsed = 0;
        _numFailures = 0;
        _revenue = 0;


        _stalledRequests = new Queue<RequestData>();

        GameStarted?.Invoke(LevelIdx, CurrentLevel, SecondsToGameStart);

        _dragging = false;
        _fatigue = StartFatigue;
        _elapsedSinceLastDragFinished = -1.0f;

        _startReady = false;
        StartCoroutine(DelayedReady());
    }

    IEnumerator DelayedReady()
    {
        yield return new WaitForSeconds(SecondsToGameStart);
        while(!Input.anyKey)
        {
            yield return null;
        }
        _startReady = true;
        GameReady?.Invoke();
    }

    void Update()
    {
        if (!_startReady)
        {
            return;
        }
            
        float dt = Time.deltaTime;
        if (_finished)
        {
            if(Input.anyKey && Time.time - _gameTimerElapsed > LevelResumeTime)
            {
                if(_result != Result.Running && _result != Result.LostEarnings && _result != Result.LostExhaustion)
                {
                    LevelIdx++;
                    if(LevelIdx == LevelList.Count)
                    {
                        LevelIdx = 0;
                        GameBeaten?.Invoke(LevelResumeTime);
                        InitLevel(LevelList[LevelIdx]);
                    }
                    else
                    {
                        CheckStartFatigue();
                        GameReset?.Invoke();
                        InitLevel(LevelList[LevelIdx]);
                    }
                }
                else
                {
                    GameReset?.Invoke();
                    InitLevel(LevelList[LevelIdx]);
                }
            }
            return;
        }
        _gameTimerElapsed += dt;
        bool exhausted = _result == Result.LostExhaustion;
        int nextRev = 0;

        if (_gameTimerElapsed >= CurrentLevel.LevelTimeSeconds || exhausted)
        {
            if(_revenue < CurrentLevel.MinRevenue)
            {
                _result = Result.LostEarnings;
                nextRev = CurrentLevel.MinRevenue;
            }
            else if(_revenue < CurrentLevel.GoodRevenue)
            {
                _result = Result.WonBase;
                nextRev = CurrentLevel.GoodRevenue;
            }
            else if (_revenue < CurrentLevel.GreatRevenue)
            {
                _result = Result.WonGood;
                nextRev = CurrentLevel.GreatRevenue;
            }
            else
            {
                _result = Result.WonGreat;
            }

            if(exhausted)
            {
                _result = Result.LostExhaustion; // Override to avoid several ifs above.
            }

            GameFinished?.Invoke(_result, _revenue, nextRev, LevelResumeTime);
            _finished = true;
            _gameTimerElapsed = Time.time;
        }
        else
        {
            // requests check:
            if(_requestIdx < CurrentLevel.NumClients)
            {
                _requestElapsed += dt;
                if(_requestElapsed >= _requestTimes[_requestIdx])
                {
                    RequestData reqData = GenerateRequest();
                    int slotIdx = ActiveClientRequestSlots.FindIndex(x => !_requestAllocations.ContainsKey(x));
                    if(slotIdx < 0)
                    {
                        _stalledRequests.Enqueue(reqData);
                    }
                    else
                    {
                        _requestAllocations[ActiveClientRequestSlots[slotIdx]] = reqData;
                        ActiveClientRequestSlots[slotIdx].InitClient(reqData);
                    }
                    _requestIdx++;
                }
            }

            if(_dragging)
            {
                float fatigueDelta = dt * DragDepletionRate;
                _fatigue += fatigueDelta;
                bool exhaustionCheck = _fatigue >= MaxFatigue;
                if(exhaustionCheck)
                {
                    _fatigue = MaxFatigue;
                    _result = Result.LostExhaustion;
                }
                OnFatigueChanged?.Invoke(FatiguePercent);
                if(exhaustionCheck)
                {
                    return;
                }
            }
            else if(_elapsedSinceLastDragFinished >= 0)
            {
                _elapsedSinceLastDragFinished += dt;
                if(_elapsedSinceLastDragFinished > IdleToRestoreTime)
                {
                    _elapsedSinceLastDragFinished = IdleToRestoreTime;
                    _fatigue -= dt * FatigueRestoreRate;
                    if(_fatigue < 0)
                    {
                        _fatigue = 0;
                        _elapsedSinceLastDragFinished = -1.0f;
                    }
                    OnFatigueChanged?.Invoke(FatiguePercent);
                }
            }

            foreach(var resBuilding in ActiveResourceBuildings)
            {
                resBuilding.UpdateGame(dt);
            }
            foreach (var recBuilding in ActiveRecipeBuildings)
            {
                recBuilding.UpdateGame(dt);
            }

            foreach(var slot in ActiveClientRequestSlots)
            {
                slot.UpdateLogic(dt);
            }
        }
    }

    void CheckStartFatigue()
    {
        if(CurrentLevel.IgnoreFatigueThresholds)
        {
            StartFatigue = 0;
            return;
        }
        float percent = FatiguePercent;
        int thresholdIdx = 0;
        while(thresholdIdx < FatigueRestoreThresholds.Count && percent > FatigueRestoreThresholds[thresholdIdx].Value)
        {
            thresholdIdx++;
        }

        float restorationPercent = FatigueRestoreThresholds[thresholdIdx].FatigueRecoveryPercent;
        float restoreAmount = _fatigue * restorationPercent * 0.01f;
        StartFatigue = _fatigue - restoreAmount;
    }

    private RequestData GenerateRequest()
    {
        List<ItemID> items = new List<ItemID>();
        items.Add(CurrentLevel.ClientRequestsItemPoolItem1[URandom.Range(0, CurrentLevel.ClientRequestsItemPoolItem1.Count)]);
        if(URandom.value < CurrentLevel.SpawnChanceSecondItem)
        {
            var item = CurrentLevel.ClientRequestsItemPoolItem2[URandom.Range(0, CurrentLevel.ClientRequestsItemPoolItem2.Count)];
            if(!items.Contains(item))
            {
                items.Add(item);
            }
        }
        RequestData reqData = new RequestData()
        {
            Timeout = URandom.Range(CurrentLevel.PatienceMin, CurrentLevel.PatienceMax),
            ClientPrefab = CurrentLevel.ClientViews[URandom.Range(0, CurrentLevel.ClientViews.Count)],
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
        _dragging = true;
        _elapsedSinceLastDragFinished = -1;

        slotResourceBuilding.DoDragStart(pos);
    }

    public void DragItemEnd(IItemGenerator slotResourceBuilding, Item itemData, Vector2 pos)
    {
        _dragging = false;
        _elapsedSinceLastDragFinished = 0;

        slotResourceBuilding.DoDragEnd(pos);
        // Valid  resource generator?
        Recipe recipeData;
        bool clear = true;
        SlotRecipesBuilding buildingSlot = FindRecipeBuildingSlotForItemAtPosition(pos, itemData, out recipeData);
        if(buildingSlot != null)
        {
            if(buildingSlot.MissingRecipe)
            {
                clear = buildingSlot.SetActiveRecipe(recipeData, itemData);
            }
            else
            {
                buildingSlot.AddRequirement(itemData);
            }

            if(clear)
            {
                slotResourceBuilding.ClearContents();
            }
            else
            {
                slotResourceBuilding.ResetSlot();
            }
            return;           
        }

        foreach(var slot in ActiveClientRequestSlots)
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
                slotResourceBuilding.ClearContents();
            }
        }

        foreach(var wasteland in ActiveDisposalFacilities)
        {
            if(wasteland.OverlapsPoint(pos))
            {
                slotResourceBuilding.ClearContents();
                return;
            }
        }

        // Otherwise: 
        slotResourceBuilding.ResetSlot();

    }

    private SlotRecipesBuilding FindRecipeBuildingSlotForItemAtPosition(Vector2 pos, Item itemData, out Recipe recipeData)
    {
        recipeData = null;
        foreach(var building in ActiveRecipeBuildings)
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
            revenue += itemData.BaseRevenue;
        }

        int idx = 0;
        while(idx < EarningModifiers.Count && timeLeftPercent > EarningModifiers[idx].TimeLeftThresholdValue)
        {
            idx++;
        }
        float multiplier = EarningModifiers[idx].ExtraPercent;
        float modifiedRevenue = revenue * multiplier;

        _revenue += Mathf.FloorToInt(modifiedRevenue);
        OnEarningsChanged?.Invoke(_revenue);
        _requestAllocations.Remove(clientSlot);

        if(_stalledRequests.Count > 0)
        {
            data = _stalledRequests.Dequeue();
            clientSlot.InitClient(data);
        }
    }

    internal void ClientRequestFailed(ClientSlot clientSlot)
    {
        var reqData = _requestAllocations[clientSlot];
        int revenue = 0;
        foreach(var itemID in reqData.Items)
        {
            Item item = GetItem(itemID);
            revenue += item.BaseRevenue;
        }
        _revenue -= revenue;
        if(_revenue < 0)
        {
            _revenue = 0;
        }

        _numFailures++;
        _requestAllocations.Remove(clientSlot);
        if (_stalledRequests.Count > 0)
        {
            var data = _stalledRequests.Dequeue();
            clientSlot.InitClient(data);
        }

    }

    internal Recipe GetRecipe(RecipeID recipeID)
    {
        return RecipeDataList.Find(x => x.RecipeID == recipeID);
    }
}
