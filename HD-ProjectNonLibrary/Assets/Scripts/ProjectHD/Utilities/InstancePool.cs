using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utilities
{
    [System.Serializable]
    public class InstancePool
    {
        private record InternalPool
        {
            private UnityEngine.Object _source;
            public UnityEngine.Object Source => _source;
            private Queue<UnityEngine.Object> _pool;

            public InternalPool(UnityEngine.Object source, int capacity)
            {
                _source = source;
                _pool = new (capacity);
            }

            public UnityEngine.Object Get()
            {
                UnityEngine.Object retInstance = null;
                if (_pool.Count > 0)
                {
                    retInstance = _pool.Dequeue();
                }
                else
                {
                    retInstance = UnityEngine.Object.Instantiate(_source);
                    UnityEngine.Object.DontDestroyOnLoad(retInstance);
                }
                return retInstance;
            }

            public E Get<E>() where E : UnityEngine.Object
            {
                var typedSource = _source as E;
                if (!typedSource) return null;

                if (_pool.Count > 0)
                {
                    var tempInstance = _pool.Dequeue();
                    var retInstance = tempInstance as E;
                    if (!retInstance)
                    {
                        _pool.Enqueue(tempInstance);
                        return null;
                    }
                    return retInstance;
                }
                else
                {
                    E retInstance = UnityEngine.Object.Instantiate<E>(typedSource);
                    UnityEngine.Object.DontDestroyOnLoad(retInstance);
                    return retInstance;
                }
            }

            public void Return(UnityEngine.Object instance)
            {
                if (!instance) return;
                if (!_source.GetType().IsInstanceOfType(instance))
                {
                    UnityEngine.Object.Destroy(instance);
                    return;
                }

                _pool.Enqueue(instance);
            }

            public void Return<E>(E instance) where E : UnityEngine.Object
            {
                if (!instance) return;
                if (!_source.GetType().IsInstanceOfType(instance))
                {
                    UnityEngine.Object.Destroy(instance);
                    return;
                }

                _pool.Enqueue(instance);
            }

            public void Clear()
            {
#if UNITY_EDITOR
                try
                {
                    var temp = EditorSettings.enterPlayModeOptionsEnabled;
                }
                catch (Exception e)
                {
                    return;
                }
#endif
                using var enumerator = _pool.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var element = enumerator.Current;
                    UnityEngine.Object.Destroy(element);
                }
                _pool.Clear();
            }

            ~InternalPool()
            {
                if (_pool.Count > 0)
                {
                    Clear();
                }

                _source = null;
                _pool = null;
            }
        }

        private static readonly int POOL_CAPACITY = 10;
        
        private Dictionary<object, InternalPool> _dicPool;
        private Dictionary<int, object> _dicInstanceKey;

        private ResourcePool _resourcePool;

        public InstancePool()
        {
            _resourcePool = new ResourcePool();
            _dicPool = new();
            _dicInstanceKey = new();
        }

        ~InstancePool()
        {
            _dicInstanceKey = null;
            _dicPool = null;
            _resourcePool = null;
        }
        
        public E Get<E>(object key) where E : UnityEngine.Object
        {
            if (key == null) return null;
            if (key is string textKey && string.IsNullOrEmpty(textKey)) return null;
            if (typeof(E) == typeof(GameObject)) throw new Exception("Error!!");
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                E source = _resourcePool.Load<E>(key);
                if (!source) return null;
                var newPool = new InternalPool(source, POOL_CAPACITY);
                _dicPool.Add(key, newPool);
                pool = newPool;
            }

            var instance = pool.Get<E>();
            if (instance)
            {
                if (!_dicInstanceKey.TryAdd(instance.GetInstanceID(), key))
                {
                    throw new System.Exception("Error!!");
                }
            }
            return instance;
        }

        public async UniTask<E> GetAsync<E>(object key) where E : UnityEngine.Object
        {
            if (key == null) return null;
            if (key is string textKey && string.IsNullOrEmpty(textKey)) return null;
            if (typeof(E) == typeof(GameObject)) throw new Exception("Error!!");
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                E source = await _resourcePool.LoadAsync<E>(key);
                if (!source) return null;
                var newPool = new InternalPool(source, POOL_CAPACITY);
                _dicPool.Add(key, newPool);
                pool = newPool;
            }

            var instance = pool.Get<E>();
            if (instance)
            {
                if (!_dicInstanceKey.TryAdd(instance.GetInstanceID(), key))
                {
                    throw new System.Exception("Error!!");
                }
            }

            return instance;
        }

        public bool TryGet<E>(string key, out E outInstance) where E : UnityEngine.Object
        {
            outInstance = Get<E>(key);
            return outInstance;
        }

        public void Return<E>(E instance) where E : UnityEngine.Object
        {
            if (!instance) return;
            if (!_dicInstanceKey.TryGetValue(instance.GetInstanceID(), out var key))
            {
                UnityEngine.Object.Destroy(instance);
                InternalDebug.LogError("Not Found Key");
                return;
            }

            _dicInstanceKey.Remove(instance.GetInstanceID());
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                UnityEngine.Object.Destroy(instance);
                InternalDebug.LogError("Not Found Pool");
                return;
            }

            pool.Return<E>(instance);
        }

        public void ReleaseAll()
        {
            using var enumerator = _dicPool.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var element = enumerator.Current;
                var key = element.Key;
                var pool = element.Value;
                pool.Clear();
            }
            _dicPool.Clear();
            _dicInstanceKey.Clear();
            _resourcePool.UnloadAll();
        }
    }
}