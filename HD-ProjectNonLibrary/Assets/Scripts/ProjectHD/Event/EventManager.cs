using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Event
{
    public abstract class GameEvent
    {
        public abstract void Reset();
        public bool IsSuccess;
    } // 기본 게임 이벤트 클래스

    public static class EventManager
    {
        // s_Events: 타입으로 액션 관리. 특정 타입의 이벤트가 발생했을 때 실행할 액션 관리
        // s_EventLookups: 델리게이트로 액션 관리. 특정 델리게이트(리스너)의 추가, 제거 및 호출할 액션 관리
        static readonly Dictionary<Type, Action<GameEvent>> s_Events = new Dictionary<Type, Action<GameEvent>>();
        static readonly Dictionary<Delegate, Action<GameEvent>> s_EventLookups = new Dictionary<Delegate, Action<GameEvent>>();

        public static void AddListener<T>(Action<T> evt) where T : GameEvent
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt), "Listener cannot be null");
            }

            if (!s_EventLookups.ContainsKey(evt))
            {
                Action<GameEvent> newAction = (e) => evt((T)e); // 새로운 액션 생성
                s_EventLookups[evt] = newAction; // 델리게이트와 액션을 딕셔너리에 추가

                if (s_Events.TryGetValue(typeof(T), out Action<GameEvent> internalAction))
                    s_Events[typeof(T)] = internalAction += newAction; // 기존 액션에 새로운 액션 추가
                else
                    s_Events[typeof(T)] = newAction; // 새로운 타입과 액션 추가
            }
            else
            {
                Debug.LogWarning("Listener is already registered for this event.");
            }
        }

        public static void RemoveListener<T>(Action<T> evt) where T : GameEvent
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt), "Listener cannot be null");
            }

            if (s_EventLookups.TryGetValue(evt, out var action))
            {
                if (s_Events.TryGetValue(typeof(T), out var tempAction))
                {
                    tempAction -= action; // 액션 제거
                    if (tempAction == null)
                        s_Events.Remove(typeof(T)); // 액션이 없으면 타입 제거
                    else
                        s_Events[typeof(T)] = tempAction; // 업데이트된 액션 저장
                }

                s_EventLookups.Remove(evt); // 델리게이트 제거
            }
            else
            {
                Debug.LogWarning("Listener does not exist.");
            }
        }

        public static void Broadcast(GameEvent evt)
        {
            if (s_Events.TryGetValue(evt.GetType(), out var action))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                try
                {
                    action.Invoke(evt); // 이벤트 타입에 해당하는 액션 호출
                    evt.Reset();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Exception during {evt.GetType()} broadcast: {ex.Message} StackTrace: {ex.StackTrace}");
                }
#else
                action.Invoke(evt); // 릴리즈 빌드에서는 성능 우선
                evt.Reset();
#endif
            }
        }

        public static void Broadcast(GameEvent evt, System.Action<GameEvent> callback)
        {
            if (s_Events.TryGetValue(evt.GetType(), out var action))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                try
                {
                    action.Invoke(evt); // 이벤트 타입에 해당하는 액션 호출
                    callback?.Invoke(evt); // 추가 콜백 호출
                    evt.Reset();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Exception during {evt.GetType()} broadcast: {ex.Message} StackTrace: {ex.StackTrace}");
                }
#else
                action.Invoke(evt); // 이벤트 타입에 해당하는 액션 호출
                callback?.Invoke(evt); // 추가 콜백 호출
                evt.Reset();
#endif
            }
        }

        public static void Clear()
        {
            s_Events.Clear();
            s_EventLookups.Clear();
        }
    }
}
