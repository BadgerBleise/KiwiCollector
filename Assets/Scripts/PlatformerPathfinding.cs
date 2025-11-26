using System.Collections.Generic;
using UnityEngine;

public class PlatformerPathfinding : MonoBehaviour {
    [Header("Waypoint Settings")]
    public List<Transform> waypoints = new List<Transform>();
    public float maxJumpDistance = 5f;
    public float maxJumpHeight = 4f;
    public LayerMask obstacleLayer;

    [Header("Visualization")]
    public bool showConnections = true;
    public Color connectionColor = Color.cyan;
    public Color pathColor = Color.green;

    private Dictionary<Transform, List<Transform>> navigationGraph;
    private List<Transform> currentPath;

    void Start() {
        BuildNavigationGraph();
    }

    void BuildNavigationGraph() {
        navigationGraph = new Dictionary<Transform, List<Transform>>();

        foreach (Transform waypoint in waypoints) {
            navigationGraph[waypoint] = new List<Transform>();

            foreach (Transform otherWaypoint in waypoints) {
                if (waypoint == otherWaypoint)
                    continue;

                if (IsReachable(waypoint.position, otherWaypoint.position)) {
                    navigationGraph[waypoint].Add(otherWaypoint);
                }
            }
        }
    }

    bool IsReachable(Vector2 from, Vector2 to) {
        float distance = Vector2.Distance(from, to);
        float heightDiff = to.y - from.y;

        if (distance > maxJumpDistance)
            return false;

        if (heightDiff > maxJumpHeight)
            return false;

        RaycastHit2D hit = Physics2D.Linecast(from, to, obstacleLayer);
        if (hit.collider != null)
            return false;

        return true;
    }

    public List<Transform> FindPath(Vector2 startPos, Vector2 targetPos) {
        Transform startWaypoint = GetNearestWaypoint(startPos);
        Transform targetWaypoint = GetNearestWaypoint(targetPos);

        if (startWaypoint == null || targetWaypoint == null)
            return null;

        if (startWaypoint == targetWaypoint)
            return new List<Transform> { targetWaypoint };

        return AStarSearch(startWaypoint, targetWaypoint);
    }

    Transform GetNearestWaypoint(Vector2 position) {
        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Transform waypoint in waypoints) {
            float distance = Vector2.Distance(position, waypoint.position);
            if (distance < nearestDistance) {
                nearestDistance = distance;
                nearest = waypoint;
            }
        }

        return nearest;
    }

    List<Transform> AStarSearch(Transform start, Transform goal) {
        Dictionary<Transform, Transform> cameFrom = new Dictionary<Transform, Transform>();
        Dictionary<Transform, float> gScore = new Dictionary<Transform, float>();
        Dictionary<Transform, float> fScore = new Dictionary<Transform, float>();

        List<Transform> openSet = new List<Transform> { start };
        HashSet<Transform> closedSet = new HashSet<Transform>();

        gScore[start] = 0;
        fScore[start] = Heuristic(start.position, goal.position);

        while (openSet.Count > 0) {
            Transform current = GetLowestFScore(openSet, fScore);

            if (current == goal) {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (!navigationGraph.ContainsKey(current))
                continue;

            foreach (Transform neighbor in navigationGraph[current]) {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGScore = gScore[current] + Vector2.Distance(current.position, neighbor.position);

                if (!openSet.Contains(neighbor)) {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= gScore[neighbor]) {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor.position, goal.position);
            }
        }

        return null;
    }

    Transform GetLowestFScore(List<Transform> openSet, Dictionary<Transform, float> fScore) {
        Transform lowest = openSet[0];
        float lowestScore = fScore.ContainsKey(lowest) ? fScore[lowest] : Mathf.Infinity;

        foreach (Transform node in openSet) {
            float score = fScore.ContainsKey(node) ? fScore[node] : Mathf.Infinity;
            if (score < lowestScore) {
                lowestScore = score;
                lowest = node;
            }
        }

        return lowest;
    }

    float Heuristic(Vector2 a, Vector2 b) {
        return Vector2.Distance(a, b);
    }

    List<Transform> ReconstructPath(Dictionary<Transform, Transform> cameFrom, Transform current) {
        List<Transform> path = new List<Transform> { current };

        while (cameFrom.ContainsKey(current)) {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        currentPath = path;
        return path;
    }

    void OnDrawGizmos() {
        if (waypoints == null || waypoints.Count == 0)
            return;

        foreach (Transform waypoint in waypoints) {
            if (waypoint == null)
                continue;

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(waypoint.position, 0.3f);
        }

        if (showConnections && navigationGraph != null) {
            Gizmos.color = connectionColor;
            foreach (var kvp in navigationGraph) {
                if (kvp.Key == null)
                    continue;

                foreach (Transform connected in kvp.Value) {
                    if (connected == null)
                        continue;

                    Gizmos.DrawLine(kvp.Key.position, connected.position);
                }
            }
        }

        if (currentPath != null && currentPath.Count > 0) {
            Gizmos.color = pathColor;
            for (int i = 0; i < currentPath.Count - 1; i++) {
                if (currentPath[i] != null && currentPath[i + 1] != null) {
                    Gizmos.DrawLine(currentPath[i].position, currentPath[i + 1].position);
                }
            }
        }
    }
}
