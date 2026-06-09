using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ProjectHD.Editor
{
    public class AddressableAutomation : IDisposable
    {
        private readonly AutomationAddressableSetting _automationAddressableSetting;
        private readonly string _autoRemoteLabelName;
        private readonly List<Object> _incorrectAddressablePathObjects;
        public AddressableAutomation(AutomationAddressableSetting setting, string autoRemoteLabelName)
        {
            _automationAddressableSetting = setting;
            _autoRemoteLabelName = autoRemoteLabelName;
            _incorrectAddressablePathObjects = new List<Object>();
        }

        public void Dispose()
        {
            _incorrectAddressablePathObjects.Clear();
        }

        public List<Object> GetAddressablePathObjects()
        {
            return _incorrectAddressablePathObjects;
        }
        
        public void RunAutomation()
        {
            _incorrectAddressablePathObjects.Clear();
            
            // 에디터 툴에서 체크하는 값(PlayerPrefs에 저장)
            bool autoGrouping = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Grouping, true);
            bool autoAddressable = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Addressable, true);
            bool autoLabel = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Label, true);
            bool autoSchema = DeviceRepository.LoadKeyForBoolean(DeviceRepositoryKey.Editor_AutoAddressable_Schema, true);
            
            foreach (AddressableCustomPathData pathData in _automationAddressableSetting.CustomPathData)
            {
                // 경로 내 파일 가져오기(경로 리스트(pathData.PathList)를 순회)
                if (!EditorToolHelper.TryGetFileList(pathData, out List<string> fileList))
                    continue;
                
                // 그룹 생성
                AddressableAssetGroup group = EnsureGroup(pathData.GroupName);
                
                // 그룹 검사
                if ((!autoGrouping && group == null))
                    continue;
                
                // 스키마 검사
                if (autoSchema)
                    EnsureSchema(group);
                
                if (!autoAddressable)
                    continue;
                
                // 어드레서블 딕셔너리 채우기
                FillCustomDataDict(pathData, fileList);
                // 어드레서블 등록, 그룹 체크, 어드레서블 라벨 체크
                ProcessAssets(group, pathData.GroupName, autoLabel);
            }
        }

        private AddressableAssetGroup EnsureGroup(string groupName)
        {
            AddressableAssetGroup group = AddressableHelper.GetGroup(groupName);
            if (group != null)
            {
                return group;
            }

            group = AddressableHelper.CreateGroup(groupName);
            Debug.Log($"[CreatedGroup] {groupName} 그룹이 생성되었습니다.");
            return group;
        }

        private void EnsureSchema(AddressableAssetGroup group)
        {
            if (!group.HasSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>())
            {
                AddressableHelper.CreateGroupSchema(group.Name);
            }
        }

        private void FillCustomDataDict(AddressableCustomPathData pathData, List<string> fileList)
        {
            // 그룹 이름 key, 에셋정보를 담은 리스트 value 딕셔너리
            if (!_automationAddressableSetting.CustomDataDict.ContainsKey(pathData.GroupName))
            {
                _automationAddressableSetting.CustomDataDict.Add(pathData.GroupName, new List<AddressableDataSubAssetData>());
            }
            
            foreach (string filePath in fileList)
            {
                if (filePath.Contains(".meta"))
                    continue;
                string unityPath = filePath.Replace(Application.dataPath, "Assets");
                // 딕셔너리 value인 에셋정보 List에 경로 내의 에셋 정보 채우기
                AddressableDataSubAssetData tempSubData = new ()
                {
                    _object = AssetDatabase.LoadAssetAtPath<Object>(unityPath), _unityPath = filePath, _fullPath = unityPath,
                    _GUID = AssetDatabase.GUIDFromAssetPath(unityPath).ToString()
                };
                _automationAddressableSetting.CustomDataDict[pathData.GroupName].Add(tempSubData);
            }
        }

        private void ProcessAssets(AddressableAssetGroup group, string groupName, bool autoLabel)
        {
            // 실제 에셋 정보가 채워진 리스트 순회 (어드레서블 엔트리 체크, 
            foreach (AddressableDataSubAssetData tempSubData in _automationAddressableSetting.CustomDataDict[groupName])
            {
                // 에셋이 어드레서블화 되어있는지 체크
                AddressableAssetEntry addressableAsset = tempSubData._object.GetAddressableAssetEntry();
                if (addressableAsset == null)
                {
                    if (tempSubData._object == null)
                    {
                        EditorToolHelper.AddLogline($"[Non-Object] {tempSubData._fullPath}", LogType.Error);
                        continue;
                    }

                    EditorToolHelper.AddLogline($"[Non-Addressable] {tempSubData._object.name}은 어드레서블화가 되어있지않습니다.", LogType.Warning);
                }

                // 없을경우 새로 어드레서블 등록
                if (addressableAsset == null)
                {
                    AddressableExtensions.SetAddressable(tempSubData._object);
                    addressableAsset = tempSubData._object.GetAddressableAssetEntry();
                    EditorToolHelper.AddLogline($"{tempSubData._object.name}은 어드레서블 등록되었습니다.", LogType.Log);
                }

                // 등록할 그룹과 현재 그룹이 다른경우 그룹 변경
                if (group != addressableAsset.parentGroup)
                {
                    AddressableAssetGroup prevGroup = addressableAsset.parentGroup;
                    _automationAddressableSetting.AddressableAssetSetting.CreateOrMoveEntry(tempSubData._GUID, group);
                    EditorToolHelper.AddLogline(
                        $"[Move] {tempSubData._object.name}은 ({prevGroup.name})그룹에서 ({group.name})그룹으로 변경되었습니다.",
                        LogType.Log);
                }
                
                // 라벨 체크
                if (autoLabel && !addressableAsset.labels.Contains(_autoRemoteLabelName))
                {
                    addressableAsset.SetLabel(_autoRemoteLabelName, true);
                }

                AddIncorrectAddressableAsset(tempSubData);
            }
        }

        private void AddIncorrectAddressableAsset(AddressableDataSubAssetData tempSubData)
        {
            // 실제 경로와 어드레서블 주소가 다른 에셋 체크
            string addressableAssetPath = tempSubData._object.GetAddressableAssetPath();
            string assetPath = AssetDatabase.GetAssetPath(tempSubData._object);
            if (addressableAssetPath != assetPath)
                _incorrectAddressablePathObjects.Add(tempSubData._object);
        }
    }
}

