using UnityEngine;

/// <summary>
/// Logic for a Region in the game. When the player enters the region, an event
/// will be fired specifying the region that the player has entered.
/// 
/// Usage: Attach this component to any GameObject with a trigger collider. When the
/// player collides with the object, the trigger will be run. 
/// 
/// Make sure you specify the name of the region in the inspector first.
/// </summary>
public class Region : MonoBehaviour
{
    public string regionName;

    private void OnTriggerEnter(Collider other)
    {
        if (regionName == string.Empty)
        {
            Debug.Log($"The Region on {gameObject.name} has not been named. It needs a name to run!");
        }
        else if (other.gameObject == GameManager.player.gameObject)
        {
            GameManager.events.OnPlayerEnteredRegion.Invoke(GameManager.player, this, regionName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (regionName == string.Empty)
        {
            Debug.Log($"The Region on {gameObject.name} has not been named. It needs a name to run!");
        }
        else if (other.gameObject == GameManager.player.gameObject)
        {
            GameManager.events.OnPlayerExitedRegion.Invoke(GameManager.player, this, regionName);
        }
    }

    public static Region GetRegionWithName (string regionName)
    {
        Region[] regions = GameObject.FindObjectsOfType<Region>();
        foreach (Region region in regions)
        {
            if (region.regionName == regionName)
            {
                return region;
            }
        }
        return null;
    }
}
