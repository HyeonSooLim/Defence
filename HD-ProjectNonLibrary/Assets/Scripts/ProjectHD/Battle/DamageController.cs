using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Battle
{
    // 캐릭터의 데미지 처리 담당
    // Damageable 컴포넌트를 가진 오브젝트가 데미지를 입을 때마다 호출됨
    // 데미지 계산, 상태 이상 적용, 체력 감소 등을 처리
    // 데미지를 입은 후 사망 여부 확인 및 사망 처리도 포함
    public class DamageController : MonoBehaviour
    {
        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.SendDamageEvent>(SendDamageAction);
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.SendDamageEvent>(SendDamageAction);
        }

        private void SendDamageAction(Event.SendDamageEvent @event)
        {
            if (@event.Damageable == null)
                return;
            var damageableID = @event.Damageable.GetInstanceID();
            var attackProperty = @event.Attackable.GetUnitProperty();
            var defenseProperty = @event.Damageable.GetUnitProperty();
            var characterType = @event.Attackable.GetCharacterType();

            var attackDamage = Mathf.RoundToInt(Runtime.BuffSetInfo.GetBuffTypeValue(@event.BaseDamage, attackProperty, characterType, ProjectEnum.BuffType.Attack));
            // TO DO: 속성 상성에 따른 데미지 보정 로직 추가
            if (!Runtime.StageInformation.SpawnedEnemies.TryGetValue(damageableID, out var monsterBehavior))
            {
                Utilities.InternalDebug.LogError($"[{this.name}] 데미지를 입은 오브젝트를 찾을 수 없습니다. InstanceID: {damageableID}");
                return;
            }

            ExecuteCalculatedDamageEvent(attackDamage, @event.Damageable);
            @event.Damageable.TakeDamage(attackDamage);
        }

        private void ExecuteCalculatedDamageEvent(int damage, Battle.IDamageable damageable)
        {
            var tempEvent = Event.Events.CalculatedDamageEvent;
            tempEvent.FinalDamage = damage;
            tempEvent.Damageable = damageable;
            Event.EventManager.Broadcast(tempEvent);
        }
    }
}