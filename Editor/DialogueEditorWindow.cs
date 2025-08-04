using Cysharp.Threading.Tasks;
using Data;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Dialogue
{
#if UNITY_EDITOR
    public class DialogueEditorWindow : OdinEditorWindow, DialogueEditorData.IDialogueEditorWindow
    {
        private static DialogueEditorWindow _window;
        
        private DialoguePlayer _dialoguePlayer;

        private CancellationTokenSource _cancellationTokenSource;

        private bool _processing;

        protected override void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!string.IsNullOrEmpty(_loadedId))
            {
                SetViewerData();
                
                _groupId = $"그룹 아이디 -> {_loadedId}";
            }
        }

        protected void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    {
                        Initialize();
                    }
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    {
                        Release();
                    }
                    break;
            }
        }

        private void Initialize()
        {
            DataTable<DialogueDataTable>.OnReloaded += Reload;
        }

        private void Release()
        {
            DataTable<DialogueDataTable>.OnReloaded -= Reload;
            
            _dialoguePlayer = null;
            _cancellationTokenSource = null;
            
            //_groupId = _previousId = _loadedId = string.Empty;
            _groupId = _loadedId = string.Empty;
            CurrentOrder = 1;
        }

        [MenuItem("Window/Macovill/대화편집 툴")]
        private static void OpenWindow()
        {
            _window = GetWindow<DialogueEditorWindow>();

            _window.titleContent = new GUIContent("대화편집 툴");

            EditorApplication.EnterPlaymode();
        }

        #region Viewer.
        //[TabGroup("ModeTab", VisibleIf = "IsEditorMode")]
        [ShowIf("@!UnityEngine.Application.isPlaying")]
        [InfoBox("플레이 모드로 진입해주세요.", InfoMessageType.Warning)]
        [Button(ButtonSizes.Gigantic, Name = "플레이")]
        private void PlayEditor()
        {
            EditorApplication.EnterPlaymode();
        }

        private string _loadedId = string.Empty;
        [TabGroup("ModeTab", "뷰어", VisibleIf = "IsPlayingMode"), GUIColor(0.96f, 0.96f, 0.86f)]
        [ValueDropdown("LoadDialogueTable")]
        [HideLabel]
        [ShowInInspector]
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 0), PropertyOrder(0)]
        public string LoadedId
        {
            get => _loadedId;
            set
            {
                if (EditorUtility.DisplayDialog("불러오기", $"{value.Replace("\t", "")} 데이터를 로딩하겠습니까?", "네", "아니오") == false)
                {
                    return;
                }

                _loadedId = value;

                _groupId = $"그룹 아이디 -> {_loadedId}";

                RefreshData();
            }
        }

        // [HideLabel]
        // private string _previousId = string.Empty;
        //
        // private void CheckSelectId()
        // {
        //     if (string.Equals(_previousId, _loadedId) == true)
        //     {
        //         return;
        //     }
        //     
        //     if (EditorUtility.DisplayDialog("불러오기", $"{_loadedId.Replace("\t", "")} 데이터를 로딩하겠습니까?", "네", "아니오") == false)
        //     {
        //         _loadedId = _previousId;
        //         
        //         return;
        //     }
        //
        //     _previousId = _loadedId;
        //
        //     _groupId = $"그룹 아이디 -> {_loadedId}";
        //
        //     RefreshData();
        // }

        private static IEnumerable LoadDialogueTable()
        {
            if (Application.isPlaying == false)
            {
                return null;
            }
            
            return DataTable.DialogueDataTable != null ? DataTable.DialogueDataTable.Select(x => x.GroupId) : null;
        }

        [ShowIf("@!string.IsNullOrEmpty(_loadedId)")]
        [HorizontalGroup("ModeTab/뷰어/Info", width: 150.0f)]
        [LabelText("현재 오더")][LabelWidth(100)]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 0), PropertyOrder(1), GUIColor(1.0f, 1.0f, 1.0f)]
        public int CurrentOrder = 1;

        [DisableIf("@string.IsNullOrEmpty(_loadedId) || _processing")]
        [ResponsiveButtonGroup("ModeTab/뷰어/Info/PlayStop")]
        [Button(ButtonSizes.Medium, Name = "대화 재생"), GUIColor(0.53f, 0.8f, 0.92f)]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 0), PropertyOrder(1)]
        private void PlayButtonViewer()
        {
            PlayViewer().Forget();
        }

        [DisableIf("@string.IsNullOrEmpty(_loadedId) || _processing")]
        [ResponsiveButtonGroup("ModeTab/뷰어/Info/PlayStop")]
        [Button(ButtonSizes.Medium, Name = "대화 중지"), GUIColor(1.0f, 0.5f, 0.44f)]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 0), PropertyOrder(1)]
        private void StopButtonViewer()
        {
            StopViewer().Forget();
        }

        private async UniTask PlayViewer()
        {
            if (_processing == true)
            {
                return;
            }
            
            await StopViewer();

            _cancellationTokenSource ??= new CancellationTokenSource();
            
            _processing = true;

            try
            {
                await _dialoguePlayer.Load(_loadedId, _loadedId);
            }
            finally
            {
                _processing = false;
            }

            await _dialoguePlayer.Play(CurrentOrder, _cancellationTokenSource);
        }

        private async UniTask StopViewer()
        {
            if (_processing == true)
            {
                return;
            }
            
            _dialoguePlayer ??= FindObjectOfType<DialoguePlayer>();

            _processing = true;

            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

                await _dialoguePlayer.Release();
            }
            finally
            {
                _processing = false;
            }
        }

        public void SetOrder(int order)
        {
            CurrentOrder = order;

            _groupId = $"그룹 아이디 -> {_loadedId}";
        }

        public void PlayImmediately(int order)
        {
            CurrentOrder = order;

            _groupId = $"그룹 아이디 -> {_loadedId}";

            PlayButtonViewer();
        }

        [ShowIf("@!string.IsNullOrEmpty(_loadedId)")]
        [BoxGroup("ModeTab/뷰어/테이블 데이터")]
        [HideLabel]
        [LabelText("@_groupId")]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 0), PropertyOrder(2)]
        [ShowInInspector]
        public List<DialogueEditorData.ViewerData> ViewData { get; } = new();

        private string _groupId = string.Empty;

        private void SetViewerData()
        {
            ViewData.Clear();
            foreach (var tableData in DataTable.DialogueDataTable.GetGroupByGroupId(_loadedId))
            {
                ViewData.Add(new DialogueEditorData.ViewerData(tableData));
            }
        }
        #endregion

        #region Editor.
        [TabGroup("ModeTab", "편집", VisibleIf = "IsPlayingMode")]
        [HideLabel]
        [InfoBox("공사중", InfoMessageType.Warning)]
        [ReadOnly]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 0), PropertyOrder(1)]
        public string UnderConstruction;
        #endregion

        #region 테이블 데이터 갱신 액션.
        private void Reload()
        {
            RefreshData();
        }
        private void RefreshData()
        {
            StopViewer().Forget();

            SetViewerData();

            this.Repaint();
        }
        #endregion
        
        //에디터 플레이 체크.
        private bool IsPlayingMode()
        {
            return (Application.isPlaying == true);
        }

        private bool IsEditorMode()
        {
            return (Application.isPlaying == false);
        }
    }
#endif
}