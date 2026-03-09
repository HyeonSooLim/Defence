using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Battle
{
    // Runtime.StageInformation.Player01CellOnCharacters 관리 중
    public class CharacterCombineController : MonoBehaviour
    {
        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.AddListener<Event.CharacterCombineEvent>(CharacterCombineAction);
            Event.EventManager.AddListener<Event.RecycleUseEvent>(RecycleUseAction);
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.RemoveListener<Event.CharacterCombineEvent>(CharacterCombineAction);
            Event.EventManager.RemoveListener<Event.RecycleUseEvent>(RecycleUseAction);
        }

        private void CharacterOnCellAction(Event.CharacterOnCellEvent @event)
        {
            var previousKey = @event.PreviousHex;
            var currentKey = @event.CurrentHex;

            // 이전 위치 캐릭터 정보 삭제
            if (Runtime.CharacterCombineInfo.Player01CellOnCharacters.ContainsKey(previousKey))
            {
                Runtime.CharacterCombineInfo.Player01CellOnCharacters.Remove(previousKey);
            }
            else if (Runtime.CharacterCombineInfo.Player02CellOnCharacters.ContainsKey(previousKey))
            {
                Runtime.CharacterCombineInfo.Player02CellOnCharacters.Remove(previousKey);
            }

            // 셀 위에 있는 캐릭터 검사 및 추가
            if (Runtime.StageInformation.Player01Cells.ContainsKey(currentKey))
            {
                if (Runtime.CharacterCombineInfo.Player01CellOnCharacters.ContainsKey(currentKey))
                {
                    Runtime.CharacterCombineInfo.Player01CellOnCharacters[currentKey] = @event.InstanceID;
                    Utilities.InternalDebug.LogError("이미 좌표에 캐릭터가 있습니다.");
                }
                else
                {
                    Runtime.CharacterCombineInfo.Player01CellOnCharacters.Add(currentKey, @event.InstanceID);
                }
            }
            else if (Runtime.StageInformation.Player02Cells.ContainsKey(currentKey))
            {
                if (Runtime.CharacterCombineInfo.Player02CellOnCharacters.ContainsKey(currentKey))
                {
                    Runtime.CharacterCombineInfo.Player02CellOnCharacters[currentKey] = @event.InstanceID;
                    Utilities.InternalDebug.LogError("이미 좌표에 캐릭터가 있습니다.");
                }
                else
                {
                    Runtime.CharacterCombineInfo.Player02CellOnCharacters.Add(currentKey, @event.InstanceID);
                }
            }
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            Runtime.CharacterCombineInfo.Player01CellOnCharacters.Clear();
            Runtime.CharacterCombineInfo.Player02CellOnCharacters.Clear();
        }

        private void CharacterCombineAction(Event.CharacterCombineEvent @event)
        {
            @event.IsSuccess = false;
            var targetInstanceID = @event.TargetInstanceID;
            var sourceInstanceID = @event.SourceInstanceID;
            if (!Runtime.StageInformation.SpawnedCharacters.TryGetValue(targetInstanceID, out var targetBehavior))
                return;
            if (!Runtime.StageInformation.SpawnedCharacters.TryGetValue(sourceInstanceID, out var sourceBehavior))
                return;

            var targetHex = (targetBehavior.CurrentHexQ, targetBehavior.CurrentHexR);
            var sourceHex = (sourceBehavior.CurrentHexQ, sourceBehavior.CurrentHexR);
            var checkCellOnData = sourceBehavior.PlayerType == ProjectEnum.PlayerType.Player01 ?
                Runtime.CharacterCombineInfo.Player01CellOnCharacters : Runtime.CharacterCombineInfo.Player02CellOnCharacters;
            if (!checkCellOnData.ContainsKey(targetHex))
                return;
            if (!checkCellOnData.ContainsKey(sourceHex))
                return;

            var nextGrade = sourceBehavior.Grade + 1;
            bool canCombine = targetInstanceID != sourceInstanceID &&
                targetBehavior.CharacterTable.Seed == sourceBehavior.CharacterTable.Seed && // 같은 시드
                targetBehavior.Grade == sourceBehavior.Grade && // 같은 등급 (1 + 1) (2 + 2) ...
                targetBehavior.PlayerType == sourceBehavior.PlayerType &&   // 같은 플레이어 소속
                Global.DataManager.GradeStat.TryGet(nextGrade, out var _); // 다음 등급이 존재하는지 

            if (canCombine)
            {
                @event.IsSuccess = true;
                checkCellOnData.Remove(targetHex);
                ExecuteCharacterDisappearEvent(targetInstanceID);
                ExecuteCharacterGradeUpEvent(sourceInstanceID, sourceBehavior.Grade, nextGrade);
            }
        }

        private void RecycleUseAction(Event.RecycleUseEvent @event)
        {
            if (Runtime.StageInformation.SpawnedCharacters.TryGetValue(@event.InstanceID, out var characterBehavior))
            {
                var key = (characterBehavior.CurrentHexQ, characterBehavior.CurrentHexR);
                if (Runtime.CharacterCombineInfo.Player01CellOnCharacters.ContainsKey(key))
                {
                    Runtime.CharacterCombineInfo.Player01CellOnCharacters.Remove(key);
                }
                else if(Runtime.CharacterCombineInfo.Player02CellOnCharacters.ContainsKey(key))
                {
                    Runtime.CharacterCombineInfo.Player02CellOnCharacters.Remove(key);
                }
            }

            // TO DO : 추후 리사이클 이벤트에 재화 관련 로직 추가
            ExecuteCharacterDisappearEvent(@event.InstanceID);
        }

        private void ExecuteCharacterGradeUpEvent(int instanceID, int currentGrade, int nextGrade)
        {
            var tempEvent = Event.Events.CharacterGradeUpEvent;
            tempEvent.InstanceID = instanceID;
            tempEvent.CurrentGrade = currentGrade;
            tempEvent.NextGrade = nextGrade;
            Event.EventManager.Broadcast(tempEvent);
        }


        private void ExecuteCharacterDisappearEvent(int instanceID)
        {
            if (Runtime.StageInformation.SpawnedCharacters.TryGetValue(instanceID, out var characterBehavior))
            {
                var tempEvent = Event.Events.CharacterDisappearEvent;
                tempEvent.InstanceID = instanceID;
                tempEvent.CharacterSeed = characterBehavior.CharacterTable.Seed;
                tempEvent.Property = characterBehavior.GetUnitProperty();
                tempEvent.CharacterType = characterBehavior.CharacterTable.CharacterType;
                tempEvent.PlayerType = characterBehavior.PlayerType;
                Event.EventManager.Broadcast(tempEvent);
            }
        }
    }
}