using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSlot : MonoBehaviour
{
    public Collider2D CollisionArea;
    public SpriteRenderer Item1;
    public SpriteRenderer Item2;
    public SpriteMask ProgressBar;
    public GameObject RequestRoot;

    public GameObject ClientRoot;
    GameObject _clientView;

    float _elapsed;
    float _baseTimeout;
    bool _ready;

    GameController _gameController;
    List<ItemID> _requiredItems;
    RequestData _requestData;

    public bool Active => gameObject.activeInHierarchy;

    public float TimeElapsed => _elapsed;

    public void Init(GameController gameController)
    {
        _gameController = gameController;
    }

    public void InitClient(RequestData requestData)
    {
        Clear();
        _requestData = requestData;
        _clientView = Instantiate(requestData.ClientPrefab, ClientRoot.transform);
        gameObject.SetActive(true);
        _requiredItems = new List<ItemID>(_requestData.Items);
        _elapsed = 0;
        _baseTimeout = requestData.Timeout;
        _ready = false;
        UpdateIcons();        
    }

    public void UpdateIcons()
    {
        bool firstItem = _requiredItems.Count > 0; ;
        Item1.enabled = firstItem;
        if (firstItem)
            Item1.sprite = _gameController.GetItem(_requiredItems[0])?.Icon;
        bool secondItem = _requiredItems.Count > 1;
        Item2.enabled = secondItem;
        if (secondItem)
            Item2.sprite = _gameController.GetItem(_requiredItems[1])?.Icon;
    }

    public void Clear()
    {
        gameObject.SetActive(false);
        Destroy(_clientView);
        _requestData = null;
    }


    // Update is called once per frame
    public void UpdateLogic(float dt)
    {
        if(!_ready && Active)
        {
            _elapsed += dt;
            if(_elapsed >= _baseTimeout)
            {
                _gameController.ClientRequestFailed(this);
                Clear();
            }
            float ratio = (_elapsed / _baseTimeout);
            ProgressBar.alphaCutoff = ratio;
            // TODO: Threshold checks (bar colour updates, animations, etc)
        }
    }

    public bool GiveItem(ItemID item)
    {
        if(_requiredItems.Contains(item))
        {
            _requiredItems.Remove(item);
            var itemData = _gameController.GetItem(item);
            bool firstItem = _requiredItems.Count > 0; ;
            Item1.enabled = firstItem;
            if (firstItem)
                Item1.sprite = _gameController.GetItem(_requiredItems[0])?.Icon;
            bool secondItem = _requiredItems.Count > 1;
            Item2.enabled = secondItem;
            if (secondItem)
                Item2.sprite = _gameController.GetItem(_requiredItems[1])?.Icon;
            if (_requiredItems.Count == 0)
            {
                _gameController.RequestFulfilled(this);
                // animate exit
                Clear();
            }
            else if (itemData.ClientWaitTimePercentRestored > 0)
            {
                float AmountRestored = itemData.ClientWaitTimePercentRestored * 0.01f * _baseTimeout;
                _elapsed -= AmountRestored;
                if (_elapsed < 0)
                {
                    _elapsed = 0;
                }
                float ratio = (_elapsed / _baseTimeout);
                ProgressBar.alphaCutoff = ratio;
            }
            return true;
        }
        return false;
    }

    internal bool ContainsPosition(Vector2 pos)
    {
        return CollisionArea.OverlapPoint(pos);
    }

    internal bool IsRequestedItem(Item itemData)
    {
        return _requiredItems.Contains(itemData.ItemID);
    }
}
