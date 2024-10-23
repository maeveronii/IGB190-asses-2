using UnityEngine;

[System.Serializable]
public class WallMovement
{
    public GameObject wall; // The wall GameObject to move
    public Vector3 moveDirection = Vector3.right; // Direction to move the wall
    public float moveSpeed = 5f; // Speed at which the wall will move
    public float moveDistance = 5f; // Distance to move the wall
    [HideInInspector] public Vector3 targetPosition; // The target position for the wall
}

public class TrapWallsTrigger : MonoBehaviour
{
    public WallMovement[] walls; // Array of walls with individual movement settings
    private bool shouldMove = false;

    private void Start()
    {
        // Calculate target positions based on the move distance
        foreach (WallMovement wallMovement in walls)
        {
            if (wallMovement.wall != null)
            {
                wallMovement.targetPosition = wallMovement.wall.transform.position + wallMovement.moveDirection.normalized * wallMovement.moveDistance;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Ensure the collider is the player
        {
            shouldMove = true; // Start moving the walls
            Debug.Log("Walls are moving now");
        }
    }

    private void Update()
    {
        if (shouldMove)
        {
            MoveWalls();
        }
    }

    private void MoveWalls()
    {
        foreach (WallMovement wallMovement in walls)
        {
            if (wallMovement.wall != null)
            {
                // Move the wall towards the target position
                wallMovement.wall.transform.position = Vector3.MoveTowards(wallMovement.wall.transform.position, wallMovement.targetPosition, wallMovement.moveSpeed * Time.deltaTime);
                Debug.Log("Walls are in the process of moving");

                // Check if the wall has reached the target position
                if (Vector3.Distance(wallMovement.wall.transform.position, wallMovement.targetPosition) < 0.01f)
                {
                    shouldMove = false; // Optionally stop moving after reaching the target
                    Debug.Log("Wall has reached its target");
                }
            }
        }
    }
}
