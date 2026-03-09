using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utilities
{
    public class SceneInstancePool
    {
        private record InternalPool
        {
            private object _sceneAssetKey;
            public string Path { get; private set; }
            private Queue<SceneInstance> _pool;

            public InternalPool(object sceneAssetKey)
            {
                _sceneAssetKey = sceneAssetKey;
                Path = sceneAssetKey as string ?? string.Empty;
                _pool = new();
            }

            ~InternalPool()
            {
                if (_pool.Count > 0)
                {
                    Clear();
                }

                _sceneAssetKey = null;
                _pool = null;
                Path = null;
            }

            public SceneInstance Get()
            {
                SceneInstance retInstance;
                if (_pool.Count > 0)
                    retInstance = _pool.Dequeue();
                else
                    retInstance = Addressables.LoadSceneAsync(_sceneAssetKey, LoadSceneMode.Additive).WaitForCompletion();
                
                SetActiveRootObject(retInstance.Scene, false);
                
                return retInstance;
            }

            public async UniTask<SceneInstance> GetAsync()
            {
                SceneInstance retInstance;
                if (_pool.Count > 0)
                    retInstance = _pool.Dequeue();
                else
                {
                    retInstance = await Addressables.LoadSceneAsync(_sceneAssetKey, LoadSceneMode.Additive, false);
                    await retInstance.ActivateAsync();
                }

                SetActiveRootObject(retInstance.Scene, false);
                
                return retInstance;
            }

            public void Return(in SceneInstance sceneInstance)
            {
                if (!sceneInstance.Scene.path.Equals(Path)) return;

                SetActiveRootObject(sceneInstance.Scene, false);

                _pool.Enqueue(sceneInstance);
            }

            private void SetActiveRootObject(Scene scene, bool active)
            {
                var list = StaticObjectPool.Pop<List<GameObject>>();
                list.Clear();
                list.Capacity = 256;
                scene.GetRootGameObjects(list);
                for (int i = 0; i < list.Count; ++i)
                {
                    list[i].SetActive(active);
                }

                list.Clear();
                StaticObjectPool.Push(list);
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
                    Addressables.UnloadSceneAsync(element).WaitForCompletion();
                }

                _pool.Clear();
            }

            public async UniTask<TimeSpan> ClearAsync()
            {
                using var enumerator = _pool.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var element = enumerator.Current;
                    await Addressables.UnloadSceneAsync(element);
                }

                _pool.Clear();
                
                return TimeSpan.Zero;
            }
        }

        private Dictionary<object, InternalPool> _dicPool;
        private Dictionary<int, object> _dicInstanceKey;

        public SceneInstancePool()
        {
            _dicPool = new Dictionary<object, InternalPool>();
            
        }

        public SceneInstance Get(object key)
        {
            if (key == null) return new SceneInstance();
            if (key is string textKey && string.IsNullOrEmpty(textKey)) return new SceneInstance();
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                pool = new(key);
                _dicPool.Add(key, pool);
            }
            
            return pool.Get();
        }

        public async UniTask<SceneInstance> GetAsync(object key)
        {
            if (key == null) return new SceneInstance();
            if (key is string textKey && string.IsNullOrEmpty(textKey)) return new SceneInstance();
            if (!_dicPool.TryGetValue(key, out var pool))
            {
                pool = new(key);
                _dicPool.Add(key, pool);
            }

            return await pool.GetAsync();
        }

        public void Return(SceneInstance sceneInstance)
        {
            using var enumerator = _dicPool.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var element = enumerator.Current;
                var pool = element.Value;
                if (!pool.Path.Equals(sceneInstance.Scene.path)) continue;
                
                pool.Return(sceneInstance);
                break;
            }
        }

        public void Clear(object key)
        {
            if (!_dicPool.TryGetValue(key, out var pool)) return;
            _dicPool.Remove(key);
            pool.Clear();
        }

        public async UniTask ClearAsync(object key)
        {
            if (!_dicPool.TryGetValue(key, out var pool)) return;
            _dicPool.Remove(key);
            await pool.ClearAsync();
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
            using var enumerator = _dicPool.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var element = enumerator.Current;
                var pool = element.Value;
                pool.Clear();
            }

            _dicPool.Clear();
        }

        public async UniTask ClearAsync()
        {
            using var enumerator = _dicPool.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var element = enumerator.Current;
                var pool = element.Value;
                await pool.ClearAsync();
            }

            _dicPool.Clear();
        }

        ~SceneInstancePool()
        {
            if (_dicPool != null)
            {
                Clear();
            }
            _dicPool = null;
        }
    }   
}