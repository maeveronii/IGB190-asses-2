using System.Collections.Generic;
using UnityEngine;

public class LogicEngineManager : MonoBehaviour
{
    public bool showErrors = true;
    public bool stopScriptExecutionOnError = false;
    private List<LogicEngine> engines = new List<LogicEngine>();
    private Dictionary<string, List<LogicEngine>> eventLookup = new Dictionary<string, List<LogicEngine>>();

    /// <summary>
    /// 
    /// </summary>
    private void Start()
    {
        Setup();
    }

    /// <summary>
    /// 
    /// </summary>
    void Update()
    {
        foreach (LogicEngine engine in engines)
            foreach (LogicEngineTimer timer in engine.activeTimers)
                timer.Update(engine);

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key))
            {
                TriggerEventOnAllEngines(null, LogicEngine.EVENT_KEY_DOWN, key.ToString());
            }
            if (Input.GetKeyUp(key))
            {
                TriggerEventOnAllEngines(null, LogicEngine.EVENT_KEY_UP, key.ToString());
            }
            if (Input.GetKey(key))
            {
                TriggerEventOnAllEngines(null, LogicEngine.EVENT_KEY_HELD, key.ToString());
            }
        }

        TriggerEventOnAllEngines(null, "EveryFrame");
    }

    /// <summary>
    /// Add this engine so that it is handled by the manager.
    /// </summary>
    public void AddEngine (LogicEngine engine)
    {
        engines.Add(engine);
        engine.Setup();

        // Cache event lookups for faster access later.
        foreach (LogicScript script in engine.scripts)
        {
            foreach (GeneralNode node in script.eventNodes)
            {
                if (!eventLookup.ContainsKey(node.functionName))
                {
                    eventLookup.Add(node.functionName, new List<LogicEngine>());
                    
                }
                eventLookup[node.functionName].Add(engine);
            }
        }
        engine.TriggerEvent(null, LogicEngine.EVENT_SCRIPT_LOADED);
    }

    /// <summary>
    /// No longer manage the specified engine.
    /// </summary>
    public void RemoveEngine (LogicEngine engine)
    {
        engine.DisableTimers();
        engines.Remove(engine);
        foreach (List<LogicEngine> list in eventLookup.Values)
            list.RemoveAll(x => x.Equals(engine));
        engine.TriggerEvent(null, LogicEngine.EVENT_SCRIPT_UNLOADED);
    }

    /// <summary>
    /// Trigger the specified event on ALL listening engines (with any appropriate presets).
    /// </summary>
    public void TriggerEventOnAllEngines (Dictionary<string, object> presets, string eventCall, object reqs = null)
    {
        if (!eventLookup.ContainsKey(eventCall)) return;
        if (presets == null) presets = new Dictionary<string, object>();

        List<LogicEngine> copy = new List<LogicEngine>(engines);
        foreach (LogicEngine engine in copy)
        {
            foreach (LogicScript script in engine.scripts)
            {
                script.RunScript(presets, engine, eventCall, reqs);
            }
        }
    }

    /// <summary>
    /// Trigger the specified event on the given engine (with any appropriate presets).
    /// </summary>
    public void TriggerEventOnEngine (LogicEngine engine, Dictionary<string, object> presets, string eventCall)
    {
        if (engine == null) return;
        if (presets == null) presets = new Dictionary<string, object>();
        foreach (LogicScript script in engine.scripts)
        {
            script.RunScript(presets, engine, eventCall);
        }
    }

    /// <summary>
    /// Perform all basic setup for the engine manager.
    /// </summary>
    private void Setup ()
    {
        GameManager.events.OnAbilityCastStarted.AddListener((arg1, arg2, arg3, arg4) => {
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_CASTING_UNIT, arg1 },
                { LogicEngine.PRESET_ABILITY_CAST, arg2 },
                { LogicEngine.PRESET_TARGET_UNIT, arg3 },
                { LogicEngine.PRESET_TARGET_POSITION, arg4 }
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, "UnitStartCast");
        });

        GameManager.events.OnAbilityCastFinished.AddListener((arg1, arg2, arg3, arg4) => {
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_CASTING_UNIT, arg1 },
                { LogicEngine.PRESET_ABILITY_CAST, arg2 },
                { LogicEngine.PRESET_TARGET_UNIT, arg3 },
                { LogicEngine.PRESET_TARGET_POSITION, arg4 }
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, "UnitFinishCast");
        });

        GameManager.events.OnUnitKilled.AddListener((arg1, arg2, arg3, arg4) => {
            Object data = arg3 == null ? null : arg3.GetData();
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_KILLED_UNIT, arg1 },
                { LogicEngine.PRESET_KILLING_UNIT, arg2 },
                { LogicEngine.PRESET_KILLING_ABILITY, (data is Ability ? data : null) },
                { LogicEngine.PRESET_IS_CRITICAL, arg4 },
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, "WhenUnitIsKilled");
        });

        GameManager.events.OnUnitDamaged.AddListener((args) => {
            Object data = args.damageSource == null ? null : args.damageSource.GetData();
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_DAMAGED_UNIT, args.damagedUnit },
                { LogicEngine.PRESET_DAMAGING_UNIT, args.damagingUnit },
                { LogicEngine.PRESET_DAMAGING_ABILITY, (data is Ability ? data : null) },
                { LogicEngine.PRESET_DAMAGE_DEALT, args.damage },
                { LogicEngine.PRESET_IS_CRITICAL, args.isCritical }, 
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, LogicEngine.EVENT_UNIT_DAMAGED);
        });

        GameManager.events.OnPlayerEnteredRegion.AddListener((player, region, regionName) =>
        {
            GameManager.logicEngine.TriggerEventOnAllEngines(null, LogicEngine.EVENT_REGION_ENTER, regionName);
        });

        GameManager.events.OnPlayerExitedRegion.AddListener((player, region, regionName) =>
        {
            GameManager.logicEngine.TriggerEventOnAllEngines(null, LogicEngine.EVENT_REGION_EXIT, regionName);
        });

        GameManager.events.OnQuestCompleted.AddListener(quest => 
        {
            GameManager.logicEngine.TriggerEventOnAllEngines(null, LogicEngine.EVENT_ON_QUEST_COMPLETED, quest.Label);
        });

        GameManager.events.OnQuestAdded.AddListener(quest =>
        {
            GameManager.logicEngine.TriggerEventOnAllEngines(null, LogicEngine.EVENT_ON_QUEST_RECEIVED, quest.Label);
        });

        GameManager.events.OnItemPickedUp.AddListener(item => 
        {
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_TRIGGERING_ITEM, item }
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, LogicEngine.EVENT_ITEM_PICKED_UP);
        });

        GameManager.events.OnItemSold.AddListener(item =>
        {
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_TRIGGERING_ITEM, item }
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, LogicEngine.EVENT_ITEM_SOLD);
        });


        GameManager.events.OnGoldPickedUp.AddListener(gold =>
        {
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_GOLD_PICKED_UP, gold }
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, LogicEngine.EVENT_ON_PICKUP_GOLD);
        });

        GameManager.events.OnHealthPickedUp.AddListener(health =>
        {
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_HEALTH_PICKED_UP, health }
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, LogicEngine.EVENT_ON_PICKUP_HEALTH);
        });


        GameManager.events.OnItemEquipped.AddListener(item =>
        {
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_TRIGGERING_ITEM, item }
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, LogicEngine.EVENT_ITEM_EQUIPPED);
        });

        GameManager.events.OnItemUnequipped.AddListener(item =>
        {
            Dictionary<string, object> presets = new Dictionary<string, object>
            {
                { LogicEngine.PRESET_TRIGGERING_ITEM, item }
            };
            GameManager.logicEngine.TriggerEventOnAllEngines(presets, LogicEngine.EVENT_ITEM_UNEQUIPPED);
        });

        GameManager.events.OnPlayerLevelUp.AddListener(item =>
        {
            GameManager.logicEngine.TriggerEventOnAllEngines(null, LogicEngine.EVENT_PLAYER_LEVEL_UP);
        });


        LogicContainer[] generalScripts = Resources.LoadAll<LogicContainer>("General Scripts");
        foreach (LogicContainer generalScript in generalScripts)
            AddEngine(generalScript.engine);
    }

    private void OnDestroy()
    {
        LogicContainer[] generalScripts = Resources.LoadAll<LogicContainer>("General Scripts");
        foreach (LogicContainer generalScript in generalScripts)
            RemoveEngine(generalScript.engine);
    }
}
