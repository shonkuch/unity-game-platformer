using UnityEngine;

// Runs the player state machine (priority = PlayerStateId value).
// Holds what the states share: sensors, sprite, horizontal tuning, facing.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputBinder))]
public class PlayerController : StateMachine<PlayerStateBase, PlayerStateId>
{
    [Header("Bindings")]
    [SerializeField] private TriggerSensor groundSensor;
    [SerializeField] private TriggerSensor wallLeftSensor;
    [SerializeField] private TriggerSensor wallRightSensor;
    [Tooltip("Covers the space between the crouched and standing collider tops; blocks standing up while touching.")]
    [SerializeField] private TriggerSensor ceilingSensor;
    [Tooltip("Must be on a child object: it's rotated to face the movement direction and squashed when crouching.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Horizontal")]
    [SerializeField, Min(0f)] private float maxSpeed = 8f;
    [SerializeField, Min(0f)] private float groundAcceleration = 60f;
    [Tooltip("Used with no input, when reversing, and when above max speed.")]
    [SerializeField, Min(0f)] private float groundDeceleration = 80f;
    [SerializeField, Min(0f)] private float airAcceleration = 30f;
    [SerializeField, Min(0f)] private float airDeceleration = 20f;
    [Tooltip("Horizontal boost acceleration (m/s²) by time since boost start (s), applied until landing: the held " +
             "direction on a ground jump, away from the wall on a wall jump or push-off. Until its last key, air control " +
             "can't brake against it; until landing, max speed doesn't. Holds its last key's value after the curve ends.")]
    [SerializeField] private AnimationCurve jumpHorizontalAcceleration = new(new Keyframe(0f, 40f), new Keyframe(0.15f, 0f));

    public PlayerInputBinder Input { get; private set; }
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    // Optional; null when the player has no PlayerStealth component.
    public PlayerStealth Stealth { get; private set; }
    // Optional; null when the player has no PlayerEffects component.
    public PlayerEffects Effects { get; private set; }

    public float MaxSpeed => maxSpeed;
    public float GroundAcceleration => groundAcceleration;
    public float GroundDeceleration => groundDeceleration;
    public float AirAcceleration => airAcceleration;
    public float AirDeceleration => airDeceleration;
    public AnimationCurve JumpHorizontalAcceleration => jumpHorizontalAcceleration;
    public float BoostCurveDuration =>
        jumpHorizontalAcceleration.length > 0 ? jumpHorizontalAcceleration[jumpHorizontalAcceleration.length - 1].time : 0f;

    public bool IsGrounded => groundSensor.IsTouching;
    public bool IsTouchingCeiling => ceilingSensor.IsTouching;
    // -1 left, 1 right.
    public int Facing { get; private set; } = 1;

    private Quaternion _spriteRotation;

    public bool IsTouchingWall(int side) => (side < 0 ? wallLeftSensor : wallRightSensor).IsTouching;

    private void Reset()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    protected override void Awake()
    {
        base.Awake();
        Input = GetComponent<PlayerInputBinder>();
        Stealth = GetComponent<PlayerStealth>();
        Effects = GetComponent<PlayerEffects>();

        if (groundSensor == null || wallLeftSensor == null || wallRightSensor == null || ceilingSensor == null ||
            spriteRenderer == null)
        {
            Fail("is missing bindings");
            return;
        }

        if (spriteRenderer.transform == transform)
        {
            Fail("needs the SpriteRenderer on a child object");
            return;
        }

        _spriteRotation = spriteRenderer.transform.localRotation;
        InitializeStates();
    }

    protected override void InitializeState(PlayerStateBase state) => state.Initialize(this);

    // Rotates the sprite child to face direction (-1 left, 1 right); 0 keeps the current facing.
    public void Face(int direction)
    {
        if (direction == 0 || direction == Facing) return;

        Facing = direction;
        spriteRenderer.transform.localRotation = _spriteRotation * Quaternion.Euler(0f, direction < 0 ? 180f : 0f, 0f);
    }
}
