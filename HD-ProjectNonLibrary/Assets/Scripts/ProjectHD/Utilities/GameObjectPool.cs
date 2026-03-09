using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Utilities
{
    public class GameObjectPool
    {
        private record InternalPool
        {
            private UnityEngine.GameObject _source;
            public UnityEngine.GameObject Source => _source;
            private Queue<UnityEngine.GameObject> _pool;

            private UnityEngine.GameObject _rootObject;
            private UnityEngine.Transform _rootTransform;
            private bool _dontDestroy = false;
            public void Construct(UnityEngine.GameObject source, GameObject rootObject, int capacity, bool dontDestroy)
            {
                _source = source;
                _pool = new (capacity);

                _rootObject = rootObject;
                _rootObject.SetActive(false);
                _rootTransform = _rootObject.transform;
                _dontDestroy = dontDestroy;
            }

            public void Destruct(out GameObject soruce, out GameObject rootObject)
            {
                using var enumerator = _pool.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var element = enumerator.Current;
                    UnityEngine.GameObject.Destroy(element);
                }
                _pool.Clear();
                _pool = null;
                
                rootObject = _rootObject;
                soruce = _source;

                _rootTransform = null;
                _rootObject = null;
                _source = null;
            }

            public UnityEngine.GameObject Get()
            {
                UnityEngine.GameObject retInstance = null;
                if (_pool.Count > 0)
                {
                    retInstance = _pool.Dequeue();
                }
                else
                {
                    retInstance = UnityEngine.GameObject.Instantiate(_source);
                    UnityEngine.GameObject.DontDestroyOnLoad(retInstance);
                }

                MoveToRoot(retInstance);
                
                return retInstance;
            }

            public void Return(UnityEngine.GameObject instance)
            {
                if (!instance) return;

                _pool.Enqueue(instance);

                MoveToRoot(instance);
            }

            private void MoveToRoot(UnityEngine.GameObject instance)
            {
                if (!instance) return;
                instance.transform.SetParent(_rootObject.transform);
            }
        }
        
        private static readonly int POOL_CAPACITY = 10;
        
        private Dictionary<object, InternalPool> _dicPool;
        private Dictionary<int, object> _dicInstanceKey;

        private ResourcePool _resourcePool;

        private System.Lazy<GameObject> _lazyRootObject;
        public GameObject RootObject => _lazyRootObject.Value;
        private Transform _rootTransform;
        private string _name;

        public GameObjectPool(string name)
        {
            _name = name;
            _resourcePool = new ResourcePool();
            _lazyRootObject = new System.Lazy<GameObject>(CreateRootObject);
            _dicPool = new();
            _dicInstanceKey = new();
        }

        ~GameObjectPool()
        {
#if !UNITY_EDITOR
            var rootObject = _lazyRootObject.Value;
            if (rootObject)
                GameObject.Destroy(rootObject);
#endif
            _rootTransform = null;
            _lazyRootObject = null;
            
            _dicInstanceKey = null;
            _dicPool = null;
            _resourcePool = null;
        }

        private GameObject CreateRootObject()
        {
            var rootObject = new GameObject($"GameObjectPool-{_name}");
            rootObject.transform.SetParent(null);
            UnityEngine.GameObject.DontDestroyOnLoad(rootObject);
            _rootTransform = rootObject.transform;
            return rootObject;
        }
        
        public GameObject Get(string key, bool active = true, bool dontDestroy = false)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                var source = _resourcePool.Load<GameObject>(key);
                if (!source) return null;
                var rootObject = new GameObject(source.name);
                rootObject.transform.SetParent(RootObject.transform);
                rootObject.SetActive(false);
                var newPool = new InternalPool();
                newPool.Construct(source, rootObject, POOL_CAPACITY, dontDestroy);
                _dicPool.Add(key, newPool);
                pool = newPool;
            }
            
            var instance = pool.Get();
            if (instance)
            {
                instance.SetActive(active);
                if (!_dicInstanceKey.TryAdd(instance.GetInstanceID(), key))
                {
                    throw new System.Exception("Error!!");
                }
            }
            return instance;
        }
        
        public GameObject Get(object key, bool active = true, bool dontDestroy = false)
        {
            if (key == null) return null;
            if (key is string textKey && string.IsNullOrEmpty(textKey)) return null;
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                var source = _resourcePool.Load<GameObject>(key);
                if (!source) return null;
                var rootObject = new GameObject(source.name);
                rootObject.transform.SetParent(RootObject.transform);
                rootObject.SetActive(false);
                var newPool = new InternalPool();
                newPool.Construct(source, rootObject, POOL_CAPACITY, dontDestroy);
                _dicPool.Add(key, newPool);
                pool = newPool;
            }
            
            var instance = pool.Get();
            if (instance)
            {
                instance.SetActive(active);
                if (!_dicInstanceKey.TryAdd(instance.GetInstanceID(), key))
                {
                    throw new System.Exception("Error!!");
                }
            }
            return instance;
        }

        public async UniTask<GameObject> GetAsync(object key, bool active = true, bool dontDestroy = false)
        {
            if (key == null) return null;
            if (key is string textKey && string.IsNullOrEmpty(textKey)) return null;
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                var source = await _resourcePool.LoadAsync<GameObject>(key);
                if (!source) return null;
                var rootObject = new GameObject(source.name);
                rootObject.transform.SetParent(RootObject.transform);
                rootObject.SetActive(false);
                var newPool = new InternalPool();
                newPool.Construct(source, rootObject, POOL_CAPACITY, dontDestroy);
                _dicPool.Add(key, newPool);
                pool = newPool;
            }

            var instance = pool.Get();
            if (instance)
            {
                instance.SetActive(active);
                if (!_dicInstanceKey.TryAdd(instance.GetInstanceID(), key))
                {
                    throw new System.Exception("Error!!");
                }
            }

            return instance;
        }

        public bool TryGet(object key, out GameObject outInstance, bool active = true)
        {
            outInstance = null;
            outInstance = Get(key, active);
            return outInstance;
        }
        
        public bool TryGet(string key, out GameObject outInstance, bool active = true)
        {
            outInstance = null;
            outInstance = Get(key, active);
            return outInstance;
        }

        public void Return(GameObject instance)
        {
            if (!instance) return;
            if (!_dicInstanceKey.TryGetValue(instance.GetInstanceID(), out var key))
            {
                // UnityEngine.GameObject.Destroy(instance);
                _dicInstanceKey.Add(instance.GetInstanceID(), instance);
                InternalDebug.LogError($"[Log][Pool]: {instance.gameObject.name} Not Found Key");
                return;
            }

            _dicInstanceKey.Remove(instance.GetInstanceID());
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                UnityEngine.GameObject.Destroy(instance);
                InternalDebug.LogError($"[Log][Pool]: {instance.gameObject.name} Not Found Pool");
                return;
            }

            instance.SetActive(false);
            pool.Return(instance);
        }

        public void ReleaseAll()
        {
            using var enumerator = _dicPool.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var element = enumerator.Current;
                var key = element.Key;
                var pool = element.Value;
                pool.Destruct(out var source, out var rootObject);
                UnityEngine.GameObject.Destroy(rootObject);
            }
            _dicPool.Clear();
            _dicInstanceKey.Clear();
            _resourcePool.UnloadAll();
        }

        public ref struct RAII
        {
            private GameObjectPool _gameObjectPool;
            private GameObject _data;

            public RAII(GameObjectPool gameObjectPool, object key, out GameObject outObject, bool active = true)
            {
                _gameObjectPool = gameObjectPool;
                _data = gameObjectPool.Get(key, active);
                outObject = _data;
            }

            public void Dispose()
            {
                _gameObjectPool.Return(_data);
                _gameObjectPool = null;
                _data = null;
            }
        }
    }
}