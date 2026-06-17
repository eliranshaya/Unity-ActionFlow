using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace Core
{
    [Serializable]
    public class ActionFlowVideoPlayer : ActionFlow
    {
        #region Variables

        public enum VideoPlayerAnimationMode
        {
            Play,
            Pause,
            Stop,
            Seek,
            SetVolume,
            LerpVolume,
            SetPlaybackSpeed,
            LerpPlaybackSpeed,
        }
        [InspectorGroup("Duration", 12, false)]
        [Condition("AnimationMode", new[] { "LerpVolume", "LerpPlaybackSpeed" })]
        public DurationMode DurationMode;

        [InspectorGroup("Video Player", 12, false)]
        [Tooltip("The VideoPlayer component to control")]
        public VideoPlayer TargetVideoPlayer;

        [Tooltip("The animation mode to apply")]
        public VideoPlayerAnimationMode AnimationMode = VideoPlayerAnimationMode.Play;

        [Condition("AnimationMode", "Seek")]
        [Tooltip("The time in seconds to jump to")]
        public double SeekTime = 0f;

        [Condition("AnimationMode", "SetVolume")]
        [Tooltip("The track index to set volume on")]
        public ushort VolumeTrack = 0;

        [Condition("AnimationMode", "SetVolume")]
        [Tooltip("The volume value to set immediately (0 = silent, 1 = full)")]
        [Range(0f, 1f)] public float VolumeValue = 1f;

        [Condition("AnimationMode", "LerpVolume")]
        [Tooltip("The track index to lerp volume on")]
        public ushort LerpVolumeTrack = 0;

        [Condition("AnimationMode", "LerpVolume")]
        [Tooltip("The volume to start from")]
        [Range(0f, 1f)] public float VolumeFrom = 0f;

        [Condition("AnimationMode", "LerpVolume")]
        [Tooltip("The volume to end at")]
        [Range(0f, 1f)] public float VolumeTo = 1f;

        [Condition("AnimationMode", "LerpVolume")]
        [Tooltip("Easing curve for the volume animation")]
        public AnimationCurve VolumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Condition("AnimationMode", "SetPlaybackSpeed")]
        [Tooltip("The playback speed to set immediately (1 = normal, 2 = double speed, 0.5 = half speed)")]
        public float PlaybackSpeedValue = 1f;

        [Condition("AnimationMode", "LerpPlaybackSpeed")]
        [Tooltip("The playback speed to start from")]
        public float PlaybackSpeedFrom = 0f;

        [Condition("AnimationMode", "LerpPlaybackSpeed")]
        [Tooltip("The playback speed to end at")]
        public float PlaybackSpeedTo = 1f;

        [Condition("AnimationMode", "LerpPlaybackSpeed")]
        [Tooltip("Easing curve for the playback speed animation")]
        public AnimationCurve PlaybackSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        #endregion

        public override float GetDuration() => Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Control a VideoPlayer component — play, pause, stop, seek, volume, and playback speed";

        public override string GetCategory() => "Video";

        public override string GetWarningMessage()
        {
            if (TargetVideoPlayer == null)
                return "⚠ Target VideoPlayer is not assigned.";

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (TargetVideoPlayer == null) yield break;

            switch (AnimationMode)
            {
                case VideoPlayerAnimationMode.Play: yield return DoPlay(); break;
                case VideoPlayerAnimationMode.Pause: yield return DoPause(); break;
                case VideoPlayerAnimationMode.Stop: yield return DoStop(); break;
                case VideoPlayerAnimationMode.Seek: yield return DoSeek(); break;
                case VideoPlayerAnimationMode.SetVolume: yield return DoSetVolume(); break;
                case VideoPlayerAnimationMode.LerpVolume: yield return DoLerpVolume(); break;
                case VideoPlayerAnimationMode.SetPlaybackSpeed: yield return DoSetPlaybackSpeed(); break;
                case VideoPlayerAnimationMode.LerpPlaybackSpeed: yield return DoLerpPlaybackSpeed(); break;
            }
        }

        private IEnumerator DoPlay()
        {
            TargetVideoPlayer.Play();
            yield break;
        }

        private IEnumerator DoPause()
        {
            TargetVideoPlayer.Pause();
            yield break;
        }

        private IEnumerator DoStop()
        {
            TargetVideoPlayer.Stop();
            yield break;
        }

        private IEnumerator DoSeek()
        {
            TargetVideoPlayer.time = SeekTime;
            yield break;
        }

        private IEnumerator DoSetVolume()
        {
            TargetVideoPlayer.SetDirectAudioVolume(VolumeTrack, VolumeValue);
            yield break;
        }

        private IEnumerator DoLerpVolume()
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;

            TargetVideoPlayer.SetDirectAudioVolume(LerpVolumeTrack, VolumeFrom);

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = VolumeCurve.Evaluate(t);
                TargetVideoPlayer.SetDirectAudioVolume(LerpVolumeTrack, Mathf.LerpUnclamped(VolumeFrom, VolumeTo, curvedT));
                elapsed += DeltaTime();
                yield return YieldInstruction;
            }

            TargetVideoPlayer.SetDirectAudioVolume(LerpVolumeTrack, VolumeTo);
        }

        private IEnumerator DoSetPlaybackSpeed()
        {
            TargetVideoPlayer.playbackSpeed = PlaybackSpeedValue;
            yield break;
        }

        private IEnumerator DoLerpPlaybackSpeed()
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;

            TargetVideoPlayer.playbackSpeed = PlaybackSpeedFrom;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = PlaybackSpeedCurve.Evaluate(t);
                TargetVideoPlayer.playbackSpeed = Mathf.LerpUnclamped(PlaybackSpeedFrom, PlaybackSpeedTo, curvedT);
                elapsed += DeltaTime();
                yield return YieldInstruction;
            }

            TargetVideoPlayer.playbackSpeed = PlaybackSpeedTo;
        }
    }
}