using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Takes the player to another scene: on entering, freezes them and shrinks their sprite into the portal while
// the enter effect and the screen flash play, then loads the scene after the delay (real time).
public class Portal : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [Tooltip("Real-time seconds from entering to loading the scene.")]
    [SerializeField, Min(0f)] private float loadDelay = 0.5f;

    [Header("Effects")]
    [Tooltip("Optional child played once when the player enters.")]
    [SerializeField] private ParticleSystem enterEffect;
    [Tooltip("Optional global volume, authored at weight 0, faded in over the delay (e.g. an exposure flash).")]
    [SerializeField] private Volume flashVolume;
    [Tooltip("Flash volume weight by fraction of the delay (0..1).")]
    [SerializeField] private AnimationCurve flashCurve = new(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.6f), new Keyframe(1f, 1f));

    private bool _entering;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_entering) return;

        var target = other.GetComponentInParent<PlayerController>();
        if (target == null || !target.enabled)
            return;

        _entering = true;
        StartCoroutine(Enter(target));
    }

    private IEnumerator Enter(PlayerController player)
    {
        // Disabling ends the current state, which restores gravity, so the body is frozen afterwards.
        player.enabled = false;
        player.Rigidbody.linearVelocity = Vector2.zero;
        player.Rigidbody.gravityScale = 0f;
        if (enterEffect != null) enterEffect.Play();

        Transform sprite = player.SpriteRenderer.transform;
        Vector3 spriteScale = sprite.localScale;

        for (float time = 0f; time < loadDelay; time += Time.unscaledDeltaTime)
        {
            float progress = time / loadDelay;
            sprite.localScale = spriteScale * (1f - progress);
            if (flashVolume != null) flashVolume.weight = flashCurve.Evaluate(progress);
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}
