using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MessageSystem;
using UI.Messages;
using UI.Panel;
using UI.Transitions;
using UnityEngine;
using System;

namespace Managers
{
    public class UIPanelStack : MonoBehaviour
    {
        [SerializeField]
        private Canvas _canvas;
        
        private List<UIPanel.Context> _contextStack = new();

        public bool IsEmpty => _contextStack.Count <= 1;

        public void Push(UIPanel.Context context)
        {
            PushAsync(context).Forget();
        }
        
        public UniTask PushAsync(UIPanel.Context context)
        {
            var prevContext = 0 < _contextStack.Count ? _contextStack[^1] : null;
            return PushAsync(context, prevContext);
        }
        
        public void Push(IEnumerable<UIPanel.Context> contexts)
        {
            PushAsync(contexts).Forget();
        }

        public async UniTask PushAsync(IEnumerable<UIPanel.Context> contexts)
        {
            var prevContext = 0 < _contextStack.Count ? _contextStack[^1] : null;

            _contextStack.AddRange(contexts.SkipLast(1));

            await PushAsync(contexts.Last(), prevContext);
        }

        private async UniTask PushAsync(UIPanel.Context context, UIPanel.Context prevContext)
        {
            prevContext?.Pause();

            context.Transition.Type = Transition.Types.Push;
            context.Transition.OutPanel = prevContext?.Panel;

            if (prevContext?.Panel != null)
            {
                prevContext.Panel.Canvas.sortingOrder = 99;
                prevContext.Panel.Interactable = false;
            }
            
            await context.Transition.OnLoad();

            await context.Transition.Out();

            if (2 <= _contextStack.Count)
            {
                // Context Stack에 이미 같은 Panel이 사용 중 인지 확인
                for (int i = 2; i <= _contextStack.Count; i++)
                {
                    if (_contextStack[^i].PanelType == context.PanelType &&
                        _contextStack[^i].Phase == UIPanel.Context.Phases.Pause)
                    {
                        // 기존 Context Exit (상태 저장 및 Pool 반환)
                        await _contextStack[^i].Exit();
                    }
                }
            }
            
            await context.Load();
            await UniTask.Yield();

            context.Panel.gameObject.SetActive(true);
            context.Panel.RectTransform.SetParent(_canvas.transform);
            context.Panel.RectTransform.localScale = Vector3.one;
            context.Panel.RectTransform.anchoredPosition3D = Vector3.zero;
            context.Panel.RectTransform.sizeDelta = Vector3.zero;
            context.Panel.RectTransform.SetAsLastSibling();
            
            // https://issuetracker.unity3d.com/issues/ui-elements-are-no-longer-rendered-when-deactivating-and-reactivating-the-parent-gameobject
            // Inner Canvas(Nested Canvas) 이슈
            context.Panel.Canvas.overrideSorting = true;
            context.Panel.Canvas.sortingOrder = 100;
            context.Panel.Canvas.enabled = false;
            
            context.Panel.CanvasGroup.alpha = 1f;

            context.Panel.Interactable = false;
            
            _contextStack.Add(context);

            await context.Enter();
            context.Resume();

            context.Panel.Canvas.enabled = true;
            
            context.Transition.Type = Transition.Types.Push;
            context.Transition.OutPanel = prevContext?.Panel;
            context.Transition.InPanel = context.Panel;
            
            MessageService.Instance.Publish(UIPanelChangedEvent.Create(context.Panel, UIPanelChangedEvent.Types.Push));
            
            await context.Transition.In();
            
            context.Panel.Interactable = true;

            if (prevContext != null)
            {
                prevContext.Panel.gameObject.SetActive(false);
            }
        }

        public void Pop()
        {
            PopAsync().Forget();
        }

        public async UniTask PopAsync()
        {
            if (_contextStack.Count <= 1)
            {
                Debug.LogError("Context Stack is Empty.");
                return;
            }

            var prevContext = _contextStack[^1];
            _contextStack.RemoveAt(_contextStack.Count - 1);
            
            // Reverse Transition
            prevContext.Transition.Type = Transition.Types.Pop;
            prevContext.Transition.OutPanel = prevContext.Panel;
            prevContext.Panel.Interactable = false;
            
            prevContext.Pause();

            await prevContext.Transition.Out();

            var context = _contextStack[^1];

            if (context.Phase is UIPanel.Context.Phases.Exit or UIPanel.Context.Phases.None)
            {
                await context.Load();
            }

            context.Panel.gameObject.SetActive(true);
            context.Panel.RectTransform.SetParent(_canvas.transform);
            context.Panel.RectTransform.localScale = Vector3.one;
            context.Panel.RectTransform.anchoredPosition3D = Vector3.zero;
            context.Panel.RectTransform.sizeDelta = Vector3.zero;
            context.Panel.RectTransform.SetAsLastSibling();
        
            // https://issuetracker.unity3d.com/issues/ui-elements-are-no-longer-rendered-when-deactivating-and-reactivating-the-parent-gameobject
            // Inner Canvas(Nested Canvas) 이슈
            context.Panel.Canvas.overrideSorting = true;
            context.Panel.Canvas.sortingOrder = 100;
            context.Panel.Canvas.enabled = false;
            
            context.Panel.CanvasGroup.alpha = 1f;
            
            context.Panel.Interactable = false;

            if (context.Phase == UIPanel.Context.Phases.Load)
            {
                await context.Enter();
            }

            context.Resume();
            
            context.Panel.Canvas.enabled = true;

            prevContext.Panel.Canvas.sortingOrder = 101;
            
            prevContext.Transition.InPanel = context.Panel;
            
            MessageService.Instance.Publish(UIPanelChangedEvent.Create(context.Panel, UIPanelChangedEvent.Types.Pop));

            await prevContext.Transition.In();
            
            context.Panel.Interactable = true;
            
            await prevContext.Exit();

            // await UIPanel.Release(prevContext.Panel);
        }

        public void Transit(UIPanel.Context context)
        {
            TransitAsync(context).Forget();
        }

        public async UniTask TransitAsync(UIPanel.Context context)
        {
            if (_contextStack.Count <= 0)
            {
                await PushAsync(context);
                return;
            }

            var prevContext = _contextStack[^1];
            
            // Reverse Transition
            prevContext.Transition.Type = Transition.Types.Pop;
            prevContext.Transition.OutPanel = prevContext.Panel;
            
            prevContext.Panel.Canvas.sortingOrder = 99;
            prevContext.Panel.Interactable = false;
            
            prevContext.Pause();

            await prevContext.Transition.Out();

            _contextStack.RemoveAt(_contextStack.Count - 1);
            _contextStack.Add(context);

            if (context.Phase is UIPanel.Context.Phases.Exit or UIPanel.Context.Phases.None)
            {
                await context.Load();
            }

            context.Panel.gameObject.SetActive(true);
            context.Panel.RectTransform.SetParent(_canvas.transform);
            context.Panel.RectTransform.localScale = Vector3.one;
            context.Panel.RectTransform.anchoredPosition3D = Vector3.zero;
            context.Panel.RectTransform.sizeDelta = Vector3.zero;
            context.Panel.RectTransform.SetAsLastSibling();
        
            // https://issuetracker.unity3d.com/issues/ui-elements-are-no-longer-rendered-when-deactivating-and-reactivating-the-parent-gameobject
            // Inner Canvas(Nested Canvas) 이슈
            context.Panel.Canvas.overrideSorting = true;
            context.Panel.Canvas.sortingOrder = 100;
            context.Panel.Canvas.enabled = false;
            
            context.Panel.CanvasGroup.alpha = 1f;
            
            context.Panel.Interactable = false;

            if (context.Phase == UIPanel.Context.Phases.Load)
            {
                await context.Enter();
            }

            context.Resume();
            
            context.Panel.Canvas.enabled = true;

            prevContext.Transition.InPanel = context.Panel;
            
            MessageService.Instance.Publish(UIPanelChangedEvent.Create(context.Panel, UIPanelChangedEvent.Types.Pop));

            await prevContext.Transition.In();
            
            context.Panel.Interactable = true;
            
            await prevContext.Exit();
        }

        public void Clear()
        {
            ClearAsync().Forget();
        }
        
        public async UniTask ClearAsync()
        {
            for (int i = 1; i <= _contextStack.Count; i++)
            {
                var context = _contextStack[^i];
                context.Pause();
                await context.Exit();
            }
            
            _contextStack.Clear();

            await UIPanel.Clear();
        }

        public bool OnEscape()
        {
            if (_contextStack.Count <= 0)
            {
                return false;
            }

            var currentContext = _contextStack[^1];

            if (currentContext.Panel.OnEscape() == true &&
                1 < _contextStack.Count)
            {
                Pop();
            }

            return true;
        }
        
        public UIPanel Get(Type t)
        {
            foreach (var i in _contextStack)
            {
                if (i.Panel.GetType() == t)
                {
                    return i.Panel;
                }
            }

            return null;
        }
    }
}