using MyUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;
using static UnityEngine.UI.CanvasScaler;
using Random = UnityEngine.Random;

[System.Serializable]
public class LogicScript
{
    #region Script Execution Logic

    public string scriptName;
    public int scriptUID;
    public bool hasWait;

    public static Unit lastCreatedUnit;
    public static Unit triggeringUnit;
    public static Unit damagingUnit;
    public static Unit killingUnit;
    public static Unit healingUnit;
    public static Ability lastAbilityCast;
    public static Ability triggeringAbility;

    public LogicScript (string scriptName)
    {
        this.scriptName = scriptName;
        scriptUID = Random.Range(0, int.MaxValue - 1);
    }

    
    [SerializeReference]
    public List<GeneralNode> eventNodes = new List<GeneralNode>();


    [SerializeReference]
    public List<GeneralNode> conditionNodes = new List<GeneralNode>();


    [SerializeReference]
    public List<GeneralNode> actionNodes = new List<GeneralNode>();

    public LogicScript Copy ()
    {
        LogicScript script = new LogicScript(scriptName);
        foreach (GeneralNode node in eventNodes)
            script.eventNodes.Add(node.Copy());
        foreach (GeneralNode node in conditionNodes)
            script.conditionNodes.Add(node.Copy());
        foreach (GeneralNode node in actionNodes)
            script.actionNodes.Add(node.Copy());
        return script;
    }

    public override int GetHashCode()
    {
        return scriptUID;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public static bool operator ==(LogicScript s1, LogicScript s2)
    {
        if (s1 is null && s2 is null) return true;
        if (s1 is null && s2 is not null) return false;
        if (s1 is not null && s2 is null) return false;
        return s1.scriptUID == s2.scriptUID;
    }

    public static bool operator !=(LogicScript s1, LogicScript s2)
    {
        if (s1 is null && s2 is null) return false;
        if (s1 is null && s2 is not null) return true;
        if (s1 is not null && s2 is null) return true; 
        return s1.scriptUID != s2.scriptUID;
    }

    /// <summary>
    /// Try to run this script with the given trigger (and no specified presets).
    /// </summary>
    public void RunScript(LogicEngine engine, string eventTrigger = "", object reqs = null)
    {
        RunScript(new Dictionary<string, object>(), engine, eventTrigger, reqs);
    }

    /// <summary>
    /// Try to run this script with the given trigger and presets.
    /// </summary>
    public void RunScript (Dictionary<string, object> presets, LogicEngine engine, string eventTrigger = "", object reqs = null)
    {
        if (engine.disabledScripts.ContainsKey(this)) return;
        LogicEngine.current = engine;

        // Handle the events.
        bool areEventsTrue = false;
        int line = 0;
        LogicEngine.currentType = "Event";
        foreach (GeneralNode eventNode in eventNodes)
        {
            presets["CurrentLineID"] = line;
            line++;
            if (eventTrigger == eventNode.functionName)
            {
                if ((eventNode.functionEvaluators.Length == 0 || reqs == null) || 
                    eventNode.functionEvaluators[0].Resolve(presets, engine, this).Equals(reqs))
                {
                    areEventsTrue = true;
                }
            }
        }
        if (!areEventsTrue) return;

        // Handle the conditions.
        LogicEngine.currentType = "Condition";
        bool areConditionsTrue = true; 
        for (int i = 0; i < conditionNodes.Count; i++)
        {
            presets["CurrentLineID"] = i;
            if (((bool)conditionNodes[i].Resolve(presets, engine, this)).Equals(false))
            {
                areConditionsTrue = false;
                break;
            }
        }
        if (!areConditionsTrue) return;

        LogicEngine.currentType = "Action";
        // Handle the actions.
        _ = RunAllActions(presets, engine);
    }

    /// <summary>
    /// This method is used to convert an async wait into a time-controlled wait. This is
    /// necessary because the Unity Timescale can be adjusted (e.g. if the game is paused).
    /// </summary>
    public async Task Wait(float duration)
    {
        // Creating a TaskCompletionSource that will be completed after the wait
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        // Starting the coroutine that handles the wait
        GameManager.instance.StartCoroutine(WaitCoroutine(duration, tcs));

        // Awaiting the TaskCompletionSource's task
        await tcs.Task;
    }

    /// <summary>
    /// Coroutine to simulate the wait. It waits for the set amount of time before setting the
    /// task completion source.
    /// </summary>
    private IEnumerator WaitCoroutine(float duration, TaskCompletionSource<bool> tcs)
    {
        if (duration <= 0)
        {
            // If the duration is zero or negative, immediately complete the task.
            tcs.SetResult(true);
        }
        else
        {
            float startTime = Time.time;
            float endTime = startTime + duration;
            // Adjusting for very short durations by ensuring we wait for at least one frame
            // This could be important for consistency in behavior, especially if the action
            // following the wait is expected to happen after some processing has occurred.
            do
            {
                // Waiting for the next frame
                yield return null;
            } while (Time.time < endTime);

            // Marking the Task as complete
            tcs.SetResult(true);
        }
    }

    /// <summary>
    /// Run all actions in the script, using the given presets and execution engine.
    /// </summary>
    public async Task RunAllActions (Dictionary<string, object> presets, LogicEngine engine, int startID = 0, int indent = 0)
    {
        for (int i = startID; i < actionNodes.Count; i++)
        {
            if (presets.ContainsKey("ActionsArePaused"))
                break;


            presets["CurrentLineID"] = i;
            LogicEngine.current = engine;
            LogicEngine.currentType = "Action";

            // Stop and go back to parent. Stop doing stuff.
            if (actionNodes[i].indent < indent) break;

            // There are a number of "special" nodes which need to be treated seperately. These perform special
            // flow logic and will adjust execution of the actions in a non-standard way.
            if (actionNodes[i].indent == indent)
            {
                // A "Wait" node will cause execution of all remaining actions to pause by the specified amount of time.
                if (actionNodes[i].functionName == "Wait")
                {
                    float value = (float)actionNodes[i].functionEvaluators[0].Resolve(presets, engine, this);
                    await Wait(value);
                }

                 
                else if (actionNodes[i].functionName == "DoActionsXTimes") // This is old and should be removed after it is no longer used.
                {
                    int repeatCount = (int)((float)actionNodes[i].functionEvaluators[0].Resolve(presets, engine, this));
                    for (int j = 0; j < repeatCount; j++)
                    {
                        await RunAllActions(presets, engine, i + 1, indent + 1);
                    }
                }

                // A "DoActionXTimes" node will repeat all child nodes the specified number of times.
                else if (actionNodes[i].functionName == "DoActionsXTimesStoringVariable")
                {
                    int repeatCount = (int)((float)actionNodes[i].functionEvaluators[0].Resolve(presets, engine, this));
                    string variableName = (string)actionNodes[i].functionEvaluators[1].Resolve(presets, engine, this);
                    for (int j = 0; j < repeatCount; j++)
                    {
                        engine.localVariables[variableName] = j;
                        await RunAllActions(presets, engine, i + 1, indent + 1);
                    }
                }

                // A "ForEachUnitInGroup" node will repeat the child actions for each unit in the group.
                else if (actionNodes[i].functionName == "ForEachUnitInGroup")
                {
                    List<Unit> unitGroup = (List<Unit>)actionNodes[i].functionEvaluators[0].Resolve(presets, engine, this);
                    string variableName = (string)actionNodes[i].functionEvaluators[1].Resolve(presets, engine, this);
                    foreach (Unit unit in unitGroup)
                    {
                        if (unit != null)
                        {
                            engine.localVariables[variableName] = unit;
                            await RunAllActions(presets, engine, i + 1, indent + 1);
                        }
                    }
                }

                // A "DoActionsWhileBool" node will repeat the child actions while the condition is true. 
                else if (actionNodes[i].functionName == "DoActionsWhileBool")
                {
                    while ((bool)actionNodes[i].functionEvaluators[0].Resolve(presets, engine, this).Equals(true))
                    {
                        await RunAllActions(presets, engine, i + 1, indent + 1);
                    }
                }

                // A "DoActionsIfBool" node will only execute the child actions if the condition is true.  
                else if (actionNodes[i].functionName == "DoActionsIfBool")
                {
                    if ((bool)actionNodes[i].functionEvaluators[0].Resolve(presets, engine, this).Equals(true))
                    {
                        await RunAllActions(presets, engine, i + 1, indent + 1);
                    }
                }

                // A "DisableScript" node will disable this script, preventing all future execution.
                else if (actionNodes[i].functionName == "DisableScript")
                {
                    engine.disabledScripts[this] = float.MaxValue;
                }

                // Otherwise, execute the node as normal.
                else {
                    try
                    {
                        actionNodes[i].Resolve(presets, engine, this);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.ToString());
                    }
                    await RunAllActions(presets, engine, i, indent + 1);
                }
            }
        }
    }

    public void Error (string message)
    {
        if (GameManager.logicEngine.showErrors)
        {
            if (LogicEngine.current != null && LogicEngine.current.engineHandler != null)
                Debug.Log($"<color=orange>[{LogicEngine.current.engineHandler.GetData().name.Replace("(Clone)", "")}] [{LogicEngine.currentScript.scriptName}] [{LogicEngine.currentType}, Line {LogicEngine.currentLine + 1}]:</color> <color=yellow>{message}</color>");
        }
        if (GameManager.logicEngine.stopScriptExecutionOnError)
        {
            LogicEngine.currentPresets.Add("ActionsArePaused", true);
        }
        //LogicEngine.current = engine;
        //LogicEngine.currentScript = script;
        //LogicEngine.currentNode = this;
    }

    #endregion

    #region Action Nodes

    public void SpinUnit (Unit unit, float speed, float duration)
    {
        if (unit == null)
        {
            Error("The unit you want to spin is invalid.");
            return;
        }

        unit.StartSpin(speed, duration);
    }

    public void SpawnUnit (Unit unit, Vector3 position)
    {
        if (unit == null)
        {
            Error("The unit to spawn is invalid.");
            return;
        }

        SpawnUnit2(unit, position, false);
    }

    public void SpawnUnit2 (Unit unit, Vector3 position, bool isEmpowered)
    {
        if (unit == null)
        {
            Error("The unit to spawn is invalid.");
            return;
        }
    
        UnitSpawnEffect spawnEffect = null;
        if (unit is Monster) spawnEffect = ((Monster)unit).spawnEffect;
        position = Utilities.GetValidNavMeshPosition(position);
        if (spawnEffect != null)
        {
            SpawnUnitWithEffect(unit, position, spawnEffect, isEmpowered);
        }
        else
        {
            Unit u = GameObject.Instantiate(unit, position, Quaternion.identity);
            if (isEmpowered && u is Monster monster) monster.Empower();
        }
    }

    public void SpawnEmpoweredUnit (Unit unit, Vector3 position)
    {
        if (unit == null)
        {
            Error("The unit to spawn is invalid.");
            return;
        }

        SpawnUnit2(unit, position, true);
    }

    public void SpawnUnitWithEffect (Unit unit, Vector3 position, UnitSpawnEffect spawnEffect, bool isEmpowered = false)
    {
        if (unit == null)
        {
            Error("The unit to spawn is invalid.");
            return;
        }
        if (spawnEffect == null)
        {
            Error("The effect to spawn is invalid.");
            return;
        }

        GameManager.instance.StartCoroutine(SpawnUnitCoroutine(unit, position, spawnEffect, isEmpowered));
    }

    private IEnumerator SpawnUnitCoroutine(Unit unit, Vector3 position, UnitSpawnEffect spawnEffect, bool isEmpowered = false)
    {
        float duration = spawnEffect.GetComponent<UnitSpawnEffect>().effectDuration;
        ObjectPooler.InstantiatePooled(spawnEffect.gameObject, position, Quaternion.identity);
        yield return new WaitForSeconds(duration);
        Unit u = GameObject.Instantiate(unit, position, Quaternion.identity);
        if (isEmpowered && u is Monster monster) monster.Empower();
    }

    public void SpawnUnits (float count, Unit unit, Vector3 position)
    {
        if (unit == null)
        {
            Error("The unit to spawn is invalid.");
            return;
        }

        int unitsToSpawn = (int)count;
        if (unit != null && unitsToSpawn > 0)
        {
            GameObject.Instantiate(unit, position, Quaternion.identity);
        }
    }

    public void KillUnit (Unit unit)
    {
        if (unit == null)
        {
            Error("The unit to kill is invalid.");
            return;
        }

        unit.Kill(LogicEngine.current.engineHandler.GetOwner(), LogicEngine.current.engineHandler);
    }

    public void AddHealth(float amount, Unit unit)
    {
        if (unit == null)
        {
            Error("The unit to add health to is invalid.");
            return;
        }

        unit.AddHealth(Mathf.Max(0, amount));
    }

    public void RemoveHealth(float amount, Unit unit)
    {
        if (unit == null)
        {
            Error("The unit to remove health from is invalid.");
            return;
        }

        unit.RemoveHealth(Mathf.Max(0, amount));
    }

    public void AddResource(float amount, Unit unit)
    {
        if (unit == null)
        {
            Error("The unit to add resource to is invalid.");
            return;
        }

        unit.AddResource(Mathf.Max(0, amount));
    }

    public void RemoveResource(float amount, Unit unit)
    {
        if (unit == null)
        {
            Error("The unit to remove resource from is invalid.");
            return;
        }

        unit.RemoveResource(Mathf.Max(0, amount));
    }

    public void AddGold(float amount)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }

        GameManager.player.AddGold(Mathf.Max(0, amount));
    }

    public void RemoveGold(float amount)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }

        GameManager.player.RemoveGold(Mathf.Max(0, amount));
    }

    public void AddExperience (float amount)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }

        GameManager.player.AddExperience(Mathf.Max(0, amount));
    }

    public void RemoveExperience(float amount)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }

        GameManager.player.RemoveExperience(Mathf.Max(0, amount));
    }

    public void SetExperience(float amount)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }

        GameManager.player.SetExperience(Mathf.Max(0, amount));
    }

    public void AddLevels(float amount)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }

        GameManager.player.AddLevels((int)Mathf.Max(0, amount));
    }

    public void RemoveLevels(float amount)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }

        GameManager.player.RemoveLevels((int)Mathf.Max(0, amount));
    }

    public void SetLevel(float amount)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }

        GameManager.player.SetLevel((int)Mathf.Max(1, amount));
    }

    public void AddItem (Item item)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }
        if (item == null)
        {
            Error("The item specified is invalid.");
            return;
        }

        GameManager.player.inventory.AddItem(item.RollItem());
    }

    public void RemoveEquipment()
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }
        for (int i = 0; i < GameManager.player.equipment.GetSlots(); i++)
        {
            GameManager.player.equipment.RemoveItemAtID(i);
        }
    }

    public void EquipItem (Item item)
    {
        item = item.RollItem();
        if (item.itemType == Item.ItemType.Weapon)
            GameManager.player.equipment.AddItemAtID(item, 0);

        else if (item.itemType == Item.ItemType.Amulet)
            GameManager.player.equipment.AddItemAtID(item, 1);

        else if (item.itemType == Item.ItemType.Armor)
            GameManager.player.equipment.AddItemAtID(item, 2);

        else if (item.itemType == Item.ItemType.Boots)
            GameManager.player.equipment.AddItemAtID(item, 3);

        else if (item.itemType == Item.ItemType.Ring && GameManager.player.equipment.GetItemAtID(4) == null)
            GameManager.player.equipment.AddItemAtID(item, 4);

        else if (item.itemType == Item.ItemType.Ring && GameManager.player.equipment.GetItemAtID(5) == null)
            GameManager.player.equipment.AddItemAtID(item, 5);

        else if (item.itemType == Item.ItemType.Ring)
            GameManager.player.equipment.AddItemAtID(item, 4);
    }

    public void RemoveItem(Item item)
    {
        if (GameManager.player == null)
        {
            Error("No player exists.");
            return;
        }
        if (item == null)
        {
            Error("The item specified is invalid.");
            return;
        }

        GameManager.player.inventory.RemoveItem(item);
    }

    public void PlayUnitAnimation (string animation, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }

        unit.PlayAnimation(animation);
    }

    public void ShowDebugMessage (string message)
    {
        Debug.Log(message);
    }

    public void ShowStatusMessage(Vector3 position, string message)
    {
        if (message.Length == 0)
        {
            Error("The message was empty.");
            return;
        }

        StatusMessageUI.Spawn(position, message, UnityEngine.Color.yellow);
    }

    public void ShowTutorialMessage(string message, float duration = 6.0f)
    {
        if (message.Length == 0)
        {
            Error("The message was empty.");
            return;
        }

        GameManager.ui.MessageWindow.DisplayMessage(message, duration);
    }

    public void Play2DSound (AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Error("The specified audio clip was invalid.");
            return;
        }
        if (volume < 0)
        {
            Error("The specified volume was invalid (less than zero).");
            return;
        }

        volume /= 100.0f;
        GameManager.music.PlaySound(clip, volume);
    }

    public void Play3DSound (AudioClip clip, Vector3 position)
    {

    }

    public void ShakeScreen(float strength)
    {
        ScreenShakeEffect effect = Camera.main.GetOrAddComponent<ScreenShakeEffect>();
        if (effect == null)
        {
            Error("Error trying to shake the camera.");
            return;
        }

        effect.shakeStrength += (strength * 0.15f);
    }

    public void ColorFlash (Color color, Unit unit, float time)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }
        
        // Don't flash if it is already flashing.
        if (unit.GetComponent<FlashTextureEffect>() != null) return;
        unit.AddComponent<FlashTextureEffect>().Setup(color, time);
    }

    public void ChangeGameMusic (AudioClip clip)
    {
        if (clip == null)
        {
            Error("The specified audio clip was invalid.");
            return;
        }

        GameManager.music.FadeIntoNewClip(clip);
    }

    public void ModifyNumberVariable(string name, float value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        if (!LogicEngine.globalVariables.ContainsKey(name)) LogicEngine.globalVariables[name] = 0f;
        LogicEngine.current.localVariables[name] = (float)LogicEngine.current.localVariables[name] + value;
    }

    public void ModifyGlobalNumberVariable(string name, float value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        if (!LogicEngine.globalVariables.ContainsKey(name)) LogicEngine.globalVariables[name] = 0f;
        LogicEngine.globalVariables[name] = (float)LogicEngine.globalVariables[name] + value;
    }

    public void SetNumberVariable(string name, float value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.current.localVariables[name] = value;
    }

    public void SetUnitGroupVariable(string name, List<Unit> value)
    {
        if (value == null)
        {
            Error("The specified unit group was invalid.");
            return;
        }

        LogicEngine.current.localVariables[name] = value;
    }

    public void AddToUnitGroup(Unit unit, string name)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }

        if (!LogicEngine.current.localVariables.ContainsKey(name)) return;
        ((List<Unit>)LogicEngine.current.localVariables[name]).Add(unit);
    }

    public void RemoveFromUnitGroup(Unit unit, string name)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }

        if (!LogicEngine.current.localVariables.ContainsKey(name)) return;
        ((List<Unit>)LogicEngine.current.localVariables[name]).Remove(unit);
    }

    public void SetBoolVariable(string name, bool value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.current.localVariables[name] = value;
    }

    public void SetUnitVariable(string name, Unit value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.current.localVariables[name] = value;
    }

    public void SetVectorVariable(string name, Vector3 value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.current.localVariables[name] = value;
    }

    public void SetStringVariable(string name, string value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.current.localVariables[name] = value;
    }

    public void SetGlobalNumberVariable(string name, float value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.globalVariables[name] = value;
    }

    public void SetGlobalBoolVariable(string name, bool value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.globalVariables[name] = value;
    }

    public void SetGlobalUnitVariable(string name, Unit value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.globalVariables[name] = value;
    }

    public void SetGlobalVectorVariable(string name, Vector3 value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.globalVariables[name] = value;
    }

    public void SetGlobalStringVariable(string name, string value)
    {
        if (name.Length == 0)
        {
            Error("The specified variable name was invalid.");
            return;
        }

        LogicEngine.globalVariables[name] = value;
    }

    public void HaveUnitDamageUnit (Unit damagingUnit, float amount, Unit damagedUnit)
    {
        if (damagingUnit == null)
        {
            Error("The damaging unit was invalid.");
            return;
        }
        if (damagedUnit == null)
        {
            Error("The damaging unit was invalid.");
            return;
        }

        damagingUnit.DamageOtherUnit(damagedUnit, amount / 100.0f, LogicEngine.current.engineHandler);
    }

    public void HaveUnitDamageUnits (Unit damagingUnit, float amount, List<Unit> units)
    {
        if (damagingUnit == null)
        {
            Error("The damaging unit was invalid.");
            return;
        }
        if (units == null)
        {
            Error("The damaged unit list was invalid.");
            return;
        }

        damagingUnit.DamageOtherUnits(units, amount / 100.0f, LogicEngine.current.engineHandler);
    }

    public void HaveUnitDamageUnit2(float amount, Unit damagedUnit)
    {
        if (damagedUnit == null)
        {
            Error("The damaged unit was invalid.");
            return;
        }

        IEngineHandler engine = LogicEngine.current.engineHandler;
        engine.GetOwner().DamageOtherUnit(damagedUnit, amount / 100.0f, engine);
    }

    public void HaveUnitDamageUnits2(float amount, List<Unit> units)
    {
        if (units == null)
        {
            Error("The damaged unit list was invalid.");
            return;
        }

        IEngineHandler engine = LogicEngine.current.engineHandler;
        engine.GetOwner().DamageOtherUnits(units, amount / 100.0f, engine);
    }

    public void PlayFeedbackAtPoint(GameFeedback feedback, Vector3 point)
    {
        if (feedback == null)
        {
            Error("The specified feedback was invalid.");
            return;
        }

        feedback.ActivateFeedback(null, null, point);
    }

    public void PlayFeedbackOnUnit (GameFeedback feedback, Unit unit)
    {
        if (feedback == null)
        {
            Error("The specified feedback was invalid.");
            return;
        }

        feedback.ActivateFeedback(unit.gameObject, unit.gameObject, unit.transform.position);
    }

    public void CreateCircleGuide (Vector3 location, float radius, float duration)
    {
        CircleEffectGuide.Spawn(location, radius, duration);
    }

    public void CreateLineGuide (Vector3 location1, Vector3 location2, float width, float duration)
    {
        LineEffectGuide.Spawn(location1, location2, width, duration);
    }

    public void CreateLineGuide2(Unit unit, float width, float length, float duration)
    {
        LineEffectGuide.Spawn(unit, width, length, duration);
    }

    public void CreateArcGuide(float arc, Unit unit, float radius, float duration)
    {
        ArcEffectGuide.Spawn(arc, unit, radius, duration);
    }

    public void PlayFeedbackOnUnits(GameFeedback feedback, List<Unit> units)
    {
        if (units == null)
        {
            Error("The specified unit list was invalid.");
            return;
        }

        foreach (Unit unit in units)
        {
            if (unit != null)
            {
                feedback.ActivateFeedback(unit.gameObject, unit.gameObject, unit.transform.position);
            }
        }
    }

    public void RemoveBuff(string buffName, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }
        unit.stats.RemoveBuffWithLabel(buffName);
    }

    public void IncreaseStatValue(string modifier, Unit unit, float mod, float duration, string buff, float maxStacks)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }

        Stat stat = StatExtensions.LabelToStat(modifier);
        if (duration > 0)
            unit.stats[stat].AddTimedValueModifier(Mathf.Max(0, mod), duration, buff, (int)maxStacks);
        else
            unit.stats[stat].AddValueModifier(Mathf.Max(0, mod), buff, (int)maxStacks);
    }

    public void IncreaseStatPercent(string modifier, Unit unit, float mod, float duration, string buff, float maxStacks)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }

        Stat stat = StatExtensions.LabelToStat(modifier);
        if (duration > 0)
            unit.stats[stat].AddTimedPercentageModifier(Mathf.Max(0, mod / 100.0f), duration, buff, (int)maxStacks);
        else
            unit.stats[stat].AddPercentageModifier(Mathf.Max(0, mod / 100.0f), buff, (int)maxStacks);
    }

    public void DecreaseStatValue(string modifier, Unit unit, float mod, float duration, string buff, float maxStacks)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }

        Stat stat = StatExtensions.LabelToStat(modifier);
        if (duration > 0)
            unit.stats[stat].AddTimedValueModifier(Mathf.Min(-mod, 0), duration, buff, (int)maxStacks);
        else
            unit.stats[stat].AddValueModifier(Mathf.Min(-mod, 0), buff, (int)maxStacks);
    }

    public void DecreaseStatPercent(string modifier, Unit unit, float mod, float duration, string buff, float maxStacks)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }

        Stat stat = StatExtensions.LabelToStat(modifier);
        if (duration > 0)
            unit.stats[stat].AddTimedPercentageModifier(Mathf.Min(0, -mod / 100.0f), duration, buff, (int)maxStacks);
        else
            unit.stats[stat].AddPercentageModifier(Mathf.Min(0, -mod / 100.0f), buff, (int)maxStacks);
    }

    public void IncreaseStatValueUnitGroup(string modifier, List<Unit> units, float mod, float duration, string buff, float maxStacks)
    {
        if (units == null)
        {
            Error("The specified unit list was invalid.");
            return;
        }

        Stat stat = StatExtensions.LabelToStat(modifier);
        foreach (Unit unit in units)
            IncreaseStatValue(modifier, unit, mod, duration, buff, maxStacks);
    }

    public void IncreaseStatPercentUnitGroup(string modifier, List<Unit> units, float mod, float duration, string buff, float maxStacks)
    {
        if (units == null)
        {
            Error("The specified unit list was invalid.");
            return;
        }

        Stat stat = StatExtensions.LabelToStat(modifier);
        foreach (Unit unit in units)
            IncreaseStatPercent(modifier, unit, mod, duration, buff, maxStacks);
    }

    public void DecreaseStatValueUnitGroup(string modifier, List<Unit> units, float mod, float duration, string buff, float maxStacks)
    {
        if (units == null)
        {
            Error("The specified unit list was invalid.");
            return;
        }

        Stat stat = StatExtensions.LabelToStat(modifier);
        foreach (Unit unit in units)
            DecreaseStatValue(modifier, unit, mod, duration, buff, maxStacks);
    }

    public void DecreaseStatPercentUnitGroup(string modifier, List<Unit> units, float mod, float duration, string buff, float maxStacks)
    {
        if (units == null)
        {
            Error("The specified unit list was invalid.");
            return;
        }

        Stat stat = StatExtensions.LabelToStat(modifier);
        foreach (Unit unit in units)
            DecreaseStatPercent(modifier, unit, mod, duration, buff, maxStacks);
    }

    public static Projectile lastCreatedProjectile = null;

    public void SpawnProjectile(Projectile projectile, Vector3 position)
    {
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        Unit unit = LogicEngine.current.GetOwner();
        if (projectile == null) return;
        Quaternion rotation = Quaternion.identity;
        if (unit != null) rotation = unit.transform.rotation;
        GameObject obj = GameObject.Instantiate(projectile.gameObject, position, rotation);
        Projectile p = obj.GetComponent<Projectile>();
        p.Setup(LogicEngine.current.engineHandler);
        lastCreatedProjectile = p;
    }

    public void SpawnProjectileNew(Unit unit, Projectile projectile, Vector3 position)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        GameObject obj = GameObject.Instantiate(projectile.gameObject, position, unit.transform.rotation);
        Projectile p = obj.GetComponent<Projectile>();
        p.Setup(LogicEngine.current.engineHandler);
        lastCreatedProjectile = p;
    }

    public void MoveForwardAtSpeed (Projectile projectile, float speed)
    {
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        projectile.MoveProjectileForwards(speed);
    }

    public void RotateProjectile(Projectile projectile, float amount)
    {
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        projectile.Rotate(amount);
    }

    public void FaceProjectileTowardsPoint (Projectile projectile, Vector3 point)
    {
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        projectile.FaceProjectileTowardsPoint(point);
    }

    public void SetProjectileLifetime (Projectile projectile, float lifetime)
    {
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        projectile.SetLifetime(lifetime);
    }

    public void DestroyProjectile (Projectile projectile)
    {
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        projectile.DestroyProjectile();
    }

    public void MoveTowardsPointAtSpeed (Projectile projectile, Vector3 point, float speed)
    {
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        projectile.MoveProjectileTowardsPoint(point, speed);
    }

    public void MoveTowardsPointInArc(Projectile projectile, Vector3 point, float time, float arcHeight)
    {
        if (projectile == null)
        {
            Error("The specified projectile was invalid.");
            return;
        }

        projectile.MoveProjectileInArcTowardsPoint(point, time, arcHeight);
    }

    public void TeleportUnit (Unit unit, Vector3 newPosition)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }

        unit.Teleport(newPosition);
        unit.StopMoving();
    }

    public void MoveUnitOverTime (Unit unit, Vector3 newPosition, float duration)
    {
        if (unit == null)
        {
            Error("The specified unit was invalid.");
            return;
        }

        unit.MoveOverTime(newPosition, duration);
    }

    public void DestroyRegions(string regionName)
    {
        if (regionName.Length == 0)
        {
            Error("The region name cannot be empty.");
            return;
        }

        Region[] regions = GameObject.FindObjectsOfType<Region>();
        foreach (Region region in regions)
        {
            if (region.regionName == regionName)
            {
                GameObject.Destroy(region.gameObject);
            }
        }
    }

    public void CreateQuest2(string questName)
    {
        if (questName.Length == 0)
        {
            Error("The quest name cannot be empty.");
            return;
        }

        Quest quest = new Quest(questName, questName);
        GameManager.quests.AddQuest(quest);
    }

    public void AddQuestRequirement2 (string requirement, string questName, float increments)
    {
        if (requirement.Length == 0)
        {
            Error("The requirement name cannot be empty.");
            return;
        }
        if (!GameManager.quests.activeQuests.ContainsKey(questName))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questName];
        quest.AddCompletionRequirement(requirement, requirement, (int)increments);
    }

    public void AddQuestReward2 (string reward, string questName)
    {
        if (reward.Length == 0)
        {
            Error("The reward name cannot be empty.");
            return;
        }
        if (!GameManager.quests.activeQuests.ContainsKey(questName)) 
        {
            Error("The player does not have the specified quest.");
            return;
        }

        GameManager.quests.activeQuests[questName].SetReward(reward);
    }

    public void SetQuestRequirementProgress2 (string questName, float progress)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questName))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questName];
        quest.SetProgress("None", (int)progress);
    }

    public void ModifyQuestRequirementProgress2 (string questName, float progress)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questName))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questName];
        quest.IncrementProgress("None", (int)progress);
    }

    public void SetSpecificQuestRequirementProgress2(string requirement, string questName, float progress)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questName))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questName];
        quest.SetProgress(requirement, (int)progress);
    }

    public void ModifySpecificQuestRequirementProgress2(string requirement, string questName, float progress)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questName))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questName];
        quest.IncrementProgress(requirement, (int)progress);
    }

    public void CreateQuest (string descriptor, string questTag)
    {
        if (questTag.Length == 0)
        {
            Error("The quest name cannot be empty.");
            return;
        }

        Quest quest = new Quest(questTag, descriptor);
        GameManager.quests.AddQuest(quest);
    }

    public void AddQuestRequirement(string descriptor, string questTag, float increments, string requirementTag)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questTag))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questTag];
        quest.AddCompletionRequirement(requirementTag, descriptor, (int)increments);
    }

    public void AddQuestReward (string reward, string questTag)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questTag))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        GameManager.quests.activeQuests[questTag].SetReward(reward);
    }

    public void SetQuestRequirementProgress (string questTag, float progress)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questTag))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questTag];
        quest.SetProgress("None", (int)progress);
    }

    public void ModifyQuestRequirementProgress(string questTag, float progress)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questTag))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questTag];
        quest.IncrementProgress("None", (int)progress);
    }

    public void SetSpecificQuestRequirementProgress(string requirementTag, string questTag, float progress)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questTag))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questTag];
        quest.SetProgress(requirementTag, (int)progress); 
    }

    public void ModifySpecificQuestRequirementProgress(string requirementTag, string questTag, float progress)
    {
        if (!GameManager.quests.activeQuests.ContainsKey(questTag))
        {
            Error("The player does not have the specified quest.");
            return;
        }

        Quest quest = GameManager.quests.activeQuests[questTag];
        quest.IncrementProgress(requirementTag, (int)progress);
    }

    public void SpawnItemDrop (Vector3 location, Item item)
    {
        if (item == null)
        {
            Error("The specified item is invalid.");
            return;
        }

        ItemPickup.Spawn(location, item);
    }

    public void SpawnGoldDrop (Vector3 location, float goldAmount)
    {
        GoldPickup.Spawn(location, (int)goldAmount);
    }

    public void SpawnHealthDrop (Vector3 location)
    {
        HealthPickup.Spawn(location);
    }

    public void WinGame()
    {
        GameManager.instance.WinGame();
    }

    public void EnableAbility(Ability ability, Unit unit)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }

        for (int i = 0; i < unit.abilities.Count; i++)
        {
            if (unit.abilities[i].abilityName == ability.abilityName)
            {
                unit.abilities[i].Unlock();
                if (unit == GameManager.player) GameManager.ui.NotificationWindow.DisplayMessage("Ability Unlocked", ability.abilityName, ability.abilityIcon);
            }
        }
    }

    public void DisableAbility(Ability ability, Unit unit)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }

        for (int i = 0; i < unit.abilities.Count; i++)
        {
            if (unit.abilities[i].abilityName == ability.abilityName)
            {
                unit.abilities[i].Lock();
            }
        }
    }

    public void ReduceAbilityCooldown (Ability ability, Unit unit, float amount)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        unit.ReduceAbilityCooldown(ability, amount);
    }

    public void AddAbilityToUnit(Ability ability, Unit unit)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        unit.AddAbility(ability);
    }

    public void RemoveAbilityFromUnit(Ability ability, Unit unit)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        unit.RemoveAbility(ability);
    }

    public void ReplaceAbilityOnUnit(Ability oldAbility, Ability newAbility, Unit unit)
    {
        if (oldAbility == null || newAbility == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        unit.ReplaceAbility(oldAbility, newAbility);
    }

    public void AddAbilityCostModifierToUnit (string increaseDecrease, Ability ability, Unit unit, float modifier, string buffName = "None", int maxStacks = 99)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        if (increaseDecrease == "Decrease") modifier *= -1;
        unit.AddAbilityCostModifier(ability, modifier, buffName, maxStacks);
    }

    public void AddAbilityCooldownModifierToUnit(string increaseDecrease, Ability ability, Unit unit, float modifier, string buffName = "None", int maxStacks = 99)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        if (increaseDecrease == "Decrease") modifier *= -1;
        unit.AddAbilityCooldownModifier(ability, modifier, buffName, maxStacks);
    }

    public void AddAbilityDamageModifierToUnit(string increaseDecrease, Ability ability, Unit unit, float modifier, string buffName = "None", float maxStacks = 99)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        if (increaseDecrease == "Decrease") modifier *= -1;
        unit.AddAbilityDamageModifier(ability, modifier / 100.0f, buffName, (int)maxStacks);
    }

    public void AddTimedAbilityCostModifierToUnit(string increaseDecrease, Ability ability, Unit unit, float modifier, float duration, string buffName = "None", float maxStacks = 99)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        if (increaseDecrease == "Decrease") modifier *= -1;
        unit.AddTimedAbilityCostModifier(ability, modifier, duration, buffName, (int)maxStacks);
    }

    public void AddTimedAbilityCooldownModifierToUnit(string increaseDecrease, Ability ability, Unit unit, float modifier, float duration, string buffName = "None", float maxStacks = 99)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        if (increaseDecrease == "Decrease") modifier *= -1;
        unit.AddTimedAbilityCooldownModifier(ability, modifier, duration, buffName, (int)maxStacks);
    }

    public void AddTimedAbilityDamageModifierToUnit(string increaseDecrease, Ability ability, Unit unit, float modifier, float duration, string buffName = "None", float maxStacks = 99)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        if (increaseDecrease == "Decrease") modifier *= -1;
        unit.AddTimedAbilityDamageModifier(ability, modifier / 100.0f, duration, buffName, (int)maxStacks);
    }

    public void RemoveAbilityModifier(Ability ability, Unit unit, string buffName)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }
        unit.RemoveAbilityBuffModifiers(ability, buffName);
    }

    public void SpawnEffectAtLocation (CustomVisualEffect effect, Vector3 position, float duration, float scale)
    {
        if (effect == null)
        {
            Error("The specified effect is invalid.");
            return;
        }

        GameObject obj = ObjectPooler.InstantiatePooled(effect.gameObject, position, Quaternion.identity);
        obj.transform.localScale = effect.transform.localScale * scale;
        if (duration > 0) obj.GetComponent<CustomVisualEffect>().DestroyAfter(duration);
    }

    private void SpawnEffectOnUnit (CustomVisualEffect effect, Unit unit, float duration, float scale)
    {
        if (effect == null)
        {
            Error("The specified effect is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }

        GameObject obj = ObjectPooler.InstantiatePooled(effect.gameObject, unit.transform.position, unit.transform.rotation);
        obj.name = effect.name;
        obj.transform.localScale = effect.transform.localScale * scale;
        if (duration > 0) obj.GetComponent<CustomVisualEffect>().DestroyAfter(duration);
        obj.transform.SetParent(unit.transform);
    }

    public void SpawnEffectOnUnit (string action, CustomVisualEffect effect, Unit unit, float duration, float scale)
    {
        if (effect == null)
        {
            Error("The specified effect is invalid.");
            return;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return;
        }

        if (action == "Play")
        {
            SpawnEffectOnUnit(effect, unit, duration, scale);
        }
        else if (action == "Stop")
        {
            Transform existing = unit.transform.Find(effect.name);
            if (existing != null)
            {
                ObjectPooler.DestroyPooled(unit.transform.Find(effect.name).gameObject);
            }
        }
        else if (action == "Play or Refresh")
        {
            Transform existing = unit.transform.Find(effect.name);
            if (existing != null)
            {
                existing.GetComponent<CustomVisualEffect>().DestroyAfter(duration);
            }
            else
            {
                SpawnEffectOnUnit(effect, unit, duration, scale);
            }
        }
    }

    public void SpawnEffectOnUnitGroup (string action, CustomVisualEffect effect, List<Unit> units, float duration, float scale)
    {
        if (effect == null)
        {
            Error("The specified effect is invalid.");
            return;
        }
        if (units == null)
        {
            Error("The specified unit group is invalid.");
            return;
        }

        foreach (Unit unit in units)
        {
            SpawnEffectOnUnit(action, effect, unit, duration, scale);
        }
    }

    #endregion

    #region Item Value Nodes

    public Item RandomItemOfRarity(string rarity)
    {
        return Item.GetRandomItemOfRarity((Item.ItemRarity)Enum.Parse(typeof(Item.ItemRarity), rarity));
    }

    public Item ThisItem()
    {
        return (Item)LogicEngine.current.engineHandler;
    }

    #endregion

    #region Number Value Nodes

    public float GetPlayerLevel ()
    {
        if (GameManager.player == null)
        {
            Error("Player could not be found.");
            return 0;
        }

        return GameManager.player.currentLevel;
    }

    public float GetPlayerGold()
    {
        if (GameManager.player == null)
        {
            Error("Player could not be found.");
            return 0;
        }

        return GameManager.player.currentGold;
    }

    public float GetPlayerItemsEquipped()
    {
        if (GameManager.player == null)
        {
            Error("Player could not be found.");
            return 0;
        }

        return GameManager.player.equipment.GetFilledSlots();
    }

    public float GetPlayerExperience()
    {
        if (GameManager.player == null)
        {
            Error("Player could not be found.");
            return 0;
        }

        return GameManager.player.currentExperience;
    }

    public float TimeSinceLevelStart()
    {
        return Time.timeSinceLevelLoad;
    }

    public float Addition (float num1, float num2)
    {
        return num1 + num2;
    }

    public float Subtraction(float num1, float num2)
    {
        return num1 - num2;
    }

    public float Multiplication(float num1, float num2)
    {
        return num1 * num2;
    }

    public float Division(float num1, float num2)
    {
        return num1 / num2;
    }

    public float RandomNumberBetween (float num1, float num2)
    {
        return Random.Range(num1, num2);
    }

    public float UnitHealth (Unit unit)
    {
        if (unit == null)
        {
            Error("Specified unit is invalid.");
            return 0;
        }
        return unit.health;
    }

    public float UnitMaxHealth(Unit unit)
    {
        if (unit == null)
        {
            Error("Specified unit is invalid.");
            return 0;
        }
        return unit.stats.GetValue(Stat.MaxHealth);
    }

    public float HealthPercent(Unit unit)
    {
        if (unit == null)
        {
            Error("Specified unit is invalid.");
            return 0;
        }
        return unit.health / unit.stats.GetValue(Stat.MaxHealth);
    }

    public float UnitResource(Unit unit)
    {
        if (unit == null)
        {
            Error("Specified unit is invalid.");
            return 0;
        }

        return unit.resource;
    }

    public float UnitMaxResource(Unit unit)
    {
        if (unit == null)
        {
            Error("Specified unit is invalid.");
            return 0;
        }

        return unit.stats.GetValue(Stat.MaxResource);
    }

    public float DistanceBetweenPoints (Vector3 vec1, Vector3 vec2)
    {
        return Vector3.Distance(vec1, vec2);
    }

    public float DistanceBetweenUnits (Unit unit1, Unit unit2)
    {
        if (unit1 == null || unit2 == null)
        {
            Error("Specified unit is invalid.");
            return 0;
        }

        return Vector3.Distance(unit1.transform.position, unit2.transform.position);
    }

    public float VectorComponent (string component, Vector3 vector)
    {

        switch (component) {
            case "X":
                return vector.x;
            case "Y":
                return vector.y;
            case "Z":
                return vector.z;
            default:
                return 0;
        }
    }

    public float CountUnitsInUnitGroup(List<Unit> unitGroup)
    {
        if (unitGroup == null)
        {
            Error("Specified unit group is invalid.");
            return 0;
        }

        return unitGroup.Count;
    }

    public float GetNumberVariable (string name)
    {
        if (!LogicEngine.globalVariables.ContainsKey(name)) return 0;
        return (float)LogicEngine.current.localVariables.GetValueOrDefault(name, 0f);
    }

    public float GetGlobalNumberVariable(string name)
    {
        if (!LogicEngine.globalVariables.ContainsKey(name)) return 0;
        return (float)LogicEngine.globalVariables.GetValueOrDefault(name, 0f);
    }

    #endregion

    #region Unit Value Nodes

    public Unit GetOwner ()
    {
        return LogicEngine.current.GetOwner();
    }

    public Unit GetPlayer ()
    {
        return GameManager.player;
    }

    public Unit GetLastCreatedUnit ()
    {
        return lastCreatedUnit;
    }

    public Unit GetTriggeringUnit ()
    {
        return triggeringUnit;
    }

    public Unit GetDamagingUnit ()
    {
        return damagingUnit;
    }

    public Unit GetKillingUnit()
    {
        return killingUnit;
    }

    public Unit GetHealingUnit()
    {
        return healingUnit;
    }

    public Unit ClosestUnitToPoint(float distance, Vector3 point)
    {
        return Utilities.GetClosest<Unit>(point, distance);
    }

    public Unit FurthestUnitToPoint(float distance, Vector3 point)
    {
        return Utilities.GetFurthest<Unit>(point, distance);
    }

    public Unit ClosestUnitToUnit (float distance, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return null;
        }

        return Utilities.GetClosest<Unit>(unit.transform.position, distance);
    }

    public Unit FurthestUnitToUnit(float distance, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return null;
        }

        return Utilities.GetFurthest<Unit>(unit.transform.position, distance);
    }

    public Unit RandomNearbyUnitToUnit(float distance, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return null;
        }

        List<Unit> units = Utilities.GetAllWithinRange<Unit>(unit.transform.position, distance);
        if (units.Count == 0) return null;
        return units[Random.Range(0, units.Count)];
    }

    public Unit RandomNearbyUnitToPoint(float distance, Vector3 point)
    {
        List<Unit> units = Utilities.GetAllWithinRange<Unit>(point, distance);
        if (units.Count == 0) return null;
        return units[Random.Range(0, units.Count)];
    }

    public Unit GetUnitWithLabel (string label)
    {
        Monster[] monsters = GameObject.FindObjectsOfType<Monster>();
        foreach (Monster monster in monsters)
        {
            if (monster.monsterLabel == label) return monster;
        }
        return null;
    }

    public Unit GetUnitVariable(string name)
    {
        return (Unit)LogicEngine.current.localVariables.GetValueOrDefault(name, null);
    }

    public Unit GetGlobalUnitVariable(string name)
    {
        return (Unit)LogicEngine.globalVariables.GetValueOrDefault(name, null);
    }

    public Unit PopUnitFromUnitGroup (List<Unit> unitGroup)
    {
        if (unitGroup == null)
        {
            Error("The specified unit group is invalid.");
            return null;
        }

        Unit unit = unitGroup[0];
        unitGroup.RemoveAt(0);
        return unit;
    }

    #endregion

    #region Bool Value Nodes

    public bool KeyIsHeld (string keyString)
    {
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (key.ToString().ToUpper() == keyString.ToUpper() && Input.GetKey(key))
            {
                return true;   
            }
        }
        return false;
    }

    public bool UnitGroupIsEmpty (List<Unit> unitGroup)
    {
        if (unitGroup == null)
        {
            Error("The specified unit group is invalid.");
            return true;
        }

        return (unitGroup.Count == 0);
    }

    public bool UnitGroupIsNotEmpty(List<Unit> unitGroup)
    {
        if (unitGroup == null)
        {
            Error("The specified unit group is invalid.");
            return false;
        }

        return (unitGroup.Count > 0);
    }

    public bool UnitIsInUnitGroup (Unit unit, List<Unit> unitGroup)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return false;
        }
        if (unitGroup == null)
        {
            Error("The specified unit group is invalid.");
            return false;
        }

        return (unitGroup.Contains(unit));
    }

    public bool UnitIsNotInUnitGroup(Unit unit, List<Unit> unitGroup)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return true;
        }
        if (unitGroup == null)
        {
            Error("The specified unit group is invalid.");
            return true;
        }

        return (!unitGroup.Contains(unit));
    }

    public bool OrComparison (bool bool1, bool bool2)
    {
        return bool1 || bool2;
    }

    public bool AndComparison (bool bool1, bool bool2)
    {
        return (bool1 && bool2);
    }

    public bool BoolComparison (bool bool1, string comparator, bool bool2)
    {
        switch (comparator)
        {
            case "Equal To":
                return bool1 == bool2;
            case "Not Equal To":
                return bool1 != bool2;
            default:
                return false;
        }
    }

    public bool NumberComparison (float num1, string comparator, float num2)
    {
        switch (comparator)
        {
            case "Equal To":
                return num1 == num2;
            case "Not Equal To":
                return num1 != num2;
            case "Less Than":
                return num1 < num2;
            case "Less Than or Equal To":
                return num1 <= num2;
            case "Greater Than":
                return num1 > num2;
            case "Greater Than or Equal To":
                return num1 >= num2;
            default:
                return false;
        }
    }

    public bool VectorComparison(Vector3 vec1, string comparator, Vector3 vec2)
    {
        switch (comparator)
        {
            case "Equal To":
                return vec1 == vec2;
            case "Not Equal To":
                return vec1 != vec2;
            default:
                return false;
        }
    }

    public bool AbilityComparison(Ability ability1, string comparator, Ability ability2)
    {
        if (ability1 == null)
        {
            Error("The specified ability is invalid.");
            return false;
        }
        if (ability2 == null)
        {
            Error("The specified ability is invalid.");
            return false;
        }

        switch (comparator)
        {
            case "Equal To":
                return ability1 == ability2;
            case "Not Equal To":
                return ability1 != ability2;
            default:
                return false;
        }
    }

    public bool StringComparison(string string1, string comparator, string string2)
    {
        switch (comparator)
        {
            case "Equal To":
                return string1 == string2;
            case "Not Equal To":
                return string1 != string2;
            default:
                return false;
        }
    }

    public bool UnitComparison(Unit unit1, string comparator, Unit unit2)
    {
        if (unit1 == null)
        {
            Error("The specified unit is invalid.");
            return false;
        }
        if (unit2 == null)
        {
            Error("The specified unit is invalid.");
            return false;
        }

        switch (comparator)
        {
            case "Equal To":
                return unit1 == unit2;
            case "Not Equal To":
                return unit1 != unit2;
            default:
                return false;
        }
    }

    public bool GetBoolVariable(string name)
    {
        return (bool)LogicEngine.current.localVariables.GetValueOrDefault(name, false);
    }

    public bool GetGlobalBoolVariable(string name)
    {
        return (bool)LogicEngine.globalVariables.GetValueOrDefault(name, false);
    }

    public bool UnitTypeMatch (Unit unit1, Unit unit2)
    {
        if (unit1 == null)
        {
            Error("The specified unit is invalid.");
            return false;
        }
        if (unit2 == null)
        {
            Error("The specified unit is invalid.");
            return false;
        }

        return unit1.unitName == unit2.unitName;
    }

    public bool UnitIsMoving(Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return false;
        }

        return unit.IsMoving();
    }

    public bool UnitIsStationary(Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return true;
        }

        return !unit.IsMoving();
    }

    public bool UnitIsCasting(Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return false;
        }

        return !unit.IsCasting();
    }

    public bool UnitCanMove(Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return false;
        }

        return unit.CanMove();
    }

    public bool UnitHasBuff(Unit unit, string buff)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return true;
        }
        if (buff.Length == 0)
        {
            Error("The buff name cannot be empty.");
            return true;
        }

        return (unit.stats.HasBuffWithLabel(buff));
    }



    public bool UnitAbilityIsOnCooldown (Ability ability, Unit unit)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return true;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return true;
        }

        return false; // TODO: FIX THIS.
    }

    public bool UnitCanCastAbility (Unit unit, Ability ability)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return true;
        }
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return true;
        }

        return false; // TODO: FIX THIS.
    }

    public bool UnitIsEmpowered (Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return true;
        }

        if (unit is Monster monster)
        {
            return monster.isEmpowered;
        }
        return false;
    }

    public bool QuestIsActive (string label)
    {
        if (label.Length == 0)
        {
            Error("The quest name cannot be empty.");
            return true;
        }

        return GameManager.quests.activeQuests.ContainsKey(label);
    }

    public bool QuestIsCompleted (string label)
    {
        return false;
    }

    #endregion

    #region Ability Value Nodes

    public Ability LastAbilityCast ()
    {
        return lastAbilityCast; 
    }

    public Ability ThisAbility()
    {
        return (Ability)LogicEngine.current.engineHandler;
    }

    public Ability RandomAbilityOnUnit (Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return null;
        }
        if (unit.abilities.Count == 0)
        {
            Error("The specified unit has no abilities.");
            return null;
        }

        Ability ability = unit.abilities[Random.Range(0, unit.abilities.Count)];
        if (ability)
        {
            Error("The random ability was undefined.");
            return null;
        }
        return ability;
    }

    public Ability LastAbilityCastByUnit(Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return null;
        }
        return unit.lastAbilityCast;
    }

    #endregion

    #region String Value Nodes

    public string NameOfUnit (Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return null;
        }

        return unit.unitName;
    }

    public string NameOfAbility (Ability ability)
    {
        if (ability == null)
        {
            Error("The specified ability is invalid.");
            return "";
        }

        return ability.abilityName;
    }

    public string CombineStrings(string str1, string str2)
    {
        return str1 + str2;
    }

    public string GetStringVariable(string name)
    {
        return (string)LogicEngine.current.localVariables.GetValueOrDefault(name, "");
    }

    public string GetGlobalStringVariable(string name)
    {
        return (string)LogicEngine.globalVariables.GetValueOrDefault(name, "");
    }

    #endregion

    #region Vector Value Nodes

    public Vector3 PositionOfUnit (Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return Vector3.zero;
        }

        return unit.transform.position;
    }

    public Vector3 PositionOfProjectile(Projectile projectile)
    {
        if (projectile == null)
        {
            Error("The specified projectile is invalid.");
            return Vector3.zero;
        }

        return projectile.transform.position;
    }

    public Vector3 CastPointOfUnit(Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return Vector3.zero;
        }

        return unit.GetCastPoint();
    }

    public Vector3 AttackPointOfUnit (Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return Vector3.zero;
        }

        return unit.GetAttackPoint();
    }

    public Vector3 VectorAddition(Vector3 vec1, Vector3 vec2)
    {
        return vec1 + vec2;
    }

    public Vector3 VectorSubtraction(Vector3 vec1, Vector3 vec2)
    {
        return vec1 - vec2;
    }

    public Vector3 VectorMultiplication(Vector3 vec1, float value)
    {
        return vec1 * value;
    }

    public Vector3 VectorDivision(Vector3 vec1, float value)
    {
        return vec1 / value;
    }

    public Vector3 GetVectorVariable(string name)
    {
        return (Vector3)LogicEngine.current.localVariables.GetValueOrDefault(name, "");
    }

    public Vector3 GetGlobalVectorVariable(string name)
    {
        return (Vector3)LogicEngine.globalVariables.GetValueOrDefault(name, "");
    }

    
    public Vector3 RandomPointNearUnit (float distance, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return Vector3.zero;
        }

        Vector3 offset = Random.insideUnitSphere * distance;
        offset.y = 0;
        return unit.transform.position + offset;
    }

    public Vector3 RandomPointNearPoint(float distance, Vector3 point)
    {
        Vector3 offset = Random.insideUnitSphere * distance;
        offset.y = 0;
        return point + offset;
    }

    #endregion

    #region Unit Group Value Nodes

    public List<Unit> EmptyUnitGroup()
    {
        return new List<Unit>();
    }

    public List<Unit> GetUnitGroupVariable(string name)
    {
        return (List<Unit>)LogicEngine.current.localVariables.GetValueOrDefault(name, new List<Unit>());
    }

    public List<Unit> AllEnemiesInArcFromUnit(float arc, Unit unit, float distance)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return new List<Unit>();
        }
        return AllEnemiesInArc(unit, arc, unit.transform.position, unit.transform.position + unit.transform.forward * distance);
    }

    public List<Unit> AllEnemiesInArc (Unit factionCheck, float arc, Vector3 from, Vector3 to)
    {
        if (factionCheck == null)
        {
            Error("The specified unit is invalid.");
            return new List<Unit>();
        }

        float distance = Vector3.Distance(from, to);
        List<Unit> allNearbyUnits = Utilities.GetAllWithinRange<Unit>(from, distance);
        List<Unit> matches = new List<Unit>();

        Vector3 compare = (to - from).normalized;
        float threshold = (arc - 180) / -180;

        foreach (Unit possibleMatch in allNearbyUnits)
        {
            if (possibleMatch.GetFaction() != factionCheck.GetFaction())
            {
                float dot = Vector3.Dot(compare, (possibleMatch.transform.position - from).normalized);
                if (dot > threshold)
                {
                    matches.Add(possibleMatch);
                }
            }
        }
        return matches;
    }

    public List<Unit> AllUnitsWithinRangeOfPoint(float distance, Vector3 point)
    {
        return Utilities.GetAllWithinRange<Unit>(point, distance);
    }

    

    public List<Unit> AllUnitsWithinRangeOfUnit(float distance, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return new List<Unit>();
        }
        return Utilities.GetAllWithinRange<Unit>(unit.transform.position, distance);
    }

    public List<Unit> AllEnemiesWithinRangeOfPoint(Unit unit, float distance, Vector3 point)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return new List<Unit>();
        }
        List<Unit> allNearbyUnits = Utilities.GetAllWithinRange<Unit>(point, distance);
        List<Unit> enemies = new List<Unit>();
        foreach (Unit possibleMatch in allNearbyUnits)
            if (unit.GetFaction() != possibleMatch.GetFaction())
                enemies.Add(possibleMatch);
        return enemies;
    }

    public List<Unit> AllEnemiesWithinRangeOfUnit(Unit factionToCheck, float distance, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return new List<Unit>();
        }
        if (factionToCheck == null)
        {
            Error("The specified unit to faction check is invalid.");
            return new List<Unit>();
        }
        List<Unit> allNearbyUnits = Utilities.GetAllWithinRange<Unit>(unit.transform.position, distance);
        List<Unit> enemies = new List<Unit>();
        foreach (Unit possibleMatch in allNearbyUnits)
            if (factionToCheck.GetFaction() != possibleMatch.GetFaction())
                enemies.Add(possibleMatch);
        return enemies;
    }

    public List<Unit> RandomUnitsWithinRangeOfPoint (int count, float distance, Vector3 point)
    {
        List<Unit> units = Utilities.GetAllWithinRange<Unit>(point, distance);
        List<Unit> returnUnits = new List<Unit>();
        for (int i = 0; i < count; i++)
        {
            Unit u = units[Random.Range(0, units.Count)];
            units.Remove(u);
            returnUnits.Add(u);
        }
        return returnUnits;
    }

    public List<Unit> AllEnemiesWithinRangeOfPoint2(float distance, Vector3 point)
    {
        Unit factionToCheck = LogicEngine.current.GetOwner();
        List<Unit> allNearbyUnits = Utilities.GetAllWithinRange<Unit>(point, distance);
        List<Unit> enemies = new List<Unit>();
        foreach (Unit possibleMatch in allNearbyUnits)
            if (factionToCheck.GetFaction() != possibleMatch.GetFaction())
                enemies.Add(possibleMatch);
        return enemies;
    }

    public List<Unit> AllEnemiesWithinRangeOfUnit2(float distance, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return new List<Unit>();
        }

        Unit factionToCheck = LogicEngine.current.GetOwner();
        List<Unit> allNearbyUnits = Utilities.GetAllWithinRange<Unit>(unit.transform.position, distance);
        List<Unit> enemies = new List<Unit>();
        foreach (Unit possibleMatch in allNearbyUnits)
            if (factionToCheck.GetFaction() != possibleMatch.GetFaction())
                enemies.Add(possibleMatch);
        return enemies;
    } 


    public List<Unit> RandomUnitsWithinRangeOfUnit (int count, float distance, Unit unit)
    {
        if (unit == null)
        {
            Error("The specified unit is invalid.");
            return new List<Unit>();
        }

        List<Unit> units = Utilities.GetAllWithinRange<Unit>(unit.transform.position, distance);
        List<Unit> returnUnits = new List<Unit>();
        for (int i = 0; i < count; i++)
        {
            Unit u = units[Random.Range(0, units.Count)];
            units.Remove(u);
            returnUnits.Add(u);
        }
        return returnUnits;
    }

    #endregion

    #region Projectile Nodes

    public Projectile LastCreatedProjectile()
    {
        return lastCreatedProjectile;
    }

    #endregion
}