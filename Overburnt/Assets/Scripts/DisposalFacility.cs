using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisposalFacility : MonoBehaviour
{
    GameController _gameController;
    public Collider2D CollisionArea;

    public void Init(GameController gameController)
    {
        _gameController = gameController;
    }

    public void Load()
    {
        gameObject.SetActive(true);
    }

    public void UnLoad()
    {
        gameObject.SetActive(false);
    }

    public bool OverlapsPoint(Vector2 worldPos)
    {
        return CollisionArea.OverlapPoint(worldPos);
    }
}
