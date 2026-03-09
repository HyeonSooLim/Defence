using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectHD.Battle
{
    public class DiceBehavior : MonoBehaviour
    {
        [Tooltip("1~6까지 주사위 눈금에 해당하는 유닛 속성 매핑: " +
            "[주사위 1 회전값: 0,0,0] " +
            "[주사위 2 회전값: 0,90,0] " +
            "[주사위 3 회전값: 90,0,0] " +
            "[주사위 4 회전값: 0,-90,0] " +
            "[주사위 5 회전값: -90,0,0] " +
            "[주사위 6 회전값: 180,0,0] ")]
        [SerializeField] SerializedDictionary<int, ProjectEnum.UnitProperty> _diceValues;

        [Tooltip("주사위 회전 횟수 값 (최소:최대)")]
        [SerializeField] Vector2 _rotateCountRange = new Vector2(5, 10);
        [Tooltip("주사위 회전 시간 값 (최소:최대)")]
        [SerializeField] Vector2 _rotateDurationRange = new Vector2(0.5f, 1.5f);
        [Tooltip("주사위 점프 높이")]
        [SerializeField] float _jumpHeight = 200f;
        [Tooltip("주사위 점프 시간")]
        [SerializeField] float _jumpDuration = 0.3f;

        private readonly CancelToken _cancelToken = new();

        public void Construct()
        {
            Event.EventManager.AddListener<Event.RollTheDiceEvent>(OnRollTheDice);
            _cancelToken.SetToken();
        }

        public void Destruct()
        {
            _cancelToken.UnSetToken();
            transform.DOKill();
            transform.localRotation = Quaternion.identity;
            Event.EventManager.RemoveListener<Event.RollTheDiceEvent>(OnRollTheDice);
        }

        private void OnRollTheDice(Event.RollTheDiceEvent @event)
        {
            int diceValue = Random.Range(1, 7);
            ProjectEnum.UnitProperty property = _diceValues[diceValue];

            var randomRotateCount = Random.Range(_rotateCountRange.x, _rotateCountRange.y);
            var randomRotation = new Vector3(Random.Range(0, 360 * randomRotateCount), Random.Range(0, 360 * randomRotateCount), Random.Range(0, 360 * randomRotateCount));
            var randomDuration = Random.Range(_rotateDurationRange.x, _rotateDurationRange.y);

            transform.DOKill();
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            DOTween.Sequence()
                .Append(transform.DOLocalMoveZ(-_jumpHeight, _jumpDuration).SetEase(Ease.OutQuad))
                .Append(transform.DORotate(randomRotation, randomDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalRotateQuaternion(Quaternion.Euler(GetDiceRotation(diceValue)), 0.5f).SetEase(Ease.OutCubic))
                .Join(transform.DOLocalMoveZ(0, _jumpDuration).SetEase(Ease.InQuad))
                //.InsertCallback(_jumpDuration + randomDuration + 0.5f, () =>
                //{
                //    CameraShakeEvent(0.2f, 1f);
                //})
                .AppendCallback(() =>
                {
                    DiceResultBroadcast(property);
                }).SetUpdate(true).ToUniTask(cancellationToken: _cancelToken.Token);
        }

        private Vector3 GetDiceRotation(int diceValue)
        {
            return diceValue switch
            {
                1 => new Vector3(0, 0, 0),
                2 => new Vector3(0, 90, 0),
                3 => new Vector3(90, 0, 0),
                4 => new Vector3(0, -90, 0),
                5 => new Vector3(-90, 0, 0),
                6 => new Vector3(180, 0, 0),
                _ => Vector3.zero,
            };
        }

        private void DiceResultBroadcast(ProjectEnum.UnitProperty property)
        {
            var resultEvent = Event.Events.DiceResultEvent;
            resultEvent.Property = property;
            Event.EventManager.Broadcast(resultEvent);
        }

        private void CameraShakeEvent(float duration, float magnitude)
        {
            var cameraShakeEvent = Event.Events.CameraShakeEvent;
            cameraShakeEvent.Duration = duration;
            cameraShakeEvent.Magnitude = magnitude;
            Event.EventManager.Broadcast(cameraShakeEvent);
        }
    }
}