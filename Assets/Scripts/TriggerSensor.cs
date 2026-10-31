using System.Collections.Generic;
using UnityEngine;

// Reports colliders on the given layers that overlap this GameObject's trigger collider, ignoring colliders on the
// same Rigidbody2D. Queried live from the collider's pose after the last physics step rather than tracked through
// trigger enter/exit events, so it releases on the first step it stops overlapping.
[RequireComponent(typeof(Collider2D))]
public class TriggerSensor : MonoBehaviour
{
    [SerializeField] private LayerMask detectLayers = ~0;

    private readonly List<Collider2D> _results = new();
    private Collider2D _collider;
    private ContactFilter2D _solidFilter;
    private ContactFilter2D _anyFilter;

    // Whether a solid (non-trigger) collider overlaps the sensor.
    public bool IsTouching => Query(_solidFilter, _results) > 0;

    // Fills results with the current overlaps, triggers included (cleared first).
    public void GetOverlaps(List<Collider2D> results) => Query(_anyFilter, results);

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _solidFilter = new ContactFilter2D { useTriggers = false };
        _solidFilter.SetLayerMask(detectLayers);
        _anyFilter = new ContactFilter2D { useTriggers = true };
        _anyFilter.SetLayerMask(detectLayers);
    }

    private int Query(ContactFilter2D filter, List<Collider2D> results)
    {
        results.Clear();
        if (!isActiveAndEnabled || !_collider.enabled) return 0;

        _collider.Overlap(filter, results);

        Rigidbody2D own = _collider.attachedRigidbody;
        if (own == null) return results.Count;

        for (int i = results.Count - 1; i >= 0; i--)
            if (results[i].attachedRigidbody == own) results.RemoveAt(i);
        return results.Count;
    }
}
