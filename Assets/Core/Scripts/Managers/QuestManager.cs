using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the active and completed quests in the game.
/// </summary>
public class QuestManager
{
    public readonly Dictionary<string, Quest> activeQuests = new Dictionary<string, Quest>();
    public readonly HashSet<string> questsCompleted = new HashSet<string>();

    /// <summary>
    /// Adds a new quest to the active quests list and triggers the associated events and feedback.
    /// </summary>
    public void AddQuest(Quest quest)
    {
        activeQuests.Add(quest.Label, quest);
        GameManager.events.OnQuestAdded.Invoke(quest);
        GameManager.assets.questReceivedFeedback.ActivateFeedback();
    }

    /// <summary>
    /// Removes a quest from the active quests list, marks it as completed, and triggers the associated feedback.
    /// </summary>
    public void RemoveQuest(Quest quest)
    {
        if (activeQuests.Remove(quest.Label))
        {
            questsCompleted.Add(quest.Label);
            GameManager.assets.questCompletedFeedback.ActivateFeedback();
        }
    }
}