using Cysharp.Threading.Tasks;
using Data;
using Managers;
using MessageSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Messages;
using UI.Overlay;
using UIStyle;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Users;

namespace UI.Panel
{
    public class UIStageSelectionPanel : UIPanel<UIStageSelectionPanel, UIStageSelectionPanel.State>
    {
        [SerializeField]
        private TextMeshProUGUI _chapterTitle;

        [SerializeField]
        private TextMeshProUGUI _partNumber;

        [SerializeField]
        private UIStyles _uiStyleDifficulty;

        // TODO: 요것도 UIStyle로?
        [SerializeField]
        private GameObject _currentStarRoot;
        [SerializeField]
        private TextMeshProUGUI _currentStar;
        [SerializeField]
        private GameObject _maxStarRoot;
        [SerializeField]
        private TextMeshProUGUI _maxStar;
        [SerializeField]
        private TextMeshProUGUI _totalStar;
        [SerializeField]
        private Slider _starSlide;
        [SerializeField]
        private GameObject _maxSlide;

        [SerializeField]
        private Button _availableReward;
        [SerializeField]
        private Button _disAvailableReward;

        [SerializeField]
        private GameObject _previousChapterButton;
        [SerializeField]
        private GameObject _nextChapterButton;

        private AdventureChapterData _adventureChapterData;
        private AdventureStageData _adventureStageData;
        private List<AdventureStageData> _adventureStageList;
        private List<StageMapSpot> _stageMapSpots;
        private StageMapController _stageMapController;
        
        private GameObject _stageMap;

        // 서버에서 받은 플레이 기록
        private Dictionary<string, OZAdventure> _playedAdventureStages = new();

        public override async UniTask OnEnter(State state)
        {
            MessageService.Instance.Subscribe<UISelectPanelBattleStartRequest>(OnBattleStart);
            MessageService.Instance.Subscribe<UIPartSelectRequest>(OnChangePart);
            MessageService.Instance.Subscribe<AdventureChapterRewardEvent>(OnRefreshChapterRewardInfo);
            MessageService.Instance.Subscribe<AdventureChapterGetEvent>(OnRefreshAdventureStagesData);
            MessageService.Instance.Subscribe<AdventureHardResetEvent>(OnRefreshAdventureStageData);
            MessageService.Instance.Subscribe<AdventureModUpEvent>(OnRefreshModUpData);
            
            _adventureChapterData = state.AdventureChapterData;
            _adventureStageData = state.AdventureStageData;

            _adventureStageList = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(_adventureChapterData.Id);

            await FadeLoading.ProcessAsync(async () =>
            {
                await Network.PacketProcessor.Instance.SendRequestAsync(new Network.Packets.Game.Adventure.AdventureChapterGetPacket(_adventureChapterData.Id));
                
                await LoadStageMap(_adventureChapterData);
                
                UpdatePanel();
            });

            if (_adventureStageData != null)
            {
                UIManager.Instance.OverlayStack.Push(UIAdventureReadyPopup.CreateContext(new UIAdventureReadyPopup.State()
                {
                    AdventureStageData = _adventureStageData,
                    AdventureChapterData = _adventureChapterData,
                    AvailableSpotClick = SetInteractionStageMap,
                    PlayedAdventureStages = _playedAdventureStages
                }));
            }
        }
        
        public override async UniTask OnExit()
        {
            MessageService.Instance.Unsubscribe<UISelectPanelBattleStartRequest>(OnBattleStart);
            MessageService.Instance.Unsubscribe<UIPartSelectRequest>(OnChangePart);
            MessageService.Instance.Unsubscribe<AdventureChapterRewardEvent>(OnRefreshChapterRewardInfo);
            MessageService.Instance.Unsubscribe<AdventureChapterGetEvent>(OnRefreshAdventureStagesData);
            MessageService.Instance.Unsubscribe<AdventureHardResetEvent>(OnRefreshAdventureStageData);
            MessageService.Instance.Unsubscribe<AdventureModUpEvent>(OnRefreshModUpData);
            
            _adventureChapterData = null;
            _adventureStageData = null;
            _stageMapController = null;
            
            _playedAdventureStages.Clear();

            UnLoadStageMap();
        }

        #region 업데이트 로직
        private async UniTask UpdateData(AdventureChapterData adventureChapterData = null)
        {
            _playedAdventureStages.Clear();
            
            _adventureChapterData = adventureChapterData ?? DataTable.AdventureChapterDataTable[User.My.AdventureModeInfo.NextStage.AdventureChapter];

            _adventureStageList = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(_adventureChapterData.Id);
            
            await Network.PacketProcessor.Instance.SendRequestAsync(new Network.Packets.Game.Adventure.AdventureChapterGetPacket(_adventureChapterData.Id));
        }

        private void UpdatePanel()
        {
            _chapterTitle.text = string.Format(LocalString.Get("Str_UI_AdventureMode_ChapterTitle"),
                 _adventureChapterData.ChapterNumber, LocalString.Get(_adventureChapterData.ChapterName));

            _partNumber.text = $"{DataTable.AdventurePartDataTable[_adventureChapterData.AdventurePart].PartNumber}";

            CalcChangeDifficulty();

            RefreshChapterRewardInfo();

            RefreshChapterButton();
            
            SoundManager.Instance.Play(_adventureChapterData.Bgm);
        }

        private void UpdateSpot(List<StageMapSpot> spots, bool byPartSelect = false)
        {
            if (_adventureStageList.Count != spots.Count)
            {
#if UNITY_EDITOR
                DebugHelper.LogError("Check AdventureStage count & StageMapSpot count");
#endif
                return;
            }

            for (int i = 0; i < spots.Count; i++)
            {
                if (string.IsNullOrEmpty(_adventureStageList[i].OpenConditionValue))
                {
                    spots[i].SetStageMapSpot(false,
                        _stageMapController.OnClickStageSpot,
                        _adventureStageList[i],
                        AdventureModeUtil.GetPlayedAdventureStage(_playedAdventureStages, _adventureStageList[i].Id));
                }
                else
                {
                    if (AdventureModeUtil.IsPlayedStage(_adventureStageList[i].OpenConditionValue))
                    {
                        spots[i].SetStageMapSpot(false,
                            _stageMapController.OnClickStageSpot,
                            _adventureStageList[i],
                            AdventureModeUtil.GetPlayedAdventureStage(_playedAdventureStages, _adventureStageList[i].Id));
                    }
                    else
                    {
                        spots[i].SetStageMapSpot(true, null, _adventureStageList[i], null);
                    }
                }
            }

            if (byPartSelect == true)
            {
                return;
            }

            int lockCount = spots.Sum(t => t.Lock ? 1 : 0);

            if (lockCount == spots.Count - 1)
            {
                spots[0].UnLockEffect().Forget();
            }
        }
                
        private void CalcChangeDifficulty()
        {
            switch (_adventureChapterData.ChapterDifficultType)
            {
                case ChapterDifficultType.Hard:
                    {
                        _uiStyleDifficulty.SetStyle("Hard_On");
                    }
                    break;
                case ChapterDifficultType.None:
                case ChapterDifficultType.Normal:
                case ChapterDifficultType.Max:
                    {
                        _uiStyleDifficulty.SetStyle(AdventureModeUtil.IsClearAllStageInChapter(_playedAdventureStages, _adventureChapterData.Id) ?
                            "Normal_On" : "Hard_Lock");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RefreshChapterRewardInfo()
        {
            int currentStar = AdventureModeUtil.GetStagesMissionStar(_playedAdventureStages);
            _currentStar.text = $"{currentStar}";

            int totalCount = AdventureModeUtil.GetTotalMissionStar(_adventureChapterData.Id);
            _maxStar.text = _totalStar.text = $"{totalCount}";
            
            _starSlide.value = currentStar  * 1.0f / totalCount;
            
            _currentStarRoot.SetActive(currentStar != totalCount);
            _maxStarRoot.SetActive(currentStar == totalCount);
            
            _maxSlide.SetActive(currentStar == totalCount);

            _availableReward.gameObject.SetActive(true);
            _disAvailableReward.gameObject.SetActive(false);
        }

        private void RefreshChapterButton()
        {
            // 클리어 한 챕터만 표시
            _previousChapterButton.SetActive(
                AdventureModeUtil.GetOpenPreviousChapterData(_adventureChapterData) != null);
            _nextChapterButton.SetActive(AdventureModeUtil.GetOpenNextChapterData(_adventureChapterData) != null);
        }
        #endregion
        
        #region 맵 로딩
        private async UniTask LoadStageMap(AdventureChapterData adventureChapterData, bool byPartSelect = false)
        {
            UnLoadStageMap();

            // HACK: 하드맵 제작은 우선 1개만 하고 홀딩하는걸로. 모험모드는 스테이지맵을 안쓸지도 모르는 상태?
            //_stageMap = await Addressables.InstantiateAsync(adventureChapterData.ChapterMapPath);
            _stageMap = await Addressables.InstantiateAsync("Assets/Data/Environment/StageMap/Cellarn01/Cellarn01_StageMap.prefab");

            await UniTask.Yield();

            _stageMapController = _stageMap.GetComponent<StageMapController>();
            _stageMapController.SetDifficulty(adventureChapterData.ChapterDifficultType);
            _stageMapController.StageMapName = adventureChapterData.ChapterMapPath;
            _stageMapController.SetMapPosition(Vector2.right * 10000.0f);

            if (_adventureStageData != null)
            {
                _stageMapController.SetCharacterPosition(
                    _adventureStageList.FindIndex(x => x.Id == _adventureStageData.Id));
            }
            else
            {
                AdventureModeUtil.SetHighestStageInChapter(adventureChapterData.Id);

                if (AdventureModeUtil.IsClearAllStageInChapter(adventureChapterData.Id))
                {
                    _stageMapController.SetCharacterPosition(_adventureStageList.Count - 1);
                }
                else
                {
                    _stageMapController.SetCharacterPosition(
                        _adventureStageList.FindIndex(x => x.Id == User.My.AdventureModeInfo.GetSelectedAdventureStage().Id));
                }
            }

            UpdateSpot(_stageMapController.Stages, byPartSelect);

            _stageMap.SetActive(true);
        }
        private void UnLoadStageMap()
        {
            if (_stageMap == null)
            {
                return;
            }

            Addressables.Release(_stageMap);

            _stageMap = null;
        }
        #endregion

        #region 버튼액션
        public void OnClickPartChange()
        {
            UIManager.Instance.OverlayStack.Push(UIAdventurePartSelectOverlay.CreateContext(new UIAdventurePartSelectOverlay.State()
            {
                AdventureChapterData = _adventureChapterData,
            }));
        }
        public void OnClickNormal()
        {
            if (_adventureChapterData.ChapterDifficultType == ChapterDifficultType.Normal)
            {
                return;
            }

            var normalChapterData = DataTable.AdventureChapterDataTable.GetNormalChapterData(_adventureChapterData.ChapterNumber);
            if (normalChapterData == null)
            {
                DebugHelper.Log($"해당 챕터넘버의 노말모드 데이터를 찾지 못했습니다. 챕터넘버 : {_adventureChapterData.ChapterNumber}");
                
                return;
            }
            
            FadeLoading.Process(async () =>
            {
                await UpdateData(normalChapterData);

                await LoadStageMap(normalChapterData);
                
                UpdatePanel();
            });
        }
        public void OnClickHard()
        {
            if (_adventureChapterData.ChapterDifficultType == ChapterDifficultType.Hard)
            {
                return;
            }

            var hardChapterData = DataTable.AdventureChapterDataTable.GetHardChapterData(_adventureChapterData.ChapterNumber);
            if (hardChapterData == null)
            {
                DebugHelper.Log($"해당 챕터넘버의 하드모드 데이터를 찾지 못했습니다. 챕터넘버 : {_adventureChapterData.ChapterNumber}");
                
                return;
            }

            if (AdventureModeUtil.IsClearAllStageInChapter(_adventureChapterData.Id) == false)
            {
                return;
            }
            
            FadeLoading.Process(async () =>
            {
                await UpdateData(hardChapterData);

                await LoadStageMap(hardChapterData);
                
                UpdatePanel();
            });
        }
        public void OnClickReward()
        {
            UIManager.Instance.OverlayStack.Push(UIAdventureMissionRewardPopup.CreateContext(new()
            {
                AdventureChapterData = _adventureChapterData,
                PlayedAdventureStages = _playedAdventureStages
            }));
        }
        public void OnClickPreviousChapter()
        {
            FadeLoading.Process(async () =>
            {
                await UpdateData(DataTable.AdventureChapterDataTable.GetPreviousChapterData(_adventureChapterData));
                
                await LoadStageMap(_adventureChapterData);

                UpdatePanel();
            });
        }
        public void OnClickNextChapter()
        {
            FadeLoading.Process(async () =>
            {
                await UpdateData(DataTable.AdventureChapterDataTable.GetNextChapterData(_adventureChapterData));
                
                await LoadStageMap(_adventureChapterData);

                UpdatePanel();
            });
        }
        #endregion

        #region 메세지
        private bool OnBattleStart(UISelectPanelBattleStartRequest msg)
        {
            UIManager.Instance.OverlayStack.Push(UIAdventureReadyPopup.CreateContext(new UIAdventureReadyPopup.State()
            {
                AdventureStageData = _adventureStageList[msg.Index],
                AdventureChapterData = _adventureChapterData,
                AvailableSpotClick = msg.FinishAction,
                PlayedAdventureStages = _playedAdventureStages
            }));

            return true;
        }

        private bool OnChangePart(UIPartSelectRequest msg)
        {
            if (_adventureChapterData == msg.AdventureChapterData)
            {
                return false;
            }

            FadeLoading.Process(async () =>
            {
                await UpdateData(msg.AdventureChapterData);
                
                await LoadStageMap(_adventureChapterData, true);

                UpdatePanel();
            });
            
            return true;
        }

        private bool OnRefreshChapterRewardInfo(AdventureChapterRewardEvent msg)
        {
            RefreshChapterRewardInfo();
            
            UIManager.Instance.OverlayStack.Push(UIRewardPopup.CreateContext(new UIRewardPopup.State
            {
                Title = LocalString.Get("Str_UI_RewardGet"),
                RewardList = msg.RewardList.ToList(),
            }));

            return true;
        }

        private bool OnRefreshAdventureStagesData(AdventureChapterGetEvent msg)
        {
            _playedAdventureStages.Clear();
            
            foreach (var playedAdventureStage in msg.PlayedAdventureStages)
            {
                if (_playedAdventureStages.ContainsKey(playedAdventureStage.Value.Id))
                {
                    _playedAdventureStages[playedAdventureStage.Value.Id] = playedAdventureStage.Value;
                }
                else
                {
                    _playedAdventureStages.Add(playedAdventureStage.Value.Id, playedAdventureStage.Value);   
                }
            }

            return true;
        }

        private bool OnRefreshAdventureStageData(AdventureHardResetEvent msg)
        {
            if (_playedAdventureStages.ContainsKey(msg.PlayedAdventureStage.Id))
            {
                _playedAdventureStages[msg.PlayedAdventureStage.Id] = msg.PlayedAdventureStage;
            }

            return true;
        }
        
        private bool OnRefreshModUpData(AdventureModUpEvent msg)
        {
            if (_playedAdventureStages.ContainsKey(msg.PlayedAdventureStage.Id))
            {
                _playedAdventureStages[msg.PlayedAdventureStage.Id] = msg.PlayedAdventureStage;
                
                RefreshChapterRewardInfo();
                
                UpdateSpot(_stageMapController.Stages);
            }

            return true;
        }
        #endregion

        public void SetInteractionStageMap(bool interaction)
        {
            _stageMapController.SetInteraction(interaction);
        }
        
        public struct State
        {
            public AdventureChapterData AdventureChapterData;
            public AdventureStageData AdventureStageData;
        }
    }
}