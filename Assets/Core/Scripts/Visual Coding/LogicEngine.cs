using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class LogicEngine
{
    // Item Presets
    public static string PRESET_TRIGGERING_ITEM = "Triggering Item";
    //public static string PRESET_THIS_ITEM = "This Item";
    public static string[] ITEM_PRESETS = new string[]
    {
        PRESET_TRIGGERING_ITEM,
        //PRESET_THIS_ITEM
    };

    // Unit Presets
    public static string PRESET_TRIGGERING_UNIT = "Triggering Unit";
    public static string PRESET_CASTING_UNIT = "Casting Unit";
    public static string PRESET_TARGET_UNIT = "Target Unit";
    public static string PRESET_DAMAGING_UNIT = "Damaging Unit";
    public static string PRESET_DAMAGED_UNIT = "Damaged Unit";
    public static string PRESET_KILLING_UNIT = "Killing Unit";
    public static string PRESET_KILLED_UNIT = "Killed Unit";
    public static string PRESET_HEALING_UNIT = "Healing Unit";
    public static string PRESET_HEALED_UNIT = "Healed Unit";
    public static string PRESET_COLLIDING_UNIT = "Colliding Unit";
    //public static string PRESET_ABILITY_OWNER = "Ability Owner";
    public static string PRESET_GOAL_UNIT = "Goal Unit";
    public static string[] UNIT_PRESETS = new string[]
    {
        PRESET_TRIGGERING_UNIT,
        PRESET_CASTING_UNIT,
        PRESET_TARGET_UNIT,
        PRESET_DAMAGING_UNIT,
        PRESET_DAMAGED_UNIT,
        PRESET_KILLING_UNIT,
        PRESET_KILLED_UNIT,
        PRESET_HEALING_UNIT,
        PRESET_HEALED_UNIT,
        PRESET_COLLIDING_UNIT,
        PRESET_GOAL_UNIT
    };

    // Ability Presets
    public static string PRESET_KILLING_ABILITY = "Killing Ability";
    public static string PRESET_DAMAGING_ABILITY = "Damaging Ability";
    public static string PRESET_HEALING_ABILITY = "Healing Ability";
    public static string PRESET_ABILITY_CAST = "Ability Cast";
    //public static string PRESET_THIS_ABILITY = "This Ability";
    public static string[] ABILITY_PRESETS = new string[]
    {
        PRESET_KILLING_ABILITY,
        PRESET_DAMAGING_ABILITY,
        PRESET_HEALING_ABILITY,
        PRESET_ABILITY_CAST,
        //PRESET_THIS_ABILITY
    };

    // Vector Presets
    public static string PRESET_TARGET_POSITION = "Ability Target Location";
    public static string PRESET_GOAL_POSITION = "Goal Position";
    public static string[] VECTOR_PRESETS = new string[]
    {
        PRESET_TARGET_POSITION,
        PRESET_GOAL_POSITION
    };

    // Number Presets
    public const string PRESET_GOLD_PICKED_UP = "Gold Added";
    public const string PRESET_HEALTH_PICKED_UP = "Health Restored";
    public const string PRESET_RESOURCES_GAINED = "Resources Gained";
    public const string PRESET_RESOURCES_LOST = "Resources Lost";
    public const string PRESET_DAMAGE_DEALT = "Damage Dealt";
    public static string[] NUMBER_PRESETS = new string[]
    {
        PRESET_GOLD_PICKED_UP,
        PRESET_HEALTH_PICKED_UP,
        PRESET_RESOURCES_GAINED,
        PRESET_RESOURCES_LOST,
        PRESET_DAMAGE_DEALT
    };

    // Projectile Presets
    //public static string PRESET_LAST_CREATED_PROJECTILE = "Last Created Projectile";
    public static string PRESET_EVENT_PROJECTILE = "Event Projectile";
    public static string[] PROJECTILE_PRESETS = new string[]
    {
        //PRESET_LAST_CREATED_PROJECTILE,
        PRESET_EVENT_PROJECTILE
    };

    // Bool Presets
    public const string PRESET_IS_CRITICAL = "Is Critical";
    public static string[] BOOL_PRESETS = new string[] 
    {
        PRESET_IS_CRITICAL
    };

    // Dynamic Presets
    public const string PRESET_UNIT_PLAYER = "Player";
    public const string PRESET_UNIT_LAST_CREATED = "Last Spawned Unit";
    public const string PRESET_PROJECTILE_LAST_CREATED = "Last Created Projectile";
    public const string PRESET_ABILITY_OWNER = "Ability Owner";
    public const string PRESET_ITEM_OWNER = "Item Owner";
    public const string PRESET_ABILITY_THIS = "This Ability";
    public const string PRESET_ITEM_THIS = "This Item";
    public const string PRESET_TIME_SINCE_START = "Time Since Level Start";
    public const string PRESET_PLAYER_LEVEL = "Player Level";

    public static Dictionary<string, string> DYNAMIC_PRESETS = new Dictionary<string, string>()
    {
        { PRESET_UNIT_PLAYER, "GetPlayer" },
        { PRESET_UNIT_LAST_CREATED, "GetLastCreatedUnit" },
        { PRESET_PROJECTILE_LAST_CREATED, "LastCreatedProjectile" },
        { PRESET_ABILITY_OWNER, "GetOwner" },
        { PRESET_ITEM_OWNER, "GetOwner" },
        { PRESET_ABILITY_THIS, "ThisAbility" },
        { PRESET_ITEM_THIS, "ThisItem" }
    };


    // EVENTS
    public const string EVENT_PROJECTILE_OWNED_COLLIDES_WITH_UNIT = "ProjectileMadeByThisCollidesWithUnit";
    public const string EVENT_PROJECTILE_COLLIDES_WITH_UNIT = "ProjectileCollidesWithUnit";
    public const string EVENT_PROJECTILE_REACHES_GOAL = "ProjectileReachesGoal";
    public const string EVENT_PROJECTILE_COLLIDES_WITH_TERRAIN = "ProjectileCollidesWithTerrain";
    public const string EVENT_PROJECTILE_TIMES_OUT = "ProjectileTimesOut";

    public const string EVENT_ON_PICKUP_GOLD = "OnPickupGold";
    public const string EVENT_ON_PICKUP_HEALTH = "OnPickupHealth";
    //public const string EVENT_ON_PICKUP_ITEM = "OnPickupItem";

    public const string EVENT_SCRIPT_LOADED = "ScriptLoaded";
    public const string EVENT_SCRIPT_UNLOADED = "ScriptUnloaded";

    public const string EVENT_REGION_ENTER = "UnitEntersRegion";
    public const string EVENT_REGION_EXIT = "UnitExitsRegion";

    public const string EVENT_TIMER_ONE_OFF_FINISHED = "OnOneOffTimerFinished";
    public const string EVENT_TIMER_CONTINUOUS_FINISHED = "OnTimerFinished";


    public const string EVENT_ON_QUEST_COMPLETED = "OnQuestCompleted";
    public const string EVENT_ON_QUEST_RECEIVED = "OnQuestReceived";

    public const string EVENT_ITEM_PICKED_UP = "OnItemPickedUp";
    public const string EVENT_ITEM_SOLD = "OnItemSold";
    public const string EVENT_ITEM_BOUGHT = "OnItemBought";
    public const string EVENT_ITEM_EQUIPPED = "OnItemEquipped";
    public const string EVENT_ITEM_UNEQUIPPED = "OnItemUnequipped";

    public const string EVENT_PLAYER_LEVEL_UP = "OnPlayerLevelUp";
    public const string EVENT_UNIT_DAMAGED = "OnUnitDamaged";

    public const string EVENT_KEY_DOWN = "OnKeyDown";
    public const string EVENT_KEY_UP = "OnKeyUp";
    public const string EVENT_KEY_HELD = "OnKeyHeld";

    



    // Scripts.
    public List<LogicScript> scripts = new List<LogicScript>();

    // Variables.
    public Dictionary<string, object> localVariables = new Dictionary<string, object>();
    public static Dictionary<string, object> globalVariables = new Dictionary<string, object>();

    public Dictionary<LogicScript, float> disabledScripts = new Dictionary<LogicScript, float>();


    [System.NonSerialized] public LogicScript selectedScript = null;
    [System.NonSerialized] public List<GeneralNode> selectedNodes = new List<GeneralNode>();
    [System.NonSerialized] public List<LogicEngineTimer> activeTimers = new List<LogicEngineTimer>();
    public static LogicEngine current;
    public static LogicScript currentScript;
    public static GeneralNode currentNode;
    public static int currentLine;
    public static string currentType;
    public static Dictionary<string, object> currentPresets;


    public static bool pausedExecution;

    public IEngineHandler engineHandler;

    /// <summary>
    /// Create a shallow copy of the engine, with the same script objects.
    /// This is helpful for creating unique engines with the same logic (e.g.
    /// during gameplay).
    /// </summary>
    public LogicEngine ShallowCopy (IEngineHandler engineHandler)
    {
        LogicEngine engine = new LogicEngine();
        engine.engineHandler = engineHandler;
        engine.scripts = scripts;
        return engine;
    }

    /// <summary>
    /// Create a deep copy of the engine, with deep copies of the same scripts,
    /// so they can be modified without affecting the original in any way.
    /// </summary>
    /// <returns></returns>
    public LogicEngine Copy ()
    {
        LogicEngine engine = new LogicEngine();
        foreach (LogicScript script in scripts)
            engine.scripts.Add(script.Copy());
        return engine;
    }

    /// <summary>
    /// Trigger the specified event with the given presets.
    /// </summary>
    public void TriggerEvent(Dictionary<string, object> presets, string eventName)
    {
        if (presets == null) presets = new Dictionary<string, object>();
        foreach (LogicScript script in scripts)
        {
            script.RunScript(presets, this, eventName);
        }
    }

    /// <summary>
    /// Setup the engine, performing any initial setup actions and creating
    /// all required timers.
    /// </summary>
    public void Setup ()
    {
        foreach (LogicScript script in scripts)
        {
            foreach (GeneralNode node in script.eventNodes)
            {
                if (node.functionName == "OnTimerFinished")
                {
                    activeTimers.Add(new LogicEngineTimer(script, node, false));
                }
                else if (node.functionName == "OnOneOffTimerFinished")
                {
                    activeTimers.Add(new LogicEngineTimer(script, node, true));
                }
            }
        }
    }

    public void DisableTimers ()
    {
        activeTimers = new List<LogicEngineTimer>();
    }

    public Unit GetOwner ()
    {
        return engineHandler.GetOwner();
    }
} 
 