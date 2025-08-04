using Cysharp.Threading.Tasks;
using Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StageMapSpot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private GameObject _lockIcon;

    [SerializeField]
    private GameObject[] _stars;

    [SerializeField]
    private TextMeshPro _stageNumber;

    [SerializeField]
    private GameObject _hardRewardRoot;
    
    [SerializeField]
    private SpriteRenderer _hardRewardIcon;

    [SerializeField]
    private TextMeshPro _hardRewardCurrentCount;

    [SerializeField]
    private TextMeshPro _hardRewardTargetCount;

    [SerializeField]
    private GameObject _unlockEffect;

    private AdventureStageData _adventureStageData;
    private Action<int> _onClickAction;
    private OZAdventure _ozAdventure;
    private float _unlockParticlesTime;
    private EventSystem _eventSystem;
    
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

    private void Awake()
    {
        _eventSystem = FindObjectOfType<EventSystem>();
        
        if (_unlockEffect == null)
        {
            return;
        }

        var unlockParticles = _unlockEffect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (var particle in unlockParticles)
        {
            var mainModule = particle.main;
            float totalTime = mainModule.startLifetime.constant + mainModule.duration;
            if (_unlockParticlesTime < totalTime)
            {
                _unlockParticlesTime = totalTime;
            }
        }
    }

    public void SetStageMapSpot(bool isLock, Action<int> onClickAction, AdventureStageData adventureStageData, OZAdventure ozAdventure)
    {
        Lock = isLock;

        _onClickAction = onClickAction;
        _adventureStageData = adventureStageData;
        _ozAdventure = ozAdventure;

        SetStar();

        var adventureChapterData = DataTable.AdventureChapterDataTable[_adventureStageData.AdventureChapter];

        _stageNumber.text =
            $"{adventureChapterData.ChapterNumber}-{_adventureStageData.StageNumber}";

        if (adventureChapterData.ChapterDifficultType == ChapterDifficultType.Hard)
        {
            if (_hardRewardRoot != null)
            {
                _hardRewardRoot.SetActive(!Lock);

                var itemData = DataTable.ItemDataTable.GetById(_adventureStageData.ClearReward2);
                if (itemData == null)
                {
                    DebugHelper.LogError($"아이템 데이터를 찾을 수 없습니다. 아이템 아이디 {_adventureStageData.ClearReward2}");
                }
                else
                {
                    _hardRewardIcon.sprite = AtlasManager.GetItemIcon(itemData.IconPath);
                }

                _hardRewardCurrentCount.text = $"{ozAdventure?.ClearCount ?? 0}";
                _hardRewardTargetCount.text = $"/{DataTable.BattleModeInfoDataTable[BattleModeType.AdventureMode].BattleLimit}";
            }
        }
    }

    private void SetStar()
    {
        for (int i = 0; i < _stars.Length; i++)
        {
            _stars[i].gameObject.SetActive(AdventureModeUtil.IsClearMission(_ozAdventure, i));
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

    public async UniTask UnLockEffect()
    {
        if (_unlockEffect == null)
        {
            return;;
        }

        _eventSystem.enabled = false;
        _unlockEffect.SetActive(true);

        await UniTask.Delay((int)(_unlockParticlesTime * 1000), true);
        
        _unlockEffect.SetActive(false);
        _eventSystem.enabled = true;
    }
}