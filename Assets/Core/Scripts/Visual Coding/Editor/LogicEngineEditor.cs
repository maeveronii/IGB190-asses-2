using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static Codice.CM.Common.CmCallContext;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;
using Random = UnityEngine.Random;

public class LogicEngineEditor
{
    public static List<GeneralNode> allNodes = new List<GeneralNode>();

    public static GUIStyle unselectedScriptStyle;
    public static GUIStyle unselectedScriptStyle2;
    public static GUIStyle selectedScriptStyle;

    public static GUIStyle windowStyle_SmallText;
    public static GUIStyle windowStyle_SmallCenteredText;
    public static GUIStyle windowStyle_HeaderText;
    public static GUIStyle windowStyle_BaseText;
    public static GUIStyle windowStyle_BodyText;
    public static GUIStyle windowStyle_TempText;
    public static GUIStyle windowStyle_ValueText;
    public static GUIStyle windowStyle_PresetText;
    public static GUIStyle windowStyle_VariableText;

    public static GUIStyle windowStyle_OddNode;
    public static GUIStyle windowStyle_EvenNode;
    public static GUIStyle windowStyle_HoveredNode;

    public static Color valueColor = new Color(0.5f, 1.0f, 0.5f);
    public static Color tempColor = new Color(1.0f, 0.5f, 0.5f);
    public static Color varColor = new Color(0.75f, 0.75f, 1.0f);
    public static Color presetColor = Color.yellow;


    public static GUIStyle windowStyle_AddButton;
    public static GUIStyle windowStyle_AddButtonSmall;
    public static GUIStyle windowStyle_TextField;

    public static float indentWidth = 30;

    private static string[] increaseDecrease = new string[] { "Increase", "Decrease" };
    private static string[] boolComparators = new string[] { "Equal To", "Not Equal To" };
    private static string[] numberComparators = new string[] { "Equal To", "Not Equal To", "Less Than",
            "Less Than or Equal To", "Greater Than", "Greater Than or Equal To" };


    public static Color hoveredNodeColor = new Color(0.35f, 0.35f, 0.35f);
    public static Color oddNodeColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);
    public static Color evenNodeColor = new Color(0.15f, 0.15f, 0.15f, 1.0f);

    public const float itemHeight = 50;
    public const float heightIncreasePerDepth = 12;

    public IEngineHandler engineHandler;
    public LogicEngine engine;
    public EditorWindow window;

    public GeneralNode hoveredNode;
    public int hoveredNodeDepth;

    public static bool isDragging = false;
    public static GeneralNode dragStartedAt = null;

    public static bool isDraggingScript = false;
    public static LogicScript scriptBeingDragged = null;

    public static Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();

    public static List<GeneralNode> nodesInClipboard = new List<GeneralNode>();

    public const string unitIcon = "Unit";
    public const string projectileIcon = "Projectile";
    public const string effectIcon = "Effect";
    public const string questIcon = "QuestNew";
    public const string soundIcon = "Audio";
    public const string uiIcon = "UI"; 
    public const string variableIcon = "Variable";
    public const string conditionIcon = "Condition";
    public const string eventIcon = "Event";
    public const string loopIcon = "Loop";
    public const string timerIcon = "Timer";
    public const string waitIcon = "Timer"; 
    public const string gameIcon = "Game";
    public const string regionIcon = "Region";
    public const string cancelIcon = "Cancel";
    public const string pickupIcon = "Pickup";
    public const string inputIcon = "Keyboard";

    private Sprite scriptIconSprite = null;

    public LogicEngineEditor (EditorWindow window, LogicEngine engine, IEngineHandler engineHandler)
    {
        this.window = window;
        this.engine = engine;
        this.engineHandler = engineHandler;
    }

    public void SetEngine (LogicEngine engine)
    {
        this.engine = engine;
        engine.selectedScript = engine.scripts[0];
    }

    public void SetEngine (IEngineHandler engineHandler)
    {
        this.engine = engineHandler.GetEngine();
        this.engineHandler = engineHandler;
        if (engine.scripts.Count == 0)
        {
            engine.scripts.Add(new LogicScript("Main"));
        }
        engine.selectedScript = engine.scripts[0];
    }

    public void SetSelectedScript (LogicScript script)
    {
        engine.selectedScript = script;
        EditorUtility.SetDirty(engineHandler.GetData());
    }

    public static GeneralNode Node_UnitStartsCastingThisAbility;
    public static GeneralNode Node_UnitFinishesCastingThisAbility;
    public static GeneralNode Node_OwnerNode;

    public static void BuildValueNodes()
    {
        GeneralNode lastCreatedProjectile = GeneralNode.Preset<ProjectileNode>(LogicEngine.PRESET_PROJECTILE_LAST_CREATED).NoValue();

        Node_UnitStartsCastingThisAbility = EventNode.Func(
            "Ability/When a unit begins casting this ability",
            "When a unit begins casting this ability",
            "WhenUnitBeginsCastingThisAbility",
            eventIcon,
            new string[] { LogicEngine.PRESET_CASTING_UNIT, LogicEngine.PRESET_TARGET_POSITION, LogicEngine.PRESET_TARGET_UNIT });

        Node_UnitFinishesCastingThisAbility = EventNode.Func(
            "Ability/When a unit finishes casting this ability",
            "When a unit finishes casting this ability",
            "WhenUnitFinishesCastingThisAbility",
            eventIcon,
            new string[] { LogicEngine.PRESET_CASTING_UNIT, LogicEngine.PRESET_TARGET_POSITION, LogicEngine.PRESET_TARGET_UNIT });

        Node_OwnerNode = GeneralNode.Preset<UnitNode>(LogicEngine.PRESET_ABILITY_OWNER).NoValue();

        #region Event Nodes

        allNodes.Add(EventNode.Func(
            "Time/On script loaded",
            "On script loaded",
            LogicEngine.EVENT_SCRIPT_LOADED,
            timerIcon));

        allNodes.Add(EventNode.Func(
            "Time/On script unloaded",
            "On script unloaded",
            LogicEngine.EVENT_SCRIPT_UNLOADED,
            timerIcon));

        allNodes.Add(EventNode.Func(
            "Time/Do every frame",
            "Do actions every frame",
            "EveryFrame",
            timerIcon));

        allNodes.Add(EventNode.Func(
            "Time/Do after X seconds",
            "After $ seconds",
            "OnOneOffTimerFinished",
            timerIcon,
            new string[] { },
            NumberNode.Value(5)));

        allNodes.Add(EventNode.Func(
            "Time/Do every X Seconds",
            "Every $ seconds",
            "OnTimerFinished",
            timerIcon,
            new string[] { },
            NumberNode.Value(5)));

        allNodes.Add(EventNode.Func(
            "Unit/Unit is killed",
            "When a unit is killed",
            "WhenUnitIsKilled",
            eventIcon,
            new string[] { LogicEngine.PRESET_KILLED_UNIT, 
                LogicEngine.PRESET_KILLING_UNIT, 
                LogicEngine.PRESET_KILLING_ABILITY,
                LogicEngine.PRESET_IS_CRITICAL})); 

        allNodes.Add(EventNode.Func(
            "Unit/Unit is damaged",
            "When a unit is damaged",
            LogicEngine.EVENT_UNIT_DAMAGED,
            eventIcon,
            new string[] { LogicEngine.PRESET_DAMAGED_UNIT,
                LogicEngine.PRESET_DAMAGING_UNIT, 
                LogicEngine.PRESET_DAMAGING_ABILITY,
                LogicEngine.PRESET_DAMAGE_DEALT,
                LogicEngine.PRESET_IS_CRITICAL}));

        allNodes.Add(EventNode.Func(
            "Unit/Unit is healed",
            "When a unit is healed",
            "WhenUnitIsHealed",
            eventIcon,
            new string[] { LogicEngine.PRESET_HEALED_UNIT, 
                LogicEngine.PRESET_HEALING_UNIT }));

        allNodes.Add(EventNode.Func(
            "Unit/Unit gains resource",
            "When a unit gains resource",
            "WhenUnitGainsResource",
            eventIcon,
            new string[] { LogicEngine.PRESET_TRIGGERING_UNIT, 
                LogicEngine.PRESET_RESOURCES_GAINED }));

        allNodes.Add(EventNode.Func(
            "Unit/Unit loses resource",
            "When a unit loses resource",
            "WhenUnitLosesResource",
            eventIcon,
            new string[] { LogicEngine.PRESET_TRIGGERING_UNIT, 
                LogicEngine.PRESET_RESOURCES_LOST }));

        allNodes.Add(Node_UnitStartsCastingThisAbility);
        allNodes.Add(Node_UnitFinishesCastingThisAbility);

        allNodes.Add(EventNode.Func(
            "Ability/When a unit starts casting specific ability",
            "When Unit Starts Casting $",
            "UnitStartCast",
            eventIcon,
            new string[] { LogicEngine.PRESET_CASTING_UNIT, 
                LogicEngine.PRESET_ABILITY_CAST },
            AbilityNode.Temp()));

        allNodes.Add(EventNode.Func(
            "Ability/When a unit finishes casting specific ability",
            "When Unit Finishes Casting $",
            "UnitFinishCast",
            eventIcon,
            new string[] { LogicEngine.PRESET_CASTING_UNIT, 
                LogicEngine.PRESET_ABILITY_CAST },
            AbilityNode.Temp()));

        allNodes.Add(EventNode.Func(
            "Player/Player gains experience",
            "When the player gains experience",
            "PlayerGainsExperience",
            eventIcon));

        allNodes.Add(EventNode.Func(
            "Player/Player gains a level",
            "When the player gains a level",
            LogicEngine.EVENT_PLAYER_LEVEL_UP,
            eventIcon));

        allNodes.Add(EventNode.Func(
            "Player/Player sells an item",
            "When the player sells an item",
            LogicEngine.EVENT_ITEM_SOLD,
            eventIcon,
            new string[] { LogicEngine.PRESET_TRIGGERING_ITEM }));

        allNodes.Add(EventNode.Func(
            "Player/Player buys an item",
            "When the player buys an item",
            LogicEngine.EVENT_ITEM_BOUGHT,
            eventIcon,
            new string[] { LogicEngine.PRESET_TRIGGERING_ITEM }));

        allNodes.Add(EventNode.Func(
            "Player/Player equips an item",
            "When the player equips an item",
            LogicEngine.EVENT_ITEM_EQUIPPED,
            pickupIcon,
            new string[] { LogicEngine.PRESET_TRIGGERING_ITEM }));

        allNodes.Add(EventNode.Func(
            "Player/Player unequips an item",
            "When the player unequips an item",
            LogicEngine.EVENT_ITEM_UNEQUIPPED,
            pickupIcon,
            new string[] { LogicEngine.PRESET_TRIGGERING_ITEM }));

        allNodes.Add(EventNode.Func(
            "Player/Player picks up an item",
            "When the player picks up an item",
            LogicEngine.EVENT_ITEM_PICKED_UP,
            pickupIcon,
            new string[] { LogicEngine.PRESET_TRIGGERING_ITEM }));

        allNodes.Add(EventNode.Func(
            "Player/Player picks up gold",
            "When the player picks up gold",
            LogicEngine.EVENT_ON_PICKUP_GOLD,
            pickupIcon,
            new string[] { LogicEngine.PRESET_GOLD_PICKED_UP }));

        allNodes.Add(EventNode.Func(
            "Player/Player picks up a health globe",
            "When the player picks up a health globe",
            LogicEngine.EVENT_ON_PICKUP_HEALTH,
            pickupIcon,
            new string[] { LogicEngine.PRESET_HEALTH_PICKED_UP }));


        allNodes.Add(EventNode.Func(
            "Region/Unit enters region",
            "Unit enters region named $",
            LogicEngine.EVENT_REGION_ENTER,
            regionIcon,
            new string[] { },
            StringNode.Temp("Region Name")));

        allNodes.Add(EventNode.Func(
            "Region/Unit leaves region",
            "Unit leaves region named $",
            LogicEngine.EVENT_REGION_EXIT,
            regionIcon,
            new string[] { },
            StringNode.Temp("Region Name")));

        allNodes.Add(EventNode.Func(
            "Projectile/Projectile collides with an enemy",
            "Projectile from this object collides with an enemy",
            LogicEngine.EVENT_PROJECTILE_OWNED_COLLIDES_WITH_UNIT,
            projectileIcon,
            new string[] { LogicEngine.PRESET_EVENT_PROJECTILE, 
                LogicEngine.PRESET_CASTING_UNIT, 
                LogicEngine.PRESET_COLLIDING_UNIT }));

        allNodes.Add(EventNode.Func(
            "Projectile/Projectile times out",
            "Projectile from this object times out",
            LogicEngine.EVENT_PROJECTILE_TIMES_OUT,
            projectileIcon,
            new string[] { LogicEngine.PRESET_EVENT_PROJECTILE, 
                LogicEngine.PRESET_CASTING_UNIT }));

        allNodes.Add(EventNode.Func(
            "Projectile/Projectile reaches its goal",
            "Projectile from this object reaches its goal",
            LogicEngine.EVENT_PROJECTILE_REACHES_GOAL,
            projectileIcon,
            new string[] { LogicEngine.PRESET_EVENT_PROJECTILE, 
                LogicEngine.PRESET_CASTING_UNIT, 
                LogicEngine.PRESET_GOAL_POSITION, 
                LogicEngine.PRESET_GOAL_UNIT }));

        allNodes.Add(EventNode.Func(
            "Projectile/Projectile collides with terrain",
            "Projectile from this object collides with the terrain",
            LogicEngine.EVENT_PROJECTILE_COLLIDES_WITH_TERRAIN,
            projectileIcon,
            new string[] { LogicEngine.PRESET_EVENT_PROJECTILE, 
                LogicEngine.PRESET_CASTING_UNIT }));

        allNodes.Add(GeneralNode.Func<EventNode>(
            "Quests/On quest completed",
            "When the quest named $ is completed",
            LogicEngine.EVENT_ON_QUEST_COMPLETED,
            questIcon,
            StringNode.Temp("Quest Name")));

        allNodes.Add(GeneralNode.Func<EventNode>(
            "Quests/On quest received",
            "When a quest with tag $ is received",
            LogicEngine.EVENT_ON_QUEST_RECEIVED,
            questIcon,
            StringNode.Temp("Quest Name")));

        allNodes.Add(GeneralNode.Func<EventNode>(
            "Input/On Key Down",
            "When the $ key is pressed down",
            LogicEngine.EVENT_KEY_DOWN,
            inputIcon,
            StringNode.Temp("Key")));

        allNodes.Add(GeneralNode.Func<EventNode>(
            "Input/On Key Up",
            "When the $ key is released",
            LogicEngine.EVENT_KEY_UP,
            inputIcon,
            StringNode.Temp("Key")));

        allNodes.Add(GeneralNode.Func<EventNode>(
            "Input/On Key Held",
            "While the $ key is held down",
            LogicEngine.EVENT_KEY_HELD,
            inputIcon,
            StringNode.Temp("Key")));

        #endregion

        #region Action Nodes

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Flow/Wait",
            "Wait for $ seconds",
            "Wait",
            waitIcon,
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<NestingActionNode>(
            "Flow/If Statement",
            "Do actions if $",
            "DoActionsIfBool",
            conditionIcon,
            BoolNode.Temp()));

        allNodes.Add(GeneralNode.Func<NestingActionNode>(
            "Flow/While Loop",
            "Do actions while $",
            "DoActionsWhileBool",
            loopIcon,
            BoolNode.Temp()));

        allNodes.Add(GeneralNode.Func<NestingActionNode>(
            "Flow/For Loop",
            "Do actions $ times (Variable Storing Current Iteration: $)",
            "DoActionsXTimesStoringVariable",
            loopIcon,
            NumberNode.Temp(),
            StringNode.Value("Loop ID")));

        allNodes.Add(GeneralNode.Func<NestingActionNode>(
            "Flow/For Each Unit in Unit Group",
            "For Each Unit in $ (Variable Storing Current Unit: $)",
            "ForEachUnitInGroup",
            loopIcon,
            UnitGroupNode.Temp().NoValue(),
            StringNode.Value("Unit")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Flow/Disable This Script",
            "Disable This Script",
            "DisableScript",
            cancelIcon));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Spawn Monster",
            "Spawn $ at $",
            "SpawnUnit",
            unitIcon,
            UnitNode.Temp().NoPreset().NoFunction(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Spawn Empowered Monster",
            "Spawn empowered $ at $",
            "SpawnEmpoweredUnit",
            unitIcon,
            UnitNode.Temp().NoPreset().NoFunction(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Deal Damage to Unit",
            "Deal $ attack damage to $",
            "HaveUnitDamageUnit2",
            unitIcon,
            NumberNode.Value(100, "%"),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Deal Damage to Unit Group",         
            "Deal $ attack damage to $", 
            "HaveUnitDamageUnits2",
            unitIcon,
            NumberNode.Value(100, "%"),
            UnitGroupNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Add Health",
            "Add $ health to $",
            "AddHealth",
            unitIcon,
            NumberNode.Value(100),
            Node_OwnerNode));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Remove Health", 
            "Remove $ health from $", 
            "RemoveHealth",
            unitIcon,
            NumberNode.Value(100), 
            Node_OwnerNode));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Add Resource",
            "Add $ resource to $",
            "AddResource",
            unitIcon,
            NumberNode.Value(100),
            Node_OwnerNode));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Remove Resource",
            "Remove $ resource from $",
            "RemoveResource",
            unitIcon,
            NumberNode.Value(100),
            Node_OwnerNode));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Teleport",
            "Teleport $ to $",
            "TeleportUnit",
            unitIcon,
            UnitNode.Temp().NoValue(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Move Over Time",
            "Move $ to $ over $",
            "MoveUnitOverTime",
            unitIcon,
            UnitNode.Temp().NoValue(),
            VectorNode.Temp(),
            NumberNode.Value(1, "s")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Play Animation",
            "Play $ animation on $",
            "PlayUnitAnimation",
            unitIcon,
            StringNode.Value(Unit.animations[1], Unit.animations).NoFunction().NoPreset(),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Kill",
            "Kill $",
            "KillUnit",
            unitIcon,
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Spin",
            "Spin $ by $ for $",
            "SpinUnit",
            unitIcon,
            UnitNode.Temp().NoValue(),
            NumberNode.Value(360, "º/s"),
            NumberNode.Value(1, "s")));


        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Remove Buff",
            "Remove buff named $ from $",
            "RemoveBuff",
            unitIcon,
            StringNode.Temp().NoFunction().NoPreset(),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Enable Ability on Unit",
            "Enable $ on $",
            "EnableAbility",
            unitIcon,
            AbilityNode.Temp().NoFunction().NoPreset(),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Disable Ability on Unit",
            "Disable $ on $",
            "DisableAbility",
            unitIcon,
            AbilityNode.Temp().NoFunction().NoPreset(),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Reduce Current Ability Cooldown",
            "Reduce current cooldown of $ on $ by $",
            "ReduceAbilityCooldown",
            unitIcon,
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Value(1, "s")));


        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Add Ability",
            "Add $ to $",
            "AddAbilityToUnit",
            unitIcon,
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Remove Ability",
            "Remove $ from $",
            "RemoveAbilityFromUnit",
            unitIcon,
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Replace Ability",
            "Replace $ with $ on $",
            "ReplaceAbilityOnUnit",
            unitIcon,
            AbilityNode.Temp().NoFunction(),
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue()));



        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Add Damage Modifier to Ability",
            "$ damage of $ on $ by $ (Buff Name: $ | Max Stacks: $)",
            "AddAbilityDamageModifierToUnit",
            unitIcon,
            StringNode.Value(increaseDecrease[0], increaseDecrease).NoPreset().NoFunction(),
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue(), 
            NumberNode.Value(50, "%"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Add Cooldown Modifier to Ability",
            "$ cooldown of $ on $ by $ (Buff Name: $ | Max Stacks: $)",
            "AddAbilityCooldownModifierToUnit",
            unitIcon,
            StringNode.Value(increaseDecrease[0], increaseDecrease).NoPreset().NoFunction(),
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Value(50, "%"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Add Cost Modifier to Ability",
            "$ cost of $ on $ by $ (Buff Name: $ | Max Stacks: $)",
            "AddAbilityCostModifierToUnit",
            unitIcon,
            StringNode.Value(increaseDecrease[0], increaseDecrease).NoPreset().NoFunction(),
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Value(50, "%"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Add Timed Damage Modifier to Ability",
            "$ damage of $ on $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "AddTimedAbilityDamageModifierToUnit",
            unitIcon,
            StringNode.Value(increaseDecrease[0], increaseDecrease).NoPreset().NoFunction(),
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Value(50, "%"),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Add Timed Cooldown Modifier to Ability",
            "$ cooldown of $ on $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "AddTimedAbilityCooldownModifierToUnit",
            unitIcon,
            StringNode.Value(increaseDecrease[0], increaseDecrease).NoPreset().NoFunction(),
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Value(50, "%"),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Add Timed Cost Modifier to Ability",
            "$ cost of $ on $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "AddTimedAbilityCostModifierToUnit",
            unitIcon,
            StringNode.Value(increaseDecrease[0], increaseDecrease).NoPreset().NoFunction(),
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Value(50, "%"),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Ability/Remove Ability Modifier",
            "Remove buffs for $ on $ named $",
            "RemoveAbilityModifier", 
            unitIcon,
            AbilityNode.Temp().NoFunction(),
            UnitNode.Temp().NoValue(),
            StringNode.Temp()));

        Stat[] allStats = (Stat[])Enum.GetValues(typeof(Stat));
        string[] statModifiers = new string[allStats.Length];
        for (int i = 0; i < statModifiers.Length; i++)
        {
            statModifiers[i] = allStats[i].Label();
        } 

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Increase Stat/Increase Stat on Unit by Value",
            "Increase $ of $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "IncreaseStatValue",
            unitIcon,
            StringNode.Value(statModifiers[0], statModifiers).NoPreset().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Temp(),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Increase Stat/Increase Stat on Unit by Percent",
            "Increase $ of $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "IncreaseStatPercent",
            unitIcon,
            StringNode.Value(statModifiers[0], statModifiers).NoPreset().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Temp("%", "Percent"),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Decrease Stat/Decrease Stat on Unit by Value",
            "Decrease $ of $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "DecreaseStatValue",
            unitIcon,
            StringNode.Value(statModifiers[0], statModifiers).NoPreset().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Temp(),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Decrease Stat/Decrease Stat on Unit by Percent",
            "Decrease $ of $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "DecreaseStatPercent",
            unitIcon,
            StringNode.Value(statModifiers[0], statModifiers).NoPreset().NoFunction(),
            UnitNode.Temp().NoValue(),
            NumberNode.Temp("%", "Percent"),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Increase Stat/Increase Stat of Unit Group by Value",
            "Increase $ of $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "IncreaseStatValueUnitGroup",
            unitIcon,
            StringNode.Value(statModifiers[0], statModifiers).NoPreset().NoFunction(),
            UnitGroupNode.Temp().NoValue(),
            NumberNode.Temp(),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Increase Stat/Increase Stat of Unit Group by Percent",
            "Increase $ of $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "IncreaseStatPercentUnitGroup",
            unitIcon,
            StringNode.Value(statModifiers[0], statModifiers).NoPreset().NoFunction(),
            UnitGroupNode.Temp().NoValue(),
            NumberNode.Temp("%", "Percent"),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Decrease Stat/Decrease Stat of Unit Group by Value",
            "Decrease $ of $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "DecreaseStatValueUnitGroup",
            unitIcon,
            StringNode.Value(statModifiers[0], statModifiers).NoPreset().NoFunction(),
            UnitGroupNode.Temp().NoValue(),
            NumberNode.Temp(),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Unit/Decrease Stat/Decrease Stat of Unit Group by Percent",
            "Decrease $ of $ by $ for $ (Buff Name: $ | Max Stacks: $)",
            "DecreaseStatPercentUnitGroup",
            unitIcon,
            StringNode.Value(statModifiers[0], statModifiers).NoPreset().NoFunction(),
            UnitGroupNode.Temp().NoValue(),
            NumberNode.Temp("%", "Percent"),
            NumberNode.Value(5, "s"),
            StringNode.Value("None"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Pickups/Spawn Item Pickup",
            "Spawn an item pickup at $ containing $",
            "SpawnItemDrop",
            pickupIcon,
            VectorNode.Temp("Location"),
            ItemNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Pickups/Spawn Gold Pickup",
            "Spawn a gold pickup at $ containing $ gold",
            "SpawnGoldDrop",
            pickupIcon,
            VectorNode.Temp("Location"),
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Pickups/Spawn Health Pickup",
            "Spawn a health pickup at $",
            "SpawnHealthDrop",
            pickupIcon,
            VectorNode.Temp("Location")));

        /*
        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Spawn Projectile",
            "Spawn $ projectile at $",
            "SpawnProjectile",
            projectileIcon,
            ProjectileNode.Temp().NoPreset().NoFunction(),
            VectorNode.Temp().NoValue()));
        */

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Spawn Projectile",
            "Have $ spawn $ at $",
            "SpawnProjectileNew",
            projectileIcon,
            UnitNode.Temp().NoValue(),
            ProjectileNode.Temp().NoPreset().NoFunction(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Rotate Projectile",
            "Rotate $ by $",
            "RotateProjectile",
            projectileIcon,
            lastCreatedProjectile,
            NumberNode.Value(30, "º")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Face Projectile Towards Point",
            "Face $ towards $",
            "FaceProjectileTowardsPoint",
            projectileIcon,
            lastCreatedProjectile,
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Move Projectile Forwards",
            "Move $ forwards at $",
            "MoveForwardAtSpeed",
            projectileIcon,
            lastCreatedProjectile,
            NumberNode.Value(3, "m/s")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Move Projectile Towards Point",
            "Move $ towards $ at $",
            "MoveTowardsPointAtSpeed",
            projectileIcon,
            lastCreatedProjectile,
            VectorNode.Temp(),
            NumberNode.Value(3, "m/s")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Move Projectile Towards Point in Arc",
            "Move $ towards $ in $ with $ arc",
            "MoveTowardsPointInArc",
            projectileIcon,
            lastCreatedProjectile,
            VectorNode.Temp(),
            NumberNode.Value(3, "s"),
            NumberNode.Value(2, "m")));

        /*
        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Move Projectile Towards Point in Set Time",
            "Move $ towards $ in $",
            "MoveTowardsPointInSetTime",
            projectileIcon,
            lastCreatedProjectile,
            VectorNode.Temp(),
            NumberNode.Value(1, "s")));

        
        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Move Projectile Towards Unit at Set Speed",
            "Move $ towards $ at $",
            "MoveTowardsUnitAtSpeed",
            projectileIcon,
            lastCreatedProjectile,
            UnitNode.Temp(),
            NumberNode.Value(3, "m/s"))); 
        
        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Move Projectile Towards Unit in Set Time",
            "Move $ towards $ in $",
            "MoveTowardsUnitInSetTime",
            projectileIcon,
            lastCreatedProjectile,
            UnitNode.Temp(),
            NumberNode.Value(1, "s")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Add Collider",
            "Add Collider to $ with radius $",
            "AddProjectileCollider",
            projectileIcon,
            lastCreatedProjectile,
            NumberNode.Value(1, "m")));
        */

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Destroy Projectile",
            "Destroy $",
            "DestroyProjectile",
            projectileIcon,
            ProjectileNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Projectile/Set Lifetime",
            "Set max lifetime of $ to $",
            "SetProjectileLifetime",
            projectileIcon,
            lastCreatedProjectile,
            NumberNode.Value(2, "s")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Add Gold",
            "Add $ gold to the player",
            "AddGold",
            unitIcon,
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Remove Gold",
            "Remove $ gold from the player",
            "RemoveGold",
            unitIcon,
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Add Experience",
            "Add $ experience to the player",
            "AddExperience",
            unitIcon,
            NumberNode.Value(100)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Set Level",
            "Set the player to level $",
            "SetLevel",
            unitIcon,
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Add Levels",
            "Add $ level(s) to the player.",
            "AddLevels",
            unitIcon,
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Remove Levels",
            "Remove $ level(s) to the player.",
            "RemoveLevels",
            unitIcon,
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Delete Equipment",
            "Remove all equipment on the player",
            "RemoveEquipment",
            unitIcon));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Equip Item",
            "Have the player equip $",
            "EquipItem",
            unitIcon,
            ItemNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Add Item",
            "Add $ to the player",
            "AddItem",
            unitIcon,
            ItemNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Player/Remove Item",
            "Remove $ to the player",
            "RemoveItem",
            unitIcon,
            ItemNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Region/Destroy Region",
            "Destroy regions named $",
            "DestroyRegions",
            regionIcon,             
            StringNode.Temp("Region Name")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "UI/Show Tutorial Message",
            "Show a tutorial message printing $ for $ seconds",
            "ShowTutorialMessage",
            uiIcon,
            StringNode.Temp(),
            NumberNode.Value(5)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "UI/Show Debug Message",
            "Show a debug message printing $",
            "ShowDebugMessage",
            uiIcon,
            StringNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "UI/Show In-World Status Message",
            "Show a status message at $ printing $",
            "ShowStatusMessage",
            uiIcon,
            VectorNode.Temp(),
            StringNode.Temp()));

        string[] options = new string[] { "Play", "Stop", "Play or Refresh" };

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Feedback/Play Effect at Location",
            "Play $ effect at $ for $ at $ scale",
            "SpawnEffectAtLocation",
            effectIcon,
            EffectNode.Temp(),
            VectorNode.Temp().NoValue(),
            NumberNode.Value(2, "s"),
            NumberNode.Value(1.0f, "x")));
        
        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Feedback/Play Effect on Unit",
            "$ $ effect on $ for $ at $ scale",
            "SpawnEffectOnUnit",
            effectIcon,
            StringNode.Value(options[0], options),
            EffectNode.Temp(),
            UnitNode.Temp().NoValue(),
            NumberNode.Value(2, "s"),
            NumberNode.Value(1.0f, "x")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Feedback/Play Effect on Unit Group",
            "$ $ effect on $ for $ at $ scale",
            "SpawnEffectOnUnitGroup",
            effectIcon,
            StringNode.Value(options[0], options),
            EffectNode.Temp(),
            UnitGroupNode.Temp().NoValue(),
            NumberNode.Value(2, "s"),
            NumberNode.Value(1.0f, "x")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Feedback/Shake Screen",
            "Shake the screen with a strength of $",
            "ShakeScreen",
            effectIcon,
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Feedback/Flash Color on Unit",
            "Flash $ on $ for $",
            "ColorFlash",
            effectIcon,
            ColorNode.Temp(),
            UnitNode.Temp().NoValue(),
            NumberNode.Value(1, "s")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Feedback/Create Circular Effect Guide",
            "Create a circular guide at $ with radius $ for $",
            "CreateCircleGuide",
            effectIcon,
            VectorNode.Temp(),
            NumberNode.Value(2, "m"),
            NumberNode.Temp("s", "Duration")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Audio/Play Sound",
            "Play $ at $ volume",
            "Play2DSound",
            soundIcon,
            AudioNode.Temp(),
            NumberNode.Value(100, "%")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Audio/Play Music",
            "Change Game Music to $",
            "ChangeGameMusic",
            soundIcon,
            AudioNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Quests/Create Quest",
            "Give the player a quest named $",
            "CreateQuest2",
            questIcon,
            StringNode.Temp("Quest Name")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Quests/Add Quest Requirement",
            "Add $ requirement to $ with $ progress increments",
            "AddQuestRequirement2",
            questIcon,
            StringNode.Temp("Requirement"),
            StringNode.Temp("Quest Name"),
            NumberNode.Value(1)
            ));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Quests/Add Quest Reward",
            "Add reward labeled $ to quest $",
            "AddQuestReward2",
            questIcon,
            StringNode.Temp("Reward"),
            StringNode.Temp("Quest Name")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Quests/Modify Quest Progress",
            "Modify quest progress of $ by $",
            "ModifyQuestRequirementProgress2",
            questIcon,
            StringNode.Temp("Quest Name"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Quests/Set Quest Progress",
            "Set quest progress of $ to $",
            "SetQuestRequirementProgress2",
            questIcon,
            StringNode.Temp("Quest Name"),
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Quests/Modify Requirement Progress",
            "Modify quest progress of $ on $ by $",
            "ModifySpecificQuestRequirementProgress2",
            questIcon,
            StringNode.Temp("Requirement"),
            StringNode.Temp("Quest Name"),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Quests/Set Requirement Progress",
            "Set quest progress of $ on $ to $",
            "SetSpecificQuestRequirementProgress2",
            questIcon,
            StringNode.Temp("Tag"),
            StringNode.Temp("Tag"),
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Game/Win Game",
            "Have the player win the game",
            "WinGame",
            gameIcon));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Set Number Variable",
            "Set script number variable named $ to $",
            "SetNumberVariable",
            variableIcon,
            StringNode.Temp("Variable Name"),
            NumberNode.Value(0)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Modify Number Variable",
            "Modify local number variable named $ by $",
            "ModifyNumberVariable",
            variableIcon,
            StringNode.Temp(),
            NumberNode.Value(0)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Set Bool Variable",
            "Set script bool variable named $ to $",
            "SetBoolVariable",
            variableIcon,
            StringNode.Temp("Variable Name"),
            BoolNode.Value(true)));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Set Unit Variable",
            "Set script unit variable named $ to $",
            "SetUnitVariable",
            variableIcon,
            StringNode.Temp("Variable Name"),
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Set Vector Variable",
            "Set script vector variable named $ to $",
            "SetVectorVariable",
            variableIcon,
            StringNode.Temp(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Set String Variable",
            "Set script string variable named $ to $",
            "SetStringVariable",
            variableIcon,
            StringNode.Temp(),
            StringNode.Temp()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Set Unit Group Variable",
            "Set Unit Group named $ to $",
            "SetUnitGroupVariable",
            variableIcon,
            StringNode.Temp("Variable Name"),
            UnitGroupNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Add Unit to Unit Group Variable",
            "Add $ to Unit Group Named $",
            "AddToUnitGroup",
            variableIcon,
            UnitNode.Temp().NoValue(),
            StringNode.Temp("Variable Name")));

        allNodes.Add(GeneralNode.Func<ActionNode>(
            "Variables/Remove Unit from Unit Group Variable",
            "Remove $ from Unit Group Named $",
            "RemoveFromUnitGroup",
            variableIcon,
            UnitNode.Temp().NoValue(),
            StringNode.Temp("Variable Name")));

        #endregion

        #region Number Value Nodes

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Unit/Unit Health",
            "$ Health",
            "UnitHealth",
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Unit/Unit Max Health",
            "$ Max Health",
            "UnitMaxHealth",
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Unit/Unit Health Percent",
            "$ Health Percent",
            "HealthPercent",
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Unit/Unit Resource",
            "$ Resource",
            "UnitResource",
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Unit/Unit Max Resource",
            "$ Max Resource",
            "UnitMaxResource",
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Player/Player Level",
            "Player Level",
            "GetPlayerLevel"));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Player/Player Gold",
            "Player Gold", 
            "GetPlayerGold"));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Player/Items Equipped",
            "Count equipped items on player",
            "GetPlayerItemsEquipped"));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Ability/Remaining Ability Cooldown",
            "Remaining cooldown for $ on $",
            "RemainingAbilityCooldown",
            AbilityNode.Temp(),
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Ability/Base Ability Cooldown",
            "Base cooldown for $",
            "RemainingAbilityCooldown",
            AbilityNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Ability/Time Since Unit Last Cast Ability",
            "Time since $ last cast $",
            "RemainingAbilityCooldown",
            UnitNode.Temp(),
            AbilityNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Time/Time Since Level Start",
            "TimeSinceLevelStart",
            "TimeSinceLevelStart"));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Math/Addition",
            "$ + $",
            "Addition",
            NumberNode.Temp(),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Math/Subtraction",
            "$ - $",
            "Subtraction",
            NumberNode.Temp(),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Math/Multiplication",
            "$ x $",
            "Multiplication",
            NumberNode.Temp(),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Math/Division",
            "$ / $",
            "Division",
            NumberNode.Temp(),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Random/Random Number",
            "Random Number Between $ and $",
            "RandomNumberBetween",
            NumberNode.Value(0),
            NumberNode.Value(1)));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Math/Vector Component",
            "$ Component of $",
            "VectorComponent",
            StringNode.Value("X", new string[] { "X", "Y", "Z" }),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Math/Distance Between Points",
            "Distance between $ and $",
            "DistanceBetweenPoints",
            VectorNode.Temp(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Math/Distance Between Units",
            "Distance between $ and $",
            "DistanceBetweenUnits",
            UnitNode.Temp(),
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Count Units in Unit Group",
            "Number of units in $",
            "CountUnitsInUnitGroup",
            UnitGroupNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<NumberNode>(
            "Number Variable",
            "Number: $",
            "GetNumberVariable",
            StringNode.Temp().NoFunction().NoPreset()));

        #endregion

        #region Unit Value Nodes

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Closest Enemy to Point",
            "Closest enemy within $ of $",
            "ClosestEnemyToPoint",
            NumberNode.Temp("m"),
            VectorNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Closest Enemy to Unit",
            "Closest enemy within $ of $",
            "ClosestEnemyToUnit",
            NumberNode.Temp("m"),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Random Enemy Near Point",
            "Closest enemy within $ of $",
            "RandomEnemyNearPoint",
            NumberNode.Temp("m"),
            VectorNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Random Enemy Near Unit",
            "Closest enemy within $ of $",
            "RandomEnemyNearUnit",
            NumberNode.Temp("m"),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Distance/Closest Unit Near Point",
            "Closest unit within $ distance of $",
            "ClosestUnitToPoint",
            NumberNode.Temp("m"),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Distance/Closest Unit Near Unit",
            "Closest unit within $ distance of $",
            "ClosestUnitToUnit",
            NumberNode.Temp("m"),
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Distance/Furthest Unit Near Point",
            "Furthest unit within $ distance of $",
            "FurthestUnitToPoint",
            NumberNode.Temp("m"),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Distance/Furthest Unit Near Unit",
            "Furthest unit within $ distance of $",
            "FurthestUnitToUnit",
            NumberNode.Temp("m"),
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Random/Random Unit Near Unit",
            "Random unit within $ distance of $",
            "RandomNearbyUnitToUnit",
            NumberNode.Temp(),
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Random/Random Unit Near Point",
            "Random unit within $ distance of $",
            "RandomNearbyUnitToPoint",
            NumberNode.Temp(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Unit with Label",
            "Unit labeled $",
            "GetUnitWithLabel",
            StringNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitNode>(
            "Unit Variable",
            "Unit: $",
            "GetUnitVariable",
            StringNode.Temp().NoFunction().NoPreset()));

        #endregion

        #region Bool Value Nodes

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Comparisons/Or Comparison",
            "$ or $",
            "OrComparison",
            conditionIcon,
            BoolNode.Temp(),
            BoolNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Comparisons/And Comparison",
            "$ and $",
            "AndComparison",
            conditionIcon,
            BoolNode.Temp(),
            BoolNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Comparisons/Bool Comparison",
            "$ $ $",
            "BoolComparison",
            conditionIcon,
            BoolNode.Temp(),
            StringNode.Value(boolComparators[0], boolComparators),
            BoolNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Comparisons/Number Comparison",
            "$ $ $",
            "NumberComparison",
            conditionIcon,
            NumberNode.Temp(),
            StringNode.Value(numberComparators[0], numberComparators),
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Comparisons/Vector Comparison",
            "$ $ $",
            "VectorComparison",
            conditionIcon,
            VectorNode.Temp(),
            StringNode.Value(boolComparators[0], boolComparators),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Comparisons/Ability Comparison",
            "$ $ $",
            "AbilityComparison",
            conditionIcon,
            AbilityNode.Temp(),
            StringNode.Value(boolComparators[0], boolComparators),
            AbilityNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Comparisons/String Comparison",
            "$ $ $",
            "StringComparison",
            conditionIcon,
            StringNode.Temp(),
            StringNode.Value(boolComparators[0], boolComparators),
            StringNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Comparisons/Unit Comparison",
            "$ $ $",
            "UnitComparison",
            conditionIcon,
            UnitNode.Temp(),
            StringNode.Value(boolComparators[0], boolComparators),
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Matches Template",
            "$ type matches $",
            "UnitTypeMatch",
            conditionIcon,
            UnitNode.Temp(),
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Is Moving",
            "$ is moving",
            "UnitIsMoving",
            conditionIcon,
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Is Stationary",
            "$ is stationary",
            "UnitIsStationary",
            conditionIcon,
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Is Casting",
            "$ is casting",
            "UnitIsCasting",
            conditionIcon,
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Can Move",
            "$ can move",
            "UnitCanMove",
            conditionIcon,
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Has Buff",
            "$ has buff labelled $",
            "UnitHasBuff",
            conditionIcon,
            UnitNode.Temp().NoValue(),
            StringNode.Temp("Label")));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Ability Is On Cooldown",
            "$ is on cooldown for $",
            "UnitAbilityIsOnCooldown",
            conditionIcon,
            AbilityNode.Temp(),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Can Cast ability",
            "$ can cast $",
            "UnitCanCastAbility",
            conditionIcon,
            UnitNode.Temp().NoValue(),
            AbilityNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit/Unit Is Empowered",
            "$ is empowered",
            "UnitIsEmpowered",
            conditionIcon,
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit Group/Unit is in Unit Group",
            "$ is in $",
            "UnitIsInUnitGroup",
            conditionIcon,
            UnitNode.Temp().NoValue(),
            UnitGroupNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit Group/Unit is not in Unit Group",
            "$ is not in $",
            "UnitIsNotInUnitGroup",
            conditionIcon,
            UnitNode.Temp().NoValue(),
            UnitGroupNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit Group/Unit Group is Empty",
            "$ is empty",
            "UnitGroupIsEmpty",
            conditionIcon,
            UnitGroupNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Unit Group/Unit Group is not Empty",
            "$ is not empty",
            "UnitGroupIsNotEmpty",
            conditionIcon,
            UnitGroupNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Player/Player Is Dead",
            "Player is dead",
            "PlayerIsDead",
            conditionIcon));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Player/Player Has Item Equipped",
            "Player has $ equipped",
            "PlayerHasItemEquipped",
            conditionIcon,
            ItemNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Player/Player Has Item In Inventory",
            "Player has $ in their inventory",
            "PlayerHasItemInInventory",
            conditionIcon,
            ItemNode.Temp()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Quest/Quest Is Active",
            "Quest named $ is currently active",
            "QuestIsActive",
            conditionIcon,
            StringNode.Temp("Quest")));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Quest/Quest Is Completed",
            "Quest named $ has been completed",
            "QuestIsCompleted",
            conditionIcon,
            StringNode.Temp("Quest")));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Region/Region Exists",
            "Region labelled $ exists",
            "RegionExists",
            conditionIcon,
            StringNode.Temp("Label")));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Region/Unit Is In Region",
            "$ is in region labelled $",
            "RegionExists",
            conditionIcon,
            UnitNode.Temp().NoValue(),
            StringNode.Temp("Label")));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Input/Key is Held",
            "$ is Held",
            "KeyIsHeld",
            inputIcon,
            StringNode.Temp("Key")));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Variable/Bool Variable",
            "Bool: $",
            "GetBoolVariable",
            StringNode.Temp().NoFunction().NoPreset()));

        allNodes.Add(GeneralNode.Func<BoolNode>(
            "Variable/Global Bool Variable",
            "Global Bool: $",
            "GetGlobalBoolVariable",
            StringNode.Temp().NoFunction().NoPreset()));

        #endregion

        #region Ability Value Nodes

        allNodes.Add(GeneralNode.Func<AbilityNode>(
            "Last Ability Cast by Unit",
            "Last Ability Cast by $",
            "LastAbilityCastByUnit",
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<AbilityNode>(
            "Random Ability on Unit",
            "Random Ability by $",
            "RandomAbilityOnUnit",
            UnitNode.Temp()));

        #endregion

        #region String Value Nodes

        allNodes.Add(GeneralNode.Func<StringNode>(
            "Name of Unit",
            "Name of $",
            "NameOfUnit",
            UnitNode.Temp()));

        allNodes.Add(GeneralNode.Func<StringNode>(
            "Name of Ability",
            "Name of $",
            "NameOfAbility", 
            AbilityNode.Temp()));

        allNodes.Add(GeneralNode.Func<StringNode>(
            "Combine Strings",
            "$ + $",
            "CombineStrings",
            StringNode.Temp(),
            StringNode.Temp()));

        allNodes.Add(GeneralNode.Func<StringNode>(
            "Variables/String Variable",
            "String: $", 
            "GetStringVariable",
            StringNode.Temp().NoFunction().NoPreset()));

        allNodes.Add(GeneralNode.Func<StringNode>(
            "Variables/Global String Variable",
            "Global String: $",
            "GetGlobalStringVariable",
            StringNode.Temp().NoFunction().NoPreset()));

        #endregion

        #region Vector Value Nodes

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Unit/Position of Unit",
            "Position of $",
            "PositionOfUnit",
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Unit/Cast Point of Unit",
            "Cast Point of $",
            "CastPointOfUnit",
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Unit/Attack Point of Unit",
            "Attack Point of $",
            "AttackPointOfUnit",
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Projectile/Position of Projectile",
            "Position of $",
            "PositionOfProjectile",
            ProjectileNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Math/Vector Addition",
            "$ + $",
            "VectorAddition",
            VectorNode.Temp(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Math/Vector Subtraction",
            "$ - $",
            "VectorSubtraction",
            VectorNode.Temp(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Math/Vector Multiplication",
            "$ x $",
            "VectorMultiplication",
            VectorNode.Temp(),
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Math/Vector Division",
            "$ / $",
            "VectorDivision",
            VectorNode.Temp(),
            NumberNode.Temp()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Random/Random Point Near Unit",
            "Random point within $ of $",
            "RandomPointNearUnit",
            NumberNode.Temp("m", "Range"),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Random/Random Point Near Point",
            "Random point within $ of $",
            "RandomPointNearPoint",
            NumberNode.Temp("m", "Range"),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<VectorNode>(
            "Variable/Vector Variable",
            "Vector: $",
            "GetVectorVariable",
            StringNode.Temp().NoFunction().NoPreset()));

        #endregion

        #region Unit Group Value Node

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "All Enemies Near Point",
            "All enemies within $ of $",
            "AllEnemiesWithinRangeOfPoint2",
            NumberNode.Temp("m"),
            VectorNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "All Enemies Near Unit",
            "All enemies within $ of $",
            "AllEnemiesWithinRangeOfUnit2",
            NumberNode.Temp("m"),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "Alls Enemies In Arc From Unit",
            "All enemies in $ arc from $ extending $",
            "AllEnemiesInArcFromUnit",
            NumberNode.Value(90, "º"),
            UnitNode.Temp().NoValue(),
            NumberNode.Temp("m")));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "Empty Unit Group",
            "Empty Unit Group",
            "EmptyUnitGroup"));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "Other/Alls Enemies In Arc",
            "All enemies of $ in $ degree arc from $ to $",
            "AllEnemiesInArc",
            UnitNode.Temp().NoValue(),
            NumberNode.Value(90),
            VectorNode.Temp(),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "Other/Alls Units Near Point",
            "All units within $ of $",
            "AllUnitsWithinRangeOfPoint",
            NumberNode.Temp("m"),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "Other/Alls Units Near Unit",
            "All units within $ of $",
            "AllUnitsWithinRangeOfUnit",
            NumberNode.Temp("m"),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "Random/Random Units Near Point",
            "$ random units within $ of $",
            "RandomUnitsWithinRangeOfPoint",
            NumberNode.Value(3),
            NumberNode.Value(5, "m"),
            VectorNode.Temp()));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "Random/Random Units Near Unit",
            "$ random units within $ of $",
            "RandomUnitsWithinRangeOfUnit",
            NumberNode.Value(3),
            NumberNode.Value(5),
            UnitNode.Temp().NoValue()));

        allNodes.Add(GeneralNode.Func<UnitGroupNode>(
            "Unit Group Variable",
            "Unit Group: $",
            "GetUnitGroupVariable",
            StringNode.Temp().NoPreset().NoFunction()));

        #endregion

        #region Item Value Node

        string[] rarityOptions = Enum.GetNames(typeof(Item.ItemRarity));
        allNodes.Add(GeneralNode.Func<ItemNode>(
            "Random Item of Rarity",
            "Random $ Item",
            "RandomItemOfRarity",
            StringNode.Value(rarityOptions[0], rarityOptions).NoPreset().NoFunction()));

        #endregion
    }

    private static void BuildStyles ()
    { 
        GUIStyle s = null;
        int defaultFontSize = 14;

        windowStyle_HeaderText = new GUIStyle(EditorStyles.boldLabel);
        windowStyle_HeaderText.fontSize = defaultFontSize;
        windowStyle_HeaderText.richText = true;

        windowStyle_BaseText = new GUIStyle(EditorStyles.boldLabel);
        windowStyle_BaseText.fontSize = defaultFontSize;
        windowStyle_BaseText.richText = true;

        windowStyle_BodyText = new GUIStyle(EditorStyles.boldLabel);
        windowStyle_BodyText.fontSize = 13;
        windowStyle_BodyText.richText = true;

        windowStyle_PresetText = new GUIStyle(EditorStyles.boldLabel);
        windowStyle_PresetText.fontSize = defaultFontSize;
        windowStyle_PresetText.normal.textColor = Color.yellow;

        windowStyle_ValueText = new GUIStyle(GUI.skin.button);
        windowStyle_ValueText.fontSize = defaultFontSize;
        windowStyle_ValueText.normal.textColor = new Color(0.5f, 1.0f, 0.5f);
        windowStyle_ValueText.richText = true;

        windowStyle_TempText = new GUIStyle(GUI.skin.button);
        windowStyle_TempText.fontSize = defaultFontSize;
        windowStyle_TempText.normal.textColor = new Color(1.0f, 0.5f, 0.5f);

        s = new GUIStyle(EditorStyles.label);
        s.fontSize = defaultFontSize;
        s.fontStyle = FontStyle.Italic;
        s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = new Color(1.0f, 1.0f, 1.0f, 0.6f);
        s.richText = true;
        windowStyle_SmallText = s;

        s = new GUIStyle(EditorStyles.label);
        s.fontStyle = FontStyle.Italic;
        s.alignment = TextAnchor.MiddleCenter;
        s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = new Color(1.0f, 1.0f, 1.0f, 0.6f);
        s.richText = true;
        windowStyle_SmallCenteredText = s;

        // Unselected Script Style
        s = new GUIStyle(EditorStyles.boldLabel);
        s.padding = new RectOffset(5, 0, 0, 0);
        s.fontSize = 13;
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleLeft;
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(new Color(0.15f, 0.15f, 0.15f));
        s.active.background = s.focused.background = MakeTexture(new Color(0.15f, 0.15f, 0.15f));
        s.active.background = s.focused.background = MakeTexture(new Color(0.15f, 0.15f, 0.15f));
        unselectedScriptStyle = s;

        // Unselected Script Style
        s = new GUIStyle(EditorStyles.boldLabel);
        s.padding = new RectOffset(5, 0, 0, 0);
        s.fontSize = 13;
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleLeft;
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(new Color(0.3f, 0.3f, 0.3f));
        s.active.background = s.focused.background = MakeTexture(new Color(0.3f, 0.3f, 0.3f));
        s.active.background = s.focused.background = MakeTexture(new Color(0.3f, 0.3f, 0.3f));
        unselectedScriptStyle2 = s; 

        // Selected Script Style
        s = new GUIStyle(EditorStyles.boldLabel);
        s.padding = new RectOffset(5, 0, 0, 0);
        s.fontSize = 13;
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleLeft;
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(new Color(.17f, .36f, .53f, 1f));
        selectedScriptStyle = s;

        // 
        s = new GUIStyle(GUI.skin.button);
        s.fontSize = 20;
        s.fontStyle = FontStyle.Bold;
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = Color.white;
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(new Color(1, 1, 1, 0.1f));
        s.hover.background = MakeTexture(new Color(1, 1, 1, 0.3f));
        s.active.background = s.focused.background = MakeTexture(new Color(1, 1, 1, 0.4f));
        s.hover.textColor = s.active.textColor = s.normal.textColor;
        s.normal.scaledBackgrounds = s.hover.scaledBackgrounds = s.active.scaledBackgrounds = null;
        windowStyle_AddButton = s;

        //
        s = new GUIStyle(GUI.skin.button);
        s.fontSize = 14;
        s.fontStyle = FontStyle.Bold;
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = new Color(0.9f, 0.9f, 0.9f);
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(new Color(0.2f, 0.2f, 0.2f, 1.0f));
        s.hover.background = MakeTexture(new Color(1, 1, 1, 0.3f));
        s.active.background = s.focused.background = MakeTexture(new Color(1, 1, 1, 0.4f));
        s.hover.textColor = s.active.textColor = s.normal.textColor;
        s.normal.scaledBackgrounds = s.hover.scaledBackgrounds = s.active.scaledBackgrounds = null;
        windowStyle_AddButtonSmall = s;

        //
        s = new GUIStyle(GUI.skin.textField);
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(new Color(0.2f, 0.2f, 0.2f, 1.0f));
          s.active.background = s.focused.background = MakeTexture(new Color(0.2f, 0.2f, 0.2f, 1.0f));
        s.hover.textColor = s.active.textColor = s.normal.textColor;
        s.alignment = TextAnchor.MiddleCenter;
        windowStyle_TextField = s;

        // Variable Nodes
        s = new GUIStyle(GUI.skin.button);
        s.fontSize = defaultFontSize;
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = new Color(0.5f, 0.5f, 1.0f);
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(oddNodeColor);
        s.hover.textColor = s.active.textColor = s.normal.textColor;
        s.normal.scaledBackgrounds = s.hover.scaledBackgrounds = s.active.scaledBackgrounds = null;
        windowStyle_VariableText = s;

        // Value Nodes
        s = new GUIStyle(GUI.skin.button);
        s.fontSize = defaultFontSize;
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = new Color(0.1f, 1.0f, 0.5f);
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(oddNodeColor);
        s.hover.textColor = s.active.textColor = s.normal.textColor;
        s.richText = true;
        s.normal.scaledBackgrounds = s.hover.scaledBackgrounds = s.active.scaledBackgrounds = null;
        windowStyle_ValueText = s;

        // Temp Nodes
        s = new GUIStyle(GUI.skin.button);
        s.fontSize = defaultFontSize;
        s.normal = s.hover = s.focused = s.active = s.onNormal;
        s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = new Color(1.0f, 0.5f, 0.5f);
        s.normal.background = s.hover.background = s.focused.background = s.active.background = MakeTexture(oddNodeColor);
        s.hover.textColor = s.active.textColor = s.normal.textColor;
        s.normal.scaledBackgrounds = s.hover.scaledBackgrounds = s.active.scaledBackgrounds = null;
        windowStyle_TempText = s;

        // Skin for odd nodes.
        GUIStyle odd = new GUIStyle(GUI.skin.button);
        odd.fontSize = defaultFontSize;
        odd.fontStyle = FontStyle.Bold;
        odd.normal = odd.hover = odd.focused = odd.active = odd.onNormal;
        odd.normal.textColor = odd.hover.textColor = odd.focused.textColor = odd.active.textColor = Color.white;
        odd.normal.background = odd.hover.background = odd.focused.background = odd.active.background = MakeTexture(oddNodeColor);
        odd.hover.textColor = odd.active.textColor = odd.normal.textColor;
        odd.normal.scaledBackgrounds = odd.hover.scaledBackgrounds = odd.active.scaledBackgrounds = null;
        windowStyle_OddNode = odd;

        // Skin for even nodes.
        GUIStyle even = new GUIStyle(GUI.skin.button);
        even.fontSize = defaultFontSize;
        even.fontStyle = FontStyle.Bold;
        even.normal = even.hover = even.focused = even.active = even.onNormal;
        even.normal.textColor = even.hover.textColor = even.focused.textColor = even.active.textColor = Color.white;
        even.normal.background = even.hover.background = even.focused.background = even.active.background = MakeTexture(evenNodeColor);
        even.hover.textColor = even.active.textColor = even.normal.textColor;
        even.normal.scaledBackgrounds = even.hover.scaledBackgrounds = even.active.scaledBackgrounds = null;
        windowStyle_EvenNode = even;

        // Skin for hovered nodes.
        GUIStyle hov = new GUIStyle(GUI.skin.button);
        hov.fontSize = defaultFontSize;
        hov.fontStyle = FontStyle.Bold;
        hov.normal = odd.hover = hov.focused = hov.active = hov.onNormal;
        hov.normal.textColor = hov.hover.textColor = hov.focused.textColor = hov.active.textColor = Color.white;
        hov.normal.background = hov.hover.background = hov.focused.background = hov.active.background = MakeTexture(hoveredNodeColor);
        hov.hover.textColor = hov.active.textColor = hov.normal.textColor;
        hov.normal.scaledBackgrounds = hov.hover.scaledBackgrounds = hov.active.scaledBackgrounds = null;
        windowStyle_HoveredNode = hov;
    }

    private static Texture2D MakeTexture(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }


    public float DrawHorizontalScriptPanel (float currentX, float currentY, float width)
    {
        //string[] toolbarOptions = new string[] { "Option 1", "Option 2", "Option 3" };
        //GUILayout.BeginArea(new Rect(area.x, area.y, 1000, 30));
        //int toolbarIndex = GUILayout.Toolbar(0, toolbarOptions); 
        //GUILayout.EndArea();

        EditorGUI.DrawRect(new Rect(currentX, currentY, width, 34), new Color(0.15f, 0.15f, 0.15f, 1));

        if (engine.scripts.Count == 0)
        {
            engine.scripts.Add(new LogicScript("Main"));
        }
        if (engine.selectedScript == null) engine.selectedScript = engine.scripts[0];

        //return;

        Rect rect = new Rect(currentX + 2, currentY + 2, 0, 30);

        Event current = Event.current;
        for (int i = 0; i < engine.scripts.Count; i++)
        //for (int i = 0; i < 3; i++)
        {
            rect.width = LogicEngineEditor.windowStyle_BodyText.CalcSize(new GUIContent(engine.scripts[i].scriptName)).x + 31;

            //Rect rect = new Rect(area.x + i * (140 + 2) + 2, area.y + 2, 140, 30);
            if (engine.scripts[i].scriptName == engine.selectedScript.scriptName)
                GUI.Label(rect, "", selectedScriptStyle);
            else
                GUI.Label(rect, "", unselectedScriptStyle2);

            if (scriptIconSprite == null)
                scriptIconSprite = Resources.Load<Sprite>("Icons/Script");
            if (scriptIconSprite != null)
                GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 5, 20, 20), scriptIconSprite.texture);



            Rect textRect = new Rect(rect);
            textRect.x += 25;
            textRect.width -= 25;
            GUI.Label(textRect, engine.scripts[i].scriptName, LogicEngineEditor.windowStyle_BodyText);

            if (rect.Contains(current.mousePosition))
            {

                if (current.type == EventType.MouseDown)
                {
                    engine.selectedNodes.Clear();
                    SetSelectedScript(engine.scripts[i]);
                    isDraggingScript = true;
                    scriptBeingDragged = engine.scripts[i];
                }
                if (current.type == EventType.ContextClick)
                {
                    engine.selectedScript = engine.scripts[i];
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Rename"), false, () => { RenameScript(engine.selectedScript); });
                    menu.AddItem(new GUIContent("Delete"), false, () => { DeleteScript(); });
                    menu.ShowAsContext();
                    current.Use();
                }
            }
            rect.x += rect.width + 2;
        }
        rect.width = 88;
        if (GUI.Button(rect, "Create New", unselectedScriptStyle2))
        {
            CreateScript();
        }

        return 34;
    }

    /// <summary>
    /// Draw the script panel for the logic engine.
    /// </summary>
    public void DrawScriptPanel(Rect area)
    {
        EditorGUI.DrawRect(area, new Color(0f, 0f, 0f)); 

        float totalHeight = area.height;
        bool needToDrawScriptDragGuide = false;
        Rect scriptDragGuideLocation = new Rect();
        bool dragIsInTopHalf = true;



        // Draw the header
        area.height = 25;
        Rect labelRect = new Rect(area);
        labelRect.x += 5;
        EditorGUI.DrawRect(area, new Color(0.1f, 0.1f, 0.1f, 1));

        Rect bodyStart = new Rect(area);
        bodyStart.y += 25;

        GUIStyle transparentBtn = new GUIStyle(GUI.skin.box);

        // Draw the button that allows users to create scripts.
        if (GUI.Button(new Rect(area.xMax - 25, area.y - 1, 25, 24), "+", windowStyle_AddButton))
        {
            CreateScript();
        }

        // Draw the title Text
        EditorGUI.LabelField(labelRect, "Logic", windowStyle_HeaderText);

        // Draw the background panel
        area.y += area.height;
        area.height = totalHeight - area.height;
        //EditorGUI.DrawRect(area, new Color(0.5f, 0.5f, 0.5f, .2f));
        EditorGUI.DrawRect(area, new Color(0.3f, 0.3f, 0.3f));
        //bodyStart.y += 1;
        bodyStart.height = 30;
        for (int i = 0; i < engine.scripts.Count; i++)
        {
            if (engine.scripts[i].scriptName == engine.selectedScript.scriptName)
                GUI.Label(bodyStart, engine.scripts[i].scriptName, selectedScriptStyle);
            else
                GUI.Label(bodyStart, engine.scripts[i].scriptName, unselectedScriptStyle); 



            Event current = Event.current;
            if (bodyStart.Contains(current.mousePosition))
            {
                if (isDraggingScript && scriptBeingDragged != engine.scripts[i])
                {
                    Rect topHalf = new Rect(bodyStart);
                    topHalf.height = bodyStart.height / 2.0f;

                    Rect lineRect = new Rect(bodyStart);
                    lineRect.height = 2;

                    

                    if (!topHalf.Contains(current.mousePosition))
                    {
                        dragIsInTopHalf = false;
                        lineRect.y += bodyStart.height;
                    }
                    needToDrawScriptDragGuide = true;
                    scriptDragGuideLocation = lineRect;
                }

                if (current.type == EventType.MouseUp && isDraggingScript)
                {
                    LogicScript target = engine.scripts[i];
                    if (scriptBeingDragged != target)
                    {
                        
                        engine.scripts.Remove(scriptBeingDragged);
                        if (dragIsInTopHalf)
                            engine.scripts.Insert(engine.scripts.IndexOf(target), scriptBeingDragged);
                        else
                            engine.scripts.Insert(engine.scripts.IndexOf(target) + 1, scriptBeingDragged);

                    }
                    isDraggingScript = false;
                    scriptBeingDragged = null;
                }

                if (current.type == EventType.MouseDown)
                {
                    engine.selectedNodes.Clear();
                    SetSelectedScript(engine.scripts[i]);
                    isDraggingScript = true;
                    scriptBeingDragged = engine.scripts[i];
                }
                if (current.type == EventType.ContextClick)
                {
                    engine.selectedScript = engine.scripts[i];
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Rename"), false, () => { RenameScript(engine.selectedScript); });
                    menu.AddItem(new GUIContent("Delete"), false, () => { DeleteScript(); });
                    menu.ShowAsContext();
                    current.Use();
                }
            }
            bodyStart.y += 31;
        }

        Rect unfocusRect = new Rect(area);
        unfocusRect.y = bodyStart.y;
        unfocusRect.height = (area.height - bodyStart.y + area.y);

 
        if (needToDrawScriptDragGuide)
            EditorGUI.DrawRect(scriptDragGuideLocation, Color.grey);

        if (GUI.Button(unfocusRect, "", GUIStyle.none))
            GUI.FocusControl(null);
    }



    /// <summary>
    /// Create a new script for the current engine.
    /// </summary>
    public void CreateScript()
    {
        string scriptName = "Script " + Random.Range(10000, 99999);
        if ((scriptName = EditorInputDialog.Show("Enter Script Name", "", scriptName)) != null)
        {
            engine.scripts.Add(new LogicScript(scriptName));
            engine.selectedScript = engine.scripts[engine.scripts.Count - 1];
            EditorUtility.SetDirty(engineHandler.GetData());

            
            engine.selectedNodes.Clear();
        }
    }

    /// <summary>
    /// Prompt for a rename of the given script.
    /// </summary>
    public void RenameScript(LogicScript script)
    {
        string newText = EditorInputDialog.Show("Enter Script Name", "", script.scriptName);
        if (newText != null)
        {
            script.scriptName = newText;
            EditorUtility.SetDirty(engineHandler.GetData());
            AssetDatabase.SaveAssetIfDirty(engineHandler.GetData());
        }
    }

    /// <summary>
    /// Delete the selected script from the given engine.
    /// </summary>
    public void DeleteScript()
    {
        engine.scripts.Remove(engine.selectedScript);
        if (engine.scripts.Count == 0)
            engine.scripts.Add(new LogicScript("Main"));
        engine.selectedScript = engine.scripts[0];
        engine.selectedNodes.Clear();
        EditorUtility.SetDirty(engineHandler.GetData());
        AssetDatabase.SaveAssetIfDirty(engineHandler.GetData());
    }



    public static Vector2 mousePosition;



    private List<GeneralNode> GetNodeList (GeneralNode node)
    {
        List<GeneralNode> nodeList = null;
        if (engine.selectedScript.eventNodes.Contains(node))
            nodeList = engine.selectedScript.eventNodes;
        else if (engine.selectedScript.conditionNodes.Contains(node))
            nodeList = engine.selectedScript.conditionNodes;
        else if (engine.selectedScript.actionNodes.Contains(node)) 
            nodeList = engine.selectedScript.actionNodes;
        return nodeList;
    }

    private List<GeneralNode> GetAllChildNodes (GeneralNode node)
    {
        List<GeneralNode> childNodes = new List<GeneralNode>();
        List<GeneralNode> nodeList = GetNodeList(node);
        int startID = nodeList.IndexOf(node) + 1;
        for (int i = startID; i < nodeList.Count; i++)
        {
            if (nodeList[i].indent > node.indent)
                childNodes.Add(nodeList[i]);
            else
                break;
        }
        return childNodes;
    }

    private bool IsParentChild (GeneralNode child, GeneralNode parent)
    {
        List<GeneralNode> nodeList = GetNodeList(parent);
        int startID = nodeList.IndexOf(parent) + 1;
        for (int i = startID; i < nodeList.Count; i++)
        {
            if (nodeList[i].indent <= parent.indent)
            {
                return false;
            }
            else if (nodeList[i] == child)
            {
                return true;
            }
        }
        return false;
    }

    private void MoveSelectedToNode (GeneralNode moveToNode, bool insertBefore)
    {
        if (GetNodeList(moveToNode) != GetNodeList(engine.selectedNodes[0])) return;
        if (engine.selectedNodes.Count == 0) return;
        if (IsParentChild(moveToNode, engine.selectedNodes[0])) return;

        GeneralNode selectedNode = engine.selectedNodes[0];
        List<GeneralNode> nodeList = GetNodeList(selectedNode);
        List<GeneralNode> nodesToMove = new List<GeneralNode> { selectedNode };
        nodesToMove.AddRange(GetAllChildNodes(selectedNode));

        // Update the node indents.
        int change = moveToNode.indent - selectedNode.indent;
        if (!insertBefore && moveToNode is NestingActionNode) change++;
        foreach (GeneralNode node in nodesToMove)
            node.indent += change;

        // Remove the nodes from the current list.
        foreach (GeneralNode node in nodesToMove)
            nodeList.Remove(node);

        // Insert the nodes at the correct location.
        int insertID = nodeList.IndexOf(moveToNode);
        if (!insertBefore) insertID++;
        nodeList.InsertRange(insertID, nodesToMove);
    }

    private void DeleteAllSelectedNodes()
    {
        // Add the node and all children.
        HashSet<GeneralNode> toDeleteSet = new HashSet<GeneralNode>(engine.selectedNodes);
        foreach (GeneralNode node in engine.selectedNodes)
            toDeleteSet.AddRange(GetAllChildNodes(node));

        // Delete all the nodes.
        foreach (GeneralNode node in toDeleteSet)
        {
            engine.selectedScript.eventNodes.Remove(node);
            engine.selectedScript.conditionNodes.Remove(node);
            engine.selectedScript.actionNodes.Remove(node);
        }
        EditorUtility.SetDirty(engineHandler.GetData());
    }

    public void AddNode(List<GeneralNode> nodeList, GeneralNode nodeToAdd)
    {
        GeneralNode copy = nodeToAdd.Copy();
        nodeList.Add(copy);
        engine.selectedNodes = new List<GeneralNode>() { copy };
        EditorUtility.SetDirty(engineHandler.GetData());
    } 

    public void AddNode(List<GeneralNode> nodeList, GeneralNode nodeToAdd, int insertID, int indent)
    {
        GeneralNode node = nodeToAdd.Copy();
        node.indent = indent;
        nodeList.Insert(insertID, node);
        EditorUtility.SetDirty(engineHandler.GetData());
    }

    public static void BuildAllData()
    {
        allNodes.Clear();
        BuildStyles();
        BuildValueNodes();
    }

    public void Process ()
    {
        if (allNodes.Count == 0 || windowStyle_AddButton == null || windowStyle_AddButton.normal == null || windowStyle_AddButton.normal.background == null || windowStyle_AddButton.normal.background.width > 1)
            BuildAllData();

        mousePosition = Event.current.mousePosition;

        if (Event.current.isKey && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Delete)
        {
            DeleteAllSelectedNodes();
        }

        if (engine == null)
            Debug.Log("Engine is null");

        if (engine.selectedScript == null)
        {
            if (engine.scripts.Count == 0)
            {
                engine.scripts.Add(new LogicScript("Main"));
            }
            engine.selectedScript = engine.scripts[0];
        }
    }

    public void DrawNodes ()
    {
        float width = window.position.width - 180;
        float currentX = 170;
        float currentY = 10;

        //DrawScriptPanel(new Rect(10, 10, 150, window.position.height - 20));


        Color eventColor = new Color(0.2f, 0.1f, 0.1f, 1);
        Color conditionColor = new Color(0.1f, 0.2f, 0.1f, 1);
        Color actionColor = new Color(0.1f, 0.1f, 0.2f, 1);
        currentY += DrawHorizontalScriptPanel(currentX, currentY, width);
        currentY += 3;
        currentY += DrawNodeList(currentX, currentY, width, "Events: ", engine.selectedScript.eventNodes, eventColor, typeof(EventNode));
        currentY += 10;
        currentY += DrawNodeList(currentX, currentY, width, "Conditions: ", engine.selectedScript.conditionNodes, conditionColor, typeof(BoolNode));
        currentY += 10;
        currentY += DrawNodeList(currentX, currentY, width, "Actions: ", engine.selectedScript.actionNodes, actionColor, typeof(ActionNode));
        currentY += 10;
    }

    public float contentHeight = 0;
    public Vector2 scrollPos;

    public void DrawNodes(Rect rect)
    {
        //float width = window.position.width - 180;
        //float currentX = 170;
        //float currentY = 10;
        float width = rect.width;
        float currentX = rect.x;
        float currentY = rect.y;

        //DrawScriptPanel(new Rect(10, 10, 150, window.position.height - 20));

        if (engine.selectedScript == null)
            engine.selectedScript = engine.scripts[0];

        float maxWidth = 0;
        foreach (GeneralNode node in engine.selectedScript.actionNodes)
        {
            maxWidth = Mathf.Max(maxWidth, GetDrawWidth(node) + (node.indent-1) * indentWidth); 
        }


        scrollPos = GUI.BeginScrollView(rect, scrollPos, new Rect(rect.x, rect.y, Mathf.Max(maxWidth + 50, rect.width - 15), contentHeight));

        if (contentHeight > rect.height)
            width -= 10;

        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.C && (e.control || e.command))
        {
            CopySelectedNode();
            e.Use();
        }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.V && (e.control || e.command))
        {
            PasteNodesInClipboard();
            e.Use();
        }

        Color eventColor = new Color(0.2f, 0.1f, 0.1f, 1);
        Color conditionColor = new Color(0.1f, 0.2f, 0.1f, 1);
        Color actionColor = new Color(0.1f, 0.1f, 0.2f, 1);
        currentY += DrawHorizontalScriptPanel(currentX, currentY, width);
        currentY += 2;
        currentY += DrawNodeList(currentX, currentY, width, "Events: ", engine.selectedScript.eventNodes, eventColor, typeof(EventNode));
        currentY += 2;
        currentY += DrawNodeList(currentX, currentY, width, "Conditions: ", engine.selectedScript.conditionNodes, conditionColor, typeof(BoolNode));
        currentY += 2;
        currentY += DrawNodeList(currentX, currentY, width, "Actions: ", engine.selectedScript.actionNodes, actionColor, typeof(ActionNode));
        currentY += 2;

        GUI.EndScrollView();
        contentHeight = currentY;
    }

    private int MaxChildDepth (GeneralNode node)
    {
        if (node.returnType != GeneralNode.ReturnType.Function)
        {
            return 1;
        }
        int depth = 0;
        for (int i = 0; i < node.functionEvaluators.Length; i++)
        {
            depth = Mathf.Max(depth, MaxChildDepth(node.functionEvaluators[i]));          
        }
        return depth + 1;
    }

    public float DrawNodeList(float x, float y, float width, string title, List<GeneralNode> nodes, Color headerColor, Type type)
    {

        Color[] colors = new Color[2] { new Color(0.5f, 0.5f, 0.5f, .2f), new Color(0.5f, 0.5f, 0.5f, .1f) };
        //float itemHeight = 40;

        //Rect r = new Rect(x, y, width, 0);
        Rect r = new Rect(x, y, width + scrollPos.x, 0); 

        // Draw the header
        r.height = 25;
        Rect labelRect = new Rect(r);
        labelRect.x += 5;
        EditorGUI.DrawRect(r, headerColor);
        EditorGUI.LabelField(labelRect, title, windowStyle_HeaderText);


        if (GUI.Button(new Rect(r.xMax - 25, r.y - 1, 25, 24), "+", windowStyle_AddButton))
        {
            GenericMenu menu = new GenericMenu();
            foreach (GeneralNode node in allNodes)
            {
                if (type.IsAssignableFrom(node.GetType()))
                {
                    menu.AddItem(new GUIContent(node.functionDescription), false,
                () => { AddNode(nodes, node); });
                }
            }
            menu.ShowAsContext();
        }
        r.y += r.height;

        //EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.Link);
        //EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.ArrowPlus);

        bool needToDrawDragGuide = false;
        
        Rect dragGuideLocation = new Rect();
        

        // Draw each node.
        Event current = Event.current;
        int currNode = 0;
        GeneralNode hovered = null;

        

        for (int i = 0; i < nodes.Count; i++)
        {
            r.height = Mathf.Max(itemHeight, (MaxChildDepth(nodes[i]) + 1) * heightIncreasePerDepth);
            
            Rect edge = new Rect(r);
            edge.x += nodes[i].indent * indentWidth;
            edge.width -= nodes[i].indent * indentWidth;
            if (engine.selectedNodes.Contains(nodes[i]))
                EditorGUI.DrawRect(edge, (new Color(.17f, .36f, .53f, 1f)));
            else
                EditorGUI.DrawRect (edge, colors[currNode % 2]);
            //nodes[i].Draw(window, edge);

            Rect iconRect = new Rect(edge.x + 5, edge.y + (r.height - 20) / 2.0f, 20, 20); 
            //edge.x += 40;

            //string defaultIconPath = "Assets/Core/Scripts/Visual Coding/Icons";
            //EditorGUIUtility.FindTexture()
            if (!icons.ContainsKey(nodes[i].nodeIcon))
            {
                if (nodes[i].nodeIcon == "")
                {
                    icons.Add(nodes[i].nodeIcon, (Resources.Load<Sprite>("Icons/DefaultIcon")).texture); 
                    //icons.Add(nodes[i].nodeIcon, Resources.Load("Icons/" +  EditorGUIUtility.FindTexture($"{defaultIconPath}/DefaultIcon.png"));
                }
                else
                {
                    //icons.Add(nodes[i].nodeIcon, EditorGUIUtility.FindTexture($"{defaultIconPath}/{nodes[i].nodeIcon}.png"));
                    Sprite sprite = Resources.Load<Sprite>($"Icons/{nodes[i].nodeIcon}");
                    if (sprite != null)
                    {
                        icons.Add(nodes[i].nodeIcon, sprite.texture);
                    }
                    else
                    {
                        Debug.Log(nodes[i].nodeIcon + " Can't be found!");
                    }

                    

                    //icons.Add(nodes[i].nodeIcon, EditorGUIUtility.FindTexture($"{defaultIconPath}/{nodes[i].nodeIcon}.png"));
                }
            }

            //if (nodes[i].nodeIcon == "")
            //    icon = EditorGUIUtility.FindTexture($"{defaultIconPath}/DefaultIcon.png");
            //else
            //    icon = EditorGUIUtility.FindTexture($"{defaultIconPath}/{nodes[i].nodeIcon}.png");

            GUI.DrawTexture(iconRect, icons[nodes[i].nodeIcon]);

            //EditorGUI.DrawTextureTransparent(iconRect, icon, ScaleMode.ScaleToFit, 0, 1, UnityEngine.Rendering.ColorWriteMask.All);

            //EditorGUI.DrawPreviewTexture(iconRect, icon);

            Rect newEdge = new Rect(edge);
            newEdge.x += 25;
            Draw(newEdge, nodes[i]);
            


            

            

            if (edge.Contains(current.mousePosition))
            {
                hovered = nodes[i];
                if (isDragging && (engine.selectedNodes.Count > 1 || engine.selectedNodes[0] != nodes[i]))
                {
                    Rect topHalf = new Rect(edge);
                    topHalf.height = edge.height / 2.0f;

                    Rect dragGuide = new Rect(edge);
                    dragGuide.height = 2;

                    if (topHalf.Contains(current.mousePosition))
                    {
                    }
                    else
                    {
                        dragGuide.y += 50;
                    }

                    if (current.type == EventType.MouseUp && isDragging)
                    {
                        isDragging = false;
                        MoveSelectedToNode(nodes[i], topHalf.Contains(current.mousePosition));
                    }

                    //Rect dragGuide = new Rect(edge);
                    //dragGuide.height = 2;
                    needToDrawDragGuide = true;
                    dragGuideLocation = dragGuide;
                    
                }

                /*
                if (current.type == EventType.MouseUp && isDragging)
                {
                    isDragging = false;
                    if (dragStartedAt != nodes[i])
                    {
                        // Store the target.
                        GeneralNode target = nodes[i];

                        // If one of the selected items is the drop target, remove it from the list.
                        engine.selectedNodes.Remove(nodes[i]);

                        // Remove all items from the node list.
                        foreach (GeneralNode node in engine.selectedNodes)
                        {
                            nodes.Remove(node);
                        }

                        // Find the ID of the target again now that items have been removed.
                        int insertID = nodes.IndexOf(target);

                        Rect above = new Rect(edge);
                        above.height = edge.height / 2.0f;
                        if (above.Contains(current.mousePosition))
                        {
                            nodes.InsertRange(insertID, engine.selectedNodes);
                            foreach (GeneralNode node in engine.selectedNodes)
                            {
                                node.indent = nodes[insertID].indent;
                            }
                        }
                        else
                        {
                            nodes.InsertRange(insertID + 1, engine.selectedNodes);
                            foreach (GeneralNode node in engine.selectedNodes)
                            {
                                node.indent = (nodes[insertID].CanHaveChildren() ? node.indent = nodes[insertID].indent + 1 : node.indent = nodes[insertID].indent);
                            }
                        }
                    }
                }
                */
                if (current.type == EventType.MouseDown)
                {
                    GUI.FocusControl(null);
                    isDragging = true;
                    dragStartedAt = nodes[i];
                    if (current.control)
                    {
                        if (engine.selectedNodes.Contains(nodes[i]))
                            engine.selectedNodes.Remove(nodes[i]);
                        else
                            engine.selectedNodes.Add(nodes[i]);
                    }
                    else
                    {
                        engine.selectedNodes.Clear();
                        engine.selectedNodes.Add(nodes[i]);
                        //DragAndDrop.StartDrag("NodeDragging");

                        //DragAndDrop.SetGenericData("NodeDragging", selectedNodes);
                        //DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                    }
                }
                if (current.type == EventType.ContextClick)
                {
                    GUI.FocusControl(null);
                    engine.selectedNodes.Clear();
                    engine.selectedNodes.Add(nodes[i]);
                    window.Repaint();
                    GenericMenu menu = new GenericMenu();
                    Vector2 temp = current.mousePosition + window.position.position;
                    //menu.AddItem(new GUIContent("Edit"), false, () => {
                    //    EditNode(engine.selectedNodes[0]);
                    //});
                    menu.AddItem(new GUIContent("Copy"), false, () => { CopySelectedNode(); });
                    menu.AddItem(new GUIContent("Delete"), false, () => { DeleteAllSelectedNodes(); });
                    menu.ShowAsContext();
                    current.Use();
                }

            }
            

            r.y += r.height;
            currNode++;

            if (nodes[i] is NestingActionNode && (i == nodes.Count - 1 || nodes[i + 1].indent != nodes[i].indent + 1))
            {
                edge.y += r.height;
                edge.x += indentWidth;
                edge.width -= indentWidth;
                EditorGUI.DrawRect(edge, colors[currNode % 2]);
                labelRect = new Rect(edge);
                labelRect.x += 15;

                EditorGUI.LabelField(labelRect, "No actions specified", windowStyle_SmallText);
                currNode++;
                r.y += r.height;
            }
        }

        if (current.type == EventType.MouseDown)
        {
            //isDragging = false;
        }

        if (current.type == EventType.MouseUp && isDragging && nodes.Contains(engine.selectedNodes[0]))
        {
            isDragging = false;
        }

        if (needToDrawDragGuide)
            EditorGUI.DrawRect(dragGuideLocation, Color.grey);

        // If no nodes exist, draw a temporary node.
        if (nodes.Count == 0)
        {
            r.height = itemHeight;
            labelRect = new Rect(r);
            labelRect.x += 15;
            EditorGUI.DrawRect(r, colors[0]);
            EditorGUI.LabelField(labelRect, "None specified", windowStyle_SmallText);
            r.y += r.height;
        }

        

        Rect focusRect = new Rect(x, y, width, r.y);
        Event current2 = Event.current;
        if (focusRect.Contains(current2.mousePosition) && current2.type == EventType.MouseDown)
            GUI.FocusControl(null);

        return r.y - y;
    }

    private void PasteNodesInClipboard ()
    {
        if (nodesInClipboard.Count == 0) return;

        foreach (GeneralNode node in nodesInClipboard)
        {
            AddNode(engine.selectedScript.actionNodes, node);
            //engine.selectedNodes = new List<GeneralNode>() { copy }; 
        }
    }

    private void CopySelectedNode ()
    {
        // No nodes to copy.
        if (engine.selectedNodes.Count == 0) return;

        // Not an action node or 
        if (!engine.selectedScript.actionNodes.Contains(engine.selectedNodes[0])) return;

        nodesInClipboard = new List<GeneralNode>();

        GeneralNode nodeToCopy = engine.selectedNodes[0];
        int offsetChange = 0;
        bool foundNode = false;
        foreach (GeneralNode node in engine.selectedScript.actionNodes)
        {
            if (foundNode && node.indent > nodeToCopy.indent)
            {
                GeneralNode copy = node.Copy();
                copy.indent += offsetChange;
                nodesInClipboard.Add(copy);
            }
            else if (foundNode && node.indent <= nodeToCopy.indent)
            {
                break;
            }
            else if (node == nodeToCopy)
            {
                foundNode = true;
                GeneralNode copy = node.Copy();
                offsetChange = -copy.indent;
                copy.indent = 0;
                nodesInClipboard.Add(copy);
            }
        }
    }

    public virtual float GetDrawWidth(GeneralNode node)
    {
        GUIContent content = null;
        if (node.returnType == GeneralNode.ReturnType.Function)
        {
            float width = 0;
            string[] parts = node.functionDynamicDescription.Split('$');
            foreach (string part in parts)
            {
                if (part.Length > 0)
                {
                    content = new GUIContent(part);
                    width += windowStyle_BaseText.CalcSize(content).x;
                }
            }
            foreach (GeneralNode n in node.functionEvaluators)
            {
                width += GetDrawWidth(n);
            }
            return width + 10;
        }
        if (node.returnType == GeneralNode.ReturnType.Value) { content = new GUIContent(node.ToString()); }
        if (node.returnType == GeneralNode.ReturnType.Temp) { content = new GUIContent(node.tempName); }
        if (node.returnType == GeneralNode.ReturnType.Preset) { content = new GUIContent(node.presetName); }
        return windowStyle_HoveredNode.CalcSize(content).x;
    }

    public virtual float Draw(Rect rect, GeneralNode node, int depth = 1)
    {
        // Determine the correct style for the node.
        GUIStyle style = null;
        if (node == hoveredNode)
            style = windowStyle_HoveredNode;
        else if (depth % 2 == 1)
            style = windowStyle_OddNode;
        else
            style = windowStyle_EvenNode;

        // Determine the correct width for the node.
        Rect temp = new Rect(rect);
        temp.width = GetDrawWidth(node);
        temp.height -= depth * 2;
        temp.y += depth * 1;

        GUIContent content = null;
        if (node is EventNode || (node.returnType == GeneralNode.ReturnType.Function && (node.functionEvaluators.Length > 0 || depth == 1)))


        //if (node is EventNode || (node.returnType == GeneralNode.ReturnType.Function))
        {
            if (depth == 1)
            {
                temp.y -= 1;
            }

            // Draw the function node button (but not if it is a base node).
            if (depth != 1)
            {
                EditorGUIUtility.AddCursorRect(temp, MouseCursor.Link);

                // Check to see if the mouse is currently hovering over this node. If so,
                // it should be drawn in a different colour.
                if (temp.Contains(Event.current.mousePosition))
                {
                    if (hoveredNode == null || depth > hoveredNodeDepth)
                    {
                        hoveredNode = node;
                        hoveredNodeDepth = depth;
                    }
                }
                else
                {
                    if (node == hoveredNode)
                    {
                        hoveredNode = null;
                    }
                }

                // Draw the button to allow for edits.    
                if (hoveredNode == node)
                {
                    if (GUI.Button(temp, "", style))
                    {
                        SetValueEditor.OpenWindow(window,  window.position.min - scrollPos + new Vector2(temp.xMin, temp.yMin + temp.height * 1.5f), node, engineHandler);
                    }
                }
                else
                {
                    GUI.Label(temp, "", style);
                }
            }

            // If the node can have children actions, create the add button.
            if (node.CanHaveChildren())
            {
                if (GUI.Button(new Rect(rect.x + rect.width - 75, rect.y - 1, 49, 49), "+", windowStyle_AddButton))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (GeneralNode n in allNodes)
                    {
                        if (n is ActionNode)
                        {
                            menu.AddItem(new GUIContent(n.functionDescription), false, () => { AddNodeAsChild(node, n); });
                        }
                    }
                    menu.ShowAsContext();
                }
            }

            // Draw each part of the function node.
            temp.x += 5;
            string[] parts = node.functionDynamicDescription.Split('$');
            int next = 0;
            foreach (string part in parts)
            {
                content = new GUIContent(part);
                temp.width = windowStyle_BaseText.CalcSize(content).x;
                GUI.Label(temp, part, windowStyle_BaseText);
                temp.x += temp.width;
                if (next < node.functionEvaluators.Length && node.functionEvaluators.Length > 0)
                {
                    // temp.x += node.functionEvaluators[next].Draw(window, temp, depth + 1);

                    temp.x += Draw(temp, node.functionEvaluators[next], depth + 1);
                    next++;
                }
            }
            return GetDrawWidth(node);
        }

        // Assign the appropriate text and color for the node.
        if (node.returnType == GeneralNode.ReturnType.Function && node.functionEvaluators.Length == 0)
            temp.width = CalcContentWidth(style, content = new GUIContent(node.functionDynamicDescription), varColor);
        else if (node.returnType == GeneralNode.ReturnType.Value)
            temp.width = CalcContentWidth(style, content = new GUIContent(node.ToString()), valueColor);
        else if (node.returnType == GeneralNode.ReturnType.Preset)
            temp.width = CalcContentWidth(style, content = new GUIContent(node.presetName), presetColor);
        else if (node.returnType == GeneralNode.ReturnType.Temp)
            temp.width = CalcContentWidth(style, content = new GUIContent(node.tempName), tempColor);
        EditorGUIUtility.AddCursorRect(temp, MouseCursor.Link);

        style.richText = true;

        // Check to see if the mouse is currently hovering over this node. If so,
        // it should be drawn in a different colour.
        if (temp.Contains(Event.current.mousePosition))
        {
            if (hoveredNode == null || depth > hoveredNodeDepth)
            {
                hoveredNode = node;
                hoveredNodeDepth = depth;
            }
        }
        else
        {
            if (node == hoveredNode)
            {
                hoveredNode = null;
            }
        }

        // Draw the node as a button (if it is hovered), or a label which looks like a button if not.
        // This is needed to get correct clicks on the window. 
        if (hoveredNode == node)
        {
            if (GUI.Button(temp, content, style))
            {
                SetValueEditor.OpenWindow(window, window.position.min - scrollPos + new Vector2(temp.xMin, temp.yMin + temp.height * 1.5f), node, engineHandler);
            }
        }
        else
        {
            GUI.Label(temp, content, style);
        }

        


        return temp.width;


    }

    private float CalcContentWidth(GUIStyle style, GUIContent content, Color color)
    {
        style.normal.textColor = style.hover.textColor = style.focused.textColor = style.active.textColor = color;
        return style.CalcSize(content).x;
    }

    public void AddNodeAsChild(GeneralNode node, GeneralNode childTemplate)
    {
        int insertID = -1;
        GeneralNode child = childTemplate.Copy();
        child.indent = node.indent + 1;
        int startID = engine.selectedScript.actionNodes.IndexOf(node);
        for (int i = startID + 1; i < engine.selectedScript.actionNodes.Count; i++)
        {
            GeneralNode toCompare = engine.selectedScript.actionNodes[i];
            if (toCompare.indent <= node.indent)
            {
                insertID = i;
                break;
            }
        }
        if (insertID >= 0)
            engine.selectedScript.actionNodes.Insert(insertID, child);
        else
            engine.selectedScript.actionNodes.Add(child);
        EditorUtility.SetDirty(engineHandler.GetData());
    }
}
