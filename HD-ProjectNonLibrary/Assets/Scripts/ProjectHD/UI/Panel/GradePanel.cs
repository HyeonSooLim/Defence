using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.UI
{
    public class GradePanel : MonoBehaviour
    {
        private const string GRADE_STARS_ASSET_KEY = "Assets/GameResources/Prefabs/UI/GradeStars.prefab";
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRectTransform;
        [SerializeField] private float _starSize = 3f;
        [SerializeField] private Vector3 _starOffset;
        private Dictionary<int, GradeStars> _activeGradeStars;

        private void Awake()
        {
            _activeGradeStars = Utilities.StaticObjectPool.Pop<Dictionary<int, GradeStars>>();
            _activeGradeStars.Clear();
        }

        private void OnEnable()
        {
            Event.EventManager.AddListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
            Event.EventManager.AddListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.AddListener<Event.CharacterDisappearEvent>(CharacterDisappearAction);
            Event.EventManager.AddListener<Event.CharacterGradeUpEvent>(CharacterGradeUpAction);
            Event.EventManager.AddListener<Event.CharacterOnDraggingEvent>(CharacterOnDraggingAction);
            Event.EventManager.AddListener<Event.CharacterDragEndEvent>(CharacterDragEndAction);
        }

        private void OnDisable()
        {
            Event.EventManager.RemoveListener<Event.CharacterOnCellEvent>(CharacterOnCellAction);
            Event.EventManager.RemoveListener<Event.ManagerUnloadEvent>(ManagerUnloadAction);
            Event.EventManager.RemoveListener<Event.CharacterDisappearEvent>(CharacterDisappearAction);
            Event.EventManager.RemoveListener<Event.CharacterGradeUpEvent>(CharacterGradeUpAction);
            Event.EventManager.RemoveListener<Event.CharacterOnDraggingEvent>(CharacterOnDraggingAction);
            Event.EventManager.RemoveListener<Event.CharacterDragEndEvent>(CharacterDragEndAction);
        }

        private void CharacterOnCellAction(Event.CharacterOnCellEvent @event)
        {
            var instanceID = @event.InstanceID;
            if (!Runtime.StageInformation.SpawnedCharacters.TryGetValue(@event.InstanceID, out var characterData))
                return;

            if (!_activeGradeStars.ContainsKey(instanceID))
            {
                var gameObject = MainManager.Instance.GameObjectPool.Get(GRADE_STARS_ASSET_KEY);
                if (gameObject.TryGetComponent<GradeStars>(out var stars) == false)
                {
                    Utilities.InternalDebug.LogError($"[{nameof(GradePanel)}] {GRADE_STARS_ASSET_KEY} is not have GradeStars component");
                    return;
                }
                else
                {
                    stars.Construct(characterData.Grade);
                }

                stars.transform.SetParent(transform);
                stars.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                stars.transform.localScale = Vector3.one * _starSize;
                _activeGradeStars.Add(instanceID, stars);
            }

            GradeStarsSetPosition(instanceID);
        }

        private void ManagerUnloadAction(Event.ManagerUnloadEvent @event)
        {
            foreach (var gradeStars in _activeGradeStars.Values)
            {
                gradeStars.Destruct();
                MainManager.Instance.GameObjectPool.Return(gradeStars.gameObject);
            }
            _activeGradeStars.Clear();
            Utilities.StaticObjectPool.Push(_activeGradeStars);
            _activeGradeStars = null;
        }

        private void CharacterDisappearAction(Event.CharacterDisappearEvent @event)
        {
            var instanceID = @event.InstanceID;
            if (_activeGradeStars.TryGetValue(instanceID, out var gradeStars))
            {
                gradeStars.Destruct();
                MainManager.Instance.GameObjectPool.Return(gradeStars.gameObject);
                _activeGradeStars.Remove(instanceID);
            }
        }

        private void CharacterGradeUpAction(Event.CharacterGradeUpEvent @event)
        {
            var instanceID = @event.InstanceID;
            if (_activeGradeStars.TryGetValue(instanceID, out var gradeStars))
            {
                gradeStars.Construct(@event.NextGrade);
            }
        }

        private void CharacterOnDraggingAction(Event.CharacterOnDraggingEvent @event)
        {
            var instanceID = @event.InstanceID;
            GradeStarsSetPosition(instanceID);
        }

        private void CharacterDragEndAction(Event.CharacterDragEndEvent @event)
        {
            var instanceID = @event.InstanceID;
            GradeStarsSetPosition(instanceID);
        }

        private void GradeStarsSetPosition(int instanceID)
        {
            if (!Runtime.StageInformation.SpawnedCharacters.TryGetValue(instanceID, out var characterData))
                return;
            if (!_activeGradeStars.ContainsKey(instanceID))
                return;
            var gradeStars = _activeGradeStars[instanceID];
            var worldPosition = characterData.transform.position + _starOffset; // 캐릭터 머리 위로 오프셋
            var anchoredPosition = StaticMethod.WorldPositionToUIAnchoredPosition(worldPosition, _canvasRectTransform, _canvas.worldCamera);
            gradeStars.RectTransform.anchoredPosition = anchoredPosition;
        }
    }
}