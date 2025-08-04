using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using GameModes;
using Users;
using MessageSystem;
using UI.Overlay;
using UI.Messages;
using Cysharp.Threading.Tasks;
using Data;
using GameModes.Transitions;
using Managers;
using System.Collections;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

namespace UI.Panel
{
    public class UIBattlePanel_New : UIPanel<UIBattlePanel_New, UIBattlePanel_New.DataBox>
    {
        #region 내부 클래스
        public enum eHudRootType
        {
            Top,
            Bottom,
            Center,
        }

        [Serializable]
        public class HudRootObject
        {
            public eHudRootType HudType;
            public GameObject RootObject;
        }

        public struct DataBox
        {
            public BattleModeType GameType;
            public BaseBattleData BattleData;
        }
        #endregion

        [Header("Hud 루트")]
        [SerializeField] private List<HudRootObject> _battleHudRoot;

        [Header("캐릭터 슬롯")]
        [SerializeField] private List<BattleCharSlot_New> _charSlotList;

        [Header("스테이지 웨이브")]
        [SerializeField] private UIBattleWave _battleWaveUI;

        [Header("보스정보")]
        [SerializeField] private BattleBossUI _bossUI;

        [Header("배틀 다이얼로그")]
        [SerializeField] private BattleDialogue _battleDialogue;

        [Header("UI 툴팁")]
        [SerializeField] private UIBattleNoticeToolTip _battleNoticeToolTip;

        [Header("기타")]
        [SerializeField] private BattleAutoIcon _autoIcon;
        [SerializeField] private BattleGameSpeedIcon _gameSpeedIcon;
        [SerializeField] private BattleTimeUI _basicTimeUI;
        [SerializeField] private BattleTimeUI _battleTimeUI;
        [SerializeField] private BattlePauseIcon _pauseIcon;
        [SerializeField] private CriwareMovieController _movieController;
        [SerializeField] private RawImage _cutSceneImage;
        [SerializeField] private GameObject _blockerBG;
        //[SerializeField] private CutSceneController _cutSceneController;

        protected float _playerMoveSpeed;
        protected float _playerMoveTime;
        private BossType _curBossType;
        private BaseActor _bossActor;

        private GameObject _panelGameObject;
        private IngameGameView _BattleView; // 게임 프로세서

        private BattleModeType _gameType = BattleModeType.None;
        private bool _isLoad = false;
        private float _prevTimeScale = 0.0f;
        private BattleModePartyType _partyType = BattleModePartyType.AdventureModeParty;

        private bool _battleUIStatus = true;
        private bool _startActiveSkill = false;

        private BaseBattleData _battleData;
        private BattleModeInfoData _battleModeData;
        public IngameGameView BattleProcesser
        {
            get
            {
                if (_panelGameObject == null)
                {
                    DebugHelper.LogError("UIBattlePanel_New 패널을 로드하지 못하였습니다.");
                    return null;
                }

                if (_BattleView == null)
                {
                    GameObject obj = Instantiate(_panelGameObject);
                    _BattleView = obj.GetComponentInChildren<IngameGameView>();
                }

                return _BattleView;
            }
        }


        public AdventureStageData CurStageData
        {
            get
            {
                if (IsLoad == false)
                    return null;

                try
                {
                    return DataTable.AdventureStageDataTable.GetByStageInfoId(BattleProcesser.GetStageBattleInfo().StageId);
                }
                catch (Exception e)
                {
                    DebugHelper.LogError($"현재 스테이지 정보(ID: {BattleProcesser.GetStageBattleInfo().StageId})를 불러올 수 없습니다. => {e}");
                    return null;
                }
            }
        }


        public ArenaBaseData CurArenaData
        {
            get
            {
                if (IsLoad == false)
                    return null;

                try
                {
                    return DataTable.ArenaBaseDataTable[User.My.ArenaInfo.PlayingArenaId];
                }
                catch (Exception e)
                {
                    DebugHelper.LogError($"현재 아레나 정보(ID: {User.My.ArenaInfo.PlayingArenaId})를 불러올 수 없습니다. => {e}");
                    return null;
                }
            }
        }

        public bool IsLoad
        {
            get
            {
                if (_isLoad == false)
                {
                    DebugHelper.LogError("패널이 로드되지 않았습니다.");
                    return false;
                }

                return true;
            }
        }

        private List<string> _curPartyKeyInfo
        {
            get
            {
                if (_partyType == BattleModePartyType.StoryModeParty)
                {
                    if (_battleData is BattleStoryData storyData)
                    {
                        return DataTable.StoryPartyInfoDataTable.GetGroupByGroupId(storyData.partyGroupId).Select(x => x.CharacterId).ToList();
                    }
                    else
                    {
                        DebugHelper.LogError($"파티그룹 아이디가 존재하지 않습니다. 스토리모드 파티 정보 로드를 실패합니다.");
                        var tempGroupID = DataTable.StoryPartyInfoDataTable.FirstOrDefault().GroupId;
                        return DataTable.StoryPartyInfoDataTable.GetGroupByGroupId(tempGroupID).Select(x => x.CharacterId).ToList();
                    }
                }
                else
                {
                    //int partyId = 2; // FIXME : 덱 프리셋 관련 내용 정리 후 수정. 현재 아레나 공격덱 2로 셋팅 (아레나 베틀뷰에도 _partyId 이름으로 동일한 것이 있음. 추후 통일해줘야한다.) 
                    return User.My.PartyInfo.GetPartyCharactersId(_partyType);
                }
            }
        }

        private int _curPartyNum
        {
            get
            {
                return _curPartyKeyInfo.Count;
            }
        }

        private List<Data.CharacterData> _curPartyBaseDataList
        {
            get
            {
                List<Data.CharacterData> newData = new List<Data.CharacterData>();
                foreach (var key in _curPartyKeyInfo)
                {
                    newData.Add(DataTable.CharacterDataTable[key]);
                }

                return newData;
            }
        }

        //public void Update()
        //{
        //    //if( Application.platform == RuntimePlatform.Android)
        //    {
        //        if (Input.GetKeyDown(KeyCode.Escape))
        //        {
        //            //if (Managers.UIManager.Instance.OverlayStack.IsEmpty)
        //            //{
        //                  MessageService.Instance.Publish(BattlePauseEvent.Create());
        //            //}
        //        }
        //    }
        //}

        public override async UniTask OnLoad()
        {
        }

        public override async UniTask OnEnter(DataBox dataBox)
        {
            _gameType = dataBox.GameType;
            _battleData = dataBox.BattleData;
            _battleModeData = DataTable.BattleModeInfoDataTable.GetById(_gameType);
            string path = "";
            switch (_gameType)
            {
                case BattleModeType.StoryMode:
                    {
                        path = "Assets/Data/GameModes/Battle/Prefab/StoryBattleScene.prefab";
                        _partyType = Oz.BattleModePartyType.StoryModeParty;
                    }
                    break;
                case BattleModeType.AdventureMode:
                    {
                        path = "Assets/Data/GameModes/Battle/Prefab/StageBattleScene.prefab";
                        _partyType = Oz.BattleModePartyType.AdventureModeParty;
                    }
                    break;
                case BattleModeType.ArenaMode:
                    {
                        path = "Assets/Data/GameModes/Battle/Prefab/ArenaBattleScene.prefab";
                        _partyType = Oz.BattleModePartyType.ArenaModeAttackParty;
                    }
                    break;
            }
            
            //기본 게임패널 로드
            _panelGameObject = await AssetManager.Instance.LoadAssetAsync(path);
            if (_panelGameObject == null)
            {
                DebugHelper.LogError("UIArenaBattlePanel 패널을 로드하지 못하였습니다.");
                return;
            }

            // 배틀 프로세서 초기화
            if (BattleProcesser != null)
            {
                PrepareInitialize(dataBox);

                await BattleProcesser.InitializeAsync(_gameType , () =>
                {
                    _isLoad = true;
                    Init();

                    PostInitialize(dataBox);
                });
            }
        }

        // 초기화 전 작업이 필요하면 이곳에서 정의
        public void PrepareInitialize(DataBox dataBox)
        {
            // 데이터 박스 세팅
            BattleProcesser.SetDataBox(dataBox);

            // 각 타입별 세팅
            switch (dataBox.BattleData)
            {
                case BattleStoryData storyData:
                    {
                        if (BattleProcesser is StoryBattlePanel storyBattle)
                        {
                            storyBattle.SetBattleStoryData(dataBox.BattleData as BattleStoryData);
                        }
                    }
                    break;

                case BattleArenaData arenaData:
                    {
                        //BattleProcesser.
                    }
                    break;

                case BattleAdventureData adventureData:
                    {
                        //BattleProcesser.
                    }
                    break;
            }
        }

        // 초기화 후 작업이 필요하면 이곳에서 정의
        public void PostInitialize(DataBox dataBox)
        {
            BattleProcesser.CutSceneImage = _cutSceneImage;

            switch (dataBox.BattleData)
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
        }

        public override async UniTask OnExit()
        {
            if (BattleProcesser != null)
                BattleProcesser.Terminate();

            ClearValue();
        }

        private void Init()
        {
            if (_gameType == BattleModeType.None)
                DebugHelper.LogError($"배틀 게임 타입이 존재하지 않습니다. => {_gameType}");

            try
            {
                InitValue();
            }
            catch (Exception e)
            {
                DebugHelper.LogError($"{e}");
            }
        }

        private void InitValue()
        {
            // 필수 UI
            _autoIcon.Init(BattleProcesser.GetIsActiveAuto(), _battleModeData.AutoSettingType);
            _gameSpeedIcon.Init(_gameType);
            _pauseIcon.Init();

            // 타이머 셋
            SetTimer();

            //_cutSceneController.Init();

            // 타입별 UI
            _battleWaveUI.Init();
            _battleNoticeToolTip.Init();
            _battleDialogue.Init();

            // 게임타입 별 Lock UI
            SetLockUIForGameType();

            // 캐릭터 슬롯
            InitCharacterSlot();

            // 이벤트 메세지
            SubscribeEvents();

            // 허드 노출
            //HudInOutAnimation(true);

            // 튜토리얼 시작...
            //Tutorial.TutorialFlowPerformer.Instance.InGameTutorialGuide("Tutorial_AITypeInfo_PopupGuide", () =>
            //{

            //});
        }

        private void ClearValue()
        {
            // 필수 UI
            _battleWaveUI.Clear();
            _battleNoticeToolTip.Clear();
            _autoIcon.Clear();
            _gameSpeedIcon.Clear();
            _basicTimeUI.Clear();
            _battleDialogue.Clear();
            //_cutSceneController.Clear();

            // 타입별 UI
            _pauseIcon.Clear();
            _bossUI.Clear();

            // 캐릭터 슬롯
            ClearCharacterSlot();

            // 이벤트 메세지
            UnsubscribeEvents();
        }

        public override bool OnEscape()
        {
            if (_gameType != BattleModeType.StoryMode)
                MessageService.Instance.PublishImmediately(BattlePauseEvent.Create());

            return true;
        }

        //private void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.Escape))
        //    {
        //        if (BattleProcesser.GetIsGamePause())
        //            OnCloseBattlePause();
        //        else
        //            MessageService.Instance.Publish(BattlePauseEvent.Create());
        //    }
        //}

        private void InitCharacterSlot()
        {
            for (int i = 0; i < _charSlotList.Count; ++i)
            {
                var curCharacterSlot = _charSlotList[i];
                if (curCharacterSlot == null)
                {
                    DebugHelper.LogError($"캐릭터 슬롯 리스트에 에러가 발생하였습니다. curCharacterSlot == null");
                    return;
                }

                if (_curPartyKeyInfo.Count > i)
                {
                    var partyInfo = _curPartyKeyInfo[i];
                    var targetCharacter = GetCharacter(partyInfo);
                    if (targetCharacter == null)
                    {
                        DebugHelper.LogError($"파티 내 타겟 캐릭터를 찾을 수 없습니다.");
                        return;
                    }

                    curCharacterSlot.Init(targetCharacter, _gameType);
                }
                else
                {
                    curCharacterSlot.Clear();
                }
            }
        }

        private void ClearCharacterSlot()
        {
            foreach (var charSlot in _charSlotList)
            {
                charSlot.Clear();
            }
        }

        private void SetLockUIForGameType()
        {
            switch (_gameType)
            {
                case BattleModeType.ArenaMode:
                    {
                        _battleWaveUI.SetActiveUI(false);
                        _battleNoticeToolTip.SetActiveUI(false);
                        _bossUI.SetActiveUI(false);
                        _pauseIcon.SetActiveUI(true);
                    }
                    break;

                // 추후에 같은 프리팹 쓰면 채워 넣어야 함
                case BattleModeType.AdventureMode:
                    {
                        _bossUI.SetActiveUI(false);
                        _pauseIcon.SetActiveUI(true);
                    }
                    break;
                case BattleModeType.StoryMode:
                    {
                        _bossUI.SetActiveUI(false);
                        _pauseIcon.SetActiveUI(false);
                    }
                    break;
            }
        }

        private void OnOut()
        {
            Network.PacketProcessor.Instance.SendRequest(new Network.Packets.Game.Stage.StageCancelPacket()
            .OnCompleted(_ =>
            {
                UiDefine.StageClearMove(UiDefine.EStageClearType.StageReStart);
            }));
        }

        private void OnFail()
        {
            BattleProcesser.GameEndOfBattle(false);
        }

        // 실패시 재도전
        private void OnRetry(string Id)
        {
            ImageLoading.Process(RetryAsync(Id).ToCoroutine());
        }

        private async UniTask RetryAsync(string Id)
        {
            var packetProcessor = Network.PacketProcessor.Instance;

            await UniTask.Yield();
            await packetProcessor.SendRequestAsync(new Network.Packets.Game.Stage.StageCancelPacket());

            //var party = User.My.PartyInfo[_partyType];
            //await packetProcessor.SendRequestAsync(new Network.Packets.Game.Character.FormationPartyPacket(party));
            //await packetProcessor.SendRequestAsync(new Network.Packets.Game.Stage.StartStagePacket(User.My.StageInfo.SelectedStage.Id));
            await packetProcessor.SendRequestAsync(new Network.Packets.Game.Stage.StageStartPacket(Id, _gameType, _partyType));

            var battleData = new BattleAdventureData();
            battleData.BattleGameType = _gameType;
            await Managers.GameModeManager.Instance.TransitAsync(BattleGameMode.CreateContext(battleData));
        }

        private void ShowBattleFailPanel()
        {
            Managers.UIManager.Instance.PanelStack.Push(UIBattleResultFailPanel.CreateContext(new()
            {
                KingDomCallback = () =>
                {
                    UiDefine.StageClearMove(Oz.Define.UiDefine.EStageClearType.StageKingdom);
                },

                RetryCallback = () =>
                {
                    Network.PacketProcessor.Instance.SendRequest(
                        new Network.Packets.Game.Stage.StageStartPacket(User.My.AdventureModeInfo.GetSelectedAdventureStage().Id, BattleModeType.AdventureMode, BattleModePartyType.AdventureModeParty)
                            .OnCompleted(_ =>
                            {
                                UiDefine.StageClearMove(UiDefine.EStageClearType.StageReStart);
                            })
                            .OnFailed(_ =>
                            {
                                Network.PacketProcessor.Instance.SendRequest(
                                    new Network.Packets.Game.Stage.StageCancelPacket());
                            }));
                },

                OutCallback = () =>
                {
                    OnOut();
                },
                
                players = BattleProcesser.GetPlayerActors(false),
                
                BattleType = _gameType
            }));
        }

        private void OnCloseBattlePause(bool resetGamespeed = true)
        {
            SetBattleProcesserGamePause(false);

            if (resetGamespeed)
            {
                TimePause(false);
                BattleProcesser.SetPauseActiveSkillTimeline(false);
            }
        }

        private void SetBattleProcesserGamePause(bool isAble)
        {
            BattleProcesser.SetIsGamePause(isAble);
            //_cutSceneController.SetGameProcesserPause(isAble);
        }

        private void SetStopCharacter(bool isStop)
        {
            IEnumerable<BaseActor> allActors = BattleProcesser.GetActors();
            foreach (var actor in allActors)
            {
                if (isStop)
                    actor.BehaviourTree.Stop();
                else
                    actor.BehaviourTree.Play();
            }
        }

        private void SetTimer()
        {
            _basicTimeUI.SetActiveUI(_gameType != BattleModeType.ArenaMode);
            _battleTimeUI.SetActiveUI(_gameType == BattleModeType.ArenaMode);

            if (_gameType != BattleModeType.ArenaMode)
            {
                _basicTimeUI.SetRemainTimeCallBack(60.0f, () => { _basicTimeUI.SetGlowEffect(true); })
                    .SetEndCallBack(() => { BattleProcesser.GameEndOfBattle(false); })
                    .Init(_battleModeData.BattleTime);
            }
            else
            {
                _battleTimeUI.SetRemainTimeCallBack(60.0f, () => { _battleTimeUI.SetGlowEffect(true); })
                    .SetEndCallBack(() => { BattleProcesser.GameEndOfBattle(false); })
                    .Init(_battleModeData.BattleTime);
            }
        }

        private void TimePause(bool isPause)
        {
            if (isPause)
            {
                TimeScaleManager.Instance.SetTimeScale(TimeScaleType.Speedx0);
            }
            else
            {
                TimeScaleManager.Instance.ResetTimeScale();
            }
        }

        private void OnCharacterHud(bool isOn)
        {
            IEnumerable<BaseActor> allActors = BattleProcesser.GetActors();
            foreach (var actor in allActors)
            {
                actor.ActivateHud(isOn);
            }
        }

        private void OnBattleHud(bool isOn)
        {
            HudSetActive(isOn);
        }

        private void HudAlpha(float amount)
        {
            foreach (var hud in _battleHudRoot)
            {
                hud.RootObject.GetComponent<CanvasGroup>().alpha = amount;
            }
        }

        private void HudInOutAnimation(bool isIn)
        {
            foreach (var hud in _battleHudRoot)
            {
                hud.RootObject.PlayAllTween(isIn, false);
            }
        }

        private bool HudInOutAnimation(HudInOutAnimationEvent msg)
        {
            HudInOutAnimation(msg.IsIn);
            return true;
        }

        private void HudAlpha(eHudRootType type, float amount)
        {
            var hudData = _battleHudRoot.Where(x => x.HudType == type).FirstOrDefault();
            hudData.RootObject.GetComponent<CanvasGroup>().alpha = amount;
        }

        private void HudSetActive(bool isOn)
        {
            for (int i = 0; i < _battleHudRoot.Count; i++)
                _battleHudRoot[i].RootObject.SetActive(isOn);
        }


        //------------------------------------------------------------------------------------------------------------------------------------------------//
        // 이벤트 공간
        //------------------------------------------------------------------------------------------------------------------------------------------------//
        private void SubscribeEvents()
        {
            MessageService.Instance.Subscribe<BattleAutoEvent>(BattleAuto);
            MessageService.Instance.Subscribe<BattleSpeedUpEvent>(BattleSpeedUp);
            MessageService.Instance.Subscribe<BattleTimeUpEvent>(BattleTimeUp);
            MessageService.Instance.Subscribe<BattlePauseEvent>(BattlePause);
            MessageService.Instance.Subscribe<BattleEndProcessEvent>(BattleEndProcess);
            MessageService.Instance.Subscribe<BattleGameTimerStatusEvent>(BattleTimerStatus);
            MessageService.Instance.Subscribe<PlayCutSceneEvent>(PlayCutScene);
            //MessageService.Instance.Subscribe<StopCutSceneEvent>(StopCutScene);
            MessageService.Instance.Subscribe<ActiveSkillEvent>(StartActiveSkill);
            MessageService.Instance.Subscribe<BattleStartEvent>(DirectingBattleStart);
            MessageService.Instance.Subscribe<HudInOutAnimationEvent>(HudInOutAnimation);
            MessageService.Instance.Subscribe<BattleChapterEvent>(SetBattleChapter);
            //MessageService.Instance.Subscribe<BattleWaveEvent>(SetCurrentWave);
            MessageService.Instance.Subscribe<WaveProcessEvent>(WaveStart);
            MessageService.Instance.Subscribe<BossSettingEvent>(BossSetting);
            MessageService.Instance.Subscribe<BossHpBarUpdateEvent>(BossHpBarUpdate);
            MessageService.Instance.Subscribe<NextWaveMoveTimeEvent>(NextWaveMoveTime);
            MessageService.Instance.Subscribe<BossDirectingInProgressEvent>(BossDirectingInProgress);
            MessageService.Instance.Subscribe<BossMonsterFocusEvent>(BossMonsterFocus);
        }

        private void UnsubscribeEvents()
        {
            MessageService.Instance.Unsubscribe<BattleAutoEvent>(BattleAuto);
            MessageService.Instance.Unsubscribe<BattleSpeedUpEvent>(BattleSpeedUp);
            MessageService.Instance.Unsubscribe<BattleTimeUpEvent>(BattleTimeUp);
            MessageService.Instance.Unsubscribe<BattlePauseEvent>(BattlePause);
            MessageService.Instance.Unsubscribe<BattleEndProcessEvent>(BattleEndProcess);
            MessageService.Instance.Unsubscribe<BattleGameTimerStatusEvent>(BattleTimerStatus);
            MessageService.Instance.Unsubscribe<PlayCutSceneEvent>(PlayCutScene);
            //MessageService.Instance.Unsubscribe<StopCutSceneEvent>(StopCutScene);
            MessageService.Instance.Unsubscribe<ActiveSkillEvent>(StartActiveSkill);
            MessageService.Instance.Unsubscribe<BattleStartEvent>(DirectingBattleStart);
            MessageService.Instance.Unsubscribe<HudInOutAnimationEvent>(HudInOutAnimation);
            MessageService.Instance.Unsubscribe<BattleChapterEvent>(SetBattleChapter);
            //MessageService.Instance.Unsubscribe<BattleWaveEvent>(SetCurrentWave);
            MessageService.Instance.Unsubscribe<WaveProcessEvent>(WaveStart);
            MessageService.Instance.Unsubscribe<BossSettingEvent>(BossSetting);
            MessageService.Instance.Unsubscribe<BossHpBarUpdateEvent>(BossHpBarUpdate);
            MessageService.Instance.Unsubscribe<NextWaveMoveTimeEvent>(NextWaveMoveTime);
            MessageService.Instance.Unsubscribe<BossDirectingInProgressEvent>(BossDirectingInProgress);
            MessageService.Instance.Unsubscribe<BossMonsterFocusEvent>(BossMonsterFocus);
        }

        private bool BattleAuto(BattleAutoEvent msg)
        {
            if (_battleModeData.AutoSettingType == AutoSettingType.OnlyAuto)
            {
                // 메시지 출력
                _battleNoticeToolTip.SetPopup(LocalString.Get("Str_UI_Impossible_ChangeAuto"));
                return true;
            }

            BattleProcesser.SetIsActiveAuto();
            msg.callback.Invoke(BattleProcesser.GetIsActiveAuto());
            return true;
        }

        private bool BattleSpeedUp(BattleSpeedUpEvent msg)
        {
            // 메시지 출력후 리턴
            if (_battleModeData.SpeedSettingType == SpeedSettingType.DefalutSpeed)
            {
                _battleNoticeToolTip.SetPopup(LocalString.Get("Str_UI_Impossible_ChangeSpeed"));
                return true;
            }

            BattleProcesser.OnGameSpeed();
            return true;
        }

        private bool BattleTimeUp(BattleTimeUpEvent msg)
        {
            //BattleProcesser.GameEndOfBattle(false); // 타이머에서 이미 처리 중
            return true;
        }

        public bool BattlePause(BattlePauseEvent msg)
        {
            if (_startActiveSkill == true)
            {
                DebugHelper.Log($"Start Active Skill BattlePause return - {_battleUIStatus}");
                return true; 
            }

            bool isPause = BattleProcesser.IsGamePause;
            if (isPause)
            {
                OnCloseBattlePause();
                return true;
            }
            
            TimePause(!isPause);
            BattleProcesser.SetPauseActiveSkillTimeline(!isPause);
            SetBattleProcesserGamePause(!isPause);

            Managers.UIManager.Instance.OverlayStack.Push(UIBattlePausePopup.CreateContext(new()
            {
                AutoSettingType = _battleModeData.AutoSettingType,

                GiveUpCallback = () =>
                {
                    OnFail();
                    OnCloseBattlePause(false);
                },

                RestartCallback = () =>
                {
                    OnRetry(User.My.AdventureModeInfo.GetSelectedAdventureStage().Id);
                    OnCloseBattlePause(false);
                    //CameraManager.Instance.GetBattleCamController().ResetCamera();
                },

                ContinueCallback = () =>
                {
                    if (_startActiveSkill == false)
                    {
                        OnCloseBattlePause();
                    }
                },

                ChangeSkipAniCallback = (bool skip) =>
                {
                    BattleProcesser.SetIsActiveSkip(skip);
                },

            }));

            return true;
        }

        public bool SetBattleChapter(BattleChapterEvent msg)
        {
            MessageService.Instance.Publish(ShowUIWithAnimEvent.Create(true, 0.5f));
            msg.callback?.Invoke();

            return true;
        }

        public bool BattleEndProcess(BattleEndProcessEvent msg)
        {
            OnBattleHud(false);
            OnCharacterHud(false);

            // 모든 캐릭터 행동 정지  TODO : 웨이브 엔드 이벤트에서 처리하게 변경
            SetStopCharacter(true);

            if (msg.success == false)
            {
                ShowBattleFailPanel();
                OnCloseBattlePause(false);
            }

            // MVP 출력
            ShowMVP(msg);
            msg.callback?.Invoke();
            return true;
        }

        public void ShowMVP(BattleEndProcessEvent msg)
        {
            if (_gameType == BattleModeType.StoryMode)
                return;

            if (msg.success == true)
            {
                UIManager.Instance.PanelStack.Push(UIBattleResultMvpPanel.CreateContext(new()
                {
                    GameType = _gameType,
                    Stage = msg.clearRes,
                    Arena = msg.areanData,
                    MvpActor = msg.mvpActor,
                    Actors = BattleProcesser.GetActors(),
                    EnterCallBack = () =>
                    {
                        BattleProcesser.SetVisible(false);
                    },
                }));
            }
        }

        public bool BattleTimerStatus(BattleGameTimerStatusEvent msg)
        {
            _basicTimeUI.SetPause(msg.pause);
            return true;
        }

        private Character GetCharacter(string key)
        {
            var playerActors = BattleProcesser.GetPlayerActors(true);
            var baseActor = playerActors?.Where(x => x.CharData.Base.Id == key).FirstOrDefault();
            if (baseActor == null)
            {
                DebugHelper.LogError($"캐릭터를 찾지 못했습니다. (key: {key})");
                return null;
            }

            return (Character)baseActor;
        }

        public bool PlayCutScene(PlayCutSceneEvent msg)
        {
            bool rotation = msg._rotation;

            //string path = msg._path + ".usm";
            _movieController.PlayMovie(msg._path, msg._rotation, () =>
            {

            });

            return true;
        }

        public bool StopCutScene(StopCutSceneEvent msg)
        {
            _movieController.StopMovie();

            return true;
        }

        private bool StartActiveSkill(ActiveSkillEvent msg)
        {
            HudAlpha(msg._start == true ? 0 : 1);

            _blockerBG?.SetActive(msg._start);

            _startActiveSkill = msg._start;

            return true;
        }

        private bool DirectingBattleStart(BattleStartEvent msg)
        {
            UIManager.Instance.OverlayStack.Push(UIBattleStartOverlay.CreateContext(new UIBattleStartOverlay.State
            {
                BattleGameType = _gameType,
                EndCallback = msg._endCallback
            }));

            return true;
        }

        private bool WaveStart(WaveProcessEvent msg)
        {
            switch (msg.State)
            {
                case Oz.Ingame.View.WaveProcess.eWaveState.WaveStart:
                    {

                    }
                    break;
                case Oz.Ingame.View.WaveProcess.eWaveState.WaveClear:
                    {

                    }
                    break;
            }

            return true;
        }

        // 몇 초만에 가야하는가 (플레이어 이동)
        protected bool NextWaveMoveTime(NextWaveMoveTimeEvent msg)
        {
            //MoveProgress(msg.MoveSpeed, msg.MoveTime);
            _playerMoveSpeed = msg.MoveSpeed;
            _playerMoveTime = msg.MoveTime;
            return true;
        }

        // 보스 세팅
        private bool BossSetting(BossSettingEvent msg)
        {
            _curBossType = msg.CurBossType;
            _bossActor = msg.BossActor;

            _bossUI.Init(_curBossType, _bossActor);
            _bossUI.SetActiveUI(true);

            return true;
        }

        // 보스 Hp bar 업데이트
        private bool BossHpBarUpdate(BossHpBarUpdateEvent msg)
        {
            _bossUI.UpdateHudBuffInfo();

            return true;
        }
        
        // 등장 연출
        private bool BossDirectingInProgress(BossDirectingInProgressEvent msg)
        {
            // 허드 관리
            HudAlpha(msg.IsBossDirecting ? 0 : 1);
            HudInOutAnimation(!msg.IsBossDirecting);

            // 워닝 이미지 (띠지 연출)
            if (msg.IsBossDirecting)
                StartCoroutine(BossWarrningAsync());
 
            return true;
        }

        // 보스 포커싱
        private bool BossMonsterFocus(BossMonsterFocusEvent msg)
        {
            if (msg.FocusOn)
                StartCoroutine(BossFoucsAsync(msg.CharacterID, msg.CharacterActor));

            return true;
        }

        // 보스 몬스터 워닝!!
        private IEnumerator BossWarrningAsync()
        {
            yield return null; 

            // 워닝 유아이
            UIManager.Instance.OverlayStack.Push(BattleBossEncountOverlay.CreateContext(new() { PlayerMoveTime = _playerMoveTime }));
            yield return new WaitForSeconds(_playerMoveTime);
        }

        // 보스 몬스터 포커싱!!
        private IEnumerator BossFoucsAsync(string characterID, BaseActor characterActor)
        {
            // 배틀 액션 카메라
            var cameraTableData = DataTable.BossAppearFocusDataTable.GetById(characterID);
            var battleActionCamera = CameraManager.Instance.GetBattleActionCamController();
            if (cameraTableData != null)
            {
                var bossPos = characterActor.transform.position;

                // 타입 1
                CameraManager.Instance.targetBattleAction.transform.position = CameraManager.Instance.targetBattle.transform.position;
                CameraManager.Instance.SetBlendType("BattleCamera", "BattleActionCamera", Cinemachine.CinemachineBlendDefinition.Style.Cut, 0.0f);
                //CameraManager.Instance.targetBattleAction.transform.position = bossPos;

                // 타입 2
                //CameraManager.Instance.targetBattleAction.transform.position = CameraManager.Instance.targetBattle.transform.position;
                //CameraManager.Instance.SetBlendType("BattleCamera", "BattleActionCamera", Cinemachine.CinemachineBlendDefinition.Style.Linear, cameraTableData.InDuration);

                // 카메라 변환 및 줌
                var curZoomSize = CameraManager.Instance.GetCameraOrthographic();
                battleActionCamera.SetZoomSize(curZoomSize);

                CameraManager.Instance.BattleActionTarget();
                battleActionCamera.CameraZoom(
                    new Vector3(bossPos.x + cameraTableData.OffsetX, bossPos.y + cameraTableData.OffsetY, bossPos.z),
                    cameraTableData.InDuration,
                    cameraTableData.ZoomSize,
                    AnimationCurve.Linear(0, 0, 1, 1));
            }

            // 애니메이션 (AppearAni 연출) 
            var stageMonsterData = ((Monster)_bossActor).StageMonsterData;
            //_bossActor.PlayAniSequence(stageMonsterData.AppearAniSequence, false, () =>
            //{
            //});

            // 수정
            _bossActor.PlayAppearAniSequence(() =>
            {
                _bossActor.SetState(BaseActor.ActorState.Idle);
            });

            // 보스 유아이
            var isGameStartAble = BattleProcesser.BattleStateType != BattleStateType.Wait;
            yield return UIManager.Instance.OverlayStack.PushAndWaitForClose(BattleBossEncountNameOverlay.CreateContext(new() { TargetCharaterID = characterID, BaseActor = characterActor, IsGameStartAble = isGameStartAble })).ToCoroutine();
     
            if (cameraTableData != null)
            {
                CameraManager.Instance.SetBlendType("BattleActionCamera", "BattleCamera", Cinemachine.CinemachineBlendDefinition.Style.Linear, cameraTableData.OutDuration);
                CameraManager.Instance.BattleTarget();
            }
        }


        // 과거에는 이렇게 함?
        //private async UniTask ProductionEffect(int waveIndex)
        //{

        //    // 카메라 세팅
        //    AdventureStageData TableData = DataTable.AdventureStageDataTable[_stageID];
        //    if (TableData != null)
        //    {
        //        var data = InGameTableUtils.GetInGameCameraValueData(_stageID, waveIndex + 1);
        //        _inGameView.SetBattleCameraData(data);
        //    }
        //}


#if UNITY_EDITOR
        private void OnEnable()
        {
            MessageService.Instance.Subscribe<ViewCharacterDebuggingEvent>(ViewCharacterDebugging);

        }

        private void OnDisable()
        {
            MessageService.Instance.Unsubscribe<ViewCharacterDebuggingEvent>(ViewCharacterDebugging);
        }

        private bool ViewCharacterDebugging(ViewCharacterDebuggingEvent msg)
        {


            return true;
        }
#endif
    }

}
