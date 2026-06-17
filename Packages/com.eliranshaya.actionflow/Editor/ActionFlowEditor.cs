using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Core
{
    [CustomPropertyDrawer(typeof(DurationMode))]
    public class DurationModeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // DurationType field

            var durationTypeProp = property.FindPropertyRelative("DurationType");
            if (durationTypeProp == null) return height;

            bool isAbsolute = durationTypeProp.enumValueIndex == (int)DurationType.Absolute;

            if (isAbsolute)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            else
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var durationTypeProp = property.FindPropertyRelative("DurationType");
            var durationProp = property.FindPropertyRelative("Duration");
            var minDurationProp = property.FindPropertyRelative("MinDuration");
            var maxDurationProp = property.FindPropertyRelative("MaxDuration");

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(rect, durationTypeProp);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            bool isAbsolute = durationTypeProp.enumValueIndex == (int)DurationType.Absolute;

            if (isAbsolute)
            {
                EditorGUI.PropertyField(rect, durationProp);
            }
            else
            {
                EditorGUI.PropertyField(rect, minDurationProp);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, maxDurationProp);
            }

            EditorGUI.EndProperty();
        }
    }
    [CustomPropertyDrawer(typeof(ActionFlow), true)]
    public class ActionFlowEditor : PropertyDrawer
    {
        private class GroupData
        {
            public List<SerializedProperty> Properties = new List<SerializedProperty>();
            public InspectorGroupAttribute Attribute;
        }

        private static Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private static Dictionary<string, List<SerializedProperty>> _groupedProperties = new Dictionary<string, List<SerializedProperty>>();

        // Clipboard for copy/paste
        private static ActionFlow _clipboard = null;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Header line

            if (property.isExpanded)
            {
                height += 5f; // Top padding for content area

                // Add description height if available
                ActionFlow flowForDescription = property.managedReferenceValue as ActionFlow;
                if (flowForDescription != null)
                {
                    string description = flowForDescription.GetDescription();
                    if (!string.IsNullOrEmpty(description))
                    {
                        float contentWidth = EditorGUIUtility.currentViewWidth - 40f;
                        GUIStyle descriptionStyle = new GUIStyle(EditorStyles.helpBox);
                        descriptionStyle.fontSize = 11;
                        descriptionStyle.padding = new RectOffset(10, 10, 6, 6);
                        descriptionStyle.wordWrap = true;

                        float descriptionHeight = descriptionStyle.CalcHeight(new GUIContent(description), contentWidth);
                        height += 5 + descriptionHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }

                // Add warning height if needed
                string warningMessage = GetWarningMessage(property);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    // Approximate width (will be accurate enough)
                    float contentWidth = EditorGUIUtility.currentViewWidth - 30f;
                    height += GetWarningHeight(warningMessage, contentWidth) + EditorGUIUtility.standardVerticalSpacing;
                }

                var groups = GetGroupedProperties(property);

                foreach (var group in groups)
                {
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    string groupKey = property.propertyPath + "_" + group.Key;
                    bool closedByDefault = group.Value.Attribute != null ? group.Value.Attribute.ClosedByDefault : true;

                    bool isGroupExpanded = GetGroupFoldoutState(groupKey, !closedByDefault);

                    if (isGroupExpanded)
                    {
                        foreach (var prop in group.Value.Properties)
                            height += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                    }
                }

                height += 5f; // Bottom padding for content area
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null)
            {
                DrawTypeSelector(position, property);
                return;
            }

            var actionType = property.managedReferenceValue.GetType();
            var labelProperty = property.FindPropertyRelative("Label");

            // Use the Label field value for display
            string displayName = labelProperty?.stringValue;
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = NicifyTypeName(actionType.Name.Replace("ActionFlow", ""));
            }

            // Get duration
            float duration = 0f;
            if (property.managedReferenceValue is ActionFlow actionFlow)
            {
                duration = actionFlow.GetDuration();
            }

            // Draw custom header with duration on the right
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            DrawCustomHeader(headerRect, property, displayName, duration);

            // Draw properties if expanded
            if (property.isExpanded)
            {
                // Calculate content area
                Rect contentAreaRect = new Rect(position.x,
                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    position.height - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

                // Draw background for content area
                if (Event.current.type == EventType.Repaint)
                {
                    Color bgColor = EditorGUIUtility.isProSkin
                        ? new Color(0.35f, 0.35f, 0.35f, 0.8f) // Dark skin - lighter gray
                        : new Color(0.85f, 0.85f, 0.85f, 0.8f); // Light skin - light gray
                    EditorGUI.DrawRect(contentAreaRect, bgColor);
                }

                EditorGUI.indentLevel++;

                Rect contentRect = new Rect(position.x + 5f, // Add small padding
                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 5f,
                    position.width - 10f, // Reduce width for padding
                    0);

                // Draw description if available
                ActionFlow flowForDescription = property.managedReferenceValue as ActionFlow;
                if (flowForDescription != null)
                {
                    string description = flowForDescription.GetDescription();
                    if (!string.IsNullOrEmpty(description))
                    {
                        GUIStyle descriptionStyle = new GUIStyle(EditorStyles.helpBox);
                        descriptionStyle.fontSize = 11;
                        descriptionStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                        descriptionStyle.padding = new RectOffset(10, 10, 6, 6);
                        descriptionStyle.wordWrap = true;
                        descriptionStyle.richText = false;

                        float descriptionHeight = descriptionStyle.CalcHeight(new GUIContent(description), contentRect.width);
                        Rect descriptionRect = new Rect(contentRect.x, contentRect.y, contentRect.width, descriptionHeight);

                        // Draw description box
                        GUI.Box(descriptionRect, description, descriptionStyle);

                        contentRect.y += descriptionHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }

                // Check for missing required fields and show warning
                string warningMessage = GetWarningMessage(property);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    float warningHeight = GetWarningHeight(warningMessage, contentRect.width);
                    Rect warningRect = new Rect(contentRect.x, contentRect.y, contentRect.width, warningHeight);
                    EditorGUI.HelpBox(warningRect, warningMessage, MessageType.Warning);
                    contentRect.y += warningHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                var groups = GetGroupedProperties(property);

                foreach (var group in groups)
                {
                    string groupKey = property.propertyPath + "_" + group.Key;
                    bool closedByDefault = group.Value.Attribute != null ? group.Value.Attribute.ClosedByDefault : true;

                    bool isGroupExpanded = GetGroupFoldoutState(groupKey, !closedByDefault);
                    float hue = (group.Value.Attribute?.GroupColorIndex ?? 24) / 139f;
                    Color groupBgColor = Color.HSVToRGB(hue, EditorGUIUtility.isProSkin ? 0.4f : 0.3f, EditorGUIUtility.isProSkin ? 0.25f : 0.75f);
                    groupBgColor.a = 0.4f;

                    Rect groupHeaderRect = new Rect(contentRect.x, contentRect.y, contentRect.width, EditorGUIUtility.singleLineHeight);

                    if (Event.current.type == EventType.Repaint)
                        EditorGUI.DrawRect(groupHeaderRect, groupBgColor);

                    isGroupExpanded = EditorGUI.Foldout(groupHeaderRect, isGroupExpanded, group.Key, true, EditorStyles.foldoutHeader);
                    SetGroupFoldoutState(groupKey, isGroupExpanded);

                    contentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    if (isGroupExpanded)
                    {
                        EditorGUI.indentLevel++;

                        foreach (var prop in group.Value.Properties)
                        {
                            contentRect.height = EditorGUI.GetPropertyHeight(prop, true);

                            // Check for AnimatorState attribute
                            FieldInfo fieldInfo = FindField(property.managedReferenceValue.GetType(),
                                prop.propertyPath, property.propertyPath);

                            var animatorStateAttr = fieldInfo?.GetCustomAttribute<AnimatorStateAttribute>();

                            if (animatorStateAttr != null && prop.propertyType == SerializedPropertyType.String)
                            {
                                DrawAnimatorStateDropdown(contentRect, prop, property, animatorStateAttr.AnimatorFieldName);
                            }
                            else
                            {
                                EditorGUI.PropertyField(contentRect, prop, true);
                            }

                            contentRect.y += contentRect.height + EditorGUIUtility.standardVerticalSpacing;
                        }

                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private Dictionary<string, GroupData> GetGroupedProperties(SerializedProperty property)
        {
            var groups = new Dictionary<string, GroupData>();
            string currentGroup = "Properties";

            if (property.managedReferenceValue == null) return groups;

            CollectProperties(property, ref currentGroup, groups);
            return groups;
        }

        private void CollectProperties(SerializedProperty rootProperty, ref string currentGroup, Dictionary<string, GroupData> groups)
        {
            SerializedProperty iterator = rootProperty.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            iterator.NextVisible(true);

            InspectorGroupAttribute currentGroupAttr = null;

            do
            {
                if (SerializedProperty.EqualContents(iterator, end)) break;

                FieldInfo fieldInfo = FindField(rootProperty.managedReferenceValue.GetType(),
                    iterator.propertyPath,
                    rootProperty.propertyPath);

                if (fieldInfo != null)
                {
                    var conditionAttr = fieldInfo.GetCustomAttribute<ConditionAttribute>();
                    if (conditionAttr != null)
                    {
                        if (!EvaluateCondition(rootProperty, iterator, conditionAttr))
                            continue;
                    }

                    var groupAttr = fieldInfo.GetCustomAttribute<InspectorGroupAttribute>();
                    if (groupAttr != null)
                    {
                        currentGroup = groupAttr.GroupName;
                        currentGroupAttr = groupAttr;
                    }
                }

                if (!groups.ContainsKey(currentGroup))
                    groups[currentGroup] = new GroupData { Attribute = currentGroupAttr };

                groups[currentGroup].Properties.Add(iterator.Copy());
            } while (iterator.NextVisible(false));
        }

        private FieldInfo FindField(Type rootType, string fullPath, string rootPath)
        {
            // Strip the root property path prefix to get relative path
            // e.g. "ActionFlows.Array.data[0].DurationHelper.Duration" -> "DurationHelper.Duration"
            string relativePath = fullPath;
            if (fullPath.StartsWith(rootPath + "."))
                relativePath = fullPath.Substring(rootPath.Length + 1);

            string[] parts = relativePath.Split('.');

            Type currentType = rootType;
            FieldInfo result = null;

            foreach (var part in parts)
            {
                if (currentType == null) return null;

                result = currentType.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (result == null) return null;

                currentType = result.FieldType;
            }

            return result;
        }

        private bool EvaluateCondition(SerializedProperty rootProperty, SerializedProperty currentProperty, ConditionAttribute condition)
        {
            string parentPath = currentProperty.propertyPath.Contains(".")
                ? currentProperty.propertyPath.Substring(0, currentProperty.propertyPath.LastIndexOf('.'))
                : null;

            SerializedProperty FindProp(string fieldName)
            {
                SerializedProperty prop = null;
                if (parentPath != null)
                    prop = rootProperty.serializedObject.FindProperty(parentPath + "." + fieldName);
                if (prop == null)
                    prop = rootProperty.FindPropertyRelative(fieldName);
                return prop;
            }

            // --- Evaluate bool condition ---
            bool boolResult = true;
            if (!string.IsNullOrEmpty(condition.BoolField))
            {
                SerializedProperty boolProp = FindProp(condition.BoolField);
                if (boolProp != null && boolProp.propertyType == SerializedPropertyType.Boolean)
                    boolResult = boolProp.boolValue == condition.BoolExpected;
            }

            // --- Evaluate enum condition ---
            bool enumResult = true;
            if (!string.IsNullOrEmpty(condition.EnumField))
            {
                SerializedProperty enumProp = FindProp(condition.EnumField);
                if (enumProp != null && enumProp.propertyType == SerializedPropertyType.Enum)
                {
                    string currentEnumName = enumProp.enumNames[enumProp.enumValueIndex];

                    if (condition.EnumValues != null && condition.EnumValues.Length > 0)
                        enumResult = System.Array.IndexOf(condition.EnumValues, currentEnumName) >= 0;
                    else if (!string.IsNullOrEmpty(condition.EnumValue))
                        enumResult = currentEnumName == condition.EnumValue;
                }
            }

            return boolResult && enumResult;
        }

        private bool GetGroupFoldoutState(string key, bool defaultValue)
        {
            if (!_foldoutStates.ContainsKey(key))
            {
                _foldoutStates[key] = defaultValue;
            }

            return _foldoutStates[key];
        }

        private void SetGroupFoldoutState(string key, bool value)
        {
            _foldoutStates[key] = value;
        }

        private void DrawCustomHeader(Rect rect, SerializedProperty property, string displayName, float duration)
        {
            // Get the Active property
            var activeProperty = property.FindPropertyRelative("Active");
            bool isActive = activeProperty != null ? activeProperty.boolValue : true;

            // Check WaitForCompletion
            var timingProperty = property.FindPropertyRelative("Timing");
            bool waitForCompletion = true;
            if (timingProperty != null)
            {
                var waitProperty = timingProperty.FindPropertyRelative("WaitForCompletion");
                if (waitProperty != null)
                    waitForCompletion = waitProperty.boolValue;
            }

            // Check if there's a warning
            bool hasWarning = !string.IsNullOrEmpty(GetWarningMessage(property));

            // Background
            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor;
                if (!isActive)
                {
                    // Red tint when inactive
                    bgColor = property.isExpanded
                        ? new Color(0.35f, 0.10f, 0.10f, 1f)
                        : new Color(0.28f, 0.08f, 0.08f, 1f);
                }
                else if (!waitForCompletion)
                {
                    // Green tint when WaitForCompletion is false
                    bgColor = property.isExpanded
                        ? new Color(0.10f, 0.30f, 0.10f, 1f)
                        : new Color(0.08f, 0.22f, 0.08f, 1f);
                }
                else
                {
                    // Default
                    bgColor = property.isExpanded
                        ? new Color(0.25f, 0.25f, 0.25f, 1f)
                        : new Color(0.20f, 0.20f, 0.20f, 1f);
                }

                EditorGUI.DrawRect(rect, bgColor);
            }

            float leftMargin = 5f;
            float checkboxWidth = 20f;
            float spacing = hasWarning ? 0f : 15f;
            float warningIconWidth = hasWarning ? 20f : 0f;
            float warningSpacing = hasWarning ? 20f : 0f; // Extra spacing after warning icon
            float menuButtonWidth = 20f;
            float durationWidth = 55f;

            // Active checkbox on the far left
            if (activeProperty != null)
            {
                Rect checkboxRect = new Rect(rect.x + leftMargin, rect.y + 2f, checkboxWidth, rect.height);

                EditorGUI.BeginChangeCheck();
                bool newActive = EditorGUI.Toggle(checkboxRect, activeProperty.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    activeProperty.boolValue = newActive;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            float currentX = rect.x + leftMargin + checkboxWidth + spacing;

            // Warning icon (if there's a warning)
            if (hasWarning)
            {
                Rect warningIconRect = new Rect(currentX, rect.y, warningIconWidth, rect.height);
                GUIStyle warningStyle = new GUIStyle(GUI.skin.label);
                warningStyle.fontSize = 14;
                warningStyle.alignment = TextAnchor.MiddleCenter;
                warningStyle.normal.textColor = new Color(1f, 0.7f, 0f); // Orange color

                GUI.Label(warningIconRect, "⚠", warningStyle);

                currentX += warningIconWidth + warningSpacing; // Add extra spacing after warning icon
            }

            // Foldout arrow and label in the middle
            float foldoutWidth = rect.width - (currentX - rect.x) - menuButtonWidth - spacing - durationWidth - spacing;
            Rect foldoutRect = new Rect(currentX, rect.y, foldoutWidth, rect.height);

            // Custom style for the label with color based on active state
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = isActive ? Color.white : new Color(0.6f, 0.6f, 0.6f);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, displayName, true, labelStyle);

            // Duration 
            float durationX = rect.x + rect.width - durationWidth - menuButtonWidth - spacing;
            Rect durationRect = new Rect(durationX, rect.y, durationWidth, rect.height);
            GUIStyle rightAlignedStyle = new GUIStyle(GUI.skin.label);
            rightAlignedStyle.alignment = TextAnchor.MiddleRight;
            rightAlignedStyle.normal.textColor = isActive ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.5f, 0.5f, 0.5f);

            GUI.Label(durationRect, $"[{duration:0.0}s]", rightAlignedStyle);

            // Menu button (3 dots) on the far right
            Rect menuButtonRect = new Rect(rect.x + rect.width - menuButtonWidth - 2f, rect.y + 1f, menuButtonWidth, rect.height - 2f);

            GUIStyle menuStyle = new GUIStyle(GUI.skin.button);
            menuStyle.fontSize = 14;
            menuStyle.fontStyle = FontStyle.Bold;
            menuStyle.alignment = TextAnchor.MiddleCenter;
            menuStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            if (GUI.Button(menuButtonRect, "⋮", menuStyle))
            {
                ShowContextMenu(property);
            }
        }

        private void ShowContextMenu(SerializedProperty property)
        {
            GenericMenu menu = new GenericMenu();

            // Remove
            menu.AddItem(new GUIContent("Remove"), false, () => RemoveElement(property));

            // Reset
            menu.AddItem(new GUIContent("Reset"), false, () => ResetElement(property));

            menu.AddSeparator("");

            // Duplicate
            menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateElement(property));

            menu.AddSeparator("");

            // Copy
            menu.AddItem(new GUIContent("Copy"), false, () => CopyElement(property));

            // Paste
            if (_clipboard != null && _clipboard.GetType() == property.managedReferenceValue.GetType())
            {
                menu.AddItem(new GUIContent("Paste"), false, () => PasteElement(property));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.ShowAsContext();
        }

        private void RemoveElement(SerializedProperty property)
        {
            // Get the parent array
            string path = property.propertyPath;
            string arrayPath = path.Substring(0, path.LastIndexOf('.'));
            SerializedProperty arrayProperty = property.serializedObject.FindProperty(arrayPath);

            if (arrayProperty != null && arrayProperty.isArray)
            {
                // Find the index
                string indexStr = path.Substring(path.LastIndexOf('[') + 1);
                indexStr = indexStr.Substring(0, indexStr.Length - 1);
                int index = int.Parse(indexStr);

                // Remove the element
                arrayProperty.DeleteArrayElementAtIndex(index);
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void ResetElement(SerializedProperty property)
        {
            if (property.managedReferenceValue != null)
            {
                var type = property.managedReferenceValue.GetType();

                // Create a new instance with default values
                var newInstance = Activator.CreateInstance(type);

                property.managedReferenceValue = newInstance;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DuplicateElement(SerializedProperty property)
        {
            // Get the parent array
            string path = property.propertyPath;
            string arrayPath = path.Substring(0, path.LastIndexOf('.'));
            SerializedProperty arrayProperty = property.serializedObject.FindProperty(arrayPath);

            if (arrayProperty != null && arrayProperty.isArray)
            {
                // Find the index
                string indexStr = path.Substring(path.LastIndexOf('[') + 1);
                indexStr = indexStr.Substring(0, indexStr.Length - 1);
                int index = int.Parse(indexStr);

                // Create a deep copy with the correct type
                var original = property.managedReferenceValue as ActionFlow;
                if (original != null)
                {
                    var duplicate = DeepCopyActionFlow(original);

                    // Insert the duplicate right after the original
                    arrayProperty.InsertArrayElementAtIndex(index + 1);
                    SerializedProperty newElement = arrayProperty.GetArrayElementAtIndex(index + 1);
                    newElement.managedReferenceValue = duplicate;

                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void CopyElement(SerializedProperty property)
        {
            if (property.managedReferenceValue is ActionFlow actionFlow)
            {
                _clipboard = DeepCopyActionFlow(actionFlow);
                Debug.Log($"Copied: {actionFlow.GetType().Name}");
            }
        }

        private void PasteElement(SerializedProperty property)
        {
            if (_clipboard != null && property.managedReferenceValue is ActionFlow currentFlow)
            {
                if (_clipboard.GetType() == currentFlow.GetType())
                {
                    var pasted = DeepCopyActionFlow(_clipboard);
                    property.managedReferenceValue = pasted;
                    property.serializedObject.ApplyModifiedProperties();
                    Debug.Log($"Pasted: {_clipboard.GetType().Name}");
                }
                else
                {
                    Debug.LogWarning("Cannot paste - type mismatch!");
                }
            }
        }

        private ActionFlow DeepCopyActionFlow(ActionFlow source)
        {
            if (source == null) return null;

            var type = source.GetType();

            // Serialize to JSON and deserialize to create a deep copy
            string json = JsonUtility.ToJson(source);
            var copy = Activator.CreateInstance(type) as ActionFlow;
            JsonUtility.FromJsonOverwrite(json, copy);

            return copy;
        }

// Remove the old generic DeepCopy method or keep it for other uses
        private T DeepCopy<T>(T source)
        {
            if (source == null) return default(T);

            // Serialize to JSON and back for deep copy
            string json = JsonUtility.ToJson(source);
            return JsonUtility.FromJson<T>(json);
        }

        private string NicifyTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return "";

            string result = "";
            for (int i = 0; i < typeName.Length; i++)
            {
                if (i > 0 && char.IsUpper(typeName[i]) && !char.IsUpper(typeName[i - 1]))
                {
                    result += " ";
                }

                result += typeName[i];
            }

            return result;
        }

        private void DrawTypeSelector(Rect position, SerializedProperty property)
        {
            if (GUI.Button(position, "Add ActionFlow"))
            {
                var menu = new GenericMenu();
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => !t.IsAbstract && typeof(ActionFlow).IsAssignableFrom(t))
                    .OrderBy(t => t.Name);

                foreach (var type in types)
                {
                    string niceName = NicifyTypeName(type.Name.Replace("ActionFlow", ""));

                    menu.AddItem(new GUIContent(niceName), false, () =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.ShowAsContext();
            }
        }

        private string GetWarningMessage(SerializedProperty property)
        {
            if (property.managedReferenceValue == null)
                return null;

            var actionFlow = property.managedReferenceValue as ActionFlow;
            if (actionFlow == null)
                return null;

            return actionFlow.GetWarningMessage();
        }

        private float GetWarningHeight(string message, float width)
        {
            GUIStyle helpBoxStyle = GUI.skin.GetStyle("HelpBox");
            GUIContent content = new GUIContent(message);
            return helpBoxStyle.CalcHeight(content, width);
        }

        private void DrawAnimatorStateDropdown(Rect rect,
            SerializedProperty stringProp,
            SerializedProperty rootProperty,
            string animatorFieldName)
        {
            SerializedProperty animatorProp = rootProperty.FindPropertyRelative(animatorFieldName);
            Animator animator = animatorProp?.objectReferenceValue as Animator;

            string label = stringProp.displayName;

            if (animator == null)
            {
                EditorGUI.PropertyField(rect, stringProp);
                return;
            }

            var controller = animator.runtimeAnimatorController
                as UnityEditor.Animations.AnimatorController;

            if (controller == null)
            {
                EditorGUI.PropertyField(rect, stringProp);
                return;
            }

            int layerIndex = 0;
            var layerIndexProp = rootProperty.FindPropertyRelative("LayerIndex");
            if (layerIndexProp != null)
                layerIndex = Mathf.Max(0, layerIndexProp.intValue);

            if (layerIndex >= controller.layers.Length)
                layerIndex = 0;

            var states = controller.layers[layerIndex].stateMachine.states;
            string[] stateNames = states.Select(s => s.state.name).ToArray();
            if (stateNames.Length == 0) return;

            int currentIndex = Array.IndexOf(stateNames, stringProp.stringValue);

            if (currentIndex < 0)
            {
                stringProp.stringValue = stateNames[0];
                stringProp.serializedObject.ApplyModifiedProperties();
                currentIndex = 0;
            }

            Rect labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
            Rect dropdownRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                rect.width - EditorGUIUtility.labelWidth, rect.height);

            EditorGUI.LabelField(labelRect, label);

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(dropdownRect, currentIndex, stateNames);
            if (EditorGUI.EndChangeCheck())
            {
                stringProp.stringValue = stateNames[newIndex];
                stringProp.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}