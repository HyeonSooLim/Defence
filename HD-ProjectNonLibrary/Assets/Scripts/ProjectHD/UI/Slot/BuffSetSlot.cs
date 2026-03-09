using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD.UI
{
    public class BuffSetSlot : MonoBehaviour
    {
        [SerializeField] private Image Icon;
        [SerializeField] private TMP_Text CountText;

        public void Construct(string iconNameKey, string countText)
        {
            if (AtlasLoader.TryGetSprite(iconNameKey, out var sprite))
                Icon.overrideSprite = sprite;

            CountText.SetText(countText);
        }

        public void Destruct()
        {
            Icon.overrideSprite = null;
            CountText.SetText(string.Empty);
        }
    }
}
