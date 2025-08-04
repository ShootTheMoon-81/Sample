using Cinemachine;
using DG.Tweening;
using MessageSystem;
using System;
using System.Collections.Generic;
using UI.Messages;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class StageMapController : MonoBehaviour
{
    private enum Direction
    {
        None,
        Left = 1,
        Right = 2,
        Up = 4, 
        Down = 8,
        LeftUp = 16,
        RightUp = 32,
        LeftDown = 64,
        RightDown = 128
    }

    public struct BoundBox
    {
        private float _minX;
        private float _maxX;
        private float _minY;
        private float _maxY;

        public float MinX
        {
            get => _minX + OffSetX;
            set => _minX = value;
        }
        public float MaxX
        {
            get => _maxX + OffSetX;
            set => _maxX = value;
        }
        public float MinY
        {
            get => _minY + OffSetY;
            set => _minY = value;
        }
        public float MaxY
        {
            get => _maxY + OffSetY;
            set => _maxY = value;
        }

        public float OffSetX;
        public float OffSetY;
    }

    [SerializeField]
    private float _moveSpeed = 5.0f;

    [SerializeField]
    private CinemachineVirtualCamera _mainCamera;

    [SerializeField]
    private CinemachineVirtualCamera _dragCamera;
    
    [SerializeField]
    private GameObject _dragTarget;

    [SerializeField]
    private Transform _characterTransform;

    [SerializeField]
    private float _characterShadowOffset = 0.4f;

    [SerializeField]
    private StageMapGround _stageMapGround;

    [SerializeField]
    private GameObject _normalRoot;

    [SerializeField]
    private GameObject _hardRoot;
    
    [SerializeField]
    private List<StageMapSpot> _normalStages;

    [SerializeField]
    private List<StageMapSpot> _hardStages;
    
    [HideInInspector]
    public DOTweenPath[] _tweenPaths;

    public List<StageMapSpot> Stages
    {
        get
        {
            switch (_currentDifficultType)
            {
                case ChapterDifficultType.Hard:
                    {
                        return _hardStages;
                    }
                case ChapterDifficultType.None:
                case ChapterDifficultType.Normal:
                case ChapterDifficultType.Max:
                    {
                        return _normalStages;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    private int _currentStageIndex;
    private int? _passStageIndex;
    private int? _targetStageIndex;
    
    private byte _keyValue = 0;
    private Direction _currentDirection = Direction.None;
    private Direction _previousDirection = Direction.None;
    
    private Animator _characterAnimator;
    private Vector3 _destination;
    private (float, float) _movementValue;

    private bool _isFinishBattleReady = true;

    private ChapterDifficultType _currentDifficultType;
    
    // FIXME: 드래그 기능 급하게 땜빵 후에 정확히 계산 할 것
    private string _stageMapName;
    public string StageMapName
    {
        get => _stageMapName;
        set
        {
            if (_currentDifficultType == ChapterDifficultType.Normal)
            {
                _boundBox = _normalBoundBoxes.ContainsKey(value) ?
                    _normalBoundBoxes[value] : new BoundBox(){ MinX = 5.5f, MaxX = 83.0f, MinY = -15.0f, MaxY = 9.0f };
            }
            else
            {
                _boundBox = _hardBoundBoxes.ContainsKey(value) ?
                    _hardBoundBoxes[value] : new BoundBox(){ MinX = 5.5f, MaxX = 83.0f, MinY = -15.0f, MaxY = 9.0f };
            }

            _stageMapName = value;
        }
    }
    private Dictionary<string, BoundBox> _normalBoundBoxes = new();
    private Dictionary<string, BoundBox> _hardBoundBoxes = new();
    private BoundBox _boundBox;
    
    private GameObject _shadowObject;
    private Transform _shadowTransform;

    private void OnValidate()
    {
        _tweenPaths = GetComponentsInChildren<DOTweenPath>(true);
        for (int i = 0; i < _tweenPaths.Length; i++)
        {
            _tweenPaths[i].relative = true;
        }
    }

    private void Awake()
    {
        _characterAnimator = _characterTransform.GetComponentInChildren<Animator>();
        
        _shadowObject = Addressables.InstantiateAsync("Assets/Data/Common/Prefab/shadow.prefab").WaitForCompletion();
        _shadowTransform = _shadowObject.transform;

        _shadowTransform.SetParent(_characterTransform);
        _shadowTransform.localPosition = new Vector3(0.0f, _characterShadowOffset, 0.0f);

        _mainCamera.Follow = _characterTransform;
        
        _stageMapGround.SetStageMapGround(OnBeginDrag, OnDragEvent, null);

        // HACK: 없어져야 할 코드
        _normalBoundBoxes.Add("Assets/Data/Environment/StageMap/Cellarn01/Cellarn01_StageMap.prefab", new BoundBox()
        {
            MinX = 7.5f, MaxX = 85.0f,
            MinY = -15.0f, MaxY = 10.0f
        });
        _normalBoundBoxes.Add("Assets/Data/Environment/StageMap/Cellarn02/Cellarn02_StageMap.prefab", new BoundBox()
        {
            MinX = 5.0f, MaxX = 90.0f,
            MinY = -20.0f, MaxY = 9.0f
        });
        _normalBoundBoxes.Add("Assets/Data/Environment/StageMap/BeastRim01/BeastRim01_StageMap.prefab", new BoundBox()
        {
            MinX = 12.0f, MaxX = 83.0f,
            MinY = -18.0f, MaxY = 12.0f
        });
        _hardBoundBoxes.Add("Assets/Data/Environment/StageMap/Cellarn01/Cellarn01_StageMap.prefab", new BoundBox()
        {
            MinX = 7.5f, MaxX = 25.0f,
            MinY = 0.0f, MaxY = 10.0f
        });
    }

    private void OnEnable()
    {
        _mainCamera.gameObject.SetActive(true);
        _dragCamera.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Addressables.Release(_shadowObject);
        
        _normalBoundBoxes.Clear();
        _hardBoundBoxes.Clear();
    }

    private void Update()
    {
        _currentDirection = Direction.None;
        _keyValue = 0;

        //CheckClickSpot();

        CalcPathIndex();

        CalcDirectionByClick();

        CalcCharacterAnimationByClick();

        if (!Input.anyKey)
        {
            return;
        }

        _movementValue = CalcMoveValue();
    }
    
    private void LateUpdate()
    {
        CalcCharacterPositionByClick();
    }

    public void SetDifficulty(ChapterDifficultType chapterDifficultType)
    {
        _currentDifficultType = chapterDifficultType;
        
        switch (_currentDifficultType)
        {
            case ChapterDifficultType.Hard:
                {
                    _normalRoot.SetActive(false);
                    _hardRoot.SetActive(true);
                }
                break;
            case ChapterDifficultType.None:
            case ChapterDifficultType.Normal:
            case ChapterDifficultType.Max:
                {
                    _normalRoot.SetActive(true);
                    _hardRoot.SetActive(false);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(chapterDifficultType), chapterDifficultType, null);
        }
    }

    public void SetMapPosition(Vector2 addPosition)
    {
        transform.position = new Vector3(addPosition.x, addPosition.y, transform.position.z);

        _boundBox.OffSetX = addPosition.x;
        _boundBox.OffSetY = addPosition.y;
        
        for (int i = 0; i < _tweenPaths.Length; i++)
        {
            _tweenPaths[i].DORestart(true);
        }
    }

    public void SetCharacterPosition(int spotNumber)
    {
        _currentStageIndex = spotNumber < 0 ? 0 : spotNumber;
        
        _destination = Stages[_currentStageIndex].transform.position;
    }
    
    private void CheckClickSpot()
    {
        if (_isFinishBattleReady == false)
        {
            return;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = CameraManager.Instance.MainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("LandMark"))
                {
                    if (_targetStageIndex.HasValue == true)
                    {
                        return;
                    }
                    
                    int clickIndex = Stages.FindIndex(x => x.gameObject == hit.collider.gameObject);
                    if (Stages[clickIndex].Lock == true)
                    {
                        return;
                    }

                    _isFinishBattleReady = false;
                    _targetStageIndex = clickIndex;
                    
                    if (_currentStageIndex == _targetStageIndex)
                    {
                        _targetStageIndex = _passStageIndex = null;
                        
                        MessageService.Instance.Publish(UISelectPanelBattleStartRequest.Create(
                            _currentStageIndex,
                            (x) => { _isFinishBattleReady = x; }));
                        
                        return;
                    }

                    _passStageIndex = (_currentStageIndex < _targetStageIndex) ? _currentStageIndex + 1 : _currentStageIndex - 1;
                }
            }
        }
    }

    public void OnClickStageSpot(int clickIndex)
    {
        if (_targetStageIndex.HasValue == true)
        {
            return;
        }
        
        _dragCamera.gameObject.SetActive(false);

        _isFinishBattleReady = false;
        _targetStageIndex = clickIndex;
                    
        if (_currentStageIndex == _targetStageIndex)
        {
            _targetStageIndex = _passStageIndex = null;

            MessageService.Instance.Publish(UISelectPanelBattleStartRequest.Create(
                _currentStageIndex,
                (x) => { _isFinishBattleReady = x; }));
                        
            return;
        }

        _moveSpeed += Mathf.Abs((float)_targetStageIndex - _currentStageIndex);
        
        _passStageIndex = (_currentStageIndex < _targetStageIndex) ? _currentStageIndex + 1 : _currentStageIndex - 1;
    }

    private void CalcPathIndex()
    {
        if (_targetStageIndex.HasValue == false)
        {
            return;
        }

        _destination = Vector3.MoveTowards(
            _characterTransform.position,
            Stages[_passStageIndex.GetValueOrDefault()].transform.position,
            Time.deltaTime * _moveSpeed);

        if (_characterTransform.position != Stages[_passStageIndex.GetValueOrDefault()].transform.position)
        {
            return;
        }

        if (_passStageIndex == _targetStageIndex)
        {
            _currentStageIndex = _targetStageIndex.GetValueOrDefault();
            _targetStageIndex = _passStageIndex = null;
            
            _moveSpeed = 5.0f;

            MessageService.Instance.Publish(UISelectPanelBattleStartRequest.Create(
                                        _currentStageIndex,
                                        (x) => { _isFinishBattleReady = x; }));
        }
        else
        {
            _currentStageIndex = _passStageIndex.GetValueOrDefault();
            _passStageIndex = (_currentStageIndex < _targetStageIndex) ? _currentStageIndex + 1 : _currentStageIndex - 1;
        }
    }
    
    private void CalcDirectionByClick()
    {
        if (_targetStageIndex.HasValue == false)
        {
            return;
        }

        Vector2 direction = _destination - _characterTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        switch (angle)
        {
            case >= 80.0f and <= 110.0f:
                {
                    _currentDirection = Direction.Up;
                    _keyValue |= (int)Direction.Up;
                }
                break;
            case >= -10.0f and <= 10.0f:
                {
                    _currentDirection = Direction.Right;
                    _keyValue |= (int)Direction.Right;
                }
                break;
            case >= -100.0f and <= -80.0f:
                {
                    _currentDirection = Direction.Down;
                    _keyValue |= (int)Direction.Down;
                }
                break;
            default:
                {
                    if ((angle is >= 170.0f and <= 180.0f or <= -170.0f and >= -180.0f))
                    {
                        _currentDirection = Direction.Left;
                        _keyValue |= (int)Direction.Left;
                    }
                    else switch (angle)
                    {
                        case > 10.0f and < 80.0f:
                            _currentDirection = Direction.RightUp;
                            _keyValue |= (int)Direction.RightUp;
                            break;
                        case > 110.0f and < 170.0f:
                            _currentDirection = Direction.LeftUp;
                            _keyValue |= (int)Direction.LeftUp;
                            break;
                        case > -170.0f and < -100.0f:
                            _currentDirection = Direction.LeftDown;
                            _keyValue |= (int)Direction.LeftDown;
                            break;
                        case > -80.0f and < -10.0f:
                            _currentDirection = Direction.RightDown;
                            _keyValue |= (int)Direction.RightDown;
                            break;
                    }
                }
                break;
        }
    }
    
    private void CalcCharacterAnimationByClick()
    {
        if (_targetStageIndex.HasValue == false)
        {
            _previousDirection = _currentDirection;
            
            switch (_previousDirection)
            {
                case Direction.Left:
                    _characterAnimator.Play("Idle_LF");
                    break;
                case Direction.Right:
                    _characterAnimator.Play("Idle_RF");
                    break;
                case Direction.Up:
                    _characterAnimator.Play("Idle_LB");
                    break;
                case Direction.Down:
                    _characterAnimator.Play("Idle_LB");
                    break;
                case Direction.LeftUp:
                    _characterAnimator.Play("Idle_LB");
                    break;
                case Direction.RightUp:
                    _characterAnimator.Play("Idle_RB");
                    break;
                case Direction.LeftDown:
                    _characterAnimator.Play("Idle_LB");
                    break;
                case Direction.RightDown:
                    _characterAnimator.Play("Idle_RB");
                    break;
                case Direction.None:
                default:
                    _characterAnimator.Play("Idle_LF");
                    break;
            }
        }
        else
        {
            switch (_currentDirection)
            {
                case Direction.Left:
                    _characterAnimator.Play("Walk_LF");
                    break;
                case Direction.Right:
                    _characterAnimator.Play("Walk_RF");
                    break;
                case Direction.Up:
                    _characterAnimator.Play("Walk_LB");
                    break;
                case Direction.Down:
                    _characterAnimator.Play("Walk_LF");
                    break;
                case Direction.LeftUp:
                    _characterAnimator.Play("Walk_LB");
                    break;
                case Direction.RightUp:
                    _characterAnimator.Play("Walk_RF");
                    break;
                case Direction.LeftDown:
                    _characterAnimator.Play("Walk_LF");
                    break;
                case Direction.RightDown:
                    _characterAnimator.Play("Walk_RF");
                    break;
                case Direction.None:
                default:
                    break;
            }
        }
    }
    
    private (float, float) CalcMoveValue()
    {
        float moveX = 0.0f;
        float moveY = 0.0f;
        
        if ((_keyValue & 1) != 0)
        {
            moveX -= (_moveSpeed * Time.deltaTime);
        }
        else if ((_keyValue & 2 ) != 0)
        {
            moveX += (_moveSpeed * Time.deltaTime);
        }
        else if ((_keyValue & 4) != 0)
        {
            moveY += (_moveSpeed * Time.deltaTime);
        }
        else if ((_keyValue & 8) != 0)
        {
            moveY -= (_moveSpeed * Time.deltaTime);
        }
        else if ((_keyValue & 16) != 0)
        {
            moveX -= (_moveSpeed * Time.deltaTime);
            moveY += (_moveSpeed * Time.deltaTime);
        }
        else if ((_keyValue & 32 ) != 0)
        {
            moveX += (_moveSpeed * Time.deltaTime);
            moveY += (_moveSpeed * Time.deltaTime);
        }
        else if ((_keyValue & 64) != 0)
        {
            moveX -= (_moveSpeed * Time.deltaTime);
            moveY -= (_moveSpeed * Time.deltaTime);
        }
        else if ((_keyValue & 128) != 0)
        {
            moveX += (_moveSpeed * Time.deltaTime);
            moveY -= (_moveSpeed * Time.deltaTime);
        }
        
        return (moveX, moveY);
    }
    
    private void CalcCharacterPositionByClick()
    {
        _characterTransform.position = _destination;
    }

    private void OnBeginDrag()
    {
        if (_dragCamera.gameObject.IsActiveInHierarchy() != false)
        {
            return;
        }

        _dragTarget.transform.position = _characterTransform.position;
        _dragCamera.gameObject.SetActive(true);
    }

    private void OnDragEvent(Vector2 target)
    {
        // FIXME: 급하게 땜빵
        float x = _dragTarget.transform.position.x - (target.x * 0.1f);
        switch (target.x)
        {
            case < 0.0f when x > _boundBox.MaxX:
            case >= 0.0f when x < _boundBox.MinX:
                x = _dragTarget.transform.position.x;
                break;
        }

        float y = _dragTarget.transform.position.y - (target.y * 0.1f);
        switch (target.y)
        {
            case < 0.0f when y > _boundBox.MaxY:
            case >= 0.0f when y < _boundBox.MinY:
                y = _dragTarget.transform.position.y;
                break;
        }
        
        _dragTarget.transform.position = new Vector3(x, y, 0.0f);
        
        //_dragTarget.transform.position -= new Vector3(target.x, target.y, 0.0f) * 0.1f;
    }

    public void SetInteraction(bool interaction)
    {
        _isFinishBattleReady = interaction;
    }
}