using UnityEngine;

public class GameState : MonoBehaviour
{
    // Game State
    public CharacterController player;
    public GameObject[] objects;
    public bool[] isHeld;

    public float windScale = 0.02f;
    public float windShiftSpeed = 0.02f;

    private void Awake()
    {
        isHeld = new bool[objects.Length];
    }
}
