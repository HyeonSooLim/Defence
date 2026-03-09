using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.UI
{
    public class ConinPanel : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _coinText;

        private void Awake()
        {
            // TO DO : 임시 설정이므로 추후 삭제 필요
            if (Global.DataManager.TestConstValue.TryGet(ProjectEnum.ConstDefine.StartingGoodsPrice, out var constValue))
            {
                Runtime.PlayerData.Account.Coin = constValue.Val;
            }

            _coinText.SetText($"{Runtime.PlayerData.Account.Coin}");
        }

        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.ChangeCoinEvent>(ChangeCoinAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.AddListener<Event.MonsterDieEvent>(MonsterDieAction);
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.ChangeCoinEvent>(ChangeCoinAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.RemoveListener<Event.MonsterDieEvent>(MonsterDieAction);
        }

        private void ChangeCoinAction(Event.ChangeCoinEvent @event) // TO DO : 콘트롤러 기능임
        {
            @event.IsSuccess = false;
            var canChange = Runtime.PlayerData.Account.Coin + @event.Amount >= 0;
            if (!canChange)
                return;

            Runtime.PlayerData.Account.Coin += @event.Amount;
            _coinText.SetText($"{Runtime.PlayerData.Account.Coin}");
            @event.IsSuccess = true;
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {

        }

        private void MonsterDieAction(Event.MonsterDieEvent @event)
        {
            int coin = 0;
            if (Runtime.StageInformation.SpawnedEnemies.TryGetValue(@event.InstanceID, out var enemyData))
            {
                if (enemyData.IsBoss)
                {
                    if (Global.DataManager.TestConstValue.TryGet(ProjectEnum.ConstDefine.BossMonsterKillingReward, out var constValue))
                    {
                        coin = constValue.Val;
                    }
                }
                else
                {
                    if(Global.DataManager.TestConstValue.TryGet(ProjectEnum.ConstDefine.MonsterKillingReward, out var constValue))
                    {
                        coin = constValue.Val;
                    }
                }
            }

            ExecuteChangeCoinEvent(coin);
        }

        private void ExecuteChangeCoinEvent(int amount)
        {
            var changeCoinEvent = Event.Events.ChangeCoinEvent;
            changeCoinEvent.Amount = amount;
            Event.EventManager.Broadcast(changeCoinEvent);
        }
    }
}