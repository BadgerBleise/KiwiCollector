using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class RuleBasedAI : MonoBehaviour {
    [Header("References")]
    public PlayerMovement playerMovement;
    public Transform groundCheck;
    public Rigidbody2D rb;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask spikeLayer;

    [Header("Detection Distances")]
    public float groundCheckRadius = 0.2f;
    public float wallCheckDistance = 3.5f;
    public float edgeCheckDistance = 1.0f;
    public float platformCheckForwardDist = 2.0f;
    public float platformCheckUpwardDist = 5.0f;
    public float spikeCheckDistance = 4.0f;

    [Header("Jump Settings")]
    public float jumpCooldown = 1.0f;
    public float minDistanceFromWall = 1.8f;
    public float targetLockDuration = 3.0f;
    public bool enableJumpUpToKiwiAbove = true;
    public bool enableDropDownToFinish = true;

    [Header("Anti-Stuck")]
    public float stuckCheckTime = 2.5f;
    public float stuckDistance = 0.4f;
    public float targetSwitchTime = 6.0f;

    [Header("Collection")]
    public float collectionRadius = 1.0f;
    public float jumpUpThreshold = 0.5f;
    public float jumpUpVerticalMin = 1.0f;
    public float dropDownThreshold = 2.0f;

    private float lastJumpTime = -999f;
    private Vector2 lastStuckCheckPosition;
    private float lastStuckCheckTimestamp;
    private float targetStartTime = -999f;

    private bool hasTargetPlatform = false;
    private float targetPlatformDirection = 0f;
    private float targetLockStartTime = -999f;
    private bool wasGrounded = true;
    private bool needsToJump = false;
    private float directionWhenJumped = 1f;

    private Transform currentTarget;
    private Transform finishLine;
    private float moveDirection = 1f;

    void Reset() {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start() {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (groundCheck == null)
            groundCheck = playerMovement.groundCheck;

        playerMovement.useAI = true;
        finishLine = GameObject.FindGameObjectWithTag("Finish")?.transform;
        lastStuckCheckPosition = transform.position;
        lastStuckCheckTimestamp = Time.time;

        FindNextTarget();
    }

    void Update() {
        Vector2 pos = transform.position;
        bool isGrounded = CheckIfOnGround();

        if (!isGrounded) {
            if (wasGrounded) {
                directionWhenJumped = moveDirection;
            }

            moveDirection = directionWhenJumped;
            playerMovement.aiMoveInput = directionWhenJumped;
            wasGrounded = isGrounded;
            return;
        }

        if (!wasGrounded) {
            hasTargetPlatform = false;
            needsToJump = false;
        }

        if (currentTarget != null) {
            float distToTarget = Vector2.Distance(transform.position, currentTarget.position);

            if (distToTarget < collectionRadius) {
                Debug.Log($"Reached target at {currentTarget.position}, finding next...");
                FindNextTarget();
            }
        }

        if (currentTarget == null) {
            FindNextTarget();
        }

        if (currentTarget != null) {
            NavigateToTarget(pos, isGrounded);
        }

        playerMovement.aiMoveInput = moveDirection;
        wasGrounded = isGrounded;

        CheckIfStuck();
    }

    void FindNextTarget() {
        GameObject[] kiwis = GameObject.FindGameObjectsWithTag("Kiwi");

        if (kiwis.Length > 0) {
            Transform closest = null;
            float closestDistance = Mathf.Infinity;

            foreach (GameObject kiwi in kiwis) {
                float distance = Vector2.Distance(transform.position, kiwi.transform.position);

                if (distance < closestDistance) {
                    closestDistance = distance;
                    closest = kiwi.transform;
                }
            }

            currentTarget = closest;
            targetStartTime = Time.time;
            Debug.Log($"New target: Closest Kiwi at {currentTarget.position} (Distance={closestDistance:F2})");
        }
        else {
            currentTarget = finishLine;
            Debug.Log("All kiwis collected! Going to finish.");
        }
    }

    void NavigateToTarget(Vector2 pos, bool isGrounded) {
        if (currentTarget == null) return;

        if (hasTargetPlatform && Time.time - targetLockStartTime < targetLockDuration) {
            moveDirection = targetPlatformDirection;

            if (needsToJump) {
                float distToWall = GetDistanceToWall(pos, moveDirection);

                if (distToWall >= minDistanceFromWall && Time.time - lastJumpTime > jumpCooldown) {
                    playerMovement.aiJumpRequest = true;
                    lastJumpTime = Time.time;
                    needsToJump = false;
                    Debug.Log("Jumping to platform!");
                }
                else if (distToWall < minDistanceFromWall) {
                    moveDirection = -targetPlatformDirection;
                }
            }
        }
        else {
            hasTargetPlatform = false;
            needsToJump = false;
            MakeDecisions(pos, isGrounded);
        }

        if (Time.time - targetStartTime > targetSwitchTime) {
            Debug.Log("Target timeout! Finding new target.");
            FindNextTarget();
        }
    }

    void MakeDecisions(Vector2 pos, bool isGrounded) {
        // FIRST: Update direction toward target if we have one
        if (currentTarget != null) {
            float dx = currentTarget.position.x - pos.x;
            float dy = currentTarget.position.y - pos.y;

            // Handle jump-up case
            if (enableJumpUpToKiwiAbove && dy > jumpUpVerticalMin && Mathf.Abs(dx) < jumpUpThreshold) {
                Debug.Log("Kiwi is directly above! Jumping up to collect it.");

                if (Time.time - lastJumpTime > jumpCooldown) {
                    playerMovement.aiJumpRequest = true;
                    lastJumpTime = Time.time;
                }

                moveDirection = 0f;
                return;
            }

            // Set direction toward target EARLY
            if (Mathf.Abs(dx) > 0.3f) {
                moveDirection = Mathf.Sign(dx);
            }
            if (enableDropDownToFinish && currentTarget == finishLine && dy < -dropDownThreshold) {
                Debug.Log($"Finish line is below! Walking toward it to drop down.");
                return;
            }

        }

        // NOW: Check obstacles with the CORRECT direction
        bool hasWallAhead = CheckForWall(pos, moveDirection);
        bool hasGroundAhead = CheckForGroundAhead(pos, moveDirection);
        bool hasPlatformAbove = CheckForPlatformAbove(pos, moveDirection);
        bool hasSpike = CheckForSpikeAhead(pos, moveDirection);


        bool shouldJump = false;

        if (hasSpike) {
            Debug.Log("Spike detected ahead! Looking for safe path...");

            if (hasPlatformAbove) {
                Debug.Log("Found platform above spikes! Climbing up.");
                hasTargetPlatform = true;
                targetPlatformDirection = moveDirection;
                targetLockStartTime = Time.time;
                needsToJump = true;
                return;
            }

            bool hasPlatformLeft = CheckForPlatformAbove(pos, -1f);
            bool hasPlatformRight = CheckForPlatformAbove(pos, 1f);

            if (hasPlatformLeft) {
                Debug.Log("Found platform on LEFT! Turning to climb.");
                moveDirection = -1f;
                hasTargetPlatform = true;
                targetPlatformDirection = moveDirection;
                targetLockStartTime = Time.time;
                needsToJump = true;
                return;
            }
            else if (hasPlatformRight) {
                Debug.Log("Found platform on RIGHT! Turning to climb.");
                moveDirection = 1f;
                hasTargetPlatform = true;
                targetPlatformDirection = moveDirection;
                targetLockStartTime = Time.time;
                needsToJump = true;
                return;
            }

            Debug.Log("No safe platform found! Attempting to jump over spike.");
            shouldJump = true;
        }
        else if (hasWallAhead) {
            if (hasPlatformAbove) {
                Debug.Log("Platform detected above wall! Locking target.");
                hasTargetPlatform = true;
                targetPlatformDirection = moveDirection;
                targetLockStartTime = Time.time;
                needsToJump = true;
            }
            else {
                bool hasPlatformOpposite = CheckForPlatformAbove(pos, -moveDirection);

                if (hasPlatformOpposite) {
                    Debug.Log("Platform detected opposite direction!");
                    moveDirection *= -1f;
                    hasTargetPlatform = true;
                    targetPlatformDirection = moveDirection;
                    targetLockStartTime = Time.time;
                    needsToJump = true;
                }
                else {
                    Debug.Log("No platforms found, turning around.");
                    moveDirection *= -1f;
                }
            }
        }
        else if (!hasGroundAhead) {
            
            if (enableDropDownToFinish && currentTarget == finishLine) {
                float dy = currentTarget.position.y - pos.y;
                if (dy < -dropDownThreshold) {
                    Debug.Log("Finish line is below! Walking off edge to drop down.");
                    shouldJump = false; 
                }
                else {
                    shouldJump = true; 
                }
            }
            else {
                shouldJump = true; 
            }
        }


        if (shouldJump && Time.time - lastJumpTime > jumpCooldown) {
            playerMovement.aiJumpRequest = true;
            lastJumpTime = Time.time;
            Debug.Log("Jumping over gap/spike!");
        }
    }

    void CheckIfStuck() {
        if (Time.time - lastStuckCheckTimestamp > stuckCheckTime) {
            float distMoved = Vector2.Distance(transform.position, lastStuckCheckPosition);

            if (distMoved < stuckDistance) {
                Debug.Log($"STUCK! Only moved {distMoved:F2}. Switching to next kiwi.");
                hasTargetPlatform = false;
                needsToJump = false;

                if (currentTarget != null) {
                    Debug.Log($"Skipping unreachable kiwi at {currentTarget.position}");
                }

                FindNextTarget();
            }

            lastStuckCheckPosition = transform.position;
            lastStuckCheckTimestamp = Time.time;
        }
    }

    bool CheckIfOnGround() {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    bool CheckForWall(Vector2 pos, float direction) {
        Vector2 origin = pos + Vector2.up * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, wallCheckDistance, wallLayer | groundLayer);
        return hit.collider != null;
    }

    float GetDistanceToWall(Vector2 pos, float direction) {
        Vector2 origin = pos + Vector2.up * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, wallCheckDistance * 2f, wallLayer | groundLayer);
        return hit.collider != null ? hit.distance : 999f;
    }

    bool CheckForGroundAhead(Vector2 pos, float direction) {
        Vector2 origin = pos + Vector2.right * direction * edgeCheckDistance;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 2.0f, groundLayer);
        return hit.collider != null;
    }

    bool CheckForPlatformAbove(Vector2 pos, float direction) {
        Vector2 checkOrigin = pos + Vector2.right * direction * platformCheckForwardDist;
        RaycastHit2D hitUp = Physics2D.Raycast(checkOrigin, Vector2.up, platformCheckUpwardDist, groundLayer);

        if (hitUp.collider != null) {
            float heightDiff = hitUp.point.y - pos.y;

            if (heightDiff > 1.0f && heightDiff < 6.0f) {
                return true;
            }
        }

        return false;
    }

    bool CheckForSpikeAhead(Vector2 pos, float direction) {
        for (float dist = 0.5f; dist <= spikeCheckDistance; dist += 0.5f) {
            Vector2 origin = pos + Vector2.right * direction * dist;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 2.0f, spikeLayer);

            if (hit.collider != null) {
                return true;
            }
        }

        return false;
    }
}
