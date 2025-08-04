using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace Dialogue.Components
{
    public class DialogueActorDOTweenEffect : DialogueActorEffect, ISerializationCallbackReceiver
    {
        private float _duration;

        public override float Duration => _duration;

        private float _time;
        
        public override float Time
        {
            get => _time;
            set
            {
                _time = value;

                foreach (var tweenAnimation in _tweenAnimations)
                {
                    if (tweenAnimation.loops < 0)
                    {
                        continue;
                    }

                    tweenAnimation.tween.Goto(Mathf.Max(0, _time - tweenAnimation.delay));
                }
            }
        }

        public override bool IsLoop => _isLoop;

        private List<DOTweenAnimation> _tweenAnimations;
        private bool _isLoop;

        private void Awake()
        {
            _tweenAnimations = ListPool<DOTweenAnimation>.Get();
            GetComponentsInChildren(_tweenAnimations);

            _isLoop = _tweenAnimations.Any(tweenAnimation => tweenAnimation.loops == -1);
        }

        private void OnDestroy()
        {
            ListPool<DOTweenAnimation>.Release(_tweenAnimations);
        }

        private void OnEnable()
        {
            _duration = 0.0f;

            foreach (var tweenAnimation in _tweenAnimations)
            {
                if (0 <= tweenAnimation.loops)
                {
                    _duration = Mathf.Max(_duration, tweenAnimation.delay + tweenAnimation.duration * tweenAnimation.loops);
                }
                else
                {
                    tweenAnimation.tween.Play();
                }
            }
        }

        private void OnDisable()
        {
            foreach (var tweenAnimation in _tweenAnimations)
            {
                tweenAnimation.tween?.Rewind();
            }
        }

        public void OnBeforeSerialize()
        {
            var tweenAnimations = GetComponentsInChildren<DOTweenAnimation>();

            foreach (var tweenAnimation in tweenAnimations)
            {
                tweenAnimation.autoGenerate = true;
                tweenAnimation.autoPlay = false;
                tweenAnimation.autoKill = false;
                tweenAnimation.isIndependentUpdate = true;
            }

        }

        public void OnAfterDeserialize()
        {
        }
    }
}