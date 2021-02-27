using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PowerSphereState
{
    public Vector3 position;
    public float radius;
    public bool isHeld;
    public bool isInGoal;
}

[System.Serializable]
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
    public int waterSphereIndex;

    public GameObject[] objects;
    public ObjectState[] objectStates;
    public GameObject[] heldObjects = new GameObject[2];

    [Header("Wind settings")]
    [Min(0), Tooltip("How much to scale the wind texture. " +
        "Higher values causes wind direction to vary at greater frequency.")]
    public float windScale = 0.02f;
    [Min(0), Tooltip("How fast the wind texture scrolls. " +
        "Higher values creates the impression of more violent wind.")]
    public float windShiftSpeed = 0.02f;
    [Min(0), Tooltip("How strong the wind is. " +
        "Higher values increases the amount by which objects are affected by wind.")]
    public float windStrength = 1f;
    [Min(0), Tooltip("How strong the dynamic wind is. " +
        "Higher values increases the amount by which objects are affected by wind" +
        "from player interaction.")]
    public float dynamicWindStrength = 20f;
    [Min(0), Tooltip("How large the radius of player wind influence is.")]
    public float dynamicWindRadius = 1f;

    [Header("Default state values")]
    public float defaultSphereInfluenceRadius = 5f;
    public float passiveBoundaryGlowRadius = 3f;

    [Header("Debug")]
    public bool debugOverrideAllGoals = false;

    private void OnEnable()
    {
        objectStates = new ObjectState[objects.Length];
        for (int i = 0; i < objects.Length; i++) {
            objectStates[i] = new ObjectState {
                isHeld = false,
                isInGoal = false,
                influenceRadius = defaultSphereInfluenceRadius,
            };
        }
    }

    public int findObjectIndex(GameObject obj)
    {
        return System.Array.IndexOf(objects, obj);
    }

    public PowerSphereState[] HeldSpheres
    {
        get
        {
            var result = new List<PowerSphereState>();
            for (int i = 0; i < objectStates.Length; i++) {
                if (objectStates[i].isHeld) {
                    result.Add(getPowerSphereState(i));
                }
            }
            return result.ToArray();
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
    public PowerSphereState GetWaterSphere()
    {
        return getPowerSphereState(waterSphereIndex);
    }

    private PowerSphereState getPowerSphereState(int index)
    {
        return new PowerSphereState {
            position = objectStates[index].isHeld ? player.transform.position : objects[index].transform.position,
            radius = objectStates[index].influenceRadius,
            isHeld = objectStates[index].isHeld,
            isInGoal = objectStates[index].isInGoal || debugOverrideAllGoals,
        };
    }
}
