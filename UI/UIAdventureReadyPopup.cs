using Cysharp.Threading.Tasks;
using Data;
using Managers;
using MessageSystem;
using Network;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Messages;
using UI.Thing;
using UI.Panel;
using UIStyle;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Users;
using Image = UnityEngine.UI.Image;

namespace UI.Overlay
{
    public class UIAdventureReadyPopup : UIPopup<UIAdventureReadyPopup, UIAdventureReadyPopup.State>
    {
        [SerializeField]
        private Image _stageMapImage;

        [SerializeField]
        private TextMeshProUGUI _stageName;

        [SerializeField]
        private UIStyles _difficultStyles;

        [SerializeField]
        private TextMeshProUGUI _recommendLevel;

        [SerializeField]
        private UIStarMissionListItem[] _starMissionListItems;
        
        [SerializeField]
        private LoopScrollRect _dropItemScrollRect;

        [SerializeField]
        private Image _modUpTicketImage;
        [SerializeField]
        private TextMeshProUGUI _currentModUpTicket;
        [SerializeField]
        private TextMeshProUGUI _selectedModUpTicket;

        [SerializeField]
        private GameObject _normalModUpRoot;
        [SerializeField]
        private TextMeshProUGUI _currentNormalModUpAp;
        [SerializeField]
        private TextMeshProUGUI _changeNormalModUpAp;
        [SerializeField]
        private TextMeshProUGUI _currentNormalBattleAp;
        [SerializeField]
        private TextMeshProUGUI _changeNormalBattleAp;
        
        [SerializeField]
        private GameObject _hardModUpRoot;
        [SerializeField]
        private TextMeshProUGUI _currentHardModUpAp;
        [SerializeField]
        private TextMeshProUGUI _changeHardModUpAp;
        [SerializeField]
        private TextMeshProUGUI _hardModUpLimitCount;
        [SerializeField]
        private TextMeshProUGUI _currentHardBattleAp;
        [SerializeField]
        private TextMeshProUGUI _changeHardBattleAp;
        [SerializeField]
        private TextMeshProUGUI _hardBattleLimitCount;

        [SerializeField]
        private Button _disableModUpButton;

        [SerializeField]
        private Button _disableReadyBattleButton;
        [SerializeField]
        private TextMeshProUGUI _disableReadyBattleText;

        [SerializeField]
        private GameObject _disableModUp;

        [SerializeField]
        private Button _previousStageArrow;
        [SerializeField]
        private Button _nextStageArrow;

        private List<DropItemData> _dropItems = new();
        private int _selectedModUpCount = 1;
        private long _currentModUpTicketCount;
        private int _hardBattleLimit;
        
        private AdventureChapterData _adventureChapterData;
        private AdventureStageData _adventureStageData;

        // FIXME: 스테이지 스팟 중복클릭 방지
        private Action<bool> _availableSpotClick;
        
        // 서버에서 받은 플레이 기록
        private Dictionary<string, OZAdventure> _playedAdventureStages;
        private OZAdventure _selectedStageData;

        public override async UniTask OnEnter(State state)
        {
            _adventureStageData = state.AdventureStageData;
            _adventureChapterData = state.AdventureChapterData;
            _availableSpotClick = state.AvailableSpotClick;
            _playedAdventureStages = state.PlayedAdventureStages;
            
            MessageService.Instance.Subscribe<AdventureHardResetEvent>(OnRefreshAdventureStageData);

            RefreshPopup();
        }

        public override async UniTask OnExit()
        {
            MessageService.Instance.Unsubscribe<AdventureHardResetEvent>(OnRefreshAdventureStageData);
            
            _dropItemScrollRect.dataSource = new LoopScrollDataSource<UIThingSlot>(
                null,
                null,
                null);

            _availableSpotClick(true);
            _availableSpotClick = null;
            
            _dropItems.Clear();
        }
        
        public override bool OnEscape()
        {
            if (Interactable == false)
            {
                return false;
            }

            UIManager.Instance.OverlayStack.Close(this);

            return true;
        }

        private void RefreshPopup()
        {
            _selectedStageData = AdventureModeUtil.GetPlayedAdventureStage(_playedAdventureStages, _adventureStageData.Id);
            
            if (_adventureStageData.StageNumber == 1)
            {
                _previousStageArrow.gameObject.SetActive(false);
                _nextStageArrow.gameObject.SetActive(true);
            }
            else if (_adventureStageData.StageNumber == DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(_adventureStageData.AdventureChapter).Count)
            {
                _previousStageArrow.gameObject.SetActive(true);
                _nextStageArrow.gameObject.SetActive(false);
            }
            else
            {
                _previousStageArrow.gameObject.SetActive(true);
                _nextStageArrow.gameObject.SetActive(true);
            }

            _dropItemScrollRect.dataSource = new LoopScrollDataSource<UIThingSlot>(
                OnListSlotInitialize,
                OnListSlotRelease,
                OnListSlotUpdate);
            
            Addressables.LoadAssetAsync<Sprite>(_adventureChapterData.ChapterMapIconPath).Completed +=
                (x) =>
                {
                    _stageMapImage.sprite = x.Result;
                };

            _stageName.text =
                string.Format(
                    LocalString.Get("Str_UI_AdventureStage_Title"),
                    _adventureChapterData.ChapterNumber, _adventureStageData.StageNumber, _adventureChapterData.ChapterDifficultType);
            
            _difficultStyles.SetStyle(_adventureChapterData.ChapterDifficultType.ToString());

            _recommendLevel.text = $"Lv.{_adventureStageData.RecommendLv}";

            for (int i = 0; i < _starMissionListItems.Length; i++)
            {
                _starMissionListItems[i].Set(_selectedStageData, _adventureStageData, i);
            }
            
            // 토벌권의 아이템 타입은 유일한걸로 정함.
            var ticketData = DataTable.ItemDataTable.GetGroupByItemType(ItemType.DestroyTicket);
            if (ticketData == null)
            {
                DebugHelper.LogError("토벌권 아이템데이터를 찾을 수 없습니다.");
                
                _currentModUpTicketCount = 0;
            }
            else if (ticketData.Count != 1)
            {
                DebugHelper.LogError("토벌권 아이템데이터가 여러개입니다.");

                _currentModUpTicketCount = 0;
            }
            else
            {
                _currentModUpTicketCount = User.My.ItemInfo.GetCount(ItemType.DestroyTicket);
                
                _modUpTicketImage.sprite = AtlasManager.GetItemIcon(ticketData?.FirstOrDefault()?.IconPath);
            }
            
            _currentModUpTicket.text = _currentModUpTicketCount.ToString("N0");

            switch (_adventureChapterData.ChapterDifficultType)
            {
                case ChapterDifficultType.None:
                case ChapterDifficultType.Normal:
                case ChapterDifficultType.Max:
                    {
                        _normalModUpRoot.SetActive(true);
                        _hardModUpRoot.SetActive(false);
                        
                        _currentNormalModUpAp.text = _changeNormalModUpAp.text = $"{User.My.PointInfo.Ap:N0}";

                        bool enough = User.My.PointInfo.Ap >= _adventureStageData.TicketCount;
                        _currentNormalBattleAp.text = $"{User.My.PointInfo.Ap:N0}";
                        _changeNormalBattleAp.text = $"{(enough ? (User.My.PointInfo.Ap - _adventureStageData.TicketCount) : User.My.PointInfo.Ap):N0}";
                
                        _disableReadyBattleButton.gameObject.SetActive(!enough);

                        _selectedModUpCount = (_currentModUpTicketCount > 0) ? 1 : 0;
                    }
                    break;
                case ChapterDifficultType.Hard:
                    {
                        _normalModUpRoot.SetActive(false);
                        _hardModUpRoot.SetActive(true);
                        
                        _currentHardModUpAp.text = _changeHardModUpAp.text = $"{User.My.PointInfo.Ap:N0}";

                        bool enough = User.My.PointInfo.Ap >= _adventureStageData.TicketCount;
                        _currentHardBattleAp.text = $"{User.My.PointInfo.Ap:N0}";
                        _changeHardBattleAp.text = $"{(enough ? (User.My.PointInfo.Ap - _adventureStageData.TicketCount) : User.My.PointInfo.Ap):N0}";
                
                        _disableReadyBattleButton.gameObject.SetActive(!enough);

                        _hardBattleLimit = DataTable.BattleModeInfoDataTable[BattleModeType.AdventureMode].BattleLimit;
                        
                        if (_hardBattleLimit > (_selectedStageData?.ClearCount ?? 0))
                        {
                            _selectedModUpCount = (_currentModUpTicketCount > 0) ? 1 : 0;
                        }
                        else
                        {
                            _selectedModUpCount = 0;
                        }
                        
                        _hardModUpLimitCount.text = _hardBattleLimitCount.text = $"{_hardBattleLimit - (_selectedStageData?.ClearCount ?? 0) - _selectedModUpCount}/{_hardBattleLimit}";
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _selectedModUpTicket.text = $"{_selectedModUpCount}{LocalString.Get("Str_UI_Ticket_UseCount")}";

            RefreshModUpApStatus();
            CalcModUpButtonStatus();

            // 초회 보상
            if (_selectedStageData == null)
            {
                if (DataTable.ThingDataTable[_adventureStageData.FirstClearReward] != null)
                {
                    var rewardThingData = DataTable.ThingDataTable[_adventureStageData.FirstClearReward];
                    if (rewardThingData != null)
                    {
                        DropItemData dropItemData = new() { RewardType = DropItemData.RewardTypes.FirstClear };

                        dropItemData.ThingData = rewardThingData switch
                        {
                            IItemData => new RewardStackable(_adventureStageData.FirstClearReward,
                                _adventureStageData.FirstClearRewardCount),
                            IPointData => new RewardPoint(_adventureStageData.FirstClearReward,
                                _adventureStageData.FirstClearRewardCount),
                            IEquipmentData => new RewardEquipment(_adventureStageData.FirstClearReward),
                            _ => dropItemData.ThingData
                        };

                        _dropItems.Add(dropItemData);
                    }
                }
                
                // 미션 보상
                if (_adventureStageData.FreecashReward > 0)
                {
                    var freeCashPointData = DataTable.PointDataTable.GetByPointType(PointType.FreeCash);
                    if (freeCashPointData != null)
                    {
                        DropItemData dropItemData = new()
                        {
                            ThingData = new RewardPoint(pointId: freeCashPointData.Id,
                                count: _adventureStageData.FreecashReward),
                            RewardType = DropItemData.RewardTypes.MissionClear
                        };

                        _dropItems.Add(dropItemData);
                    }
                }
            }
            else
            {
                if (AdventureModeUtil.IsClearMission(_selectedStageData) == false)
                {
                    // 미션 보상
                    // FIXME: 아이템을 생성하는 공용로직이 필요하지 않을까
                    if (_adventureStageData.FreecashReward > 0)
                    {
                        var freeCashPointData = DataTable.PointDataTable.GetByPointType(PointType.FreeCash);
                        if (freeCashPointData != null)
                        {
                            DropItemData dropItemData = new()
                            {
                                ThingData = new RewardPoint(pointId: freeCashPointData.Id,
                                    count: _adventureStageData.FreecashReward),
                                RewardType = DropItemData.RewardTypes.MissionClear
                            };

                            _dropItems.Add(dropItemData);
                        }
                    }
                }
            }

            // 드랍 보상
            // FIXME: 순서대로 넣기 위해선 어쩔 수 없나...
            AddRewardItem(_adventureStageData.ClearReward1, _adventureStageData.ClearRewardCount1);
            AddRewardItem(_adventureStageData.ClearReward2, _adventureStageData.ClearRewardCount2);
            AddRewardItem(_adventureStageData.ClearReward3, _adventureStageData.ClearRewardCount3);
            AddRewardItem(_adventureStageData.ClearReward4, _adventureStageData.ClearRewardCount4);
            AddRewardItem(_adventureStageData.ClearReward5, _adventureStageData.ClearRewardCount5);
            
            // 골드보상.
            if (_adventureStageData.GoldReward > 0)
            {
                var goldPointData = DataTable.PointDataTable.GetByPointType(PointType.Gold);
                if (goldPointData != null)
                {
                    DropItemData dropItemData = new()
                    {
                        ThingData = new RewardPoint(pointId: goldPointData.Id,
                            count: _adventureStageData.FreecashReward),
                        RewardType = DropItemData.RewardTypes.Normal
                    };

                    _dropItems.Add(dropItemData);
                }
            }

            _dropItemScrollRect.totalCount = _dropItems.Count;
            _dropItemScrollRect.SendItemData(_dropItems, null);
            _dropItemScrollRect.RefillCells();
        }
        
        private void AddRewardItem(string id, int count)
        {
            var thingData = DataTable.ThingDataTable[id];

            if (thingData == null)
            {
                return;
            }

            DropItemData dropItemData;

            switch (thingData)
            {
                case PointData:
                    dropItemData = new DropItemData { ThingData = new RewardPoint(id, count) };
                    _dropItems.Add(dropItemData);
                    break;
                case ItemData:
                    dropItemData = new DropItemData { ThingData = new RewardStackable(id, count) };
                    _dropItems.Add(dropItemData);
                    break;
                case EquipmentData:
                    dropItemData = new DropItemData { ThingData = new RewardEquipment(id, count) };
                    _dropItems.Add(dropItemData);
                    break;
                // case Character:
                //     _dropItems.Add(new RewardStackable(itemId, count));
                //     break;
                default:
                    Debug.LogError( "Not Item Reward Type " + thingData.GetType().Name);
                    break;
            }
        }

        private void CalcModUpButtonStatus()
        {
            _disableModUp.SetActive(AdventureModeUtil.IsClearMission(_selectedStageData) == false);
            if (_currentModUpTicketCount <= 0)
            {
                _disableModUpButton.gameObject.SetActive(true);
            }
            else
            {
                _disableModUpButton.gameObject.SetActive(User.My.PointInfo.Ap < _adventureStageData.TicketCount * _selectedModUpCount);
            }
        }
        
        // 메세지
        private bool OnRefreshAdventureStageData(AdventureHardResetEvent msg)
        {
            if (_playedAdventureStages.ContainsKey(msg.PlayedAdventureStage.Id))
            {
                _playedAdventureStages[msg.PlayedAdventureStage.Id] = msg.PlayedAdventureStage;
            }

            _selectedStageData = msg.PlayedAdventureStage;
            
            RefreshPopup();

            return true;
        }

        #region 액션
        public void OnClickClose()
        {
            UIManager.Instance.OverlayStack.Close(this);
        }
        public void OnClickMonsterInfo()
        {
            var stageInfoData = DataTable.StageInfoDataTable.GetById(_adventureStageData.StageInfoId);
            if (stageInfoData != null)
            {
                UIManager.Instance.OverlayStack.Push(UIAdventureEnemyListOverlay.CreateContext(new() { StageId = stageInfoData.Id, StageNo = SB.Str(_adventureChapterData.ChapterNumber, "-", _adventureStageData.StageNumber), }).SetOrder(1));
            }
        }
        public void OnClickSubtractTicket()
        {
            // MEMO: 서버는 ModUp, 기획은 Destroy. (소탕 -> 토벌로 바뀜)
            if (1 > _selectedModUpCount)
            {
                return;
            }
 
            _selectedModUpCount -= 1; 
            
            _selectedModUpTicket.text = $"{_selectedModUpCount}{LocalString.Get("Str_UI_Ticket_UseCount")}";

            switch (_adventureChapterData.ChapterDifficultType)
            {
                case ChapterDifficultType.None:
                case ChapterDifficultType.Normal:
                case ChapterDifficultType.Max:
                    {
                        
                    }
                    break;
                case ChapterDifficultType.Hard:
                    {
                        _hardModUpLimitCount.text = $"{_hardBattleLimit - (_selectedStageData?.ClearCount ?? 0) - _selectedModUpCount}/{_hardBattleLimit}";
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            RefreshModUpApStatus();
            
            CalcModUpButtonStatus();
        }
        public void OnClickAddTicket()
        {
            // MEMO: 서버는 ModUp, 기획은 Destroy. (소탕 -> 토벌로 바뀜)
            switch (_adventureChapterData.ChapterDifficultType)
            {
                case ChapterDifficultType.None:
                case ChapterDifficultType.Normal:
                case ChapterDifficultType.Max:
                    {
                        if (_selectedModUpCount >= _currentModUpTicketCount)
                        {
                            return;
                        }

                        if (_adventureStageData.TicketCount * (_selectedModUpCount + 1) > User.My.PointInfo.Ap)
                        {
                            return;
                        }

                        _selectedModUpCount += 1; 
            
                        _selectedModUpTicket.text = $"{_selectedModUpCount}{LocalString.Get("Str_UI_Ticket_UseCount")}";

                        RefreshModUpApStatus();
                        
                        CalcModUpButtonStatus();
                    }
                    break;
                case ChapterDifficultType.Hard:
                    {
                        if (_hardBattleLimit <= (_selectedStageData?.ClearCount ?? 0) + _selectedModUpCount)
                        {
                            return;
                        }
                        
                        if (_selectedModUpCount >= _currentModUpTicketCount)
                        {
                            return;
                        }

                        if (_adventureStageData.TicketCount * (_selectedModUpCount + 1) > User.My.PointInfo.Ap)
                        {
                            return;
                        }

                        _selectedModUpCount += 1; 
            
                        _selectedModUpTicket.text = $"{_selectedModUpCount}{LocalString.Get("Str_UI_Ticket_UseCount")}";
                        
                        _hardModUpLimitCount.text = $"{_hardBattleLimit - (_selectedStageData?.ClearCount ?? 0) - _selectedModUpCount}/{_hardBattleLimit}";

                        RefreshModUpApStatus();
                        
                        CalcModUpButtonStatus();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void RefreshModUpApStatus()
        {
            switch (_adventureChapterData.ChapterDifficultType)
            {
                case ChapterDifficultType.None:
                case ChapterDifficultType.Normal:
                case ChapterDifficultType.Max:
                    {
                        _changeNormalModUpAp.text = $"{User.My.PointInfo.Ap - (_adventureStageData.TicketCount * _selectedModUpCount):N0}";
                    }
                    break;
                case ChapterDifficultType.Hard:
                    {
                        _changeHardModUpAp.text = $"{User.My.PointInfo.Ap - (_adventureStageData.TicketCount * _selectedModUpCount):N0}";
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public void OnClickSweep()
        {
            if (_adventureChapterData.ChapterDifficultType == ChapterDifficultType.Hard)
            {
                if (_selectedStageData.ClearCount >= _hardBattleLimit)
                {
                    UIManager.Instance.OverlayStack.Push(Overlay.UICashUsagePopup.CreateContext(new()
                    {
                        OzAdventure = _selectedStageData,
                        AdventureChapterData = _adventureChapterData,
                        NeedCash = CalcRechargingCost()
                    }));

                    return;
                }
            }
            
            if (_selectedModUpCount == 0)
            {
                return;
            }

            UIManager.Instance.OverlayStack.Push(Overlay.UISystemMessagePopup.CreateContext(new()
            {
                type = UISystemMessagePopup.Type.Yes_No,
                TitleString = LocalString.Get("Str_UI_Notification"),
                InfoString = $"{string.Format(LocalString.Get("Str_UI_Destroy_StartAsk"), "AP", _adventureStageData.TicketCount * _selectedModUpCount, _selectedModUpCount)}",
                YesString = LocalString.Get("Str_UI_Button_Confirm"),
                NoString = LocalString.Get("Str_UI_Button_Cancel"),
                YesAction = () => 
                {
                        PacketProcessor.Instance.SendRequest(
                                new Network.Packets.Game.Adventure.AdventureModUpPacket(
                                    BattleModeType.AdventureMode, BattleModePartyType.AdventureModeParty, _adventureStageData.Id, _selectedModUpCount).
                        OnCompleted(response =>
                        {
                            if (response.ErrCode != 0)
                            {
                                return;
                            }

                            User.My.ReaderInfo.ReaderExp = response.ExpReader.ResExp;
                            User.My.ReaderInfo.ReaderLevel = response.ExpReader.ResLevel;

                            foreach (var character in response.ExpChars)
                            {
                                User.My.CharacterInfo[character.Key].Exp = character.Value.ResExp;
                                User.My.CharacterInfo[character.Key].Level = character.Value.ResLevel;
                            }

                            OnClickClose();
                            
                            UIManager.Instance.OverlayStack.Push(UIDestroyResultPopup.CreateContext(new UIDestroyResultPopup.State()
                            {
                                ResultData = response
                            }));
                        })
                        .OnFailed(_ =>
                        {
                            
                        }));

                },
                NoAction = () =>
                {

                },
            }));
        }
        public void OnClickReadyBattle()
        {
            if (_adventureChapterData.ChapterDifficultType == ChapterDifficultType.Hard)
            {
                if (_selectedStageData?.ClearCount >= _hardBattleLimit)
                {
                    UIManager.Instance.OverlayStack.Push(Overlay.UICashUsagePopup.CreateContext(new()
                    {
                        OzAdventure = _selectedStageData,
                        AdventureChapterData = _adventureChapterData,
                        NeedCash = CalcRechargingCost()
                    }));

                    return;
                }
            }

            if (string.IsNullOrEmpty(_adventureStageData.OpenConditionValue) == false)
            {
                if (AdventureModeUtil.IsPlayedStage(_adventureStageData.OpenConditionValue) == false)
                {
                    return;
                }
            }

            User.My.AdventureModeInfo.SetSelectedAdventureStage(_adventureStageData.Id);
            
            UIManager.Instance.PanelStack.Push(UIAdventureBattleReadyPanel.CreateContext(new UIAdventureBattleReadyPanel.State
            {
                Stage = User.My.AdventureModeInfo.GetSelectedAdventureStage()
            }));
            
            UIManager.Instance.OverlayStack.Close(this);
        }
        public void OnClickPreviousStage()
        {
            List<AdventureStageData> list = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(_adventureStageData.AdventureChapter);
            
            if (list is not { Count: > 0 })
            {
                return;
            }
        
            foreach (var stage in list.Where(stage => stage.StageNumber == (_adventureStageData.StageNumber - 1)))
            {
                _adventureStageData = stage;

                break;
            }

            _dropItemScrollRect.ClearCells();

            _dropItems.Clear();
            
            RefreshPopup();
        }
        public void OnClickNextStage()
        {
            List<AdventureStageData> list = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(_adventureStageData.AdventureChapter);
            
            if (list is not { Count: > 0 })
            {
                return;
            }
        
            foreach (var stage in list.Where(stage => stage.StageNumber == (_adventureStageData.StageNumber + 1)))
            {
                _adventureStageData = stage;

                break;
            }

            _dropItemScrollRect.ClearCells();

            _dropItems.Clear();
            
            RefreshPopup();
        }
        #endregion

        private int CalcRechargingCost()
        {
            // 충전 비용 = (최초 충전 비용) + (최초 충전 비용) x (할증비율) x (오늘 충전 횟수)
            // 최초 충전 비용
            float firstChargingCost = float.Parse(DataTable.BaseSettingDataTable["Base_BattleLimit_FirstCost"].Value1);

            // 할증비율
            float extraChargingRate = float.Parse(DataTable.BaseSettingDataTable["Base_BattleLimit_AddCost"].Value1);

            int rechargingCost = Mathf.RoundToInt
            (
                firstChargingCost + (firstChargingCost * (1.0f + extraChargingRate) * _selectedStageData.ResetCount)
            );

            return rechargingCost;
        }
        
        #region 드랍아이템
        private void OnListSlotInitialize(UIThingSlot slot)
        {
            slot.OnClick =
                (x) =>
                {
#if UNITY_EDITOR
                    DebugHelper.Log($"Click Item: {x.Value.Id}");
#endif
                };
            
            slot.AttachModule<UIItemSlotRewardModule>();
        }
        private void OnListSlotRelease(UIThingSlot slot)
        {
            slot.DetachModule<UIItemSlotRewardModule>();
            slot.OnClick = null;
        }
        private void OnListSlotUpdate(UIThingSlot slot, int index)
        {
            slot.Value = _dropItems[index].ThingData;

            switch (_dropItems[index])
            {
                case { RewardType: DropItemData.RewardTypes.FirstClear }:
                    {
                        slot.GetModule<UIItemSlotRewardModule>().SetFirstReward();
                    }
                    break;
                case { RewardType: DropItemData.RewardTypes.MissionClear }:
                    {
                        slot.GetModule<UIItemSlotRewardModule>().SetMissionReward();
                    }
                    break;
                case { RewardType: DropItemData.RewardTypes.Normal }:
                    {
                        slot.GetModule<UIItemSlotRewardModule>().SetDropReward();
                    }
                    break;
            }
        }
        private struct DropItemData
        {
            public enum RewardTypes
            {
                Normal,
                FirstClear,
                MissionClear,
            }
            
            public IThingData ThingData;
            public RewardTypes RewardType;
        }
        #endregion

        public struct State
        {
            public AdventureChapterData AdventureChapterData;
            public AdventureStageData AdventureStageData;
            public Action<bool> AvailableSpotClick;
            // 서버에서 받은 플레이 기록
            public Dictionary<string, OZAdventure> PlayedAdventureStages;
        }
    }
}