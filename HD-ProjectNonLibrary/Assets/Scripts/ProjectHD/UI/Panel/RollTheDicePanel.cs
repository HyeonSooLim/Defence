using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD.UI
{
    public class RollTheDicePanel : MonoBehaviour
    {
        private const string DICE_ASSET_KEY = "Assets/GameResources/Prefabs/Dice/Dice.prefab";
        private const string ROLL_SFX_KEY = "Assets/GameResources/Audio/UI/Dice_Audio.wav";
        private const string ROLL_BUTTON_SFX_KEY = "Assets/GameResources/Audio/UI/Button_Audio.mp3";

        [SerializeField] private Button _rollButton;
        [SerializeField] private Transform[] _diceSpawnPoints;
        [SerializeField] private TMP_Text _remainTimeText;
        [SerializeField] private TMP_Text _rollTheDiceNeedCoinText;

        private int _rollCount = 0;
        private List<GameObject> _spawnedDice;
        private Dictionary<ProjectEnum.UnitProperty, int> _diceResults; 
        private readonly CancelToken _cancelToken = new();

        private void Awake()
        {
            _spawnedDice = Utilities.StaticObjectPool.Pop<List<GameObject>>();
            _spawnedDice.Clear();

            _diceResults = Utilities.StaticObjectPool.Pop<Dictionary<ProjectEnum.UnitProperty, int>>();
            _diceResults.Clear();

            _cancelToken.SetToken();
            SetNeedRollTheDiceCoin();
        }

        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.DiceResultEvent>(DiceResultAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(CleanUpAction);
            Event.EventManager.AddListener<Event.SceneLoadingCompleteEvent>(SceneLoadingAfterAction);
            Event.EventManager.AddListener<Event.StageWaveStartRemainTimeEvent>(WaveStartRemainTimeAction);

            _rollButton.onClick.AddListener(() =>
            {
                SoundManager.Instance.PlaySFX(ROLL_BUTTON_SFX_KEY);

                if (!CheckCell())
                {
                    Utilities.InternalDebug.Log("더 이상 캐릭터를 소환할 수 없습니다.");
                    return;
                }

                CheckCoinAndRolltheDiceEvent();
            });

            _remainTimeText.gameObject.SetActive(false);
        }


        private void SpawnDice()
        {
            foreach (var point in _diceSpawnPoints)
            {
                var dice = MainManager.Instance.GameObjectPool.Get(DICE_ASSET_KEY);
                if (dice.TryGetComponent<Battle.DiceBehavior>(out var diceBehavior))
                {
                    diceBehavior.Construct();
                    dice.transform.SetParent(point, false);
                    dice.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    dice.transform.localScale = Vector3.one * 1000;
                    _spawnedDice.Add(dice);
                }
            }
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.DiceResultEvent>(DiceResultAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(CleanUpAction);
            Event.EventManager.RemoveListener<Event.SceneLoadingCompleteEvent>(SceneLoadingAfterAction);
            Event.EventManager.RemoveListener<Event.StageWaveStartRemainTimeEvent>(WaveStartRemainTimeAction);
            _rollButton.onClick.RemoveAllListeners();
            Event.EventPool<Event.CharacterSpawnEvent>.ReleaseEvent();
            Utilities.InternalDebug.Log($"[CleanUp]{name}: OnDisable");
        }

        #region Events

        private void SceneLoadingAfterAction(Event.SceneLoadingCompleteEvent @event)
        {
            SpawnDice();
        }

        private void DiceResultAction(Event.DiceResultEvent @event)
        {
            _rollCount++;
            if(_diceResults.TryGetValue(@event.Property, out var count))
            {
                _diceResults[@event.Property] = count + 1;
            }
            else
            {
                _diceResults[@event.Property] = 1;
            }
            Debug.Log($"Dice Result: {@event.Property}, Roll Count: {_rollCount}");

            if (_rollCount >= _diceSpawnPoints.Length)
            {
                _rollCount = 0;
                ExecuteCharacterSpawnEvent();
            }
        }

        private void CleanUpAction(Event.ManagerUnloadEvent @event)
        {
            _cancelToken.UnSetToken();
            _remainTimeText.DOKill();

            foreach (var dice in _spawnedDice)
            {
                if (dice.TryGetComponent<Battle.DiceBehavior>(out var diceBehavior))
                {
                    diceBehavior.Destruct();
                }
                MainManager.Instance.GameObjectPool.Return(dice);
            }
            _spawnedDice.Clear();
            Utilities.StaticObjectPool.Push(_spawnedDice);
            _spawnedDice = null;

            _diceResults.Clear();
            Utilities.StaticObjectPool.Push(_diceResults);
            _diceResults = null;

            Utilities.InternalDebug.Log($"[CleanUp]{this.name}: CleanUp");
        }

        private void WaveStartRemainTimeAction(Event.StageWaveStartRemainTimeEvent @event)
        {
            _remainTimeText.DOKill();
            _remainTimeText.gameObject.SetActive(true);
            _remainTimeText.text = $"{@event.RemainTime}";
            _remainTimeText.alpha = 0;

            DOTween.Sequence()
                .Append(_remainTimeText.DOFade(1, 0.3f))
                .Join(_remainTimeText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack))
                .Append(_remainTimeText.transform.DOScale(1.0f, 0.2f).SetEase(Ease.InBack))
                .Join(_remainTimeText.DOFade(0, 0.2f))
                .AppendCallback(() =>
                {
                    _remainTimeText.gameObject.SetActive(false);
                }).ToUniTask(cancellationToken: _cancelToken.Token);
        }

        #endregion

        private void ExecuteCharacterSpawnEvent()
        {
            var spawnEvent = Event.EventPool<Event.CharacterSpawnEvent>.GetEvent();
            var randomProperty = StaticMethod.GetRandomElement(_diceResults.Keys);
            var randomSeed = 0;
            if (Global.DataManager.CharacterPropertyGroup.TryGet(randomProperty, out var propertyGroup))
            {
                var randomElemet = StaticMethod.GetRandomElement(propertyGroup);
                randomSeed = randomElemet.Seed;
            }
            spawnEvent.Seed = randomSeed;
            spawnEvent.Level = 1;
            spawnEvent.Grade = 1;
            if (!Global.DataManager.CharacterTable.TryGet(randomSeed, out var _))
            {
                Debug.LogError($"Character Seed Not Found: {randomSeed}");
                Event.EventPool<Event.CharacterSpawnEvent>.ReturnEvent(spawnEvent);
                return;
            }

            Event.EventManager.Broadcast(spawnEvent);
            Event.EventPool<Event.CharacterSpawnEvent>.ReturnEvent(spawnEvent);
            _diceResults.Clear();   // 한 번 소환 후 클리어
        }

        private void CheckCoinAndRolltheDiceEvent()
        {
            var needCoin = 0;
            if (Global.DataManager.TestConstValue.TryGet(ProjectEnum.ConstDefine.RollingDicePrice, out var constValue))
            {
                needCoin = constValue.Val;
            }

            var tempEvent = Event.Events.ChangeCoinEvent;
            tempEvent.Amount = -needCoin;
            Event.EventManager.Broadcast(tempEvent, (result) =>
            {
                if (result.IsSuccess)
                {
                    Event.EventManager.Broadcast(Event.Events.RollTheDiceEvent);
                    SoundManager.Instance.PlaySFX(ROLL_SFX_KEY);
                }
                else
                {
                    Utilities.InternalDebug.Log("코인이 부족합니다.");
                }
            });
        }

        private void SetNeedRollTheDiceCoin()
        {
            if (Global.DataManager.TestConstValue.TryGet(ProjectEnum.ConstDefine.RollingDicePrice, out var constValue))
            {
                _rollTheDiceNeedCoinText.SetText($"X {constValue.Val}");
            }
            else
            {
                _rollTheDiceNeedCoinText.SetText("X 0");
            }
        }

        private bool CheckCell()
        {
            bool isPlayer01 = Runtime.PlayerData.Account.PlayerType == ProjectEnum.PlayerType.Player01;
            var checkCells = isPlayer01 ? Runtime.StageInformation.Player01Cells : Runtime.StageInformation.Player02Cells;

            foreach (var cell in checkCells.Values)
            {
                if (!cell.IsCharacterHere)
                {
                    return true;
                }
            }

            return false;
        }
    }
}