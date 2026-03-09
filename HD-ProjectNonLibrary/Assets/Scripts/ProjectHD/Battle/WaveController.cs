using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Battle
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] private int _stageSeed = 101;

        private int _totalWaveCount = 0;

        private readonly CancelToken _cancelToken = new();

        private void Awake()
        {
            _totalWaveCount = 0;
            _cancelToken.SetToken();
            SetEvents();
        }

        private void SetEvents()
        {
            Event.EventManager.AddListener<Event.StageSettingEvent>(StateSettingAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.AddListener<Event.StageWaveCompleteEvent>(WaveCompleteAction);
        }

        private void UnSetEvets()
        {
            Event.EventManager.RemoveListener<Event.StageSettingEvent>(StateSettingAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.RemoveListener<Event.StageWaveCompleteEvent>(WaveCompleteAction);
            Event.EventPool<Event.StageWaveStartEvent>.ReleaseEvent();
            Event.EventPool<Event.StageWaveStartRemainTimeEvent>.ReleaseEvent();
        }

        private void StateSettingAction(Event.StageSettingEvent @event)
        {
            _stageSeed = @event.StageSeed;
            _totalWaveCount = 0;
            WaveStart();
        }

        private void WaveStart()    // 게임 스타트 이벤트?
        {
            if (Global.DataManager.StageTable.TryGet(_stageSeed, out var stageTable)
                && Global.DataManager.StageWaveGroup.TryGet(stageTable.WaveGroupSeed, out var stageWaveGroups))
            {
                var startIndex = 0;
                _totalWaveCount = stageWaveGroups.Count;
                WaveAsync(startIndex).Forget();
                Runtime.StageInformation.CurrentWaveIndex = startIndex;
            }
        }

        private async UniTask WaveAsync(int waveIndex)
        {
            var interval = 0;
            if (Global.DataManager.TestConstValue.TryGet(ProjectEnum.ConstDefine.MonsterWaveWaitTime, out var testConstValue))
            {
                interval = testConstValue.Val;
            }

            if (waveIndex < _totalWaveCount)
            {
                while (interval > 0)
                {
                    if (_cancelToken.TokenSource.IsCancellationRequested)
                        return;
                    StageWaveStartRemainTimeEvent(interval);
                    await UniTask.WaitForSeconds(1, cancellationToken: _cancelToken.Token);
                    interval--;
                }
                ExecuteStageWaveStartEvent(waveIndex);
            }
            else
            {
                // end 이벤트
            }
        }

        #region Events

        private void ExecuteStageWaveStartEvent(int waveIndex)
        {
            var waveEvent = Event.EventPool<Event.StageWaveStartEvent>.GetEvent();
            waveEvent.StageSeed = _stageSeed;
            waveEvent.StageWaveIndex = waveIndex;
            Event.EventManager.Broadcast(waveEvent);
            Event.EventPool<Event.StageWaveStartEvent>.ReturnEvent(waveEvent);
            Utilities.InternalDebug.Log($"Wave Start Event: {waveIndex}");
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            _cancelToken.UnSetToken();
            UnSetEvets();
        }

        private void WaveCompleteAction(Event.StageWaveCompleteEvent @event)
        {
            WaveAsync(@event.StageWaveIndex).Forget();
        }

        private void StageWaveStartRemainTimeEvent(int remainTime)
        {
            var waveEvent = Event.EventPool<Event.StageWaveStartRemainTimeEvent>.GetEvent();
            waveEvent.RemainTime = remainTime;
            Event.EventManager.Broadcast(waveEvent);
            Event.EventPool<Event.StageWaveStartRemainTimeEvent>.ReturnEvent(waveEvent);
        }

        #endregion
    }
}