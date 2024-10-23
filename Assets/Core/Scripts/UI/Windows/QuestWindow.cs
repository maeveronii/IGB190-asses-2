using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the display of quests in the quest window.
/// </summary>
public class QuestWindow : UIWindow
{
    [SerializeField] private GameObject header;
    [SerializeField] private QuestWindowItem[] questWindowItems;

    /// <summary>
    /// Sets up the quest window, subscribing to relevant events.
    /// </summary>
    public override void Setup()
    {
        base.Setup();
        SubscribeToEvents();
    }

    /// <summary>
    /// Subscribes to quest-related events to refresh the display when quests are updated.
    /// </summary>
    private void SubscribeToEvents()
    {
        GameManager.events.OnQuestAdded.AddListener(RefreshDisplay);
        GameManager.events.OnQuestCompleted.AddListener(RefreshDisplay);
        GameManager.events.OnQuestUpdated.AddListener(RefreshDisplay);
    }

    /// <summary>
    /// Refreshes the quest display when quests are added, completed, or updated.
    /// </summary>
    private void RefreshDisplay(Quest quest)
    {
        Quest[] activeQuests = GameManager.quests.activeQuests.Values.ToArray();
        header.SetActive(activeQuests.Length > 0);

        for (int i = 0; i < questWindowItems.Length; i++)
        {
            if (i < activeQuests.Length)
            {
                questWindowItems[i].Show(activeQuests[i]);
            }
            else
            {
                questWindowItems[i].Hide();
            }
        }

        // Reset the window to ensure the display is updated correctly
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}