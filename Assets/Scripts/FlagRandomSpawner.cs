using UnityEngine;

public class FlagRandomSpawner : MonoBehaviour {
    [Header("Spawn Points")]
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;

    private void Awake() {
        RandomizeSpawnPosition();
    }

    private void RandomizeSpawnPosition() {
        if (leftSpawnPoint == null || rightSpawnPoint == null) {
            Debug.LogError("FlagRandomSpawner: Spawn points are not assigned!");
            return;
        }

        int randomChoice = Random.Range(0, 2);

        if (randomChoice == 0) {
            transform.position = leftSpawnPoint.position;
        }
        else {
            transform.position = rightSpawnPoint.position;
        }
    }
}
