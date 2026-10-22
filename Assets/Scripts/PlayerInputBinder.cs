using UnityEngine;

// Reads raw keyboard state through the legacy Input Manager and exposes it to the controller.
public class PlayerInputBinder : MonoBehaviour
{
    [SerializeField] private KeyCode[] leftKeys = { KeyCode.A, KeyCode.LeftArrow };
    [SerializeField] private KeyCode[] rightKeys = { KeyCode.D, KeyCode.RightArrow };
    [SerializeField] private KeyCode[] jumpKeys = { KeyCode.Space, KeyCode.W, KeyCode.UpArrow };
    [SerializeField] private KeyCode[] crouchKeys = { KeyCode.S, KeyCode.DownArrow };
    [SerializeField] private KeyCode[] attackKeys = { KeyCode.J, KeyCode.Mouse0 };
    [Tooltip("How long (s) a jump press stays valid, so pressing just before landing still jumps.")]
    [SerializeField, Min(0.02f)] private float jumpBufferTime = 0.12f;
    [Tooltip("How long (s) an attack press stays valid, so pressing just before an attack can start still attacks.")]
    [SerializeField, Min(0.02f)] private float attackBufferTime = 0.12f;

    // -1 left, 0 none (or both), 1 right.
    public float MoveAxis { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool JumpBuffered => _jumpBufferTimer > 0f;
    public bool AttackBuffered => _attackBufferTimer > 0f;

    private float _jumpBufferTimer;
    private float _attackBufferTimer;

    private void Update()
    {
        MoveAxis = (AnyHeld(rightKeys) ? 1f : 0f) - (AnyHeld(leftKeys) ? 1f : 0f);
        CrouchHeld = AnyHeld(crouchKeys);

        _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.deltaTime);
        if (AnyPressed(jumpKeys))
            _jumpBufferTimer = jumpBufferTime;

        _attackBufferTimer = Mathf.Max(0f, _attackBufferTimer - Time.deltaTime);
        if (AnyPressed(attackKeys))
            _attackBufferTimer = attackBufferTime;
    }

    private void OnDisable()
    {
        MoveAxis = 0f;
        CrouchHeld = false;
        _jumpBufferTimer = 0f;
        _attackBufferTimer = 0f;
    }

    // Returns whether a buffered press was used up.
    public bool ConsumeJump()
    {
        if (!JumpBuffered) return false;

        _jumpBufferTimer = 0f;
        return true;
    }

    // Returns whether a buffered press was used up.
    public bool ConsumeAttack()
    {
        if (!AttackBuffered) return false;

        _attackBufferTimer = 0f;
        return true;
    }

    private static bool AnyHeld(KeyCode[] keys)
    {
        foreach (KeyCode key in keys)
            if (Input.GetKey(key)) return true;
        return false;
    }

    private static bool AnyPressed(KeyCode[] keys)
    {
        foreach (KeyCode key in keys)
            if (Input.GetKeyDown(key)) return true;
        return false;
    }
}
