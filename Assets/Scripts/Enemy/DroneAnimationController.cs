using UnityEngine;

public class DroneAnimationController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the EnemyVisuals object (or its Animator) here")]
    [SerializeField] private Animator animator;

    // Const animation state names matching Animator Controller
    private const string ANIM_MOVE = "Drone_Move";
    private const string ANIM_ATTACK = "Drone_Attack";
    private const string ANIM_DEATH = "Drone_Explosion";

    private string currentAnimation;

    private void Awake()
    {
        // Fallback: search on this object or children only if not assigned manually
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
    }

    // Тимчасовий метод для миттєвої перевірки при запуску гри
    private void Start()
    {
        PlayMove();
    }

    /// <summary>
    /// Call this method from EnemyController to switch animation states.
    /// </summary>
    public void ChangeAnimationState(string newAnimation)
    {
        if (animator == null)
        {
            Debug.LogWarning($"[DroneAnimationController] Animator component is missing on {gameObject.name}!");
            return;
        }

        if (currentAnimation == newAnimation) return;

        animator.Play(newAnimation);
        currentAnimation = newAnimation;
    }

    // Helper methods for easy call from state machine
    public void PlayMove() => ChangeAnimationState(ANIM_MOVE);
    public void PlayAttack() => ChangeAnimationState(ANIM_ATTACK);
    public void PlayDeath() => ChangeAnimationState(ANIM_DEATH);
}