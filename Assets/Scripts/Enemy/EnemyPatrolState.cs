using UnityEngine;

// Walks between two waypoints, starting toward A. On reaching one it hands over to Idle for the wait time,
// then heads to the other. Only horizontal distance counts.
[DisallowMultipleComponent]
public class EnemyPatrolState : EnemyStateBase
{
    [Tooltip("Positions are captured at start, so the waypoints may be children of the enemy.")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField, Min(0f)] private float speed = 2f;
    [Tooltip("Horizontal distance (m) at which a waypoint counts as reached.")]
    [SerializeField, Min(0.01f)] private float arrivalDistance = 0.1f;
    [Tooltip("How long (s) to idle at each waypoint.")]
    [SerializeField, Min(0f)] private float waitTime = 1.5f;

    private Vector2[] _points;
    private int _targetIndex;
    private float _resumeAt;

    public override EnemyStateId Id => EnemyStateId.Patrol;
    public override int AnimationValue => Controller.Facing;

    public override void Initialize(EnemyController controller)
    {
        base.Initialize(controller);

        if (pointA == null || pointB == null)
        {
            Debug.LogError($"{nameof(EnemyPatrolState)} on {name} needs both waypoints; it won't patrol.", this);
            return;
        }

        _points = new Vector2[] { pointA.position, pointB.position };
    }

    public override bool CanBeActivated(EnemyStateId? currentState) => _points != null && Time.time >= _resumeAt;

    public override void UpdateState()
    {
        float dx = _points[_targetIndex].x - Rigidbody.position.x;

        if (Mathf.Abs(dx) <= arrivalDistance)
        {
            _targetIndex = 1 - _targetIndex;
            _resumeAt = Time.time + waitTime;
            BrakeHorizontal(Controller.Deceleration);
            return;
        }

        int direction = DirectionOf(dx);
        Controller.Face(direction);
        AccelerateHorizontal(direction * speed, Controller.Acceleration);
    }

    public override void ResetState()
    {
        _targetIndex = 0;
        _resumeAt = 0f;
    }

    // Always drawn: waypoints and the path between them; while playing, the current target is highlighted.
    private void OnDrawGizmos()
    {
        Vector2 a, b;
        if (_points != null)
        {
            a = _points[0];
            b = _points[1];
        }
        else if (pointA != null && pointB != null)
        {
            a = pointA.position;
            b = pointB.position;
        }
        else
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.15f);
        Gizmos.DrawWireSphere(b, 0.15f);

        if (_points == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_points[_targetIndex], 0.25f);
    }
}
