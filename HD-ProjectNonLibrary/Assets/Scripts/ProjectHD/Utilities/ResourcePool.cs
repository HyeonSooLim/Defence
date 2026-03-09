using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utilities
{
    public class ResourcePool
    {
        private record AssetReferenceCount(int Count)
        {
            public AsyncOperationHandle Handle;
            public int Count { get; set; } = Count;
        }

        public readonly struct Key
        {
            public object Obj { get; }
            public Type Type { get; }

            public Key(object obj, Type type)
            {
                Obj = obj;
                Type = type;
            }

            public override bool Equals(object obj)
            {
                if (obj is Key key)
                {
                    return Equals(Obj, key.Obj) && Type == key.Type;
                }
                return false;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + (Obj?.GetHashCode() ?? 0);
                    hash = hash * 23 + (Type?.GetHashCode() ?? 0);
                    return hash;
                }
            }
        }

        private Dictionary<Key, AssetReferenceCount> _dicAssetReferenceCount;
        private Dictionary<int, Key> _dicAssetKey;

        public ResourcePool()
        {
            _dicAssetReferenceCount = new();
            _dicAssetKey = new();
        }

        ~ResourcePool()
        {
            if (_dicAssetReferenceCount != null)
                UnloadAll();

            _dicAssetReferenceCount = null;

            _dicAssetKey.Clear();
            _dicAssetKey = null;
        }

        #region Load

        private void InternalCacheOperation(Key key, in AsyncOperationHandle handle)
        {
            if (!_dicAssetReferenceCount.TryGetValue(key, out var referenceCount))
            {
                referenceCount = new(0);
                _dicAssetReferenceCount.Add(key, referenceCount);
            }

            referenceCount.Handle = handle;
            ++referenceCount.Count;
            _dicAssetReferenceCount[key] = referenceCount;
        }

        private void InternalCacheKey(Object asset, Key key)
        {
            var instanceID = asset.GetInstanceID();
            _dicAssetKey.TryAdd(instanceID, key);
        }

        public E Load<E>(object key, bool suppressErrorLog = false) where E : Object
        {
            if (key == null || ReferenceEquals(key, string.Empty))
            {
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError(new System.NullReferenceException());
                }
                return null;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<E>(key);
                handle.WaitForCompletion();
                if (!handle.IsValid()) return null;

                Key newKey = new(key, typeof(E));
                InternalCacheOperation(newKey, handle);
                InternalCacheKey(handle.Result, newKey);

                return handle.Result;
            }
            catch (Exception e)
            {
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError(e);
                }

                return null;
            }
        }

        public bool TryLoad<E>(object key, out E asset, bool suppressErrorLog = false) where E : Object
        {
            asset = null;
            if (key == null || ReferenceEquals(key, string.Empty))
            {
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError(new System.NullReferenceException());
                }
                return false;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<E>(key);
                handle.WaitForCompletion();
                if (!handle.IsValid()) return false;

                Key newKey = new(key, typeof(E));
                InternalCacheOperation(newKey, handle);
                InternalCacheKey(handle.Result, newKey);

                asset = handle.Result;
                return handle.Result;
            }
            catch (Exception e)


            {
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError(e);
                }
                return false;
            }
        }

        public IEnumerator LoadRoutine<E>(object key, bool suppressErrorLog = false) where E : Object
        {
            if (key == null || ReferenceEquals(key, string.Empty))
            {
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError(new System.NullReferenceException());
                }
                yield break;
            }

            var handle = Addressables.LoadAssetAsync<E>(key);
            yield return handle;

            if (!handle.IsValid()) yield break;
            Key newKey = new(key, typeof(E));
            InternalCacheOperation(newKey, handle);
            InternalCacheKey(handle.Result, newKey);
        }

        public async UniTask<E> LoadAsync<E>(object key, bool suppressErrorLog = false) where E : Object
        {
            if (key == null || ReferenceEquals(key, string.Empty))
            {
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError(new System.NullReferenceException());
                }
                return null;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<E>(key);
                var task = handle.ToUniTask();
                await task;
                if (!handle.IsValid()) return null;
                Key newKey = new(key, typeof(E));
                InternalCacheOperation(newKey, handle);
                InternalCacheKey(handle.Result, newKey);
                return handle.Result;
            }
            catch (Exception e)
            {
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError(e);
                }
                return null;
            }
        }

        #endregion

        #region Unload

        private void InternalRemoveInvalidHandle(Key key)
        {
            _dicAssetReferenceCount.Remove(key);
        }

        private int InternalDecacheOperation(Key key, in AsyncOperationHandle handle)
        {
            if (!_dicAssetReferenceCount.TryGetValue(key, out var referenceCount)) return -1;

            referenceCount.Handle = handle;
            --referenceCount.Count;
            if (referenceCount.Count <= 0)
            {
                _dicAssetReferenceCount.Remove(key);
            }
            else
            {
                _dicAssetReferenceCount[key] = referenceCount;
            }

            return referenceCount.Count;
        }

        private void InternalUnCacheKey(Object asset)
        {
            var instanceID = asset.GetInstanceID();
            _dicAssetKey.Remove(instanceID);
        }

        public int Unload(Key key, bool suppressErrorLog = false)
        {
            if (key.Obj == null) return -1;
            if (!_dicAssetReferenceCount.TryGetValue(key, out var referenceCount)) return -1;
            if (!referenceCount.Handle.IsValid())
            {
                InternalRemoveInvalidHandle(key);
                return -1;
            }

            if (referenceCount.Count == 0)
            {
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError($"{key} Ref Count == 0");
                }
                return -1;
            }

            int count = referenceCount.Count;
            var handle = referenceCount.Handle;
            var obj = handle.Result as Object;

            Addressables.Release(handle);
            int resultCount = InternalDecacheOperation(key, handle);
            if (resultCount == 0)
            {
                InternalUnCacheKey(obj);
                InternalRemoveInvalidHandle(key);
            }

            return resultCount;
        }

        public int Unload(Object asset, bool suppressErrorLog = false)
        {
            var instanceID = asset.GetInstanceID();
            if (!_dicAssetKey.TryGetValue(instanceID, out var key))
            {
#if UNITY_EDITOR
                if (!suppressErrorLog)
                {
                    Utilities.InternalDebug.LogError($"Asset not Exist : {asset}");
                }
#endif
                return -1;
            }
            if (key.Obj == null) throw new Exception("ERROR!!");

            return Unload(key, suppressErrorLog);
        }

        public void UnloadAll(Key key)
        {
            if (key.Obj == null) return;
            if (!_dicAssetReferenceCount.TryGetValue(key, out var referenceCount)) return;
            if (!referenceCount.Handle.IsValid())
            {
                InternalRemoveInvalidHandle(key);
                return;
            }

            if (referenceCount.Count == 0)
                throw new Exception($"{key} Ref Count == 0");

            int count = referenceCount.Count;
            var handle = referenceCount.Handle;

            InternalUnCacheKey(handle.Result as Object);
            for (int i = 0; i < count; ++i)
            {
                Addressables.Release(handle);
            }
            InternalRemoveInvalidHandle(key);
        }

        public void UnloadAll()
        {
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.BeginSample("UnloadAll_Addressables"); // 프로파일링 시작

            int unloadCount = 0;
            try
            {
                if (EditorSettings.enterPlayModeOptionsEnabled)
                {
                    int a = 3;
                    a += a;
                }
            }
            catch (Exception e)
            {
                return;
            }
#endif
            using var enumerator = _dicAssetReferenceCount.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var element = enumerator.Current;
                var key = element.Key;
                var referenceCount = element.Value;
                var count = referenceCount.Count;
                var handle = referenceCount.Handle;

                if (referenceCount.Count == 0)
                    throw new Exception($"{handle} Ref Count == 0");

                //for (int i = 0; i < count; ++i)
                //{
                //    Addressables.Release(handle);
                //}
                int handleCount = 0;
                try
                {
                    while (handle.IsValid())
                    {
#if UNITY_EDITOR
                        Utilities.InternalDebug.Log($"[Log_Addressable_Release] handle: {handle.DebugName}" +
                            $"\n UnloadCount: {++unloadCount}, handleCount: {++handleCount}");
#endif
                        Addressables.Release(handle);
                    }

                    if (handle.IsValid())
                    {
                        Utilities.InternalDebug.Log($"[Log_Addressable_Release] handle Set default: {handle.DebugName}");
                        Addressables.Release(handle);
                        referenceCount.Handle = default; // 명확하게 핸들을 초기화
                    }
                }
                catch (Exception e)
                {
                    Utilities.InternalDebug.LogError($"Addressables.Release(handle) failed: {e.Message}");
                }
            }

#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.EndSample(); // 프로파일링 종료
#endif
            _dicAssetKey.Clear();
            _dicAssetReferenceCount.Clear();
        }

        #endregion
    }
}