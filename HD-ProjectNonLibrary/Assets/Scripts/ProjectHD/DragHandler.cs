using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectHD
{
    [RequireComponent(typeof(EventTrigger))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class DragHandler : MonoBehaviour
    {
        bool _isDragging = false;
        public event System.Action<Vector3> OnDragAction;
        public event System.Action<bool> OnDragStateChangedAction;

        [SerializeField] CapsuleCollider _capsuleCollider;

        public void OnBeginDrag(BaseEventData eventData)
        {
            _isDragging = true;
            OnDragStateChangedAction?.Invoke(true);
            Debug.Log("OnBeginDrag");
        }

        public void OnDrag(BaseEventData eventData)
        {
            if (_isDragging)
            {
                OnDragAction?.Invoke(Input.mousePosition);
                Debug.Log("OnDrag");
            }
        }

        public void OnEndDrag(BaseEventData eventData)
        {
            _isDragging = false;
            OnDragStateChangedAction?.Invoke(false);
            Debug.Log("OnEndDrag");
        }

        public void EventClear()
        {
            OnDragAction = null;
            OnDragStateChangedAction = null;
        }

        [Button]
        private void SetComponent()
        {
            _capsuleCollider = GetComponent<CapsuleCollider>();
        }

        [Button]
        private void SetColliderSize()
        {
            if (!_capsuleCollider)
                return;

            var parent = transform.parent;
            if (!parent)
                return;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child == transform)
                    continue;
                if (child.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
                {
                    var bounds = skinnedMeshRenderer.bounds;
                    var radius = (bounds.extents.x + bounds.extents.z) / 3;
                    var height = bounds.extents.y * 2;
                    var offSetY = height > 3 ? 2 : 1;
                    _capsuleCollider.center = new Vector3(0, offSetY, 0);
                    _capsuleCollider.radius = radius;
                    _capsuleCollider.height = height;
                    return;
                }
            }
        }
    }
}