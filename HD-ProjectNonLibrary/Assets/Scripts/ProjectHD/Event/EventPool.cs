using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Event
{
    /// <summary>
    /// Events에 Static으로 새로 만들 거나 EventPool에서 가져오거나 선택(반환 필수)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class EventPool<T> where T : GameEvent, new()
    {
        private static readonly Queue<T> pool = new();

        public static T GetEvent()
        {
#if UNITY_EDITOR
            Debug.Log($"{typeof(T).Name}");
#endif
            // pool에서 이벤트를 가져오거나 새로운 T 타입의 인스턴스를 생성
            return pool.Count > 0 ? pool.Dequeue() : new T();
        }

        public static void ReturnEvent(T evt)
        {
            // 이벤트를 pool에 반환
            evt.Reset(); // 이벤트 상태 초기화
            pool.Enqueue(evt);
        }

        public static void ReleaseEvent()
        {
            // pool을 비움
            pool.Clear();
        }
    }
}