using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using UnityEngine.UI;
using Utilities;

namespace ProjectHD
{
    public class AtlasLoader : Singleton<AtlasLoader>
    {
        private const string Icons = "Icons";
        private const string InGame = "InGame";
        private const string Lobby = "Lobby";

        private bool alreadyInitialized = false;

        //[RuntimeInitializeOnLoadMethod]
        public static void Startup()
        {
            Instance._Startup();
        }

        private void _Startup()
        {
            if (!alreadyInitialized)
            {
                InternalDebug.Log("Startup");
                SpriteAtlasManager.atlasRequested -= RequestAtlas; // 중복 방지
                SpriteAtlasManager.atlasRequested += RequestAtlas;
                SpriteAtlasManager.atlasRegistered -= AtlasRegistered;
                SpriteAtlasManager.atlasRegistered += AtlasRegistered;
                alreadyInitialized = true;
            }
        }

        private readonly Dictionary<string, SpriteAtlas> _dicSpriteAtlas = new();
        private readonly Dictionary<string, Sprite> _dicSprite = new();

        // 리소스 다운로드 후 호출
        public async UniTask AfterResourceDownloaded()
        {
            await ForceRequestAtlasesAsync(new string[] { InGame });
        }

        void RequestAtlas(string tag, System.Action<SpriteAtlas> callback)
        {
            Utilities.InternalDebug.Log("Tag Name : " + tag);
            string atlasAssetKey;
            SpriteAtlas atlas;
            if (tag.Equals("Title"))
            {
                atlas = Resources.Load<SpriteAtlas>("SpriteAtlas/Title");
            }
            else
            {
                atlasAssetKey = $"Assets/GameResources/SpriteAtlas/{tag}.spriteatlasv2";
                atlas = Addressables.LoadAssetAsync<SpriteAtlas>(atlasAssetKey).WaitForCompletion();
            }
            callback(atlas);

            _dicSpriteAtlas[atlas.tag] = atlas;
        }

        private async UniTask ForceRequestAtlasesAsync(params string[] tags)
        {
            foreach (string tag in tags)
            {
                string atlasAssetKey = $"Assets/GameResources/SpriteAtlas/{tag}.spriteatlasv2";
                SpriteAtlas atlas = null;

                try
                {
                    var handle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasAssetKey);
                    await handle.ToUniTask();
                    atlas = handle.Result;

                    if (atlas != null)
                    {
                        _dicSpriteAtlas[atlas.tag] = atlas;
                        Utilities.InternalDebug.Log($"[{tag}] 로드 완료");
                    }
                    else
                    {
                        Utilities.InternalDebug.LogWarning($"[{tag}] 아틀라스 로드 실패");
                    }
                }
                catch (Exception e)
                {
                    Utilities.InternalDebug.LogError($"[{tag}] 예외 발생: {e.Message}");
                }
            }
        }

        void AtlasRegistered(SpriteAtlas atlas)
        {
            //_dicSpriteAtlas[atlas.tag] = atlas;
            Utilities.InternalDebug.Log($"[{atlas}] 등록 완료");
        }

        bool _TryGetSprite(string spriteName, out Sprite outSprite)
        {
            if (string.IsNullOrEmpty(spriteName))
                goto NOT_FOUND;
            if (_dicSprite.TryGetValue(spriteName, out outSprite) && outSprite != null) return true;
            using (var enumerator = _dicSpriteAtlas.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var kvp = enumerator.Current;
                    if (!_dicSpriteAtlas.TryGetValue(kvp.Key, out var atlas)) continue;
                    var sprite = atlas.GetSprite(spriteName);
                    if (sprite)
                    {
                        outSprite = sprite;
                        _dicSprite.Add(spriteName, sprite);
                        return true;
                    }
                }
            }

        NOT_FOUND:
            outSprite = null;
            return false;
        }

        private async UniTask<Sprite> TryGetSpriteAsync(string atlasTag, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            if (_dicSprite.TryGetValue(spriteName, out var outSprite) && outSprite != null)
                return outSprite;

            if (!_dicSpriteAtlas.TryGetValue(atlasTag, out var atlas))
            {
                return null;
            }

            var sprite = atlas.GetSprite(spriteName);
            if (sprite)
            {
                _dicSprite.Add(spriteName, sprite);
            }
            return sprite;
        }

        bool _TryGetSprite(string atlasTag, string spriteName, out Sprite outSprite)
        {
            if (string.IsNullOrEmpty(spriteName))
                goto NOT_FOUND;
            if (_dicSprite.TryGetValue(spriteName, out outSprite) && outSprite != null) return true;

            if (!_dicSpriteAtlas.TryGetValue(atlasTag, out var atlas))
            {
                outSprite = null;
                return false;
            }

            var sprite = atlas.GetSprite(spriteName);
            if (sprite)
            {
                outSprite = sprite;
                _dicSprite.Add(spriteName, sprite);
                return true;
            }

        NOT_FOUND:
            outSprite = null;
            return false;
        }

        private static ProfilerMarker ProfilerMarker_TryGetSprite = new ProfilerMarker("ProjectHD.AtlasLoader.TryGetSprite");
        public static bool TryGetSprite(string spriteName, out Sprite outSprite)
        {
            using (ProfilerMarker_TryGetSprite.Auto())
            {
                return Instance._TryGetSprite(spriteName, out outSprite);
            }
        }

        public static bool TryGetSpriteFromIcon(string spriteName, out Sprite outSprite)
        {
            using (ProfilerMarker_TryGetSprite.Auto())
            {
                return Instance._TryGetSprite(Icons, spriteName, out outSprite);
            }
        }

        public async static UniTask<bool> TryGetSpriteFromIconAsync(string spriteName, Image image)
        {
            var sprite = await Instance.TryGetSpriteAsync(Icons, spriteName);
            if (image == null)
            {
                InternalDebug.LogError($"Cannot Find Image({spriteName})");
                return false;
            }

            if (sprite != null)
            {
                image.overrideSprite = sprite;
            }
            else
            {
                InternalDebug.LogError($"Cannot Find Sprite({spriteName})");
            }

            return sprite != null;
        }

        public static Sprite GetSprite(string spriteName)
        {
            return Instance._TryGetSprite(spriteName, out var outSprite) ? outSprite : null;
        }

        private static bool TryGetSprite(string atlasTag, string spriteName, out Sprite outSprite)
        {
            return Instance._TryGetSprite(atlasTag, spriteName, out outSprite);
        }
    }
}