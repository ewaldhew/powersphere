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
    [SerializeField]
    GameState gameState;

    // Events
    [SerializeField]
    public static PlayerPickupEvent PlayerPick = new PlayerPickupEvent();

    void PlayerPickup(MessageTypes.Pickup m)
    {
        for (int i = 0; i < gameState.objects.Length; i++) {
            if (gameState.objects[i] == m.picked) {
                gameState.isHeld[i] = m.picker != null;
            }
        }
    }

    private void Start()
    {
    }

    private void OnEnable()
    {
        PlayerPick.AddListener(PlayerPickup);
    }
    private void OnDisable()
    {
        PlayerPick.RemoveListener(PlayerPickup);
    }
}
