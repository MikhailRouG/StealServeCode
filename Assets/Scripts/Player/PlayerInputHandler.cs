using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads player input from the "Player" action map and exposes it to other components.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Tooltip("Input actions asset. If empty, project-wide actions (InputSystem.actions) are used.")]
    [SerializeField] private InputActionAsset _inputActions;
    [SerializeField] private string _actionMapName = "Player";

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    /// <summary>True only in the frame when Jump was pressed.</summary>
    public bool JumpPressed { get; private set; }

    private void Awake()
    {
        InputActionAsset asset = _inputActions != null ? _inputActions : InputSystem.actions;
        if (asset == null)
        {
            Debug.LogError($"{nameof(PlayerInputHandler)}: no InputActionAsset assigned.", this);
            enabled = false;
            return;
        }

        InputActionMap map = asset.FindActionMap(_actionMapName, throwIfNotFound: true);
        _moveAction = map.FindAction("Move", throwIfNotFound: true);
        _lookAction = map.FindAction("Look");
        _jumpAction = map.FindAction("Jump");
    }

    private void OnEnable()
    {
        _moveAction?.actionMap.Enable();
    }

    private void OnDisable()
    {
        _moveAction?.actionMap.Disable();
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        JumpPressed = false;
    }

    private void Update()
    {
        MoveInput = _moveAction.ReadValue<Vector2>();
        LookInput = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
        JumpPressed = _jumpAction != null && _jumpAction.WasPressedThisFrame();
    }
}
