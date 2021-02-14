using UnityEngine;
using UnityEngine.Events;

using GameEvents;

namespace GameEvents
{
    [System.Serializable]
    public class PlayerPickupEvent : UnityEvent<MessageTypes.Pickup> { }

    [System.Serializable]
    public class PickupCompleteEvent : UnityEvent<MessageTypes.PickupComplete> { }

    [System.Serializable]
    public class PowerSphereGoalEvent : UnityEvent<MessageTypes.Goal> { }
}

public class GameLogicController : MonoBehaviour
{
    [SerializeField]
    GameState gameState;

    // Events
    public static PlayerPickupEvent PlayerPick = new PlayerPickupEvent();
    public static PickupCompleteEvent PickupComplete = new PickupCompleteEvent();
    public static PowerSphereGoalEvent PowerSphereGoal = new PowerSphereGoalEvent();

    void OnPlayerPickup(MessageTypes.Pickup m)
    {
        // drop currently held item, if any
        if (gameState.heldObjects[m.slotIndex] != null) {
            gameState.heldObjects[m.slotIndex].SendMessage("OnDrop");
            gameState.heldObjects[m.slotIndex] = null;
            return;
        }

        GameObject target = null;
        foreach (var item in m.candidates) {
            bool isAlreadyHeld = System.Array.Exists(gameState.heldObjects, obj => obj == item);
            if (isAlreadyHeld) {
                continue;
            } else {
                target = item;
                break;
            }
        }
        if (!target) {
            return;
        }

        target.SendMessage("OnPickup", m);
        gameState.heldObjects[m.slotIndex] = target; // the powerspherecontroller (inner)
    }

    void OnPickupComplete(MessageTypes.PickupComplete m) {
        int objIndex = gameState.findObjectIndex(m.picked); // the outer sphere object
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
        PickupComplete.AddListener(OnPickupComplete);
        PowerSphereGoal.AddListener(OnPowerSphereGoal);
    }
    private void OnDisable()
    {
        PlayerPick.RemoveListener(OnPlayerPickup);
        PickupComplete.RemoveListener(OnPickupComplete);
        PowerSphereGoal.RemoveListener(OnPowerSphereGoal);
    }

    private void Update()
    {
        for (int i = 0; i < gameState.objectStates.Length; i++) {
            ref ObjectState powerSphere = ref gameState.objectStates[i];
            bool isInGoal = powerSphere.isInGoal || gameState.debugOverrideAllGoals;
            if (isInGoal && powerSphere.influenceRadius > 0) {
                const float targetRadius = 200f;
                float currentRadius = powerSphere.influenceRadius;
                float newRadius = currentRadius * 1.1f;
                if (targetRadius - currentRadius < 10f) {
                    newRadius = -1;
                }
                powerSphere.influenceRadius = newRadius;
            } else if (!isInGoal && powerSphere.influenceRadius < 0) {
                powerSphere.influenceRadius = gameState.defaultSphereInfluenceRadius;
            }
        }
    }
}
