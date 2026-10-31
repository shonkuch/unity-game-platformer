using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// On every hit, punches the volume's vignette toward the damage look, then bleeds back to the profile's own look.
// A hit during the effect rises again from the current strength, so it never pops. Runs on real time so
// hit-stop doesn't freeze it, and edits a runtime copy of the profile, never the asset.
[DisallowMultipleComponent]
public class DamageVignette : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private Damageable damageable;

    [Header("Peak")]
    [SerializeField] private Color damageColor = new(0.533f, 0.031f, 0.251f);
    [SerializeField, Range(0f, 1f)] private float peakIntensity = 0.6f;
    [SerializeField, Range(0.01f, 1f)] private float peakSmoothness = 0.5f;

    [Header("Timing")]
    [Tooltip("Real-time seconds to rise from the profile's look to the peak.")]
    [SerializeField, Min(0f)] private float attack = 0.06f;
    [Tooltip("Strength (1 peak, 0 the profile's look) by real-time seconds since the peak.")]
    [SerializeField] private AnimationCurve release = new(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.6f, 0f, 0f, 0f));

    private VolumeProfile _profile;
    private Vignette _vignette;
    private Color _baseColor;
    private float _baseIntensity;
    private float _baseSmoothness;
    private float _strength;
    private float _time;
    private bool _rising;
    private bool _playing;

    private float ReleaseEnd => release.length > 0 ? release[release.length - 1].time : 0f;

    private void Reset()
    {
        volume = GetComponentInChildren<Volume>();
        damageable = GetComponent<Damageable>();
    }

    private void Awake()
    {
        if (volume == null || damageable == null)
        {
            Debug.LogError($"{nameof(DamageVignette)} on {name} needs a volume and a damageable.", this);
            enabled = false;
            return;
        }

        // Volume.profile copies the shared profile on first access.
        _profile = volume.profile;
        if (!_profile.TryGet(out _vignette)) _vignette = _profile.Add<Vignette>(true);

        _baseColor = _vignette.color.value;
        _baseIntensity = _vignette.intensity.value;
        _baseSmoothness = _vignette.smoothness.value;
        _vignette.color.overrideState = true;
        _vignette.intensity.overrideState = true;
        _vignette.smoothness.overrideState = true;
    }

    private void OnEnable()
    {
        if (damageable != null) damageable.damaged.AddListener(OnDamaged);
    }

    private void OnDisable()
    {
        if (damageable != null) damageable.damaged.RemoveListener(OnDamaged);
    }

    private void OnDestroy()
    {
        if (_profile != null) Destroy(_profile);
    }

    private void Update()
    {
        if (!_playing) return;

        _time += Time.unscaledDeltaTime;

        if (_rising)
        {
            _strength = attack > 0f ? Mathf.Clamp01(_time / attack) : 1f;
            if (_strength >= 1f)
            {
                _rising = false;
                _time = 0f;
            }
        }
        else
        {
            _strength = Mathf.Clamp01(release.Evaluate(Mathf.Min(_time, ReleaseEnd)));
            if (_time >= ReleaseEnd)
            {
                _strength = 0f;
                _playing = false;
            }
        }

        Apply();
    }

    private void OnDamaged(int hp)
    {
        _playing = true;
        _rising = true;
        _time = attack * _strength;
    }

    private void Apply()
    {
        _vignette.color.value = Color.Lerp(_baseColor, damageColor, _strength);
        _vignette.intensity.value = Mathf.Lerp(_baseIntensity, peakIntensity, _strength);
        _vignette.smoothness.value = Mathf.Lerp(_baseSmoothness, peakSmoothness, _strength);
    }
}
