using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Users;
using System.Linq;
using Managers;
using System;
using System.Collections;
using DG.Tweening;
using System.Threading.Tasks;
using Dialogue;

namespace UI.Overlay
{
    public class UIPrologueMessengerOverlay : UIOverlay<UIPrologueMessengerOverlay>
    {
        enum MessengerType
        {
            LeftBox,
            RightBox,
            Message,
            ChoiceMessage,
            ChoideBox,
            MemoriesSummon,
            Delay,
            TextReady,
        }

        struct MessengerData
        {
            public MessengerType type;
            public int textIndex1;
            public int textIndex2;
            public float duration;
            public float speed;
            public string sound;
        }

        private List<GameObject> _messageObjs = new();
        private List<MessengerData> _messages = new()
        {
            new() { type = MessengerType.Delay, duration = 1 },
            new() { type = MessengerType.LeftBox, duration = 0.1f },
            new() { type = MessengerType.TextReady, duration = 1.5f},  // ....
            new() { type = MessengerType.Message, textIndex1 = 2, duration = 2f, speed = 0.05f, sound = "Dorothyscene_2" },  // "선배?"
            new() { type = MessengerType.LeftBox, duration = 0.1f },
            new() { type = MessengerType.TextReady, duration = 1.5f },  // ....
            new() { type = MessengerType.Message, textIndex1 = 3, duration = 3f, speed = 0.05f, sound = "Dorothyscene_3" },    // 선배.. 인가요?
            new() { type = MessengerType.RightBox, duration = 0.1f },
            new() { type = MessengerType.Message, textIndex1 = 4, duration = 2f, speed = 0.05f },    // "?!"
            new() { type = MessengerType.LeftBox, duration = 0.1f },
            new() { type = MessengerType.TextReady, duration = 1.5f },  // ....
            new() { type = MessengerType.Message, textIndex1 = 5, duration = 3f, speed = 0.05f, sound = "Dorothyscene_4" },    // 혹시 괜찮다면...
            new() { type = MessengerType.LeftBox, duration = 0.1f },
            new() { type = MessengerType.TextReady, duration = 1.5f },  // ....
            new() { type = MessengerType.Message, textIndex1 = 6, duration = 4f, speed = 0.05f, sound = "Dorothyscene_5" },    // 이곳으로 와주시겠어요?
            new() { type = MessengerType.ChoideBox, textIndex1 = 7, textIndex2 = 8, duration = 0 },
            new() { type = MessengerType.RightBox, duration = 0.1f },
            new() { type = MessengerType.ChoiceMessage, duration = 2f, speed = 0.05f },    // 선택한 텍스트...
            new() { type = MessengerType.LeftBox, duration = 0.1f },
            new() { type = MessengerType.TextReady, duration = 1.5f },  // ....
            new() { type = MessengerType.Message, textIndex1 = 9, duration = 7.5f, speed = 0.05f, sound = "Dorothyscene_6" },    // 도쿄는 아니고... 해외도 아니고...
            new() { type = MessengerType.LeftBox, duration = 0.1f },
            new() { type = MessengerType.TextReady, duration = 1.5f },  // ....
            new() { type = MessengerType.Message, textIndex1 = 10, duration = 2f, speed = 0.05f, sound = "Dorothyscene_7" },    // 그냥 평범한
            new() { type = MessengerType.LeftBox, duration = 0.1f },
            new() { type = MessengerType.TextReady, duration = 1.5f },  // ....
            new() { type = MessengerType.Message, textIndex1 = 11, duration = 2f, speed = 0.05f, sound = "Dorothyscene_8" },    // 이세계예요.
            new() { type = MessengerType.ChoideBox, textIndex1 = 12, textIndex2 = 13, duration = 0 },
            new() { type = MessengerType.RightBox, duration = 0.1f, speed = 0.1f },
            new() { type = MessengerType.ChoiceMessage, duration = 2f, speed = 0.05f },    // 선택한 텍스트...
            new() { type = MessengerType.MemoriesSummon, duration = 0 },
            new() { type = MessengerType.Delay, duration = 0.5f },
        };

        [SerializeField] private GameObject _left;
        [SerializeField] private GameObject _right;
        [SerializeField] private GameObject _choice;
        [SerializeField] private GameObject _memoriesSummon;
        [SerializeField] private Transform _root;
        [SerializeField] private GameObject _hide;
        [SerializeField] private GameObject _ani;
        
        private Action _callback = null;
        private int _choiceTextIndex = 0;
        
        public async UniTask OnEnter(Action callBack)
        {
            gameObject.SetActive(true);
  
            _callback = callBack;

            _= NextMessage();
        }

        private string Message(int idex)
        {
            string message = idex switch
            {
                1 => "....",
                2 => LocalString.Get("Str_UI_Prolog_09"),
                3 => LocalString.Get("Str_UI_Prolog_10"),
                4 => LocalString.Get("Str_UI_Prolog_60"),
                5 => LocalString.Get("Str_UI_Prolog_11"),
                6 => LocalString.Get("Str_UI_Prolog_12"),
                7 => LocalString.Get("Str_UI_Prolog_13"),
                8 => LocalString.Get("Str_UI_Prolog_14"),
                9 => LocalString.Get("Str_UI_Prolog_15"),
                10 => LocalString.Get("Str_UI_Prolog_16"),
                11 => LocalString.Get("Str_UI_Prolog_17"),
                12 => LocalString.Get("Str_UI_Prolog_18"),
                13 => LocalString.Get("Str_UI_Prolog_19"),
            };

            return message;
        }
        private async UniTask NextMessage()
        {

            if (_messages.Count() <= 0)
            {
                OnClose();
                return;
            }

            MessengerData dat = _messages.First();
            _messages.Remove(dat);
            switch (dat.type)
            {
                case MessengerType.LeftBox:
                    { 
                        _messageObjs.Add(Instantiate(_left, _root));
                        UpdateScroll();
                    }
                    break;
                case MessengerType.RightBox: 
                    { 
                        _messageObjs.Add(Instantiate(_right, _root));
                        UpdateScroll();
                    }
                    break;
                case MessengerType.TextReady:
                    {
                        var message = _messageObjs.Last().GetComponent<UIPrologueMessageText>();
                        message.Ready();
                    }
                    break;
                case MessengerType.Message:
                    {
                        var message = _messageObjs.Last().GetComponent<UIPrologueMessageText>();
                        message.SetMessage(Message(dat.textIndex1), dat.duration, dat.speed, dat.sound);
                    }
                    break;
                case MessengerType.ChoideBox:
                    {
                        _choiceTextIndex = dat.textIndex1;
                        var obj = Instantiate(_choice, _root);
                        UIPrologueMessageChoiceBox message = obj.GetComponent<UIPrologueMessageChoiceBox>();
                        message.Set(Message(dat.textIndex1), Message(dat.textIndex2), (idx) =>
                        {
                            _choiceTextIndex += idx;
                            _messageObjs.Remove(obj);
                            Destroy(obj);
                            UpdateScroll();
                            _ = NextMessage();
                        });

                        _messageObjs.Add(obj);
                        UpdateScroll();
                    }
                    break;
                case MessengerType.ChoiceMessage:
                    {
                        var message = _messageObjs.Last().GetComponent<UIPrologueMessageText>();
                        message.SetMessage(Message(_choiceTextIndex), dat.duration, dat.speed);
                    }
                    break;
                case MessengerType.MemoriesSummon:
                    {
                        _memoriesSummon.SetActive(true);
                        _messageObjs.Add(_memoriesSummon);
                        UpdateScroll();
                    }
                    return;
            }

            if (dat.duration > 0)
            {
                await DOTween.Sequence().OnComplete(() =>
                {
                    _ = NextMessage();

                }).AppendInterval(dat.duration).SetUpdate(true);
            }
        }

        private void UpdateScroll()
        {
            if (_messageObjs.Count() <= 1)
                return;

            float startPosY = _messageObjs.First().transform.localPosition.y;
            float posY = 0;
            for (int i_1 = 0; i_1 < _messageObjs.Count() - 1; ++i_1)
            {
                posY += ((RectTransform)_messageObjs[i_1].transform).sizeDelta.y;
            }

            var listObj = _messageObjs.Last();
            listObj.transform.localPosition = new Vector2(listObj.transform.localPosition.x, startPosY - posY);
            posY += ((RectTransform)listObj.transform).sizeDelta.y;

            float sizeY = ((RectTransform)_root).sizeDelta.y + _root.localPosition.y;
            if (posY > (sizeY - 50))
            {
                float offset = Math.Abs(posY - sizeY) + 50;
                _root.localPosition = new Vector2(0, _root.localPosition.y + offset);
            }
        }

        public void OnMemoriesClick()
        {
            _hide.SetActive(true);
            
            _= NextMessage();
        }

        public void OnClose()
        {
            _callback?.Invoke(); 

            foreach (var o in _messageObjs)
            {
                Destroy(o);
            }
            _messageObjs.Clear();
            gameObject.SetActive(false);
        }
    }
}