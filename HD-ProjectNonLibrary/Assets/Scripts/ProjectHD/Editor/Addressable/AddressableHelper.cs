using UnityEngine.AddressableAssets;

namespace ProjectHD.Editor
{
    using System.Linq;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;
    using UnityEngine;

    public static class AddressableHelper
    {
        public static AddressableAssetEntry CreateAssetEntry<T>(T source, string groupName, string label) where T : Object
        {
            var entry = CreateAssetEntry(source, groupName);
            if (source != null)
            {
                source.AddAddressableAssetLabel(label);
            }

            return entry;
        }

        public static AddressableAssetEntry CreateAssetEntry<T>(T source, string groupName) where T : Object
        {
            if (source == null || string.IsNullOrEmpty(groupName) || !AssetDatabase.Contains(source))
                return null;

            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            var group = !GroupExists(groupName) ? CreateGroup(groupName) : GetGroup(groupName);

            var entry = addressableSettings.CreateOrMoveEntry(sourceGuid, group);
            entry.address = sourcePath;

            addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            return entry;
        }

        public static AssetReferenceT<T> CreateAssetReferenceT<T>(T source) where T : Object
        {
            if (source == null || !AssetDatabase.Contains(source))
                return null;

            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            var entry = addressableSettings.CreateOrMoveEntry(sourceGuid, addressableSettings.DefaultGroup);
            entry.address = sourcePath;

            addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            return new AssetReferenceT<T>(sourceGuid);
        }

        public static AssetReferenceT<T> GetAssetReferenceT<T>(T source) where T : Object
        {
            if (source == null || !AssetDatabase.Contains(source))
                return null;

            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            return new AssetReferenceT<T>(sourceGuid);
        }

        public static AssetReference GetAssetReference(Object source)
        {
            if (source == null || !AssetDatabase.Contains(source))
                return null;

            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            return new AssetReference(sourceGuid);
        }

        public static AddressableAssetEntry CreateAssetEntry<T>(T source) where T : Object
        {
            if (source == null || !AssetDatabase.Contains(source))
                return null;

            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            var entry = addressableSettings.CreateOrMoveEntry(sourceGuid, addressableSettings.DefaultGroup);
            entry.address = sourcePath;

            addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            return entry;
        }

        public static AddressableAssetGroup GetGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return null;

            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            return addressableSettings.FindGroup(groupName);
        }

        public static AddressableAssetGroup CreateGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return null;

            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            var group = addressableSettings.CreateGroup(groupName, false, false, false, addressableSettings.DefaultGroup.Schemas);

            addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, group, true);

            return group;
        }

        public static bool GroupExists(string groupName)
        {
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            return addressableSettings.FindGroup(groupName) != null;
        }

        public static void CreateGroupSchema(string groupName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = GetGroup(groupName);

            for (int i = 0; i < settings.DefaultGroup.Schemas.Count; i++)
            {
                if (group.HasSchema(settings.DefaultGroup.Schemas[i].GetType()))
                    continue;
                group.AddSchema(settings.DefaultGroup.Schemas[i]);
                Utilities.InternalDebug.LogWarning($"{groupName}에 {settings.DefaultGroup.Schemas[i].name}을 추가하였습니다.");
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaAdded, group, true);
        }

        public static void SetBundledAssetGroupSchemaClearBehavior(string groupName, UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema.CacheClearBehavior cacheClearBehavior)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(groupName);

            for (int i = 0; i < group.Schemas.Count; i++)
            {
                var groupSchema = group.Schemas[i];
                if (groupSchema is UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema bundledAssetGroupSchema)
                {
                    bundledAssetGroupSchema.AssetBundledCacheClearBehavior = cacheClearBehavior;
                    Utilities.InternalDebug.LogWarning($"{groupName}에 {settings.DefaultGroup.Schemas[i].name}의 AssetBundledCacheClearBehavior 설정이 {cacheClearBehavior}로 설정되었습니다.");
                }
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaModified, group, true);
        }

        public static void ApplyCacheClearBehaviorToRemoteGroups(string remoteName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Utilities.InternalDebug.LogWarning("[Addressables] Settings가 존재하지 않습니다.");
                return;
            }

            int modifiedCount = 0;

            foreach (var group in settings.groups)
            {
                if (group == null || group.ReadOnly)
                    continue;

                // 그룹 내 에셋 중 하나라도 "Remote" 레이블이 있으면 해당 그룹 대상으로 판단
                bool hasRemoteLabel = group.entries.Any(entry => entry.labels.Contains(remoteName));
                if (!hasRemoteLabel)
                    continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;

                // 설정 변경
                schema.AssetBundledCacheClearBehavior = BundledAssetGroupSchema.CacheClearBehavior.ClearWhenWhenNewVersionLoaded;
                modifiedCount++;

                Utilities.InternalDebug.Log($"✅ 그룹 '{group.Name}'에 CacheClearBehavior 설정 적용 완료.");
            }

            if (modifiedCount > 0)
            {
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaModified, null, true);
                Utilities.InternalDebug.Log($"🎉 {modifiedCount}개의 그룹에 CacheClearBehavior가 적용되었습니다!");
            }
            else
            {
                Utilities.InternalDebug.Log("ℹ️ 'Remote' 레이블을 포함한 그룹이 없습니다. 적용된 항목이 없습니다.");
            }
        }
    }
}