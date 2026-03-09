using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Battle
{
    public interface IAttackable
    {
        void SpawnEffect(string key);
        void SendDamage();
        ProjectEnum.UnitProperty GetUnitProperty();
        ProjectEnum.CharacterType GetCharacterType();
    }

    public interface IDamageable
    {
        void TakeDamage(int damage);
        int GetInstanceID();
        ProjectEnum.UnitProperty GetUnitProperty();
    }

    public interface IHealable
    {
        void Heal(int healAmount);
    }

    public interface ISelectable
    {
        void Select();
        void Deselect();
    }

    public interface IHexhable
    {
        int CurrentHexQ { get; }
        int CurrentHexR { get; }
        void UpdateHexAndExecuteEvent(bool isFirst = false);
    }
}

