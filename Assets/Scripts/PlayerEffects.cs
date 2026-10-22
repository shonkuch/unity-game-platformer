using UnityEngine;

// Particle feedback for player actions; states call it. Every system is optional and should be a child of the
// Player root (not of the sprite) using World simulation space. Hit particles live on Damageable instead.
[DisallowMultipleComponent]
public class PlayerEffects : MonoBehaviour
{
    [Tooltip("Played on landing, placed at the feet.")]
    [SerializeField] private ParticleSystem landing;
    [Tooltip("Played on starting a wall grab. Place it at the right wall; it is mirrored for the left wall.")]
    [SerializeField] private ParticleSystem wallGrab;
    [Tooltip("Played when an attack starts. Place it for facing right; it is mirrored when facing left.")]
    [SerializeField] private ParticleSystem attack;

    private Pose _wallGrabPose;
    private Pose _attackPose;

    private void Awake()
    {
        if (wallGrab != null) _wallGrabPose = LocalPose(wallGrab);
        if (attack != null) _attackPose = LocalPose(attack);
    }

    public void PlayLanding()
    {
        if (landing != null) landing.Play();
    }

    // side: -1 left wall, 1 right wall.
    public void PlayWallGrab(int side) => PlayMirrored(wallGrab, _wallGrabPose, side);

    // facing: -1 left, 1 right.
    public void PlayAttack(int facing) => PlayMirrored(attack, _attackPose, facing);

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
