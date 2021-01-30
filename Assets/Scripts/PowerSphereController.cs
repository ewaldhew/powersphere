using UnityEngine;

public class PowerSphereController : MonoBehaviour
{
    [SerializeField]
    Collider playerCollider;
    [SerializeField]
    Collider selfCollider;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != playerCollider) {
            return;
        }

        float distance;
        Vector3 direction;
        Physics.ComputePenetration(
            playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
            selfCollider, transform.position, transform.rotation, out direction, out distance);

        selfCollider.attachedRigidbody.AddForce(distance * -direction, ForceMode.Impulse);
    }
}
