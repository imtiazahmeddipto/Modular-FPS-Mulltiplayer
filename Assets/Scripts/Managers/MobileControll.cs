using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MobileControll : MonoBehaviour
{
    public FixedJoystick moveJoystick;    // Movement joystick
    public FixedJoystick lookJoystick;    // Look joystick (new)
    public float lookJoystickSensitivity;
    public FixedButton fireButton;
    public FixedButton JumpButton;
    public FixedButton scopeButton;
    public FixedButton reloadButton;
    public FixedButton nextWeaponButton;
    public FixedButton prevWeaponButton;
    public FixedTouchField TouchField;    // Existing swipe-based look

    private bool isUsingLookJoystick = false;

    void Update()
    {
        var controller = GetComponent<PlayerMovement>();

        // Handle movement
        controller.runAxis = moveJoystick.Direction;
        controller.JuppAxis = JumpButton.Pressed;

        // Determine if look joystick is active
        isUsingLookJoystick = lookJoystick.Direction.magnitude > 0f;
        
        // Handle looking (prioritize joystick if used, otherwise use FixedTouchField)
        if (isUsingLookJoystick)
        {
            controller.playerLook.lookAxis = lookJoystick.Direction * lookJoystickSensitivity; // Scale for sensitivity
            
        }
        else
        {
            controller.playerLook.lookAxis = TouchField.TouchDist;
        }

        controller.NextWeaponButton = nextWeaponButton.Pressed;
        controller.PrevWeaponButton = prevWeaponButton.Pressed;
        controller.ScopeButton = scopeButton.Pressed;
        controller.ReloadButton = reloadButton.Pressed;

        controller.FireButton = fireButton.Pressed || lookJoystick.IsPressed;
    }
}
