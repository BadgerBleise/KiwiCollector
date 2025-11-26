using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class AStarAgent : MonoBehaviour {
    [Header("References")]
    public PlatformerPathfinding pathfinding;
    public PlayerMovement playerMovement;
    public Transform groundCheck;
    public Rigidbody2D rb;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask spikeLayer;

    [Header("Pathfinding Settings")]
    public float pathUpdateInterval = 0.5f;
    public float waypointReachedDistance = 1.5f;
    public float collectionRadius = 1.0f;

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
    public float verticalJumpThreshold = 2.0f;

    [Header("Visualization")]
    public bool drawPath = true;
    public bool drawDebugInfo = false;

    private List<Transform> currentPath;
    private int currentWaypointIndex;
    private float lastPathUpdateTime;
    private float lastJumpTime = -999f;

    private Transform currentTarget;
    private Transform finishLine;
    private float moveDirection;

    private bool hasTargetPlatform = false;
    private float targetPlatformDirection = 0f;
    private float targetLockStartTime = -999f;
    private bool wasGrounded = true;
    private bool needsToJump = false;
    private float directionWhenJumped = 1f;

    void Reset() {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start() {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (groundCheck == null && playerMovement != null)
            groundCheck = playerMovement.groundCheck;

        if (pathfinding == null)
            pathfinding = FindObjectOfType<PlatformerPathfinding>();

        playerMovement.useAI = true;
        finishLine = GameObject.FindGameObjectWithTag("Finish")?.transform;

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

        if (currentTarget == null) {
            FindNextTarget();
            if (currentTarget == null) {
                playerMovement.aiMoveInput = 0f;
                return;
            }
        }

        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distToTarget < collectionRadius) {
            FindNextTarget();
            return;
        }

        if (Time.time - lastPathUpdateTime > pathUpdateInterval) {
            UpdatePath();
            lastPathUpdateTime = Time.time;
        }

        if (hasTargetPlatform && Time.time - targetLockStartTime < targetLockDuration) {
            moveDirection = targetPlatformDirection;

            if (needsToJump) {
                float distToWall = GetDistanceToWall(pos, moveDirection);

                if (distToWall >= minDistanceFromWall && Time.time - lastJumpTime > jumpCooldown) {
                    playerMovement.aiJumpRequest = true;
                    lastJumpTime = Time.time;
                    needsToJump = false;
                    if (drawDebugInfo)
                        Debug.Log("JUMPING to platform!");
                }
                else if (distToWall < minDistanceFromWall) {
                    moveDirection = -targetPlatformDirection;
                }
            }
        }
        else {
            hasTargetPlatform = false;
            needsToJump = false;
            NavigateToTarget(pos, isGrounded);
        }

        playerMovement.aiMoveInput = moveDirection;
        wasGrounded = isGrounded;
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
            if (drawDebugInfo)
                Debug.Log($"New target: {currentTarget.name} at distance {closestDistance:F2}");
        }
        else {
            currentTarget = finishLine;
            if (drawDebugInfo)
                Debug.Log("All kiwis collected! Going to finish.");
        }

        UpdatePath();
    }

    void UpdatePath() {
        if (pathfinding == null || currentTarget == null) {
            currentPath = null;
            if (drawDebugInfo)
                Debug.LogWarning("Pathfinding or target is null - using direct navigation");
            return;
        }

        currentPath = pathfinding.FindPath(transform.position, currentTarget.position);
        currentWaypointIndex = 0;

        if (currentPath == null || currentPath.Count == 0) {
            if (drawDebugInfo)
                Debug.LogWarning("No path found - using direct navigation");
        }
        else {
            if (drawDebugInfo)
                Debug.Log($"Path found with {currentPath.Count} waypoints");
        }
    }

    void NavigateToTarget(Vector2 pos, bool isGrounded) {
        if (currentPath != null && currentPath.Count > 0 && currentWaypointIndex < currentPath.Count) {
            NavigateAlongPath(pos, isGrounded);
        }
        else {
            NavigateDirectly(pos, isGrounded);
        }
    }

    void NavigateAlongPath(Vector2 pos, bool isGrounded) {
        Transform targetWaypoint = currentPath[currentWaypointIndex];

        if (targetWaypoint == null) {
            currentWaypointIndex++;
            return;
        }

        Vector2 targetPosition = targetWaypoint.position;
        float distanceToWaypoint = Vector2.Distance(pos, targetPosition);

        if (distanceToWaypoint < waypointReachedDistance) {
            if (drawDebugInfo)
                Debug.Log($"Reached waypoint {currentWaypointIndex}");

            currentWaypointIndex++;
            if (currentWaypointIndex >= currentPath.Count) {
                NavigateDirectly(pos, isGrounded);
                return;
            }

            targetWaypoint = currentPath[currentWaypointIndex];
            if (targetWaypoint != null) {
                targetPosition = targetWaypoint.position;
            }
        }

        float horizontalDiff = targetPosition.x - pos.x;
        float verticalDiff = targetPosition.y - pos.y;

        if (Mathf.Abs(horizontalDiff) > 0.1f) {
            moveDirection = Mathf.Sign(horizontalDiff);
        }
        else {
            moveDirection = 0f;
        }

        if (verticalDiff > verticalJumpThreshold && Mathf.Abs(horizontalDiff) < 2.0f) {
            if (drawDebugInfo)
                Debug.Log($"Waypoint is {verticalDiff:F2} units above - checking for wall to jump");

            CheckForVerticalJump(pos, verticalDiff, targetPosition);
        }

        HandleObstacles(pos, isGrounded);
    }

    void CheckForVerticalJump(Vector2 pos, float verticalDiff, Vector2 targetPosition) {
        bool hasWallLeft = CheckForWall(pos, -1f);
        bool hasWallRight = CheckForWall(pos, 1f);
        bool hasPlatformLeft = CheckForPlatformAbove(pos, -1f);
        bool hasPlatformRight = CheckForPlatformAbove(pos, 1f);

        if (hasWallRight && hasPlatformRight) {
            if (drawDebugInfo)
                Debug.Log("Found platform above to the RIGHT - locking");
            hasTargetPlatform = true;
            targetPlatformDirection = 1f;
            targetLockStartTime = Time.time;
            needsToJump = true;
            moveDirection = 1f;
        }
        else if (hasWallLeft && hasPlatformLeft) {
            if (drawDebugInfo)
                Debug.Log("Found platform above to the LEFT - locking");
            hasTargetPlatform = true;
            targetPlatformDirection = -1f;
            targetLockStartTime = Time.time;
            needsToJump = true;
            moveDirection = -1f;
        }
        else {
            float horizontalDiff = targetPosition.x - pos.x;

            if (Mathf.Abs(horizontalDiff) < 3f && verticalDiff > 2f) {
                bool hasSpikeInDirection = CheckForSpikeAhead(pos, Mathf.Sign(horizontalDiff));

                if (hasSpikeInDirection) {
                    if (drawDebugInfo)
                        Debug.Log("Spike detected ahead! Looking for safe platform to climb");

                    if (hasPlatformLeft && !CheckForSpikeAhead(pos, -1f)) {
                        if (drawDebugInfo)
                            Debug.Log("Found safe platform on LEFT - climbing");
                        hasTargetPlatform = true;
                        targetPlatformDirection = -1f;
                        targetLockStartTime = Time.time;
                        needsToJump = true;
                        moveDirection = -1f;
                        return;
                    }
                    else if (hasPlatformRight && !CheckForSpikeAhead(pos, 1f)) {
                        if (drawDebugInfo)
                            Debug.Log("Found safe platform on RIGHT - climbing");
                        hasTargetPlatform = true;
                        targetPlatformDirection = 1f;
                        targetLockStartTime = Time.time;
                        needsToJump = true;
                        moveDirection = 1f;
                        return;
                    }
                }

                if (drawDebugInfo)
                    Debug.Log("No wall found - attempting direct jump toward waypoint above");

                if (Mathf.Abs(horizontalDiff) > 0.5f) {
                    moveDirection = Mathf.Sign(horizontalDiff);
                }

                if (Mathf.Abs(horizontalDiff) < 2f && Time.time - lastJumpTime > jumpCooldown * 0.5f) {
                    if (!hasSpikeInDirection) {
                        playerMovement.aiJumpRequest = true;
                        lastJumpTime = Time.time;
                        if (drawDebugInfo)
                            Debug.Log("Jumping toward waypoint above!");
                    }
                    else if (drawDebugInfo) {
                        Debug.Log("Cannot jump - spike in path!");
                    }
                }
            }
        }
    }

    void NavigateDirectly(Vector2 pos, bool isGrounded) {
        if (currentTarget == null) {
            moveDirection = 0f;
            return;
        }

        float horizontalDiff = currentTarget.position.x - pos.x;

        if (Mathf.Abs(horizontalDiff) > 0.3f) {
            moveDirection = Mathf.Sign(horizontalDiff);
        }
        else {
            moveDirection = 0f;
        }

        HandleObstacles(pos, isGrounded);
    }

    void HandleObstacles(Vector2 pos, bool isGrounded) {
        if (Mathf.Approximately(moveDirection, 0f))
            return;

        bool hasWallAhead = CheckForWall(pos, moveDirection);
        bool hasGroundAhead = CheckForGroundAhead(pos, moveDirection);
        bool hasPlatformAbove = CheckForPlatformAbove(pos, moveDirection);
        bool hasSpike = CheckForSpikeAhead(pos, moveDirection);

        bool shouldJump = false;

        if (hasSpike) {
            if (drawDebugInfo)
                Debug.Log("Spike detected ahead! Looking for safe path...");

            if (hasPlatformAbove) {
                if (drawDebugInfo)
                    Debug.Log("Found platform above spikes! Climbing up.");
                hasTargetPlatform = true;
                targetPlatformDirection = moveDirection;
                targetLockStartTime = Time.time;
                needsToJump = true;
                return;
            }

            bool hasPlatformLeft = CheckForPlatformAbove(pos, -1f);
            bool hasPlatformRight = CheckForPlatformAbove(pos, 1f);
            bool hasSpikeLeft = CheckForSpikeAhead(pos, -1f);
            bool hasSpikeRight = CheckForSpikeAhead(pos, 1f);

            if (hasPlatformLeft && !hasSpikeLeft) {
                if (drawDebugInfo)
                    Debug.Log("Found safe platform on LEFT! Turning to climb.");
                moveDirection = -1f;
                hasTargetPlatform = true;
                targetPlatformDirection = moveDirection;
                targetLockStartTime = Time.time;
                needsToJump = true;
                return;
            }
            else if (hasPlatformRight && !hasSpikeRight) {
                if (drawDebugInfo)
                    Debug.Log("Found safe platform on RIGHT! Turning to climb.");
                moveDirection = 1f;
                hasTargetPlatform = true;
                targetPlatformDirection = moveDirection;
                targetLockStartTime = Time.time;
                needsToJump = true;
                return;
            }

            if (drawDebugInfo)
                Debug.Log("No safe platform found! Attempting to jump over spike.");
            shouldJump = true;
        }
        else if (hasWallAhead && hasPlatformAbove) {
            hasTargetPlatform = true;
            targetPlatformDirection = moveDirection;
            targetLockStartTime = Time.time;
            needsToJump = true;
            if (drawDebugInfo)
                Debug.Log("Wall with platform above detected - locking target");
        }
        else if (hasWallAhead && !hasPlatformAbove) {
            bool hasPlatformOpposite = CheckForPlatformAbove(pos, -moveDirection);

            if (hasPlatformOpposite) {
                moveDirection *= -1f;
                hasTargetPlatform = true;
                targetPlatformDirection = moveDirection;
                targetLockStartTime = Time.time;
                needsToJump = true;
                if (drawDebugInfo)
                    Debug.Log("Platform in opposite direction - turning around");
            }
        }
        else if (!hasGroundAhead && !hasSpike) {
            shouldJump = true;
        }

        if (shouldJump && Time.time - lastJumpTime > jumpCooldown) {
            playerMovement.aiJumpRequest = true;
            lastJumpTime = Time.time;
            if (drawDebugInfo)
                Debug.Log("Jumping over gap/spike");
        }
    }

    bool CheckIfOnGround() {
        if (groundCheck == null)
            return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    bool CheckForWall(Vector2 pos, float direction) {
        if (Mathf.Approximately(direction, 0f))
            return false;

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
        if (Mathf.Approximately(direction, 0f))
            return true;

        Vector2 origin = pos + Vector2.right * direction * edgeCheckDistance;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 2.0f, groundLayer);
        return hit.collider != null;
    }

    bool CheckForPlatformAbove(Vector2 pos, float direction) {
        if (Mathf.Approximately(direction, 0f))
            return false;

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
        if (Mathf.Approximately(direction, 0f))
            return false;

        for (float dist = 0.5f; dist <= spikeCheckDistance; dist += 0.5f) {
            Vector2 origin = pos + Vector2.right * direction * dist;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 2.0f, spikeLayer);

            if (hit.collider != null) {
                if (drawDebugInfo)
                    Debug.Log($"Spike detected at distance {dist:F1} in direction {direction}");
                return true;
            }
        }

        return false;
    }

    void OnDrawGizmos() {
        if (!drawPath || currentPath == null || currentPath.Count == 0)
            return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < currentPath.Count; i++) {
            if (currentPath[i] == null)
                continue;

            Gizmos.DrawWireSphere(currentPath[i].position, 0.5f);

            if (i < currentPath.Count - 1 && currentPath[i + 1] != null) {
                Gizmos.DrawLine(currentPath[i].position, currentPath[i + 1].position);
            }
        }

        if (currentWaypointIndex < currentPath.Count && currentPath[currentWaypointIndex] != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentPath[currentWaypointIndex].position, waypointReachedDistance);
        }

        if (currentTarget != null) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
