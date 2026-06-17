#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;

namespace Core
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class AnimatorStateAttribute : PropertyAttribute
    {
        public string AnimatorFieldName;

        // [AnimatorState("TargetAnimator")]
        public AnimatorStateAttribute(string animatorFieldName)
        {
            this.AnimatorFieldName = animatorFieldName;
        }
    }
    /// <summary>
    /// An attribute used to group inspector fields under common dropdowns
    /// Implementation inspired by Rodrigo Prinheiro's work, available at https://github.com/RodrigoPrinheiro/unityFoldoutAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class InspectorGroupAttribute : PropertyAttribute
    {
        public string GroupName;
        public int GroupColorIndex;
        public bool ClosedByDefault;

        public InspectorGroupAttribute(string groupName, int groupColorIndex = 24, bool closedByDefault = false)
        {
            if (groupColorIndex > 139)
            {
                groupColorIndex = 139;
            }

            this.GroupName = groupName;
            this.GroupColorIndex = groupColorIndex;
            this.ClosedByDefault = closedByDefault;
        }
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class ConditionAttribute : PropertyAttribute
    {
        public string BoolField = "";
        public bool BoolExpected = true;

        public string EnumField = "";
        public string EnumValue = "";
        public string[] EnumValues = null;

        // --- Bool only ---
        // [Condition("IsEnabled")]
        // [Condition("IsEnabled", false)] = show when IsEnabled is false
        public ConditionAttribute(string boolField, bool boolExpected = true)
        {
            this.BoolField = boolField;
            this.BoolExpected = boolExpected;
        }

        // --- Enum only, single value ---
        // [Condition("AnimationMode", "Typewriter")]
        public ConditionAttribute(string enumField, string enumValue)
        {
            this.EnumField = enumField;
            this.EnumValue = enumValue;
        }

        // --- Enum only, multiple values ---
        // [Condition("AnimationMode", new[] { "Typewriter", "TypewriterWithPunch" })]
        public ConditionAttribute(string enumField, string[] enumValues)
        {
            this.EnumField = enumField;
            this.EnumValues = enumValues;
        }

        // --- Bool AND enum single value ---
        // [Condition("ReplaceText", "AnimationMode", "Typewriter")]
        public ConditionAttribute(string boolField, string enumField, string enumValue)
        {
            this.BoolField = boolField;
            this.BoolExpected = true;
            this.EnumField = enumField;
            this.EnumValue = enumValue;
        }

        // --- Bool AND enum multiple values ---
        // [Condition("ReplaceText", "AnimationMode", new[] { "Typewriter", "TypewriterWithPunch" })]
        public ConditionAttribute(string boolField, string enumField, string[] enumValues)
        {
            this.BoolField = boolField;
            this.BoolExpected = true;
            this.EnumField = enumField;
            this.EnumValues = enumValues;
        }
    }
}