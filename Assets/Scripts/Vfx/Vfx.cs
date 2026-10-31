using UnityEngine;

// Effects that must outlive whatever triggers them: one-shot prefabs (impacts, deaths) and attached loops
// (trails) left behind when their owner is destroyed. Both destroy themselves once their particles are gone.
public static class Vfx
{
    // Instantiates and plays prefab; null does nothing. Returns the instance, or null.
    public static ParticleSystem Spawn(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        ParticleSystem instance = Object.Instantiate(prefab, position, rotation);
        ParticleSystem.MainModule main = instance.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
        instance.Play(true);
        return instance;
    }

    // Unparents an attached effect and stops its emission so its live particles can fade out on their own.
    public static void Release(ParticleSystem attached)
    {
        if (attached == null) return;

        attached.transform.SetParent(null, true);
        ParticleSystem.MainModule main = attached.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
        attached.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
