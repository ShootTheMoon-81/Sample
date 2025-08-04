using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GameModes.Transitions;
using Managers;
using Provider;
using UI.Panel;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameModes
{
    public abstract class GameMode : MonoBehaviour
    {
        public static Transition DefaultTransition => new ScreenFadeTransition();
        
        public abstract UIPanel.Context DefaultPanelContext { get; }
        
        public virtual async UniTask OnLoad()
        {
        }
        
        public virtual async UniTask OnExit()
        {
        }
        
        public virtual async UniTask OnUnload()
        {
        }

        private static GameModeProvider _provider;

        private static GameModeProvider Provider => _provider ??=
            Addressables.LoadAssetAsync<GameModeProvider>("Providers/GameModeProvider.asset").WaitForCompletion();
        
        public struct EmptyState
        {
        }

        public abstract class Context
        {
            public enum Phases
            {
                None,
                Load,
                Enter,
                Exit,
            }
            
            public abstract GameMode GameMode { get; }
            
            public abstract Transition Transition { get; }
            
            public abstract System.Type GameModeType { get; }
            public abstract System.Type StateType { get; }

            public Phases Phase { get; set; } = Phases.None;

            public abstract UniTask Load();
            public abstract UniTask Enter();
            public abstract UniTask Exit();

            public abstract UniTask Unload();
            
            protected static UniTask<TGameMode> Get<TGameMode>() where TGameMode : GameMode => Provider.Get<TGameMode>();
            protected static void Return(GameMode gameMode) => Provider.Return(gameMode);
            protected static UniTask Release<TGameMode>() where TGameMode : GameMode => Provider.Release<TGameMode>();
        }
    }

    public abstract class GameMode<TGameMode, TState> : GameMode
        where TGameMode : GameMode<TGameMode, TState>
    {
        public override async UniTask OnLoad()
        {
        }

        public virtual async UniTask OnEnter(TState state)
        {
        }
        
        public override async UniTask OnExit()
        {
        }

        public override async UniTask OnUnload()
        {
        }

        public virtual TState GetState()
        {
            return default;
        }

        public static Context.Builder CreateContext(TState state)
        {
            return new Context.Builder(state);
        }

        public new class Context : GameMode.Context
        {
            private TGameMode _gameMode;
            private TState _state;
            private Transition _transition;
            private List<UIPanel.Context> _panelStack = new();

            public override GameMode GameMode => _gameMode;

            public override Transition Transition => _transition;

            public override System.Type GameModeType => typeof(TGameMode);
            public override System.Type StateType => typeof(TState);

            private Context(TState state, Transition transition, List<UIPanel.Context> panelStack)
            {
                _state = state;
                _transition = transition;
                _panelStack = panelStack;
            }

            public override async UniTask Load()
            {
                _gameMode ??= await Get<TGameMode>();
                Phase = Phases.Load;
            }

            public override async UniTask Enter()
            {
                var state = _state;
                _state = default;

                await _gameMode.OnEnter(state);
                
                _panelStack.Insert(0, _gameMode.DefaultPanelContext);
                
                await UIManager.Instance.PanelStack.PushAsync(_panelStack);

                Phase = Phases.Enter;
            }

            public override async UniTask Exit()
            {
                await UIManager.Instance.ClearAsync();

                if (_gameMode == null)
                {
                    return;
                }
                
                _state = _gameMode.GetState();

                await _gameMode.OnExit();
                
                Return(_gameMode);
                _gameMode = null;
                
                Phase = Phases.Exit;
            }

            public override async UniTask Unload()
            {
                await Release<TGameMode>();
            }
            
            public struct Builder
            {
                private TState _state;
                private Transition _transition;
                private List<UIPanel.Context> _panelStack;

                public Builder(TState state)
                {
                    _state = state;
                    _transition = default;
                    _panelStack = default;
                }

                public Builder SetTransition(Transition transition)
                {
                    _transition = transition;
                    return this;
                }

                public Builder AddUIPanel(UIPanel.Context context)
                {
                    _panelStack ??= new();
                    _panelStack.Add(context);
                    return this;
                }

                public Context Build() => new(_state, _transition ?? GetDefaultTransition(), _panelStack ?? new());
            
                public static implicit operator Context(Builder builder) => builder.Build();


                private static System.Func<Transition> _defaultTransition;

                private static Transition GetDefaultTransition()
                {
                    const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                    _defaultTransition ??= (System.Func<Transition>)typeof(TGameMode).GetProperty("DefaultTransition", bindingFlags)?
                        .GetMethod.CreateDelegate(typeof(System.Func<Transition>));

                    return _defaultTransition?.Invoke(); 
                }
            }
        }
    }

    public abstract class GameMode<TGameMode> : GameMode<TGameMode, GameMode.EmptyState>
        where TGameMode : GameMode<TGameMode>
    {
        public sealed override UniTask OnEnter(EmptyState state) => OnEnter();

        public virtual async UniTask OnEnter()
        {
        }
        
        public static Context.Builder CreateContext()
        {
            return new Context.Builder(new());
        }
    }
}