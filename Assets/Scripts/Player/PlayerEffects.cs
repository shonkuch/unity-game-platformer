using UnityEngine;

// Particle feedback for player actions; states call it, and it watches stealth for the cloak breaking. Every system
// is optional and should be a child of the Player root (not of the sprite) using World simulation space.
// Hit particles and the death effect live on Damageable instead.
[DisallowMultipleComponent]
public class PlayerEffects : MonoBehaviour
{
    [Tooltip("Played on landing, placed at the feet.")]
    [SerializeField] private ParticleSystem landing;
    [Tooltip("Played on a ground jump's takeoff, placed at the feet.")]
    [SerializeField] private ParticleSystem jump;
    [Tooltip("Looping, emitting by distance while running on the ground; placed at the feet.")]
    [SerializeField] private ParticleSystem run;
    [Tooltip("Played on starting a wall grab and on a wall jump. Place it at the right wall; it is mirrored for the left wall.")]
    [SerializeField] private ParticleSystem wallGrab;
    [Tooltip("Played when an attack starts. Place it for facing right; it is mirrored when facing left.")]
    [SerializeField] private ParticleSystem attack;
    [Tooltip("Played on starting a crouch, placed at the feet.")]
    [SerializeField] private ParticleSystem crouch;
    [Tooltip("Played when active stealth ends (moving, or being hit).")]
    [SerializeField] private ParticleSystem cloakBreak;

    private PlayerStealth _stealth;
    private Pose _wallGrabPose;
    private Pose _attackPose;
    private bool _wasCloaked;

    private void Awake()
    {
        _stealth = GetComponent<PlayerStealth>();
        if (wallGrab != null) _wallGrabPose = LocalPose(wallGrab);
        if (attack != null) _attackPose = LocalPose(attack);
    }

    private void LateUpdate()
    {
        if (_stealth == null) return;

        bool cloaked = _stealth.IsStealthActive;
        if (cloaked == _wasCloaked) return;

        _wasCloaked = cloaked;
        if (!cloaked) Play(cloakBreak);
    }

    public void PlayLanding() => Play(landing);

    public void PlayJump() => Play(jump);

    public void PlayCrouch() => Play(crouch);

    public void SetRunning(bool running)
    {
        if (run == null || running == run.isEmitting) return;

        if (running) run.Play();
        else run.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // side: -1 left wall, 1 right wall.
    public void PlayWallGrab(int side) => PlayMirrored(wallGrab, _wallGrabPose, side);

    // facing: -1 left, 1 right.
    public void PlayAttack(int facing) => PlayMirrored(attack, _attackPose, facing);

    private static void Play(ParticleSystem particles)
    {
        if (particles != null) particles.Play();
    }

    // Mirrors the right-side pose (position and emission direction) to the left side when side < 0.
    private static void PlayMirrored(ParticleSystem particles, Pose rightPose, int side)
    {
        if (particles == null) return;

        Vector3 position = rightPose.position;
        particles.transform.localPosition = new Vector3(position.x * side, position.y, position.z);
        particles.transform.localRotation = Quaternion.Euler(0f, side < 0 ? 180f : 0f, 0f) * rightPose.rotation;
        particles.Play();
    }

    private static Pose LocalPose(ParticleSystem particles) =>
        new(particles.transform.localPosition, particles.transform.localRotation);
}
