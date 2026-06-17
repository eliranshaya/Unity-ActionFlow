#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Core
{
    [CustomEditor(typeof(ActionFlowComponent))]
    public class ActionFlowComponentEditor : Editor
    {
        private ActionFlowComponent _component;
        private SerializedProperty _actionFlowSettingsProperty;
        private SerializedProperty _actionFlowsProperty;
        private SerializedProperty _resetBeforeExecutionProperty;
        private SerializedProperty _resetTargetsProperty;

        private int _draggingIndex = -1;
        private int _dragTargetIndex = -1;
        private Rect[] _elementRects; // Track each element's rect for accurate drag detection

        private void OnEnable()
        {
            _component = (ActionFlowComponent)target;
            _actionFlowSettingsProperty = serializedObject.FindProperty("_actionFlowSettings");
            _actionFlowsProperty = serializedObject.FindProperty("ActionFlows");
            _resetBeforeExecutionProperty = serializedObject.FindProperty("_resetBeforeExecution");
            _resetTargetsProperty = serializedObject.FindProperty("_resetTargets");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw ActionFlow Settings
            EditorGUILayout.PropertyField(_actionFlowSettingsProperty, true);

            EditorGUILayout.Space(5);
            DrawTotalDurationField();

            EditorGUILayout.Space(5);
            DrawResetSettings();

            EditorGUILayout.Space(10);

            // Draw separator line
            DrawSeparatorLine();

            EditorGUILayout.Space(10);

            // ===== ActionFlows Header (Always visible, no foldout) =====
            DrawActionFlowsHeader();

            EditorGUILayout.Space(5);

            // Draw ActionFlows array (always visible)
            DrawActionFlowsArray();

            EditorGUILayout.Space(5);

            // Draw the centered Add button at the bottom
            DrawAddButton();

            EditorGUILayout.Space(10);

            // Draw separator line
            DrawSeparatorLine();

            EditorGUILayout.Space(5);

            // Play/Stop buttons
            DrawControls();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTotalDurationField()
        {
            var calculateProp = serializedObject.FindProperty("_calculateTotalDuration");
            if (calculateProp == null) return;

            EditorGUILayout.PropertyField(calculateProp, new GUIContent("Calculate Total Duration"));

            if (calculateProp.boolValue)
            {
                float totalDuration = CalculateTotalDuration();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.FloatField("Total Duration", totalDuration);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawActionFlowsHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 12;

            EditorGUILayout.LabelField($"Action Flows ({_actionFlowsProperty.arraySize})", headerStyle);
        }

        private void DrawActionFlowsArray()
        {
            // FIRST PASS: Calculate all element heights and positions WITHOUT drawing drop indicators
            _elementRects = new Rect[_actionFlowsProperty.arraySize];
            float currentY = GUILayoutUtility.GetRect(0, 0).y; // Get starting Y position

            for (int i = 0; i < _actionFlowsProperty.arraySize; i++)
            {
                SerializedProperty element = _actionFlowsProperty.GetArrayElementAtIndex(i);
                float height = EditorGUI.GetPropertyHeight(element, true);

                // Store the rect (we'll adjust Y positions in a moment)
                _elementRects[i] = new Rect(0, currentY, 0, height);
                currentY += height;
            }

            // SECOND PASS: Actually draw everything
            for (int i = 0; i < _actionFlowsProperty.arraySize; i++)
            {
                SerializedProperty element = _actionFlowsProperty.GetArrayElementAtIndex(i);

                // Draw drop indicator line BEFORE this element if needed
                if (_draggingIndex >= 0 && _dragTargetIndex == i)
                {
                    DrawDropIndicator(GUILayoutUtility.GetRect(0, 3));
                }

                // Get the rect for the entire property
                Rect propertyRect = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(element, true));

                // Update the stored rect with the actual drawn position
                _elementRects[i] = propertyRect;

                // Calculate hamburger icon rect (left side, same height as first line)
                Rect handleRect = new Rect(propertyRect.x, propertyRect.y, 20, EditorGUIUtility.singleLineHeight);

                // Draw drag handle
                DrawDragHandle(handleRect, i);
                HandleDragEvents(handleRect, i);

                // Calculate the rect for the property field (shifted right to make room for hamburger)
                Rect propertyFieldRect = new Rect(propertyRect.x + 25, // 20px for hamburger + 5px space
                    propertyRect.y,
                    propertyRect.width - 25,
                    propertyRect.height);

                // Draw the property field in the shifted rect
                EditorGUI.PropertyField(propertyFieldRect, element, true);
            }

            // Draw final drop indicator AFTER all elements if needed
            if (_draggingIndex >= 0 && _dragTargetIndex == _actionFlowsProperty.arraySize)
            {
                DrawDropIndicator(GUILayoutUtility.GetRect(0, 3));
            }

            // If no elements, show a message
            if (_actionFlowsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Action Flows added yet. Click the button below to add one.", MessageType.Info);
            }
        }

        private void DrawDropIndicator(Rect rect)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), new Color(0.3f, 0.6f, 1f, 0.8f));
        }

        private void DrawDragHandle(Rect rect, int index)
        {
            // Change cursor to show it's draggable
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);

            // Highlight if this is being dragged
            if (_draggingIndex == index)
            {
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.6f, 1f, 0.3f));
            }

            // Draw three horizontal lines (hamburger icon)
            Color lineColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            float lineWidth = 12f;
            float lineHeight = 2f;
            float spacing = 3f;

            float centerX = rect.x + rect.width / 2f;
            float centerY = rect.y + rect.height / 2f;

            // Top line
            Rect topLine = new Rect(centerX - lineWidth / 2f, centerY - spacing - lineHeight, lineWidth, lineHeight);
            EditorGUI.DrawRect(topLine, lineColor);

            // Middle line
            Rect middleLine = new Rect(centerX - lineWidth / 2f, centerY - lineHeight / 2f, lineWidth, lineHeight);
            EditorGUI.DrawRect(middleLine, lineColor);

            // Bottom line
            Rect bottomLine = new Rect(centerX - lineWidth / 2f, centerY + spacing, lineWidth, lineHeight);
            EditorGUI.DrawRect(bottomLine, lineColor);
        }

        private void HandleDragEvents(Rect handleRect, int index)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (handleRect.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        _draggingIndex = index;
                        evt.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (_draggingIndex == index)
                    {
                        // Calculate which position we're hovering over
                        _dragTargetIndex = CalculateDragTargetIndex(evt.mousePosition);
                        Repaint();
                        evt.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (_draggingIndex == index && evt.button == 0)
                    {
                        // Only perform move if we have a valid target and it's actually different
                        if (_dragTargetIndex >= 0)
                        {
                            // Determine the actual final position after the move
                            int finalPosition = _dragTargetIndex;
                            if (_dragTargetIndex > _draggingIndex)
                            {
                                finalPosition--; // Adjust because we're removing the element first
                            }

                            // Only move if the final position is different
                            if (finalPosition != _draggingIndex)
                            {
                                MoveArrayElement(_draggingIndex, _dragTargetIndex);
                            }
                        }

                        _draggingIndex = -1;
                        _dragTargetIndex = -1;
                        Repaint();
                        evt.Use();
                    }

                    break;

                case EventType.Repaint:
                    if (_draggingIndex >= 0)
                    {
                        Repaint();
                    }

                    break;
            }
        }

        private int CalculateDragTargetIndex(Vector2 mousePosition)
        {
            if (_elementRects == null || _elementRects.Length == 0)
            {
                return -1;
            }

            // Find which element or gap the mouse is currently over
            for (int i = 0; i < _elementRects.Length; i++)
            {
                Rect rect = _elementRects[i];

                // Calculate the midpoint between this element and the previous one
                float topBoundary;
                if (i == 0)
                {
                    topBoundary = rect.y - 1000f; // Very top
                }
                else
                {
                    topBoundary = (_elementRects[i - 1].yMax + rect.y) / 2f;
                }

                // Calculate the midpoint between this element and the next one
                float bottomBoundary;
                if (i == _elementRects.Length - 1)
                {
                    bottomBoundary = rect.yMax + 1000f; // Very bottom
                }
                else
                {
                    bottomBoundary = (rect.yMax + _elementRects[i + 1].y) / 2f;
                }

                // If mouse is in the top half of this element's zone, insert before it
                if (mousePosition.y >= topBoundary && mousePosition.y < rect.y + (rect.height / 2f))
                {
                    return i;
                }

                // If mouse is in the bottom half of this element's zone, insert after it
                if (mousePosition.y >= rect.y + (rect.height / 2f) && mousePosition.y <= bottomBoundary)
                {
                    return i + 1;
                }
            }

            // Fallback
            return _elementRects.Length;
        }

        private void MoveArrayElement(int fromIndex, int toIndex)
        {
            serializedObject.Update();

            // Adjust target index if moving down
            int targetIndex = toIndex;
            if (fromIndex < toIndex)
            {
                targetIndex--;
            }

            // Store the element we're moving
            var movingElement = _actionFlowsProperty.GetArrayElementAtIndex(fromIndex).managedReferenceValue;

            // Remove from old position
            _actionFlowsProperty.DeleteArrayElementAtIndex(fromIndex);

            // Insert at new position
            _actionFlowsProperty.InsertArrayElementAtIndex(targetIndex);
            _actionFlowsProperty.GetArrayElementAtIndex(targetIndex).managedReferenceValue = movingElement;

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAddButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Centered Add button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 11;
            buttonStyle.fontStyle = FontStyle.Bold;

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);

            if (GUILayout.Button(new GUIContent("+ Add Action Flow", "Add new ActionFlow to the sequence"),
                    buttonStyle, GUILayout.Width(150), GUILayout.Height(25)))
            {
                ShowAddActionFlowMenu();
            }

            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddActionFlowMenu()
        {
            GenericMenu menu = new GenericMenu();

            var actionFlowTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && typeof(ActionFlow).IsAssignableFrom(type))
                .OrderBy(type => type.Name)
                .ToList();

            var categorizedTypes = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Type>>();

            foreach (var type in actionFlowTypes)
            {
                var instance = Activator.CreateInstance(type) as ActionFlow;
                string category = instance != null ? instance.GetCategory() : "Other";

                if (!categorizedTypes.ContainsKey(category))
                {
                    categorizedTypes[category] = new System.Collections.Generic.List<Type>();
                }

                categorizedTypes[category].Add(type);
            }

            foreach (var category in categorizedTypes.OrderBy(kvp => kvp.Key))
            {
                foreach (var type in category.Value)
                {
                    string typeName = type.Name.Replace("ActionFlow", "");
                    string niceName = NicifyTypeName(typeName);
                    string menuPath = $"{category.Key}/{niceName}";

                    menu.AddItem(new GUIContent(menuPath), false, () => AddActionFlow(type));
                }
            }

            menu.ShowAsContext();
        }

        private void AddActionFlow(Type type)
        {
            // Add new element to array
            int newIndex = _actionFlowsProperty.arraySize;
            _actionFlowsProperty.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newElement = _actionFlowsProperty.GetArrayElementAtIndex(newIndex);

            // Create new instance of the type
            var newInstance = Activator.CreateInstance(type);
            newElement.managedReferenceValue = newInstance;

            serializedObject.ApplyModifiedProperties();

            Debug.Log($"<color=cyan>✓ Added {NicifyTypeName(type.Name.Replace("ActionFlow", ""))} to ActionFlows</color>");
        }

        private void DrawControls()
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                if (GUILayout.Button("▶ Play", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    if (Application.isPlaying)
                    {
                        _component.StartExecution();
                    }
                }

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("■ Stop", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    if (Application.isPlaying)
                    {
                        _component.StopExecution();
                    }
                }

                GUI.backgroundColor = new Color(0.5f, 0.7f, 1f);
                if (GUILayout.Button("↺ Reset", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    if (Application.isPlaying)
                    {
                        _component.ResetAnimationTargets();
                    }
                }

                GUI.backgroundColor = Color.white;

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                EditorGUILayout.HelpBox("You can preview ActionFlows while the game is running.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to preview ActionFlows.",
                    MessageType.Warning);
            }
        }

        private void DrawResetSettings()
        {
            if (_resetBeforeExecutionProperty == null || _resetTargetsProperty == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(_resetBeforeExecutionProperty, new GUIContent("Reset Before Execution"));
            EditorGUILayout.PropertyField(_resetTargetsProperty, new GUIContent("Reset Targets"), true);
        }

        private void DrawSeparatorLine()
        {
            Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 1));
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

        private float CalculateTotalDuration()
        {
            if (_component.ActionFlows == null || _component.ActionFlows.Length == 0)
            {
                return 0f;
            }

            float total = 0f;
            foreach (var flow in _component.ActionFlows)
            {
                if (flow != null)
                {
                    total += flow.GetDuration();

                    if (flow.Timing != null)
                    {
                        total += flow.Timing.InitialDelay;
                    }
                }
            }

            return total;
        }
    }
}
#endif
