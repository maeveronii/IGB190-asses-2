using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public class GeneralNode
{
    public enum ReturnType { Value, Variable, Function, Temp, Preset, None }
    public ReturnType returnType = ReturnType.Value;

    // Temporary Evaluator
    public string tempName;
    
    // Function Evaluators
    public string functionName;
    public string functionDescription;
    public string functionDynamicDescription;

    [SerializeReference]
    public GeneralNode[] functionEvaluators = new GeneralNode[] { };

    public string presetName;
    public string variableName;

    public string stringSuffix = "";

    public int indent = 0;

    public string nodeIcon = "";

    public bool allowValue = true;
    public bool allowPreset = true;
    public bool allowFunction = true;

    public virtual bool CanHaveChildren ()
    {
        return false;
    }
     
    public GeneralNode() { }

    public virtual bool IsHovered (Rect parent)
    {
        return (parent.Contains(Event.current.mousePosition));
    }

    public override string ToString()
    {
        if (GetValue() == null)
            return "Undefined";
        else
        {
            if (GetValue() is string)
            {
                string text = GetValue().ToString();
                if (text.Length < 70) 
                    return text + stringSuffix;
                else
                    return text.Substring(0, 66) + stringSuffix + " ...";
            }
            //if (GetValue() is GameObject) return ((GameObject)GetValue()).name + stringSuffix;
            //if (GetValue() is GameFeedback) return ((GameFeedback)GetValue()).name + stringSuffix;
            //if (GetValue() is Projectile) return ((Projectile)GetValue()).name + stringSuffix;

            if (GetValue() is GameObject go)
            {
                if (go == null)
                {
                    returnType = ReturnType.Temp;
                    return "MISSING VALUE";
                }
                else
                {
                    return ((GameObject)GetValue()).name + stringSuffix;
                }
            }
            if (GetValue() is GameFeedback feedback)
            {
                if (feedback == null)
                {
                    returnType = ReturnType.Temp;
                    return "MISSING VALUE";
                }
                else
                {
                    return ((GameFeedback)GetValue()).name + stringSuffix;
                }
            }
            if (GetValue() is Projectile projectile)
            {
                if (projectile == null)
                {
                    returnType = ReturnType.Temp;
                    return "MISSING VALUE";
                }
                else
                {
                    return ((Projectile)GetValue()).name + stringSuffix;
                }
            }
            if (GetValue() is Unit unit)
            { 
                if (unit == null)
                {
                    returnType = ReturnType.Temp;
                    return "MISSING VALUE";
                }
                else
                {
                    return((Unit)GetValue()).name + stringSuffix;
                }
            }



            if (GetValue() is AudioClip) return ((AudioClip)GetValue()).name + stringSuffix;
            if (GetValue() is Color)
            {
                string text = $"#{ColorUtility.ToHtmlStringRGB((Color)GetValue())}";
                return $"<color={text}>{text}</color>";

                //return "#" + ColorUtility.ToHtmlStringRGB((Color)GetValue());
            }
            return GetValue().ToString() + stringSuffix;
        }
    }

    public object Resolve (Dictionary<string, object> presets, LogicEngine engine, LogicScript script)
    {
        LogicEngine.current = engine;
        LogicEngine.currentScript = script;
        LogicEngine.currentNode = this;
        if (presets == null || !presets.ContainsKey("CurrentLineID"))
            LogicEngine.currentLine = 0;
        else
            LogicEngine.currentLine = (int)presets["CurrentLineID"];
        LogicEngine.currentPresets = presets;

        if (returnType == ReturnType.Value)
            return GetValue();
        else if (returnType == ReturnType.Temp)
            return null;
        else if (returnType == ReturnType.Variable)
            return null;
        else if (returnType == ReturnType.Preset)
        {
            if (presets.ContainsKey(presetName))
            {
                return presets[presetName];
            }
            else
            {
                MethodInfo methodInfo = (typeof(LogicScript)).GetMethod(LogicEngine.DYNAMIC_PRESETS[presetName]);
                if (methodInfo == null) Debug.Log($"No method named {LogicEngine.DYNAMIC_PRESETS[presetName]} found!");
                return methodInfo.Invoke(script, null);
            }
            
        }
        else
        {
            if (presets != null && presets.ContainsKey(functionName))
            {
                return presets[functionName];
            }

            MethodInfo methodInfo = (typeof(LogicScript)).GetMethod(functionName);
            if (methodInfo == null) Debug.Log($"No method named {functionName} found!");
            object[] resolvedArgs = new object[functionEvaluators.Length];
            for (int i = 0; i < resolvedArgs.Length; i++)
            {
                resolvedArgs[i] = functionEvaluators[i].Resolve(presets, engine, script);
            }
            return methodInfo.Invoke(script, resolvedArgs);
        }
    }

    public virtual bool Validate ()
    {
        if (returnType == ReturnType.Value)
        {
            return true;
        }
        else if (returnType == ReturnType.Temp)
        {
            return false;
        }
        else if (returnType == ReturnType.Variable)
        {
            return false;
        }
        else if (returnType == ReturnType.Function)
        {
            bool valid = true;
            foreach (GeneralNode value in functionEvaluators)
            {
                if (!value.Validate())
                {
                    valid = false;
                }
            }
            return valid;
        }
        return true;
    }

    /*
    protected virtual float GetDrawWidth ()
    {
        GUIContent content = null;
        if (returnType == ReturnType.Function)
        {
            float width = 0;
            string[] parts = functionDynamicDescription.Split('$');
            foreach (string part in parts)
            {
                if (part.Length > 0)
                {
                    content = new GUIContent(part);
                    width += MyTestWindow.windowStyle_BaseText.CalcSize(content).x;
                }
            }
            foreach (GeneralNode node in functionEvaluators)
            {
                width += node.GetDrawWidth();
            }
            return width + 10;
        }
        if (returnType == ReturnType.Value) { content = new GUIContent(ToString() + stringSuffix); }
        if (returnType == ReturnType.Temp) { content = new GUIContent(tempName); }
        if (returnType == ReturnType.Variable) { content = new GUIContent(presetName); }
        return MyTestWindow.windowStyle_HoveredNode.CalcSize(content).x;
    }

    public void AddNodeAsChild (GeneralNode node, GeneralNode childTemplate)
    {
        int id = -1;
        GeneralNode child = childTemplate.Copy();
        child.indent = node.indent + 1;
        if ((id = MyTestWindow.selectedScript.eventNodes.IndexOf(node)) >= 0)
            MyTestWindow.selectedScript.eventNodes.Insert(id + 1, child);
        else if ((id = MyTestWindow.selectedScript.conditionNodes.IndexOf(node)) >= 0)
            MyTestWindow.selectedScript.conditionNodes.Insert(id + 1, child);
        else if ((id = MyTestWindow.selectedScript.actionNodes.IndexOf(node)) >= 0)
            MyTestWindow.selectedScript.actionNodes.Insert(id + 1, child);
        EditorUtility.SetDirty(MyTestWindow.selectedAbility);
    }

    public virtual float Draw (EditorWindow window, Rect rect, int depth = 1)
    {
        // Determine the correct style for the node.
        GUIStyle style = null;
        if (this == MyTestWindow.hoveredNode)
            style = MyTestWindow.windowStyle_HoveredNode;
        else if (depth % 2 == 1)
            style = MyTestWindow.windowStyle_OddNode;
        else
            style = MyTestWindow.windowStyle_EvenNode;
        
        // Determine the correct width for the node.
        Rect temp = new Rect(rect);
        temp.width = GetDrawWidth();
        temp.height -= depth * 2;
        temp.y += depth * 1;

        GUIContent content = null;
        if (this is EventNode || (returnType == ReturnType.Function && functionEvaluators.Length > 0))
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
                    if (MyTestWindow.hoveredNode == null || depth > MyTestWindow.hoveredNodeDepth)
                    {
                        MyTestWindow.hoveredNode = this;
                        MyTestWindow.hoveredNodeDepth = depth;
                    }
                }
                else
                {
                    if (this == MyTestWindow.hoveredNode)
                    {
                        MyTestWindow.hoveredNode = null;
                    }
                }
                        
                // Draw the button to allow for edits.    
                if (MyTestWindow.hoveredNode == this)
                {
                    if (GUI.Button(temp, "", style))
                    {
                        SetValueEditor.OpenWindow(window, window.position.min + new Vector2(temp.xMin, temp.yMin + temp.height * 1.5f), this, MyTestWindow.selectedAbility);
                    }
                }
                else
                {
                    GUI.Label(temp, "", style);
                }
            }

            // If the node can have children actions, create the add button.
            if (CanHaveChildren())
            {
                if (GUI.Button(new Rect(rect.x + rect.width - 40, rect.y - 1, 39, 39), "+", MyTestWindow.windowStyle_AddButton))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (GeneralNode node in MyTestWindow.allNodes)
                    {
                        if (node is ActionNode)
                        {
                            menu.AddItem(new GUIContent(node.functionDescription), false, () => { AddNodeAsChild(this, node); });
                        }
                    }
                    menu.ShowAsContext();
                }
            }

            // Draw each part of the function node.
            temp.x += 5;
            string[] parts = functionDynamicDescription.Split('$');
            int next = 0;
            foreach (string part in parts)
            {
                content = new GUIContent(part);
                temp.width = MyTestWindow.windowStyle_BaseText.CalcSize(content).x;
                GUI.Label(temp, part, MyTestWindow.windowStyle_BaseText);
                temp.x += temp.width;
                if (next < functionEvaluators.Length)
                {
                    temp.x += functionEvaluators[next].Draw(window, temp, depth + 1);
                    next++;
                }
            }
            return GetDrawWidth();
        }

        // Assign the appropriate text and color for the node.

        
        EditorGUIUtility.AddCursorRect(temp, MouseCursor.Link);

        // Check to see if the mouse is currently hovering over this node. If so,
        // it should be drawn in a different colour.
        if (temp.Contains(Event.current.mousePosition))
        {
            if (MyTestWindow.hoveredNode == null || depth > MyTestWindow.hoveredNodeDepth)
            {
                MyTestWindow.hoveredNode = this;
                MyTestWindow.hoveredNodeDepth = depth;
            }
        }
        else
        {
            if (this == MyTestWindow.hoveredNode)
            {
                MyTestWindow.hoveredNode = null;
            }
        }

        // Draw the node as a button (if it is hovered), or a label which looks like a button if not.
        // This is needed to get correct clicks on the window. 
        if (MyTestWindow.hoveredNode == this)
        {
            if (GUI.Button(temp, content, style))
            {
                SetValueEditor.OpenWindow(window, window.position.min + new Vector2(temp.xMin, temp.yMin + temp.height*1.5f), this, MyTestWindow.selectedAbility);
            }
        }
        else
        {
            GUI.Label(temp, content, style);
        }
        return temp.width;
    }

    private float CalcContentWidth (GUIStyle style, GUIContent content, Color color)
    {
        style.normal.textColor = style.hover.textColor = style.focused.textColor = style.active.textColor = color;
        return style.CalcSize(content).x;
    }
    */
     
    public GeneralNode Copy ()
    {
        GeneralNode node = New();
        node.CopyFrom(this);
        return node;
    }

    public virtual GeneralNode CopyFrom(GeneralNode toCopy)
    {
        returnType = toCopy.returnType;
        tempName = toCopy.tempName;
        functionDescription = toCopy.functionDescription;
        functionDynamicDescription = toCopy.functionDynamicDescription;
        functionName = toCopy.functionName;
        functionEvaluators = toCopy.functionEvaluators;
        presetName = toCopy.presetName;
        variableName = toCopy.variableName;
        indent = toCopy.indent;
        stringSuffix = toCopy.stringSuffix;
        nodeIcon = toCopy.nodeIcon;

        allowValue = toCopy.allowValue;
        allowPreset = toCopy.allowPreset;
        allowFunction = toCopy.allowFunction;
        SetValue(toCopy.GetValue());

        functionEvaluators = new GeneralNode[toCopy.functionEvaluators.Length];
        for (int i = 0; i < functionEvaluators.Length; i++)
            functionEvaluators[i] = toCopy.functionEvaluators[i].Copy();

        return this;
    }
    

    public virtual GeneralNode New() { return new GeneralNode(); }
    public virtual object GetValue () { return null; }
    public virtual void SetValue(object value) { }

    public virtual object Evaluate ()
    {
        if (returnType == ReturnType.Value) return GetValue();
        if (returnType == ReturnType.Function) return null;
        if (returnType == ReturnType.Variable) return null;
        if (returnType == ReturnType.Temp) return null;
        return null;
    }


    public GeneralNode NoValue ()
    {
        allowValue = false;
        return this;
    }

    public GeneralNode NoPreset()
    {
        allowPreset = false;
        return this;
    }

    public GeneralNode NoFunction()
    {
        allowFunction = false;
        return this;
    }

    public static GeneralNode Value<T>(object value, string suffix = "") where T : GeneralNode, new()
    {
        GeneralNode node = new T();
        node.returnType = ReturnType.Value;
        node.SetValue(value);
        node.stringSuffix = suffix;
        return node;
    }

    public static GeneralNode Temp<T>(string tempName, string suffix = "", bool allowValue = true, bool allowPreset = true, bool allowFunction = true) where T : GeneralNode, new()
    {
        GeneralNode node = new T();
        node.returnType = ReturnType.Temp;
        node.tempName = tempName;
        node.stringSuffix = suffix;
        node.allowValue = allowValue;
        node.allowPreset = allowPreset;
        node.allowFunction = allowFunction;
        return node;
    }

    public static GeneralNode Preset<T>(string presetName) where T : GeneralNode, new()
    {
        GeneralNode node = new T();
        node.returnType = ReturnType.Preset;
        node.presetName = presetName;
        return node;
    }

    public static GeneralNode Func<T>(string desc, string dynamicDesc,
        string functionName, params GeneralNode[] args) where T : GeneralNode, new()
    {
        return new T()
        {
            returnType = ReturnType.Function,
            functionDescription = desc,
            functionDynamicDescription = dynamicDesc,
            functionName = functionName,
            functionEvaluators = args,
        };
    }

    public static GeneralNode Func<T>(string desc, string dynamicDesc,
        string functionName, string nodeIcon, params GeneralNode[] args) where T : GeneralNode, new()
    {
        return new T()
        {
            returnType = ReturnType.Function,
            functionDescription = desc,
            functionDynamicDescription = dynamicDesc,
            functionName = functionName,
            functionEvaluators = args,
            nodeIcon = nodeIcon
        };
    }
}

/// <summary>
/// Stores the data for an event node - should only store a function to run.
/// </summary>
[System.Serializable]
public class EventNode : GeneralNode
{
    public string[] presets;

    public override GeneralNode New() { return new EventNode(); }

    public static GeneralNode Func(string desc, string dynamicDesc,
        string functionName, string nodeIcon, string[] presetValues = null, params GeneralNode[] args)
    {
        if (presetValues != null && presetValues.Length > 0)
        {
            string extraDesc = "";
            for (int i = 0; i < presetValues.Length; i++)
            {
                string end = (i != presetValues.Length - 1) ? ", " : "";
                extraDesc += $"<color=yellow>{presetValues[i]}</color>{end}";
            }
            dynamicDesc += $" (Presets: {extraDesc})";
        }

        EventNode node = (EventNode)GeneralNode.Func<EventNode>(desc, dynamicDesc, functionName, nodeIcon, args);

        if (presetValues == null)
            node.presets = new string[] { };
        else
        {
            node.presets = presetValues;
        }
        return node;
    }

    public override GeneralNode CopyFrom(GeneralNode toCopy)
    {
        GeneralNode copy = base.CopyFrom(toCopy);
        EventNode copyEvent = (EventNode)copy;
        EventNode originalEvent = (EventNode)toCopy;
        if (originalEvent.presets == null)
            copyEvent.presets = null;
        else
        {
            copyEvent.presets = new string[originalEvent.presets.Length];
            for (int i = 0; i < originalEvent.presets.Length; i++)
                copyEvent.presets[i] = originalEvent.presets[i];
        }
        
        return copyEvent;
    }
}

/// <summary>
/// Stores the data for an action node - should only store a function to run.
/// </summary>
[System.Serializable]
public class NestingActionNode : ActionNode
{
    public override GeneralNode New() { return new NestingActionNode(); }
    public override bool CanHaveChildren()
    {
        return true;
    }
}

/// <summary>
/// Stores the data for an action node - should only store a function to run.
/// </summary>
[System.Serializable]
public class ActionNode : GeneralNode
{
    public override GeneralNode New() { return new ActionNode(); }
}

/// <summary>
/// Stores the data for a number node - i.e. a node that is storing a number value,
/// or a function which will resolve into a number.
/// </summary>
[System.Serializable]
public class NumberNode : GeneralNode
{
    public float value;

    public override GeneralNode New() { return new NumberNode(); }

    public override void SetValue(object value) { this.value = (float)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string suffix = "", string t = "Number") { return Temp<NumberNode>(t, suffix); }

    public static GeneralNode Value(float value, string suffix = "") {
        return Value<NumberNode>(value, suffix); 
    }
}

/// <summary>
/// Stores the data for a unit node - i.e. a node that is storing a unit,
/// or a function which will resolve into a unit.
/// </summary>
[System.Serializable]
public class UnitNode : GeneralNode
{
    public Unit value;

    public override GeneralNode New() { return new UnitNode(); }

    public override void SetValue(object value) { this.value = (Unit)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Unit") { return Temp<UnitNode>(t); }

    public static GeneralNode Value(Unit value) { return Value<UnitNode>(value); }
}

/// <summary>
/// Stores the data for a vector node - i.e. a node that is storing a vector value,
/// or a function which will resolve into a vector.
/// </summary>
[System.Serializable]
public class VectorNode : GeneralNode
{
    public Vector3 value;

    public override GeneralNode New() { return new VectorNode(); }

    public override void SetValue(object value) { this.value = (Vector3)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Location") { return Temp<VectorNode>(t); }

    public static GeneralNode Value(Vector3 value) { return Value<VectorNode>(value); }
}

/// <summary>
/// Stores the data for a color node - i.e. a node that is storing a color value,
/// or a function which will resolve into a color.
/// </summary>
[System.Serializable]
public class ColorNode : GeneralNode
{
    public Color value;

    public override GeneralNode New() { return new ColorNode(); }

    public override void SetValue(object value) { this.value = (Color)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Color") { return Temp<ColorNode>(t); }

    public static GeneralNode Value(Color value) { return Value<ColorNode>(value); }
}

/// <summary>
/// Stores the data for a bool node - i.e. a node that is storing a bool,
/// or a function which will resolve into a bool.
/// </summary>
[System.Serializable]
public class BoolNode : GeneralNode
{
    public bool value;

    public override GeneralNode New() { return new BoolNode(); }

    public override void SetValue(object value) { this.value = (bool)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Bool") { return Temp<BoolNode>(t); }

    public static GeneralNode Value(bool value) { return Value<BoolNode>(value); }
}

/// <summary>
/// Stores the data for a ability node - i.e. a node that is storing an ability,
/// or a function which will resolve into a ability.
/// </summary>
[System.Serializable]
public class AbilityNode : GeneralNode
{
    public Ability value;

    public override GeneralNode New() { return new AbilityNode(); }

    public override void SetValue(object value) { this.value = (Ability)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Ability") { return Temp<AbilityNode>(t); }

    public static GeneralNode Value(Ability value) { return Value<AbilityNode>(value); }
}

/// <summary>
/// Stores the data for a ability node - i.e. a node that is storing an ability,
/// or a function which will resolve into a ability.
/// </summary>
[System.Serializable]
public class AudioNode : GeneralNode
{
    public AudioClip value;

    public override GeneralNode New() { return new AudioNode(); }

    public override void SetValue(object value) { this.value = (AudioClip)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Audio Clip") { return Temp<AudioNode>(t); }

    public static GeneralNode Value(AudioClip value) { return Value<AudioNode>(value); }
}

/// <summary>
/// Stores the data for a ability node - i.e. a node that is storing an ability,
/// or a function which will resolve into a ability.
/// </summary>
[System.Serializable]
public class GameFeedbackNode : GeneralNode
{
    public GameFeedback value;

    public override GeneralNode New() { return new GameFeedbackNode(); }

    public override void SetValue(object value) { this.value = (GameFeedback)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Game Feedback") { return Temp<GameFeedbackNode>(t); }

    public static GeneralNode Value(GameFeedback value) { return Value<GameFeedbackNode>(value); }
}

/// <summary>
/// Stores the data for a ability node - i.e. a node that is storing an ability,
/// or a function which will resolve into a ability.
/// </summary>
[System.Serializable]
public class EffectNode : GeneralNode
{
    public CustomVisualEffect value;

    public override GeneralNode New() { return new EffectNode(); }

    public override void SetValue(object value) { this.value = (CustomVisualEffect)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Effect") { return Temp<EffectNode>(t); }

    public static GeneralNode Value(CustomVisualEffect value) { return Value<EffectNode>(value); }
}

[System.Serializable]
public class GameObjectNode : GeneralNode
{
    public GameObject value;

    public override GeneralNode New() { return new GameObjectNode(); }

    public override void SetValue(object value) { this.value = (GameObject)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Game Object") { return Temp<GameObjectNode>(t); }

    public static GeneralNode Value(GameObject value) { return Value<GameObjectNode>(value); }
}

public class ProjectileNode : GeneralNode
{
    public Projectile value;

    public override GeneralNode New() { return new ProjectileNode(); }

    public override void SetValue(object value) { this.value = (Projectile)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Projectile") { return Temp<ProjectileNode>(t); }

    public static GeneralNode Value(Projectile value) { return Value<ProjectileNode>(value); }
}

public class ItemNode : GeneralNode
{
    public Item value;

    public override GeneralNode New() { return new ItemNode(); }

    public override void SetValue(object value) { this.value = (Item)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Item") { return Temp<ItemNode>(t); }

    public static GeneralNode Value(Item value) { return Value<ItemNode>(value); }
}

public class GroupNode : GeneralNode { }

/// <summary>
/// Stores the data for a unit group node - i.e. a node that resolves into a group of units.
/// </summary>
[System.Serializable]
public class UnitGroupNode : GroupNode
{
    public override GeneralNode New() { return new UnitGroupNode(); }
    public static GeneralNode Temp(string t = "Unit Group") { return Temp<UnitGroupNode>(t); }
}

/// <summary>
/// Stores the data for a unit group node - i.e. a node that resolves into a group of units.
/// </summary>
[System.Serializable]
public class VectorGroupNode : GroupNode
{
    public override GeneralNode New() { return new VectorGroupNode(); }
    public static GeneralNode Temp(string t = "Vector Group") { return Temp<VectorGroupNode>(t); }
}


/// <summary>
/// Stores the data for a ability node - i.e. a node that is storing an ability,
/// or a function which will resolve into a ability.
/// </summary>
[System.Serializable]
public class StringNode : GeneralNode
{
    public string value;
    public string[] options = new string[] { };

    public override GeneralNode New() { return new StringNode(); }

    public override void SetValue(object value) { this.value = (string)value; }

    public override object GetValue() { return value; }

    public static GeneralNode Temp(string t = "Text") { return Temp<StringNode>(t); }

    public static GeneralNode Value(string value)
    {
        StringNode node = (StringNode)Value<StringNode>(value);
        return node;
    }

    public static GeneralNode Value(string value, string[] options) 
    { 
        StringNode node = (StringNode)Value<StringNode>(value);
        node.options = new string[options.Length];
        for (int i = 0; i < options.Length; i++)
            node.options[i] = options[i];
        return node; 
    }

    public override GeneralNode CopyFrom(GeneralNode toCopy)
    {
        GeneralNode copy = base.CopyFrom(toCopy);
        StringNode copyString = (StringNode)copy;
        StringNode originalString = (StringNode)toCopy;
        copyString.options = new string[originalString.options.Length];
        for (int i = 0; i < originalString.options.Length; i++)
            copyString.options[i] = originalString.options[i];
        return copyString;
    }
}