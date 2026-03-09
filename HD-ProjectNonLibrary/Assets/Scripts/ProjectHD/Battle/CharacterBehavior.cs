using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Battle
{
    // TO DO : 캐릭터 배치 이벤트가 일어날 때 자신의 공격 범위 셀 저장(갱신)
    // 몬스터는 자신이 이동 중인 셀이 변할 때 Broadcast
    // 몬스터의 이동 Broadcast에 반응하여 자신의 공격 범위 안인지 검사
    // 자신의 히트 카운트 만큼 가까운 적에게 SendDamage 이벤트 Broadcast
    [RequireComponent(typeof(Animator))]
    public class CharacterBehavior : MonoBehaviour, IAttackable, IHexhable
    {
        public const string STAY_ANIMATION = "Stay";

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _point;
        [SerializeField] private DragHandler _dragHandler;

        private float _attackDelay;
        private int _level;
        private int _grade;
        private Data.CharacterTable _characterTable;
        private MonsterBehavior _target;
        private ProjectEnum.PlayerType _playerType;

        private bool _isDragging;
        private bool _isRecycleOn;

        private int _previousHexQ = int.MinValue;
        private int _previousHexR = int.MinValue;
        private int _currentHexQ = int.MinValue;
        private int _currentHexR = int.MinValue;

        public int Level => _level;
        public int Grade => _grade;
        public Data.CharacterTable CharacterTable => _characterTable;
        public ProjectEnum.PlayerType PlayerType => _playerType;
        public int CurrentHexQ => _currentHexQ;
        public int CurrentHexR => _currentHexR;

        public void Construct(int seed, int level, int grade, ProjectEnum.PlayerType playerType)
        {
            if (!Global.DataManager.CharacterTable.TryGet(seed, out _characterTable))
                Utilities.InternalDebug.LogError($"Invalid CharacterTable Seed");

            Event.EventManager.AddListener<Event.MonsterHexUpdateEvent>(MonsterHexUpdateAction);
            Event.EventManager.AddListener<Event.MonsterGoalInEvent>(MonsterGoalInAction);
            Event.EventManager.AddListener<Event.MonsterDieEvent>(MonsterDieAction);
            Event.EventManager.AddListener<Event.CharacterGradeUpEvent>(CharacterGradeUpAction);
            Event.EventManager.AddListener<Event.RecycleEnterEvent>(RecycleOnAction);

            _isDragging = false;
            _isRecycleOn = false;

            _level = level;
            _grade = grade;
            _playerType = playerType;
            UpdateHexAndExecuteEvent(true);
            SetDragHandler();
            SetGrade(_grade);
        }

        public void Destruct()
        {
            Event.EventManager.RemoveListener<Event.MonsterHexUpdateEvent>(MonsterHexUpdateAction);
            Event.EventManager.RemoveListener<Event.MonsterGoalInEvent>(MonsterGoalInAction);
            Event.EventManager.RemoveListener<Event.MonsterDieEvent>(MonsterDieAction);
            Event.EventManager.RemoveListener<Event.CharacterGradeUpEvent>(CharacterGradeUpAction);
            Event.EventManager.RemoveListener<Event.RecycleEnterEvent>(RecycleOnAction);

            _previousHexQ = int.MinValue;
            _previousHexR = int.MinValue;
            _currentHexQ = int.MinValue;
            _currentHexR = int.MinValue;
            _attackDelay = 0f;
            _level = 0;
            _grade = 0;
            _characterTable = null;
            _isDragging = false;
            UnSetDragHandler();
        }

        private void SetGrade(int grade)
        {
            _grade = grade;
            Utilities.InternalDebug.Log($"캐릭터 등급이 {_grade}로 변경되었습니다.");
        }

        public void CharacterHandle()
        {
            if (_animator != null)
            {
                if (IsStayAnimation())
                {
                    if (_attackDelay > 0)
                    {
                        _attackDelay -= Time.deltaTime;
                        return;
                    }

                    if (_isDragging)
                        return;

                    if (_target == null)
                        return;

                    if (!CanAttack(_target))
                    {
                        _target = null;
                        return;
                    }

                    _attackDelay = GetCurrentAttackDelay();
                    PlayAttackAnimation();
                }
            }
        }

        private void PlayAttackAnimation()
        {
            var randomAnimationIndex = Random.Range(1, 3);
            var randomAttack = (ProjectEnum.AnimationState)randomAnimationIndex;
            _animator.CrossFadeInFixedTime(randomAttack.ToString(), 0.2f);
        }

        public void PlayStayAnimation(float fixedDuration = 0.2f)
        {
            _animator.CrossFadeInFixedTime(STAY_ANIMATION, fixedDuration);
        }

        private void UpdateTarget()
        {
            if (_target != null && CanAttack(_target))
                return;

            foreach(var monster in Runtime.StageInformation.SpawnedEnemies.Values)
            {
                if (CanAttack(monster))
                {
                    if (_target == null)
                        _target = monster;
                    else
                    {
                        var toTargetDistance = Vector3.Distance(_target.transform.position, transform.position);
                        var toMonsterDistance = Vector3.Distance(monster.transform.position, transform.position);
                        if (toMonsterDistance < toTargetDistance)   // 더 가깝다면
                            _target = monster;
                    }
                }
            }
        }

        private bool CanAttack(MonsterBehavior target)
        {
            return MonsterHexDistance(target) <= _characterTable.CharacterRange && !target.IsDie;
        }

        #region Interface

        public void SpawnEffect(string key)
        {
            if (_animator == null)
                return;
            if (key.IsNullOrEmpty())
                return;

            var point = _point ? _point : transform;
            ExecuteSpawnEffectEvent(key, point);
        }

        public void SendDamage()
        {
            if (Global.DataManager.CharLevelStatTable.TryGet((_characterTable.CharacterLevelTableSeed, _level), out var charLevelStatTable)
                && Global.DataManager.GradeStat.TryGet(_grade, out var gradeStat))
            {
                int attack = Mathf.RoundToInt(charLevelStatTable.Attack * gradeStat.Attack);
                ExecuteSendDamageEvent(attack); // 기본 공격력에 등급 공격력 곱함(버프는 이후 계산) => DamageController에서 처리
            }
        }

        public ProjectEnum.UnitProperty GetUnitProperty()
        {
            return _characterTable.CharacterProperty;
        }

        public ProjectEnum.CharacterType GetCharacterType()
        {
            return _characterTable.CharacterType;
        }

        public void UpdateHexAndExecuteEvent(bool isFirst = false)
        {
            var hex = GetCurrentHex();
            if (hex.x != _currentHexQ || hex.y != _currentHexR)
            {
                _previousHexQ = _currentHexQ;
                _previousHexR = _currentHexR;
                _currentHexQ = hex.x;
                _currentHexR = hex.y;
                ExecuteCharacterOnCellEvent(isFirst);
                Utilities.InternalDebug.Log($"캐릭터 셀 좌표 위치:({_currentHexQ},{_currentHexR}) 시드:({_characterTable.Seed})");
            }
        }

        private Vector2Int GetCurrentHex()
        {
            return StaticMethod.WorldToHex(Runtime.HexSize.Width, Runtime.HexSize.Height, transform.position, Runtime.HexSize.StageCellHexOffset);
        }

        private bool IsStayAnimation()
        {
            return _animator.GetCurrentAnimatorStateInfo(0).IsName(STAY_ANIMATION);
        }

        #endregion

        #region Drag

        private void SetDragHandler()
        {
            _dragHandler.EventClear();
            _dragHandler.OnDragAction += OnDrag;
            _dragHandler.OnDragStateChangedAction += OnDragStateChanged;
        }

        private void UnSetDragHandler()
        {
            _dragHandler.EventClear();
        }

        private void OnDrag(Vector3 position)
        {
            if (!IsStayAnimation())
                PlayStayAnimation();

            // 모델이 위치할 깊이 (카메라에서 얼마나 떨어져 있는지)
            float modelDepth = CameraManager.Instance.MainCamera.WorldToScreenPoint(transform.position).z;

            // 마우스 위치를 월드 좌표로 변환
            Vector3 worldPos = CameraManager.Instance.MainCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, modelDepth));

            // 모델 위치 갱신
            transform.position = new (worldPos.x, transform.position.y, worldPos.z);
            ExecuteCharacterOnDraggingEvent(transform.position);
        }

        private void OnDragStateChanged(bool isDragging)
        {
            _isDragging = isDragging;

            if (!isDragging)
            {
                if (_isRecycleOn)
                {
                    ExecuteChangeCoinEvent();
                    ExecuteRecycleUseEvent();
                    return;
                }

                var hex = GetCurrentHex();
                var playerCellonCharacters = _playerType == ProjectEnum.PlayerType.Player01 ?
                    Runtime.CharacterCombineInfo.Player01CellOnCharacters : Runtime.CharacterCombineInfo.Player02CellOnCharacters;

                var key = (hex.x, hex.y);
                var checkPlayerCells = _playerType == ProjectEnum.PlayerType.Player01 ?
                    Runtime.StageInformation.Player01Cells : Runtime.StageInformation.Player02Cells;

                if (!checkPlayerCells.ContainsKey(key)) // 플레이어의 영역(1,2 혹은 그외)이 아니라면
                {
                    if (checkPlayerCells.TryGetValue((_currentHexQ, _currentHexR), out var cellBehavior))
                    {
                        MoveToCellPosition(cellBehavior);
                        UpdateTarget();
                        ExecuteCharacterDragEndEvent(cellBehavior.transform.position);
                    }
                    else
                    {
                        Utilities.InternalDebug.Log($"쉘 좌표 데이터를 확인해주세요");
                    }
                    return;
                }

                if (playerCellonCharacters.ContainsKey(key))    // 해당 칸에 캐릭터가 있다면
                {
                    var targetInstanceID = playerCellonCharacters[key];
                    if (Runtime.StageInformation.SpawnedCharacters.TryGetValue(targetInstanceID, out var characterObject)
                        && characterObject.TryGetComponent<CharacterBehavior>(out var characterBehavior))
                    {
                        RequestCharacterCombineEvent(targetInstanceID);
                    }
                }
                else
                {
                    var playerCells = _playerType == ProjectEnum.PlayerType.Player01 ?
                        Runtime.StageInformation.Player01Cells : Runtime.StageInformation.Player02Cells;
                    if (playerCells.TryGetValue(key, out var cellBehavior))
                    {
                        MoveToCellPosition(cellBehavior);
                        UpdateHexAndExecuteEvent();
                    }
                }

                UpdateTarget();
            }
        }

        private void MoveToCellPosition(CellBehavior cellBehavior)
        {
            transform.SetLocalPositionAndRotation(cellBehavior.transform.localPosition, Quaternion.identity);
        }

        #endregion

        #region Events

        private void ExecuteSpawnEffectEvent(string key, Transform point)
        {
            var tempEvent = Event.Events.SpawnEffectEvent;
            tempEvent.AssetKey = key;
            tempEvent.Transform = point;
            Event.EventManager.Broadcast(tempEvent);
        }

        private void ExecuteSendDamageEvent(int damage)
        {
            if (_target != null && !_target.IsDie)
            {
                var tempEvent = Event.Events.SendDamageEvent;
                tempEvent.Attackable = this;
                tempEvent.Damageable = _target;
                tempEvent.BaseDamage = damage;
                Event.EventManager.Broadcast(tempEvent);
                //_target.TakeDamage(damage);
            }
        }

        private void ExecuteCharacterOnCellEvent(bool isFirst)
        {
            var tempEvent = Event.Events.CharacterOnCellEvent;
            tempEvent.InstanceID = GetInstanceID();
            tempEvent.PreviousHex = (_previousHexQ, _previousHexR);
            tempEvent.CurrentHex = (_currentHexQ, _currentHexR);
            tempEvent.IsFirst = isFirst;
            Event.EventManager.Broadcast(tempEvent);
        }

        private void RequestCharacterCombineEvent(int targetInstanceID)
        {
            var tempEvent = Event.Events.CharacterCombineEvent;
            tempEvent.SourceInstanceID = GetInstanceID();
            tempEvent.TargetInstanceID = targetInstanceID;
            Event.EventManager.Broadcast(tempEvent, (result) =>
            {
                if (result.IsSuccess)
                {
                    UpdateHexAndExecuteEvent();
                }

                var checkPlayerCells = _playerType == ProjectEnum.PlayerType.Player01 ?
                    Runtime.StageInformation.Player01Cells : Runtime.StageInformation.Player02Cells;
                if (checkPlayerCells.TryGetValue((_currentHexQ, _currentHexR), out var cellBehavior))
                {
                    MoveToCellPosition(cellBehavior);
                    ExecuteCharacterDragEndEvent(cellBehavior.transform.position);
                }

            });
        }

        private void ExecuteCharacterOnDraggingEvent(Vector3 position)
        {
            var tempEvent = Event.Events.CharacterOnDraggingEvent;
            tempEvent.InstanceID = GetInstanceID();
            tempEvent.Position = position;
            Event.EventManager.Broadcast(tempEvent);
        }

        private void ExecuteCharacterDragEndEvent(Vector3 position)
        {
            var tempEvent = Event.Events.CharacterDragEndEvent;
            tempEvent.InstanceID = GetInstanceID();
            tempEvent.Position = position;
            Event.EventManager.Broadcast(tempEvent);
        }

        private void ExecuteRecycleUseEvent()
        {
            var tempEvent = Event.Events.RecycleUseEvent;
            tempEvent.InstanceID = GetInstanceID();
            Event.EventManager.Broadcast(tempEvent);
        }

        private void ExecuteChangeCoinEvent()
        {
            var tempEvent = Event.Events.ChangeCoinEvent;
            int coin = 0;
            if (Global.DataManager.TestConstValue.TryGet(ProjectEnum.ConstDefine.RecyclePrice, out var constValue))
            {
                coin = constValue.Val;
            }
            coin *= _grade; // 등급에 비례하여 획득
            tempEvent.Amount = coin;
            Event.EventManager.Broadcast(tempEvent);
        }

        private void CharacterGradeUpAction(Event.CharacterGradeUpEvent @event)
        {
            if (@event.InstanceID != GetInstanceID())
                return;
            SetGrade(@event.NextGrade);
        }

        private void MonsterHexUpdateAction(Event.MonsterHexUpdateEvent @event)
        {
            var attackRange = _characterTable.CharacterRange;
            if (_target != null && MonsterHexDistance(_target) <= attackRange)
            {
                DoLookPosition(_target.transform.position);
                return;
            }

            if (!Runtime.StageInformation.SpawnedEnemies.TryGetValue(@event.InstanceID, out var monster))
                return;

            if (monster.CurrentHexQ == _currentHexQ && monster.CurrentHexQ == _currentHexR)   // 자기 자신
                return;

            if (MonsterHexDistance(monster) <= attackRange)
            {
                DoLookPosition(monster.transform.position);
                _target = monster;
            }
        }

        private void MonsterGoalInAction(Event.MonsterGoalInEvent @event)
        {
            if (_target == null)
                return;
            if (_target.gameObject.GetInstanceID() == @event.InstanceID)
                _target = null;
        }

        private void MonsterDieAction(Event.MonsterDieEvent @event)
        {
            if (_target == null)
                return;
            if (_target.gameObject.GetInstanceID() == @event.InstanceID)
                _target = null;
        }

        private void RecycleOnAction(Event.RecycleEnterEvent @event)
        {
            _isRecycleOn = @event.IsOn;
        }

        private float GetCurrentAttackDelay()   // 어택 스피드 증가 검사
        {
            var tempAttackDelay = Runtime.BuffSetInfo.GetBuffTypeValue(_characterTable.CharacterAttackSpeed,
                _characterTable.CharacterProperty, _characterTable.CharacterType, ProjectEnum.BuffType.AttackSpeed);

            if (tempAttackDelay != _characterTable.CharacterAttackSpeed)
            {
                Utilities.InternalDebug.Log($"공격 속도 버프 적용: {tempAttackDelay}초");
                return tempAttackDelay;
            }
            return _characterTable.CharacterAttackSpeed;
        }

        private int MonsterHexDistance(MonsterBehavior monster)
        {
            return StaticMethod.HexDistance(_currentHexQ, _currentHexR, monster.CurrentHexQ, monster.CurrentHexR);
        }

        private void DoLookPosition(Vector3 position, float duration = 0.2f)
        {
            transform.DOKill();
            transform.DOLookAt(position, duration);
        }

        #endregion

        [Button(ButtonSizes.Large)]
        private void SetComponent()
        {
            _animator = GetComponent<Animator>();
            _point = transform.Find("Point");
            if (_point == null)
                Utilities.InternalDebug.Log("이펙트 Point를 찾을 수 없습니다.");
            _dragHandler = transform.Find("DragHandler").GetComponent<DragHandler>();
        }
    }
}