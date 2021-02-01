using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using GameEvents;

namespace GameEvents
{
    [System.Serializable]
    public class PlayerPickupEvent : UnityEvent<MessageTypes.Pickup> { }
}

public class GameLogicController : MonoBehaviour
{
    // Game State
    [SerializeField]
    GameObject[] objects;
    [SerializeField]
    bool[] isHeld;

    // Events
    [SerializeField]
    public static PlayerPickupEvent PlayerPick = new PlayerPickupEvent();

    void PlayerPickup(MessageTypes.Pickup m)
    {
        for (int i = 0; i < objects.Length; i++) {
            if (objects[i] == m.picked) {
                isHeld[i] = m.picker != null;
            }
        }
    }

    private void Start()
    {
        isHeld = new bool[objects.Length];
    }

    private void OnEnable()
    {
        PlayerPick.AddListener(PlayerPickup);
    }
    private void OnDisable()
    {
        PlayerPick.RemoveListener(PlayerPickup);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
