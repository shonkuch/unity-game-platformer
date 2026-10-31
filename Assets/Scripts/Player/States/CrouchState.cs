using UnityEngine;

// Crouch key held: slower movement, shorter capsule and a squashed sprite.
// Starts only on the ground; stays while held or while the ceiling sensor blocks standing up.
// After a jump ends it, the key must be released and pressed again.
[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider2D))]
public class CrouchState : MovingState
{
    [SerializeField, Range(0f, 1f)] private float crouchSpeedMultiplier = 0.4f;
    [Tooltip("Crouched collider height as a fraction of the standing height; the feet stay in place.")]
    [SerializeField, Range(0.1f, 1f)] private float crouchHeightMultiplier = 0.5f;
    [Tooltip("Crouched sprite Y scale as a fraction of the standing scale; the sprite's bottom stays in place.")]
 [SerializeField, Range(0.1f, 1f)] private float crouchSpriteScaleMultiplier = 1;
    private CapsuleCollider2D _body;
    private Transform _sprite;
    private Vector2 _standingBodySize;
    private Vector2 _standingBodyOffset;
    private Vector3 _standingSpriteScale;
    private Vector3 _standingSpritePosition;
    private bool _needsRepress;

    public override PlayerStateId Id => PlayerStateId.Crouch;
    protected override float SpeedCap => Controller.MaxSpeed * crouchSpeedMultiplier;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
        _body = GetComponent<CapsuleCollider2D>();
        _sprite = Controller.SpriteRenderer.transform;
        _standingBodySize = _body.size;
        _standingBodyOffset = _body.offset;
        _standingSpriteScale = _sprite.localScale;
        _standingSpritePosition = _sprite.localPosition;
    }

    public override bool CanBeActivated(PlayerStateId? currentState)
    {
        bool held = Input.CrouchHeld;
        if (!held) _needsRepress = false;

        bool wantsCrouch = held && !_needsRepress;
        if (currentState == Id) return wantsCrouch || Controller.IsTouchingCeiling;
        return wantsCrouch && Controller.IsGrounded;
    }

    public override void EnterState()
    {
        base.EnterState();
        SetCrouching(true);
        if (Controller.Effects != null) Controller.Effects.PlayCrouch();
    }

    public override void EndState()
    {
        base.EndState();
        SetCrouching(false);

        // Only a higher state (a jump) ends the crouch while the key is still held.
        if (Input.CrouchHeld) _needsRepress = true;
    }

    public override void ResetState() => _needsRepress = false;

    private void SetCrouching(bool crouching)
    {
        float height = _standingBodySize.y * (crouching ? crouchHeightMultiplier : 1f);
        float feet = _standingBodyOffset.y - _standingBodySize.y / 2f;
        _body.size = new Vector2(_standingBodySize.x, height);
        _body.offset = new Vector2(_standingBodyOffset.x, feet + height / 2f);

        // Squash around the sprite's bottom edge so it stays on the ground.
        float scaleY = _standingSpriteScale.y * (crouching ? crouchSpriteScaleMultiplier : 1f);
        Sprite sprite = Controller.SpriteRenderer.sprite;
        float spriteBottom = sprite != null ? sprite.bounds.min.y : 0f;
        _sprite.localScale = new Vector3(_standingSpriteScale.x, scaleY, _standingSpriteScale.z);
        _sprite.localPosition = _standingSpritePosition + Vector3.up * (spriteBottom * (_standingSpriteScale.y - scaleY));
    }
}
