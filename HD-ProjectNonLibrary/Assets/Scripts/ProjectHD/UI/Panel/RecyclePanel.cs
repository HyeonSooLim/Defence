using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectHD.UI
{
    public class RecyclePanel : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Color _hightColor = Color.red;
        [SerializeField] private Color _normalColor = Color.white;

        public void PointerEnter(BaseEventData eventData)
        {
            _iconImage.color = _hightColor;
            ExecuteRecycleOnEvent(true);
        }

        public void PointerExit(BaseEventData eventData)
        {
            _iconImage.color = _normalColor;
            ExecuteRecycleOnEvent(false);
        }

        private void ExecuteRecycleOnEvent(bool isOn)
        {
            var tempEvent = Event.Events.RecycleEnterEvent;
            tempEvent.IsOn = isOn;
            Event.EventManager.Broadcast(tempEvent);
        }
    }
}