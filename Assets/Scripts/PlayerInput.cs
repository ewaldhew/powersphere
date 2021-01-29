using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Tooltip("Sensitivity multiplier for moving the camera around")]
    public float lookSensitivity = 1f;
    [Tooltip("Additional sensitivity multiplier for WebGL")]
    public float webglLookSensitivityMultiplier = 0.25f;
    [Tooltip("Limit to consider an input when using a trigger on a controller")]
    public float triggerAxisThreshold = 0.4f;
    [Tooltip("Used to flip the vertical input axis")]
    public bool invertYAxis = false;
    [Tooltip("Used to flip the horizontal input axis")]
    public bool invertXAxis = false;

    bool m_FireInputWasHeld;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        m_FireInputWasHeld = GetFireInputHeld();
    }

    public bool IsInputDisabled()
    {
        return Cursor.lockState != CursorLockMode.Locked;
    }

    public Vector3 GetMoveInput()
    {
        if (IsInputDisabled()) {
            return Vector3.zero;
        }

        Vector3 move = new Vector3(Input.GetAxisRaw(AppConstants.k_AxisNameHorizontal), 0f, Input.GetAxisRaw(AppConstants.k_AxisNameVertical));
        move = Vector3.ClampMagnitude(move, 1f);

        return move;
    }

    public float GetLookInputsHorizontal()
    {
        return GetMouseOrStickLookAxis(AppConstants.k_MouseAxisNameHorizontal, AppConstants.k_AxisNameJoystickLookHorizontal);
    }

    public float GetLookInputsVertical()
    {
        return GetMouseOrStickLookAxis(AppConstants.k_MouseAxisNameVertical, AppConstants.k_AxisNameJoystickLookVertical);
    }

    public bool GetJumpInputDown()
    {
        if (IsInputDisabled()) {
            return false;
        }

        return Input.GetButtonDown(AppConstants.k_ButtonNameJump);
    }

    public bool GetJumpInputHeld()
    {
        if (IsInputDisabled()) {
            return false;
        }

        return Input.GetButton(AppConstants.k_ButtonNameJump);
    }

    public bool GetFireInputDown()
    {
        return GetFireInputHeld() && !m_FireInputWasHeld;
    }

    public bool GetFireInputReleased()
    {
        return !GetFireInputHeld() && m_FireInputWasHeld;
    }

    public bool GetFireInputHeld()
    {
        if (IsInputDisabled()) {
            return false;
        }

        bool isGamepad = Input.GetAxis(AppConstants.k_ButtonNameGamepadFire) != 0f;
        if (isGamepad) {
            return Input.GetAxis(AppConstants.k_ButtonNameGamepadFire) >= triggerAxisThreshold;
        } else {
            return Input.GetButton(AppConstants.k_ButtonNameFire);
        }
    }

    public bool GetAimInputHeld()
    {
        if (IsInputDisabled()) {
            return false;
        }

        bool isGamepad = Input.GetAxis(AppConstants.k_ButtonNameGamepadAim) != 0f;
        bool i = isGamepad ? (Input.GetAxis(AppConstants.k_ButtonNameGamepadAim) > 0f) : Input.GetButton(AppConstants.k_ButtonNameAim);
        return i;
    }

    float GetMouseOrStickLookAxis(string mouseInputName, string stickInputName)
    {
        if (IsInputDisabled()) {
            return 0f;
        }

        // Check if this look input is coming from the mouse
        bool isGamepad = Input.GetAxis(stickInputName) != 0f;
        float i = isGamepad ? Input.GetAxis(stickInputName) : Input.GetAxisRaw(mouseInputName);

        // handle inverting vertical input
        if (invertYAxis)
            i *= -1f;

        // apply sensitivity multiplier
        i *= lookSensitivity;

        if (isGamepad) {
            // since mouse input is already deltaTime-dependant, only scale input with frame time if it's coming from sticks
            i *= Time.deltaTime;
        } else {
            // reduce mouse input amount to be equivalent to stick movement
            i *= 0.01f;
#if UNITY_WEBGL
            // Mouse tends to be even more sensitive in WebGL due to mouse acceleration, so reduce it even more
            i *= webglLookSensitivityMultiplier;
#endif
        }

        return i;
    }
}
