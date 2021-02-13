using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Collider), typeof(Rigidbody))]
public class GoalTriggerReactor : MonoBehaviour
{
    [SerializeField, Tooltip("Trigger when this object touches this")]
    Collider goalTrigger;
    [SerializeField, Tooltip("Set trigger on the animation")]
    string setTriggerName;
    [SerializeField, Tooltip("Activate these particle effects")]
    ParticleSystem[] effects;

    private Rigidbody targetBody;
    private Animator targetAnimator;
    private bool animationStarted = false;
    private bool isHeld = false;

    void Start()
    {
        TryGetComponent(out targetBody);
        TryGetComponent(out targetAnimator);
    }

    private void OnPickup(MessageTypes.PickupComplete m)
    {
        if (animationStarted) {
            return;
        }

        isHeld = true;
        m.picked.SendMessage("OnPickupConfirmed", m);
    }
    private void OnDrop()
    {
        isHeld = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != goalTrigger || isHeld) {
            return;
        }

        // Prepare for animation - disable collision and reset outer rotation
        animationStarted = true;
        targetBody.isKinematic = true;
        gameObject.transform.position = goalTrigger.transform.position;
        gameObject.transform.rotation = Quaternion.identity;

        // Dispatch animation
        targetAnimator.SetTrigger(setTriggerName);

        foreach (var effect in effects) {
            var emissionModule = effect.emission;
            emissionModule.enabled = true;
        }
    }

    private void OnTriggerAnimationEnd()
    {
        foreach (var effect in effects) {
            var emissionModule = effect.emission;
            emissionModule.enabled = false;
        }

        GameLogicController.PowerSphereGoal.Invoke(new MessageTypes.Goal {
            powerSphere = this.gameObject,
            goal = goalTrigger.gameObject,
        });
    }
}
