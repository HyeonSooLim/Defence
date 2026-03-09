using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectHD.Battle
{
    public class BuffSetController : MonoBehaviour
    {
        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
            Event.EventManager.AddListener<Event.CharacterDisappearEvent>(CharacterDisappearAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
            Event.EventManager.RemoveListener<Event.CharacterDisappearEvent>(CharacterDisappearAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
        }

        private void CharacterOnCellAction(Event.CharacterOnCellEvent @event)
        {
            if (!Runtime.StageInformation.SpawnedCharacters.TryGetValue(@event.InstanceID, out var characterBehavior))
                return;
            if (characterBehavior.PlayerType != Runtime.PlayerData.Account.PlayerType)
                return;

            var characterSeed = characterBehavior.CharacterTable.Seed;
            if (Runtime.BuffSetInfo.RegisteredCharacterSeed.ContainsKey(characterSeed)) // 등록된 캐릭터 종류는 제외
            {
                Runtime.BuffSetInfo.RegisteredCharacterSeed[characterSeed] += 1;
                return;
            }
            Runtime.BuffSetInfo.RegisteredCharacterSeed.Add(characterSeed, 1);

            var property = characterBehavior.GetUnitProperty();
            var type = characterBehavior.CharacterTable.CharacterType;

            if (Runtime.BuffSetInfo.BuffProperties.ContainsKey(property))
                Runtime.BuffSetInfo.BuffProperties[property] += 1;
            else
                Runtime.BuffSetInfo.BuffProperties.Add(property, 1);

            if (Runtime.BuffSetInfo.BuffTypes.ContainsKey(type))
                Runtime.BuffSetInfo.BuffTypes[type] += 1;
            else
                Runtime.BuffSetInfo.BuffTypes.Add(type, 1);

            ExecuteUpdateBuffSetEvent();
        }

        private void CharacterDisappearAction(Event.CharacterDisappearEvent @event)
        {
            if (@event.PlayerType != Runtime.PlayerData.Account.PlayerType)
                return;

            var checkSeed = @event.CharacterSeed;
            if (!Runtime.BuffSetInfo.RegisteredCharacterSeed.TryGetValue(checkSeed, out var registeredCharacterSeedCount))
                return;

            var property = @event.Property;
            var type = @event.CharacterType;

            if (registeredCharacterSeedCount - 1 <= 0)
                Runtime.BuffSetInfo.RegisteredCharacterSeed.Remove(checkSeed);
            else
            {
                Runtime.BuffSetInfo.RegisteredCharacterSeed[checkSeed] -= 1;
                return; // 등록된 개체가 남아있다면 버프 감소할 필요 없음(버프에 필요한 최소 개체 수 1)
            }

            if (Runtime.BuffSetInfo.BuffProperties.TryGetValue(property, out var propertyValue))
            {
                if (propertyValue - 1 <= 0)
                    Runtime.BuffSetInfo.BuffProperties.Remove(property);
                else
                    Runtime.BuffSetInfo.BuffProperties[property] -= 1;
            }

            if (Runtime.BuffSetInfo.BuffTypes.TryGetValue(type, out var typeValue))
            {
                if (typeValue - 1 <= 0)
                    Runtime.BuffSetInfo.BuffTypes.Remove(type);
                else
                    Runtime.BuffSetInfo.BuffTypes[type] -= 1;
            }

            ExecuteUpdateBuffSetEvent();
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            Runtime.BuffSetInfo.RegisteredCharacterSeed.Clear();
            Runtime.BuffSetInfo.BuffProperties.Clear();
            Runtime.BuffSetInfo.BuffTypes.Clear();
        }

        private void ExecuteUpdateBuffSetEvent()
        {
            var tempEvent = Event.Events.UpdateBuffSetEvent;
            Event.EventManager.Broadcast(tempEvent);
        }
    }
}