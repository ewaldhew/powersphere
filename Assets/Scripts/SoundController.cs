using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundController : MonoBehaviour
{
    [SerializeField]
    GameState gameState;

    [SerializeField]
    AudioSource windAudioSource;
    private float windMasterVolume;

    [SerializeField]
    AudioSource leavesAudioSource;
    [SerializeField]
    AudioClip[] leavesSoundPalette;

    private void OnEnable()
    {
        windMasterVolume = windAudioSource.volume;
    }

    private void Update()
    {
        // wind
        {
            var windSphere = gameState.GetWindSphere();
            float volumeFactor = 1.0f;
            if (windSphere.radius > 0) {
                float distance = (windSphere.position - transform.position).magnitude;
                volumeFactor = 1 - (distance / windSphere.radius);
            }

            volumeFactor *= Mathf.Max(0.1f, gameState.windShiftSpeed);

            windAudioSource.volume = windMasterVolume * volumeFactor;
        }

        // leaves
        {
            if (gameState.player.velocity.magnitude > 0 && Random.value < 0.01f) {
                int index = Mathf.FloorToInt(Random.value * leavesSoundPalette.Length - Mathf.Epsilon);
                leavesAudioSource.PlayOneShot(leavesSoundPalette[index], 0.1f);
            }
        }
    }
}
