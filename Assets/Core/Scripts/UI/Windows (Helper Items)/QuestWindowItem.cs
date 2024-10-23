using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestWindowItem : MonoBehaviour
{
    public TextMeshProUGUI questHeader;
    public TextMeshProUGUI[] questItemSlots;
    public TextMeshProUGUI questReward;

    public void Show (Quest quest)
    {
        gameObject.SetActive(true);
        questHeader.text = quest.Description;
        Quest.QuestItem[] questItems = quest.GetItems();
        for (int i = 0; i < questItemSlots.Length; i++)
        {
            if (i < questItems.Length)
            {
                questItemSlots[i].gameObject.SetActive(true);

                string progress = "";
                if (questItems[i].MaxProgress > 1)
                {
                    progress = $"[{questItems[i].CurrentProgress}/{questItems[i].MaxProgress}]";
                }
                questItemSlots[i].text = $" - {questItems[i].Description} {progress}";
            }
            else
            {
                questItemSlots[i].gameObject.SetActive(false);
            }
        }
        if (quest.Reward.Length > 0)
        {
            questReward.gameObject.SetActive(true);
            questReward.text = $" - <color=yellow>Reward</color>: {quest.Reward}";
        }
        else
        {
            questReward.gameObject.SetActive(false);
        }
    }

    public void Hide ()
    {
        gameObject.SetActive(false);
    }
}
