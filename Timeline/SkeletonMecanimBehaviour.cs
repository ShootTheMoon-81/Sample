using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Timeline.Tracks.SkeletonMecanimTrack
{
    public class SkeletonMecanimBehaviour : PlayableBehaviour
    {
        public float MixInDuration { get; set; }
        
        public string State { get; set; }
        
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
        }
    }
}