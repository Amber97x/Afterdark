// Using directives
using UnityEngine;
using UnityEngine.InputSystem;

// Camera controller for building and editing maps
// Finds an anchor point (Ground intersect) that the camera can move, rotate and zoom around
// Supports keyboard and Gamepad
public class OrbitCameraController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference look; // Vector2 (mouse delta / right stick)
    [SerializeField] private InputActionReference lookGate; // Button (Right mouse)
    [SerializeField] private InputActionReference move; // Vector2 (WASD / Arrows / left stick)
    [SerializeField] private InputActionReference zoom; // Axis (scroll / triggers)

    [Header("Ground / Angles / Limits")]
    [SerializeField] private float groundHeight = 0.0f; // Focus height (Change when working on 2nd story etc)
    [SerializeField] private float minPitch = 15.0f; // Minimum pitch angle in degrees
    [SerializeField] private float maxPitch = 80.0f; // Maximum pitch angle in degrees
    [SerializeField] private float minDistance = 5.0f; // Minimum distance away from anchor the parent gameobject is allowed
    [SerializeField] private float maxDistance = 50.0f; // Maximum distance away from anchor the parent gameobject is allowed

    [Header("Speeds")]
    [SerializeField] private float mouseLookSpeed = 45.0f; // Maximum rotation speed with mouse
    [SerializeField] private float gamepadLookSpeed = 90.0f; // Maximum rotation speed with gamepad
    [SerializeField] private float moveSpeed = 10.0f; // Maximum panning speed
    [SerializeField] private float mouseZoomSpeed = 1.5f; // Maximum zooming speed with mouse
    [SerializeField] private float gamepadZoomSpeed = 50.0f; // Maximum zooming speed with gamepad

    // State variables
    private Vector3 anchor; // Ground point we orbit / pan around
    private float yaw; // Yaw rotation in degrees
    private float pitch; // Pitch rotation in degrees
    private float distance; // to anchor along -forward
    private InputDevice lastDevice; // Most recently used input device

    // On script enabled
    private void OnEnable()
    {
        // Initialize
        InitializeInputActionReferences();
        InitializeAngles();
        InitializeAnchor();
        UpdateTransform();

        // Subscribe to input actions for device tracking
        HookDeviceTracker(true);
    }

    // On script disabled
    private void OnDisable()
    {
        // Unsubscribe from input actions for device tracking
        HookDeviceTracker(false);

        // Shutdown
        ShutdownInputActionReferences();
    }

    // Called once per tick
    private void Update()
    {
        // Handle input events (Move, Look, Zoom)
        HandleLook();
        HandleMove();
        HandleZoom();

        // Update transforms position and rotation
        UpdateTransform();
    }

    // Enables relevant input action references
    private void InitializeInputActionReferences()
    {
        // Enable relevant input action references
        look.action.Enable();
        lookGate.action.Enable();
        move.action.Enable();
        zoom.action.Enable();
    }

    // Disables relevant input action references
    private void ShutdownInputActionReferences()
    {
        // Disable relevant input action references
        look.action.Disable();
        lookGate.action.Disable();
        move.action.Disable();
        zoom.action.Disable();
    }

    // Stores current angles from the parent object in yaw and pitch state variables
    // Modifies the current pitch state variable if it exceeds the min / max allowed pitch angle
    private void InitializeAngles()
    {
        //Copy the parent gameobjects yaw and clamped pitch rotations in degrees
        Vector3 eulerAngles = transform.rotation.eulerAngles;
        yaw = eulerAngles.y;
        pitch = Mathf.Clamp(eulerAngles.x, minPitch, maxPitch);
    }

    // Calculates the initial anchor position and distance based on the parent gameobjects current transform
    // Clamps the distance state variable between the min / max allowed distances
    private void InitializeAnchor()
    {
        // Calculate initial ground intnersect from the parent gameobjects current transform
        anchor = GroundIntersect(transform.position, transform.forward);

        // Calculate the initial distance and clamp between min / max allowed values
        float height = transform.position.y - groundHeight;
        float pitchRadians = Mathf.Max(0.001f, pitch * Mathf.Deg2Rad);
        distance = Mathf.Clamp(height / Mathf.Sin(pitchRadians), minDistance, maxDistance);
    }

    // Calculates and returns the position of a ground intersect based on the position and forward attributes
    private Vector3 GroundIntersect(Vector3 position, Vector3 forward)
    {
        // Calculate the distance to the ground from the position travelling in the forward direction
        float forwardDistanceToGround = (groundHeight - position.y) / forward.y;

        // Calculate the ground intersect and clamp Y to the ground height
        Vector3 groundIntersect = position + forward * forwardDistanceToGround;
        groundIntersect.y = groundHeight;

        // Return the ground intersect
        return groundIntersect;
    }

    // Updates the position and rotation of the parent gameobject based on the current state variables
    private void UpdateTransform()
    {
        // Calculate the desired rotation quaternion
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0.0f);

        // Update the position and rotation of the parent gameobject based on the current state variables
        transform.SetPositionAndRotation(anchor - (rotation * Vector3.forward) * distance, rotation);
    }

    // Enabled or disabled input action listening for device tracking depending on the subscribe flag
    private void HookDeviceTracker(bool subscribe)
    {
        // Create array of actions to listen on
        var actions = new[]
        {
            look.action,
            lookGate.action,
            move.action,
            zoom.action
        };

        // For each action in the actions array
        foreach (var action in actions)
        {
            //If we should subscribe to the action for device tracking
            if(subscribe)
            {
                //Subscribe to the action
                action.performed += OnAnyActionPerformed;
            }
            //Otherwise
            else
            {
                //Unsubscribe from the action
                action.performed -= OnAnyActionPerformed;
            }
        }
    }

    // Called when a subscribed input action is performed to determine the most decently used input device
    private void OnAnyActionPerformed(InputAction.CallbackContext callbackContext)
    {
        // Update the last device state
        lastDevice = callbackContext.control?.device;
    }

    // Handles look input and updates relevant state
    private void HandleLook()
    {
        // Read look input
        Vector2 lookInput = look.action.ReadValue<Vector2>();

        // If look input should be processed
        if (lookInput != Vector2.zero)
        {
            // If the input came from a gamepad
            if (lastDevice is Gamepad)
            {
                // Update yaw & pitch using gamepad input and gamepad look speed (Pitch is clamped between min/max pitch)
                yaw += lookInput.x * gamepadLookSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - lookInput.y * gamepadLookSpeed * Time.deltaTime, minPitch, maxPitch);
            }
            // Otherwise check if the right mouse button is down (look gate)
            else if (lookGate.action.IsPressed())
            {
                // Update yaw & pitch using mouse input and mouse look speed (Pitch is clamped between min/max pitch)
                yaw += lookInput.x * mouseLookSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - lookInput.y * mouseLookSpeed * Time.deltaTime, minPitch, maxPitch);
            }

            // Update ground intersect to ensure rotation feels like it's orbiting around the ground intersect
            anchor = GroundIntersect(transform.position, transform.forward);
        }
    }

    // Handles move input and updates relevant state
    private void HandleMove()
    {
        // Read move input
        Vector2 moveInput = move.action.ReadValue<Vector2>();

        // If move input should be processed
        if (moveInput != Vector2.zero)
        {
            // Calculate right and forward vectors relative to the yaw state
            Quaternion yawRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            Vector3 right = yawRotation * Vector3.right;
            Vector3 forward = yawRotation * Vector3.forward;

            // Move anchor along ground relative to yaw and clamp to current ground height
            anchor += (right * moveInput.x + forward * moveInput.y) * moveSpeed * Time.deltaTime;
            anchor.y = groundHeight;
        }
    }

    // Handles zoom input and updates relevant state
    private void HandleZoom()
    {
        // Read zoom input
        float zoomInput = zoom.action.ReadValue<float>();

        // If zoom input should be processed
        if (zoomInput != 0f)
        {
            // If look input is coming from a gamepad
            if (lastDevice is Gamepad)
            {
                // Update the zoom (distance state) using gamepad input and gamepad zoom speed
                distance = Mathf.Clamp(distance - zoomInput * gamepadZoomSpeed * Time.deltaTime, minDistance, maxDistance);
            }
            // Otherwise use input from mouse
            else
            {
                // Update the zoom (distance state) using mouse input and mouse zoom speed
                distance = Mathf.Clamp(distance - zoomInput * mouseZoomSpeed, minDistance, maxDistance);
            }
        }
    }
}
