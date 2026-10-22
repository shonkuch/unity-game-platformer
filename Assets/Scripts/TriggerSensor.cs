using System.Collections.Generic;
using UnityEngine;

// Tracks colliders on the given layers that overlap this GameObject's trigger collider.
// Lives on a child of the player so its trigger events come from the player's Rigidbody2D.
[RequireComponent(typeof(Collider2D))]
public class TriggerSensor : MonoBehaviour
{
    [SerializeField] private LayerMask detectLayers = ~0;

    private readonly HashSet<Collider2D> _overlaps = new();

    public bool IsTouching => _nonTriggerOverlaps().Count > 0;

    private HashSet<Collider2D> _nonTriggerOverlaps()
    {
        HashSet<Collider2D> overlaps = new();
        
        foreach (var overlap in _overlaps)
        {
            if (!overlap.isTrigger)
            {
                overlaps.Add(overlap);
            }
        }
        
        return overlaps;
    }
    
    // Fills results with the current overlaps (cleared first).
    public void GetOverlaps(List<Collider2D> results)
    {
        results.Clear();
        results.AddRange(_overlaps);
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other);
        if ((detectLayers.value & (1 << other.gameObject.layer)) != 0)
            _overlaps.Add(other);
    }

    // Also fires for colliders that get disabled or destroyed (Physics2D "Callbacks On Disable" is on).
    private void OnTriggerExit2D(Collider2D other)
    {
        _overlaps.Remove(other);
    }

    private void OnDisable()
    {
        _overlaps.Clear();
    }
}
