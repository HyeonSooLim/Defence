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

        private void MoveToScene()
        {
            SceneLoadManager.Instance.MoveToScene(_nextScene, CleanUp());
        }

        public async UniTask CleanUp()
        {
            _startButton.onClick.RemoveListener(MoveToScene);
            DG.Tweening.DOTween.CompleteAll();
            await UniTask.DelayFrame(1);
            DG.Tweening.DOTween.KillAll();
        }
    }
}
