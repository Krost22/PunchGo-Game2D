using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Input Settings")]
    [Tooltip("Reference to the Move action from your Input Actions asset. This handles PC (WASD) and supported joysticks automatically.")]
    public InputActionReference moveAction;

    [Header("Mobile Joystick (Optional Custom Override)")]
    [Tooltip("Vector2 for a custom Android UI joystick input. Can be updated via SetJoystickInput if you aren't using the OnScreenStick component.")]
    public Vector2 customJoystickInput;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Ensure the Rigidbody is configured for standard top-down 2D movement by default
        if (rb.gravityScale > 0)
        {
            Debug.LogWarning("CharacterController: Rigidbody2D gravity scale is > 0. If this is a top-down game, set it to 0.");
        }
    }

    void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }
    }

    void Update()
    {
        // 1. Read input from the New Input System (handles WASD, Gamepads, and OnScreenSticks seamlessly)
        Vector2 inputSystemVector = Vector2.zero;
        if (moveAction != null)
        {
            inputSystemVector = moveAction.action.ReadValue<Vector2>();
        }

        // 2. Determine movement direction. 
        // If a custom UI Joystick is providing input, prioritize it. Otherwise use the New Input System.
        if (customJoystickInput.magnitude > 0.01f)
        {
            movement = customJoystickInput.normalized;
        }
        else
        {
            movement = inputSystemVector.normalized;
        }
    }

    void FixedUpdate()
    {
        // Apply physics-based movement using Rigidbody2D for maximum stability
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        // Rotate the character to smoothly look at the movement direction
        if (movement.sqrMagnitude > 0.01f)
        {
            // Calculate the angle. Subtracting 90 aligns the top of the sprite with the movement.
            float targetAngle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = Mathf.LerpAngle(rb.rotation, targetAngle, Time.fixedDeltaTime * 15f);
        }
    }

    // --- Custom Android Joystick UI Callbacks ---
    // If you have a custom joystick script instead of Unity's OnScreenStick, you can hook it up here.
    
    public void SetJoystickInput(Vector2 inputDirection)
    {
        customJoystickInput = inputDirection;
    }

    public void ClearJoystickInput()
    {
        customJoystickInput = Vector2.zero;
    }
}
