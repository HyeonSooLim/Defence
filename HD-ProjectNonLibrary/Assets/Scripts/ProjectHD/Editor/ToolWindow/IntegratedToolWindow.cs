using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using EditorUtility = UnityEditor.EditorUtility;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UIElements.Toggle;

namespace ProjectHD.Editor
{
    public class IntegratedToolWindow : EditorWindow
    {
        enum DrawForm
        {
            AutomationBuild,
            SheetToJsonTool,
        }

        enum LogType
        {
            Log,
            Warning,
            Error,
        }

        private string[] ButtonNames = new[] { "빌드 자동화", "데이터 시트 자동화 v2" };

        public const string EDITOR_INTEGRATEDTOOL_WINDOW_MENUITEM = "Tools/종합 툴";
        public const string AUTO_REMOTE_LABEL_NAME = "Remote";
        public const string AUTO_JSON_LABEL_NAME = "JsonData";
        public const string AUTO_BYTE_LABEL_NAME = "MessagePackData";
        public const string LogPath = "\\CustomLogs";
        public const string IncorrectAddressablePath = "\\IncorrectAddressablePathLog.txt";
        public static IntegratedToolWindow WND { get; private set; }

        private AutomationAddressableSetting _automationAddressableSetting;
        public static IntegratedToolSetting _integratedToolSetting;
        private static SheetImporterSettings _sheetImporterSettings;

        private StringBuilder masterSb = new StringBuilder();
        [SerializeField] private List<Object> _incorrectAddressablePathObjects = new List<Object>();


        private DrawForm _curDrawForm = DrawForm.AutomationBuild;
        private Button _currentActiveButton;
        private VisualElement _rightPanel;
        private ListView _eventListView;

        private Color _activeButtonColor = new Color(0.2f, 0.2f, 0.2f);
        private Color _inactiveButtonColor = new Color(0.5f, 0.5f, 0.5f);
        private bool _isWorking = false;

        //Sheet Tool
        private ListView _sheetListView;

        private List<SheetPathEditorData> _sheetLocalPathData = new List<SheetPathEditorData>();

        private ScrollView _scrollView;

        public class SheetPathEditorData
        {
            public string Name = "시트 이름";
            public string Address = "";
            public bool isImport = false;
        }

        System.Diagnostics.ProcessStartInfo _processStartInfo = new();   // 윈도우 탐색기 열기용

        [MenuItem(EDITOR_INTEGRATEDTOOL_WINDOW_MENUITEM)]
        public static void ShowWindow()
        {
            WND = GetWindow<IntegratedToolWindow>();
            WND.titleContent = new GUIContent(EditorConst.EDITOR_INTERGRATED_WINDOW_NAME);
            WND.position = new Rect(100, 100, 800, 600);
        }

        public void CreateGUI()
        {
            var splitView = new TwoPaneSplitView(0, 150, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            _automationAddressableSetting = AssetDatabase.LoadAssetAtPath<AutomationAddressableSetting>(
                EditorPath.EDITOR_AUTOADDRESSABLE_DEFAULTSETTING_PATH);
            _integratedToolSetting = AssetDatabase.LoadAssetAtPath<IntegratedToolSetting>(
                EditorPath.EDITOR_INTERGRATEDTOOLSETTING_PATH);
            _sheetImporterSettings = AssetDatabase.LoadAssetAtPath<SheetImporterSettings>(EditorPath.EDITOR_SheetImporter_PATH);

            DrawLeftList();

            splitView.Add(_eventListView);
            _rightPanel = new VisualElement();
            splitView.Add(_rightPanel);

            DrawRightPanel();
        }

        #region DrawEditorUI

        private void DrawLeftList()
        {
            _eventListView = new ListView();
            Func<VisualElement> makeItem = () => new Button();
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                var tempButton = (e as Button);

                if (i == UnsafeUtility.EnumToInt(_curDrawForm))
                {
                    _currentActiveButton = tempButton;
                    _currentActiveButton.style.backgroundColor = _activeButtonColor;
                }
                else
                {
                    tempButton.style.backgroundColor = _inactiveButtonColor;
                }

                tempButton.clicked += () =>
                {
                    _currentActiveButton.style.backgroundColor = _inactiveButtonColor;
                    _currentActiveButton = tempButton;
                    _currentActiveButton.style.backgroundColor = _activeButtonColor;
                    _curDrawForm = (DrawForm)i;
                    DrawRightPanel();
                };
                tempButton.text = ButtonNames[i];
            };

            _eventListView.makeItem = makeItem;
            _eventListView.bindItem = bindItem;
            _eventListView.fixedItemHeight = 45;
            _eventListView.itemsSource = ButtonNames;
        }

        public void Refresh()
        {
            DrawRightPanel();
        }

        private void DrawRightPanel()
        {
            _rightPanel.Clear();
            _isWorking = false;
            switch (_curDrawForm)
            {
                case DrawForm.AutomationBuild:
                    DrawAutomationBuildPanel();
                    break;
                case DrawForm.SheetToJsonTool:
                    DrawControlDataSheet2Json();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void DrawAutomationBuildPanel()
        {
            VisualElement Panel = new();

            _scrollView = new ScrollView();
            Panel.Add(_scrollView);
            _scrollView.Clear();

            _rightPanel.Add(Panel);

            CreateTextField(_scrollView, DeviceRepositoryKey.Editor_Build_Version, _integratedToolSetting.liveVersion, "버전");
            CreateTextField(_scrollView, DeviceRepositoryKey.Editor_Build_BuildOutputPath, _integratedToolSetting.buildPath, "빌드 output 경로");
            CreateTextField(_scrollView, DeviceRepositoryKey.Editor_Build_BuildName, _integratedToolSetting.fileName, "어플 이름");
            CreateToggle(_scrollView, DeviceRepositoryKey.Editor_Build_DevMode, _integratedToolSetting.useDevBuild, "개발자 빌드");
            CreateToggle(_scrollView, DeviceRepositoryKey.Editor_Build_CleanBuild, _integratedToolSetting.cleanBuild, "클린 빌드");

            var symbolDefineToggle = UtilityUIElement.CreateToggle(DefineSymbolEditor.IsSymbolAlreadyDefined("QA_BUILD"), "QA 빌드", evt =>
            {
                if (evt.newValue)
                    DefineSymbolEditor.AddDefineSymbol("QA_BUILD");
                else
                    DefineSymbolEditor.RemoveDefineSymbol("QA_BUILD");
            });
            _scrollView.Add(symbolDefineToggle);

#if UNITY_EDITOR
            CreateIntField(_scrollView, DeviceRepositoryKey.Editor_Build_BundleNumber, _integratedToolSetting.bundleNumber, "번들버전");
            CreateToggle(_scrollView, DeviceRepositoryKey.Editor_Build_AppBundle, _integratedToolSetting.appBundle, "앱번들");

            var tempMarketType = (ProjectEnum.PlatformMarketType)Enum.Parse(typeof(ProjectEnum.PlatformMarketType),
                DeviceRepository.LoadKeyForString(DeviceRepositoryKey.Editor_Build_MarketType,
                    _integratedToolSetting.marketType.ToString()));

            var currentPlatform = Application.dataPath;
            if (currentPlatform.Contains("PlayStore"))
                tempMarketType = ProjectEnum.PlatformMarketType.PlayStore;
            else if (currentPlatform.Contains("OneStore"))
                tempMarketType = ProjectEnum.PlatformMarketType.OneStore;
            else
                currentPlatform = string.Empty;
#if UNITY_IOS
                tempMarketType = Define.PlatformMarketType.AppStore;
#endif
            if (!string.IsNullOrEmpty(currentPlatform))
            {
                DeviceRepository.SaveKeyForString(DeviceRepositoryKey.Editor_Build_MarketType, tempMarketType.ToString());
            }

            var marketTypeEnumField = UtilityUIElement.CreateEnumField(tempMarketType, "마켓 스토어", evt =>
            {
                DeviceRepository.SaveKeyForString(DeviceRepositoryKey.Editor_Build_MarketType, evt.newValue.ToString());
            });
            _scrollView.Add(marketTypeEnumField);
#endif

            CreateButton(_scrollView, "어드레서블 빌드(리소스)", OnClickStartResourceBuild);
#if !UNITY_IOS
            Label label = new("어드레서블 빌드(리소스) 후에 빌드를 해주세요.");
            Label label2 = new("----------------------------------------------------------------------------------");
            label.style.alignSelf = Align.Center;
            label2.style.alignSelf = Align.Center;
            _scrollView.Add(label);
            _scrollView.Add(label2);

            CreateButton(_scrollView, "빌드 시작", OnClickStartBuild);
            CreateButton(_scrollView, "리소스와 빌드같이 뽑기", OnClickBuildAndResource);
#endif
        }

        private static void CreateButton(VisualElement panel, string label, Action action)
        {
            panel.Add(UtilityUIElement.CreateButton(label, action));
        }

        private void OnClickBuildAndResource()
        {
            var tempBuildVersion = DeviceRepository.LoadKeyForString(DeviceRepositoryKey.Editor_Build_Version, _integratedToolSetting.liveVersion);
            Builder.BuildAddressables(tempBuildVersion);
            Build(tempBuildVersion);
        }

        private void OnClickStartResourceBuild()
        {
            var tempBuildVersion = DeviceRepository.LoadKeyForString(DeviceRepositoryKey.Editor_Build_Version, _integratedToolSetting.liveVersion);
            Builder.BuildAddressables(tempBuildVersion);
        }

        private void OnClickStartBuild()
        {
            var tempBuildVersion = DeviceRepository.LoadKeyForString(DeviceRepositoryKey.Editor_Build_Version, _integratedToolSetting.liveVersion);
            Build(tempBuildVersion);
        }

        private void CreateToggle(VisualElement panel, DeviceRepositoryKey keyType, bool defaultVal, string labelName)
        {
            var devMode = DeviceRepository.LoadKeyForBoolean(keyType, defaultVal);
            var devToggleMode = UtilityUIElement.CreateToggle(devMode, labelName, evt
                => DeviceRepository.SaveKeyForBoolean(keyType, evt.newValue));
            panel.Add(devToggleMode);
        }

        private void CreateTextField(VisualElement panel, DeviceRepositoryKey keyType, string defaultVal, string labelName, out TextField textField)
        {
            var keyValue = DeviceRepository.LoadKeyForString(keyType, defaultVal);
            textField = UtilityUIElement.CreateTextField(keyValue, labelName, evt
                => DeviceRepository.SaveKeyForString(keyType, evt.newValue));
            panel.Add(textField);
        }

        private static string CreateTextField(VisualElement leftPanel, string stringValue, string labelName)
        {
            leftPanel.Add(UtilityUIElement.CreateTextField(stringValue, labelName,
                _ => stringValue = _.newValue));
            return stringValue;
        }

        private void CreateTextField(VisualElement panel, DeviceRepositoryKey keyType, string defaultVal, string labelName)
        {
            var keyValue = DeviceRepository.LoadKeyForString(keyType, defaultVal);
            var textField = UtilityUIElement.CreateTextField(keyValue, labelName, evt
                => DeviceRepository.SaveKeyForString(keyType, evt.newValue));
            panel.Add(textField);
        }

        private void CreateIntField(VisualElement panel, DeviceRepositoryKey keyType, int defaultVal, string labelName)
        {
            var ftpURL = DeviceRepository.LoadKeyForInt(keyType, defaultVal);
            var ftpURLTextField = UtilityUIElement.CreateIntegerField(ftpURL, labelName, evt
                => DeviceRepository.SaveKeyForInt(keyType, evt.newValue));
            panel.Add(ftpURLTextField);
        }

        private void Build(string tempBuildVersion)
        {
            var tempbundleNumber =
                DeviceRepository.LoadKeyForInt(DeviceRepositoryKey.Editor_Build_BundleNumber, _integratedToolSetting.bundleNumber);
            var tempbuildPath =
                DeviceRepository.LoadKeyForString(DeviceRepositoryKey.Editor_Build_BuildOutputPath, _integratedToolSetting.buildPath);
            var tempbuildName =
                DeviceRepository.LoadKeyForString(DeviceRepositoryKey.Editor_Build_BuildName, _integratedToolSetting.fileName);
            var tempdevMode =
                DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_Build_DevMode, _integratedToolSetting.useDevBuild);
            var tempcleanBuild =
                DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_Build_CleanBuild, _integratedToolSetting.cleanBuild);
            var tempappBundle =
                DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_Build_AppBundle, _integratedToolSetting.appBundle);
            var tempMarketType = (ProjectEnum.PlatformMarketType)Enum.Parse(typeof(ProjectEnum.PlatformMarketType),
                DeviceRepository.LoadKeyForString(DeviceRepositoryKey.Editor_Build_MarketType,
                    _integratedToolSetting.marketType.ToString()));
#if UNITY_ANDROID
            Builder.BuildAOS(tempBuildVersion,
                tempbundleNumber,
                tempbuildPath,
                tempbuildName,
                tempdevMode,
                tempcleanBuild,
                tempappBundle,
                tempMarketType);
#elif UNITY_IOS
                Builder.BuildIOS(tempBuildVersion,
                    tempbuildPath,
                    tempbuildName,
                    tempdevMode,
                    tempcleanBuild);
#endif
        }

        private void DrawAutomationAddressablePanel()
        {
            var targetPanel = _rightPanel;

            VisualElement Panel = new();
            _scrollView = new ScrollView();
            _scrollView.Clear();
            Panel.Add(_scrollView);
            targetPanel.Add(Panel);

            if (_incorrectAddressablePathObjects.Count > 0)
            {
                var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
                _rightPanel.Add(splitView);


                var automationLeftPanel = new VisualElement();
                splitView.Add(automationLeftPanel);
                var automationRightPanel = new VisualElement();
                splitView.Add(automationRightPanel);
                targetPanel = automationLeftPanel;


                CreateButton(automationRightPanel, "클리어", () =>
                {
                    _incorrectAddressablePathObjects.Clear();
                    Refresh();
                });

                var propertyField = UtilityUIElement.CreateAssetRefField(this,
                    "어드레서블 레이블에 Remote추가",
                    "_incorrectAddressablePathObjects");
                automationRightPanel.Add(propertyField);
            }


            var autoAddressableSetting = UtilityUIElement.CreateObjectField(_automationAddressableSetting, "자동화 세팅", _ =>
            {
                _automationAddressableSetting = (AutomationAddressableSetting)_.newValue;
            });
            _scrollView.Add(autoAddressableSetting);

            CreateToggle(_scrollView, DeviceRepositoryKey.Editor_AutoAddressable_Grouping, true, "어드레서블 그룹 자동 설정");
            CreateToggle(_scrollView, DeviceRepositoryKey.Editor_AutoAddressable_Addressable, true, "어드레서블 자동 설정");
            CreateToggle(_scrollView, DeviceRepositoryKey.Editor_AutoAddressable_Label, true, "어드레서블 레이블 설정");
            CreateToggle(_scrollView, DeviceRepositoryKey.Editor_AutoAddressable_Schema, true, "어드레서블 스키마 설정");
            CreateButton(_scrollView, "어드레서블 자동화", OnClickAutoAddressable);
            CreateButton(_scrollView, "어드레서블 그룹 정렬", OnClickAutoAddressableOrder);
            CreateButton(_scrollView, "어드레서블 레이블에 Remote추가", OnClickAddRemoteLabelToAddressable);
            CreateButton(_scrollView, "모든 Remote레이블 어드레서블 그룹에 CacheClearBehavior 설정", OnClickSetRemoteLabelCacheClearBehavior);
        }

        private void OnClickSetRemoteLabelCacheClearBehavior()
        {
            AddressableHelper.ApplyCacheClearBehaviorToRemoteGroups(AUTO_REMOTE_LABEL_NAME);
        }

        private void OnClickAddRemoteLabelToAddressable()
        {
            var temp = _automationAddressableSetting.AutoAddressGroupList;
            foreach (var group in _automationAddressableSetting.AddressableAssetSetting.groups)
            {
                if (group == null)
                    continue;
                if (!temp.Contains(group.name))
                    continue;

                foreach (var entry in group.entries)
                    if (!entry.labels.Contains(AUTO_REMOTE_LABEL_NAME))
                        entry.SetLabel(AUTO_REMOTE_LABEL_NAME, true);
            }
        }

        private void OnClickAutoAddressableOrder()
        {
            _automationAddressableSetting.CustomPathData.Sort((x, y) =>
            {
                return x.GroupName.CompareTo(y.GroupName);
            });
            EditorUtility.SetDirty(_automationAddressableSetting);
            AssetDatabase.SaveAssets();
            Refresh();
        }

        private void OnClickAutoAddressable()
        {
            //_automationAddressableSetting.DigimonDataDict.Clear();
            _automationAddressableSetting.CustomDataDict.Clear();
            _incorrectAddressablePathObjects.Clear();
            //AutoAddressableForDigimon();
            AutoAddressableForCustom();
            CreateLogfile();

            Refresh();
            CleanUpMemoryAsync().Forget();
        }

        private void CreateLogfile()
        {
            CreateLogFile_IncorrectAddressablePath();
        }

        private void CreateLogFile_IncorrectAddressablePath()
        {
            var stringBuild = new StringBuilder();
            for (int i = 0; i < _incorrectAddressablePathObjects.Count; i++)
            {
                var addressableAssetPath = _incorrectAddressablePathObjects[i].GetAddressableAssetPath();
                var assetPath = AssetDatabase.GetAssetPath(_incorrectAddressablePathObjects[i]);
                stringBuild.AppendLine($"파일 이름: {_incorrectAddressablePathObjects[i].name}");
                stringBuild.AppendLine($"파일 경로: {assetPath}");
                stringBuild.AppendLine($"어드 경로: {addressableAssetPath}");
                stringBuild.AppendLine($"");
            }

            // "Assets" 폴더의 상위 폴더 경로를 가져오기
            string parentDirectory = Path.GetDirectoryName(Application.dataPath);

            // 상위 폴더 경로를 상대 경로로 변환하기
            string relativePath = parentDirectory.Replace(Application.dataPath, "Assets");

            Debug.Log("상대 경로: " + relativePath);

            var filePath = _integratedToolSetting.incorrectAddressablePath == "" ? relativePath : relativePath;
            filePath += LogPath;
            Directory.CreateDirectory(filePath);
            filePath += IncorrectAddressablePath;
            // 파일이 존재하는지 확인
            if (!File.Exists(filePath))
            {
                // 파일이 없으면 새로운 파일 생성 후 텍스트 추가
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    // 텍스트 추가
                    // File.WriteAllText(@filePath, stringBuild.ToString());
                    sw.Write(stringBuild.ToString());
                }

                Debug.Log($"{filePath}에 새로운 로그파일이 생성되었습니다.");
            }
            else
            {
                // 파일이 이미 존재하는 경우에는 기존 파일에 텍스트 추가
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    // 텍스트 추가
                    sw.Write(stringBuild.ToString());
                }

                Debug.Log($"{filePath}에 새로운 로그파일이 생성되었습니다.");
            }
        }

        public static readonly string ApplicationConfigURL = "https://dsc-cdn.gameking.com/dsc4/ko/application_config1.txt";

        #endregion

        #region DataSheetController

        private void DrawControlDataSheet2Json()
        {
            _rightPanel.Clear();

            UIToolkit.SplitView splitView = new UIToolkit.SplitView();
            VisualElement leftPanel = new();
            VisualElement rightPanel = new();
            splitView.Add(leftPanel);
            splitView.Add(rightPanel);
            splitView.fixedPaneInitialDimension = 400;
            _rightPanel.Add(splitView);

            var AssetsDefaultPath = "HD-ProjectNonLibrary/Assets";
            var excelfolderName = "Excel";

            leftPanel.Add(UtilityUIElement.CreateLabel("\n개인 pc의 엑셀 파일을 json으로 변경시켜 테스트 하기 위한 툴입니다.\n\n엑셀 파일이 있는 경로를 가르켜야만 우측 목록이 활성화됩니다.\n"));
            var localPath = Application.dataPath.Replace(AssetsDefaultPath, excelfolderName);
            _sheetImporterSettings.localExcelPath = localPath;
            var pathField = UtilityUIElement.CreateTextField(_sheetImporterSettings.localExcelPath, "로컬 저장소 시트 경로", _ => {
                _sheetImporterSettings.localExcelPath = _.newValue;
                ReadExcelListFromLocal();
            });

            leftPanel.Add(pathField);

            _sheetImporterSettings.jsonPath = CreateTextField(leftPanel, _sheetImporterSettings.jsonPath, "Json 경로");
            _sheetImporterSettings.messagePackPath = CreateTextField(leftPanel, _sheetImporterSettings.messagePackPath, "메세지팩 경로");
            CreateButton(leftPanel, "전체 선택", () => SetSheeListCheckState(true));
            CreateButton(leftPanel, "전체 해제", () => SetSheeListCheckState(false));
            CreateButton(leftPanel, "코드 자동 생성(시트 데이터 Importer)", ExecuteTableImporterGenerator);
            CreateButton(leftPanel, "선택된 시트로 MessagePack, JSON 텍스트 에셋 생성", SelectedExcelToJson);
            //CreateButton(leftPanel, "선택된 시트 기반 코드 생성", GenerateCodeFromLocalExcel);
            CreateButton(leftPanel, "시트 경로 윈도우 탐색기 열기", () =>
            {
                _processStartInfo.FileName = _sheetImporterSettings.localExcelPath;
                System.Diagnostics.Process.Start(_processStartInfo);
            });
            _sheetListView = new ListView();
            rightPanel.Add(_sheetListView);
            ReadExcelListFromLocal();
        }

        private void SetSheeListCheckState(bool state)
        {
            // 데이터 모델 업데이트
            for (int i = 0; i < _sheetLocalPathData.Count; i++)
            {
                _sheetLocalPathData[i].isImport = state;
            }

            // ListView 갱신
            _sheetListView.RefreshItems();  // bindItem 다시 실행
        }

        private void ReadExcelListFromLocal()
        {
            List<SheetImporterSettings.SheetPathData> sheetPathData = new();
            _sheetLocalPathData.Clear();
            string path = _sheetImporterSettings.localExcelPath;
            if (path == string.Empty)
            {
                Debug.LogWarning("파일 경로를 확인/설정해 주세요.");
                _sheetListView.Clear();
                _sheetListView.RefreshItems();
                return;
            }
            DirectoryInfo di = new(path);
            if (di.Exists == false)
            {
                _sheetListView.Clear();
                _sheetListView.RefreshItems();
                return;
            }
            foreach (FileInfo file in di.GetFiles())
            {
                string name = file.Name;
                if (IsContainOtherLanguage(name))
                    continue;
                if (name.Contains(".xlsx") == false)
                    continue;
                var temp = new SheetPathEditorData();
                temp.Address = path;
                temp.Name = name.Split(".")[0];
                _sheetLocalPathData.Add(temp);
                sheetPathData.Add(new(name, path));
            }
            DrawControlLocalDataSheetList(sheetPathData);
        }

        /// <summary>
        /// if string contains only English letter return false
        /// </summary>
        private bool IsContainOtherLanguage(string s)
        {
            char[] charArr = s.ToCharArray();
            foreach (char c in charArr)
            {
                if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.OtherLetter)
                    return true;
            }
            return false;
        }
        private async void SetDelayButton(int f = 2000)
        {
            _isWorking = true;
            await UniTask.DelayFrame(f);
            _isWorking = false;
        }

        private void ExecuteTableImporterGenerator()
        {
            TableImporterGeneratorEditor.GenerateTableImporters();
        }

        private void SelectedExcelToJson()
        {
            if (_sheetImporterSettings.localExcelPath == string.Empty)
            {
                EditorUtility.DisplayDialog("경고!", "파일 경로를 확인/설정해 주세요.", "확인");
                Debug.LogWarning("파일 경로를 확인/설정해 주세요.");
                return;
            }
            if (_isWorking)
                return;
            string titleString = $"Local Excel To Data";
            int totalWorkProcessCount = _sheetLocalPathData.Count;
            sheetPaths.Clear();
            for (int i = 0; i < _sheetLocalPathData.Count; i++)
            {
                if (!_sheetLocalPathData[i].isImport)
                    continue;
                sheetPaths.Add(string.Format("{0}/{1}.xlsx", _sheetLocalPathData[i].Address, _sheetLocalPathData[i].Name));
            }
            if (sheetPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("경고!", "시트를 선택하셔야 합니다.", "확인");
                return;
            }
            EditorExcelToData.ClearTable();
            SetDelayButton();


            int totalCount = sheetPaths.Count;

            for(int i =0; i < totalCount; ++i)
            {
                EditorUtility.DisplayProgressBar(titleString, string.Format("엑셀 파일({0})을 캐싱 중입니다.", sheetPaths[i]), 0.5f);
                EditorExcelToData.CashingExcelData(sheetPaths[i]);
                EditorUtility.DisplayProgressBar(titleString, string.Format("엑셀 데이터({0})를 데이터화중입니다.", sheetPaths[i]), 0.75f);
                SheetToDataEditorWindow.SheetImport(sheetPaths[i]);
                List<string> newDataPaths = new List<string>();
                EditorUtility.DisplayProgressBar(titleString, "새로 생성된 데이터를 등록중입니다.", 1);
                var temp = EditorExcelToData.ConvertExcelToJson(ref newDataPaths);
                if (temp)
                    AssetDatabase.Refresh();
                for (int idx = 0; idx < newDataPaths.Count; idx++)
                {
                    var unityPath = newDataPaths[idx];
                    bool isJson = unityPath.Contains("json");
                    var groupName = isJson ? "JsonData" : "MessagePackData";
                    var group = AddressableHelper.GetGroup(groupName);
                    var dataObj = AssetDatabase.LoadAssetAtPath<Object>(newDataPaths[idx]);
                    var addressableAsset = AddressableExtensions.GetAddressableAssetEntry(dataObj);
                    if (addressableAsset == null)
                    {
                        AddressableExtensions.SetAddressable(dataObj);
                        addressableAsset = AddressableExtensions.GetAddressableAssetEntry(dataObj);
                        if (group != addressableAsset.parentGroup)
                        {
                            var prevGroupName = addressableAsset.parentGroup;
                            var guid = AssetDatabase.GUIDFromAssetPath(unityPath).ToString();
                            _automationAddressableSetting.AddressableAssetSetting.CreateOrMoveEntry(guid, group);
                            addressableAsset.SetLabel(AUTO_REMOTE_LABEL_NAME, true);
                            if (isJson)
                                addressableAsset.SetLabel(AUTO_JSON_LABEL_NAME, true);
                            else
                                addressableAsset.SetLabel(AUTO_BYTE_LABEL_NAME, true);

                            Debug.Log($"[Move] {dataObj.name}은 ({prevGroupName})그룹에서 ({group.name})그룹으로 변경되었습니다.");
                        }
                    }
                }
            }           
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            EditorExcelToData.ClearTable();
            sheetPaths.Clear();
        }

        private void DrawControlLocalDataSheetList(List<SheetImporterSettings.SheetPathData> sheetDatas)
        {
            if (_sheetListView == null)
                _sheetListView = new();
            _sheetListView.Clear();
            _sheetListView.RefreshItems();

            Func<VisualElement> makeItem = () =>
            {
                var toggle = new Toggle();
                toggle.RegisterValueChangedCallback(OnToggleChanged);
                return toggle;
            };

            Action<VisualElement, int> bindItem = (e, i) =>
            {
                var toggle = e as Toggle;

                // 인덱스를 userData에 저장
                toggle.userData = i;

                // 상태 설정
                toggle.label = _sheetLocalPathData[i].Name;
                toggle.value = _sheetLocalPathData[i].isImport;
            };

            void OnToggleChanged(ChangeEvent<bool> evt)
            {
                var toggle = evt.target as Toggle;
                int index = (int)toggle.userData;
                _sheetLocalPathData[index].isImport = evt.newValue;
            }

            _sheetListView.itemsSource = _sheetLocalPathData;
            _sheetListView.makeItem = makeItem;
            _sheetListView.bindItem = bindItem;
            _sheetListView.fixedItemHeight = 20;
        }
        public static string GetAddress(string address)
        {
            return $"{address}/export?format=xlsx";
        }

        private List<string> sheetPaths = new();
       
        #endregion

        #region AutoAddressable

        private void AutoAddressableForCustom()
        {
            var pathData = _automationAddressableSetting.CustomPathData;

            foreach (var tempPathData in pathData)
            {
                if (GetFileList(tempPathData, out List<string> fileList))
                    continue;

                var groupName = tempPathData.GroupName;
                var group = AddressableHelper.GetGroup(groupName);

                if (!_automationAddressableSetting.CustomDataDict.ContainsKey(tempPathData.GroupName))
                {
                    _automationAddressableSetting.CustomDataDict.Add(tempPathData.GroupName, new List<AddressableDataSubAssetData>());
                }

                foreach (string filePath in fileList)
                {
                    if (filePath.Contains(".meta"))
                        continue;
                    var unityPath = filePath.Replace(Application.dataPath, "Assets");
                    var tempSubData = new AddressableDataSubAssetData();
                    tempSubData._object = AssetDatabase.LoadAssetAtPath<Object>(unityPath);
                    tempSubData._unityPath = filePath;
                    tempSubData._fullPath = unityPath;
                    tempSubData._GUID = AssetDatabase.GUIDFromAssetPath(unityPath).ToString();
                    _automationAddressableSetting.CustomDataDict[tempPathData.GroupName].Add(tempSubData);
                }

                foreach (var tempSubData in _automationAddressableSetting.CustomDataDict[groupName])
                {
                    bool setDirty = false;
                    ///에셋이 어드레서블화 되어있는지 체크
                    var addressableAsset = AddressableExtensions.GetAddressableAssetEntry(tempSubData._object);
                    if (addressableAsset == null)
                    {
                        if (tempSubData._object == null)
                        {
                            AddLogline($"[Non-Object] {tempSubData._fullPath}", LogType.Error);
                            return;
                        }

                        AddLogline($"[Non-Addressable] {tempSubData._object.name}은 어드레서블화가 되어있지않습니다.", LogType.Warning);
                    }

                    var _autoGrouping = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Grouping, true);
                    var _autoAddressable = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Addressable, true);
                    var _autoLabel = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Label, true);
                    var _autoSchema = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Schema, true);

                    if ((!_autoGrouping && group == null) || (addressableAsset == null && !_autoAddressable))
                        continue;

                    if (group == null)
                        group = AddressableHelper.CreateGroup(groupName);
                    else if (_autoSchema && group.HasSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>() == false)    // 스키마 체크
                    {
                        AddressableHelper.CreateGroupSchema(groupName);
                    }

                    //없을경우 새로 어드레서블 등록
                    if (addressableAsset == null)
                    {
                        AddressableExtensions.SetAddressable(tempSubData._object);
                        addressableAsset = AddressableExtensions.GetAddressableAssetEntry(tempSubData._object);
                        AddLogline($"{tempSubData._object.name}은 어드레서블 등록되었습니다.", LogType.Log);
                    }

                    //등록할 그룹과 현재 그룹이 다른경우 그룹 변경
                    if (group != addressableAsset.parentGroup)
                    {
                        var prevGroupName = addressableAsset.parentGroup;
                        _automationAddressableSetting.AddressableAssetSetting.CreateOrMoveEntry(tempSubData._GUID, group);
                        AddLogline(
                            $"[Move] {tempSubData._object.name}은 ({prevGroupName})그룹에서 ({group.name})그룹으로 변경되었습니다.",
                            LogType.Log);
                    }

                    var addressableAssetPath = tempSubData._object.GetAddressableAssetPath();
                    var assetPath = AssetDatabase.GetAssetPath(tempSubData._object);
                    if (addressableAssetPath != assetPath)
                        _incorrectAddressablePathObjects.Add(tempSubData._object);

                    if (_autoLabel && !addressableAsset.labels.Contains(AUTO_REMOTE_LABEL_NAME))
                    {
                        addressableAsset.SetLabel(AUTO_REMOTE_LABEL_NAME, true);
                    }
                }
            }
        }

        private bool GetFileList(AddressableCustomPathData tempPathData, out List<string> fileList)
        {
            fileList = new List<string>();
            foreach (var tempPath in tempPathData.PathList)
            {
                var tempFullPath = Application.dataPath + tempPath;
                switch (tempPathData.SearchOption)
                {
                    case AutoSearchOption.None:
                        break;
                    case AutoSearchOption.OnlyFiles:
                        fileList.Add(tempFullPath);
                        break;
                    case AutoSearchOption.Folder:
                        fileList.AddRange(GetAllFilesInFolder(tempFullPath, SearchOption.TopDirectoryOnly));
                        if (fileList.Count == 0)
                        {
                            AddLogline($"[Error] 해당 경로에는 폴더나 파일이 없습니다. ({tempFullPath})", LogType.Error);
                            return true;
                        }

                        break;
                    case AutoSearchOption.SearchSubdirectories:
                        fileList = GetAllFilesInFolder(tempFullPath, SearchOption.AllDirectories);
                        if (fileList.Count == 0)
                        {
                            AddLogline($"[Error] 해당 경로에는 폴더나 파일이 없습니다. ({tempFullPath})", LogType.Error);
                            return true;
                        }

                        break;
                    default:
                        break;
                }
            }

            return false;
        }

        private bool GetFileList(AddressablePathData pathData, string tempFullPath, out List<string> fileList)
        {
            fileList = new List<string>();
            switch (pathData.SearchOption)
            {
                case AutoSearchOption.None:
                    return true;
                case AutoSearchOption.OnlyFiles:
                    fileList.Add(tempFullPath + pathData.FileExtensionName);
                    break;
                case AutoSearchOption.Folder:
                    fileList = GetAllFilesInFolder(tempFullPath, SearchOption.TopDirectoryOnly);
                    if (fileList.Count == 0)
                    {
                        AddLogline($"[Error] 해당 경로에는 폴더나 파일이 없습니다. ({tempFullPath})", LogType.Warning);
                        return true;
                    }

                    break;
                case AutoSearchOption.SearchSubdirectories:
                    fileList = GetAllFilesInFolder(tempFullPath, SearchOption.AllDirectories);
                    if (fileList.Count == 0)
                    {
                        AddLogline($"[Error] 해당 경로에는 폴더나 파일이 없습니다. ({tempFullPath})", LogType.Warning);
                        return true;
                    }

                    break;
                default:
                    break;
            }

            return false;
        }

        private void AddLogline(string tempErrorLog, LogType logType)
        {
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(tempErrorLog);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(tempErrorLog);
                    break;
                case LogType.Error:
                    Debug.LogError(tempErrorLog);
                    break;
                default:
                    break;
            }
            masterSb.AppendLine(tempErrorLog);
        }

        private void AutoAddressable(string groupName,
            List<string> fileList,
            AddressableDigimonData addressableDigimonData,
            AddressablePathData pathData)
        {
            var _autoGrouping = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Grouping, true);
            var _autoAddressable = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Addressable, true);
            var _autoLabel = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Label, true);
            var _autoSchema = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Schema, true);

            ///어드레서블 그룹 체크
            var group = AddressableHelper.GetGroup(groupName);
            if (!addressableDigimonData.SubAssetDataList.ContainsKey(pathData.DevName))
            {
                addressableDigimonData.SubAssetDataList.Add(pathData.DevName, new List<AddressableDataSubAssetData>());
            }

            foreach (string filePath in fileList)
            {
                if (filePath.Contains(".meta"))
                    continue;
                var unityPath = filePath.Replace(Application.dataPath, "Assets");
                var tempSubData = new AddressableDataSubAssetData();
                tempSubData._object = AssetDatabase.LoadAssetAtPath<Object>(unityPath);
                tempSubData._unityPath = filePath;
                tempSubData._fullPath = unityPath;
                tempSubData._GUID = AssetDatabase.GUIDFromAssetPath(unityPath).ToString();
                addressableDigimonData.SubAssetDataList[pathData.DevName].Add(tempSubData);
            }

            foreach (var tempSubData in addressableDigimonData.SubAssetDataList[pathData.DevName])
            {
                ///에셋이 어드레서블화 되어있는지 체크
                var addressableAsset = AddressableExtensions.GetAddressableAssetEntry(tempSubData._object);
                if (addressableAsset == null)
                {
                    if (tempSubData._object == null)
                    {
                        AddLogline($"[Non-Object] {tempSubData._fullPath}", LogType.Warning);
                        return;
                    }

                    AddLogline($"[Non-Addressable] {tempSubData._object.name}은 어드레서블화가 되어있지않습니다.", LogType.Warning);
                }

                if ((!_autoGrouping && group == null) || (addressableAsset == null && !_autoAddressable))
                    continue;

                if (group == null)
                    group = AddressableHelper.CreateGroup(groupName);
                else if (_autoSchema && group.HasSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>() == false)   // 스키마 체크
                {
                    AddressableHelper.CreateGroupSchema(groupName);
                }
                
                //없을경우 새로 어드레서블 등록
                if (addressableAsset == null)
                {
                    AddressableExtensions.SetAddressable(tempSubData._object);
                    addressableAsset = AddressableExtensions.GetAddressableAssetEntry(tempSubData._object);
                    AddLogline($"{tempSubData._object.name}은 어드레서블 등록되었습니다.", LogType.Log);
                }

                //등록할 그룹과 현재 그룹이 다른경우 그룹 변경
                if (group != addressableAsset.parentGroup)
                {
                    var prevGroupName = addressableAsset.parentGroup;
                    _automationAddressableSetting.AddressableAssetSetting.CreateOrMoveEntry(tempSubData._GUID, group);
                    AddLogline($"[Move] {tempSubData._object.name}은 ({prevGroupName})그룹에서 ({group.name})그룹으로 변경되었습니다.",
                        LogType.Log);
                }

                var addressableAssetPath = tempSubData._object.GetAddressableAssetPath();
                var assetPath = AssetDatabase.GetAssetPath(tempSubData._object);
                if (addressableAssetPath != assetPath)
                    _incorrectAddressablePathObjects.Add(tempSubData._object);

                if (_autoLabel && !addressableAsset.labels.Contains(AUTO_REMOTE_LABEL_NAME))
                    addressableAsset.SetLabel(AUTO_REMOTE_LABEL_NAME, true);
            }
        }

        #endregion

        #region Array/List Meta 자동생성

        public static void CreateArrayListMeta()
        {
            EditorToolHelper.CreateArrayListMeta();
        }

        #endregion

        #region Utility

        static List<string> GetAllFilesInFolder(string folderPath, SearchOption searchOption)
        {
            List<string> fileList = new List<string>();

            try
            {
                // 폴더 내의 모든 파일 가져오기
                string[] files = Directory.GetFiles(folderPath, "*.*", searchOption);

                foreach (string file in files)
                {
                    fileList.Add(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("오류 발생: " + ex.Message);
            }

            return fileList;
        }

        private async UniTask CleanUpMemoryAsync()
        {
            await Resources.UnloadUnusedAssets();
            System.GC.Collect();
        } 

        #endregion
    }
}