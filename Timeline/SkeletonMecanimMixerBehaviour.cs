using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;

namespace Timeline.Tracks.SkeletonMecanimTrack
{
    public class SkeletonMecanimMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var skeletonMecanim = playerData as SkeletonMecanim;

            if (skeletonMecanim?.Translator?.Animator == null)
            {
                return;
            }

            var animator = skeletonMecanim.Translator.Animator;
            
            animator.Rebind();
            
            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; ++i)
            {
                float inputWeight = playable.GetInputWeight(i);

                if (inputWeight <= 0.0f)
                {
                    continue;
                }
                
                Play(playable, i, animator);
                
                if (inputWeight < 1.0f && i < inputCount - 1)
                {
                    animator.Update(0);

                    Mix(playable, i + 1, animator);
                }

                break;
            }
            
            animator.Update(info.deltaTime);

            skeletonMecanim.Translator.Apply(skeletonMecanim.Skeleton);
            skeletonMecanim.skeleton.UpdateWorldTransform();
        }

        private void Play(Playable playable, int index, Animator animator)
        {
            var inputPlayable = (ScriptPlayable<SkeletonMecanimBehaviour>)playable.GetInput(index);
            var input = inputPlayable.GetBehaviour();
                
            float time = (float)inputPlayable.GetTime();

            animator.PlayInFixedTime(input.State, 0, time);
        }

        private void Mix(Playable playable, int index, Animator animator)
        {
            float inputWeight = playable.GetInputWeight(index);
            var inputPlayable = (ScriptPlayable<SkeletonMecanimBehaviour>)playable.GetInput(index);
            var input = inputPlayable.GetBehaviour();
                
            float time = (float)inputPlayable.GetTime();
            
            animator.CrossFadeInFixedTime(input.State,
                input.MixInDuration, 0,
                time,
                inputWeight);
        }
    }
}