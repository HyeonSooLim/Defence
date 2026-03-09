using System;

namespace ProjectHD
{
    public class ProjectEnum
    {
        public enum LogType
        {
            None,
            Log_UI = 1 << 0,
            Log_Time = 1 << 1,
            Log_Network = 1 << 2,
            Log_Sound = 1 << 3,
            Log_Battle = 1 << 4,
            Log_Core = 1 << 5,
            LOG_Build = 1 << 6,
        }

        public enum PlatformMarketType
        {
            None,
            PlayStore,
            OneStore,
            AppStore,
        }

        public enum SceneName
        {
            None,

            MainWorkSpace,
            TitleWorkSpace,
            LoginWorkSpace,
            LoadingWorkSpace,

            LobbyWorkSpace,
            InGameWorkSpace,
            BattleWorkSpace,
            ResultWorkSpace,
        }

        public enum ConstDefine
        {
            StartingGoodsType,
            StartingGoodsPrice,
            RollingDicePrice,
            Dicelock1Price,
            Dicelock2Price,
            MonsterRecallTime,
            MonsterKillingReward,
            BossMonsterKillingReward,
            MonsterWaveWaitTime,
            RecyclePrice,
        }

        public enum CharacterType
        {
            None,
            Rogue,
            Ninja,
            Warrior,
            DragonKnight,
            Archer,
            Alchemist,
            Summoner,
            Mage,
            Lancer,
            Gunslinger,
            Knight,
        }

        public enum UnitProperty
        {
            None,
            Fire,
            Water,
            Earth,
            Wind,
            Light,
            Dark,
        }

        public enum CharacterCostItem
        {
            None,
            Gold,
        }

        public enum BuffType
        {
            None,
            Attack,
            AttackSpeed,
        }

        public enum DebuffType
        {
            None,
            Stun,
            Bleeding,
            Firedamage,
            TrailofFire,
            Slow,
            BlazingPath,
            StunAndSlow,
            Knockback,
        }

        public enum MonsterType
        {
            None,
            Normal,
            Elite,
            Boss,
        }

        public enum AnimationState
        {
            Stay = 0,
            Attack01 = 1,
            Attack02 = 2,
        }

        public enum PlayerType
        {
            Player01,
            Player02,
        }
    }
}
