using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD.UI
{

    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private Button _restartButton;
        public Button RestartButton => _restartButton;

        private void OnDestroy()
        {
            _restartButton.onClick.RemoveAllListeners();
        }
    }
}
