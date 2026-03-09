using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Utilities
{
    static class StaticObjectPool
    {
        private static class Pool<E> where E : class
        {
            //private static readonly Stack<E> pool = new Stack<E>(10);
            private static Lazy<Stack<E>> _lazyLargeObject = new Lazy<Stack<E>>();
            private static Stack<E> _pool => _lazyLargeObject.Value;

            public static int Lent { get; private set; }
            public static int Return { get; private set; }
            
            static Pool()
            {
                
            }

            public static void Push(E obj)
            {
                if (obj == null) return;
                if (typeof(E).IsInterface) throw new Exception("Interface!!");
                if (typeof(E).IsAbstract) throw new Exception("Abstract!!");
                
                lock (_pool)
                {
                    ++Return;
                    //Debug.Log($"Push {typeof(E).Name} {Pool<E>.Lent - Pool<E>.Return}");
                    _pool.Push(obj);
                }
            }

            public static E Pop()
            {
                if (typeof(E).IsInterface) throw new Exception("Interface!!");
                if (typeof(E).IsAbstract) throw new Exception("Abstract!!");

                lock (_pool)
                {
                    ++Lent;
                    //Debug.Log($"Pop {typeof(E).Name} {Pool<E>.Lent - Pool<E>.Return}");
                    return _pool.Count > 0 ? _pool.Pop() : Activator.CreateInstance<E>();
                }
            }
        }

        private static HashSet<Type> _usedTypes = new();
        public static ICollection<Type> UsedTypes => _usedTypes;

        public static void Push<E>(E obj) where E : class
        {
            _usedTypes.Add(typeof(E));
            Pool<E>.Push(obj);
        }

        public static E Pop<E>() where E : class
        {
            _usedTypes.Add(typeof(E));
            return Pool<E>.Pop();
        }

        //블록 탈출전에 객체 리턴을 보장.
        public ref struct RAII<E> where E : class
        {
            private E _data;

            public RAII(out E outData)
            {
                _data = Pop<E>();
                outData = _data;
            }
            public void Dispose()
            {
                Push(_data);
                _data = null;
                //InternalDebug.Log(typeof(E).Name);
            }
        }
    }
}