using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace ProjectHD.Data
{
    public class DataSet<E>
    {
        private List<E> _data;
        public IReadOnlyList<E> Data=> _data;
        
        public DataSet()
        {
            _data = new();
        }
        
        public void ReadData(string jsonData)
        {
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate,
            };
            E[] dataSet = Newtonsoft.Json.JsonConvert.DeserializeObject<E[]>(jsonData, settings);
            _data.AddRange(dataSet);
        }
        
        public async UniTask ReadDataAsync(string jsonData)
        {
            using TextReader textReader = new StringReader(jsonData);
            using Newtonsoft.Json.JsonTextReader jsonTextReader = new Newtonsoft.Json.JsonTextReader(textReader);

            Newtonsoft.Json.JsonSerializer jsonSerializer = new Newtonsoft.Json.JsonSerializer();
            jsonSerializer.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None;
            jsonSerializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
            jsonSerializer.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate;
            E[] dataSet = jsonSerializer.Deserialize<E[]>(jsonTextReader);
            _data.AddRange(dataSet);
        }
        
        public void ReadData(byte[] msgPackBytes)
        {
            E[] dataSet = MessagePack.MessagePackSerializer.Deserialize<E[]>(msgPackBytes);
            _data.AddRange(dataSet);
        }

        public async UniTask ReadDataAsync(byte[] msgPackBytes)
        {
            using System.IO.MemoryStream memoryStream = new MemoryStream(msgPackBytes);
            E[] dataSet = await MessagePack.MessagePackSerializer.DeserializeAsync<E[]>(memoryStream);
            _data.AddRange(dataSet);
        }

        public void ReadData(IList<E> listData)
        {
            _data.AddRange(listData);
        }

        public void ReadData(IEnumerable<E> enumerable)
        {
            _data.AddRange(enumerable);
        }
    }
    
    public class SingleData<K, E> where E : IDataKey<K>
    {
        public readonly E DEFAULT = default(E);

        protected Dictionary<K, E> m_dicData;
        public IReadOnlyDictionary<K, E> Dictionary => m_dicData;

        protected DataSet<E> _dataSet;
        public DataSet<E> Set => _dataSet;

        public SingleData()
        {
            m_dicData = new Dictionary<K, E>();
            _dataSet = new();
        }

        public void ReadData(string jsonData)
        {
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate,
            };
            E[] dataSet = Newtonsoft.Json.JsonConvert.DeserializeObject<E[]>(jsonData, settings);
            Ingest(dataSet);
        }

        public async UniTask ReadDataAsync(string jsonData)
        {
            using TextReader textReader = new StringReader(jsonData);
            using Newtonsoft.Json.JsonTextReader jsonTextReader = new Newtonsoft.Json.JsonTextReader(textReader);

            Newtonsoft.Json.JsonSerializer jsonSerializer = new Newtonsoft.Json.JsonSerializer
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate
            };
            E[] dataSet = jsonSerializer.Deserialize<E[]>(jsonTextReader);
            Ingest(dataSet);
        }

        public void ReadData(byte[] msgPackBytes)
        {
            E[] dataSet = MessagePack.MessagePackSerializer.Deserialize<E[]>(msgPackBytes);
            Ingest(dataSet);
        }

        public async UniTask ReadDataAsync(byte[] msgPackBytes)
        {
            using System.IO.MemoryStream memoryStream = new MemoryStream(msgPackBytes);
            E[] dataSet = await MessagePack.MessagePackSerializer.DeserializeAsync<E[]>(memoryStream);
            Ingest(dataSet);
        }

        public void ReadData(IList<E> listData)
        {
            Ingest(listData);
        }

        public void ReadData(IEnumerable<E> enumerable)
        {
            Ingest(enumerable);
        }

        public E Get(in K key, bool suppressWarnings = false)
        {
            E data;
            if (m_dicData.TryGetValue(key, out data))
            {
                return data;
            }
            else
            {
                if (!suppressWarnings) UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' : ", typeof(E), key));
                return default;
            }
        }

        public bool TryGet(in K key, out E data, bool suppressWarnings = false)
        {
            if (m_dicData.TryGetValue(key, out data))
            {
                return true;
            }
            else
            {
                data = default;
                if (!suppressWarnings) UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' : ", typeof(E), key));
                return false;
            }
        }

        public void Traverse(Action<E> action)
        {
            foreach (E e in m_dicData.Values)
            {
                action?.Invoke(e);
            }
        }

        public Dictionary<K, E>.Enumerator GetEnumerator()
        {
            return m_dicData.GetEnumerator();
        }

        private void Ingest(IEnumerable<E> datas)
        {
            foreach (var data in datas)
            {
                try
                {
                    m_dicData.Add(data.Key, data);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e + $", ID : {data.Key}");
                }
            }
            _dataSet.ReadData(datas);
        }
    }
    
    public class SingleData<K1, K2, K, E> : SingleData<K, E> where E : IDataKey<K>
    {
        private System.Func<K1, K2, K> onCalculateKey;

        public SingleData(System.Func<K1, K2, K> onCalculateKey)
        {
            this.onCalculateKey = onCalculateKey;
        }

        public E Get(in K1 k1, in K2 k2, bool suppressWarnings = false)
        {
            E data;
            var key = onCalculateKey(k1, k2);
            if (m_dicData.TryGetValue(key, out data))
            {
                return data;
            }
            else
            {
                if (!suppressWarnings) 
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' '{2}' : ", typeof(E), k1, k2));
                return default;
            }
        }

        public bool TryGet(in K1 k1, in K2 k2, out E data, bool suppressWarnings = false)
        {
            var key = onCalculateKey(k1, k2);
            if (m_dicData.TryGetValue(key, out data))
            {
                return true;
            }
            else
            {
                data = default;
                if (!suppressWarnings) 
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' '{2}' : ", typeof(E), k1, k2));
                return false;
            }
        }
    }
    
    public class SingleData<K1, K2, K3, K, E> : SingleData<K, E> where E : IDataKey<K>
    {
        private System.Func<K1, K2, K3, K> onCalculateKey;

        public SingleData(System.Func<K1, K2, K3, K> onCalculateKey)
        {
            this.onCalculateKey = onCalculateKey;
        }

        public E Get(in K1 k1, in K2 k2, in K3 k3, bool suppressWarnings = false)
        {
            E data;
            var key = onCalculateKey(k1, k2, k3);
            if (m_dicData.TryGetValue(key, out data))
            {
                return data;
            }
            else
            {
                data = default;
                if (!suppressWarnings)
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' '{2}' '{3}' : ", typeof(E), k1, k2, k3));
                return default;
            }
        }

        public bool TryGet(in K1 k1, in K2 k2, in K3 k3, out E data, bool suppressWarnings = false)
        {
            var key = onCalculateKey(k1, k2, k3);
            if (m_dicData.TryGetValue(key, out data))
            {
                return true;
            }
            else
            {
                data = default;
                if (!suppressWarnings)
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' '{2}' '{3}' : ", typeof(E), k1, k2, k3));
                return false;
            }
        }
    }
    
    public class GroupedData<K, E> where E : IDataKey<K>
    {
        public readonly E DEFAULT = default(E);

        protected Dictionary<K, IReadOnlyList<E>> m_dicData;
        public IReadOnlyDictionary<K, IReadOnlyList<E>> Dictionary => m_dicData;
        protected DataSet<E> _dataSet;
        public DataSet<E> Set => _dataSet;

        public GroupedData()
        {
            m_dicData = new Dictionary<K, IReadOnlyList<E>>();
            _dataSet = new();
        }

        public void ReadData(string jsonData)
        {
            Newtonsoft.Json.JsonSerializerSettings settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate,
            };
            E[] dataSet = Newtonsoft.Json.JsonConvert.DeserializeObject<E[]>(jsonData, settings);
            var groups = dataSet.GroupBy(d => d.Key);
            foreach (var group in groups)
            {
                try
                {
                    m_dicData.Add(group.Key, group.ToList());
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e + $", ID : {group.Key}");
                }
            }
            _dataSet.ReadData(dataSet);
        }

        public async UniTask ReadDataAsync(string jsonData)
        {
            using TextReader textReader = new StringReader(jsonData);
            using Newtonsoft.Json.JsonTextReader jsonTextReader = new Newtonsoft.Json.JsonTextReader(textReader);

            Newtonsoft.Json.JsonSerializer jsonSerializer = new Newtonsoft.Json.JsonSerializer();
            jsonSerializer.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None;
            jsonSerializer.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
            jsonSerializer.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate;
            E[] dataSet = jsonSerializer.Deserialize<E[]>(jsonTextReader);
            var groups = dataSet.GroupBy(d => d.Key);
            foreach (var group in groups)
            {
                try
                {
                    m_dicData.Add(group.Key, group.ToList());
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e + $", ID : {group.Key}");
                }
            }
            _dataSet.ReadData(dataSet);
        }

        public void ReadData(byte[] msgPackBytes)
        {
            E[] dataSet = MessagePack.MessagePackSerializer.Deserialize<E[]>(msgPackBytes);
            var groups = dataSet.GroupBy(d => d.Key);
            foreach (var group in groups)
            {
                try
                {
                    m_dicData.Add(group.Key, group.ToList());
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e + $", ID : {group.Key}");
                }
            }
            _dataSet.ReadData(dataSet);
        }

        public async UniTask ReadDataAsync(byte[] msgPackBytes)
        {
            using System.IO.MemoryStream memoryStream = new MemoryStream(msgPackBytes);
            E[] dataSet = await MessagePack.MessagePackSerializer.DeserializeAsync<E[]>(memoryStream);
            var groups = dataSet.GroupBy(d => d.Key);
            foreach (var group in groups)
            {
                try
                {
                    m_dicData.Add(group.Key, group.ToList());
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e + $", ID : {group.Key}");
                }
            }
            _dataSet.ReadData(dataSet);
        }

        public void ReadData(IList<E> listData)
        {
            var groups = listData.GroupBy(d => d.Key);
            foreach (var group in groups)
            {
                try
                {
                    m_dicData.Add(group.Key, group.ToList());
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e + $", ID : {group.Key}");
                }
            }
            _dataSet.ReadData(listData);
        }

        public void ReadData(IEnumerable<E> enumerable)
        {
            var groups = enumerable.GroupBy(d => d.Key);
            foreach (var group in groups)
            {
                try
                {
                    m_dicData.Add(group.Key, group.ToList());
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e + $", ID : {group.Key}");
                }
            }
            _dataSet.ReadData(enumerable);
        }

        public IReadOnlyList<E> Get(in K key, bool suppressWarnings = false)
        {
            IReadOnlyList<E> data;
            if (m_dicData.TryGetValue(key, out data))
            {
                return data;
            }
            else
            {
                data = default;
                if (!suppressWarnings)
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' : ", typeof(E), key));
                return default;
            }
        }

        public bool TryGet(in K key, out IReadOnlyList<E> data, bool suppressWarnings = false)
        {
            if (m_dicData.TryGetValue(key, out data))
            {
                return true;
            }
            else
            {
                data = default;
                if (!suppressWarnings)
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' : ", typeof(E), key));
                return false;
            }
        }

        public void Traverse(Action<E> action)
        {
            foreach (var group in m_dicData.Values)
            {
                foreach (E e in group)
                {
                    action?.Invoke(e);   
                }
            }
        }

        public Dictionary<K, IReadOnlyList<E>>.Enumerator GetEnumerator()
        {
            return m_dicData.GetEnumerator();
        }
    }

    public class GroupedData<K1, K2, K, E> : GroupedData<K, E> where E : IDataKey<K>
    {
        private System.Func<K1, K2, K> onCalculateKey;

        public GroupedData(System.Func<K1, K2, K> onCalculateKey)
        {
            this.onCalculateKey = onCalculateKey;
        }

        public IReadOnlyList<E> Get(in K1 k1, in K2 k2, bool suppressWarnings = false)
        {
            IReadOnlyList<E> data;
            var key = onCalculateKey(k1, k2);
            if (m_dicData.TryGetValue(key, out data))
            {
                return data;
            }
            else
            {
                if (!suppressWarnings)
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' '{2}' : ", typeof(E), k1, k2));
                return default;
            }
        }

        public bool TryGet(in K1 k1, in K2 k2, out IReadOnlyList<E> data, bool suppressWarnings = false)
        {
            var key = onCalculateKey(k1, k2);
            if (m_dicData.TryGetValue(key, out data))
            {
                return true;
            }
            else
            {
                data = default;
                if (!suppressWarnings)
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' '{2}' : ", typeof(E), k1, k2));
                return false;
            }
        }
    }

    public class GroupedData<K1, K2, K3, K, E> : GroupedData<K, E> where E : IDataKey<K>
    {
        private System.Func<K1, K2, K3, K> onCalculateKey;

        public GroupedData(System.Func<K1, K2, K3, K> onCalculateKey)
        {
            this.onCalculateKey = onCalculateKey;
        }

        public IReadOnlyList<E> Get(in K1 k1, in K2 k2, in K3 k3, bool suppressWarnings = false)
        {
            IReadOnlyList<E> data;
            var key = onCalculateKey(k1, k2, k3);
            if (m_dicData.TryGetValue(key, out data))
            {
                return data;
            }
            else
            {
                data = default;
                if (!suppressWarnings)
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' '{2}' '{3}' : ", typeof(E), k1, k2, k3));
                return default;
            }
        }

        public bool TryGet(in K1 k1, in K2 k2, in K3 k3, out IReadOnlyList<E> data, bool suppressWarnings = false)
        {
            var key = onCalculateKey(k1, k2, k3);
            if (m_dicData.TryGetValue(key, out data))
            {
                return true;
            }
            else
            {
                data = default;
                if (!suppressWarnings)
                    UnityEngine.Debug.LogError(string.Format("Data Not Exist {0} '{1}' '{2}' '{3}' : ", typeof(E), k1, k2, k3));
                return false;
            }
        }
    }
}