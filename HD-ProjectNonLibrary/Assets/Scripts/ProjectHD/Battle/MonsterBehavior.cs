using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectHD.Battle
{
    public class MonsterBehavior : MonoBehaviour, IDamageable, IHexhable
    {
        [SerializeField] private NavMeshAgent _navMeshAgent;
        [SerializeField] private Animator _animator;

        private PathData _pathData;
        private int _waypointIndex;

        private int _monsterSeed;
        private int _tempLevel = 1;
        private float _vitality = 0;
        private bool _isDie;
        private bool _isBoss;
        public bool IsDie => _isDie;
        public bool IsBoss => _isBoss;

        private int _currentHexQ = int.MinValue;
        private int _currentHexR = int.MinValue;
        public int CurrentHexQ => _currentHexQ;
        public int CurrentHexR => _currentHexR;

        public async UniTask Consturct(int monsterSeed, PathData pathData)
        {
            if (!Global.DataManager.MonsterTable.TryGet(monsterSeed, out var monsterTable))
                return;

            if (!Global.DataManager.MonsterLevelStatTable.TryGet((monsterTable.MonsterLevelTableSeed, _tempLevel), out var monsterLevelStatTable))
                return;

            // stat
            _monsterSeed = monsterSeed;
            _vitality = monsterLevelStatTable.Vitality;
            _isDie = false;
            _isBoss = monsterTable.MonsterType == ProjectEnum.MonsterType.Boss;

            _waypointIndex = 0;
            _pathData = pathData;
            var spawnPoint = pathData.StartPoint;
            _navMeshAgent.enabled = false;
            transform.SetParent(spawnPoint);
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            transform.localScale = Vector3.one;
            _navMeshAgent.enabled = true;
            _navMeshAgent.ResetPath();

            await UniTask.WaitUntil(() => _navMeshAgent.isOnNavMesh);
            await UniTask.Yield();

            _navMeshAgent.speed = monsterLevelStatTable.MoveSpeed;
            _navMeshAgent.acceleration = 8f;
            _navMeshAgent.stoppingDistance = 1f;
            _navMeshAgent.avoidancePriority = Random.Range(30, 70);
            _navMeshAgent.autoBraking = false;

            _animator.CrossFadeInFixedTime("Run", 0.1f);
            ExecuteMonsterHealthUpdateEvent(_vitality);
        }

        public void Destruct()
        {
            _monsterSeed = 0;
            _isDie = true;
            _vitality = 0;
            _navMeshAgent.enabled = false;
            _pathData = null;
        }

        public void HandleMonster()
        {
            if (!_navMeshAgent.isOnNavMesh)
                return;

            UpdateHexAndExecuteEvent();
            ExecuteMonsterMoveEvent();

            if (IsPathFindingWayPoint())
                return;

            bool isGoalIn = HasReachedGoal();
            if (isGoalIn)
            {
                ExecuteGoalInEvent();
            }
        }

        private bool IsPathFindingWayPoint()
        {
            bool isPathFinding = true;
            if (!HasReachedGoal())
                return isPathFinding;

            if (_pathData.WayPoints.Count > _waypointIndex)
            {
                _navMeshAgent.SetDestination(_pathData.WayPoints[_waypointIndex].position);
                _waypointIndex++;
            }
            else
            {
                isPathFinding = false;
            }
            return isPathFinding;
        }

        private bool HasReachedGoal()
        {
            return !_navMeshAgent.pathPending &&
                   _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance &&
                   (!_navMeshAgent.hasPath || _navMeshAgent.velocity.sqrMagnitude < 0.05f);
        }

        public void UpdateHexAndExecuteEvent(bool isFirst = false)
        {
            var hex = GetCurrentHex();
            if (hex.x != _currentHexQ || hex.y != _currentHexR)
            {
                _currentHexQ = hex.x;
                _currentHexR = hex.y;
                ChageHexEvent();
            }
        }

        private Vector2Int GetCurrentHex()
        {
            return StaticMethod.WorldToHex(Runtime.HexSize.Width, Runtime.HexSize.Height, transform.position, Runtime.HexSize.StageCellHexOffset);
        }

        #region Interface
        public void TakeDamage(int damage)
        {
            _vitality -= damage;
            ExecuteMonsterHealthUpdateEvent(_vitality);
            Utilities.InternalDebug.Log($"데미지: {damage}, 몬스터 현재 체력: {_vitality}");
            _isDie = _vitality <= 0;
            if (_isDie)
            {
                ExecuteDieEvent();
            }
        }

        public ProjectEnum.UnitProperty GetUnitProperty()
        {
            var property = ProjectEnum.UnitProperty.None;
            if (Global.DataManager.MonsterTable.TryGet(_monsterSeed, out var monsterTable))
            {
                property = monsterTable.MonsterProperty;
            }
            return property;
        }

        #endregion

        #region Events

        private void ExecuteGoalInEvent()
        {
            var monsterGoalEvent = Event.Events.MonsterGoalInEvent;
            monsterGoalEvent.InstanceID = GetInstanceID();
            Event.EventManager.Broadcast(monsterGoalEvent);
        }

        private void ExecuteDieEvent()
        {
            var monsterDieEvent = Event.Events.MonsterDieEvent;
            monsterDieEvent.InstanceID = GetInstanceID();
            Event.EventManager.Broadcast(monsterDieEvent);
        }

        private void ChageHexEvent()
        {
            var changeHexEvent = Event.Events.MonsterHexUpdateEvent;
            changeHexEvent.InstanceID = GetInstanceID();
            Event.EventManager.Broadcast(changeHexEvent);
            Utilities.InternalDebug.Log($"몬스터 셀 좌표 위치:({_currentHexQ},{_currentHexR})");
        }

        private void ExecuteMonsterMoveEvent()
        {
            var tempEvent = Event.Events.MonsterMoveEvent;
            tempEvent.InstanceID = GetInstanceID();
            Event.EventManager.Broadcast(tempEvent);
        }

        private void ExecuteMonsterHealthUpdateEvent(float vitality)
        {
            var tempEvent = Event.Events.MonsterHealthUpdateEvent;
            tempEvent.InstanceID = GetInstanceID();
            tempEvent.CurrentHealth = Mathf.Max(0, vitality);
            Event.EventManager.Broadcast(tempEvent);
        }

        #endregion

#if UNITY_EDITOR
        [Button]
        private void SetComponent()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
        }
#endif
    }
}