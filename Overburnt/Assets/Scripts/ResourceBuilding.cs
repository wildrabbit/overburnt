using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum SlotStatus
{
    PickupPending,
    Generating,
    AwaitingResources
}


public class ResourceBuilding : MonoBehaviour
{
    public GameController GameController;
  
    public List<SlotResourceBuilding> Slots;
    List<SlotResourceBuilding> AvailableSlots;

    public void Init(GameController gameController)
    {
        GameController = gameController;
        foreach(var s in Slots)
        {
            s.Init(this);
        }
        AvailableSlots = new List<SlotResourceBuilding>();
    }

    public void LoadBuilding(ResourceBuildingInfo info)
    {
        gameObject.SetActive(true);
        AvailableSlots.Clear();
        foreach(var s in Slots)
        {
            if(info.Slots.Contains(s))
            {
                s.Load();
                AvailableSlots.Add(s);
            }
            else
            {
                s.Unload();
            }
        }
    }

    public void Unload()
    {
        gameObject.SetActive(false);
        foreach(var slot in AvailableSlots)
        {
            slot.Unload();
        }
        AvailableSlots.Clear();

    }

    // Update is called once per frame
    public void UpdateGame(float dt)
    {
        foreach (var slot in AvailableSlots)
        {
            slot.UpdateSlot(dt);
        }
    }
}
