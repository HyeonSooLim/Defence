using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD
{
    public static class Runtime
    {
        public static GameData.PlayerData PlayerData = new();

        public static class HexSize
        {
            public static float Width;
            public static float Height;
            public static Vector3 StageCellHexOffset;
        }

        public static class StageInformation
        {
            public static int PlayerLife = 3;
            public static int StageSeed = 101;
            public static int StageLevel;
            public static Dictionary<int, Battle.CharacterBehavior> SpawnedCharacters = new();
            public static Dictionary<int, Battle.MonsterBehavior> SpawnedEnemies = new();
            public static Dictionary<System.ValueTuple<int, int>, Battle.CellBehavior> Player01Cells = new();
            public static Dictionary<System.ValueTuple<int, int>, Battle.CellBehavior> Player02Cells = new();
            public static int CurrentWaveIndex = 0;
        }

        public static class CharacterCombineInfo
        {
            public static Dictionary<System.ValueTuple<int, int>, int> Player01CellOnCharacters = new();
            public static Dictionary<System.ValueTuple<int, int>, int> Player02CellOnCharacters = new();
        }

        public static class BuffSetInfo
        {
            public static Dictionary<int, int> RegisteredCharacterSeed = new();
            public static Dictionary<ProjectEnum.UnitProperty, int> BuffProperties = new();
            public static Dictionary<ProjectEnum.CharacterType, int> BuffTypes = new();

            public static float GetBuffTypeValue(float baseValue, ProjectEnum.UnitProperty unitProperty, ProjectEnum.CharacterType characterType, ProjectEnum.BuffType buffType)
            {
                float value = baseValue;
                var property = unitProperty;
                if (Runtime.BuffSetInfo.BuffProperties.TryGetValue(property, out var buffProperty)
                    && Global.DataManager.UnitPropertyBuffSetMaxCount.TryGet(property, out var unitTypeBuffSetMaxCount))
                {
                    var maxBuffCount = Mathf.Min(buffProperty, unitTypeBuffSetMaxCount.MaxCount);
                    if (Global.DataManager.UnitPropertyBuffSet.TryGet((property, maxBuffCount), out var unitPropertyBuffSet, true)
                        && unitPropertyBuffSet.BuffType == buffType)
                    {
                        value += unitPropertyBuffSet.Value;
                        Utilities.InternalDebug.Log($"버프 [속성] {property}, 기본값: {baseValue}, 버프 적용값: {value}");
                    }
                }

                var type = characterType;
                if (Runtime.BuffSetInfo.BuffTypes.TryGetValue(type, out var buffCharacterTypes)
                    && Global.DataManager.UnitTypeBuffSetMaxCount.TryGet(type, out var unitTypeBuffSetMaxCount2))
                {
                    var maxBuffCount = Mathf.Min(buffCharacterTypes, unitTypeBuffSetMaxCount2.MaxCount);
                    if (Global.DataManager.UnitTypeBuffSet.TryGet((type, maxBuffCount), out var unitTypeBuffSet, true)
                        && unitTypeBuffSet.BuffType == buffType)
                    {
                        value += unitTypeBuffSet.Value;
                        Utilities.InternalDebug.Log($"버프 [타입] {type}, 기본값: {baseValue}, 버프 적용값: {value}");
                    }
                }
                return value;
            }
        }
    }
}