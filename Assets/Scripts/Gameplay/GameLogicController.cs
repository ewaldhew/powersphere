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
        int objIndex = gameState.findObjectIndex(m.picked);
        gameState.objectStates[objIndex].isHeld = m.picker != null;
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

    private void Update()
    {
        for (int i = 0; i < gameState.objectStates.Length; i++) {
            ref ObjectState powerSphere = ref gameState.objectStates[i];
            if (powerSphere.isInGoal && powerSphere.influenceRadius > 0) {
                const float targetRadius = 200f;
                float currentRadius = powerSphere.influenceRadius;
                float newRadius = currentRadius * 1.1f;
                if (targetRadius - currentRadius < 10f) {
                    newRadius = -1;
                }
                powerSphere.influenceRadius = newRadius;
            }
        }
    }
}
