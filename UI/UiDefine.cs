using System;
using System.Collections;
using System.Collections.Generic;
using GameModes;
using GameModes.Transitions;
using Managers;
using UI.Overlay;
using UI.Panel;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Users;

namespace Oz.Define
{
    public class MainMenuSlotData
    {
        public int MaxSlotCount = 0;
        public int MaxSlotDataCount = 0;

        public MainMenuSlotData(int maxSlot, int maxSlotData)
        {
            MaxSlotCount = maxSlot;
            MaxSlotDataCount = maxSlotData;
        }
    }

    public class UiDefine
    {
        public enum EHudType
        {
            None,
            DeungeonHud,
            WorldMapHud,
            StageHud,
        }

        public enum EDungeonType
        {
            None = -1,
            AttributeDungeon,
            DayDungeon,
            InfinityDungeon,
            EventDungeon,
            Max,
        }

        public enum EChapterType
        {
            LockChapter = 1,
            ClearChapter,
            PlayChapter,
            ClearLockChapter,
        }

        public enum EStageSlotType
        {
            ClearStage = 1,
            PlayingStage,
            LockStage,
            ClearLockStage,
        }

        public enum EStageClearType
        {
            StageNone = 0,
            StageReStart,   // 현재 스테이지의 파티 구성화면
            StageSelect,        // 현재 챕터의 스테이지 선택화면
            StageNextStage,     // 현재 챕터의 스테이지 선택화면
            StageKingdom,       // 왕국 sng 화면
        }

        public static void StageClearMove(EStageClearType type)
        {
            Time.timeScale = 1;

            switch (type)
            {
                default:
                case EStageClearType.StageNone:
                    {
                        Managers.GameModeManager.Instance.Transit(LobbyGameMode.CreateContext());
                    }
                    break;
                case EStageClearType.StageSelect:
                case EStageClearType.StageReStart:
                    {
                        Network.PacketProcessor.Instance.SendRequest(
                            new Network.Packets.Game.Stage.StageStartPacket(User.My.AdventureModeInfo.GetSelectedAdventureStage().Id, BattleModeType.AdventureMode, BattleModePartyType.AdventureModeParty)
                                .OnCompleted(_ =>
                                {
                                    var battleData = new BattleAdventureData();
                                    battleData.BattleGameType = BattleModeType.AdventureMode;
                                    GameModeManager.Instance.Transit(BattleGameMode.CreateContext(battleData).SetTransition(new BattleModeTransition()));
                                })
                                .OnFailed(_ =>
                                {
                                    Network.PacketProcessor.Instance.SendRequest(
                                        new Network.Packets.Game.Stage.StageCancelPacket());

                                    UIManager.Instance.OverlayStack.Push(UISystemMessagePopup.CreateContext(new()
                                    {
                                        type = UISystemMessagePopup.Type.Yes,
                                        TitleString = "title",
                                        InfoString = "Already has playing stage.\nDo you want to cancel ?",
                                        YesString = "Retry",
                                        NoString = "Cancel",
                                        YesAction = () => { },
                                        NoAction = null,
                                        EnableBgClick = true,
                                    }));
                                }));
                    }
                    break;
                case EStageClearType.StageNextStage:
                    {
                        Network.PacketProcessor.Instance.SendRequest(
                            new Network.Packets.Game.Stage.StageStartPacket(User.My.AdventureModeInfo.NextStage.Id, BattleModeType.AdventureMode, BattleModePartyType.AdventureModeParty)
                                .OnCompleted(_ =>
                                {
                                    User.My.AdventureModeInfo.SetSelectedAdventureStage(User.My.AdventureModeInfo.NextStage.Id);

                                    var battleData = new BattleAdventureData();
                                    battleData.BattleGameType = BattleModeType.AdventureMode;
                                    GameModeManager.Instance.Transit(BattleGameMode.CreateContext(battleData).SetTransition(new BattleModeTransition()));
                                })
                                .OnFailed(_ =>
                                {
                                    Network.PacketProcessor.Instance.SendRequest(
                                        new Network.Packets.Game.Stage.StageCancelPacket());

                                    UIManager.Instance.OverlayStack.Push(UISystemMessagePopup.CreateContext(new()
                                    {
                                        type = UISystemMessagePopup.Type.Yes,
                                        TitleString = "title",
                                        InfoString = "Already has playing stage.\nDo you want to cancel ?",
                                        YesString = "Retry",
                                        NoString = "Cancel",
                                        YesAction = () => { },
                                        NoAction = null,
                                        EnableBgClick = true,
                                    }));
                                }));
                    }
                    break;
                case EStageClearType.StageKingdom:
                    {
                        Managers.GameModeManager.Instance.Transit(LobbyGameMode.CreateContext());
                    }
                    break;
            }

            //Managers.GameModeManager.Instance.Transit(LobbyGameMode.CreateContext());
            //ScreenManagerLegacy.Instance.Set<LobbyScreenLegacy>(ScreenState.Lobby, (screen) =>
            //{
            //    if (screen != null)
            //    {
            //        screen.StageType = type;
            //    }
            //});
        }

        public static void ImageRectSize(Image image, Sprite sprite)
        {
            if (sprite == null)
                return;

            RectTransform rt = image.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            }
            image.sprite = sprite;
        }

        // 마지막 . 빼기
        public static String FileExtension(string file)
        {
            string fileName = file;
            int fileExtPos = fileName.LastIndexOf(".");
            if (fileExtPos >= 0)
                fileName = fileName.Substring(0, fileExtPos);

            return fileName;
        }
        
        //파일 경로 및 확장자 빼기.
        public static string PickOutFileName(string source)
        {
            if (!source.Contains("prefab"))
                return source;
            
            string fileName = string.Empty;
            string pathIdentifier = "/";
            string extensionIdentifier = ".";
            
            int index = source.LastIndexOf(pathIdentifier, StringComparison.Ordinal) + 1;
            if (index >= 0)
                fileName = source.Substring(index, source.Length - index);
            
            index = fileName.LastIndexOf(extensionIdentifier, StringComparison.Ordinal);
            if (index >= 0)
                fileName = fileName.Substring(0, index);

            return fileName;
        }
    }
}