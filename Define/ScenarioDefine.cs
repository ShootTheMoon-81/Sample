using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Define
{
    public class ScenarioDefine
    {
        // ToolRoot
        public const string ScenarioRoot = "ScenarioEditor";

        public const string ScenarioPath = @"../../data/Scenario/";

        public const string ScenarioCharacterEmpty = "None";

        public const string ScenarioCharacterToolTag = "ScenarioCharacter";
        public const string ScenarioEmotionToolTag = "Emotion";
        public const string ScenarioBGAnchorsToolTag = "BGAnchors";
        public const string ScenarioAddImageToolTag = "AddImage";
        public enum ScenarioLoadType
        {
            NewScenario, LoadScenario
        }

        public enum ScenarioType
        {
            None = 0,
            FullType,
            InPlayType,
        }

        public enum PopUpType
        {
            None_PopUp,
            Use_PopUp,
        }

        public enum ScenarioDirectionType
        {
            ScenarioNone = 0,
            ScenarioShake,
            ScenarioColorRevers,
            ScenarioFadeOut,
            ScenarioFadeIn,
            ScenarioConverging,
            ScenarioMax,
        }

        public enum ScenarioBGScaleType
        {
            BaseBG,
            ScaleBG,
            ReScaleBG,
        }

        public enum ScenarioCharacterPos
        {
            None,
            Center,
            Left,
            Right,
            Custom,
        }

        public enum ScenarioCharacterDirectionType
        {
            CharacterNone,
            CharacterShake_LeftRight,
            CharacterShake_TopBottom,
            CharacterCustom,
            CharacterScale,
        }

        public enum ScenarioCharacterBatch
        {
            Batch,
            Enter,
            Exit,            
            Move,
            Pass,
        }

        public enum ScenarioEnterType
        {
            Immediate,
            Left,
            Right,
            Top,
            Bottom,
            Custom,
            FadeIn,
        }

        public enum ScenarioExitType
        {
            Immediate,
            Left,
            Right,
            Top,
            Bottom,
            Custom,
            FadeOut,
        }

        public enum ScenarioPassType
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
        }

        public enum ScenarioCharacterEnable
        {
            Enable,
            DisEnable,
            Sihousette,
        }

        public enum ScenarioCharacterJumpType
        {
            Jump_On,
            Jump_Off,
        }

        public enum ScenarioTalk
        {
            END,
            KEEP,
        }

        public enum ScenarioTalkNamePos
        {
            None,
            Left,
            Center,
            Right,
        }

        public enum ScenarioTalkType
        {
            Base = 0,
            Whisper,
            Cry,
        }

        public static ScenarioType GetScenarioType(string scenarioType) => (ScenarioType)Enum.Parse(typeof(ScenarioType), scenarioType);


        public enum ScenarioTutorialCharacterType
        {
            Immediate,
            Move,
        }

        public enum ScenarioTutorialCharacterOutType
        {
            Immediate,
            Move,
            FadeOut,
        }

        public enum ScenarioTutorialType
        {
            None = 0,
            Skill,
        }

        public enum ScenarioAspectBox
        {
            Top = 0,
            Bottom,
            Left,
            Right,
        }

        public static List<string> GetCharacterList()
        {
            if (DataTable.CharacterDataTable == null)
                return null;

            List<string> list = new List<string>();
            list.Add(ScenarioCharacterEmpty);
            var charList = DataTable.CharacterDataTable.GetGroupByCharacterTypeAndAvailable(CharacterType.Hero, true);

            foreach (var data in charList)
            {
                list.Add(data.Id);
            }
            return list;
        }

        public static void ScenarioColorRevers( Color baseColor, ref Color reversColor)
        {
            reversColor = new Color((1 - baseColor.r), (1 - baseColor.g), (1 - baseColor.b), baseColor.a);
        }

#if UNITY_EDITOR
        public static bool ScenarioLog = false;
#endif
        public static void ShowEditorPlayLog(string log)
        {
#if UNITY_EDITOR
            if (ScenarioLog)
                Debug.Log("<color=#00ff00> Scenario Log : " + log + " </color>");
#endif
        }
    }
}
