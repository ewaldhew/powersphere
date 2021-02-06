using UnityEngine;

public struct PowerSphereState
{
    public Vector3 position;
    public bool isHeld;
}

public class GameState : MonoBehaviour
{
    public CharacterController player;

    // Interactive objects tracking
    public GameObject[] objects;
    public bool[] isHeld;

    public float windScale = 0.02f;
    public float windShiftSpeed = 0.02f;

    private void Awake()
    {
        isHeld = new bool[objects.Length];
    }

    public PowerSphereState colorSphere
    {
        get
        {
            return new PowerSphereState {
                position = objects[0].transform.position,
                isHeld = isHeld[0],
            };
        }
    }

}
