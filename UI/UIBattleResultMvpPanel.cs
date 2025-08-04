using Cysharp.Threading.Tasks;
using Data;
using GameModes;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Transitions;
using UnityEngine;
using UI.Overlay;
using Users;
using UIStyle;
using Managers;
using MessageSystem;
using System;
using UI.Messages;
using UnityEngine.UI;

namespace UI.Panel
{
    public class UIBattleResultMvpPanel : UIPanel<UIBattleResultMvpPanel, UIBattleResultMvpPanel.State>
    {
        public static Transition DefaultTransition => new FadeTransition();
        
        [SerializeField] UIStyles _uiStyle;
        [SerializeField] private TMP_Text _nameText = null;
        [SerializeField] private TMP_Text _titleText = null;

        [SerializeField] private GameObject _retryBtn = null;
        [SerializeField] private GameObject _nextStageBtn = null;
        [SerializeField] private GameObject _outBtn = null;
        [SerializeField] private GameObject _motionMvpRoot = null;

        // 연출 1. 승리문구 연출.
        [SerializeField]
        private GameObject _victoryRoot;
        [SerializeField]
        private GameObject _victoryPve;
        [SerializeField]
        private TweenManager _victoryPveTween;
        [SerializeField] 
        private GameObject _victoryPvp;
        [SerializeField]
        private TweenManager _victoryPvpTween;
        
        // 연출 2. 영웅 5마리. 프리팹 이름.
        [SerializeField]
        private GameObject _battleMvpPositionRoot;
        [SerializeField]
        private TweenManager _battleMvpPositionTween;
        
        // 연출 3. 계정 경험치.
        [SerializeField]
        private GameObject _accountExpRoot;
        [SerializeField]
        private TextMeshProUGUI _readerLevel;
        [SerializeField]
        private TextMeshProUGUI _readerName;
        [SerializeField]
        private Slider _readerExpSlider;
        [SerializeField]
        private TextMeshProUGUI _readerCurrentExpText;
        [SerializeField]
        private TextMeshProUGUI _readerNeedExpText;
        [SerializeField]
        private TextMeshProUGUI _readerExpEarned;
        [SerializeField]
        private TextMeshProUGUI _battleElapsedTime;

        // 연출 6. 다음 버튼
        [SerializeField]
        private TweenManager _nextTween;
        
        // 연출 7. Mvp 캐릭터 및 상세정보.
        [SerializeField]
        private GameObject _mvpCharacterRoot;
        [SerializeField]
        private TweenManager _characterOutTween;
        [SerializeField]
        private GameObject _mvpCharacterInfo;
        [SerializeField]
        private TweenManager _mvpCharacterTween;
        [SerializeField]
        private GameObject[] _mvpCharacterBg;

        [SerializeField] private UIAdventureBattleResult _adventureBattleResult;
        [SerializeField] private UIArenaBattleResult _arenaBattleResul;

        [SerializeField] List<UIMvpPositionItem> _partyCharacters;

        [SerializeField] private GameObject _pveButtons;
        [SerializeField] private GameObject _pvpButtons;

        [SerializeField]
        private GameObject _nextStageButton;
        
        private UICharacter _motionMvpCharacter = null;
        private CharacterData _mvpCharacter = null;
        private BaseActor _mvpActor = null;
        private StageClearAns _resultData = null;
        private OZArena _arenaData = null;
        private bool _completeQuest = false;
        private IEnumerable<BaseActor> _players = null;
        private BattleModePartyType _partyType = BattleModePartyType.AdventureModeParty;
        private Action _enterClallBack = null;
        private BattleModeType _battleType;

        public override async UniTask OnEnter(State state)
        {
            Time.timeScale = 1;
            GlobalVolumeRoot.Instance.GlobalVolumeBlur(true);

            _motionMvpCharacter = null;

            _players    = state.Actors;
            _mvpActor   = state.MvpActor;
            _resultData = state.Stage;
            _arenaData  = state.Arena;
            Type        = state.GameType;
            _enterClallBack = state.EnterCallBack;

            // 음성 출력
            PlayWaveClearCharacterVoice();
            Invoke("EnterCallBack", 0.5f);

            SetFirstDirection().Forget();
        }

        public override async UniTask OnExit()
        {
            GlobalVolumeRoot.Instance.GlobalVolumeBlur(false);
            
            _pveButtons.SetActive(false);
            _pvpButtons.SetActive(false);
            
            // 연출 7.
            for (int i = 0; i < _mvpCharacterBg.Length; i++)
            {
                _mvpCharacterBg[i].SetActive(false);
            }
            _mvpCharacterRoot.SetActive(false);
            _arenaBattleResul.gameObject.SetActive(false);
            _adventureBattleResult.gameObject.SetActive(false);
            _mvpCharacterRoot.SetActive(false);
           
            // 연출 3.
            _accountExpRoot.SetActive(false);
            
            // 연출 2.
            _battleMvpPositionRoot.SetActive(false);
            
            // 연출 1.
            _victoryPvp.SetActive(false);
            _victoryPve.SetActive(false);            
            _victoryRoot.SetActive(false);
        }

        public override bool OnEscape()
        {
            return false;
        }

        // 연출 플로우.
        // 1. 전투 승리 문구 연출.
        // 2. 영웅파티 카드가 촤르륵 연출.
        // 3. 계정 경험치 연출.
        // 4. 계정 레벨업 상황이라면 계정 레벨업 팝업 띄움. 유저액션으로 닫을때까지 대기하고 닫으면 5번.
        // 5. 영웅파티 경험치 증가 연출 + 영웅파티 레벨업 연출.
        // 6. 5번 연출 끝나면 버튼 생성해 유저액션 기다림. 버튼 누르면 7번.
        // 7. Mvp 캐릭터 일러스트와 함께 전투상세 결과를 보여준다.
        // 8. 보상창을 보여준다. 보상창은 유저액션으로 닫을 수 있다. 닫으면 9번.
        // 9. 7번 단계에서 하단버튼을 생성해 다음 액션을 기다린다.
        private async UniTask SetFirstDirection()
        {
            // 연출 1.
            _victoryRoot.SetActive(true);
            
            switch (_battleType)
            {
                case BattleModeType.StoryMode:
                case BattleModeType.AdventureMode:
                    {
                        _readerName.text = $"{User.My.ReaderInfo.ReaderName}";
                        _battleElapsedTime.text = $"{TimeSpan.FromSeconds(_resultData.ResStage.CloseSec)}";
                        _readerLevel.text = $"{_resultData.ExpReader.ResLevel - _resultData.ExpReader.IncLevel}";
                        _readerExpEarned.text = $"+{_resultData.ExpReader.IncExp:N0}";
                        
                        _victoryPve.SetActive(true);
                        _victoryPvp.SetActive(false);
                        
                        //await UniTask.Delay((int)(_victoryPveTween.GetTweensFullTime("Active") * 1000), DelayType.UnscaledDeltaTime);
                        
                        await LoadPartyCharacters(_partyType, _resultData.ExpChars.ToList());
                    }
                    break;
                case BattleModeType.ArenaMode:
                    {
                        _victoryPve.SetActive(false);
                        _victoryPvp.SetActive(true);

                        await LoadPartyCharactersPvp(_partyType);
                    }
                    break;
            }

            // 연출 2.
            _battleMvpPositionRoot.SetActive(true);
            _battleMvpPositionTween.ActivateState("MvPStayPositionAnim");

            await UniTask.Delay((int)(_battleMvpPositionTween.GetTweensFullTime("MvPStayPositionAnim") * 1000));

            if (_battleType == BattleModeType.StoryMode || _battleType == BattleModeType.AdventureMode)
            {
                // 연출 3.
                _accountExpRoot.SetActive(true);

                ReaderExpDirection().Forget();
                
                // 연출 4.
                if (_resultData.ExpReader.IncLevel > 0)
                {
                    MessageService.Instance.Publish(ProductChangeTypeEvent.Create(ProductionSlot.ProductionType.Energy, ProductionSlot.ProductionType.Energy));
                
                    await UIManager.Instance.OverlayStack.PushAndWaitForClose(UILeaderLevelUpResultOverlay.CreateContext(new UILeaderLevelUpResultOverlay.State
                    {
                        BeforeLevel = _resultData.ExpReader.ResLevel - _resultData.ExpReader.IncLevel,
                        AfterLevel = _resultData.ExpReader.ResLevel
                    }));
                }
                
                // 연출 5.
                int finishExpDirection = 0;
                for (int i = 0; i < _partyCharacters.Count; i++)
                {
                    if (_partyCharacters[i].gameObject.IsActiveInHierarchy())
                    {
                        _partyCharacters[i].Set(() => { finishExpDirection--; }).Forget();
                        finishExpDirection++;
                    }
                }
                
                await UniTask.WaitUntil(() => finishExpDirection == 0);
                
                // 연출 6.
                _nextTween.ActivateState("NextButton");
            }
            else
            {
                SetSecondDirection().Forget();
            }
        }

        // 하이브 QA 기간에 수정 요청.
        // 8번 단계를 7번 연출이 끝나길 기다렸다가 동작하도록 변경해달라는 요청.
        // 8번 단계를 9번까지 끝내고 동작하게 해달라는 요청은 보상창보다 버튼을 먼저 누를 가능성이 있어 거절함.
        private async UniTask SetSecondDirection()
        {
            for (int i = 0; i < _partyCharacters.Count; i++)
            {
                if (_partyCharacters[i].gameObject.IsActiveInHierarchy())
                {
                    _partyCharacters[i].SetOffLevelUpSlider(false);
                }
            }

            // 연출 7.
            for (int i = 0; i < _mvpCharacterBg.Length; i++)
            {
                _mvpCharacterBg[i].SetActive(true);
            }
            _accountExpRoot.SetActive(false);
            _mvpCharacterRoot.SetActive(true);
            //_characterOutTween.ActivateState("MvpTween");
            _mvpCharacterTween.ActivateState("MvpMove");

            switch (_battleType)
            {
                case BattleModeType.StoryMode:
                case BattleModeType.AdventureMode:
                    {
                        _arenaBattleResul.gameObject.SetActive(false);
                        
                        _adventureBattleResult.Set(_players, _resultData);
                        _adventureBattleResult.gameObject.SetActive(true);
                    }
                    break;

                case BattleModeType.ArenaMode:
                    {
                        _adventureBattleResult.gameObject.SetActive(false);

                        _arenaBattleResul.ArenaData = _arenaData;
                        _arenaBattleResul.Party = User.My.PartyInfo.GetPartyCharactersId(_partyType);
                        //_arenaBattleResul.Mvp = _mvpCharacter?.Base.Id;
                        _arenaBattleResul.gameObject.SetActive(true);
                    }
                    break;
            }
            
            _mvpCharacterInfo.SetActive(true);

            if (_battleType == BattleModeType.StoryMode || _battleType == BattleModeType.AdventureMode)
            {
                // FIXME: TweenManager가 Delay 까지 포함해 트윈시간을 계산해준다면 대체할 수 있다.
                await UniTask.Delay((int)(_adventureBattleResult.TotalTweenDuration * 1000));
                
                // 연출 8.
                await UIManager.Instance.OverlayStack.PushAndWaitForClose(UIRewardPopup.CreateContext(new UIRewardPopup.State
                {
                    Title = LocalString.Get("Str_UI_RewardGet"),
                    RewardList = _resultData.ResCommon.Rewards.ToList(),
                }));
                
                // 연출 9.
                _pveButtons.SetActive(true);

                // if (AdventureModeUtil.IsClearAllStageInChapter(User.My.AdventureModeInfo.GetSelectedAdventureStage().AdventureChapter))
                // {
                //     if (AdventureModeUtil.GetOpenNextChapterData(DataTable.AdventureChapterDataTable[User.My.AdventureModeInfo.GetSelectedAdventureStage().AdventureChapter]) != null)
                //     {
                //         _nextStageButton.SetActive(true);
                //     }
                //     else
                //     {
                //         _nextStageButton.SetActive(false);
                //     }
                // }
                // else
                // {
                //     _nextStageButton.SetActive(true);
                // }

                _nextStageButton.SetActive(
                    AdventureModeUtil.IsLastStageInChapter(
                        User.My.AdventureModeInfo.GetSelectedAdventureStage().Id,
                        User.My.AdventureModeInfo.GetSelectedAdventureStage().AdventureChapter) == false);
            }
            else
            {
                await UniTask.Delay((int)(_characterOutTween.GetTweensFullTime("MvpTween") * 1000));
                
                // 연출 9.
                _pvpButtons.SetActive(true);
            }
        }

        public BattleModeType Type
        {
            set
            {
                _battleType = value;

                // _completeQuest = QuestManager.Instance.IsCompleteMainQuest();

                AdventureModeUtil.CalcNextStage();

                _partyType = (_battleType == BattleModeType.StoryMode || _battleType == BattleModeType.AdventureMode) ? BattleModePartyType.AdventureModeParty : BattleModePartyType.ArenaModeAttackParty;
                // 스토리모드에서 MVP가 생긴다면 이곳에서 뭔가 해줘야 하긴 할듯??

                List<string> partyIds = User.My.PartyInfo.GetPartyCharactersId(_partyType);
                if (_mvpActor == null && _players?.Count() > 0)
                {
                    _mvpActor = _players.First();
                }

                if (_mvpActor != null)
                {
                    // mvp 셋팅
                    _mvpCharacter = ((Character)_mvpActor).CharData;
                    _nameText.text = LocalString.Get(_mvpCharacter.Base.StrCharacterName);
                    _titleText.text = LocalString.Get(_mvpCharacter.Base.StrCharacterTitle);
                }
            }
        }

        private async UniTask ShowBottomButtons(float delay)
        {
            await UniTask.Delay((int)(delay * 1000));
            
            if (_resultData.ResCommon.Rewards.Count > 0)
            {
                await UIManager.Instance.OverlayStack.PushAndWaitForClose(UIRewardPopup.CreateContext(new UIRewardPopup.State
                {
                    Title = LocalString.Get("Str_UI_RewardGet"),
                    RewardList = _resultData.ResCommon.Rewards.ToList(),
                }));
            }
            
            _pveButtons.SetActive(true);
        }

        private void EnterCallBack()
        {
            _enterClallBack?.Invoke();
        }

        public async UniTask LoadPartyCharacters(BattleModePartyType partyType, List<KeyValuePair<string, OZResExp>> partyExpList)
        {
            if (_mvpCharacter == null)
                return;
            
            List<string> partyIds = User.My.PartyInfo.GetPartyCharactersId(partyType).OrderByDescending(x => x == _mvpCharacter.Base.Id).ToList();

            for (int i = 0; i < _partyCharacters.Count; ++i)
            {
                if (i >= partyIds.Count())
                {
                    _partyCharacters[i].gameObject.SetActive(false);
                }
                else
                {
                    _partyCharacters[i].gameObject.SetActive(true);

                    OZResExp characterExp = null;
                    for (int j = 0; j < partyExpList.Count; j++)
                    {
                        if (string.Equals(partyExpList[j].Key, partyIds[i], StringComparison.OrdinalIgnoreCase))
                        {
                            characterExp = partyExpList[j].Value;
                            break;
                        }
                    }
                    
                    await _partyCharacters[i].Load(User.My.CharacterInfo[partyIds[i]], characterExp);
                }
            }
        }
        public async UniTask LoadPartyCharactersPvp(BattleModePartyType partyType)
        {
            if (_mvpCharacter == null)
                return;

            List<string> partyIds = User.My.PartyInfo.GetPartyCharactersId(partyType).OrderByDescending(x => x == _mvpCharacter.Base.Id).ToList();

            for (int i = 0; i < _partyCharacters.Count; ++i)
            {
                if (i >= partyIds.Count())
                {
                    _partyCharacters[i].gameObject.SetActive(false);
                }
                else
                {
                    _partyCharacters[i].gameObject.SetActive(true);

                    await _partyCharacters[i].Load(User.My.CharacterInfo[partyIds[i]]);
                }
            }
        }

        private async UniTask OnOut()
        {
            Time.timeScale = 1;
            var request = new Network.Packets.Game.Arena.ArenaMatchingUsersPacket(User.My.ArenaInfo.arenabotID);
            var response = await Network.PacketProcessor.Instance.SendRequestAsync(request);
            SetNextStage();
            UiDefine.StageClearMove(UiDefine.EStageClearType.StageKingdom);
        }

        public void OnClickOutPve()
        {
            ReloadLobby();
            //OnOut().Forget();
        }

        public void OnClickOutPvp()
        {
            GameModeManager.Instance.Transit(LobbyGameMode.CreateContext()
                .AddUIPanel(UIAdventureDashboardPanel.CreateContext(new ()
                {
                    
                })));
        }

        public void OnRetry()
        {
            // UIManager.Instance.OverlayStack.Push(UISystemMessagePopup.CreateContext(new()
            // {
            //     type = UISystemMessagePopup.Type.Yes_No,
            //     TitleString = LocalString.Get("Str_UI_Guide"),
            //     InfoString = string.Format(LocalString.Get("Str_UI_BattleRetry_Popup_Info"), User.My.AdventureModeInfo.SelectedAdventureStageData.TicketCount),
            //     YesString = LocalString.Get("Str_UI_Button_Confirm"),
            //     NoString = LocalString.Get("Str_UI_Button_Cancel"),
            //     YesAction = () => 
            //     {
            //         UiDefine.StageClearMove(UiDefine.EStageClearType.StageReStart);
            //     },
            //     NoAction = () => 
            //     {
            //
            //     },
            // }));
            
            GameModeManager.Instance.Transit(LobbyGameMode.CreateContext()
                .AddUIPanel(UIAdventureDashboardPanel.CreateContext(new ()
                {
                    
                }))
                .AddUIPanel(UIStageSelectionPanel.CreateContext(new ()
                {
                    AdventureChapterData = DataTable.AdventureChapterDataTable[User.My.AdventureModeInfo.GetSelectedAdventureStage().AdventureChapter],
                    AdventureStageData = User.My.AdventureModeInfo.GetSelectedAdventureStage()
                })));
        }
        
        public void OnNextStage()
        {
            //UiDefine.StageClearMove(UiDefine.EStageClearType.StageNextStage);
            
            Time.timeScale = 1;
            
            User.My.AdventureModeInfo.SetSelectedAdventureStage(User.My.AdventureModeInfo.NextStage.Id);
            
            GameModeManager.Instance.Transit(LobbyGameMode.CreateContext()
                .AddUIPanel(UIAdventureDashboardPanel.CreateContext(new ()
                {
                    
                }))
                .AddUIPanel(UIStageSelectionPanel.CreateContext(new ()
                {
                    AdventureChapterData = DataTable.AdventureChapterDataTable[User.My.AdventureModeInfo.GetSelectedAdventureStage().AdventureChapter],
                    AdventureStageData = User.My.AdventureModeInfo.GetSelectedAdventureStage()
                })));
        }

        private void ReloadLobby()
        {
            Time.timeScale = 1;
            
            GameModeManager.Instance.Transit(LobbyGameMode.CreateContext()
                .AddUIPanel(UIAdventureDashboardPanel.CreateContext(new ()
                {
                    
                }))
                .AddUIPanel(UIStageSelectionPanel.CreateContext(new ()
                {
                    AdventureChapterData = DataTable.AdventureChapterDataTable[User.My.AdventureModeInfo.GetSelectedAdventureStage().AdventureChapter]
                })));
        }

        public void OnSelectStage()
        {
            SoundManager.Instance.Stop(Oz.SoundChannelType.BattleSfx);
            
            if (_completeQuest == false)
                UiDefine.StageClearMove(UiDefine.EStageClearType.StageSelect);
            else
                UiDefine.StageClearMove(UiDefine.EStageClearType.StageKingdom);
        }

        public void OnClickArena()
        {
            Network.PacketProcessor.Instance.SendRequest(
                new Network.Packets.Game.Arena.ArenaMatchingUsersPacket(User.My.ArenaInfo.arenabotID)
                    .OnCompleted(response =>
                    {
                        UIManager.Instance.PanelStack.Push(UIArenaMatchingPanel.CreateContext(new() { Matchings = response.Users }));
                        UIManager.Instance.OverlayStack.PushAsync(UINavigationOverlay.CreateContext()).Forget();
                        MessageService.Instance.Publish(UINavigationShowRequest.Create());
                    }));
        }

        public void OnReport()
        {
            UIManager.Instance.OverlayStack.Push(UIBattleReportOverlay.CreateContext(new()
            {
                Actors = _players,
                MvpActor = _mvpActor,
                PartyType = _partyType,
            }));
        }

        private void SetNextStage()
        {
            if (_battleType == BattleModeType.StoryMode || _battleType == BattleModeType.AdventureMode)
            {
                AdventureModeUtil.CalcNextStage();
            }
        }

        public void SetMvpAni(string ani)
        {
            if (_motionMvpCharacter == null)
                return;

            _motionMvpCharacter.SetAni(0, ani);
        }

        public void ClearMvpMotionChar()
        {
            GlobalUtils.ClearParnet(_motionMvpRoot);
        }

        private void PlayWaveClearCharacterVoice()
        {
            // MVP가 보이스가 있다면
            var getVoiceID = DataTable.VoiceDataTable.GetRandomTargetVoiceID("BattleVictory", _mvpActor.CharData.Base.Id);
            if (string.IsNullOrEmpty(getVoiceID) == false)
            {
                SoundManager.Instance.Play(getVoiceID);
            }
            else
            {
                var partyIDs = _players.Select(x => x.CharData.Base.Id).ToList();
                getVoiceID = DataTable.VoiceDataTable.GetRandomTargetVoiceID("BattleVictory", partyIDs);

                if (string.IsNullOrEmpty(getVoiceID) == false)
                    SoundManager.Instance.Play(getVoiceID);
            }
        }

        public void OnClickNext()
        {
            SetSecondDirection().Forget();
        }
        
        private async UniTask ReaderExpDirection()
        {
            // TODO: 로나님과 상의.
            float totalDirectingTime = 1.0f;
            float elapsedTime = 0.0f;

            // 레벨증가.
            if (_resultData.ExpReader.IncLevel > 0)
            {
                int previousLevel = _resultData.ExpReader.ResLevel - _resultData.ExpReader.IncLevel;
                long previousAccumExp = _resultData.ExpReader.ResExp - _resultData.ExpReader.IncExp;
                long previousNeedExp = previousAccumExp - DataTable.LevelExpDataTable[previousLevel].ReaderLvAccumExp;
                long currentNeedExp = DataTable.LevelExpDataTable[previousLevel + 1].ReaderLvNeedExp;
                int levelNeedExp = DataTable.LevelExpDataTable[previousLevel + 1].ReaderLvNeedExp;

                _readerCurrentExpText.text = $"{previousNeedExp:N0}";
                _readerNeedExpText.text = $"{levelNeedExp:N0}";
                _readerExpEarned.text = $"+{_resultData.ExpReader.IncExp:N0}";
                _readerLevel.text = $"{previousLevel}";
                
                while (previousLevel <= _resultData.ExpReader.ResLevel)
                {
                    while (elapsedTime < totalDirectingTime / _resultData.ExpReader.IncLevel)
                    {
                        elapsedTime += Time.deltaTime;

                        _readerExpSlider.value =
                            Mathf.Lerp(previousNeedExp, currentNeedExp, elapsedTime / totalDirectingTime * _resultData.ExpReader.IncLevel) /
                            levelNeedExp;

                        await UniTask.Yield();
                    }

                    elapsedTime = 0.0f;
                    previousLevel++;
                    previousNeedExp = 0;

                    if (previousLevel > _resultData.ExpReader.ResLevel)
                    {
                        return;
                    }

                    if (previousLevel == _resultData.ExpReader.ResLevel)
                    {
                        currentNeedExp = _resultData.ExpReader.ResExp - DataTable.LevelExpDataTable[_resultData.ExpReader.ResLevel].ReaderLvAccumExp;

                        levelNeedExp = DataTable.LevelExpDataTable[previousLevel + 1].ReaderLvNeedExp;
                        
                        _readerCurrentExpText.text = $"{currentNeedExp:N0}";
                        _readerNeedExpText.text = $"{levelNeedExp:N0}";           
                        _readerLevel.text = $"{_resultData.ExpReader.ResLevel}";
                    }
                    else
                    {
                        currentNeedExp = DataTable.LevelExpDataTable[previousLevel + 1].ReaderLvNeedExp;
                        
                        levelNeedExp = DataTable.LevelExpDataTable[previousLevel + 1].ReaderLvNeedExp;
                        
                        _readerCurrentExpText.text = $"{currentNeedExp:N0}";
                        _readerNeedExpText.text = $"{levelNeedExp:N0}";           
                        _readerLevel.text = $"{_resultData.ExpReader.ResLevel}";
                    }

                    await UniTask.Yield();
                }
            }
            else
            {
                _readerLevel.text = $"{_resultData.ExpReader.ResLevel}";
                
                // 만렙.
                if (DataTable.LevelExpDataTable.LastOrDefault().Level == _resultData.ExpReader.ResLevel)
                {
                    _readerExpSlider.value = 1.0f;
                    
                    int maxLevelExp = DataTable.LevelExpDataTable.LastOrDefault().ReaderLvNeedExp;

                    _readerCurrentExpText.text = $"{maxLevelExp:N0}";
                    _readerNeedExpText.text = $"{maxLevelExp:N0}";

                    _readerExpEarned.text = string.Empty;

                    // FIXME: 대기가 필요할까?
                    await UniTask.Delay((int)(totalDirectingTime * 1000));
                }
                // 경험치만 증가.
                else
                {
                    long currentNeedExp = _resultData.ExpReader.ResExp - DataTable.LevelExpDataTable[_resultData.ExpReader.ResLevel].ReaderLvAccumExp;
                    long previousAccumExp = currentNeedExp - _resultData.ExpReader.IncExp;
                    int levelNeedExp = DataTable.LevelExpDataTable[_resultData.ExpReader.ResLevel + 1].ReaderLvNeedExp;

                    _readerCurrentExpText.text = $"{currentNeedExp:N0}";
                    _readerNeedExpText.text = $"{levelNeedExp:N0}";
                    
                    _readerExpEarned.text = $"+{_resultData.ExpReader.IncExp:N0}";

                    while (elapsedTime < totalDirectingTime)
                    {
                        elapsedTime += Time.deltaTime;

                        _readerExpSlider.value =
                            Mathf.Lerp(previousAccumExp, currentNeedExp, elapsedTime / totalDirectingTime) /
                            levelNeedExp;

                        await UniTask.Yield();
                    }
                }
            }
        }

        public struct State
        {
            public BattleModeType GameType;
            public StageClearAns Stage;
            public OZArena Arena;
            public BaseActor MvpActor;
            public IEnumerable<BaseActor> Actors;
            public Action EnterCallBack;
        }
    }
}