using UnityEngine;

// Cloaks the player while standing still. Stationary states call Activate, moving states call Deactivate;
// the curve value is written to a float property of the sprite's material every frame it changes.
// A started deactivation always plays to the end; standing still again re-cloaks only after it finishes.
[DisallowMultipleComponent]
public class PlayerStealth : MonoBehaviour
{
    private const int MatchSamples = 64;

    private enum Phase { Off, Activating, Active, Deactivating }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Float property of the sprite's material that receives the stealth value.")]
    [SerializeField] private string materialProperty = "_Phase";
    [Tooltip("Stealth value by time (s) since standing still began. Stealth becomes active when the curve ends.")]
    [SerializeField] private AnimationCurve activationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Stealth value by time (s) since moving began. Always plays to the end once started.")]
    [SerializeField] private AnimationCurve deactivationCurve = AnimationCurve.EaseInOut(0f, 1f, 0.25f, 0f);

    public bool IsStealthActive => _phase == Phase.Active;
    public float Value { get; private set; }

    private MaterialPropertyBlock _block;
    private int _propertyId;
    // Optional; any hit drops stealth.
    private Damageable _damageable;
    private Phase _phase;
    // Whether the player is standing still (last call was Activate rather than Deactivate).
    private bool _wantsStealth;
    private float _time;

    private AnimationCurve PlayingCurve => _phase == Phase.Activating ? activationCurve : deactivationCurve;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        _propertyId = Shader.PropertyToID(materialProperty);
        _damageable = GetComponent<Damageable>();

        if (spriteRenderer == null)
        {
            Debug.LogError($"{nameof(PlayerStealth)} on {name} is missing its SpriteRenderer.", this);
            enabled = false;
            return;
        }

        ApplyValue();
    }

    private void OnEnable()
    {
        if (_damageable != null) _damageable.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (_damageable != null) _damageable.Damaged -= OnDamaged;

        _phase = Phase.Off;
        _wantsStealth = false;
        Value = 0f;
        if (spriteRenderer != null) ApplyValue();
    }

    private void Update()
    {
        if (_phase != Phase.Activating && _phase != Phase.Deactivating) return;

        AnimationCurve curve = PlayingCurve;
        float end = EndTime(curve);
        _time += Time.deltaTime;
        Value = curve.Evaluate(Mathf.Min(_time, end));
        ApplyValue();

        if (_time < end) return;

        if (_phase == Phase.Activating)
        {
            _phase = Phase.Active;
        }
        else
        {
            _phase = Phase.Off;
            if (_wantsStealth) StartCurve(Phase.Activating);
        }
    }

    // Starts cloaking, or queues it behind a running deactivation. Repeated calls do nothing.
    public void Activate()
    {
        if (!enabled) return;

        _wantsStealth = true;
        if (_phase == Phase.Off) StartCurve(Phase.Activating);
    }

    // IsStealthActive turns false immediately; the value then fades out over the deactivation curve.
    public void Deactivate()
    {
        if (!enabled) return;

        _wantsStealth = false;
        if (_phase == Phase.Activating || _phase == Phase.Active) StartCurve(Phase.Deactivating);
    }

    // Drops the value straight to 0 (e.g. when hit). If the player is still standing, cloaking restarts from 0.
    public void DropInstantly()
    {
        if (!enabled) return;

        _phase = Phase.Off;
        Value = 0f;
        ApplyValue();

        if (_wantsStealth) StartCurve(Phase.Activating);
    }

    private void OnDamaged(int amount) => DropInstantly();

    // Starts the curve where its value is closest to the current one, so an interrupted fade doesn't pop.
    private void StartCurve(Phase phase)
    {
        _phase = phase;
        _time = TimeOfClosestValue(PlayingCurve, Value);
    }

    private void ApplyValue()
    {
        spriteRenderer.GetPropertyBlock(_block);
        _block.SetFloat(_propertyId, Value);
        spriteRenderer.SetPropertyBlock(_block);
    }

    private static float TimeOfClosestValue(AnimationCurve curve, float value)
    {
        if (curve.length == 0) return 0f;

        float start = curve[0].time;
        float end = EndTime(curve);
        float bestTime = start;
        float bestDistance = float.MaxValue;

        for (int i = 0; i <= MatchSamples; i++)
        {
            float time = Mathf.Lerp(start, end, i / (float)MatchSamples);
            float distance = Mathf.Abs(curve.Evaluate(time) - value);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTime = time;
            }
        }

        return bestTime;
    }

    private static float EndTime(AnimationCurve curve) => curve.length > 0 ? curve[curve.length - 1].time : 0f;
}
