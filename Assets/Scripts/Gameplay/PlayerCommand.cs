using UnityEngine;

[System.Serializable]
public class PlayerCommand
{
    public float ExpireTime;
    public float ActivatedTime { get; private set; }

    private bool shouldExecute = false;
    private float counter = 0;

    public PlayerCommand(float expireTimeSeconds)
    {
        ExpireTime = expireTimeSeconds;
    }

    public void Update(bool shouldActivate)
    {
        if (shouldExecute) {
            // WAIT
            counter += Time.deltaTime; // -> CLEAR_CHECK
        }
        if (counter > ExpireTime) {
            // CLEAR_CHECK
            shouldExecute = false; // -> CLEAR
            counter = 0;
        }
        if (shouldActivate) {
            // WAIT_START
            shouldExecute = true; // -> WAIT
            counter = 0;
        }
    }

    public bool Activate()
    {
        if (!shouldExecute) {
            return false;
        }

        shouldExecute = false;
        ActivatedTime = Time.time;
        counter = 0;

        return true;
    }
}
