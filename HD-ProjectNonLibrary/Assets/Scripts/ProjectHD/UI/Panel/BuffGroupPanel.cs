using ProjectHD.Data;
using ProjectHD.GameData;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProjectHD.UI
{
    public class BuffGroupPanel : MonoBehaviour
    {
        [SerializeField] private UITool.TableView _tableView;

        private List<Data.IDataKey<int>> _tableViewItems;
        private StringBuilder _stringBuilder;

        private void Awake()
        {
            _tableViewItems = Utilities.StaticObjectPool.Pop<List<Data.IDataKey<int>>>();
            _tableViewItems.Clear();
            _stringBuilder = Utilities.StaticObjectPool.Pop<StringBuilder>();
            _stringBuilder.Clear();
        }

        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.UpdateBuffSetEvent>(UpdateBuffSetAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            _tableView.OnStart();
            UpdateTableViewData();
            SetTableView();
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.UpdateBuffSetEvent>(UpdateBuffSetAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
        }

        private void UpdateBuffSetAction(Event.UpdateBuffSetEvent @event)
        {
            UpdateTableView();
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            TableViewItemsClear();
            Utilities.StaticObjectPool.Push(_tableViewItems);
            _tableViewItems = null;

            _stringBuilder.Clear();
            Utilities.StaticObjectPool.Push(_stringBuilder);
            _stringBuilder = null;
        }

        private void UpdateTableView()
        {
            UpdateTableViewData();
            _tableView.ReLoad(false);
        }

        private void UpdateTableViewData()
        {
            TableViewItemsClear();
            int index = 0;
            foreach (var data in Global.DataManager.UnitPropertyBuffSetMaxCount.Set.Data)
            {
                BuffGroupData buffGroupData = Utilities.StaticObjectPool.Pop<BuffGroupData>();
                buffGroupData.Clear();

                int activeCount = 0;
                if (Runtime.BuffSetInfo.BuffProperties.TryGetValue(data.Key, out var value))
                    activeCount = value;

                buffGroupData.SetData(index, data.Key, ProjectEnum.CharacterType.None, activeCount, data.MaxCount);
                _tableViewItems.Add(buffGroupData);
                index++;
            }

            index = 0;
            foreach (var data in Global.DataManager.UnitTypeBuffSetMaxCount.Set.Data)
            {
                BuffGroupData buffGroupData = Utilities.StaticObjectPool.Pop<BuffGroupData>();
                buffGroupData.Clear();

                int activeCount = 0;
                if (Runtime.BuffSetInfo.BuffTypes.TryGetValue(data.Key, out var value))
                    activeCount = value;

                buffGroupData.SetData(index, ProjectEnum.UnitProperty.None, data.Key, activeCount, data.MaxCount);
                _tableViewItems.Add(buffGroupData);
                index++;
            }
        }

        private void SetTableView()
        {
            _tableView.SetDelegate(new(_tableViewItems, (gameObject, item) =>
            {
                var slot = gameObject.GetComponent<BuffSetSlot>();
                var data = (BuffGroupData)item;

                _stringBuilder.Clear();
                slot.Destruct();
                int activeCount = Mathf.Min(data.CurrentActiveCount, data.MaxCount);
                bool isColored = false;
                if (Global.DataManager.UnitPropertyBuffSet.TryGet((data.CharacterProperty, data.CurrentActiveCount), out var unitPropertyBuffSet, true))
                {
                    isColored = true;
                }
                else if (Global.DataManager.UnitTypeBuffSet.TryGet((data.CharacterType, data.CurrentActiveCount), out var unitTypeBuffSet, true))
                {
                    isColored = true;
                }

                if (isColored)
                {
                    _stringBuilder.Append("<color=#4fecff>");
                    _stringBuilder.Append(activeCount);
                    _stringBuilder.Append("</color>");
                }
                else
                    _stringBuilder.Append(activeCount);

                _stringBuilder.Append($"/{data.MaxCount}");
                slot.Construct(data.IconName, _stringBuilder.ToString());
            }));

            _tableView.ReLoad(false);
        }

        private void TableViewItemsClear()
        {
            for (int i = 0; i < _tableViewItems.Count; i++)
            {
                var item = _tableViewItems[i];
                if (item is BuffGroupData data)
                {
                    data.Clear();
                    Utilities.StaticObjectPool.Push(data);
                }
            }
            _tableViewItems.Clear();
        }

        public class BuffGroupData : Data.IDataKey<int>
        {
            public int Key { get; private set; }
            public ProjectEnum.UnitProperty CharacterProperty { get; private set; }
            public ProjectEnum.CharacterType CharacterType { get; private set; }
            public int CurrentActiveCount { get; private set; }
            public int MaxCount { get; private set; }
            public string IconName { get; private set; }

            public BuffGroupData()
            {
            }

            public void SetData(int key, ProjectEnum.UnitProperty characterProperty, ProjectEnum.CharacterType characterType, int currentActiveCount, int maxCount)
            {
                Key = key;
                CharacterProperty = characterProperty;
                CharacterType = characterType;
                CurrentActiveCount = currentActiveCount;
                MaxCount = maxCount;
                if (Global.DataManager.UnitPropertyDefine.TryGet(characterProperty, out var propertyDefine, true))
                {
                    IconName = propertyDefine.IconName;
                }
                else if (Global.DataManager.UnitTypeDefine.TryGet(characterType, out var typeDefine, true))
                {
                    IconName = typeDefine.IconName;
                }
                else
                {
                    IconName = string.Empty;
                }
            }

            public void Clear()
            {
                CharacterProperty = ProjectEnum.UnitProperty.None;
                CharacterType = ProjectEnum.CharacterType.None;
                CurrentActiveCount = 0;
                MaxCount = 0;
            }
        }
    }
}