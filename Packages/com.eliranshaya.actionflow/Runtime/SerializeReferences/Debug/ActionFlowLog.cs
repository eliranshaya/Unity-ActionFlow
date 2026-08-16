using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowLog : ActionFlow
    {
        public enum LogType
        {
            Log,
            Warning,
            Error
        }

        [InspectorGroup("Log Settings", 12, false)]
        [Tooltip("The message to log to the console")]
        [TextArea(3, 10)]
        public string LogMessage = "Log message";

        [Tooltip("The type of log message (Log, Warning, or Error)")]
        public LogType MessageType = LogType.Log;

        [Tooltip("If true, the message will be logged with rich text formatting (colors, bold, etc.)")]
        public bool UseRichText = true;

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription()
        {
            return "Log a message to the Unity Console with configurable log type (Log, Warning, Error)";
        }

        public override string GetCategory()
        {
            return "Debug";
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(LogMessage))
            {
                return UniTask.CompletedTask;
            }

            string message = UseRichText ? LogMessage : LogMessage.Replace("<color=", "").Replace("</color>", "").Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "");

            switch (MessageType)
            {
                case LogType.Log:
                    Debug.Log(message);
                    break;

                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;

                case LogType.Error:
                    Debug.LogError(message);
                    break;
            }

            return UniTask.CompletedTask;
        }
    }
}