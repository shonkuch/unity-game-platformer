using UnityEngine;
using UnityEngine.SceneManagement;

// Hit-stop and slow motion through Time.timeScale, measured in real time. Overlapping holds merge into the
// slowest scale and the latest end. The scale returns to 1 when the hold ends and on every scene load,
// so a running hold never leaks into the next scene.
public class TimeEffects : MonoBehaviour
{
    private static TimeEffects _instance;

    private float _scale = 1f;
    private float _until;

    private bool IsHolding => _until > Time.unscaledTime;

    private static TimeEffects Instance
    {
        get
        {
            if (_instance != null) return _instance;

            var host = new GameObject(nameof(TimeEffects));
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<TimeEffects>();
            return _instance;
        }
    }

    // Holds the time scale at scale (0 freezes) for duration real-time seconds. A duration of 0 does nothing.
    public static void Hold(float scale, float duration)
    {
        if (duration <= 0f) return;

        TimeEffects instance = Instance;
        float until = Time.unscaledTime + duration;

        if (instance.IsHolding)
        {
            instance._scale = Mathf.Min(instance._scale, scale);
            instance._until = Mathf.Max(instance._until, until);
        }
        else
        {
            instance._scale = scale;
            instance._until = until;
        }

        Time.timeScale = instance._scale;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        if (_until > 0f && !IsHolding) Restore();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Restore();

    private void Restore()
    {
        _until = 0f;
        _scale = 1f;
        Time.timeScale = 1f;
    }
}
