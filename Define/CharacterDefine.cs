using StatData;
using System;
using TMPro;
using UnityEngine;

namespace Define
{
    public class CharacterDefine
    {
        public const int MaxStartCount = 5;
        public const int MaxSkillCount = 4;

        public const int MaxPlayerSlot = 6;
        public const int MaxPlayerCount = 5;

        public const int MaxCharacterStar = 4;

        public const int MaxCharacterStateSlot = 3;
        public const int MaxCharacterSkillSlot = 3;
        public const int MaxCharacterLevelUpSlot = 3;

        // 서버 연동됨

        public enum CharacterSubSort
        {
            SORT_FAVORITE = 0,
            SORT_GRADE,      
            SORT_LIKEABILITY,   
            SORT_GET,        
            SORT_JOB,
            SORT_ELEMENT,
            SORT_NAME,
            SORT_LEVEL,
            SORT_ID,
            MAX
        }

        public enum CharacterFilter
        {
            POSSESSIVEGRADEUP = 0,  // 소지 상태에서 각성가능
            POSSESSIVE,             // 소지
            NOPOSSESSIVEGRADEUP,    // 미소지 상태에서 획득가능
            NOPOSSESSIVE,           // 미소지(에테르로만 보유중)
            EMPTY,                  // 미소지(에테르도 없음)
            MAX,
        }

        public enum CharacterIconType
        {
            ICON_L,
            ICON_M,
            ICON_S,
            ICON_XS,
            ICON_LONG,
        }

        public enum CharacterPrefabType
        {
            CHARACTER_BASE,     // 캐릭터 인게임 기본 이미지
            CHARACTER_CUT,      // 캐릭터 인게임 스킬 컷인
            CHARACTER_BATCH,    // 캐릭터 배치
            CHARACTER_ILL,      // 캐릭터 영웅 정보 이미지 일러스트
            CHARACTER_STORY,    // 캐릭터 스토리
            CHARACTER_KINGDOM,  // 캐릭터 KINGDOM
            CHARACTER_SUMMON,   // 캐릭터 뽑기
            CHARACTER_SKILL,    // 캐릭터 스킬
            CHARACTER_STAGE,    // 캐릭터 스테이지
        }

        public enum StateSlotType
        {
            STATESLOT_ELEMENT = 0,
            STATESLOT_POSTYPE,
            STATESLOT_BATTLETYPE,
        }

        public enum GrowthMaterialType
        {
            UNIQUE_ETHER,
            ELEMENT_ETHER,
        }

        public static string GetFormationImage(FormationType type)
        {
            string imagePath = type switch
            {
                FormationType.DefaultFormation => "ICON_PARTY_FORMATION_DEFAULT",
                FormationType.FrontFormation => "ICON_PARTY_FORMATION_FRONT",
                FormationType.MidFormation => "ICON_PARTY_FORMATION_MIDDLE",
                FormationType.BackFormation => "ICON_PARTY_FORMATION_BACK",
                _ => string.Empty
            };

            return imagePath;
        }
    }

    public static class CharacterSortTypeExtension
    {
        public static string GetCharacterSortValueText(this CharacterDefine.CharacterSubSort Sort, ICharacterData Value)
        {
            string subName = Sort switch
            {
                //CharacterDefine.CharacterSubSort.SORT_POWER => $"{Value.Stat.CombatPower:#,0}",
                CharacterDefine.CharacterSubSort.SORT_NAME => Value.DisplayName,
                CharacterDefine.CharacterSubSort.SORT_ELEMENT => Value.Element.GetCharacterElementalText(),
                CharacterDefine.CharacterSubSort.SORT_JOB => Value.RoleType.GetCharacterRoleTypeText(),
                CharacterDefine.CharacterSubSort.SORT_LEVEL => SB.Str(Value.Level),
                CharacterDefine.CharacterSubSort.SORT_LIKEABILITY => SB.Str(Value.Likeability),
                _ => string.Empty,
            };

            return subName;
        }

        public static string GetCharacterSortName(this CharacterDefine.CharacterSubSort Sort)
        {
            string subName = Sort switch
            {
                CharacterDefine.CharacterSubSort.SORT_FAVORITE => LocalString.Get("Str_UI_Sorting_Select"),
                CharacterDefine.CharacterSubSort.SORT_GRADE => LocalString.Get("Str_UI_Sorting_Grade"),
                //CharacterDefine.CharacterSubSort.SORT_POWER => LocalString.Get("Str_UI_Sorting_Power"),
                CharacterDefine.CharacterSubSort.SORT_LIKEABILITY => LocalString.Get("Str_UI_Sorting_Friendship"),
                CharacterDefine.CharacterSubSort.SORT_GET => LocalString.Get("Str_UI_Sorting_SummonTime"),
                CharacterDefine.CharacterSubSort.SORT_JOB => LocalString.Get("Str_UI_Sorting_Role"),
                CharacterDefine.CharacterSubSort.SORT_ELEMENT => LocalString.Get("Str_UI_Sorting_Element"),
                CharacterDefine.CharacterSubSort.SORT_NAME => LocalString.Get("Str_UI_Sorting_Name"),
                CharacterDefine.CharacterSubSort.SORT_LEVEL => LocalString.Get("Str_UI_Sorting_Level"),

                _ => string.Empty,
            };

            return subName;
        }
    }

    public enum MoveSearchType
    {
        CLOSEST,  //가장 가까운 적
        FRONTFIRST,  // 자신기준으로 전방에서 가장 가까운 적
        FRONTLAST, // 자신 기준으로 전방에서 가장 먼 적
        BACKFIRST, // 자신 기준으로 후방에서 가장 가까운 적
        BACKLAST, // 자신 기준으로 후방에서 가장 먼 적
    }



    public static class CharacterElementalExtension
    {
        public static string GetCharacterElementalImage(this ElementType element, bool on = true)
        {
            string imagePath = element switch
            {
                ElementType.Instinct => "ICON_CHAR_ELEMENTTYPE_FIRE",
                ElementType.Emotional => "ICON_CHAR_ELEMENTTYPE_WATER",
                ElementType.Reason => "ICON_CHAR_ELEMENTTYPE_WIND",
                _ => string.Empty
            };

            if (on == false)
            {
                imagePath += "_OFF";
            }

            return imagePath;
        }


        public static string GetCharacterElementalText(this ElementType element)
        {
            string elementalText = element switch
            {
                ElementType.None => LocalString.Get("Str_UI_Element_Instinct"),
                ElementType.Instinct => LocalString.Get("Str_UI_Element_Instinct"),
                ElementType.Emotional => LocalString.Get("Str_UI_Element_Emotional"),
                ElementType.Reason => LocalString.Get("Str_UI_Element_Reason"),
                //ElementType.Light => LocalString.Get("Str_UI_Element_Light"),
                //ElementType.Dark => LocalString.Get("Str_UI_Element_Dark"),
                _ => string.Empty
            };

            return elementalText;
        }

        public static string GetCharacterElementalDescText(this ElementType element)
        {
            string elementalText = element switch
            {
                ElementType.None => LocalString.Get("Str_UI_Element_Instinct_Desc"),
                ElementType.Instinct => LocalString.Get("Str_UI_Element_Instinct_Desc"),
                ElementType.Emotional => LocalString.Get("Str_UI_Element_Emotional_Desc"),
                ElementType.Reason => LocalString.Get("Str_UI_Element_Reason_Desc"),
                //ElementType.Light => LocalString.Get("Str_UI_Element_Light"),
                //ElementType.Dark => LocalString.Get("Str_UI_Element_Dark"),
                _ => string.Empty
            };

            return elementalText;
        }
    }

    public static class CharacterPosTypeExtension
    {
        public static string GetCharacterPosTypeImage(this PositionType posType, bool on = true)
        {
            string imagePath = posType switch
            {
                PositionType.Front => "ICON_CHAR_POSITIONTYPE_FRONT",
                PositionType.Middle => "ICON_CHAR_POSITIONTYPE_MID",
                PositionType.Back => "ICON_CHAR_POSITIONTYPE_BACK",
                _ => string.Empty
            };

            if (on == false)
            {
                imagePath += "_OFF";
            }

            return imagePath;
        }

        public static string GetCharacterPosTypeText(this PositionType posType)
        {
            string positionTypeText = posType switch
            {
                PositionType.Front => LocalString.Get("Str_UI_BattlePosition_Front"),
                PositionType.Middle => LocalString.Get("Str_UI_BattlePosition_Middle"),
                PositionType.Back => LocalString.Get("Str_UI_BattlePosition_Back"),
                _ => string.Empty
            };

            return positionTypeText;
        }

        public static string GetCharacterPosTypeDescText(this PositionType posType)
        {
            string positionTypeText = posType switch
            {
                PositionType.Front => LocalString.Get("Str_UI_BattlePosition_Front_Desc"),
                PositionType.Middle => LocalString.Get("Str_UI_BattlePosition_Middle_Desc"),
                PositionType.Back => LocalString.Get("Str_UI_BattlePosition_Back_Desc"),
                _ => string.Empty
            };

            return positionTypeText;
        }
    }


    //public static class CharacterAITypeExtension
    //{
    //    public static string GetCharacterAITypeImage(this AIType aiType, bool on = true)
    //    {
    //        string imagePath = aiType switch
    //        {
    //            AIType.Assault => "ICON_CHAR_AITYPE_ASSAULT",
    //            AIType.Strategic => "ICON_CHAR_AITYPE_STRATEGIC",
    //            AIType.Bomber => "ICON_CHAR_AITYPE_BOMBER",
    //            AIType.Support => "ICON_CHAR_AITYPE_SUPPORT",
    //            _ => string.Empty
    //        };
    //
    //        if (on == false)
    //        {
    //            imagePath += "_OFF";
    //        }
    //
    //        return imagePath;
    //    }
    //
    //    public static string GetCharacterAITypeText(this AIType aiType)
    //    {
    //        string aiTypeText = aiType switch
    //        {
    //            AIType.Assault => LocalString.Get("Str_UI_AIType_Assault"),
    //            AIType.Strategic => LocalString.Get("Str_UI_AIType_Strategic"),
    //            AIType.Bomber => LocalString.Get("Str_UI_AIType_Bomber"),
    //            AIType.Support => LocalString.Get("Str_UI_AIType_Support"),
    //            _ => string.Empty
    //        };
    //        return aiTypeText;
    //    }
    //
    //    public static string GetCharacterAITypeDescText(this AIType aiType)
    //    {
    //        string aiTypeText = aiType switch
    //        {
    //            AIType.Assault => LocalString.Get("Str_UI_AIType_Assault_Desc"),
    //            AIType.Strategic => LocalString.Get("Str_UI_AIType_Strategic_Desc"),
    //            AIType.Bomber => LocalString.Get("Str_UI_AIType_Bomber_Desc"),
    //            AIType.Support => LocalString.Get("Str_UI_AIType_Support_Desc"),
    //            _ => string.Empty
    //        };
    //        return aiTypeText;
    //    }
    //
    //    public static Color GetCharacterAITypeColor(this AIType aiType)
    //    {
    //        Color color = Color.white;
    //        switch (aiType)
    //        {
    //            case AIType.Assault: ColorUtility.TryParseHtmlString("#ff5c82", out color); break;
    //            case AIType.Strategic: ColorUtility.TryParseHtmlString("#68ddff", out color); break;
    //            case AIType.Bomber: ColorUtility.TryParseHtmlString("#81e5a7", out color); break;
    //            case AIType.Support: ColorUtility.TryParseHtmlString("#ffdc52", out color); break;
    //            case AIType.None:
    //                break;
    //            case AIType.Max:
    //                break;
    //            default:
    //                throw new ArgumentOutOfRangeException(nameof(aiType), aiType, null);
    //        }
    //        return color;
    //    }
    //}

    public static class CharacterRoleTypeExtension
    {
        public static string GetCharacterRoleTypeImage(this RoleType roleType)
        {
            string imagePath = roleType switch
            {
                RoleType.Fighter => "ICON_CHAR_ROLETYPE_FIGHTER",
                RoleType.Ranger => "ICON_CHAR_ROLETYPE_RANGER",
                RoleType.Lord => "ICON_CHAR_ROLETYPE_LORD",
                RoleType.Knight => "ICON_CHAR_ROLETYPE_KNIGHT",
                RoleType.Guardian => "ICON_CHAR_ROLETYPE_GUARDIAN",
                RoleType.Caster => "ICON_CHAR_ROLETYPE_CASTER",
                RoleType.Trickster => "ICON_CHAR_ROLETYPE_TRICKSTER",
                _ => string.Empty
            };

            
            return imagePath;
        }

        public static string GetCharacterRoleTypeText(this RoleType roleType)
        {
            string roleTypeText = roleType switch
            {
                RoleType.Fighter => LocalString.Get("Str_UI_RoleTypeName_Fighter"),
                RoleType.Ranger => LocalString.Get("Str_UI_RoleTypeName_Ranger"),
                RoleType.Lord => LocalString.Get("Str_UI_RoleTypeName_Lord"),
                RoleType.Knight => LocalString.Get("Str_UI_RoleTypeName_Knight"),
                RoleType.Guardian => LocalString.Get("Str_UI_RoleTypeName_Guardian"),
                RoleType.Caster => LocalString.Get("Str_UI_RoleTypeName_Caster"),
                RoleType.Trickster => LocalString.Get("Str_UI_RoleTypeName_Trickster"),
                _ => string.Empty
            };
            return roleTypeText;
        }

        public static string GetCharacterRoleTypeDescText(this RoleType roleType)
        {
            string roleTypeText = roleType switch
            {
                RoleType.Fighter => LocalString.Get("Str_UI_RoleTypeDesc_Fighter"),
                RoleType.Ranger => LocalString.Get("Str_UI_RoleTypeDesc_Ranger"),
                RoleType.Lord => LocalString.Get("Str_UI_RoleTypeDesc_Lord"),
                RoleType.Knight => LocalString.Get("Str_UI_RoleTypeDesc_Knight"),
                RoleType.Guardian => LocalString.Get("Str_UI_RoleTypeDesc_Guardian"),
                RoleType.Caster => LocalString.Get("Str_UI_RoleTypeDesc_Caster"),
                RoleType.Trickster => LocalString.Get("Str_UI_RoleTypeDesc_Trickster"),
                _ => string.Empty
            };
            return roleTypeText;
        }

        public static Color GetCharacterRoleTypeColor(this RoleType roleType)
        {
            Color color = Color.white;
            switch (roleType)
            {
                // FIXME : RoleType 변경 대응 필요
                //case RoleType.Defender: ColorUtility.TryParseHtmlString("#8fd1ff", out color); break;
                //case RoleType.Attacker: ColorUtility.TryParseHtmlString("#ff5c82", out color); break;
                //case RoleType.Assister: ColorUtility.TryParseHtmlString("#81e5a7", out color); break;
                //case RoleType.Slayer: ColorUtility.TryParseHtmlString("#d1a4ff", out color); break;
                case RoleType.None:
                    break;
                case RoleType.Max:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(roleType), roleType, null);
            }
            return color;
        }
    }

    public static class CharacterIconTypeExtecsion
    {
        public static string GetCharacterIconImage(this CharacterDefine.CharacterIconType iconType, string resourcesPath)
        {
            string imagePath = iconType switch
            {
                CharacterDefine.CharacterIconType.ICON_L => "_ICON_L",
                CharacterDefine.CharacterIconType.ICON_M => "_ICON_M",
                CharacterDefine.CharacterIconType.ICON_S => "_ICON_S",
                CharacterDefine.CharacterIconType.ICON_XS => "_ICON_XS",
                CharacterDefine.CharacterIconType.ICON_LONG => "_ICON_LONG",
                _ => string.Empty
            };

            return SB.Str(resourcesPath, imagePath);
        }
    }

    public static class CharacterPrefabExtension
    {
        public static string GetCharacterPrefab(this CharacterDefine.CharacterPrefabType prefabType, string resourcesPath)
        {
            string path = prefabType switch
            {
                CharacterDefine.CharacterPrefabType.CHARACTER_KINGDOM => SB.Str("Assets/Data/Character/Kingdom/"),
                CharacterDefine.CharacterPrefabType.CHARACTER_BATCH => SB.Str("Assets/Data/Character/InGame/", resourcesPath, "/"),
                _ => SB.Str("Assets/Data/Character/OutGame/", resourcesPath, "/")
            };

            string fileName;
            switch (prefabType)
            {
                case CharacterDefine.CharacterPrefabType.CHARACTER_BASE:
                    {
                        fileName = SB.Str(resourcesPath);
                        break;
                    }
                case CharacterDefine.CharacterPrefabType.CHARACTER_CUT:
                    {
                        fileName = SB.Str(resourcesPath, "_CUT");
                        fileName.Insert(1, "B");
                        break;
                    }
                case CharacterDefine.CharacterPrefabType.CHARACTER_BATCH:
                    {
                        fileName = SB.Str(resourcesPath, "_UI");
                        break;
                    }
                case CharacterDefine.CharacterPrefabType.CHARACTER_ILL:
                    {
                        fileName = resourcesPath.Insert(1, "L");
                        break;
                    }
                case CharacterDefine.CharacterPrefabType.CHARACTER_SKILL:
                    {
                        fileName = SB.Str(resourcesPath); // 결정 안됨
                        break;
                    }
                case CharacterDefine.CharacterPrefabType.CHARACTER_KINGDOM:
                    {
                        fileName = resourcesPath.Insert(1, "S");
                        fileName = SB.Str(resourcesPath, "/", fileName);
                        break;
                    }
                case CharacterDefine.CharacterPrefabType.CHARACTER_SUMMON:
                    {
                        fileName = SB.Str(resourcesPath); // 결정 안됨
                        break;
                    }
                case CharacterDefine.CharacterPrefabType.CHARACTER_STORY:
                    {
                        fileName = SB.Str(resourcesPath, "_OFF");
                        fileName.Insert(1, "E");
                        break;
                    }
                case CharacterDefine.CharacterPrefabType.CHARACTER_STAGE:
                    {
                        fileName = SB.Str(resourcesPath, "_STAGE");
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(prefabType), prefabType, null);
            }

            return SB.Str(path, fileName, ".prefab");
        }
    }

    public static class CharacterStatTypeExtecsion
    {
        public static string DisplayName(this CharacterStatType type)
        {
            string name = type switch
            {
                CharacterStatType.BaseAtk => "Str_UI_BaseAtk",
                CharacterStatType.BaseDef => "Str_UI_BaseDef",
                CharacterStatType.BaseHp => "Str_UI_BaseHp",
                CharacterStatType.BaseCri => "Str_UI_BaseCri",
                CharacterStatType.BaseCriResist => "Str_UI_BaseCriResist",
                CharacterStatType.BaseCriDmgRate => "Str_UI_BaseCriDmgRate",
                CharacterStatType.BaseAvo => "Str_UI_BaseAvo",
                CharacterStatType.BaseAcc => "Str_UI_BaseAcc",
                CharacterStatType.BaseAtkSpd => "Str_UI_BaseAtkSpd",
                CharacterStatType.BaseMoveSpd => "Str_UI_BaseMoveSpd",
                CharacterStatType.BaseHpRecoveryRate => "Str_UI_BaseHpRecoveryRate",
                _ => string.Empty
            };

            return LocalString.Get(name);
        }
    }
}