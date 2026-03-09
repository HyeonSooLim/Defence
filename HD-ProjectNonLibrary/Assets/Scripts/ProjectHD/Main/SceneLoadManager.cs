using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectHD
{
    public class SceneLoadManager : Singleton<SceneLoadManager>
    {
        //public static SceneLoadManager Instance; // 싱글톤 인스턴스
        private ISceneLoader _sceneLoader; // 씬 로더 인터페이스

        // Odin 인스펙터를 활용하여 인스펙터에서 씬 로더 선택 가능하도록 구현(의존성 주입)
        [SerializeField, ValueDropdown("GetLoader", DropdownTitle = "Select Scene Object", IsUniqueList = true,
            DrawDropdownForListElements = false, ExcludeExistingValuesInList = true)]
        private string _selectedLoader;

        [SerializeField]
        ProjectEnum.SceneName _firstNextScene = ProjectEnum.SceneName.TitleWorkSpace;

#if UNITY_EDITOR
        public IEnumerable<ValueDropdownItem<string>> GetLoader()
        {
            var isceneloaders = typeof(ISceneLoader).Assembly.GetTypes().
                Where(t => t.GetInterfaces().Contains(typeof(ISceneLoader)));
            return isceneloaders.Select(t => new ValueDropdownItem<string>(t.Name, t.Name));
        }
#endif

        private async UniTask Awake()
        {
            if (_selectedLoader.IsNullOrEmpty())
            {
                Utilities.InternalDebug.LogError("LoaderName is null or empty.");
                return;
            }
            //_sceneLoader = System.Activator.CreateInstance(System.Type.GetType(_selectedLoader)) as ISceneLoader; // 씬 로더 인스턴스 생성
            var typeName = $"ProjectHD.{_selectedLoader}, Assembly-CSharp";    // asmdef를 사용했다면 어셈블리 네임 변경
            var type = System.Type.GetType(typeName);
            _sceneLoader = System.Activator.CreateInstance(type) as ISceneLoader;
            _sceneLoader.SetEvent();

            ApplicationSettings();
            await MainManager.Instance.AsyncStart(); // 메인 매니저 초기화
            await UniTask.Yield(); // 프레임 대기

            // 리소스 데이터 다운로드 후에 실행해야 함
            await MainManager.Instance.AfterResourceDownload(); // 아틀라스 로더 초기화
            await Global.DataManager.ReadDataAsync();
            await UniTask.Yield(); // 프레임 대기

            MoveToScene(_firstNextScene, UniTask.Defer(CleanUp)); // 타이틀 씬으로 이동
        }

        private void OnDestroy()
        {
            _sceneLoader?.RemoveEvent();
            _selectedLoader = null;
            Utilities.InternalDebug.Log("SceneLoadManager Destroyed");
        }

        /// <summary>
        /// Clean up the current scene and move to the specified scene.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="cleanUpAction">씬 이동 전 현재 씬에서 정리해야할 일</param>
        /// <param name="afterSceneLoadAction">씬 이동 후 새 씬에서 실행할 일</param>
        public void MoveToScene(ProjectEnum.SceneName sceneName, UniTask cleanUp)
        {
            _sceneLoader.MoveToScene(sceneName, cleanUp); // 씬 로드
        }

        private void PlayerSetting()
        {
            Utilities.InternalDebug.Log("플레이어 셋팅");
        }

        private void GameOptionSetting()
        {
            //Input.multiTouchEnabled = false; // 멀티 터치 비활성화
            Application.targetFrameRate = 60; // 프레임 제한
            Application.runInBackground = true; // 백그라운드 실행 허용
            Application.backgroundLoadingPriority = ThreadPriority.Low; // 백그라운드 로딩 우선순위 설정
        }

        private void ApplicationSettings()
        {
            GameOptionSetting();
            PlayerSetting();
        }

        public async UniTask CleanUp()
        {
            DG.Tweening.DOTween.CompleteAll();
            await UniTask.DelayFrame(1);
            DG.Tweening.DOTween.KillAll();
            await MainManager.Instance.CleanUp(); // 리소스 정리
            await UniTask.Yield();
        }
    }
}
