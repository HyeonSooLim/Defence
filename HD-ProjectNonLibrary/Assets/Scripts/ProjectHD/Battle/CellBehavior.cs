using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Battle
{
    public class CellBehavior : MonoBehaviour
    {
        public int Index => _index;
        public int Q => _q;
        public int R => _r;
        public bool IsCharacterHere => _isCharacterHere;

        [SerializeField] private int _index;
        [SerializeField] private bool _isCharacterHere;
        [SerializeField] private int _q;
        [SerializeField] private int _r;

        public void SetIndex(int index)
        {
            _index = index;
        }

        public void SetCharacterHere(bool isCharacterHere)
        {
            _isCharacterHere = isCharacterHere;
        }

        public void SetHex(int q, int r)
        {
            _q = q;
            _r = r;
        }
    }
}