using TMPro;
using UnityEngine;

public class DeathWindow : UIWindow, IPausing
{
    [SerializeField] private TextMeshProUGUI causeOfDeathText;

    public override void Setup()
    {
        base.Setup();
        GameManager.events.OnPlayerKilled.AddListener(OnPlayerKilled);
    }

    /// <summary>
    /// Handles the player being killed by updating the death message.
    /// </summary>
    private void OnPlayerKilled(Player player, Unit unit)
    {
        // Update the cause of death text based on whether a unit caused the death
        causeOfDeathText.text = unit != null
            ? $"You were slain by a {unit.unitName}"
            : "You were slain";

        // Show the death window after a delay
        Invoke(nameof(Show), 2.0f);
    }

    /// <summary>
    /// Revives the player and hides the death window.
    /// </summary>
    public void RevivePlayer()
    {
        GameManager.player.Revive();
        GameManager.player.stats.Get(Stat.DamageTaken).AddTimedPercentageModifier(0, 2); // Temporary invincibility
        Hide();
    }
}