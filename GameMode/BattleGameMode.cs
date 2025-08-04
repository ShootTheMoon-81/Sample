using Cysharp.Threading.Tasks;  
using UI.Panel;
using MessageSystem;
using UI.Messages;
using System;
using System.Collections;
using UI.Overlay;
using UnityEngine;
using Managers;
using Data;

namespace GameModes
{
    #region 데이터 박스
    public abstract class BaseBattleData 
    {
        public BattleModeType BattleGameType;
    }

    public class BattleStoryData : BaseBattleData
    {
        public string TableName;
        public string storyInfoId;
        public string partyGroupId;
        public GameMode.Context _exitContext;
    }

    public class BattleArenaData : BaseBattleData
    {

    }

    public class BattleAdventureData : BaseBattleData
    {

    }
    #endregion

    public class BattleGameMode : GameMode<BattleGameMode, BaseBattleData>
    {
        private BaseBattleData _battleData;

        public override UIPanel.Context DefaultPanelContext =>
            UIBattlePanel_New.CreateContext(new UIBattlePanel_New.DataBox
            {
                GameType = _battleData.BattleGameType,
                BattleData = _battleData
            });
           

        //public override BattleGameType GetState() => battleGameType;

        public override async UniTask OnEnter(BaseBattleData gameData)
        {
            _battleData = gameData;

            // 무엇인가 처리할 일이 있다면 사용
            switch (gameData)
            {
                case BattleStoryData storyData:
                    {

                    }
                    break;

                case BattleArenaData arenaData:
                    {

                    }
                    break;

                case BattleAdventureData adventureData:
                    {

                    }
                    break;
            }

            //if (_battleGameType is BattleModeType.AdventureMode || _battleGameType is BattleModeType.ArenaMode)
            //    MessageService.Instance.Subscribe<BattleEndProcessEvent>(BattleEndProcess); 
        }
        public override async UniTask OnExit()
        {
            TimeScaleManager.Instance.Release();
            MessageService.Instance.PublishImmediately(GameModeBattleExitEvent.Create());
            //if (_battleGameType is BattleModeType.AdventureMode || _battleGameType is BattleModeType.ArenaMode)
            //    MessageService.Instance.Unsubscribe<BattleEndProcessEvent>(BattleEndProcess);
        }
    }
}