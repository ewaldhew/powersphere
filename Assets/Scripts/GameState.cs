using UnityEngine;

public class GameState : MonoBehaviour
{
    // Game State
    public CharacterController player;
    public GameObject[] objects;
    public bool[] isHeld;

    private void Awake()
    {
        isHeld = new bool[objects.Length];
    }
}
