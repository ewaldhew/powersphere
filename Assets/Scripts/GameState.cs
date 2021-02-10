using UnityEngine;

public struct PowerSphereState
{
    public Vector3 position;
    public float radius;
    public bool isHeld;
    public bool isInGoal;
}
public struct ObjectState
{
    public bool isHeld;
    public bool isInGoal;
    public float influenceRadius;
}

public class GameState : MonoBehaviour
{
    public CharacterController player;

    // Interactive objects tracking
    public GameObject[] objects;
    public ObjectState[] objectStates;

    private static class TrackedObjects {
        public const int ColorSphere = 0;
    }

    public float windScale = 0.02f;
    public float windShiftSpeed = 0.02f;

    private void Awake()
    {
        objectStates = new ObjectState[objects.Length];

        objectStates[TrackedObjects.ColorSphere].influenceRadius = 5f;
    }

    public PowerSphereState colorSphere
    {
        get
        {
            int i = TrackedObjects.ColorSphere;
            return new PowerSphereState {
                position = objects[i].transform.position,
                radius = objectStates[i].influenceRadius,
                isHeld = objectStates[i].isHeld,
                isInGoal = objectStates[i].isInGoal,
            };
        }
    }

    public int findObjectIndex(GameObject obj)
    {
        return System.Array.IndexOf(objects, obj);
    }

    private void Update()
    {
        for (int i = 0; i < objectStates.Length; i++) {
            ref ObjectState powerSphere = ref objectStates[i];
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
