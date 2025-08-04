using Cysharp.Threading.Tasks;
using Data;
using Network.Packets.Cheat;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheatAdventureMode : MonoBehaviour
{
    #region 챕터
    [SerializeField]
    private TMP_Dropdown _chapterIds;

    [SerializeField]
    private TextMeshProUGUI _selectedChapterNumberText;

    [SerializeField]
    private Button _chapterCheatButton;

    private string _selectedChapterId;
    private List<string> _selectedChapterData = new();
    #endregion
    
    #region 스테이지
    [SerializeField]
    private TMP_Dropdown _stageIds;

    [SerializeField]
    private Button _stageCheatButton;

    private string _selectedStageId;
    #endregion

    private void Awake()
    {
        _chapterIds.onValueChanged.AddListener(
            (x) =>
            {
                // int.TryParse(_chapterIds.options[x].text, out int chapterNumber);
                // if (chapterNumber > 0)
                // {
                //     _selectedChapterId = chapterNumber;
                //     _selectedChapterNumberText.text = $"선택한 챕터 넘버 : {_selectedChapterId}";
                //     
                //     RefreshStageList();
                // }
                // else
                // {
                //     DebugHelper.LogError("Can't find Chapter Number : {_chapterIds.options[x].text}");
                // }

                _selectedChapterId = _chapterIds.options[x].text;
                _selectedChapterNumberText.text = $"선택한 챕터 : {_selectedChapterId}";
                
            });
        
        _stageIds.onValueChanged.AddListener(
            (x) =>
            {
                _selectedStageId =_stageIds.options[x].text;
            });
    }

    private void OnEnable()
    {
        _chapterIds.options.Clear();
        // foreach (var optionData in Data.DataTable.AdventureChapterDataTable.Select(adventureChapterData =>
        //              new TMP_Dropdown.OptionData { text = $"{adventureChapterData.ChapterNumber}" }))
        // {
        //     _chapterIds.options.Add(optionData);
        // }
        foreach (var adventureChapterData in DataTable.AdventureChapterDataTable)
        {
            TMP_Dropdown.OptionData optionData = new() { text = $"{adventureChapterData.Id}" };

            _selectedChapterData.Add(adventureChapterData.Id);
            
            _chapterIds.options.Add(optionData);
        }
        _chapterIds.RefreshShownValue();
        
        _chapterIds.onValueChanged.Invoke(_chapterIds.value);

        RefreshStageList();
    }

    private void OnDisable()
    {
        _selectedChapterData.Clear();
    }

    public void OnClickChapterCheat()
    {
        if ( _selectedChapterData.Contains(_selectedChapterId) == false)
        {
            DebugHelper.LogError($"Can't find Chapter Number : {_selectedChapterId}");
            return;
        }
        
        Network.PacketProcessor.Instance.SendRequestAsync(new CheatAdventureClearChapterPacket(_selectedChapterId)).Forget();
    }

    private void RefreshStageList()
    {
        _stageIds.options.Clear();
        AdventureChapterData selectedAdventureChapterData = DataTable.AdventureChapterDataTable.FirstOrDefault(adventureChapterData => adventureChapterData.Id == _selectedChapterId);
        if (selectedAdventureChapterData != null)
        {
            _stageIds.gameObject.SetActive(true);
            _stageCheatButton.gameObject.SetActive(true);

            foreach (var optionData in DataTable.AdventureStageDataTable
                         .GetGroupByAdventureChapter(selectedAdventureChapterData.Id).Select(adventureStageData =>
                             new TMP_Dropdown.OptionData { text = adventureStageData.Id }))
            {
                _stageIds.options.Add(optionData);
            }
            _stageIds.RefreshShownValue();
            
            _stageIds.onValueChanged.Invoke(_stageIds.value);
        }
        else
        {
            _stageIds.gameObject.SetActive(false);
            _stageCheatButton.gameObject.SetActive(false);
        }
    }
    
    public void OnClickStageCheat()
    {
        if (string.IsNullOrEmpty(_selectedStageId))
        {
            DebugHelper.LogError("Can't find Stage ID : {_selectedStageId}");
            return;
        }

        // TODO: OpenConditionType이 StageClear일때만 쓸 수 있는 방법임.
        List<string> stages = Data.DataTable.AdventureStageDataTable.
            TakeWhile(adventureStageData => !string.Equals(adventureStageData.OpenConditionValue, _selectedStageId)).
            Select(adventureStageData => adventureStageData.Id).ToList();

        if (stages.Count > 0)
        {
            Network.PacketProcessor.Instance.SendRequestAsync(new CheatAdventureClearPacket(stages)).Forget();
        }
    }
}