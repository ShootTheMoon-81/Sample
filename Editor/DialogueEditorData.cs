using Cysharp.Threading.Tasks;
using Data;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using ObjectFieldAlignment = Sirenix.OdinInspector.ObjectFieldAlignment;

namespace Dialogue
{
    public class DialogueEditorData
    {
        public interface IDialogueEditorWindow
        {
            void SetOrder(int order);
            
            void PlayImmediately(int order);
        }

        [Serializable]
        public class ViewerData
        {
            public ViewerData(DialogueData dialogueData)
            {
                BgResource = dialogueData.BG;
                // Actor1Resource = tableData.ActorSlots[0].Split(",")[1];
                // Actor2Resource = tableData.ActorSlots[1].Split(",")[1];
                // Actor3Resource = tableData.ActorSlots[2].Split(",")[1];
                Order = dialogueData.Order;
                Speaker = dialogueData.SpeakActorId;
                CurrentTalk = $"{LocalString.Get($"Str_{dialogueData.Id}")}";

                //LoadResource().Forget();
            }

            [VerticalGroup("RowData")]
            [HorizontalGroup("RowData/Base", width: 0.5f)]
            [HideLabel]
            [ReadOnly]
            public int Order;

            [VerticalGroup("RowData")]
            [HorizontalGroup("RowData/Base", width: 0.5f)]
            [HideLabel]
            [ReadOnly]
            public string Speaker;

            [HorizontalGroup("RowData/Detail", width: 100)]
            [PreviewField(100, ObjectFieldAlignment.Left)]
            [HideLabel]
            [ReadOnly]
            public Sprite Bg;

            [HideInInspector]
            public string BgResource;

            [HorizontalGroup("RowData/Detail", width: 50)]
            [PreviewField(50, ObjectFieldAlignment.Left)]
            [HideLabel]
            [ReadOnly]
            public Image Actor1;

            [HideInInspector]
            public string Actor1Resource;

            [HorizontalGroup("RowData/Detail", width: 50)]
            [PreviewField(50, ObjectFieldAlignment.Left)]
            [HideLabel]
            [ReadOnly]
            public Image Actor2;

            [HideInInspector]
            public string Actor2Resource;

            [HorizontalGroup("RowData/Detail", width: 50)]
            [PreviewField(50, ObjectFieldAlignment.Left)]
            [HideLabel]
            [ReadOnly]
            public Image Actor3;

            [HideInInspector]
            public string Actor3Resource;

            [HorizontalGroup("RowData/Detail")]
            [EnableGUI]
            [GUIColor(0.96f, 0.96f, 0.86f)]
            [CustomValueDrawer("OnButtonGUI")]
            public string CurrentTalk;

            private static GUIStyle _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
            };

            private string OnButtonGUI(string text)
            {
                if (GUILayout.Button(text, _buttonStyle, GUILayout.Height(100)) == true)
                {
                    SetOrder();
                }
                
                return text;
            }
            
            private void SetOrder()
            {
#if UNITY_EDITOR
                var windowType = Type.GetType("Dialogue.DialogueEditorWindow, Assembly-CSharp-Editor");
                var window = EditorWindow.GetWindow(windowType, false, "Dialogue Editor Window") as IDialogueEditorWindow;
                //window?.SetOrder(Order);
                window?.PlayImmediately(Order);
#endif
            }

            public async UniTask LoadResource()
            {
                if (string.IsNullOrEmpty(BgResource))
                {
                    return;
                }

                var resource = 
                    await Addressables.LoadAssetAsync<GameObject>(DataTable.DialogueResourceDataTable[BgResource].ResourceReference);

                if (resource.TryGetComponent(out SpriteRenderer spriteRenderer))
                {
                    Bg = spriteRenderer.sprite;
                }
            }
        }
    }

    internal class JobThread : MonoBehaviour
    {
        internal static JobThread Thread;

        private Queue<Action> _jobs = new();

        private void Awake()
        {
            Thread = this;
        }

        private void Update()
        {
            while (_jobs.Count > 0)
            {
                _jobs.Dequeue().Invoke();
            }
        }

        internal void AddJob(Action action)
        {
            _jobs.Enqueue(action);
        }
    }
}