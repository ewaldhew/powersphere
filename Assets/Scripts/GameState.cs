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
    public int colorSphereIndex;
    public int windSphereIndex;
    public int greenSphereIndex;

    public GameObject[] objects;
    public ObjectState[] objectStates;

    public float windScale = 0.02f;
    public float windShiftSpeed = 0.02f;

    private void OnEnable()
    {
        objectStates = new ObjectState[objects.Length];
        for (int i = 0; i < objects.Length; i++) {
            objectStates[i] = new ObjectState {
                isHeld = false,
                isInGoal = false,
                influenceRadius = 5f,
            };
        }
    }

    public int findObjectIndex(GameObject obj)
    {
        return System.Array.IndexOf(objects, obj);
    }

    public PowerSphereState? HeldSphere
    {
        get
        {
            int index = System.Array.FindIndex(objectStates, (state) => { return state.isHeld; });
            if (index != -1) {
                return getPowerSphereState(index);
            } else {
                return null;
            }
        }
    }

    public PowerSphereState GetColorSphere()
    {
        return getPowerSphereState(colorSphereIndex);
    }
    public PowerSphereState GetWindSphere()
    {
        return getPowerSphereState(windSphereIndex);
    }
    public PowerSphereState GetGreenSphere()
    {
        return getPowerSphereState(greenSphereIndex);
    }

    private PowerSphereState getPowerSphereState(int index)
    {
        return new PowerSphereState {
            position = objectStates[index].isHeld ? player.transform.position : objects[index].transform.position,
            radius = objectStates[index].influenceRadius,
            isHeld = objectStates[index].isHeld,
            isInGoal = objectStates[index].isInGoal,
        };
    }
}
