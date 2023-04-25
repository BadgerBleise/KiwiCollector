using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public Transform groundCheck;
    public float checkRadius = 0.1f;
    public LayerMask whatIsGround;
    private bool isFacingRight = true;
    private bool isGrounded = false;
    public AudioSource jumpSound;


    void Update()
    {
        // Check if player is touching ground
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        // Movement input
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // Flip sprite based on movement direction
        if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }

        // Jump input
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) && isGrounded)
        {
            // Adding jump sound
            jumpSound.Play();
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
        // Get the velocity
        Vector2 horizontalMove = rb.velocity;
        // Don't use the vertical velocity
        horizontalMove.y = 0;
        // Calculate the approximate distance that will be traversed
        float distance = horizontalMove.magnitude * Time.fixedDeltaTime;
        // Normalize horizontalMove since it should be used to indicate direction
        horizontalMove.Normalize();
        RaycastHit2D[] hits = new RaycastHit2D[1];

        // Check if the body's current velocity will result in a collision
        if (rb.Cast(horizontalMove, hits, distance) > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.CompareTag("Ground")) 
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                }
            }
        }

    }

   

    void Flip()
    {
        // Switch the way the player is facing
        isFacingRight = !isFacingRight;

        // Flip the sprite
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    /*void OnCollisionStay2D(Collision2D other) 
    {
        if(other.gameObject.CompareTag("Ground"))
     {
        
         rb.velocity = new Vector2(0, 0);
     }
    }*/
}