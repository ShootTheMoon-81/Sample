using Cysharp.Threading.Tasks;
using UI.Panel;
using UIStyle;
using UnityEngine;


namespace GameModes
{
    public class AdventureGameMode : GameMode<AdventureGameMode>
    {
        public override UIPanel.Context DefaultPanelContext => UIAdventureDashboardPanel.CreateContext(new UIAdventureDashboardPanel.State());

        public override async UniTask OnEnter()
        {
            
        }

        public override async UniTask OnExit()
        {
            
        }
    }
}