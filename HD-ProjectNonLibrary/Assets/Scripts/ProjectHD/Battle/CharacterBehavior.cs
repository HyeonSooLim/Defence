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
        // 클래스 상단에 한 번만 선언해서 재사용 (가비지 컬렉션 방지)
        private static MaterialPropertyBlock _sharedMpb;
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
        
        private const string STAY_ANIMATION = "Stay";
        private const string SPAWN_SFX_KEY = "Assets/GameResources/Audio/UI/Summoning_Audio.wav";

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _point;
        [SerializeField] private DragHandler _dragHandler;
        [SerializeField] private List<SkinnedMeshRenderer> _renderers;
        
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

        private void Awake()
        {
            if (_sharedMpb == null)
            {
                _sharedMpb = new MaterialPropertyBlock();
            }
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
        
        // 스프라이트 아틀라스를 이용해 매핑된 텍스처를 이용하는 방식. 텍스처를 바꿀 때, ST값을 바꿀 때
        // 각각 1회 씩 드로우콜이 늘어나서 쓰지 않는 함수 (2026.06.25)
        private void SetRendering()
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var render = _renderers[i];
                if (render && AtlasLoader.TryGetSprite(gameObject.name.Replace("(Clone)", ""), out var sprite))
                {
                    Rect textureRect = sprite.textureRect;
                    // sprite.texture 대신 sprite.rect를 써서 아틀라스 내부 원본 크기 기준으로 계산해야 정확할 수 있습니다.
                    Vector2 texSize = new (sprite.texture.width, sprite.texture.height);
                    Vector2 scale = new (textureRect.width / texSize.x, textureRect.height / texSize.y);
                    Vector2 offset = new (textureRect.x / texSize.x, textureRect.y / texSize.y);
                    
                    // 기존 렌더러의 MPB를 가져와서 오프셋만 덮어쓰기
                    render.GetPropertyBlock(_sharedMpb);
                    _sharedMpb.SetTexture(MainTex, sprite.texture);
                    _sharedMpb.SetVector(MainTexSt, new Vector4(scale.x, scale.y, offset.x, offset.y));
                    render.SetPropertyBlock(_sharedMpb);
                }
            }
        }

        private void SetGrade(int grade)
        {
            _grade = grade;
            Utilities.InternalDebug.Log($"캐릭터 등급이 {_grade}로 변경되었습니다.");
        }

        public void CharacterHandle()
        {
            if (!_animator)
                return;

            if (!IsStayAnimation())
                return;

            if (_attackDelay > 0)
            {
                _attackDelay -= Time.deltaTime;
                return;
            }

            if (_isDragging)
                return;

            if (!_target)
                return;

            if (!CanAttack(_target))
            {
                _target = null;
                return;
            }

            _attackDelay = GetCurrentAttackDelay();
            PlayAttackAnimation();
        }

        private void PlayAttackAnimation()
        {
            var randomAnimationIndex = UnityEngine.Random.Range(1, 3);
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

            foreach (MonsterBehavior monster in Runtime.StageInformation.SpawnedEnemies.Values)
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
            transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);
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

                Vector2Int hex = GetCurrentHex();  // 현재 드래그 중인 캐릭터 좌표
                (int x, int y) key = (hex.x, hex.y);

                if (!CheckPlayerCell(key))  // 플레이어의 영역(1,2 혹은 그외) 체크
                    return;

                CheckAndExecuteCharacterCombine(key);    // 캐릭터 합성
                UpdateTarget();
            }
        }

        private bool CheckPlayerCell((int, int) key)
        {
            var checkPlayerCells = GetPlayerCells();

            if (!checkPlayerCells.ContainsKey(key))
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
                return false;
            }

            return true;
        }

        private void CheckAndExecuteCharacterCombine((int, int) key)
        {
            var playerCellonCharacters = _playerType == ProjectEnum.PlayerType.Player01 ?
                Runtime.CharacterCombineInfo.Player01CellOnCharacters : Runtime.CharacterCombineInfo.Player02CellOnCharacters;

            if (playerCellonCharacters.TryGetValue(key, out int targetInstanceID))    // 해당 칸에 캐릭터가 있다면
            {
                if (Runtime.StageInformation.SpawnedCharacters.TryGetValue(targetInstanceID, out var characterObject)
                    && characterObject.TryGetComponent<CharacterBehavior>(out var characterBehavior))
                {
                    RequestCharacterCombineEvent(targetInstanceID);
                }
            }
            else
            {
                var playerCells = GetPlayerCells();
                if (playerCells.TryGetValue(key, out var cellBehavior))
                {
                    MoveToCellPosition(cellBehavior);
                    UpdateHexAndExecuteEvent();
                }
            }
        }

        private void MoveToCellPosition(CellBehavior cellBehavior)
        {
            transform.SetLocalPositionAndRotation(cellBehavior.transform.localPosition, Quaternion.identity);
        }

        private Dictionary<System.ValueTuple<int, int>, Battle.CellBehavior> GetPlayerCells()
        {
            return _playerType == ProjectEnum.PlayerType.Player01 ?
                    Runtime.StageInformation.Player01Cells : Runtime.StageInformation.Player02Cells;
        }

        #endregion

        #region Events

        private void ExecuteSpawnEffectEvent(string key, Transform point, float duration = 1.5f)
        {
            var tempEvent = Event.Events.SpawnEffectEvent;
            tempEvent.AssetKey = key;
            tempEvent.Transform = point;
            tempEvent.Duration = duration;
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
            var instanceID = GetInstanceID();
            var tempEvent = Event.Events.CharacterOnCellEvent;
            tempEvent.InstanceID = instanceID;
            tempEvent.PreviousHex = (_previousHexQ, _previousHexR);
            tempEvent.CurrentHex = (_currentHexQ, _currentHexR);
            tempEvent.IsFirst = isFirst;
            Event.EventManager.Broadcast(tempEvent);

            if (isFirst)
            {
                if (Runtime.StageInformation.SpawnedCharacters.TryGetValue(instanceID, out var characterBehavior)
                    && Global.DataManager.UnitPropertyDefine.TryGet(characterBehavior.CharacterTable.CharacterProperty, out var unitPropertyDefine))
                {
                    if (unitPropertyDefine.SpawnEffectAssetKey.IsNullOrEmpty())
                        return;

                    ExecuteSpawnEffectEvent(unitPropertyDefine.SpawnEffectAssetKey, characterBehavior.transform, 2);
                    ExecutePlaySFX(SPAWN_SFX_KEY);
                }
            }
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

                Dictionary<System.ValueTuple<int, int>, Battle.CellBehavior> checkPlayerCells = GetPlayerCells();
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

        private void ExecutePlaySFX(string assetKey)
        {
            var tempEvent = Event.Events.PlaySFXEvent;
            tempEvent.AssetKey = assetKey;
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
            if (!_target)
                return;
            if (_target.gameObject.GetInstanceID() == @event.InstanceID)
                _target = null;
        }

        private void MonsterDieAction(Event.MonsterDieEvent @event)
        {
            if (!_target)
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

            if (!Mathf.Approximately(tempAttackDelay, _characterTable.CharacterAttackSpeed))
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

        #if UNITY_EDITOR
        [Button(ButtonSizes.Large)]
        private void SetComponent()
        {
            _animator = GetComponent<Animator>();
            _point = transform.Find("Point");
            if (_point == null)
                Utilities.InternalDebug.Log("이펙트 Point를 찾을 수 없습니다.");
            _dragHandler = transform.Find("DragHandler").GetComponent<DragHandler>();
            
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            _renderers.Clear();
            _renderers.AddRange(renderers);
        }
        
        [Button(ButtonSizes.Large)]
        private void ChangeMaterial(Material material) // ref는 굳이 필요하지 않습니다.
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var render = _renderers[i];
                if (render == null) return;

                // 1. 기존 머티리얼 개수만큼 새로운 배열을 생성합니다.
                Material[] newMaterials = new Material[render.sharedMaterials.Length];
    
                // 2. 새 배열을 원하는 원본 머티리얼로 채웁니다.
                for (int j = 0; j < newMaterials.Length; j++)
                {
                    newMaterials[j] = material;
                }

                // 3. 변경 사항을 에디터 되돌리기(Undo) 시스템에 등록합니다. (인스펙터 저장용)
                UnityEditor.Undo.RecordObject(render, "Change Shared Materials");

                // 4. 배열 전체를 통째로 할당해야 인스턴스가 생성되지 않습니다.
                render.sharedMaterials = newMaterials;

                // 5. 에디터에 변경 사항이 있음을 알려 씬이 저장되도록 합니다.
                UnityEditor.EditorUtility.SetDirty(render);
            }
        }
        #endif
    }
}