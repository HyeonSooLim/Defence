
namespace ProjectHD.Event
{
    /// <summary>
    /// Events에 Static으로 새로 만들 거나 EventPool에서 가져오거나 선택(반환 필수)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Events
    {
        public static SceneLoadingEvent SceneLoadingEvent = new();
        public static NextSceneLoadCompleteEvent NextSceneLoadCompleteEvent = new();
        public static SceneLoadingCompleteEvent SceneLoadingCompleteEvent = new();
        public static ManagerInitialzeCompleteEvent ManagerInitialzeCompleteEvent = new();
        public static ManagerUnloadEvent ManagerUnloadEvent = new();
        public static SetActiveSceneEvent SetActiveSceneEvent = new();

        public static MainCameraSetActiveEvent MainCameraSetActiveEvent = new();
        public static StageSettingEvent StageSettingEvent = new();

        public static SpawnEffectEvent SpawnEffectEvent = new();

        public static RollTheDiceEvent RollTheDiceEvent = new();
        public static DiceResultEvent DiceResultEvent = new();
        public static CameraShakeEvent CameraShakeEvent = new();

        public static CharacterOnCellEvent CharacterOnCellEvent = new();
        public static CharacterCombineEvent CharacterCombineEvent = new();
        public static CharacterGradeUpEvent CharacterGradeUpEvent = new();
        public static CharacterDisappearEvent CharacterDisappearEvent = new();
        public static CharacterOnDraggingEvent CharacterOnDraggingEvent = new();
        public static CharacterDragEndEvent CharacterDragEndEvent = new();

        public static MonsterGoalInEvent MonsterGoalInEvent = new();
        public static MonsterHexUpdateEvent MonsterHexUpdateEvent = new();
        public static MonsterHealthUpdateEvent MonsterHealthUpdateEvent = new();
        public static MonsterMoveEvent MonsterMoveEvent = new();
        public static MonsterDieEvent MonsterDisappearEvent = new();
        public static SendDamageEvent SendDamageEvent = new();
        public static CalculatedDamageEvent CalculatedDamageEvent = new();
        public static MonsterDieEvent MonsterDieEvent = new();

        public static UpdateBuffSetEvent UpdateBuffSetEvent = new();

        public static RecycleEnterEvent RecycleEnterEvent = new();
        public static RecycleUseEvent RecycleUseEvent = new();

        public static ChangeCoinEvent ChangeCoinEvent = new();
    }

    #region Scene Events

    public class SceneLoadingEvent : GameEvent
    {
        public float Progress;
        public override void Reset()
        {
            Progress = 0;
        }
    }

    public class NextSceneLoadCompleteEvent : GameEvent
    {
        public ProjectEnum.SceneName CurrentSceneName;
        public override void Reset()
        {
            CurrentSceneName = ProjectEnum.SceneName.None;
        }
    }

    public class SceneLoadingCompleteEvent : GameEvent
    {
        public override void Reset()
        {
        }
    }

    public class ManagerInitialzeCompleteEvent : GameEvent
    {
        public bool IsDone;
        public override void Reset()
        {
            IsDone = false;
        }
    }

    public class ManagerUnloadEvent : GameEvent
    {
        public override void Reset()
        {
        }
    }

    public class SetActiveSceneEvent : GameEvent
    {
        public UnityEngine.SceneManagement.Scene Scene;
        public override void Reset()
        {
            Scene = default;
        }
    }

    #endregion

    #region Camera Events

    public class MainCameraSetActiveEvent : GameEvent
    {
        public bool IsOnCameraRoot;
        public bool IsOnArm;
        public bool IsOnMainCamera;
        public override void Reset()
        {
            IsOnCameraRoot = false;
            IsOnArm = false;
            IsOnMainCamera = false;
        }
    }

    public class CameraShakeEvent : GameEvent
    {
        public float Duration;
        public float Magnitude;
        public override void Reset()
        {
            Duration = 0;
            Magnitude = 0;
        }
    }

    #endregion

    #region Effect Events

    public class SpawnEffectEvent : GameEvent
    {
        public string AssetKey;
        public float Duration = 1.5f;
        public UnityEngine.Transform Transform;
        public override void Reset()
        {
            AssetKey = string.Empty;
            Duration = 1.5f;
            Transform = null;
        }
    }

    #endregion

    #region Battle Events

    public class StageSettingEvent : GameEvent
    {
        public int StageSeed;
        public int PlayerLife;
        // 스테이지 정보
        public override void Reset()
        {
            StageSeed = 0;
            PlayerLife = 0;
        }
    }

    public class RollTheDiceEvent : GameEvent
    {
        public override void Reset()
        {
        }
    }

    public class DiceResultEvent : GameEvent
    {
        public ProjectEnum.UnitProperty Property;
        // 필요하다면 value 추가
        public override void Reset()
        {
            Property = ProjectEnum.UnitProperty.None;
        }
    }

    public class StageWaveStartEvent : GameEvent
    {
        public int StageSeed;
        public int StageWaveIndex;
        public override void Reset()
        {
            StageSeed = 0;
            StageWaveIndex = 0;
        }
    }

    public class StageWaveCompleteEvent : GameEvent
    {
        public int StageSeed;
        public int StageWaveIndex;
        public override void Reset()
        {
            StageSeed = 0;
            StageWaveIndex = 0;
        }
    }

    public class StageWaveStartRemainTimeEvent : GameEvent
    {
        public int RemainTime;
        public override void Reset()
        {
            RemainTime = 0;
        }
    }

    public class CharacterSpawnEvent : GameEvent
    {
        public int Seed;
        public int Level;
        public int Grade;
        public override void Reset()
        {
            Seed = 0;
            Level = 0;
            Grade = 0;
        }
    }

    public class CharacterOnCellEvent : GameEvent
    {
        public int InstanceID;
        public System.ValueTuple<int, int> PreviousHex;
        public System.ValueTuple<int, int> CurrentHex;
        public bool IsFirst;
        public override void Reset()
        {
            InstanceID = 0;
            IsFirst = false;
            PreviousHex = (0, 0);
            CurrentHex = (0, 0);
        }
    }

    public class CharacterCombineEvent : GameEvent
    {
        public int SourceInstanceID;    // 합성 주체(레벨업)
        public int TargetInstanceID;    // 합성 대상(소멸)
        public override void Reset()
        {
            SourceInstanceID = 0;
            TargetInstanceID = 0;
        }
    }

    public class CharacterGradeUpEvent : GameEvent
    {
        public int InstanceID;
        public int CurrentGrade;
        public int NextGrade;
        public override void Reset()
        {
            InstanceID = 0;
            CurrentGrade = 0;
            NextGrade = 0;
        }
    }

    public class CharacterDisappearEvent : GameEvent
    {
        public int InstanceID;
        public int CharacterSeed;
        public ProjectEnum.UnitProperty Property;
        public ProjectEnum.CharacterType CharacterType;
        public ProjectEnum.PlayerType PlayerType;
        public override void Reset()
        {
            InstanceID = 0;
            CharacterSeed = 0;
            Property = ProjectEnum.UnitProperty.None;
            CharacterType = ProjectEnum.CharacterType.None;
            PlayerType = default;
        }
    }

    public class CharacterOnDraggingEvent : GameEvent
    {
        public int InstanceID;
        public UnityEngine.Vector3 Position;
        public override void Reset()
        {
            InstanceID = 0;
            Position = UnityEngine.Vector3.zero;
        }
    }
    
    public class CharacterDragEndEvent : GameEvent
    {
        public int InstanceID;
        public UnityEngine.Vector3 Position;
        public override void Reset()
        {
            InstanceID = 0;
            Position = UnityEngine.Vector3.zero;
        }
    }

    public class MonsterGoalInEvent : GameEvent
    {
        public int InstanceID;
        public override void Reset()
        {
            InstanceID = 0;
        }
    }

    public class MonsterHexUpdateEvent : GameEvent
    {
        public int InstanceID;
        public override void Reset()
        {
            InstanceID = 0;
        }
    }

    public class MonsterMoveEvent : GameEvent
    {
        public int InstanceID;
        public override void Reset()
        {
            InstanceID = 0;
        }
    }

    public class MonsterHealthUpdateEvent : GameEvent
    {
        public int InstanceID;
        public float CurrentHealth;
        public override void Reset()
        {
            InstanceID = 0;
            CurrentHealth = 0;
        }
    }

    public class MonsterDieEvent : GameEvent
    {
        public int InstanceID;
        public override void Reset()
        {
            InstanceID = 0;
        }
    }

    public class SendDamageEvent : GameEvent
    {
        public Battle.IAttackable Attackable;
        public Battle.IDamageable Damageable;
        public int BaseDamage;
        public override void Reset()
        {
            Attackable = null;
            Damageable = null;
            BaseDamage = 0;
        }
    }

    public class CalculatedDamageEvent : GameEvent
    {
        public int FinalDamage;
        public Battle.IDamageable Damageable;
        public override void Reset()
        {
            FinalDamage = 0;
            Damageable = null;
        }
    }

    public class UpdateBuffSetEvent : GameEvent
    {
        public override void Reset()
        {
        }
    }

    public class RecycleEnterEvent : GameEvent
    {
        public bool IsOn;
        public override void Reset()
        {
            IsOn = false;
        }
    }

    public class RecycleUseEvent : GameEvent
    {
        public int InstanceID;
        public int CharacterSeed;
        public ProjectEnum.UnitProperty Property;
        public ProjectEnum.CharacterType CharacterType;
        public ProjectEnum.PlayerType PlayerType;
        public override void Reset()
        {
            InstanceID = 0;
            CharacterSeed = 0;
            Property = ProjectEnum.UnitProperty.None;
            CharacterType = ProjectEnum.CharacterType.None;
            PlayerType = default;
        }
    }

    public class ChangeCoinEvent : GameEvent
    {
        public int Amount;
        public override void Reset()
        {
            Amount = 0;
        }
    }

    #endregion
}
