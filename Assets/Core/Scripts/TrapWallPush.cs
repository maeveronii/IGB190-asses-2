using UnityEngine;
using System.Collections.Generic; // For using HashSet

public class TrapWallPush : MonoBehaviour
{
    public float pushDistance = 1f; // Distance to push the player away from the wall

    public Player player;

    // To track walls the player is touching
    private HashSet<Collider> wallsTouchingPlayer = new HashSet<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Calculate the direction to push the player away from the wall
            Vector3 pushDirection = (other.transform.position - transform.position).normalized;

            // Calculate the new position for the player
            Vector3 newPosition = other.transform.position + pushDirection * pushDistance;

            // Move the player to the new position
            other.transform.position = newPosition;
        }
    }
}
