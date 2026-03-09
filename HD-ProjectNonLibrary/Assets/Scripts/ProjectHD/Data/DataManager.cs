using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ProjectHD
{
    public class DataManager
    {
        #region Original

        public Data.SingleData<int, Data.Dummy> Dummy { get; private set; }        
        public Data.SingleData<int, Data.ImmutableDummy> ImmutableDummy { get; private set; }
        public Data.SingleData<int, int, long, Data.DummyDummy> DummyDummy { get; private set; }
        public Data.SingleData<ProjectEnum.ConstDefine, Data.TestConstValue> TestConstValue { get; private set; }
        public Data.SingleData<int, Data.GradeStat> GradeStat { get; private set; }
        public Data.SingleData<int, Data.CharacterTable> CharacterTable { get; private set; }
        public Data.SingleData<System.ValueTuple<int,int>, Data.CharLevelStatTable> CharLevelStatTable { get; private set; }
        public Data.SingleData<int, Data.MonsterTable> MonsterTable { get; private set; }
        public Data.SingleData<System.ValueTuple<int, int>, Data.MonsterLevelStatTable> MonsterLevelStatTable { get; private set; }
        public Data.GroupedData<int, Data.BuffGroup> BuffGroup { get; private set; }
        public Data.GroupedData<int, Data.DebuffGroup> DebuffGroup { get; private set; }
        public Data.SingleData<int, Data.GameItem> GameItem { get; private set; }
        public Data.SingleData<int, Data.StageTable> StageTable { get; private set; }
        public Data.GroupedData<int, Data.StageWaveGroup> StageWaveGroup { get; private set; }
        public Data.SingleData<int, Data.StagePointReward> StagePointReward { get; private set; }
        public Data.SingleData<ProjectEnum.UnitProperty, Data.UnitPropertyDefine> UnitPropertyDefine { get; private set; }
        public Data.SingleData<ProjectEnum.CharacterType, Data.UnitTypeDefine> UnitTypeDefine { get; private set; }
        public Data.SingleData<System.ValueTuple<ProjectEnum.UnitProperty, int>, Data.UnitPropertyBuffSet> UnitPropertyBuffSet { get; private set; }
        public Data.SingleData<System.ValueTuple<ProjectEnum.CharacterType, int>, Data.UnitTypeBuffSet> UnitTypeBuffSet { get; private set; }

        #endregion
        #region Wrapper

        public Data.GroupedData<ProjectEnum.UnitProperty, Data.CharacterPropertyGroup> CharacterPropertyGroup { get; private set; }
        public Data.SingleData<ProjectEnum.UnitProperty, Data.UnitPropertyBuffSetMaxCount> UnitPropertyBuffSetMaxCount { get; private set; }
        public Data.SingleData<ProjectEnum.CharacterType, Data.UnitTypeBuffSetMaxCount> UnitTypeBuffSetMaxCount { get; private set; }

        #endregion

        public DataManager()
        {
            
        }

        public async UniTask ReadDataAsync()
        {
            if (Application.isEditor)
            {
                await UniTask.Lazy(LoadAllMessagePackAsync);
            }
            else
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        await UniTask.Lazy(LoadAllMessagePackAsync);
                        break;
                    case RuntimePlatform.IPhonePlayer:
                        await UniTask.Lazy(LoadAllMessagePackAsync);
                        break;
                    default:
                        await UniTask.Lazy(LoadAllMessagePackAsync);
                        break;
                }
            }
        }

        private async UniTask<int> PostLoadDataAsync()  // Wrapper 조합용(기본 데이터 로드 후 커스텀으로 래핑할 데이터)
        {
            CharacterPropertyGroup = new();
            List<Data.CharacterPropertyGroup> groups = new();
            using var enumerator = CharacterTable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                Data.CharacterPropertyGroup entryMember = new(current.Value.CharacterProperty, current.Value.Seed);
                groups.Add(entryMember);
            }
            CharacterPropertyGroup.ReadData(groups);
            await UniTask.Yield();

            UnitPropertyBuffSetMaxCount = new();
            List<Data.UnitPropertyBuffSetMaxCount> propertyBuffSetMaxCount = new();
            Dictionary<ProjectEnum.UnitProperty, int> propertiesMaxCounts = new();

            foreach (var property in UnitPropertyBuffSet.Set.Data)
            {
                if (propertiesMaxCounts.ContainsKey(property.CharacterProperty))
                {
                    if (propertiesMaxCounts[property.CharacterProperty] < property.ActiveCount)
                        propertiesMaxCounts[property.CharacterProperty] = property.ActiveCount;
                }
                else
                    propertiesMaxCounts.Add(property.CharacterProperty, property.ActiveCount);
            }

            foreach (var tempProperty in propertiesMaxCounts)
            {
                propertyBuffSetMaxCount.Add(new(tempProperty.Key, tempProperty.Value));
            }

            UnitPropertyBuffSetMaxCount.ReadData(propertyBuffSetMaxCount);
            await UniTask.Yield();

            UnitTypeBuffSetMaxCount = new();
            List<Data.UnitTypeBuffSetMaxCount> typeBuffSetMaxCount = new();
            Dictionary<ProjectEnum.CharacterType, int> typesMaxCounts = new();

            foreach (var type in UnitTypeBuffSet.Set.Data)
            {
                if (typesMaxCounts.ContainsKey(type.CharacterType))
                {
                    if (typesMaxCounts[type.CharacterType] < type.ActiveCount)
                        typesMaxCounts[type.CharacterType] = type.ActiveCount;
                }
                else
                    typesMaxCounts.Add(type.CharacterType, type.ActiveCount);
            }

            foreach (var tempType in typesMaxCounts)
            {
                typeBuffSetMaxCount.Add(new(tempType.Key, tempType.Value));
            }

            UnitTypeBuffSetMaxCount.ReadData(typeBuffSetMaxCount);
            await UniTask.Yield();

            return 0;
        }
        
        #region Load Json

        public async UniTask LoadAllJsonsAsync()
        {
            var assetLocationsHandle = Addressables.LoadResourceLocationsAsync("JsonData", typeof(TextAsset));
            var assetLocations = await assetLocationsHandle;

            var testAssetsHandle = Addressables.LoadAssetsAsync<TextAsset>(assetLocations, null);
            var testAssets = await testAssetsHandle;

            await LoadJsonTextAssetsAsync(testAssets);
            //await LoadAutoJson();
            await PostLoadDataAsync();

            Addressables.Release(testAssetsHandle);
            Addressables.Release(assetLocationsHandle);
        }

        async UniTask LoadJsonTextAssetsAsync(IList<TextAsset> textAssets)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<UniTask> lisTask = new List<UniTask>();
            for (int i = 0; i < textAssets.Count; ++i)
            {
                var jsonFile = textAssets[i];
                string fileName = string.Copy(jsonFile.name);
                string jsonText = string.Copy(jsonFile.text);

                var task = UniTask.RunOnThreadPool(async delegate { await LoadJsonTextAsync(fileName, jsonText); });
                lisTask.Add(task);
            }

            await UniTask.WhenAll(lisTask);

            Utilities.InternalDebug.Log("total json : " + stopwatch.Elapsed.Milliseconds + " [ms]");
        }

        private async UniTask LoadJsonTextAsync(string fileName, string asset)
        {
            switch (fileName)
            {
                case "Dummy":
                    Dummy = new();
                    await Dummy.ReadDataAsync(asset);
                    break;
                case "ImmutableDummy":
                    ImmutableDummy = new();
                    await ImmutableDummy.ReadDataAsync(asset);
                    break;
                case "DummyDummy":
                    DummyDummy = new(Data.DummyDummy.CalculateKey);
                    await DummyDummy.ReadDataAsync(asset);
                    break;
                case "TestConstValue":
                    TestConstValue = new();
                    await TestConstValue.ReadDataAsync(asset);
                    break;
                case "GradeStat":
                    GradeStat = new();
                    await GradeStat.ReadDataAsync(asset);
                    break;
                case "CharacterTable":
                    CharacterTable = new();
                    await CharacterTable.ReadDataAsync(asset);
                    break;
                case "CharLevelStatTable":
                    CharLevelStatTable = new();
                    await CharLevelStatTable.ReadDataAsync(asset);
                    break;
                case "MonsterTable":
                    MonsterTable = new();
                    await MonsterTable.ReadDataAsync(asset);
                    break;
                case "MonsterLevelStatTable":
                    MonsterLevelStatTable = new();
                    await MonsterLevelStatTable.ReadDataAsync(asset);
                    break;
                case "BuffGroup":
                    BuffGroup = new();
                    await BuffGroup.ReadDataAsync(asset);
                    break;
                case "DebuffGroup":
                    DebuffGroup = new();
                    await DebuffGroup.ReadDataAsync(asset);
                    break;
                case "GameItem":
                    GameItem = new();
                    await GameItem.ReadDataAsync(asset);
                    break;
                case "StageTable":
                    StageTable = new();
                    await StageTable.ReadDataAsync(asset);
                    break;
                case "StageWaveGroup":
                    StageWaveGroup = new();
                    await StageWaveGroup.ReadDataAsync(asset);
                    break;
                case "StagePointReward":
                    StagePointReward = new();
                    await StagePointReward.ReadDataAsync(asset);
                    break;
                case "UnitPropertyDefine":
                    UnitPropertyDefine = new();
                    await UnitPropertyDefine.ReadDataAsync(asset);
                    break;
                case "UnitTypeDefine":
                    UnitTypeDefine = new();
                    await UnitTypeDefine.ReadDataAsync(asset);
                    break;
                case "UnitPropertyBuffSet":
                    UnitPropertyBuffSet = new();
                    await UnitPropertyBuffSet.ReadDataAsync(asset);
                    break;
                case "UnitTypeBuffSet":
                    UnitTypeBuffSet = new();
                    await UnitTypeBuffSet.ReadDataAsync(asset);
                    break;
            }
        }
        
        #endregion

        #region Load Message Pack

        public async UniTask LoadAllMessagePackAsync()
        {
            var assetLocationsHandle = Addressables.LoadResourceLocationsAsync("MessagePackData", typeof(TextAsset));
            var assetLocations = await assetLocationsHandle;

            var textAssetsHandle = Addressables.LoadAssetsAsync<TextAsset>(assetLocations, null);
            var textAssets = await textAssetsHandle;

            await LoadMessagePackAssetsAsync(textAssets);
            //await LoadAutoMessagePack();
            await PostLoadDataAsync();

            Addressables.Release(textAssetsHandle);
            Addressables.Release(assetLocationsHandle);
        }

        async UniTask LoadMessagePackAssetsAsync(IList<TextAsset> textAssets)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<UniTask> lisTask = new List<UniTask>();
            for (int i = 0; i < textAssets.Count; ++i)
            {
                var jsonFile = textAssets[i];
                string fileName = string.Copy(jsonFile.name);
                byte[] copyBytes = new byte[jsonFile.bytes.Length];
                System.Buffer.BlockCopy(jsonFile.bytes, 0, copyBytes, 0, jsonFile.bytes.Length);

                var task = UniTask.RunOnThreadPool(async delegate { await LoadMessagePackAsync(fileName, copyBytes); });
                lisTask.Add(task);
            }
            
            await UniTask.WhenAll(lisTask);

            Utilities.InternalDebug.Log("total message pack : " + stopwatch.Elapsed.Milliseconds + " [ms]");
        }

        private async UniTask LoadMessagePackAsync(string fileName, byte[] asset)
        {
            switch (fileName)
            {
                case "Dummy":
                    Dummy = new();
                    await Dummy.ReadDataAsync(asset);
                    break;
                case "ImmutableDummy":
                    ImmutableDummy = new();
                    await ImmutableDummy.ReadDataAsync(asset);
                    break;
                case "DummyDummy":
                    DummyDummy = new(Data.DummyDummy.CalculateKey);
                    await DummyDummy.ReadDataAsync(asset);
                    break;
                case "TestConstValue":
                    TestConstValue = new();
                    await TestConstValue.ReadDataAsync(asset);
                    break;
                case "GradeStat":
                    GradeStat = new();
                    await GradeStat.ReadDataAsync(asset);
                    break;
                case "CharacterTable":
                    CharacterTable = new();
                    await CharacterTable.ReadDataAsync(asset);
                    break;
                case "CharLevelStatTable":
                    CharLevelStatTable = new();
                    await CharLevelStatTable.ReadDataAsync(asset);
                    break;
                case "MonsterTable":
                    MonsterTable = new();
                    await MonsterTable.ReadDataAsync(asset);
                    break;
                case "MonsterLevelStatTable":
                    MonsterLevelStatTable = new();
                    await MonsterLevelStatTable.ReadDataAsync(asset);
                    break;
                case "BuffGroup":
                    BuffGroup = new();
                    await BuffGroup.ReadDataAsync(asset);
                    break;
                case "DebuffGroup":
                    DebuffGroup = new();
                    await DebuffGroup.ReadDataAsync(asset);
                    break;
                case "GameItem":
                    GameItem = new();
                    await GameItem.ReadDataAsync(asset);
                    break;
                case "StageTable":
                    StageTable = new();
                    await StageTable.ReadDataAsync(asset);
                    break;
                case "StageWaveGroup":
                    StageWaveGroup = new();
                    await StageWaveGroup.ReadDataAsync(asset);
                    break;
                case "StagePointReward":
                    StagePointReward = new();
                    await StagePointReward.ReadDataAsync(asset);
                    break;
                case "UnitPropertyDefine":
                    UnitPropertyDefine = new();
                    await UnitPropertyDefine.ReadDataAsync(asset);
                    break;
                case "UnitTypeDefine":
                    UnitTypeDefine = new();
                    await UnitTypeDefine.ReadDataAsync(asset);
                    break;
                case "UnitPropertyBuffSet":
                    UnitPropertyBuffSet = new();
                    await UnitPropertyBuffSet.ReadDataAsync(asset);
                    break;
                case "UnitTypeBuffSet":
                    UnitTypeBuffSet = new();
                    await UnitTypeBuffSet.ReadDataAsync(asset);
                    break;
            }
        }

        #endregion
    }
}