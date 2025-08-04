using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Timeline.Tracks.SkeletonMecanimTrack
{
    [TrackColor(1, 0, 0)]
    [TrackClipType(typeof(SkeletonMecanimClip))]
    [TrackBindingType(typeof(SkeletonMecanim))]
    public class SkeletonMecanimTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                var animatorStateClip = clip.asset as SkeletonMecanimClip;
                if (animatorStateClip != null)
                {
                    clip.displayName = animatorStateClip.State;
                    animatorStateClip.MixInDuration = (float)clip.mixInDuration;
                }
            }
            
            return ScriptPlayable<SkeletonMecanimMixerBehaviour>.Create(graph, inputCount);
        }
    }
}