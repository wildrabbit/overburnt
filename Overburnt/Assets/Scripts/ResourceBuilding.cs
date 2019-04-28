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

    public void GiveItem(Item item)
    {
        
    }

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
}
