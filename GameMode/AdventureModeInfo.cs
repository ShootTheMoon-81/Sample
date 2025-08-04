using Data;
using System.Collections.Generic;
using System.Linq;

namespace Users
{
    public class AdventureModeInfo : UserInfo
    {
        private AdventureStageData _selectedAdventureStageData;
        public AdventureStageData GetSelectedAdventureStage()
        {
            if (_selectedAdventureStageData is null)
            {
                SetSelectedAdventureStage(User.My.ReaderInfo.GetHighAdventureStage(ChapterDifficultType.Normal));
            }

            return _selectedAdventureStageData;
        }
        public void SetSelectedAdventureStage(string adventureStageId)
        {
            if (string.IsNullOrEmpty(adventureStageId) == false)
            {
                var adventureStageData = DataTable.AdventureStageDataTable[adventureStageId];
                if (adventureStageData is not null)
                {
                    _selectedAdventureStageData = adventureStageData;
                }
            }

            _selectedAdventureStageData ??= DataTable.AdventureStageDataTable.FirstOrDefault();
        }
        
        private AdventureStageData _nextStage;
        public AdventureStageData NextStage
        {
            get
            {
                if (_nextStage == null)
                {
                    AdventureModeUtil.CalcNextStage();
                }
                
                return _nextStage;
            }
            set
            {
                _nextStage = value;
            }
        }

        #region 챕터 보상 수령 정보
        private Dictionary<string, OZAdventureChapter> _adventureChapterClearData = new();

        public IEnumerable<OZAdventureChapter> AdventureChapters
        {
            get => _adventureChapterClearData.Values.Select(x => x);
            set
            {
                foreach (var adventureChapter in value)
                {
                    if (_adventureChapterClearData.ContainsKey(adventureChapter.Id))
                    {
                        _adventureChapterClearData[adventureChapter.Id] = adventureChapter;
                    }
                    else
                    {
                        _adventureChapterClearData.Add(adventureChapter.Id, adventureChapter);   
                    }
                }
            }
        }

        public void SetAdventureChapter(OZAdventureChapter adventureChapter)
        {
            if (adventureChapter == null)
            {
                return;
            }
            
            if (_adventureChapterClearData.ContainsKey(adventureChapter.Id))
            {
                _adventureChapterClearData[adventureChapter.Id] = adventureChapter;
            }
            else
            {
                _adventureChapterClearData.Add(adventureChapter.Id, adventureChapter);   
            }
        }

        public OZAdventureChapter GetAdventureChapter(string chapterId)
        {
            return _adventureChapterClearData.ContainsKey(chapterId) ? _adventureChapterClearData[chapterId] : null;
        }
        #endregion
        
        #region 사용하지 않아도 되는지 확인.
        private string _playingAdventureId;
        public string PlayingAdventureId
        {
            get
            {                
                return string.IsNullOrEmpty(_playingAdventureId) ? "N_St_001_001" : _playingAdventureId;
            }
        }
        public StageStartAns ServerDataStageStart { get; private set; }

        public void SetPlayingStageData(string adventureId, StageStartAns stageStartAns)
        {
            _playingAdventureId = adventureId;
            ServerDataStageStart = stageStartAns;
        }
        #endregion

        #region 서버작업 완료까지 임시
        // private Dictionary<string, List<OZAdventure>> _adventures_Temp = new();
        //
        // public void SetAdventureData_Temp(IEnumerable<OZAdventure> ozAdventures)
        // {
        //     foreach (var inputData in ozAdventures)
        //     {
        //         var difficultType = DataTable.AdventureChapterDataTable[DataTable.AdventureStageDataTable[inputData.Id].AdventureChapter].ChapterDifficultType;
        //
        //         SetAdventureData_Temp(inputData);
        //     }
        // }
        //
        // public void SetAdventureData_Temp(OZAdventure ozAdventure)
        // {
        //     var difficultType = DataTable.AdventureChapterDataTable[DataTable.AdventureStageDataTable[ozAdventure.Id].AdventureChapter].ChapterDifficultType;
        //     
        //     if (_adventures_Temp.TryGetValue(DataTable.AdventureStageDataTable[ozAdventure.Id].AdventureChapter, out var adventureList))
        //     {
        //         if (adventureList.Contains(ozAdventure))
        //         {
        //             foreach (var ownData in adventureList.Where(ownData => string.Equals(ownData.Id, ozAdventure.Id, StringComparison.OrdinalIgnoreCase)))
        //             {
        //                 ownData.UpdateFrom(ozAdventure);
        //             }
        //         }
        //         else
        //         {
        //             adventureList.Add(ozAdventure);
        //         }
        //     }
        //     else
        //     {
        //         _adventures_Temp.Add(DataTable.AdventureStageDataTable[ozAdventure.Id].AdventureChapter, new List<OZAdventure>() { ozAdventure });
        //     }
        // }
        //
        // public List<OZAdventure> TestRequestAPI(string adventureChapterId)
        // {
        //     return _adventures_Temp.TryGetValue(adventureChapterId, out var adventureList) ? adventureList : null;
        // }
        
        // public OZAdventure this[string adventureStageId]
        // {
        //     get
        //     {
        //         return _adventuresTemp.SelectMany(difficult => difficult.Value).
        //             FirstOrDefault(adventure => string.Equals(adventure.AdventureId, adventureStageId, StringComparison.OrdinalIgnoreCase));
        //     }
        //     set
        //     {
        //         ChapterDifficultType chapterDifficultType = DataTable
        //             .AdventureChapterDataTable[DataTable.AdventureStageDataTable[adventureStageId].AdventureChapter]
        //             .ChapterDifficultType;
        //         
        //         if (_adventuresTemp.TryGetValue(DataTable.AdventureStageDataTable[adventureStageId].AdventureChapter, out var adventures))
        //         {
        //             bool isContain = false;
        //             foreach (var adventure in adventures.Where(adventure => string.Equals(adventure.AdventureId, adventureStageId, StringComparison.OrdinalIgnoreCase)))
        //             {
        //                 adventure.UpdateFrom(value);
        //
        //                 isContain = true;
        //             }
        //
        //             if (!isContain)
        //             {
        //                 adventures.Add(value);
        //             }                    
        //         }
        //         else
        //         {
        //             _adventuresTemp.Add(DataTable.AdventureStageDataTable[adventureStageId].AdventureChapter, new List<OZAdventure>() { value });
        //         }
        //     }
        // }
        #endregion

        // private enum MissionClearType
        // {
        //     NotClear = 0,
        //     First = 1,          // 001
        //     Second = 2,         // 010
        //     FirstSecond = 3,    // 011
        //     ThirdClear = 4,     // 100
        //     FirstThird = 5,     // 101
        //     SecondThird = 6,    // 110
        //     All = 7             // 111
        // }

        // private readonly Dictionary<string, int> _missionStarInfo = new Dictionary<string, int>()
        // {
        //     { "001", 1 },
        //     { "010", 1 },
        //     { "011", 2 },
        //     { "100", 1 },
        //     { "101", 2 },
        //     { "110", 2 },
        //     { "111", 3 }
        // };

        // private Dictionary<ChapterDifficultType, List<OZAdventure>> _adventures = new();

        // public IEnumerable<OZAdventure> Adventures
        // {
        //     get => _adventures.Values.SelectMany(x => x);
        //     set
        //     {
        //         foreach (var inputData in value)
        //         {
        //             var difficultType = DataTable.AdventureChapterDataTable[DataTable.AdventureStageDataTable[inputData.AdventureId].AdventureChapter].ChapterDifficultType;
        //
        //             if (_adventures.TryGetValue(difficultType, out var adventureList))
        //             {
        //                 if (adventureList.Contains(inputData))
        //                 {
        //                     foreach (var ownData in adventureList.Where(ownData => string.Equals(ownData.AdventureId, inputData.AdventureId, StringComparison.OrdinalIgnoreCase)))
        //                     {
        //                         ownData.UpdateFrom(inputData);
        //                     }
        //                 }
        //                 else
        //                 {
        //                     adventureList.Add(inputData);
        //                 }
        //             }
        //             else
        //             {
        //                 _adventures.Add(difficultType, new List<OZAdventure>() { inputData });
        //             }
        //         }
        //
        //         // 리더정보로 세팅.
        //         // 어드벤처는 클리어 정보들을 내려주는 패킷이므로, 초기에는 없을 수 있음. 
        //         // 관련 된 예외처리 진행
        //         // if (_adventures == null || _adventures.Count == 0)
        //         // {
        //         //     SetSelectedStage(DataTable.AdventureStageDataTable.FirstOrDefault().Id);
        //         // }
        //         // else
        //         // {
        //         //     SetSelectedStage(DataTable.AdventureStageDataTable[_adventures.LastOrDefault().Value.LastOrDefault()?.AdventureId].Id);
        //         // }
        //     }
        // }

        // // TODO: 하나만 사용하도록.
        // public OZAdventure this[ChapterDifficultType chapterDifficultType, string id]
        // {
        //     get
        //     {
        //         return _adventures.TryGetValue(chapterDifficultType, out var adventures) ? 
        //             adventures.FirstOrDefault(adventure => string.Equals(adventure.AdventureId, id, StringComparison.OrdinalIgnoreCase)) : null;
        //     }
        //     set
        //     {
        //         if (_adventures.TryGetValue(chapterDifficultType, out var adventures))
        //         {
        //             bool isContain = false;
        //             foreach (var adventure in adventures.Where(adventure => string.Equals(adventure.AdventureId, id, StringComparison.OrdinalIgnoreCase)))
        //             {
        //                 adventure.UpdateFrom(value);
        //
        //                 isContain = true;
        //             }
        //
        //             if (!isContain)
        //             {
        //                 adventures.Add(value);
        //             }                    
        //         }
        //         else
        //         {
        //             _adventures.Add(chapterDifficultType, new List<OZAdventure>() { value });
        //         }
        //     }
        // }
        // public OZAdventure this[string id]
        // {
        //     get
        //     {
        //         return _adventures.SelectMany(difficult => difficult.Value).
        //             FirstOrDefault(adventure => string.Equals(adventure.AdventureId, id, StringComparison.OrdinalIgnoreCase));
        //     }
        //     set
        //     {
        //         ChapterDifficultType chapterDifficultType = DataTable
        //             .AdventureChapterDataTable[DataTable.AdventureStageDataTable[id].AdventureChapter]
        //             .ChapterDifficultType;
        //         
        //         if (_adventures.TryGetValue(chapterDifficultType, out var adventures))
        //         {
        //             bool isContain = false;
        //             foreach (var adventure in adventures.Where(adventure => string.Equals(adventure.AdventureId, id, StringComparison.OrdinalIgnoreCase)))
        //             {
        //                 adventure.UpdateFrom(value);
        //
        //                 isContain = true;
        //             }
        //
        //             if (!isContain)
        //             {
        //                 adventures.Add(value);
        //             }                    
        //         }
        //         else
        //         {
        //             _adventures.Add(chapterDifficultType, new List<OZAdventure>() { value });
        //         }
        //     }
        // }

        // public bool IsExistStage(string id)
        // {
        //     AdventureStageData adventureStageData = DataTable.AdventureStageDataTable[id];
        //     if (adventureStageData == null)
        //     {
        //         return false;
        //     }
        //
        //     AdventureChapterData adventureChapterData = DataTable
        //         .AdventureChapterDataTable[adventureStageData.AdventureChapter];
        //     if (adventureChapterData == null)
        //     {
        //         return false;
        //     }
        //
        //     ChapterDifficultType chapterDifficultType = adventureChapterData.ChapterDifficultType;
        //
        //     bool isContain = false;
        //     if (!_adventures.TryGetValue(chapterDifficultType, out var adventures))
        //     {
        //         return false;
        //     }
        //
        //     foreach (var adventure in adventures.Where(adventure => string.Equals(adventure.AdventureId, id, StringComparison.OrdinalIgnoreCase)))
        //     {
        //         isContain = true;
        //     }
        //
        //     return isContain;
        // }
        
        // public int GetStagesMissionStar(ChapterDifficultType chapterDifficultType, List<AdventureStageData> stageData)
        // {
        //     if (_adventures.Count == 0)
        //     {
        //         return 0;
        //     }
        //
        //     return _adventures[chapterDifficultType].Sum(
        //         ozAdventure => stageData.Where(adventureStageData => string.Equals(ozAdventure.AdventureId, adventureStageData.Id, StringComparison.OrdinalIgnoreCase)).Sum(
        //             adventureStageData => GetStageMissionStar(ozAdventure.AdventureId)));
        // }

        // public int GetStageMissionStar(string adventureStageId)
        // {
        //     if (!IsExistStage(adventureStageId))
        //     {
        //         return 0;
        //     }
        //
        //     string binary = Convert.ToString(this[adventureStageId].Mission, 2).PadLeft(3, '0');
        //
        //     int missionStar = _missionStarInfo.ContainsKey(binary) ? _missionStarInfo[binary] : 0;
        //
        //     return missionStar;
        // }

        // public bool IsClearMission(string adventureStageId)
        // {
        //     if (!IsExistStage(adventureStageId))
        //     {
        //         return false;
        //     }
        //
        //     return this[adventureStageId].Mission >= (int)MissionClearType.All;
        // }

        // public bool IsClearMission(string adventureStageId, int index)
        // {
        //     if (!IsExistStage(adventureStageId))
        //     {
        //         return false;
        //     }
        //
        //     MissionClearType missionClearType = (MissionClearType)this[adventureStageId].Mission;
        //
        //     switch (missionClearType)
        //     {
        //         case MissionClearType.NotClear:
        //             break;
        //         case MissionClearType.First:
        //             {
        //                 if (index is 0)
        //                 {
        //                     return true;
        //                 }
        //             }
        //             break;
        //         case MissionClearType.Second:
        //             {
        //                 if (index is 1)
        //                 {
        //                     return true;
        //                 }
        //             }
        //             break;
        //         case MissionClearType.FirstSecond:
        //             {
        //                 if (index is 0 or 1)
        //                 {
        //                     return true;
        //                 }
        //             }
        //             break;
        //         case MissionClearType.ThirdClear:
        //             {
        //                 {
        //                     if (index is 2)
        //                     {
        //                         return true;
        //                     }
        //                 }
        //             }
        //             break;
        //         case MissionClearType.FirstThird:
        //             {
        //                 {
        //                     if (index is 0 or 2)
        //                     {
        //                         return true;
        //                     }
        //                 }
        //             }
        //             break;
        //         case MissionClearType.SecondThird:
        //             {
        //                 if (index is 1 or 2)
        //                 {
        //                     return true;
        //                 }
        //             }
        //             break;
        //         case MissionClearType.All:
        //             {
        //                 if (index is 0 or 1 or 2)
        //                 {
        //                     return true;
        //                 }
        //             }
        //             break;
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        //     
        //     return false;
        // }

        // TODO: 서버데이터가 어떻게 정렬되어 오는지 확인이 필요함. 챕터 난이도에 따른 정렬이 이루어져 있는지 확인.
        // public OZAdventure GetLastAttemptStage()
        // {
        //     return Adventures.LastOrDefault();
        // }
        
        // public AdventureChapterData IsExistPreviousChapter(AdventureChapterData adventureChapterData)
        // {
        //     var previousChapterData = DataTable.AdventureChapterDataTable.GetPreviousChapterData(adventureChapterData);
        //     if (previousChapterData is null)
        //     {
        //         
        //     }
        //     
        //     
        //     return previousChapterData is null ? null : DataTable.AdventureChapterDataTable.CalcOpenChapterData(previousChapterData);
        //
        //     // var stageData = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(chapterData.Id);
        //     // string firstOpenCondition = stageData.FirstOrDefault()?.Id;
        //     // return Adventures.Any(ozAdventure => string.Equals(ozAdventure.AdventureId, firstOpenCondition, StringComparison.OrdinalIgnoreCase)) ? chapterData : null;
        // }

        // public AdventureChapterData IsExistNextChapter(AdventureChapterData adventureChapterData)
        // {
        //     var chapterData = DataTable.AdventureChapterDataTable.GetNextChapterData(adventureChapterData);
        //     return chapterData is null ? null : DataTable.AdventureChapterDataTable.CalcOpenChapterData(chapterData);
        //
        //     // var stageData = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(chapterData.Id);
        //     // string firstOpenCondition = stageData.FirstOrDefault()?.OpenCondition;
        //     // return Adventures.Any(ozAdventure => string.Equals(ozAdventure.AdventureId, firstOpenCondition, StringComparison.OrdinalIgnoreCase)) ? chapterData : null;
        //     //
        //     // return null;
        // }

        // public AdventurePartData GetLastClearPart(AdventureChapterData adventureChapterData)
        // {
        //     AdventureChapterData nextChapter = IsExistNextChapter(adventureChapterData);
        //
        //     return nextChapter == null ? DataTable.AdventurePartDataTable[adventureChapterData.AdventurePart] : DataTable.AdventurePartDataTable[nextChapter.AdventurePart];
        // }

        // public bool CheckOpenCondition(OpenConditionType openConditionType, string id)
        // {
        //     switch (openConditionType)
        //     {
        //         case OpenConditionType.None:
        //         case OpenConditionType.No:
        //             {
        //                 return true;
        //             }
        //         case OpenConditionType.AccountLv:
        //             {
        //                 return (int)OpenConditionType.AccountLv >= User.My.ReaderInfo.ReaderLevel;
        //             }
        //         case OpenConditionType.StageClear:
        //             {
        //                 return User.My.AdventureModeInfo.IsExistStage(id);
        //             }
        //         case OpenConditionType.OperationTool:
        //             {
        //                 return true;
        //             }
        //     }
        //
        //     return false;
        // }

        #region 캐싱 정보
        // private ChapterDifficultType _selectedChapterDifficultType;
        // public ChapterDifficultType SelectedChapterDifficultType
        // {
        //     get
        //     {
        //         if (_selectedChapterDifficultType == ChapterDifficultType.None)
        //         {
        //             if (PlayerPrefs.GetString(StageDefine.PlayerPrefDifficultType).Length <= 0)
        //             {
        //                 _selectedChapterDifficultType = ChapterDifficultType.Normal;
        //
        //                 PlayerPrefs.SetString(StageDefine.PlayerPrefDifficultType, _selectedChapterDifficultType.ToString());
        //             }
        //             else
        //             {
        //                 _selectedChapterDifficultType = EnumUtil<ChapterDifficultType>.StringToEnum(PlayerPrefs.GetString(StageDefine.PlayerPrefDifficultType));
        //             }
        //         }
        //
        //         return _selectedChapterDifficultType;
        //     }
        //     set
        //     {
        //         _selectedChapterDifficultType = value;
        //
        //         PlayerPrefs.SetString(StageDefine.PlayerPrefDifficultType, _selectedChapterDifficultType.ToString());
        //     }
        // }
        //
        // private AdventureStageData _selectedAdventureStageData;
        // public AdventureStageData SelectedAdventureStageData
        // {
        //     get
        //     {
        //         if (_selectedAdventureStageData != null)
        //         {
        //             return _selectedAdventureStageData;
        //         }
        //
        //         // 서버에서 준 스테이지 데이터로 교체.
        //         // var selectedStateId = PlayerPrefs.GetString(SelectedChapterDifficultType == ChapterDifficultType.Normal ?
        //         //     StageDefine.PlayerPrefNormalAdventure : StageDefine.PlayerPrefHardAdventure);
        //         //     
        //         // _selectedAdventureStageData = DataTable.AdventureStageDataTable[selectedStateId] ?? DataTable.AdventureStageDataTable.FirstOrDefault();
        //
        //         SetSelectedStage(User.My.ReaderInfo.GetHighStage(BattleModeType.AdventureMode));
        //
        //         return _selectedAdventureStageData;
        //     }
        //     // set
        //     // {
        //     //     _selectedAdventureStageData = value;
        //     //
        //     //     SelectedChapterDifficultType =
        //     //         DataTable.AdventureChapterDataTable[value.AdventureChapter].ChapterDifficultType;
        //     // }
        // }
        //
        // public void SetSelectedStage(string id)
        // {
        //     if (string.IsNullOrEmpty(id) == false)
        //     {
        //         _selectedAdventureStageData = DataTable.AdventureStageDataTable[id] ??
        //                                       DataTable.AdventureStageDataTable[_adventures[SelectedChapterDifficultType].LastOrDefault().AdventureId];
        //     }
        //     else
        //     {
        //         if (_adventures.Count > 0)
        //         {
        //             _selectedAdventureStageData = DataTable.AdventureStageDataTable[_adventures[SelectedChapterDifficultType].LastOrDefault().AdventureId];
        //         }
        //         else
        //         {
        //             _selectedAdventureStageData = DataTable.AdventureStageDataTable.FirstOrDefault();
        //         }
        //     }
        // }

        // public void SetHighestStageInChapter(string chapterId)
        // {
        //     List<AdventureStageData> list = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(chapterId);
        //     
        //     if (list is not { Count: > 0 })
        //     {
        //         return;
        //     }
        //
        //     for (int i = 0; i < list.Count; i++)
        //     {
        //         if (IsExistStage(list[i].Id))
        //         {
        //             _selectedAdventureStageData = list[i];
        //         }
        //     }
        // }

        // public bool IsClearAllStageInChapter(string chapterId, string stageId)
        // {
        //     List<AdventureStageData> list = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(chapterId);
        //     
        //     if (list is not { Count: > 0 })
        //     {
        //         return false;
        //     }
        //
        //     return string.Equals(list.LastOrDefault().Id,stageId, StringComparison.OrdinalIgnoreCase);
        // }

        // public void SetNextStage()
        // {
        //     List<AdventureStageData> list = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(SelectedAdventureStageData.AdventureChapter);
        //     
        //     if (list is not { Count: > 0 })
        //     {
        //         return;
        //     }
        //
        //     foreach (var stage in list.Where(stage => stage.StageNumber > SelectedAdventureStageData.StageNumber))
        //     {
        //         NextStage = stage;
        //             
        //         return;
        //     }
        //
        //     var nextChapterData = IsExistNextChapter(DataTable.AdventureChapterDataTable[SelectedAdventureStageData.AdventureChapter]);
        //     
        //     if (nextChapterData != null)
        //     {
        //         NextStage = DataTable.AdventureStageDataTable.GetGroupByAdventureChapter(nextChapterData.Id).FirstOrDefault();
        //         
        //         //UpdateSelectedStage();
        //     }
        //     else
        //     {
        //         NextStage = SelectedAdventureStageData;
        //     }
        // }
        //
        // // FIXME: NextStage를 한번에 처리할 수 있을지도
        // public void UpdateSelectedStage()
        // {
        //     _selectedAdventureStageData = _nextStage;
        // }
        #endregion
    }    
}