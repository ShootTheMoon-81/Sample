using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UI.Overlay;
using UI.Panel;
using UI.Transitions;
using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    [DefaultExecutionOrder(-100)]
    public class UIManager : MonoSingleton<UIManager>
    {
        [SerializeField]
        private Camera _uiCamera;
        
        [SerializeField]
        private UIPanelStack _panelStack;
        
        [SerializeField]
        private UIOverlayStack _overlayStack;
        
        [SerializeField]
        private Canvas _cameraCanvas;

        public UIPanelStack PanelStack => _panelStack;
        public UIOverlayStack OverlayStack => _overlayStack;

        public Camera UICamera => _uiCamera;
        
        public Canvas CameraCanvas => _cameraCanvas;
        
        private Vector2 _resolution;
        
        private void Awake()
        {
            // GetComponentsInChildren(_canvasScalers);
        }

        public void Clear()
        {
            ClearAsync().Forget();
        }

        public async UniTask ClearAsync()
        {
            await _overlayStack.ClearAsync();
            await _panelStack.ClearAsync();
        }

        private void Update()
        {
            var viewport = _uiCamera.rect;
            var currentResolution = new Vector2(Screen.width * viewport.width, Screen.height * viewport.height);

            if (_resolution != currentResolution)
            {
                _resolution = currentResolution;

                _uiCamera.orthographicSize = _resolution.y * 0.5f;
                _uiCamera.transform.position = _resolution * new Vector2(0.5f + viewport.x, 0.5f + viewport.y);
            }
            
            if (Input.GetKeyDown(KeyCode.Escape) == true)
            {
                if (_overlayStack.OnEscape() == true)
                {
                    return;
                }

                if (_panelStack.OnEscape() == true)
                {
                    return;
                }
            }
        }
    }
}