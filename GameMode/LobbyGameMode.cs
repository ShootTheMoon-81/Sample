using Cysharp.Threading.Tasks;
using Managers;
using Field;
using MessageSystem;
using SoundSource;
using UI.Messages;
using UI.Overlay;
using UI.Panel;
using UnityEngine;

namespace GameModes
{
    public class LobbyGameMode : GameMode<LobbyGameMode>
    {
        public override UIPanel.Context DefaultPanelContext => UILobbyPanel.CreateContext();

        private Location _location;

        public override async UniTask OnEnter()
        {
            await UIManager.Instance.OverlayStack.PushAsync(UINavigationOverlay.CreateContext());
            
            MessageService.Instance.Publish(UINavigationShowRequest.Create());

            CameraManager.Instance.Initialize(Vector3.zero);
            //Camera.main.GetUniversalAdditionalCameraData().SetRenderer(0);

             _location = Location.Create("Kingdom");
            await _location.Load();

            _location.OnEnter();
        }

        public override async UniTask OnExit()
        {
            CameraManager.Instance.Terminate();
            //Camera.main.GetUniversalAdditionalCameraData().SetRenderer(1);

            if (_location != null)
            {
                _location.Unload();
                DestroyImmediate(_location.gameObject);

                _location = null;
            }
        }
    }
}