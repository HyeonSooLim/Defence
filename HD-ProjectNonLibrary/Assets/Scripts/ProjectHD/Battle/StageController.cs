using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace ProjectHD.Battle
{
    [System.Serializable]
    public class PathData
    {
        public Transform StartPoint;
        public List<Transform> WayPoints;
    }

    [System.Serializable]
    public class MonsterwayCell
    {
        public int Index;
        public Transform Transform;
        public Vector3 WorldPosition;
        public int Q;   // 가로 좌표
        public int R;   // 세로 좌표

        public MonsterwayCell(int index, Transform transform, int q, int r, Vector3 worldPosition)
        {
            Index = index;
            Transform = transform;
            Q = q;
            R = r;
            WorldPosition = worldPosition;
        }
    }

    // Runtime.StageInformation.SpawnedCharacters 관리 중
    public class StageController : MonoBehaviour
    {        
        private const string ENEMY_ASSET_KEY = "Assets/GameResources/Prefabs/Monster/GoburiMon.prefab";

        [SerializeField] private PathData[] _pathDatas;
        [SerializeField] private NavMeshSurface _navMeshSurface;
        [SerializeField] private Transform _player1Root;
        [SerializeField] private Transform _player2Root;
        [SerializeField] private CellBehavior[] _player1Cells;
        [SerializeField] private CellBehavior[] _player2Cells;

        [SerializeField] private Transform _monsterwayRoot;
        [SerializeField] private MonsterwayCell[] _monsterwayCells;
        [SerializeField] private Vector3 _hexOffSet = new(0, 0, -1);

        public event System.Action MonsterHandler;
        public event System.Action CharacterHandler;

        private readonly CancelToken _cancelToken = new();

        private int _currentWaveMonsterSpawnCount;
        bool _isWaveSpawnEnd;
        private int _currentPathDataIndex;

        public int CurrentPathDataIndex
        {
            get => _currentPathDataIndex;
            private set
            {
                if (value < 0 || value >= _pathDatas.Length)
                {
                    _currentPathDataIndex = 0;
                    return;
                }
                _currentPathDataIndex = value;
            }
        }

        private void Awake()
        {
            SetRuntimeHexCell();

            var navMeshData = _navMeshSurface.navMeshData;
            if (navMeshData == null)
            {
                _navMeshSurface.BuildNavMesh();
                Utilities.InternalDebug.Log("네브메시 데이터가 없어 새로 빌드했습니다.");
            }

            MonsterHandler = null;
            CharacterHandler = null;

            SetEvents();
            _cancelToken.SetToken();
            _currentWaveMonsterSpawnCount = 0;
            _isWaveSpawnEnd = false;
            _currentPathDataIndex = 0;
        }

        private void Update()
        {
            MonsterHandler?.Invoke();

            CharacterHandler?.Invoke();

            CheckAndExecuteWaveEndEvent();  // 웨이브 종료 체크
        }

        private void SetEvents()
        {
            Event.EventManager.AddListener<Event.StageWaveStartEvent>(ExecuteStageWaveStartAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.AddListener<Event.MonsterGoalInEvent>(MonsterGoalInAction);
            Event.EventManager.AddListener<Event.CharacterSpawnEvent>(CharacterSpawnAction);
            Event.EventManager.AddListener<Event.MonsterDieEvent>(MonsterDieAction);
            Event.EventManager.AddListener<Event.CharacterDisappearEvent>(CharacterDisappearAction);
            Event.EventManager.AddListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
        }

        private void UnSetEvets()
        {
            Event.EventManager.RemoveListener<Event.StageWaveStartEvent>(ExecuteStageWaveStartAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.RemoveListener<Event.MonsterGoalInEvent>(MonsterGoalInAction);
            Event.EventPool<Event.StageWaveCompleteEvent>.ReleaseEvent();
            Event.EventManager.RemoveListener<Event.CharacterSpawnEvent>(CharacterSpawnAction);
            Event.EventManager.RemoveListener<Event.MonsterDieEvent>(MonsterDieAction);
            Event.EventManager.RemoveListener<Event.CharacterDisappearEvent>(CharacterDisappearAction);
            Event.EventManager.RemoveListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
        }

        private void SetRuntimeHexCell()
        {
            if (_monsterwayCells == null || _monsterwayCells.Length == 0)
            {
                Utilities.InternalDebug.LogError("몬스터 웨이 셀이 설정되지 않았습니다.");
                return;
            }

            var cell = _monsterwayCells[0];
            var bounds = cell.Transform.GetComponent<MeshRenderer>().bounds;
            Runtime.HexSize.Width = bounds.size.x;
            Runtime.HexSize.Height = bounds.size.z;
            Runtime.HexSize.StageCellHexOffset = _hexOffSet;

            for (int i = 0; i < _player1Cells.Length; i++)
            {
                var playerCell = _player1Cells[i];
                var key = (playerCell.Q, playerCell.R);
                if (Runtime.StageInformation.Player01Cells.ContainsKey(key))
                {
                    Utilities.InternalDebug.LogError($"플레이어1 셀 중에 중복된 좌표의 셀이 있습니다. ({key})");
                    continue;
                }
                Runtime.StageInformation.Player01Cells.Add(key, playerCell);
            }

            for (int i = 0; i < _player2Cells.Length; i++)
            {
                var playerCell = _player2Cells[i];
                var key = (playerCell.Q, playerCell.R);
                if (Runtime.StageInformation.Player02Cells.ContainsKey(key))
                {
                    Utilities.InternalDebug.LogError($"플레이어2 셀 중에 중복된 좌표의 셀이 있습니다. ({key})");
                    continue;
                }
                Runtime.StageInformation.Player02Cells.Add(key, playerCell);
            }
        }

        private async UniTask SpawnEnemyAsync(int stageSeed, int waveIndex)
        {
            if (Global.DataManager.StageTable.TryGet(stageSeed, out var stageTable)
                && Global.DataManager.StageWaveGroup.TryGet(stageTable.WaveGroupSeed, out var stageWaveGroups))
            {
                _currentWaveMonsterSpawnCount = 0;

                var group = stageWaveGroups[waveIndex];
                var count = group.MonsterCount;
                Utilities.InternalDebug.Log($"[{name}] 스테이지 시드: ({stageSeed}), 웨이브 인덱스: ({waveIndex}), 웨이브 몬스터 카운트: ({count})");
                int delay = GetMonsterRecallDelayTime();

                for (int i = 0; i < count; i++)
                {
                    if (_cancelToken.TokenSource.IsCancellationRequested)
                    {
                        Utilities.InternalDebug.Log("맵 컨트롤러가 취소되었습니다.");
                        return;
                    }

                    var randomMonsterSeed = StaticMethod.GetRandomElement(group.MonsterSeeds);
                    string modelKey = GetMonsterAssetKey(randomMonsterSeed);

                    var enemy = MainManager.Instance.GameObjectPool.Get(modelKey);
                    if (enemy.transform.TryGetComponent<MonsterBehavior>(out var monsterBehavior))
                    {
                        var pathData = GetPathData();
                        if (pathData == null)
                            return;

                        monsterBehavior.Consturct(randomMonsterSeed, pathData).Forget();

                        var instanceID = monsterBehavior.GetInstanceID();
                        if (!Runtime.StageInformation.SpawnedEnemies.ContainsKey(instanceID))
                        {
                            Runtime.StageInformation.SpawnedEnemies.Add(instanceID, monsterBehavior);
                        }
                        MonsterHandler += monsterBehavior.HandleMonster;
                        _currentWaveMonsterSpawnCount++;
                    }

                    await UniTask.Delay(delay, cancellationToken: _cancelToken.Token);
                }

                _isWaveSpawnEnd = true;
            }
        }

        private void SpawnPlayerCharacter(int characterSeed, int level, int grade, string modelKey, ProjectEnum.PlayerType playerType, System.Action successCallback = null)
        {
            if (modelKey.IsNullOrEmpty())
            {
                Utilities.InternalDebug.LogError("모델 키가 비어있습니다.");
                return;
            }

            bool isPlayer1 = playerType == ProjectEnum.PlayerType.Player01;
            var root = isPlayer1 ? _player1Root : _player2Root;
            var cell = GetPlayerCell(isPlayer1);
            if (cell != null)
            {
                var character = MainManager.Instance.GameObjectPool.Get(modelKey);

                character.transform.SetParent(root);
                character.transform.SetLocalPositionAndRotation(cell.transform.localPosition, Quaternion.identity);
                //character.transform.localScale = Vector3.one;
                cell.SetCharacterHere(true);

                if (character.TryGetComponent<CharacterBehavior>(out var characterBehavior))
                {
                    var instanceID = characterBehavior.GetInstanceID();
                    if (!Runtime.StageInformation.SpawnedCharacters.ContainsKey(instanceID))
                    {
                        Runtime.StageInformation.SpawnedCharacters.Add(instanceID, characterBehavior);
                    }

                    characterBehavior.Construct(characterSeed, level, grade, playerType);
                    characterBehavior.PlayStayAnimation();
                    CharacterHandler += characterBehavior.CharacterHandle;
                    successCallback?.Invoke();
                }
            }
            else
            {
                // TO DO : 빈 셀을 찾지 못했을 때 처리
                Utilities.InternalDebug.LogError("빈 셀을 찾지 못했습니다.");
            }
        }

        private int GetMonsterRecallDelayTime()
        {
            int delay = 0;
            if (Global.DataManager.TestConstValue.TryGet(ProjectEnum.ConstDefine.MonsterRecallTime, out var constValue))
            {
                delay = constValue.Val;
            }
            return delay;
        }

        private string GetMonsterAssetKey(int monsterSeed)
        {
            string ramdomModelKey;
            if (Global.DataManager.MonsterTable.TryGet(monsterSeed, out var monsterTable)
                && !string.IsNullOrEmpty(monsterTable.ModelAssetKey))
            {
                ramdomModelKey = monsterTable.ModelAssetKey;
            }
            else
            {
                ramdomModelKey = ENEMY_ASSET_KEY;
            }
            return ramdomModelKey;
        }

        private CellBehavior GetPlayerCell(bool isPlayer1)
        {
            var cellArray = isPlayer1 ? _player1Cells : _player2Cells;

            for (int i = 0; i < cellArray.Length; i++)
            {
                var cell = cellArray[i];
                if (cell.IsCharacterHere)
                    continue;

                return cell;
            }

            return null;
        }

        private PathData GetPathData()
        {
            if (_pathDatas.Length == 0)
            {
                Utilities.InternalDebug.LogError("경로 데이터가 없습니다.");
                return null;
            }

            return _pathDatas[CurrentPathDataIndex++];
            //var pathData = StaticMethod.GetRandomElement(_pathDatas);
            //return pathData;
        }

        private void DestructMonster(int instanceID)
        {
            if (Runtime.StageInformation.SpawnedEnemies.ContainsKey(instanceID))
            {
                var enemy = Runtime.StageInformation.SpawnedEnemies[instanceID];
                MonsterHandler -= enemy.HandleMonster;
                enemy.Destruct();
                Runtime.StageInformation.SpawnedEnemies.Remove(instanceID);
                MainManager.Instance.GameObjectPool.Return(enemy.gameObject);
                _currentWaveMonsterSpawnCount--;    // TO DO : 몬스터가 죽었을 때도 감소해야 함
                Utilities.InternalDebug.Log($"남은 몬스터 수: {_currentWaveMonsterSpawnCount}");
            }
        }

        #region Events

        private void ExecuteStageWaveStartAction(Event.StageWaveStartEvent @event)
        {
            SpawnEnemyAsync(@event.StageSeed, @event.StageWaveIndex).Forget();
        }

        public void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            UnSetEvets();
            _cancelToken.UnSetToken();

            foreach (var enemy in Runtime.StageInformation.SpawnedEnemies.Values)
            {
                enemy.Destruct();
                MainManager.Instance.GameObjectPool.Return(enemy.gameObject);
            }
            Runtime.StageInformation.SpawnedEnemies.Clear();

            foreach (var characterBehavior in Runtime.StageInformation.SpawnedCharacters.Values)
            {
                CharacterHandler -= characterBehavior.CharacterHandle;
                characterBehavior.Destruct();
                MainManager.Instance.GameObjectPool.Return(characterBehavior.gameObject);
            }
            Runtime.StageInformation.SpawnedCharacters.Clear();

            Runtime.StageInformation.Player01Cells.Clear();
            Runtime.StageInformation.Player02Cells.Clear();

            MonsterHandler = null;
            CharacterHandler = null;
        }

        private void MonsterGoalInAction(Event.MonsterGoalInEvent @event)
        {
            var instanceID = @event.InstanceID;
            DestructMonster(instanceID);
        }

        private void MonsterDieAction(Event.MonsterDieEvent @event)
        {
            var instanceID = @event.InstanceID;
            DestructMonster(instanceID);
        }

        private void CharacterSpawnAction(Event.CharacterSpawnEvent @event)
        {
            if (Global.DataManager.CharacterTable.TryGet(@event.Seed, out var characterTable))
            {
                SpawnPlayerCharacter(@event.Seed, @event.Level, @event.Grade, characterTable.ModelAssetKey, Runtime.PlayerData.Account.PlayerType,
                () => Utilities.InternalDebug.Log($"속성:({characterTable.CharacterProperty}, 시드:({characterTable.Seed}) 소환!"));

                // 2P용 캐릭터도 소환(임시)
                var tempPlayerType = Runtime.PlayerData.Account.PlayerType == ProjectEnum.PlayerType.Player01 ?
                    ProjectEnum.PlayerType.Player02 : ProjectEnum.PlayerType.Player01;
                SpawnPlayerCharacter(@event.Seed, @event.Level, @event.Grade, characterTable.ModelAssetKey, tempPlayerType,
                () => Utilities.InternalDebug.Log($"속성:({characterTable.CharacterProperty}, 시드:({characterTable.Seed}) 소환!"));
            }
        }

        private void CharacterOnCellAction(Event.CharacterOnCellEvent @event)
        {
            var previousKey = @event.PreviousHex;
            var currentKey = @event.CurrentHex;

            if (Runtime.StageInformation.Player01Cells.TryGetValue(previousKey, out var previousCell01))
            {
                previousCell01.SetCharacterHere(false);
            }
            else if (Runtime.StageInformation.Player02Cells.TryGetValue(previousKey, out var previousCell02))
            {
                previousCell02.SetCharacterHere(false);
            }

            if (Runtime.StageInformation.Player01Cells.TryGetValue(currentKey, out var currentCell01))
            {
                currentCell01.SetCharacterHere(true);
            }
            else if (Runtime.StageInformation.Player02Cells.TryGetValue(currentKey, out var currentCell02))
            {
                currentCell02.SetCharacterHere(true);
            }
        }

        private void CharacterDisappearAction(Event.CharacterDisappearEvent @event)
        {
            var instanceID = @event.InstanceID;
            if (Runtime.StageInformation.SpawnedCharacters.ContainsKey(instanceID))
            {
                var characterBehavior = Runtime.StageInformation.SpawnedCharacters[instanceID];
                CharacterHandler -= characterBehavior.CharacterHandle;

                var key = (characterBehavior.CurrentHexQ, characterBehavior.CurrentHexR);
                if (characterBehavior.PlayerType == ProjectEnum.PlayerType.Player01)
                {
                    if (Runtime.StageInformation.Player01Cells.TryGetValue(key, out var cell))
                    {
                        cell.SetCharacterHere(false);
                    }
                }
                else
                {
                    if (Runtime.StageInformation.Player02Cells.TryGetValue(key, out var cell))
                    {
                        cell.SetCharacterHere(false);
                    }
                }

                characterBehavior.Destruct();
                MainManager.Instance.GameObjectPool.Return(characterBehavior.gameObject);
                Runtime.StageInformation.SpawnedCharacters.Remove(instanceID);
            }
        }

        private void ExecuteWaveEndEvent()
        {
            var waveEndEvent = Event.EventPool<Event.StageWaveCompleteEvent>.GetEvent();
            waveEndEvent.StageWaveIndex = ++Runtime.StageInformation.CurrentWaveIndex;
            Event.EventManager.Broadcast(waveEndEvent);
            Event.EventPool<Event.StageWaveCompleteEvent>.ReturnEvent(waveEndEvent);
        }

        private void CheckAndExecuteWaveEndEvent()
        {
            if (_isWaveSpawnEnd && _currentWaveMonsterSpawnCount <= 0)
            {
                _isWaveSpawnEnd = false;
                ExecuteWaveEndEvent();
            }
        }

        #endregion

#if UNITY_EDITOR

        [Button(ButtonSizes.Medium)]
        private void SetPlayerCells()
        {
            if (_player1Root == null || _player2Root == null)
            {
                Utilities.InternalDebug.LogError("플레이어 루트가 설정되지 않았습니다.");
                return;
            }

            var chlildren1 = _player1Root.GetComponentsInChildren<CellBehavior>();
            _player1Cells = new CellBehavior[chlildren1.Length];
            for (int i = 0; i < chlildren1.Length; i++)
            {
                var cell = chlildren1[i];
                _player1Cells[i] = cell;
            }
            var chlildren2 = _player2Root.GetComponentsInChildren<CellBehavior>();
            _player2Cells = new CellBehavior[chlildren2.Length];
            for (int i = 0; i < chlildren2.Length; i++)
            {
                var cell = chlildren2[i];
                _player2Cells[i] = cell;
            }

            // 정렬 (좌 -> 우, 앞 -> 뒤)
            _player1Cells.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
            _player1Cells.Sort((a, b) => a.transform.localPosition.z.CompareTo(b.transform.localPosition.z));

            _player2Cells.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
            _player2Cells.Sort((a, b) => a.transform.localPosition.z.CompareTo(b.transform.localPosition.z));

            using var raii = new Utilities.StaticObjectPool.RAII<Dictionary<System.ValueTuple<int, int>, int>>(out var checkDictionary);
            checkDictionary.Clear();

            for (int i = 0; i < _player1Cells.Length; i++)
            {
                var cell = _player1Cells[i];
                cell.SetIndex(i);

                var bounds = cell.GetComponent<MeshRenderer>().bounds;
                var hexWidth = bounds.size.x;
                var hexHeight = bounds.size.z;
                Vector3 worldPos = cell.transform.position;
                Vector2Int axialCoords = StaticMethod.WorldToHex(hexWidth, hexHeight, worldPos, _hexOffSet);
                var q = axialCoords.x;
                var r = axialCoords.y;
                cell.SetHex(q, r);

                Debug.Log($"셀 좌표: q = {q}, r = {r}");
                var key = (q, r);
                if (checkDictionary.TryGetValue(key, out var value))
                    Utilities.InternalDebug.LogError($"좌표({q},{r}, 인덱스:({i})와 저장된 인덱스:({value}))중복된 좌표가 있습니다");
                else
                    checkDictionary.Add(key, i);
            }
            for (int i = 0; i < _player2Cells.Length; i++)
            {
                var cell = _player2Cells[i];
                cell.SetIndex(i);

                var bounds = cell.GetComponent<MeshRenderer>().bounds;
                var hexWidth = bounds.size.x;
                var hexHeight = bounds.size.z;
                Vector3 worldPos = cell.transform.position;
                Vector2Int axialCoords = StaticMethod.WorldToHex(hexWidth, hexHeight, worldPos, _hexOffSet);
                var q = axialCoords.x;
                var r = axialCoords.y;
                cell.SetHex(q, r);

                Debug.Log($"셀 좌표: q = {q}, r = {r}");
                var key = (q, r);
                if (checkDictionary.TryGetValue(key, out var value))
                    Utilities.InternalDebug.LogError($"좌표({q},{r}, 인덱스:({i})와 저장된 인덱스:({value}))중복된 좌표가 있습니다");
                else
                    checkDictionary.Add(key, i);
            }

            _player2Cells.Sort((a, b) => a.Index.CompareTo(b.Index));
            _player2Cells.Sort((a, b) => a.Index.CompareTo(b.Index));

            Utilities.InternalDebug.Log($"플레이어1 셀 개수: {_player1Cells.Length}, 플레이어2 셀 개수: {_player2Cells.Length}");
            checkDictionary.Clear();
        }

        [Button(ButtonSizes.Medium)]
        private void SetMonsterwayCells()
        {
            _monsterwayCells = new MonsterwayCell[_monsterwayRoot.childCount];
            using var raii = new Utilities.StaticObjectPool.RAII<Dictionary<System.ValueTuple<int,int>, int>>(out var checkDictionary);
            checkDictionary.Clear();

            for (int i = 0; i < _monsterwayCells.Length; i++)
            {
                var cell = _monsterwayRoot.GetChild(i);
                var bounds = cell.GetComponent<MeshRenderer>().bounds;
                var hexWidth = bounds.size.x;
                var hexHeight = bounds.size.z;
                Vector3 worldPos = cell.position;
                Vector2Int axialCoords = StaticMethod.WorldToHex(hexWidth, hexHeight, worldPos, _hexOffSet);
                var q = axialCoords.x;
                var r = axialCoords.y;
                MonsterwayCell tempCell = new(i, cell, q, r, worldPos);
                _monsterwayCells[i] = tempCell;

                Debug.Log($"셀 좌표: q = {q}, r = {r}");
                var key = (q, r);
                if (checkDictionary.TryGetValue(key, out var value))
                    Utilities.InternalDebug.LogError($"좌표({q},{r}, 인덱스:({i})와 저장된 인덱스:({value}))중복된 좌표가 있습니다");
                else
                    checkDictionary.Add(key, i);
            }

            checkDictionary.Clear();
        }

        [Button(ButtonSizes.Medium)]
        private void CheckPlayerAndMonsterHex()
        {
            if (_player1Cells == null || _player2Cells == null || _monsterwayCells == null)
            {
                Utilities.InternalDebug.LogError("플레이어 셀 또는 몬스터 웨이 셀이 설정되지 않았습니다.");
                return;
            }
            using var raii = new Utilities.StaticObjectPool.RAII<Dictionary<System.ValueTuple<int, int>, int>>(out var checkDictionary);
            checkDictionary.Clear();
            for (int i = 0; i < _player1Cells.Length; i++)
            {
                var cell = _player1Cells[i];
                var key = (cell.Q, cell.R);
                if (checkDictionary.TryGetValue(key, out var value))
                    Utilities.InternalDebug.LogError($"플레이어1 좌표({cell.Q},{cell.R}, 인덱스:({i})와 저장된 인덱스:({value}))중복된 좌표가 있습니다");
                else
                    checkDictionary.Add(key, i);
            }
            for (int i = 0; i < _player2Cells.Length; i++)
            {
                var cell = _player2Cells[i];
                var key = (cell.Q, cell.R);
                if (checkDictionary.TryGetValue(key, out var value))
                    Utilities.InternalDebug.LogError($"플레이어2 좌표({cell.Q},{cell.R}, 인덱스:({i})와 저장된 인덱스:({value}))중복된 좌표가 있습니다");
                else
                    checkDictionary.Add(key, i);
            }
            for (int i = 0; i < _monsterwayCells.Length; i++)
            {
                var cell = _monsterwayCells[i];
                var key = (cell.Q, cell.R);
                if (checkDictionary.TryGetValue(key, out var value))
                    Utilities.InternalDebug.LogError($"몬스터웨이 좌표({cell.Q},{cell.R}, 인덱스:({i})와 저장된 인덱스:({value}))중복된 좌표가 있습니다");
                else
                    checkDictionary.Add(key, i);
            }
            Utilities.InternalDebug.Log("플레이어1, 플레이어2, 몬스터웨이 좌표 체크 완료");
            checkDictionary.Clear();
        }

#endif

    }
}