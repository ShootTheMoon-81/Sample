using Sirenix.OdinInspector;
using Spine.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Utils;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace Timeline.Tracks.SkeletonMecanimTrack
{
    public class SkeletonMecanimClip : PlayableAsset, ITimelineClipAsset
    {
        public float MixInDuration { get; set; }
        
        [SerializeField]
        private string _state;
        
        [ShowInInspector]
        public string State
        {
            get => _state;
            set => _state = value;
        }
        
        public ClipCaps clipCaps => ClipCaps.Extrapolation | ClipCaps.Blending | ClipCaps.SpeedMultiplier | ClipCaps.ClipIn;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SkeletonMecanimBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            
            behaviour.MixInDuration = MixInDuration;

            behaviour.State = State;
            
            return playable;
        }
    }
}