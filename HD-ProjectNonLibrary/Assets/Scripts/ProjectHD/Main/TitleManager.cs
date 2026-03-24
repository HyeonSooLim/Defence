using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD
{
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private ProjectEnum.SceneName _nextScene = ProjectEnum.SceneName.BattleWorkSpace;

        private void Start()
        {
            _startButton.onClick.AddListener(MoveToScene);
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(MoveToScene);
        }

        private void MoveToScene()
        {
            MainManager.Instance.MoveToOherScene(_nextScene, CleanUp());
        }

        public async UniTask CleanUp()
        {
            DG.Tweening.DOTween.CompleteAll();
            await UniTask.DelayFrame(1);
            DG.Tweening.DOTween.KillAll();
        }
    }
}
