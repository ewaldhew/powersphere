using UnityEngine;
using UnityEngine.Events;

using GameEvents;

namespace GameEvents
{
    [System.Serializable]
    public class PlayerPickupEvent : UnityEvent<MessageTypes.Pickup> { }

    [System.Serializable]
    public class PowerSphereGoalEvent : UnityEvent<MessageTypes.Goal> { }
}

public class GameLogicController : MonoBehaviour
{
    [SerializeField]
    GameState gameState;

    // Events
    public static PlayerPickupEvent PlayerPick = new PlayerPickupEvent();
    public static PowerSphereGoalEvent PowerSphereGoal = new PowerSphereGoalEvent();

    void OnPlayerPickup(MessageTypes.Pickup m)
    {
        for (int i = 0; i < gameState.objects.Length; i++) {
            if (gameState.objects[i] == m.picked) {
                gameState.objectStates[i].isHeld = m.picker != null;
            }
        }
    }

    void OnPowerSphereGoal(MessageTypes.Goal m)
    {
        m.powerSphere.SetActive(false);
        m.goal.SetActive(false);

        int objIndex = gameState.findObjectIndex(m.powerSphere);
        gameState.objectStates[objIndex].isHeld = false;
        gameState.objectStates[objIndex].isInGoal = true;
    }

    private void OnEnable()
    {
        PlayerPick.AddListener(OnPlayerPickup);
        PowerSphereGoal.AddListener(OnPowerSphereGoal);
    }
    private void OnDisable()
    {
        PlayerPick.RemoveListener(OnPlayerPickup);
        PowerSphereGoal.RemoveListener(OnPowerSphereGoal);
    }
}
