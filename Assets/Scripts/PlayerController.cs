using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    GameObject playerCamera;
    [SerializeField]
    PlayerInput playerInput;
    [SerializeField]
    CharacterController baseController;

    [Header("Movement")]
    [SerializeField, Range(0f, 50f),
        Tooltip("Base movement speed")]
    float moveSpeed = 10f;

    [SerializeField, Range(0f, 10f),
        Tooltip("Scale of acceleration in air")]
    float airControlFactor = 0.1f;

    [SerializeField, Range(0f, 10f)]
    float jumpSpeed = 10f;
    [SerializeField]
    float gravityAcceleration = 30f;

    [SerializeField, Range(0f, 100f),
        Tooltip("Maximum acceleration. Higher values gives more responsive and jerky movement")]
    float maxAcceleration = 40f;

    [SerializeField, Range(0f, 2000f),
        Tooltip("Rotation speed for moving the camera")]
    float rotationSpeed = 200f;

    [SerializeField,
        Tooltip("Ground check physics layer mask")]
    LayerMask groundCheckLayerMask;

    [SerializeField,
        Tooltip("Pickup-able items layer mask")]
    LayerMask pickupLayerMask;

    private Vector3 ColliderBottom
    {
        get
        {
            return transform.position - (transform.up * baseController.radius);
        }
    }
    private Vector3 ColliderTop
    {
        get
        {
            return transform.position + (transform.up * (baseController.height - baseController.radius));
        }
    }

    private Vector3 velocity;

    private PlayerCommand jump;
    private bool isOnGround;
    private Vector3 groundNormal;

    private const float GROUND_CHECK_DISTANCE = 0.05f;
    private const float MIN_JUMP_TIME = 0.5f; // NOTE: arbitrary, can be found by solving a quadratic though
    private const float JUMP_WINDOW = 0.2f; // seconds

    private PlayerCommand pickup1;
    private PlayerCommand pickup2;
    private GameObject[] heldObjects = new GameObject[2];

    // Start is called before the first frame update
    void Start()
    {
        jump = new PlayerCommand(JUMP_WINDOW);
        pickup1 = new PlayerCommand(1f);
        pickup2 = new PlayerCommand(1f);
    }

    // Update is called once per frame
    void Update()
    {
        // update ground state
        {
            isOnGround = false;
            groundNormal = Vector3.up;

            float maxGroundingDistance = baseController.skinWidth + GROUND_CHECK_DISTANCE;

            if (Time.time >= jump.ActivatedTime + MIN_JUMP_TIME &&
                Physics.CapsuleCast(ColliderBottom, ColliderTop, baseController.radius, Vector3.down, out RaycastHit hit, maxGroundingDistance, groundCheckLayerMask, QueryTriggerInteraction.Ignore)) {
                // for use in projecting move direction
                groundNormal = hit.normal;

                bool isGroundFacingUp = Vector3.Dot(hit.normal, transform.up) > 0f;
                bool isSlopeBelowLimit = Vector3.Angle(hit.normal, transform.up) <= baseController.slopeLimit;
                isOnGround = isGroundFacingUp && isSlopeBelowLimit;

                // snap to ground
                if (isOnGround && hit.distance > baseController.skinWidth) {
                    baseController.Move(hit.distance * Vector3.down);
                }
            }

            if (isOnGround) {
                velocity.y = 0;
            }
        }

        // handle input
        {
            // camera control
            {
                float rx = playerInput.GetLookInputsHorizontal() * rotationSpeed;
                float ry = playerInput.GetLookInputsVertical() * rotationSpeed;
                float ryClamped = Mathf.Clamp(playerCamera.transform.localEulerAngles.x - ry, 20, 60);

                transform.Rotate(new Vector3(0f, rx, 0f), Space.Self);
                playerCamera.transform.localEulerAngles = new Vector3(ryClamped, 0f, 0f);
            }

            // movement
            {
                Vector3 inputVelocity = transform.TransformVector(playerInput.GetMoveInput()) * moveSpeed;
                Vector3 tangent = Vector3.Cross(inputVelocity.normalized, transform.up);
                Vector3 targetVelocity = Vector3.Cross(groundNormal, tangent).normalized * inputVelocity.magnitude;

                float acceleration = isOnGround ? maxAcceleration : maxAcceleration * airControlFactor;
                float maxSpeedChange = acceleration * Time.deltaTime;
                float vx = Mathf.MoveTowards(velocity.x, targetVelocity.x, maxSpeedChange);
                float vy = Mathf.MoveTowards(velocity.y, targetVelocity.y, maxSpeedChange);
                float vz = Mathf.MoveTowards(velocity.z, targetVelocity.z, maxSpeedChange);
                velocity = new Vector3(vx, isOnGround ? vy : velocity.y, vz);

                // add jump
                jump.Update(playerInput.GetJumpInputDown());
                if (isOnGround && jump.Activate()) {
                    velocity.y = jumpSpeed;

                    isOnGround = false;
                    groundNormal = Vector3.up;
                }

                // apply gravity
                if (!isOnGround) {
                    velocity += Vector3.down * gravityAcceleration * Time.deltaTime;
                }

                // collide with other geometry
                Vector3 postVelocity = velocity;
                if (Physics.CapsuleCast(ColliderBottom, ColliderTop, baseController.radius, velocity.normalized, out RaycastHit hit, velocity.magnitude * Time.deltaTime, 0, QueryTriggerInteraction.Ignore)) {
                    postVelocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                }

                baseController.Move(velocity * Time.deltaTime);
                velocity = postVelocity;
            }

            // pickup
            {
                pickup1.Update(playerInput.GetFireInputDown());
                if (pickup1.Activate()) {
                    Pickup(0);
                }
                pickup2.Update(playerInput.GetFire2InputDown());
                if (pickup2.Activate()) {
                    Pickup(1);
                }
            }
        }
    }

    private void Pickup(int slotIndex)
    {
        // drop currently held item, if any
        if (heldObjects[slotIndex] != null) {
            heldObjects[slotIndex].SendMessage("OnDrop");
            heldObjects[slotIndex] = null;
            return;
        }

        Collider[] items = Physics.OverlapSphere(transform.position, baseController.radius * 2f, pickupLayerMask);
        if (items.Length <= 0) {
            return;
        }

        GameObject target = null;
        foreach (var item in items) {
            bool isAlreadyHeld = System.Array.Exists(heldObjects, obj => obj == item.gameObject);
            if (isAlreadyHeld) {
                continue;
            } else {
                target = item.gameObject;
                break;
            }
        }
        if (!target) {
            return;
        }

        var pickupMessage = new MessageTypes.Pickup {
            picker = gameObject,
            picked = null,
            slotIndex = slotIndex,
        };

        target.SendMessage("OnPickup", pickupMessage);
        heldObjects[slotIndex] = target;
    }
}
