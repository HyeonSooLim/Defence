using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD
{
    public class TitleManager : BaseManager<TitleManager>
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private ProjectEnum.SceneName _nextScene = ProjectEnum.SceneName.BattleWorkSpace;

        private void Start()
        {
            _startButton.onClick.AddListener(() => MoveToOherScene(_nextScene, CleanUp()));
            base.Initialize().Forget();
        }

        public async UniTask CleanUp()
        {
            base.DeInitialize().Forget();
            _startButton.onClick.RemoveAllListeners();
            DG.Tweening.DOTween.CompleteAll();
            await UniTask.DelayFrame(1);
            DG.Tweening.DOTween.KillAll();
        }
    }
}
