
using System.Collections.Generic;

namespace UITool { 
    public class CustomListPool<T> where T : class, new()
    {
        public delegate void CustomListPoolReuse(T item);
        public delegate void CustomListPoolUnuse(T item);
        public CustomListPoolReuse Reuse { get; protected set; } = null;
        public CustomListPoolUnuse Unuse { get; protected set; } = null;
        public List<T> datas { get; private set; } = null;

        public CustomListPool()
        {
            Initialize();
        }
        public CustomListPool(CustomListPoolReuse reuse, CustomListPoolUnuse unuse)
        {
            Initialize();
            Reuse = reuse;
            Unuse = unuse;
        }
        /// <summary>
        /// 현재 Pool에 확보된 수량
        /// </summary>
        public int Count
        {
            get
            {
                if (datas == null)
                    return 0;
                return datas.Count;
            }
        }
        protected void Initialize()
        {
            Reuse = null;
            Unuse = null;
            Clear();
        }
        /// <summary>
        /// Pool에 사용완료한 아이템 넣기
        /// </summary>
        /// <param name="item">T</param>
        public void Put(T item)
        {
            if (datas == null)
                datas = new List<T>();

            if (!datas.Contains(item))
            {
                Unuse?.Invoke(item);
                datas.Add(item);
            }
        }
        /// <summary>
        /// Pool 정리
        /// </summary>
        public void Clear()
        {
            if (datas == null)
                datas = new List<T>();
            else
                datas.Clear();
        }
        /// <summary>
        /// 풀에 확보된 내용중 첫번째 가져오기
        /// </summary>
        /// <returns>확보된 T</returns>
        public T Get()
        {
            var last = Count - 1;
            if (last < 0)
                return null;

            // Pop the last object in pool
            var item = datas[last];
            datas.RemoveAt(last);

            Reuse?.Invoke(item);

            return item;
        }
        /// <summary>
        /// T 필요 확보 수량 만큼 만들기
        /// </summary>
        /// <param name="count">확보 수량</param>
        public void Spawn(int count)
        {
            while (Count < count)
            {
                Put(new T());
            }
        }
        public void SetReuse(CustomListPoolReuse reuse)
        {
            Reuse = reuse;
        }
        public void SetUnuse(CustomListPoolUnuse unuse)
        {
            Unuse = unuse;
        }
    }
}