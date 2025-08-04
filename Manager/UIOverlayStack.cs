using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UI.Overlay;
using UnityEngine;
using Extensions;
using System;
using MessageSystem;
using UI.Messages;

namespace Managers
{
    public class UIOverlayStack : MonoBehaviour
    {
        [SerializeField]
        private Canvas _canvas;
        
        private SortedList<int, List<UIOverlay.Context>> _orderedStack = new();

        public bool IsEmpty => _orderedStack.Count <= 0;

        private int HighestOrder => _orderedStack.Keys[^1];


        public void Push(UIOverlay.Context context)
        {
            PushAsync(context).Forget();
        }

        public async UniTask PushAsync(UIOverlay.Context context)
        {
            if (IsEmpty == false && HighestOrder <= context.SortingOrder)
            {
                var prevContext = _orderedStack[HighestOrder][^1];

                if (prevContext.Overlay != null)
                {
                    prevContext.Overlay.Interactable = false;
                }
            }

            if (_orderedStack.TryGetValue(context.SortingOrder, out var contextStack) == false)
            {
                contextStack = new List<UIOverlay.Context>();
                _orderedStack[context.SortingOrder] = contextStack;
            }
            
            contextStack.Add(context);
            
            await context.Load();
            
            await UniTask.Yield();
            
            context.Overlay.gameObject.SetActive(true);
            context.Overlay.RectTransform.SetParent(_canvas.transform);
            context.Overlay.RectTransform.localScale = Vector3.one;
            context.Overlay.RectTransform.anchoredPosition3D = Vector3.zero;
            context.Overlay.RectTransform.sizeDelta = Vector3.zero;
            context.Overlay.RectTransform.SetAsLastSibling();
            
            // https://issuetracker.unity3d.com/issues/ui-elements-are-no-longer-rendered-when-deactivating-and-reactivating-the-parent-gameobject
            // Inner Canvas(Nested Canvas) 이슈
            context.Overlay.Canvas.overrideSorting = true;
            context.Overlay.Canvas.sortingOrder = 100 + (100 * context.SortingOrder + contextStack.Count * 2 + 1);
            
            context.Overlay.CanvasGroup.alpha = 1f;

            context.Overlay.Interactable = false;
            context.Overlay.Canvas.enabled = true;

            await context.Enter();

            if (HighestOrder <= context.SortingOrder)
            {
                context.Overlay.Interactable = true;
            }

            context.Resume();

            MessageService.Instance.Publish(UIOverlayChangedEvent.Create(context.Overlay, UIOverlayChangedEvent.Types.Push));
        }

        public async UniTask PushAndWaitForClose(UIOverlay.Context context)
        {
            await PushAsync(context);
            await UniTask.WaitUntil(() => context.Phase == UIOverlay.Context.Phases.Exit, PlayerLoopTiming.LastUpdate);
        }

        public void Close(Type t)
        {
            foreach (IList<UIOverlay.Context> contextStack in _orderedStack.Values.Reverse())
            {
                foreach (var context in contextStack)
                {
                    if (context.Overlay?.GetType() == t)
                    {
                        CloseAsync(context.Overlay).Forget();
                        return;
                    }
                }
            }
        }

        public void Close(UIOverlay overlay)
        {
            CloseAsync(overlay).Forget();
        }

        public async UniTask CloseAsync(UIOverlay overlay)
        {
            var context = GetContext(overlay);

            if (context == null)
            {
                return;
            }

            if (context.Phase != UIOverlay.Context.Phases.Resume)
            {
                await UniTask.WaitUntil(() => context.Phase == UIOverlay.Context.Phases.Resume);
            }

            context.Overlay.Interactable = false;

            await context.Exit();

            var contextStack = _orderedStack[context.SortingOrder];

            contextStack.Remove(context);

            if (contextStack.Count <= 0)
            {
                _orderedStack.Remove(context.SortingOrder);
            }

            if (IsEmpty == false)
            {
                var prevContext = _orderedStack[HighestOrder][^1];

                if (prevContext.Overlay != null)
                {
                    prevContext.Overlay.Interactable = true;
                }
            }
        }
        
        public void Clear()
        {
            ClearAsync().Forget();
        }
        
        public async UniTask ClearAsync()
        {
            foreach (IList<UIOverlay.Context> contextStack in _orderedStack.Values.Reverse())
            {
                foreach (var context in contextStack.Reverse())
                {
                    await context.Exit();

                }
                
                contextStack.Clear();
            }
            
            _orderedStack.Clear();

            await UIOverlay.Clear();
        }

        public bool OnEscape()
        {
            if (IsEmpty == true)
            {
                return false;
            }

            foreach (IList<UIOverlay.Context> contextStack in _orderedStack.Values.Reverse())
            {
                foreach (var context in contextStack.Reverse())
                {
                    if (context.Phase is UIOverlay.Context.Phases.Enter or UIOverlay.Context.Phases.Resume &&
                        context.Overlay.OnEscape() == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private UIOverlay.Context GetContext(UIOverlay overlay)
        {
            foreach (var contextStack in _orderedStack.Values)
            {
                var index = contextStack.FindIndex(context => context.Overlay == overlay);

                if (0 <= index)
                {
                    return contextStack[index];
                }
            }

            return null;
        }
    }
}