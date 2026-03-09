using System;
using System.Collections.Generic;
using MasterMemory;
using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

namespace ProjectHD.Data
{
    public interface IDataKey<E>
    {
        public E Key { get; }
    }

    [MemoryTable("Dummy"), MessagePackObject(true)]
    public record Dummy : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; }
        public string Value { get; }

        public Dummy(int seed, string value)
        {
            Seed = seed;
            Value = value;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Seed;
    }

    [MemoryTable("ImmutableDummy"), MessagePackObject(true)]
    public record ImmutableDummy(int Seed, string Value) : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; } = Seed;
        public string Value { get; } = Value;

        [IgnoreMember][JsonIgnore] public int Key => Seed;
    }


    [MemoryTable("DummyDummy"), MessagePackObject(true)]
    public record DummyDummy(int Seed, int Seed2, string Value) : IDataKey<long>
    {
        [PrimaryKey]
        public int Seed { get; } = Seed;
        [SecondaryKey(0)]
        public int Seed2 { get; } = Seed2;
        public string Value { get; } = Value;

        [IgnoreMember]
        [JsonIgnore]
        public long Key => CalculateKey(Seed, Seed2);

        public static long CalculateKey(int seed, int seed2)
        {
            return (long)seed + ((long)seed2 << 32);
        }
    }

    [MemoryTable("TestConstValue"), MessagePackObject(true)]
    public record TestConstValue : IDataKey<ProjectEnum.ConstDefine>
    {
        [PrimaryKey]
        public ProjectHD.ProjectEnum.ConstDefine Type { get; }
        public int Val { get; }
        public string Str { get; }

        public TestConstValue(ProjectEnum.ConstDefine type, int val, string str)
        {
            Type = type;
            Val = val;
            Str = str;
        }

        [IgnoreMember]
        [JsonIgnore]
        public ProjectEnum.ConstDefine Key => Type;
    }

    [MemoryTable("GradeStat"), MessagePackObject(true)]
    public record GradeStat : IDataKey<int>
    {
        [PrimaryKey]
        public int Grade { get; }
        public float Attack { get; }
        public float Vitality { get; }

        public GradeStat(int grade, float attack, float vitality)
        {
            Grade = grade;
            Attack = attack;
            Vitality = vitality;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Grade;
    }

    [MemoryTable("UnitPropertyDefine"), MessagePackObject(true)]
    public record UnitPropertyDefine : IDataKey<ProjectHD.ProjectEnum.UnitProperty>
    {
        [PrimaryKey]
        public ProjectHD.ProjectEnum.UnitProperty Property { get; }
        public ProjectHD.ProjectEnum.UnitProperty AdventageProperty { get; }
        public ProjectHD.ProjectEnum.UnitProperty DisadventageProperty { get; }
        public float AdventageScale { get; }
        public float DisadventageScale { get; }
        public string SpawnEffectAssetKey { get; }
        public float OffsetY { get; }
        public string IconName { get; set; }

        public UnitPropertyDefine(ProjectEnum.UnitProperty property,
            ProjectEnum.UnitProperty adventageProperty,
            ProjectEnum.UnitProperty disadventageProperty,
            float adventageScale,
            float disadventageScale,
            string spawnEffectAssetKey,
            float offsetY,
            string iconName)
        {
            Property = property;
            AdventageProperty = adventageProperty;
            DisadventageProperty = disadventageProperty;
            AdventageScale = adventageScale;
            DisadventageScale = disadventageScale;
            SpawnEffectAssetKey = spawnEffectAssetKey;
            OffsetY = offsetY;
            IconName = iconName;
        }

        [IgnoreMember]
        [JsonIgnore]
        public ProjectHD.ProjectEnum.UnitProperty Key => Property;
    }

    [MemoryTable("UnitTypeDefine"), MessagePackObject(true)]
    public record UnitTypeDefine : IDataKey<ProjectHD.ProjectEnum.CharacterType>
    {
        [PrimaryKey]
        public ProjectHD.ProjectEnum.CharacterType Type { get; }
        public string IconName { get; set; }

        public UnitTypeDefine(ProjectEnum.CharacterType type,
            string iconName)
        {
            Type = type;
            IconName = iconName;
        }

        [IgnoreMember]
        [JsonIgnore]
        public ProjectHD.ProjectEnum.CharacterType Key => Type;
    }

    [MemoryTable("CharacterTable"), MessagePackObject(true)]
    public record CharacterTable : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; }
        public string CharacterNameKey { get; }
        public ProjectEnum.CharacterType CharacterType { get; }
        public ProjectEnum.UnitProperty CharacterProperty { get; }
        public int CharacterHitCount { get; }
        public int CharacterRange { get; }
        public float CharacterAttackSpeed { get; }
        public int CharacterLevelTableSeed { get; }
        public string CharacterIconName { get; }
        public string ModelAssetKey { get; }

        public CharacterTable(int seed,
            string characterNameKey,
            ProjectEnum.CharacterType characterType,
            ProjectEnum.UnitProperty characterProperty,
            int characterHitCount,
            int characterRange,
            float characterAttackSpeed,
            int characterLevelTableSeed,
            string characterIconName,
            string modelAssetKey)
        {
            Seed = seed;
            CharacterNameKey = characterNameKey;
            CharacterType = characterType;
            CharacterProperty = characterProperty;
            CharacterHitCount = characterHitCount;
            CharacterRange = characterRange;
            CharacterAttackSpeed = characterAttackSpeed;
            CharacterLevelTableSeed = characterLevelTableSeed;
            CharacterIconName = characterIconName;
            ModelAssetKey = modelAssetKey;        
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Seed;
    }

    // Wrapping
    public record CharacterPropertyGroup : IDataKey<ProjectEnum.UnitProperty>
    {
        public ProjectEnum.UnitProperty CharacterProperty { get; }
        public int Seed { get; }

        public CharacterPropertyGroup(ProjectEnum.UnitProperty characterProperty, int seed)
        {
            CharacterProperty = characterProperty;
            Seed = seed;
        }

        [IgnoreMember]
        [JsonIgnore]
        public ProjectEnum.UnitProperty Key => CharacterProperty;
    }

    [MemoryTable("CharLevelStatTable"), MessagePackObject(true)]
    public record CharLevelStatTable : IDataKey<ValueTuple<int,int>>
    {
        [PrimaryKey]
        public int Seed { get; }
        [SecondaryKey(0)]
        public int Level { get; }
        public ProjectEnum.CharacterCostItem CostItem { get; }
        public int Cost { get; }
        public int Piece { get; }
        public int Attack { get; }
        public int Vitality { get; }
        public float CriticalDmg { get; }
        public float CriticalRate { get; }
        public int BuffGroupSeed { get; }
        public int DebuffGroupSeed { get; }

        public CharLevelStatTable(int seed,
            int level,
            ProjectEnum.CharacterCostItem costItem,
            int cost,
            int piece,
            int attack,
            int vitality,
            float criticalDmg,
            float criticalRate,
            int buffGroupSeed,
            int debuffGroupSeed)
        {
            Seed = seed;
            Level = level;
            CostItem = costItem;
            Cost = cost;
            Piece = piece;
            Attack = attack;
            Vitality = vitality;
            CriticalDmg = criticalDmg;
            CriticalRate = criticalRate;
            BuffGroupSeed = buffGroupSeed;
            DebuffGroupSeed = debuffGroupSeed;
        }

        [IgnoreMember]
        [JsonIgnore]

        public ValueTuple<int,int> Key => (Seed, Level);
    }

    [MemoryTable("UnitPropertyBuffSet"), MessagePackObject(true)]
    public record UnitPropertyBuffSet : IDataKey<ValueTuple<ProjectEnum.UnitProperty, int>>
    {
        [PrimaryKey]
        public ProjectHD.ProjectEnum.UnitProperty CharacterProperty { get; }
        public int ActiveCount { get; }
        public ProjectEnum.BuffType BuffType { get; }
        public float Value { get; }
        public float Duration { get; }
        public string NameKey { get; }


        public UnitPropertyBuffSet(ProjectEnum.UnitProperty characterProperty,
            int activeCount,
            ProjectEnum.BuffType buffType,
            float value,
            float duration,
            string nameKey)
        {
            CharacterProperty = characterProperty;
            ActiveCount = activeCount;
            BuffType = buffType;
            Value = value;
            Duration = duration;
            NameKey = nameKey;
        }

        [IgnoreMember]
        [JsonIgnore]

        public ValueTuple<ProjectEnum.UnitProperty, int> Key => (CharacterProperty, ActiveCount);
    }

    [MemoryTable("UnitTypeBuffSet"), MessagePackObject(true)]
    public record UnitTypeBuffSet : IDataKey<ValueTuple<ProjectEnum.CharacterType, int>>
    {
        [PrimaryKey]
        public ProjectHD.ProjectEnum.CharacterType CharacterType { get; }
        public int ActiveCount { get; }
        public ProjectEnum.BuffType BuffType { get; }
        public float Value { get; }
        public float Duration { get; }
        public string NameKey { get; }


        public UnitTypeBuffSet(ProjectEnum.CharacterType characterType,
            int activeCount,
            ProjectEnum.BuffType buffType,
            float value,
            float duration,
            string nameKey)
        {
            CharacterType = characterType;
            ActiveCount = activeCount;
            BuffType = buffType;
            Value = value;
            Duration = duration;
            NameKey = nameKey;
        }

        [IgnoreMember]
        [JsonIgnore]

        public ValueTuple<ProjectEnum.CharacterType, int> Key => (CharacterType, ActiveCount);
    }

    public record UnitPropertyBuffSetMaxCount : IDataKey<ProjectEnum.UnitProperty>
    {
        public ProjectEnum.UnitProperty CharacterProperty { get; }
        public int MaxCount { get; }

        public UnitPropertyBuffSetMaxCount(ProjectEnum.UnitProperty characterProperty, int maxCount)
        {
            CharacterProperty = characterProperty;
            MaxCount = maxCount;
        }

        [IgnoreMember]
        [JsonIgnore]
        public ProjectEnum.UnitProperty Key => CharacterProperty;
    }

    public record UnitTypeBuffSetMaxCount : IDataKey<ProjectEnum.CharacterType>
    {
        public ProjectEnum.CharacterType CharacterType { get; }
        public int MaxCount { get; }

        public UnitTypeBuffSetMaxCount(ProjectEnum.CharacterType characterType, int maxCount)
        {
            CharacterType = characterType;
            MaxCount = maxCount;
        }

        [IgnoreMember]
        [JsonIgnore]
        public ProjectEnum.CharacterType Key => CharacterType;
    }

    [MemoryTable("MonsterTable"), MessagePackObject(true)]
    public record MonsterTable : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; }
        public string CharacterNameKey { get; }
        public ProjectEnum.MonsterType MonsterType { get; }
        public ProjectEnum.UnitProperty MonsterProperty { get; }
        public int MonsterLevelTableSeed { get; }
        public string MonsterIconName { get; }
        public string ModelAssetKey { get; }

        public MonsterTable(int seed,
            string characterNameKey,
            ProjectEnum.MonsterType monsterType,
            ProjectEnum.UnitProperty monsterProperty,
            int monsterLevelTableSeed,
            string monsterIconName,
            string modelAssetKey)
        {
            Seed = seed;
            CharacterNameKey = characterNameKey;
            MonsterType = monsterType;
            MonsterProperty = monsterProperty;
            MonsterLevelTableSeed = monsterLevelTableSeed;
            MonsterIconName = monsterIconName;
            ModelAssetKey = modelAssetKey;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Seed;
    }

    [MemoryTable("MonsterLevelStatTable"), MessagePackObject(true)]
    public record MonsterLevelStatTable : IDataKey<ValueTuple<int,int>>
    {
        [PrimaryKey]
        public int Seed { get; }
        [SecondaryKey(0)]
        public int Level { get; }
        public int Vitality { get; }
        public int MoveSpeed { get; }
        public int DebuffGroupSeed { get; }

        public MonsterLevelStatTable(int seed,
            int level,
            int vitality,
            int moveSpeed,
            int debuffGroupSeed)
        {
            Seed = seed;
            Level = level;
            Vitality = vitality;
            MoveSpeed = moveSpeed;
            DebuffGroupSeed = debuffGroupSeed;
        }

        [IgnoreMember]
        [JsonIgnore]

        public ValueTuple<int, int> Key => (Seed, Level);
    }

    [MemoryTable("BuffGroup"), MessagePackObject(true)]
    public record BuffGroup : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; }
        public ProjectEnum.BuffType Type { get; }
        public float Duration { get; }
        public float Value_1 { get; }
        public float Value_2 { get; }
        public string EffectAssetKey { get; }

        public BuffGroup(int seed,
            ProjectEnum.BuffType type,
            float duration,
            float value_1,
            float value_2,
            string effectAssetKey)
        {
            Seed = seed;
            Type = type;
            Duration = duration;
            Value_1 = value_1;
            Value_2 = value_2;
            EffectAssetKey = effectAssetKey;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Seed;
    }

    [MemoryTable("DebuffGroup"), MessagePackObject(true)]
    public record DebuffGroup : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; }
        public ProjectEnum.DebuffType Type { get; }
        public float Duration { get; }
        public float Value_1 { get; }
        public float Value_2 { get; }
        public string EffectAssetKey { get; }

        public DebuffGroup(int seed,
            ProjectEnum.DebuffType type,
            float duration,
            float value_1,
            float value_2,
            string effectAssetKey)
        {
            Seed = seed;
            Type = type;
            Duration = duration;
            Value_1 = value_1;
            Value_2 = value_2;
            EffectAssetKey = effectAssetKey;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Seed;
    }

    [MemoryTable("GameItem"), MessagePackObject(true)]
    public record GameItem : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; }
        //public protocol.ITEM_DIVISION Division { get; }
        //public protocol.GAME_ITEM_TYPE Type { get; }
        public int Grade { get; }
        public int Order { get; }
        public bool IsPile { get; }
        public bool IsPreprocessing { get; }
        public bool IsConsumable { get; }
        public bool IsStorageCounting { get; }
        public bool IsAvailableInStorage { get; }
        public int LinkedSeed { get; }
        public int Reserved1 { get; }
        public int Reserved2 { get; }
        public int Reserved3 { get; }
        public string TitleKey { get; }
        public string DevName { get; }
        public string ContentsKey { get; }
        public int ItemQuantitymax { get; }
        //public string UseKey { get; }
        //public string TargetKey { get; }
        public bool IsDecreasable { get; }
        public string IconName { get; }

        public GameItem(int seed,
            //protocol.ITEM_DIVISION division,
            //protocol.GAME_ITEM_TYPE type,
           int grade,
            int order,
            bool isPile,
            bool isPreprocessing,
            bool isConsumable,
            bool isStorageCounting,
            bool isAvailableInStorage,
            int linkedSeed,
            int reserved1,
            int reserved2,
            int reserved3,
            string titleKey,
            string devName,
            string contentsKey,
            int itemQuantityMax,
            //string useKey,
            //string targetKey,
            bool isDecreasable,
            string iconName)
        {
            Seed = seed;
            //Division = division;
            //Type = type;
            Grade = grade;
            Order = order;
            IsPile = isPile;
            IsPreprocessing = isPreprocessing;
            IsConsumable = isConsumable;
            IsStorageCounting = isStorageCounting;
            IsAvailableInStorage = isAvailableInStorage;
            LinkedSeed = linkedSeed;
            Reserved1 = reserved1;
            Reserved2 = reserved2;
            Reserved3 = reserved3;
            TitleKey = titleKey;
            DevName = devName;
            ContentsKey = contentsKey;
            ItemQuantitymax = itemQuantityMax;
            //UseKey = useKey;
            //TargetKey = targetKey;
            IsDecreasable = isDecreasable;
            IconName = iconName;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Seed;
    }

    [MemoryTable("StageTable"), MessagePackObject(true)]
    public record StageTable : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; }
        public string StageNameKey { get; }
        public int WaveGroupSeed { get; }
        public int NextStagePoint { get; }
        public string StageIconName { get; }
        public string SceneAssetKey { get; }

        public StageTable(int seed,
            string stageNameKey,
            int waveGroupSeed,
            int nextStagePoint,
            string stageIconName,
            string sceneAssetKey)
        {
            Seed = seed;
            StageNameKey = stageNameKey;
            WaveGroupSeed = waveGroupSeed;
            NextStagePoint = nextStagePoint;
            StageIconName = stageIconName;
            SceneAssetKey = sceneAssetKey;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Seed;
    }

    [MemoryTable("StageWaveGroup"), MessagePackObject(true)]
    public record StageWaveGroup : IDataKey<int>
    {
        [PrimaryKey]
        public int Seed { get; }
        public int[] MonsterSeeds { get; }
        public int MonsterCount { get; }

        public StageWaveGroup(int seed,
            int[] monsterSeeds,
            int monsterCount)
        {
            Seed = seed;
            MonsterSeeds = monsterSeeds;
            MonsterCount = monsterCount;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => Seed;
    }

    [MemoryTable("StagePointReward"), MessagePackObject(true)]
    public record StagePointReward : IDataKey<int>
    {
        [MessagePackObject(true)]
        public record RewardItem
        {
            public int ItemSeed { get; }    // 시트 열 이름 FreeReward.ItemSeed 와 PremiumReward.ItemSeed
            public int ItemCount { get; }   // 시트 열 이름 FreeReward.ItemCount 와 PremiumReward.ItemCount
            public RewardItem(int itemSeed, int itemCount)
            {
                ItemSeed = itemSeed;
                ItemCount = itemCount;
            }
        }

        [PrimaryKey]
        public int StagePoint { get; }
        public RewardItem FreeReward { get; }
        public RewardItem PremiumReward { get; }
        public ProjectEnum.ConstDefine Type { get; }

        public StagePointReward(int stagePoint,
            RewardItem freeReward,
            RewardItem premiumReward,
            ProjectEnum.ConstDefine type)
        {
            StagePoint = stagePoint;
            FreeReward = freeReward;
            PremiumReward = premiumReward;
            Type = type;
        }

        [IgnoreMember]
        [JsonIgnore]
        public int Key => StagePoint;
    }       
}
