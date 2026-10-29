using Unity.Cinemachine;
using UnityEngine;

// Camera shake and time effects when its Damageable is hit or dies. Shake needs a CinemachineImpulseListener
// on the camera. Any effect set to 0 is skipped.
[DisallowMultipleComponent]
[RequireComponent(typeof(Damageable))]
public class DamageFeedback : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [Tooltip("Impulse force on every hit that doesn't kill.")]
    [SerializeField, Min(0f)] private float hitShake = 0.15f;
    [Tooltip("Impulse force on death.")]
    [SerializeField, Min(0f)] private float deathShake = 0.3f;
    [Tooltip("Time scale held on death; 0 freezes (hit-stop).")]
    [SerializeField, Range(0f, 1f)] private float deathTimeScale;
    [Tooltip("Real-time seconds the death time scale is held.")]
    [SerializeField, Min(0f)] private float deathTimeDuration = 0.05f;

    private Damageable _damageable;

    private void Reset()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Awake()
    {
        _damageable = GetComponent<Damageable>();
    }

    private void OnEnable()
    {
        _damageable.damaged.AddListener(OnDamaged);
        _damageable.died.AddListener(OnDied);
    }

    private void OnDisable()
    {
        _damageable.damaged.RemoveListener(OnDamaged);
        _damageable.died.RemoveListener(OnDied);
    }

    // Died follows on the same call, so a lethal hit only shakes once.
    private void OnDamaged(int hp)
    {
        if (!_damageable.IsDead) Shake(hitShake);
    }

    private void OnDied()
    {
        Shake(deathShake);
        TimeEffects.Hold(deathTimeScale, deathTimeDuration);
    }

    private void Shake(float force)
    {
        if (impulseSource != null && force > 0f) impulseSource.GenerateImpulseWithForce(force);
    }
}
