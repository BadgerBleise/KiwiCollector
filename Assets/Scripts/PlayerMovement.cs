using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float airControlMultiplier = 0.8f;

    [Header("Jumping")]
    public float jumpForce = 14f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask whatIsGround;

    [Header("References")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public AudioSource jumpSound;

    [Header("AI Control")]
    public bool useAI = false;
    [HideInInspector] public float aiMoveInput = 0f;
    [HideInInspector] public bool aiJumpRequest = false;

    private bool isFacingRight = true;
    private bool isGrounded = false;
    private bool wasGrounded = false;

    void Update() {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        float moveInput = useAI ? aiMoveInput : Input.GetAxisRaw("Horizontal");

        float currentSpeed = moveSpeed;
        if (!isGrounded) {
            currentSpeed *= airControlMultiplier;
        }

        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);

        if (moveInput > 0 && !isFacingRight) {
            Flip();
        }
        else if (moveInput < 0 && isFacingRight) {
            Flip();
        }

        bool jumpPressed = useAI ? aiJumpRequest :
            (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W));

        if (jumpPressed && isGrounded) {
            if (jumpSound != null) {
                jumpSound.Play();
            }
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        ApplyBetterJumpPhysics();

        aiJumpRequest = false;
    }

    void ApplyBetterJumpPhysics() {
        if (rb.velocity.y < 0) {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space) && !useAI) {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    void Flip() {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
