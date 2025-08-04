using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameModes.Transitions
{
    public class BattleModeTransition : Transition
    {
        private GameObject _transition;
        private BattleModeTransitionContoller _transitionContoller;
        
        public override UniTask OnLoad() => LoadTransitionPrefab();

        public override UniTask Out() => DirectingOutTransition();

        public override UniTask In() => DirectingInTransition();

        // 아웃 (변경 시작)
        private async UniTask DirectingOutTransition()
        {
            if (_transitionContoller == null)
            {
                return;
            }
            
            _transitionContoller.SetOutDirecting();
            
            await UniTask.Delay(Mathf.FloorToInt(_transitionContoller.OutDirectingTime * 1000));
        }

        // 인 (변경 끝남)
        private async UniTask DirectingInTransition()
        {
            if (_transitionContoller == null)
            {
                return;
            }
            
            _transitionContoller.SetInDirecting();
            
            await UniTask.Delay(Mathf.FloorToInt(_transitionContoller.InDirectingTime * 1000));
            
            //InstanceManager.Instance.Return(_transition);
            GameObject.DestroyImmediate(_transition);
        }

        private async UniTask LoadTransitionPrefab()
        {
            GameObject loadResource = await AssetManager.Instance.LoadAssetAsync("Assets/Data/Common/Prefab/BattleModeTransition.prefab");
            if (loadResource == null)
            {
                DebugHelper.Log($"Can't find asset bundle: Assets/Data/Common/Prefab/BattleModeTransition.prefab");
                
                return;
            }
            
            await UniTask.Yield();

            _transition = GameObject.Instantiate(loadResource);
            //_transition = InstanceManager.Instance.GetFromSource(loadResource);
            _transitionContoller = _transition.GetComponent<BattleModeTransitionContoller>();
            if (_transitionContoller == null)
            {
                DebugHelper.Log("Can't find script: BattleModeTransitionContoller");
            }
        }
    }
}