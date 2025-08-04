using Cysharp.Threading.Tasks;
using Data;
using DG.Tweening;
using Extensions;
using Managers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UI.Overlay;
using UnityEngine;

namespace Dialogue
{
    public class DialoguePlayer : MonoBehaviour
    {
        [SerializeField]
        private DialogueBackground _background;
        
        [SerializeField]
        private DialogueActor[] _actors;
        
        [SerializeField]
        private DialogueScreen _screen;
        
        [SerializeField]
        private DialogueTextBox _textBox;

        [SerializeField]
        private DialogueChoices _choices;
        
        #region UI
        [SerializeField]
        private GameObject _hudRoot;
        
        [SerializeField]
        private GameObject _hideButton;
        [SerializeField]
        private GameObject _showButton;

        [SerializeField]
        private GameObject _rootSideButton;
        [SerializeField]
        private GameObject _logButton;
        [SerializeField]
        private GameObject _autoButton;
        [SerializeField]
        private DOTweenAnimation _autoButtonAnimation;
        [SerializeField]
        private GameObject _skipButton;

        [SerializeField]
        private GameObject _touchCursor;
        #endregion

        private string _currentDialogueGroupId;
        private Dictionary<int, DialogueDirection> _directions;
        private Dictionary<int, List<DialogueDirection>> _contexts;

        private Stack<List<DialogueDirection>.Enumerator> _contextStack = new();

        private bool _input;
        
        private CancellationTokenSource _cancellationTokenSource;
        
        public bool IsAutoPlay { get; set; }
        
        public async UniTask Load(string groupId, string title)
        {
            var dialogueGroup = DataTable.DialogueDataTable.GetGroupByGroupId(groupId);

            _currentDialogueGroupId = groupId;
            
            _hudRoot.SetActive(false);
            _touchCursor.SetActive(false);
            _screen.Camera.gameObject.SetActive(false);
            
            _textBox.SetStyle(1);
            
            _contexts = new();
            _directions = new(dialogueGroup.Count + 1);

            foreach (var data in dialogueGroup)
            {
                if (_contexts.TryGetValue(data.Context, out var context) == false)
                {
                    context = new List<DialogueDirection>();
                    _contexts.Add(data.Context, context);
                }

                var direction = new DialogueDirection(data);
                
                context.Add(direction);
                _directions.Add(data.Order, direction);
            }
            
            _hudRoot.SetActive(false);

            if (title != null)
            {
                _directions[0] = new DialogueDirection()
                {
                    Order = 0,
                    BackgroundCommands = _directions[1].BackgroundCommands,
                    ActorCommands = new()
                    {
                        new(), new(), new()
                    },
                    ScreenCommands = new()
                    {
                        new DialogueScreenShowTitleCommand(LocalString.Get(title)),
                        new DialogueScreenBgmCommand(dialogueGroup[0].Bgm)
                    },
                    TextBoxCommands = new()
                    {
                        new DialogueTextBoxHideCommand(1)
                    },
                    IsUIHide = true,
                    IsSkippable = false,
                };
                
                if (string.IsNullOrEmpty(dialogueGroup[0].CameraMove) == false)
                {
                    var cameraMove = dialogueGroup[0].CameraMove;
                    var newLineIndex = cameraMove.IndexOf('\n');
                
                    _directions[0].ScreenCommands.Add(
                        new DialogueScreenCameraMoveCommand(newLineIndex == -1 ? cameraMove : cameraMove[..newLineIndex]));
                }
                
                _contexts[0].Insert(0, _directions[0]);
            }

            foreach (var direction in _directions.Values)
            {
                foreach (var command in direction.BackgroundCommands)
                {
                    await command.Preload(_background);
                }
                
                for (int slot = 0; slot < _actors.Length; slot++)
                {
                    foreach (var command in direction.ActorCommands[slot])
                    {
                        await command.Preload(_actors[slot]);
                    }
                }
        
                foreach (var command in direction.ScreenCommands)
                {
                    await command.Preload(_screen);
                }

                foreach (var command in direction.TextBoxCommands)
                {
                    await command.Preload(_textBox);
                }
            }
        }

        public async UniTask Play(int order, CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
            
            if (_cancellationTokenSource.IsCancellationRequested == false)
            {
                try
                {
                    _input = false;
                    
                    _screen.Camera.gameObject.SetActive(true);

                    _contextStack.Clear();
                    _contextStack.Push(_contexts.GetValueOrDefault(0).GetEnumerator());

                    order = Mathf.Clamp(order, _directions.Keys.Min(), _directions.Keys.Max());

                    JumpTo(order);

                    while (_contextStack.TryPop(out var enumerator))
                    {
                        if (enumerator.MoveNext() == false)
                        {
                            continue;
                        }

                        _contextStack.Push(enumerator);

                        await Play(enumerator.Current, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException e)
                {

                }
            }
        }

        private async UniTask Play(DialogueDirection direction, CancellationToken cancellationToken = default)
        {
            _hudRoot.SetActive(direction.IsUIHide == false);

            ExecuteDirection(direction);
            
            Canvas.ForceUpdateCanvases();

            var time = 0.0f;
            var duration = direction.Duration;

            while (time < duration)
            {
                UpdateDirection(direction, time);
                await UniTask.Yield(cancellationToken);
                time += Time.unscaledDeltaTime * (_input == false || direction.IsSkippable == false ? 1.0f : duration / 0.1f);
            }

            _input = false;

            UpdateDirection(direction, duration);

            LateExecuteDirection(direction);

            if (IsAutoPlay == true || direction.IsSkippable == false)
            {
                _touchCursor.SetActive(false);
                await UniTask.Delay((int)(direction.AutoDelay * 1000), ignoreTimeScale: true, cancellationToken: cancellationToken);
            }
            else
            {
                _touchCursor.SetActive(true);
                
                await UniTask.WaitUntil(() => _input, PlayerLoopTiming.Update, cancellationToken);
                
                _touchCursor.SetActive(false);
            }

            if (direction.Choices != null)
            {
                // 선택지 정보가 있는 경우 UI 출력 및 선택 대기
                var nextContextId = await _choices.Choice(direction.Choices, cancellationToken);
                    
                if (nextContextId != 0 && _contexts.TryGetValue(nextContextId, out var context) == true)
                {
                    _contextStack.Push(context.GetEnumerator());
                }
            }

            ExitDirection(direction);
            
            _input = false;
        }

        public void OnInput()
        {
            _input = true;
        }

        public void OnAutoPlaySwitchValueChanged(bool value)
        {
            IsAutoPlay = value;
        }

        public async UniTask Release()
        {
            if (_directions == null)
            {
                return;
            }
            
            foreach (var direction in _directions.Values)
            {
                foreach (var command in direction.BackgroundCommands)
                {
                    await command.Unload(_background);
                }

                for (int slot = 0; slot < _actors.Length; slot++)
                {
                    foreach (var command in direction.ActorCommands[slot])
                    {
                        await command.Unload(_actors[slot]);
                    }
                }

                foreach (var command in direction.ScreenCommands)
                {
                    await command.Unload(_screen);
                }

                foreach (var command in direction.TextBoxCommands)
                {
                    await command.Unload(_textBox);
                }
            }

            _background.Release();

            foreach (var actor in _actors)
            {
                actor.Release();
            }

            _screen.Release();
            _textBox.Release();

            _choices.Hide();
            
            _directions.Clear();
            _directions = null;
        }

        private void ExecuteDirection(DialogueDirection direction)
        {
            foreach (var command in direction.BackgroundCommands)
            {
                command.Execute(_background);
            }

            for (int slot = 0; slot < _actors.Length; slot++)
            {
                foreach (var command in direction.ActorCommands[slot])
                {
                    command.Execute(_actors[slot]);
                }
            }
            
            foreach (var command in direction.ScreenCommands)
            {
                command.Execute(_screen);
            }

            foreach (var command in direction.TextBoxCommands)
            {
                command.Execute(_textBox);
            }
        }
        
        private void UpdateDirection(DialogueDirection direction, float time)
        {
            foreach (var command in direction.BackgroundCommands)
            {
                command.Update(_background, time);
            }

            for (int slot = 0; slot < _actors.Length; slot++)
            {
                foreach (var command in direction.ActorCommands[slot])
                {
                    command.Update(_actors[slot], time);
                }
            }
            
            foreach (var command in direction.ScreenCommands)
            {
                command.Update(_screen, time);
            }

            foreach (var command in direction.TextBoxCommands)
            {
                command.Update(_textBox, time);
            }
        }
        
        private void LateExecuteDirection(DialogueDirection direction)
        {
            foreach (var command in direction.BackgroundCommands)
            {
                command.LateExecute(_background);
            }

            for (int slot = 0; slot < _actors.Length; slot++)
            {
                foreach (var command in direction.ActorCommands[slot])
                {
                    command.LateExecute(_actors[slot]);
                }
            }
            
            foreach (var command in direction.ScreenCommands)
            {
                command.LateExecute(_screen);
            }

            foreach (var command in direction.TextBoxCommands)
            {
                command.LateExecute(_textBox);
            }
        }
        
        private void ExitDirection(DialogueDirection direction)
        {
            foreach (var command in direction.BackgroundCommands)
            {
                command.Exit(_background);
            }

            for (int slot = 0; slot < _actors.Length; slot++)
            {
                foreach (var command in direction.ActorCommands[slot])
                {
                    command.Exit(_actors[slot]);
                }
            }
            
            foreach (var command in direction.ScreenCommands)
            {
                command.Exit(_screen);
            }

            foreach (var command in direction.TextBoxCommands)
            {
                command.Exit(_textBox);
            }
        }

        private void JumpTo(int order)
        {
            var branchedDirections = new Dictionary<int, (DialogueDirection direction, int choice)>(_contexts.Count);
            
            foreach (var direction in _directions.Values)
            {
                if (direction.Choices == null)
                {
                    continue;
                }

                for (int i = 0; i < direction.Choices.Count; i++)
                {
                    if (direction.Choices[i].contextId != 0)
                    {
                        branchedDirections.Add(direction.Choices[i].contextId, (direction, i));
                    }
                }
            }

            var selectedChoice = new Dictionary<int, int>(_contexts.Count);

            var contextId = _directions[order].Context;

            while (contextId != 0)
            {
                if (branchedDirections.TryGetValue(contextId, out var branch) == false)
                {
                    break;
                }
                
                selectedChoice.Add(branch.direction.Order, branch.choice);

                contextId = branch.direction.Context;
            }
            
            while (_contextStack.TryPop(out var enumerator))
            {
                var nextEnumerator = enumerator;
                
                if (nextEnumerator.MoveNext() == false)
                {
                    continue;
                }

                var direction = nextEnumerator.Current;

                if (direction.Order == order)
                {
                    _contextStack.Push(enumerator);
                    break;
                }
                
                _contextStack.Push(nextEnumerator);
                    
                ExecuteDirection(direction);
                var duration = direction.Duration;
                UpdateDirection(direction, duration);
                LateExecuteDirection(direction);
                ExitDirection(direction);
                    
                if (direction.Choices != null)
                {
                    var nextContextId = direction.Choices[selectedChoice.GetValueOrDefault(direction.Order)].contextId;
                    
                    if (nextContextId != 0 && _contexts.TryGetValue(nextContextId, out var context) == true)
                    {
                        _contextStack.Push(context.GetEnumerator());
                    }
                }
            }
        }
        
        #region 버튼액션
        public void OnClickHideButton()
        {
            _hideButton.SetActive(false);
            _showButton.SetActive(true);
        
            _rootSideButton.SetActive(false);
        
            _textBox.gameObject.SetActive(false);
            _choices.gameObject.SetActive(false);
        }

        public void OnClickShowButton()
        {
            _hideButton.SetActive(true);
            _showButton.SetActive(false);
        
            _rootSideButton.SetActive(true);
        
            _textBox.gameObject.SetActive(true);
            _choices.gameObject.SetActive(_choices.IsWaitingChoice);
        }

        public void OnClickLogButton()
        {
            //todo: 만들어야함.
        }

        public void OnClickAutoButton()
        {
            if (IsAutoPlay)
            {
                _autoButtonAnimation.DORewind();
                
                _input = false;
            }
            else
            {
                _autoButtonAnimation.DOPlay();

                _input = true;
            }
        
            OnAutoPlaySwitchValueChanged(!IsAutoPlay);
        }

        public void OnClickSkipButton()
        {
            ShowSkipPopup();
        }

        public void ShowSkipPopup()
        {
            UIManager.Instance.OverlayStack.Push(UIDialogueSkipPopup.CreateContext(new UIDialogueSkipPopup.State
            {
                DialogueId = _currentDialogueGroupId,
                CancellationTokenSource = _cancellationTokenSource
            }));
        }
        #endregion
        
        public readonly struct DialogueDirection
        {
            public List<DialogueCommand<DialogueBackground>> BackgroundCommands { get; init; }
            public List<List<DialogueCommand<DialogueActor>>> ActorCommands { get; init; }
            public List<DialogueCommand<DialogueScreen>> ScreenCommands { get; init; }
            public List<DialogueCommand<DialogueTextBox>> TextBoxCommands { get; init; }
            public List<DialogueChoiceData> Choices { get; init; }

            public float Duration
            {
                get
                {
                    var duration = 0.0f;
                    
                    foreach (var command in BackgroundCommands)
                    {
                        duration = Mathf.Max(duration, command.Duration);
                    }

                    foreach (var commands in ActorCommands)
                    {
                        foreach (var command in commands)
                        {
                            duration = Mathf.Max(duration, command.Duration);
                        }
                    }
                
                    foreach (var command in ScreenCommands)
                    {
                        duration = Mathf.Max(duration, command.Duration);

                    }
                
                    foreach (var command in TextBoxCommands)
                    {
                        duration = Mathf.Max(duration, command.Duration);
                    }

                    return duration;
                }
            }

            public int Order { get; init; }
            public int Context { get; init; }
            
            public string StringId { get; init; } 
            
            public bool IsSkippable { get; init; }
            public float AutoDelay { get; init; }
            
            public bool IsUIHide { get; init; }

            public DialogueDirection(DialogueData data)
            {
                Order = data.Order;
                Context = data.Context;
                
                StringId = $"Str_{data.Id}";
                
                IsSkippable = data.SkipBan == 0;
                AutoDelay = data.AutoDelay;

                IsUIHide = data.IsUiHide == 1;

                BackgroundCommands = CreateBackgroundCommands(data);
                ActorCommands = new ()
                {
                    CreateActorCommands(0, data),
                    CreateActorCommands(1, data),
                    CreateActorCommands(2, data),
                };
                
                ScreenCommands = CreateScreenCommands(data);
                TextBoxCommands = CreateTextBoxCommands(data);
                
                Choices = null;
                
                var text = LocalString.Get(StringId);

                if (string.IsNullOrEmpty(text) == true)
                {
                    return;
                }

                if (text[0] != '/')
                {
                    text = $"/\n{text}";
                }
                
                text = DialoguePreset.Apply(text);

                foreach (var context in text.AsSpan().TrimStart('/').Split("\n/"))
                {
                    FunctionParameters parameters;

                    var commandLineEndIndex = context.IndexOf('\n');

                    if (0 <= commandLineEndIndex)
                    {
                        parameters = $"{context[..commandLineEndIndex].ToString()}, \"{context[(commandLineEndIndex + 1)..].ToString()}\"";
                    }
                    else
                    {
                        parameters = context;
                    }

                    switch (parameters[0].ToString())
                    {
                        case "":
                        {
                            var speakActorData = DataTable.DialogueActorDataTable[data.SpeakActorId];
                            var speaker = (FunctionParameters)data.ActorName;
                            
                            var speakerName = speaker.GetValueOrDefault(0, speakActorData?.StrCharacterName);
                            var speakerTitle = speaker.GetValueOrDefault(1, speakActorData?.StrCharacterTitle);
                            
                            TextBoxCommands.Add(new DialogueTextBoxPrintTextCommand(
                                name: string.IsNullOrEmpty(speakerName) == false ? LocalString.Get(speakerName) : String.Empty,
                                title: string.IsNullOrEmpty(speakerTitle) == false ? LocalString.Get(speakerTitle) : String.Empty,
                                text: parameters[^1],
                                fontSize: data.FontSize,
                                delay: data.TextDelay));
                        }
                        break;

                        case "C" or "Clear" or "c" or "clear":
                        {
                            TextBoxCommands.Add(new DialogueTextBoxClearCommand());
                        }
                        break;

                        case "S" or "s":
                        {
                            Choices ??= new List<DialogueChoiceData>(5);
                            Choices.Add(new DialogueChoiceData()
                            {
                                text = parameters[^1],
                                contextId = parameters[..^1].GetValueOrDefault(1, 0),
                            });
                        }
                        break;

                        default:
                        {
                            var function = DialogueFunction<DialogueScreen>.CreateFunction(parameters);

                            if (function == null)
                            {
                                continue;
                            }

                            ScreenCommands.Add(function);
                        }
                        break;
                    }
                }
            }

            private static List<DialogueCommand<DialogueBackground>> CreateBackgroundCommands(DialogueData data)
            {
                var list = new List<DialogueCommand<DialogueBackground>>();
                
                if (string.IsNullOrEmpty(data.BG) == false)
                {
                    list.Add(new DialogueBackgroundChangeCommand(data.BG));
                }

                return list;
            }

            private static List<DialogueCommand<DialogueActor>> CreateActorCommands(int slotIndex, DialogueData data)
            {
                var list = new List<DialogueCommand<DialogueActor>>();

                foreach (var parameters in data.ActorSlots[slotIndex].AsSpan().Split("\n"))
                {
                    FunctionParameters functionParameters = new(parameters);
                    
                    switch (functionParameters[0].ToString())
                    {
                        case "In" or "in":
                            {
                                list.Add(new DialogueActorInCommand(functionParameters));
                            }
                            break;
                        case "Out" or "out":
                            {
                                list.Add(new DialogueActorOutCommand(functionParameters));
                            }
                            break;
                        default:
                            {
                                DebugHelper.LogError($"Invalid ActorSlot[{slotIndex + 1}] data. Table id: {data.Id}");
                            }
                            break;
                    }
                }
                
                list.Add(new DialogueActorSpotlightCommand(data.SpeakActorId, data.IsActorSpotLights[slotIndex]));

                if (data.ActorFaces[slotIndex] != 0)
                {
                    list.Add(new DialogueActorEmotionChangeCommand(data.ActorFaces[slotIndex]));
                }
                
                if (data.ActorEmojis[slotIndex] != 0)
                {
                    list.Add(new DialogueActorEmojiCommand(data.ActorEmojis[slotIndex]));
                }

                var actorFunc = DialoguePreset.Apply(data.ActorFuncs[slotIndex]);

                foreach (var parameters in actorFunc.AsSpan().Split("\n"))
                {
                    var function = DialogueFunction<DialogueActor>.CreateFunction(parameters);

                    if (function == null)
                    {
                        continue;
                    }

                    list.Add(function);
                }
                
                return list;
            }
            
            private static List<DialogueCommand<DialogueScreen>> CreateScreenCommands(DialogueData data)
            {
                var list = new List<DialogueCommand<DialogueScreen>>();

                if (string.IsNullOrEmpty(data.CameraMove) == false)
                {
                    list.Add(new DialogueScreenCameraMoveCommand(data.CameraMove));
                }
                
                if (string.IsNullOrEmpty(data.Popup) == false)
                {
                    list.Add(new DialogueScreenPopupCommand(data.Popup));
                }
                
                var screenFunc = DialoguePreset.Apply(data.ScreenFunc);

                foreach (var parameters in screenFunc.AsSpan().Split("\n"))
                {
                    var function = DialogueFunction<DialogueScreen>.CreateFunction(parameters);

                    if (function == null)
                    {
                        continue;
                    }

                    list.Add(function);
                }
                
                if (string.IsNullOrEmpty(data.Bgm) == false)
                {
                    list.Add(new DialogueScreenBgmCommand(data.Bgm));
                }
                
                if (string.IsNullOrEmpty(data.Sfx) == false)
                {
                    list.Add(new DialogueScreenSfxCommand(data.Sfx));
                }

                return list;
            }
            
            private static List<DialogueCommand<DialogueTextBox>> CreateTextBoxCommands(DialogueData data)
            {
                var list = new List<DialogueCommand<DialogueTextBox>>();
                
                list.Add(new DialogueTextBoxHideCommand(data.IsTextBoxHide));

                // if (string.IsNullOrEmpty(data.TextId) == false)
                // {
                //     list.Add(data.TextId switch
                //     {
                //         "C" or "Clear" or "c" or "clear" => new DialogueTextBoxClearCommand(),
                //         _ => new DialogueTextBoxPrintTextCommand(data.TextId, data.FontSize, data.TextDelay)
                //     });
                // }

                return list;
            }
        }

        public struct DialogueChoiceData
        {
            public string text;
            public int contextId;
        }
    }
}