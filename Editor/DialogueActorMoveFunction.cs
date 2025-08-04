using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dialogue.Components;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Dialogue
{
    [DialogueFunction("Move")]
    public class DialogueActorMoveFunction : DialogueFunction<DialogueActor>
    {
        private float _duration;
        private string _curveKey;
        private Vector2 _to;
        private DialogueActorMoveCurveData.MoveCurve? _moveCurve;

        private string _targetId;

        private bool _moveX = true;
        private bool _moveY = true;

        protected Sequence _moveSequence;
        protected Tween _xTween;
        protected Tween _yTween;

        private const string CurveDataPath = "Assets/Data/Content/Dialogue/ScriptableObject/MoveCurve.asset";
        
        public override float Duration => _duration;
        
        public override void Init(FunctionParameters parameters)
        {
            Vector2 to;
            FunctionParameters remainingParam;

            switch (parameters[1].ParseString())
            {
                case "L" or "l":
                {
                    to = new Vector2(-500f, 0f);
                    remainingParam = parameters[2..];
                }
                break;

                case "M" or "m":
                {
                    to = new Vector2(0f, 0f);
                    remainingParam = parameters[2..];
                }
                break;
                
                case "R" or "r":
                {
                    to = new Vector2(500f, 0f);
                    remainingParam = parameters[2..];
                }
                break;

                default:
                {
                    to = new Vector2(parameters[1], parameters[2]);
                    remainingParam = parameters[3..];
                }
                break;
            };
            
            var duration = remainingParam.GetValueOrDefault(0, 0.0f);
            var curveKey = remainingParam.GetValueOrDefault(1, "Linear");
            
            Init(null, to, duration, curveKey, true, true);
        }

        public void Init(string targetId, string to, float duration, string curveKey)
        {
            Init(
                targetId,
                to switch
                {
                    "L" or "l" => new Vector2(-500f, 0f),
                    "M" or "m" => new Vector2(0f, 0f),
                    "R" or "r" => new Vector2(500f, 0f),
                    _ => Vector2.zero
                },
                duration,
                curveKey,
                moveX: true,
                moveY: true);
        }

        public void Init(string targetId, Vector2 to, float duration, string curveKey, bool moveX, bool moveY)
        {
            _to = to;
            _duration = duration;
            _curveKey = curveKey;

            if (string.Equals(_curveKey, "LPop", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_curveKey, "RPop", StringComparison.OrdinalIgnoreCase))
            {
                _to.y = 200.0f;
            }

            _moveX = moveX;
            _moveY = moveY;
        }

        public override async UniTask Preload(DialogueActor target)
        {
            if (string.IsNullOrEmpty(_curveKey) == false)
            {
                var directionData = await Addressables.LoadAssetAsync<DialogueActorMoveCurveData>(CurveDataPath);

                _moveCurve = directionData[_curveKey];
            }
        }

        public override void Execute(DialogueActor target)
        {
            _targetId ??= target.CurrentActorId;

            var targetActor = target.Get<DialogueActorCharacter>(_targetId);
            
            if (_moveX != true && _moveY != true)
            {
                return;
            }

            _moveSequence = DOTween.Sequence().SetAutoKill(false);
                    
            if (_moveX == true)
            {
                _xTween = targetActor.transform
                    .DOLocalMoveX(_to.x, _duration)
                    .SetEase(_moveCurve.Value.x)
                    .SetUpdate(UpdateType.Manual);
                        
                _moveSequence.Join(_xTween);
            }

            if (_moveY == true)
            {
                _yTween = targetActor.transform
                    .DOLocalMoveY(_to.y, _duration)
                    .SetEase(_moveCurve.Value.y)
                    .SetUpdate(UpdateType.Manual);

                _moveSequence.Join(_yTween);
            }
        }
        
        public override void Update(DialogueActor target, float time)
        {
            if (_moveCurve == null)
            {
                return;
            }
            
            _moveSequence?.Goto(time);
        }
        
        public override void LateExecute(DialogueActor target)
        {
            if (_xTween != null)
            {
                _xTween.Kill();
                _xTween = null;
            }
            
            if (_yTween != null)
            {
                _yTween.Kill();
                _yTween = null;
            }

            if (_moveSequence != null)
            {
                _moveSequence.Kill();
                _moveSequence = null;
            }
        }
    }
}