using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace ProjectHD.GameData
{
    #region

    public interface IKey<E>
    {
        ref readonly E GetKey();
    }

    public interface IListKey<E>
    {
        ref readonly E GetListKey();
    }

    public interface IKey<E1, E2, E> : IKey<E>
    {
        E MakeKey<E1, E2>();
    }

    public interface Inventory<K, E> where E : IKey<K>
    {
        IReadOnlyList<E> storage { get; }

        void Cache();
        bool Add(in E item);
        void Replace(in E item);
        bool Remove(in K itemKey);
        bool Remove(in K itemKey, out E itemRemoved);
        bool Remove(in E item);
        E Get(in K itemKey);
        bool TryGet(in K itemKey, out E item);
        void Clear();
    }

    public interface ListInventory<K, E> where E : IListKey<K>
    {
        IReadOnlyList<E> Get(in K itemKey);
        bool Remove(in K itemKey);
        bool Remove(in K itemKey, out IReadOnlyList<E> itemRemoved);
        bool TryGet(in K itemKey, out IReadOnlyList<E> item);
    }

    [Serializable]
    public class ComplexInventory<K1, K2, E> : Inventory<K1, E>, ListInventory<K2, E> where E : IKey<K1>, IListKey<K2>
    {
        [SerializeField]
        private List<E> _storage = new List<E>();
        public IReadOnlyList<E> storage => _storage;
        [NonSerialized]
        private Dictionary<K1, E> _cache1 = new Dictionary<K1, E>();
        [NonSerialized]
        private Dictionary<K2, List<E>> _cache2 = new Dictionary<K2, List<E>>();

        public void Cache()
        {
            if (_cache1 != null)
                _cache1.Clear();
            else
                _cache1 = new Dictionary<K1, E>();
            for (int i = 0; i < _storage.Count; ++i)
            {
                var item = _storage[i];
                _cache1.Add(item.GetKey(), item);
            }

            if (_cache2 != null)
                _cache2.Clear();
            else
                _cache2 = new Dictionary<K2, List<E>>();
            for (int i = 0; i < _storage.Count; ++i)
            {
                var item = _storage[i];
                if (_cache2.TryGetValue(item.GetListKey(), out var listItem))
                    listItem.Add(item);
                else
                    _cache2.Add(item.GetListKey(), new List<E>() { item });
            }
        }

        public bool Add(in E item)
        {
            if (_cache1.ContainsKey(item.GetKey()))
                return false;
            // if (_cache2.ContainsKey(item.GetListKey())) 
            //     return false;

            _storage.Add(item);
            var key = item.GetKey();
            var listKey = item.GetListKey();
            if (_cache1.ContainsKey(key))
                _cache1[key] = item;
            else
                _cache1.Add(key, item);

            if (_cache2.ContainsKey(listKey))
                _cache2[item.GetListKey()].Add(item);
            else
                _cache2.Add(listKey, new List<E>() { item });

            return true;
        }

        public void Replace(in E item)
        {
            //????
            if (_cache1.TryGetValue(item.GetKey(), out var existItem))
            {
                _cache1[item.GetKey()] = item;
                Remove(existItem);
            }
            Add(item);
        }

        public bool Remove(in K1 itemKey)
        {
            if (itemKey == null)
                return false;

            if (_cache1.TryGetValue(itemKey, out var item))
            {
                _storage.Remove(item);
                if (_cache2.TryGetValue(item.GetListKey(), out var subItem))
                {
                    if (subItem.Count == 1)
                    {
                        _cache1.Remove(itemKey);
                        _cache2.Remove(item.GetListKey());
                    }
                    else
                    {
                        Cache();
                    }

                }

                return true;
            }

            return false;
        }

        public bool Remove(in K1 itemKey, out E itemRemoved)
        {
            itemRemoved = default;
            if (itemKey == null) return false;

            if (_cache1.TryGetValue(itemKey, out var item))
            {
                itemRemoved = item;
                _storage.Remove(item);
                _cache1.Remove(itemKey);
                if (_cache2.TryGetValue(item.GetListKey(), out var subItem))
                    _cache2.Remove(item.GetListKey());
                return true;
            }

            return false;
        }

        public bool Remove(in E item)
        {
            if (item == null) return false;

            _storage.Remove(item);

            if (_cache1.TryGetValue(item.GetKey(), out var outItem))
            {
                _cache1.Remove(item.GetKey());
                if (_cache2.TryGetValue(item.GetListKey(), out var subItem))
                    _cache2.Remove(item.GetListKey());
                return true;
            }

            return false;
        }

        public E Get(in K1 itemKey)
        {
            return _cache1.TryGetValue(itemKey, out E item) ? item : default(E);
        }

        public bool TryGet(in K1 itemKey, out E item)
        {
            return _cache1.TryGetValue(itemKey, out item);
        }

        public void Clear()
        {
            _cache1.Clear();
            _cache2.Clear();
            _storage.Clear();
        }

        public bool Remove(in K2 itemKey)
        {
            if (itemKey == null)
                return false;

            if (_cache2.TryGetValue(itemKey, out var item))
            {
                for (int i = 0; i < item.Count; i++)
                {
                    if (_cache1.TryGetValue(item[i].GetKey(), out var subItem))
                        _cache1.Remove(item[i].GetKey());
                    _storage.Remove(item[i]);
                }
                _cache2.Remove(itemKey);
                return true;
            }

            return false;
        }

        public bool Remove(in K2 itemKey, out IReadOnlyList<E> itemRemoved)
        {
            itemRemoved = default;
            if (itemKey == null) return false;

            if (_cache2.TryGetValue(itemKey, out var item))
            {
                itemRemoved = item;
                for (int i = 0; i < item.Count; i++)
                {
                    if (_cache1.TryGetValue(item[i].GetKey(), out var subItem))
                        _cache1.Remove(item[i].GetKey());
                    _storage.Remove(item[i]);
                }
                _cache2.Remove(itemKey);
                return true;
            }

            return false;
        }

        public bool TryGet(in K2 itemKey, out IReadOnlyList<E> item)
        {
            item = null;
            if (_cache2.TryGetValue(itemKey, out var outitem))
                item = outitem;
            return item != null;
        }

        public IReadOnlyList<E> Get(in K2 itemKey)
        {
            return _cache2.GetValueOrDefault(itemKey);
        }
    }

    [Serializable]
    public class SimpleInventory<K, E> : Inventory<K, E> where E : IKey<K>
    {
        [SerializeField]
        private List<E> _storage;
        public IReadOnlyList<E> storage => _storage;

        [NonSerialized]
        private Dictionary<K, E> _cache;

        public SimpleInventory()
        {
            _storage = new List<E>(100);
            _cache = new Dictionary<K, E>();
        }

        public SimpleInventory(IEqualityComparer<K> comparer)
        {
            _storage = new List<E>(100);
            _cache = new Dictionary<K, E>(500, comparer);
        }

        public void Cache()
        {
            if (_cache != null)
                _cache.Clear();
            else
                _cache = new Dictionary<K, E>();
            for (int i = 0; i < _storage.Count; ++i)
            {
                var item = _storage[i];
                _cache.Add(item.GetKey(), item);
            }
        }

        public bool Add(in E item)
        {
            if (_cache.ContainsKey(item.GetKey())) return false;

            _storage.Add(item);
            _cache[item.GetKey()] = item;
            return true;
        }

        public void Replace(in E item)
        {
            if (_cache.TryGetValue(item.GetKey(), out var existItem))
                Remove(existItem);
            Add(item);
        }

        public bool Remove(in K itemKey)
        {
            if (itemKey == null) return false;

            if (_cache.TryGetValue(itemKey, out var item))
            {
                _storage.Remove(item);
                _cache.Remove(itemKey);
                return true;
            }

            return false;
        }

        public bool Remove(in K itemKey, out E itemRemoved)
        {
            itemRemoved = default;
            if (itemKey == null) return false;

            if (_cache.TryGetValue(itemKey, out var item))
            {
                itemRemoved = item;
                _storage.Remove(item);
                _cache.Remove(itemKey);
                return true;
            }

            return false;
        }

        public bool RemoveAt(int index, out E itemRemoved)
        {
            itemRemoved = default;
            if (_storage.Count <= index)
                return false;

            var key = _storage[index].GetKey();

            if (_cache.TryGetValue(key, out var item))
            {
                itemRemoved = item;
                _storage.RemoveAt(index);
                _cache.Remove(key);
                return true;
            }
            return false;
        }

        public bool Remove(in E item)
        {
            if (item == null) return false;

            _storage.Remove(item);
            if (_cache.TryGetValue(item.GetKey(), out var outItem))
            {
                _cache.Remove(item.GetKey());
                return true;
            }

            return false;
        }

        public E Get(in K itemKey)
        {
            return _cache.TryGetValue(itemKey, out E item) ? item : default(E);
        }

        public bool TryGet(in K itemKey, out E item)
        {
            return _cache.TryGetValue(itemKey, out item);
        }

        public bool ContainsKey(in K itemKey)
        {
            return _cache.ContainsKey(itemKey);
        }

        public void Clear()
        {
            _cache.Clear();
            _storage.Clear();
        }

        public void Sort(IComparer<E> comparer)
        {
            _storage.Sort(comparer);
        }
    }

    #endregion

    [Serializable]
    public class PlayerData
    {
        public Account Account;

        public PlayerData()
        {
            Account = new();
        }        
    }

    [Serializable]
    public class Account
    {
        public long ID;
        public long Tag;
        public string Nick;
        public ProjectEnum.PlayerType PlayerType = ProjectEnum.PlayerType.Player01; // TO DO : 나중에 네트워크 작업 시 호스트와 게스트 구분. 현재(25-09-10) 임시 기본값
        public int Coin;
    }    
}
