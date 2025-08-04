using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 맵 제작을 위한 테스트 환경
/// </summary>
public class StageMapTestScenePerspective : MonoBehaviour
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

    [Header("캐릭터 이동 속도(테스트전용)")]
    [SerializeField]
    private float _moveSpeed = 5.0f;
    
    [SerializeField]
    private Transform _characterTransform;

    // [Header("카메라 이동 속도")]
    // [SerializeField]
    // private float _followCameraMove = 1.5f;
    //
    // [Header("카메라 Y 오프셋")]
    // [SerializeField]
    // private float _cameraOffsetY = -5.0f;
    //
    // [Header("캐릭터 크기 조정 여부")]
    // [SerializeField]
    // private bool _characterScaleAdjust;
    //
    // [Header("팔로우 카메라 여부")]
    // [SerializeField]
    // private bool _followCamera;
    //
    // [Header("카메라 거리 조정 여부")]
    // [SerializeField]
    // private bool _cameraScaleAdjust;
    //
    // [Header("스테이지")]
    // [SerializeField]
    // private List<GameObject> stages;

    private Camera _camera;
    
    private Animator _characterAnimator;

    private StageMapPerspectiveModifier[] _stageMapLayers;
    
    private byte _keyValue = 0;
    private Direction _currentDirection = Direction.None;
    private Direction _previousDirection = Direction.None;

    private float _originalCharacterScale;
    private float _originalCameraSize;
    private float _oroginalCameraFov;

    private (float, float) _movementValue;

    private bool _isMouseClick;
    private Vector3 _clickPosition;
    private Vector3 _destination;
    
    // RigidBody.
    private Rigidbody2D _characterRigidBody2D;
    
    // Spot.
    private int _currentStageindex;
    private int? _targetStageindex;

    private void Awake()
    {
        _camera = Camera.main;
        _originalCameraSize = _camera.orthographicSize;
        _oroginalCameraFov = _camera.fieldOfView;

        _characterAnimator = _characterTransform.GetComponentInChildren<Animator>();
        _originalCharacterScale = _characterTransform.localScale.x;
        _destination = _characterTransform.position;

        // float distance = Vector3.Distance(_characterAnimator.transform.localPosition, _camera.transform.localPosition);
        // _cameraOffsetY = Mathf.Abs(Mathf.Tan(_camera.transform.rotation.eulerAngles.x) * distance);
        
        _stageMapLayers = GetComponentsInChildren<StageMapPerspectiveModifier>(false);
    }

    #region RigidBody.
    // private void OnEnable()
    // {
    //     if (_characterRigidBody2D != null)
    //     {
    //         return;
    //     }
    //     
    //     if (_characterAnimator.gameObject.TryGetComponent(out _characterRigidBody2D))
    //     {
    //         return;
    //     }
    //     
    //     _characterAnimator.gameObject.AddComponent<Rigidbody2D>();
    //     _characterRigidBody2D = _characterAnimator.gameObject.GetComponent<Rigidbody2D>();
    //     _characterRigidBody2D.gravityScale = 0.0f;
    //     _characterRigidBody2D.isKinematic = false;
    // }
    //
    // private void OnDisable()
    // {
    //     if (_characterAnimator.gameObject.GetComponent<Rigidbody2D>())
    //     {
    //         _characterAnimator.gameObject.RemoveComponent<Rigidbody2D>();
    //     }
    // }
    #endregion

    private void Update()
    {
        // _currentDirection = Direction.None;
        // _keyValue = 0;

        //CheckClickSpot();
        CalcMouseClick();
            
        //CalcDirectionByKeyboard();
        CalcDirectionByMouseClick();

        //CalcCharacterAnimationByKeyboard();
        CalcCharacterAnimationByMouse();

        if (!Input.anyKey)
        {
            _currentDirection = Direction.None;
            _keyValue = 0;
            _movementValue = (0, 0);
            
            return;
        }
        
        _movementValue = CalcMoveValue();
    }

    private void LateUpdate()
    {
        CalcCharacterMoveByMouseClick();
        //MoveCharacterByKeyboard(_movementValue.Item1, _movementValue.Item2);
        
        // if (!_followCamera)
        // {
        //     MoveCameraByMouse();
        //     //MoveCameraByKeyboard(_movementValue.Item1, _movementValue.Item2);
        // }
        // else
        // {
        //     MoveFollowCamera();
        // }
        //
        // if (_cameraScaleAdjust)
        // {
        //     AdjustCameraScale();
        // }
        //
        // if (_characterScaleAdjust)
        // {
        //     AdjustCharacterScale();
        // }

        //AdjustEnvironmentMove();
    }

    private void CalcMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = new(Input.mousePosition.x, Input.mousePosition.y, _camera.nearClipPlane);
            _clickPosition = _camera.ViewportToWorldPoint(mousePosition);
            
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Map"))
                {
                    _isMouseClick = true;
                    _clickPosition = hit.point;
                }
            }
        }

        if (!_isMouseClick)
        {
            return;
        }

        var characterObject = _characterTransform.gameObject;
        var characterPosition = characterObject.transform.position;
        _clickPosition.z = characterPosition.z;
        
        _destination = Vector3.MoveTowards(characterPosition, _clickPosition, Time.deltaTime * _moveSpeed);

        if (_characterTransform.gameObject.transform.position == _clickPosition)
        {
            _isMouseClick = false;
        }
    }
    private void CheckClickSpot()
    {
        // if (Input.GetMouseButtonDown(0))
        // {
        //     Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        //     if (Physics.Raycast(ray, out RaycastHit hit))
        //     {
        //         if (hit.collider.CompareTag("LandMark"))
        //         {
        //             _isMouseClick = true;
        //             _clickPosition = hit.point;
        //
        //             int index = stages.FindIndex(x => x == hit.collider.gameObject);
        //             _targetStageindex = (index > 0) ? index : null;
        //         }
        //     }
        // }
        //
        // if (!_isMouseClick)
        // {
        //     return;
        // }
        //
        // var characterObject = _characterAnimator.gameObject;
        // var characterPosition = characterObject.transform.position;
        // _clickPosition.z = characterPosition.z;
        //
        // _destination = Vector3.MoveTowards(characterPosition, _clickPosition, Time.deltaTime * _moveSpeed);
        //
        // if (_characterAnimator.gameObject.transform.position == _clickPosition)
        // {
        //     _isMouseClick = false;
        // }
    }

    private void CalcDirectionByMouseClick()
    {
        if (!_isMouseClick)
        {
            return;
        }
        
        //float angle = Vector3.SignedAngle(_characterAnimator.gameObject.transform.forward, _destination, Vector3.forward);
        //float angle = Vector3.Angle(_destination - _characterAnimator.gameObject.transform.position, Vector3.up);
        Vector2 direction = _destination - _characterTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        switch (angle)
        {
            case >= 80.0f and <= 110.0f:
                _currentDirection = Direction.Up;
                _keyValue |= (int)Direction.Up;
                break;
            case >= -10.0f and <= 10.0f:
                _currentDirection = Direction.Right;
                _keyValue |= (int)Direction.Right;
                break;
            case >= -100.0f and <= -80.0f:
                _currentDirection = Direction.Down;
                _keyValue |= (int)Direction.Down;
                break;
            default:
                {
                    if ((angle is >= 170.0f and <= 180.0f) && (angle is <= -170.0f and >= -180.0f))
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

                    break;
                }
        }
    }
    private void CalcDirectionByKeyboard()
    {
        if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) && (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)))
        {
            _currentDirection = Direction.LeftUp;
            _keyValue |= (int)Direction.LeftUp;
        }
        else if ((Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) && (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)))
        {
            _currentDirection = Direction.RightUp;
            _keyValue |= (int)Direction.RightUp;
        }
        else if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) && (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)))
        {
            _currentDirection = Direction.LeftDown;
            _keyValue |= (int)Direction.LeftDown;
        }
        else if ((Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) && (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)))
        {
            _currentDirection = Direction.RightDown;
            _keyValue |= (int)Direction.RightDown;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            _currentDirection = Direction.Left;
            _keyValue |= (int)Direction.Left;
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            _currentDirection = Direction.Right;
            _keyValue |= (int)Direction.Right;
        }
        else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            _currentDirection = Direction.Up;
            _keyValue |= (int)Direction.Up;
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            _currentDirection = Direction.Down;
            _keyValue |= (int)Direction.Down;
        }
    }

    private void CalcCharacterAnimationByMouse()
    {
        if (!_isMouseClick)
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
    private void CalcCharacterAnimationByKeyboard()
    {
        if (_previousDirection == _currentDirection)
        {
            return;
        }

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
                break;
        }
        
        _previousDirection = _currentDirection;
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

    private void MoveCameraByMouse()
    {
        // var characterPosition = _characterAnimator.transform.position;
        // var cameraPosition = _camera.transform;
        //
        // cameraPosition.position = new Vector3(characterPosition.x, characterPosition.y + _cameraOffsetY, cameraPosition.position.z);;
    }
    private void MoveCameraByKeyboard(float x, float y)
    {
        var cameraObject = _camera.gameObject;
        var position = cameraObject.transform.position;

        cameraObject.transform.position = new Vector3(position.x + x, position.y + y, position.z);;
    }

    private void MoveFollowCamera()
    {
        // var cameraObject = _camera.gameObject;
        // var cameraPosition = cameraObject.transform.position;
        // var characterPosition = _characterAnimator.gameObject.transform.position;
        // var targetPosition = new Vector3(characterPosition.x, characterPosition.y + _cameraOffsetY, cameraPosition.z);
        //
        // cameraPosition = Vector3.Lerp(cameraPosition, targetPosition, (_followCameraMove * Time.deltaTime));
        //
        // cameraObject.transform.position = cameraPosition;
    }
    
    private void AdjustCameraScale()
    {
        var cameraGameObject = _camera.gameObject;
        float scaleFactor;
        
        if (_camera.orthographic)
        {
            scaleFactor = _originalCameraSize - (cameraGameObject.transform.position.y / _camera.orthographicSize);
            _camera.orthographicSize = scaleFactor;
        }
        else
        {
            scaleFactor = _oroginalCameraFov + cameraGameObject.transform.position.y;
            _camera.fieldOfView = scaleFactor;
        }
    }

    private void MoveCharacterByKeyboard(float x, float y)
    {
        var characterObject = _characterTransform.gameObject;
        var position = characterObject.transform.position;
        
        position = new Vector3(position.x + x, position.y + y, position.z);
        
        characterObject.transform.position = position;
    }

    private void CalcCharacterMoveByMouseClick()
    {
        //_characterRigidBody2D.MovePosition(_destination);
        
        _characterTransform.position = _destination;
    }

    private void AdjustCharacterScale()
    {
        var characterObject = _characterTransform.gameObject;
        float scaleFactor = _originalCharacterScale - (characterObject.transform.position.y / (_camera.orthographicSize * 4.0f));
        
        // if (_camera.orthographic)
        // {
        //     scaleFactor = _originalCharacterScale - (characterObject.transform.position.y / (_camera.orthographicSize * 4.0f));
        // }
        // else
        // {
        //     scaleFactor = _originalCharacterScale - characterObject.transform.position.y;
        // }

        characterObject.transform.localScale = new Vector3(scaleFactor, scaleFactor, characterObject.transform.localScale.z);
    }

    private void AdjustEnvironmentMove()
    {
        for (int i = 0; i < _stageMapLayers.Length; i++)
        {
            var characterPosition = _characterTransform.position;
            float x = characterPosition.x;
            float y = characterPosition.y;
        
            float backGroundModifierX = x - (x * _stageMapLayers[i].modifier);
            float backGroundModifierY = y - (y * _stageMapLayers[i].modifier);
        
            float foreGroundModifierX = x - (x * _stageMapLayers[i].modifier);
            float foreGroundModifierY = y - (y * _stageMapLayers[i].modifier);
        
            float nearGroundModifierX = x - (x * _stageMapLayers[i].modifier);
            float nearGroundModifierY = y - (y * _stageMapLayers[i].modifier);
        
            _stageMapLayers[i].gameObject.transform.position = new Vector3(backGroundModifierX, backGroundModifierY, _stageMapLayers[i].gameObject.transform.position.z);
            _stageMapLayers[i].gameObject.transform.position = new Vector3(foreGroundModifierX, foreGroundModifierY, _stageMapLayers[i].gameObject.transform.position.z);
            _stageMapLayers[i].gameObject.transform.position = new Vector3(nearGroundModifierX, nearGroundModifierY, _stageMapLayers[i].gameObject.transform.position.z);
        }
    }
}