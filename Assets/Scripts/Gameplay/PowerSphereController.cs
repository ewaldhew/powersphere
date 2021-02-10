using UnityEngine;

public class PowerSphereController : MonoBehaviour
{
    [SerializeField]
    GameObject rootObject;
    [SerializeField]
    Collider playerCollider;
    [SerializeField]
    Collider selfCollider;

    [SerializeField]
    string droppedLayerName = "Default";

    private Rigidbody rootBody;
    private bool wasDropped;
    private int droppedLayer;
    private int previousLayer = 0;

    // Start is called before the first frame update
    void Start()
    {
        rootObject.TryGetComponent(out rootBody);
        droppedLayer = LayerMask.NameToLayer(droppedLayerName);
        if (droppedLayer == -1) {
            droppedLayer = LayerMask.NameToLayer("Default");
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != playerCollider || rootBody.isKinematic || wasDropped) {
            return;
        }

        float distance;
        Vector3 direction;
        Physics.ComputePenetration(
            playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
            selfCollider, transform.position, transform.rotation, out direction, out distance);

        selfCollider.attachedRigidbody.AddForce(distance * -direction, ForceMode.Impulse);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != playerCollider || !wasDropped) {
            return;
        }

        wasDropped = false;
        rootObject.layer = previousLayer;
    }

    public void OnPickup(MessageTypes.Pickup m)
    {
        Vector3[] holdOffsets = { Vector3.left, Vector3.right };
        rootObject.transform.SetParent(m.picker.transform);
        rootObject.transform.localPosition = holdOffsets[m.slotIndex];
        rootBody.isKinematic = true;

        rootObject.SendMessage("OnPickup", SendMessageOptions.DontRequireReceiver);

        m.picked = rootObject;
        GameLogicController.PlayerPick.Invoke(m);
    }

    public void OnDrop()
    {
        wasDropped = true;
        previousLayer = rootObject.layer;

        // NOTE: needed so that TriggerExit doesn't happen immediately
        selfCollider.enabled = false;

        Vector3 rollVec = rootObject.transform.parent.forward;
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.SetParent(null);
        rootObject.layer = droppedLayer;
        rootBody.isKinematic = false;
        rootBody.AddForce(rollVec * playerCollider.bounds.size.sqrMagnitude, ForceMode.Impulse);

        selfCollider.enabled = true;

        rootObject.SendMessage("OnDrop", SendMessageOptions.DontRequireReceiver);

        GameLogicController.PlayerPick.Invoke(new MessageTypes.Pickup {
            picker = null,
            picked = rootObject,
            slotIndex = -1,
        });
    }
}
