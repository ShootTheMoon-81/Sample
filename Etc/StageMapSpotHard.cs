using Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StageMapSpotHard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private GameObject _lockIcon;

    [SerializeField]
    private GameObject[] _stars;

    [SerializeField]
    private TextMeshPro _stageNumber;

    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    [SerializeField]
    private TextMeshPro _countStatus;

    private AdventureStageData _adventureStageData;
    private Action<int> _onClickAction;
    private OZAdventure _ozAdventure;
    
    public bool Lock
    {
        get
        {
            return _lockIcon.IsActiveInHierarchy();
        }
        private set
        {
            _lockIcon.SetActive(value);
        }
    }

    public void SetStageMapSpot(bool isLock, Action<int> onClickAction, AdventureStageData adventureStageData, OZAdventure ozAdventure)
    {
        Lock = isLock;

        _onClickAction = onClickAction;
        _adventureStageData = adventureStageData;
        _ozAdventure = ozAdventure;

        SetStar();

        _stageNumber.text =
            $"{DataTable.AdventureChapterDataTable[_adventureStageData.AdventureChapter].ChapterNumber}-{_adventureStageData.StageNumber}";
    }

    private void SetStar()
    {
        for (int i = 0; i < _stars.Length; i++)
        {            
            _stars[i].gameObject.SetActive(AdventureModeUtil.IsClearMission(_ozAdventure));
        }
    }

    #region 인스펙터 할당용
    // public void OnValidate()
    // {
    //     if (_lockIcon == null)
    //     {
    //         _lockIcon = transform.Find("LockSpot").gameObject;
    //     }
    //     
    //     if (_stars.Length == 0)
    //     {
    //         _stars = (from child in transform.GetComponentsInChildren<SpriteRenderer>()
    //             where child.gameObject.name.Contains("Star_filled_") select child.gameObject).ToArray();
    //     }
    // }
    #endregion

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Lock == true)
        {
            return;
        }
        
        _onClickAction?.Invoke(_adventureStageData.StageNumber - 1);
    }
}