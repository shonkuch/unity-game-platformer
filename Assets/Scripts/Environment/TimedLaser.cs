using UnityEngine;

// Cycles off (interval) → telegraph (warning, harmless) → on (interval). While on, damages the first hitbox it
// touches and switches off. Every effect is optional.
public class TimedLaser : MonoBehaviour
{
    private enum Phase { Off, Telegraph, On }

    [SerializeField] private SpriteRenderer laserBeam;
    [Tooltip("Time (s) the beam stays on, and stays off before the telegraph.")]
    [SerializeField] private float interval;
    [Tooltip("Warning time (s) between off and on; the beam can't hurt yet. 0 skips it.")]
    [SerializeField, Min(0f)] private float telegraphDuration = 0.4f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private Transform castPoint;
    [SerializeField] private Vector2 castSize = new(1f, 1f);
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Effects")]
    [Tooltip("Thin warning line shown, flickering, during the telegraph.")]
    [SerializeField] private SpriteRenderer telegraphLine;
    [Tooltip("Telegraph line flickers per second.")]
    [SerializeField, Min(0.1f)] private float telegraphFlickerRate = 16f;
    [Tooltip("Looping child played during the telegraph (emitter sparks).")]
    [SerializeField] private ParticleSystem telegraphEffect;
    [Tooltip("Child played once when the beam turns on.")]
    [SerializeField] private ParticleSystem activateEffect;
    [Tooltip("Child played once when the beam turns off (interval over, or after a hit).")]
    [SerializeField] private ParticleSystem deactivateEffect;
    [Tooltip("Prefab spawned at the center of whatever the beam damages.")]
    [SerializeField] private ParticleSystem hitEffect;

    private Phase _phase;
    private float _timer;

    private void Start()
    {
        if (castPoint == null)
            Debug.LogError($"{nameof(TimedLaser)} on {name} has no cast point; attacks won't hit.", this);

        SetPhase(Phase.Off);
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Off:
                if (_timer >= interval) SetPhase(telegraphDuration > 0f ? Phase.Telegraph : Phase.On);
                break;
            case Phase.Telegraph:
                if (telegraphLine != null) telegraphLine.enabled = Mathf.Repeat(_timer * telegraphFlickerRate, 1f) < 0.5f;
                if (_timer >= telegraphDuration) SetPhase(Phase.On);
                break;
            case Phase.On:
                if (_timer >= interval) SetPhase(Phase.Off);
                break;
        }
    }

    private void SetPhase(Phase phase)
    {
        if (_phase == Phase.On && phase == Phase.Off && deactivateEffect != null) deactivateEffect.Play();

        _phase = phase;
        _timer = 0;
        laserBeam.enabled = phase == Phase.On;
        if (telegraphLine != null) telegraphLine.enabled = phase == Phase.Telegraph;

        if (telegraphEffect != null)
        {
            if (phase == Phase.Telegraph) telegraphEffect.Play();
            else telegraphEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (phase == Phase.On && activateEffect != null) activateEffect.Play();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_phase != Phase.On)
            return;

        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var target = other.GetComponentInParent<Damageable>();
        if (target == null)
            return;

        if (target.IsHitbox(other))
        {
            target.TakeDamage(damage);
            Vfx.Spawn(hitEffect, other.bounds.center, Quaternion.identity);
            SetPhase(Phase.Off);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (castPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(castPoint.position, castSize);
    }
}
