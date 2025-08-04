using Cysharp.Threading.Tasks;
using Data;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UI.Thing;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Slot
{
    public class UIDestroyRewardSlot : UISlot<OZRewards>
    {
        [SerializeField]
        private TextMeshProUGUI _number;
        
        [SerializeField]
        private LoopScrollRect _dropItemScrollRect;

        [SerializeField]
        private RectTransform _tweenRoot;

        [SerializeField]
        private CanvasGroup _tweenCanvas;

        [Header("각 트윈들의 지속시간(기본값:0.3)")]
        [SerializeField]
        private float _tweenDuration = 0.3f;

        [Header("슬롯들의 초기 x 위치(기본값:-200)")]
        [SerializeField]
        private float _tweenStartX = -200.0f;

        [Header("연출 종료 시 슬롯의 y 위치(기본값:200)")]
        [SerializeField]
        private float _tweenEndY = 200.0f;

        [SerializeField]
        private DOTweenAnimation[] _doTweenAnimations;

        [SerializeField]
        private DOTweenAnimation[] _firstTween;
        
        [SerializeField]
        private DOTweenAnimation[] _secondTween;
        
        public float TotalTweenDuration { get; private set; }
        public float TotalTweenTime => FirstTweenTime + SecondTweenTime;
        public float FirstTweenTime { get; private set; }
        public float SecondTweenTime { get; private set; }

        public Action<UIDestroyRewardSlot> OnClick { get; set; }
        
        private List<IThingData> _dropItems = new();
    
        #region DOAnimation 컴포넌트는 from 설정이 제대로 되는 꼴을 못봤다.
        private Sequence _firstTweenSequence;
        private Sequence _secondTweenSequence;

        private Action<int> _finishAction;
        #endregion
        
        public override void Awake()
        {
            base.Awake();
            
            //_doTweenAnimations = GetComponentsInChildren<DOTweenAnimation>(true);

            // foreach (var tween in _doTweenAnimations)
            // {
            //     float totalTweenTime = tween.delay + tween.duration;
            //     TotalTweenDuration = TotalTweenDuration < totalTweenTime ? totalTweenTime : TotalTweenDuration;
            // }
            
            // foreach (var first in _firstTween)
            // {
            //     float firstTweenTime = first.delay + first.duration;
            //     FirstTweenTime = FirstTweenTime < firstTweenTime ? firstTweenTime : FirstTweenTime;
            // }
            //
            // foreach (var second in _secondTween)
            // {
            //     float secondTweenTime = second.delay + second.duration;
            //     SecondTweenTime = SecondTweenTime < secondTweenTime ? secondTweenTime : SecondTweenTime;
            // }
        }
        
        protected override void OnValueChanged(OZRewards value)
        {
            if (value == null)
            {
                _dropItemScrollRect.ClearCells();
                return;
            }
            
            // HACK
            _tweenCanvas.alpha = 0.0f;
            _tweenRoot.anchoredPosition = new Vector2(_tweenStartX, _tweenRoot.anchoredPosition.y);

            if (_number != null)
            {
                _number.text = string.Format(LocalString.Get("Str_UI_Destroy_ResultCount"), value.Note);
            }

            _dropItems.Clear();

            foreach (var reward in value.Rewards)
            {
                var rewardThing = reward.Reward;
                    
                if (rewardThing is ICountableReward countableReward &&
                    _dropItems.Find(x => x.Id == countableReward.Id) is ICountableReward containedReward)
                {
                    containedReward.Count += countableReward.Count;
                }
                    
                _dropItems.Add(rewardThing);
            }
            
            _dropItemScrollRect.dataSource = new LoopScrollDataSource<UIThingSlot>(
                OnListSlotInitialize,
                OnListSlotRelease,
                OnListSlotUpdate);
            
            _dropItemScrollRect.totalCount = _dropItems.Count;
            _dropItemScrollRect.SendItemData(_dropItems, null);
            _dropItemScrollRect.RefillCells();
        }
        
        private void OnDisable()
        {
            //ResetTween();
            
            DebugHelper.Log("OnDisable");
            
            // if (_firstTweenSequence != null)
            // {
            //     _firstTweenSequence.Kill();
            //     _firstTweenSequence = null;
            // }
            //
            // if (_secondTweenSequence != null)
            // {
            //     _secondTweenSequence.Kill();
            //     _secondTweenSequence = null;
            // }
            //
            // _finishAction = null;

            _tweenRoot.anchoredPosition = Vector2.zero;
        }

        public void SetSlot(int index, Action<int> finishAction)
        {
            //int.TryParse(Value.Note, out int note);
            
            //Test1(note - 1, finishAction).Forget();
            
            //return;
            
            //DebugHelper.Log($"SetSlot : {index}");
            
            // if (_firstTweenSequence != null)
            // {
            //     return;
            // }
            _finishAction = finishAction;
            
            // for (int i = 0; i < _doTweenAnimations.Length; i++)
            // {
            //     _doTweenAnimations[i].delay = delay;
            //     _doTweenAnimations[i].CreateTween(true, true);
            //     //_doTweenAnimations[i].DOPlay();
            // }

            // foreach (var first in _firstTween)
            // {
            //     first.delay = FirstTweenTime + delay;
            //     first.CreateTween(true, true);
            // }
            // foreach (var second in _secondTween)
            // {
            //     second.delay = FirstTweenTime + SecondTweenTime + delay;
            //     second.CreateTween(true, true);
            // }
            
            // if (_firstTweenSequence != null)
            // {
            //     _firstTweenSequence.Kill();
            //     _firstTweenSequence = null;
            // }
            _firstTweenSequence = DOTween.Sequence().SetAutoKill(false);
            _secondTweenSequence = DOTween.Sequence().SetAutoKill(false);
            
            _tweenCanvas.alpha = 0.0f;
            _tweenRoot.anchoredPosition = new Vector2(_tweenStartX, _tweenRoot.anchoredPosition.y);
            
            // _firstTweenSequence.PrependInterval(_tweenDuration * index);
            // _firstTweenSequence.SetDelay(_tweenDuration * index);

            _firstTweenSequence
                .Join(_tweenCanvas.DOFade(1.0f, _tweenDuration).SetEase(Ease.OutQuad))
                .Join(_tweenRoot.DOAnchorPosX(0.0f, _tweenDuration).SetEase(Ease.OutExpo))
                .PrependInterval((_tweenDuration * 2) * index)
                .OnComplete(() =>
                {
                    //int.TryParse(Value.Note, out int note);

                    //finishAction?.Invoke(note - 1);
                });;
                //.SetDelay(_tweenDuration * index);
            
            // _firstTweenSequence.Join(_tweenRoot
            //     .DOAnchorPosX(0.0f, _tweenDuration)
            //     .SetEase(Ease.OutExpo)
            //     .SetDelay(_tweenDuration * index));

            // _secondTweenSequence
            //     .Join(_tweenRoot.DOAnchorPosY(_tweenEndY, _tweenDuration).SetEase(Ease.OutExpo))
            //     .PrependInterval(_firstTweenSequence.Delay() + _firstTweenSequence.Duration())
            //     .OnComplete(() =>
            //     {
            //         finishAction?.Invoke(index);
            //     });

            // DebugHelper.Log($"y: {_dropItemScrollRect.content.anchoredPosition.y}");
            // DebugHelper.Log($"top: {_dropItemScrollRect.content.gameObject.GetComponent<VerticalLayoutGroup>().padding.top}");
            // DebugHelper.Log($"bottom: {_dropItemScrollRect.content.gameObject.GetComponent<VerticalLayoutGroup>().padding.top}");
            // DebugHelper.Log($"height: {_dropItemScrollRect.ScrollRectSlot.GetComponent<RectTransform>().rect.height}");
            // DebugHelper.Log($"width: {_dropItemScrollRect.ScrollRectSlot.GetComponent<RectTransform>().rect.width}");
            // DebugHelper.Log($"preferredHeight: {_dropItemScrollRect.ScrollRectSlot.GetComponent<LayoutElement>().preferredHeight}");

            //_secondTweenSequence.SetDelay((_tweenDuration * index) + _firstTweenSequence.Delay() + _firstTweenSequence.Duration());
            // _secondTweenSequence.Join(_tweenRoot
            //     .DOAnchorPosY(_tweenEndY, _tweenDuration)
            //     .SetEase(Ease.OutExpo)
            //     .SetDelay((_tweenDuration * index) + _firstTweenSequence.Delay() + _firstTweenSequence.Duration()));

            //_firstTweenSequence.Play();
            //_secondTweenSequence.Play();
            
            // DebugHelper.Log($"first delay: {_firstTweenSequence.Delay()} / first duration: {_firstTweenSequence.Duration()}");
            // DebugHelper.Log($"second delay: {_secondTweenSequence.Delay()} / second duration: {_secondTweenSequence.Duration()}");

            // DebugHelper.Log($"first: {_firstTweenSequence.Delay() + _firstTweenSequence.Duration()} " +
            //                 $"/ second: {_secondTweenSequence.Delay() + _secondTweenSequence.Duration()}" +
            //                 $" / index: {index}");
        }

        public void PlayTween(int index, Action<bool> finishAction)
        {
            DebugHelper.Log("PlayTween");
            
            _firstTweenSequence = DOTween.Sequence().SetAutoKill(false);

            _firstTweenSequence
                .Join(_tweenCanvas.DOFade(1.0f, _tweenDuration).SetEase(Ease.OutQuad))
                .Join(_tweenRoot.DOAnchorPosX(0.0f, _tweenDuration).SetEase(Ease.OutExpo))
                .PrependInterval(_tweenDuration * 0.0f)
                .OnComplete(() => { DebugHelper.Log("EndTween"); finishAction?.Invoke(true); });
        }

        private async UniTask Test1(int index, Action<int> finishAction)
        {
            await UniTask.Delay(1000);
            
            finishAction?.Invoke(index);
        }
        
        private void ResetTween()
        {
            // for (int i = 0; i < _doTweenAnimations.Length; i++)
            // {
            //     _doTweenAnimations[i].delay = 0.0f;
            // }

            // foreach (var first in _firstTween)
            // {
            //     first.delay = 0.0f;
            // }
            //
            // foreach (var second in _secondTween)
            // {
            //     second.delay = 0.0f;
            // }
        }

        public void Test()
        {
            DebugHelper.Log("Test");
        }

        #region 드랍아이템
        private void OnListSlotInitialize(UIThingSlot slot)
        {
            slot.OnClick =
                (x) =>
                {
#if UNITY_EDITOR
                    DebugHelper.Log($"Click Item: {x.Value.Id}");
#endif
                };
            
            slot.AttachModule<UIItemSlotRewardModule>();
        }
        private void OnListSlotRelease(UIThingSlot slot)
        {
            slot.OnClick = null;
            
            slot.DetachModule<UIItemSlotRewardModule>();
        }
        private void OnListSlotUpdate(UIThingSlot slot, int index)
        {
            slot.Value = _dropItems[index];

            slot.GetModule<UIItemSlotRewardModule>().SetDropReward();
        }
        #endregion
    }
}