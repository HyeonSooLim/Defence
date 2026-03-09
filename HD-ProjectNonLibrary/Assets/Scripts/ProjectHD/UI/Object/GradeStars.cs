using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectHD.UI
{
    public class GradeStars : MonoBehaviour
    {
        [SerializeField] private Image[] _gradeStarImages;
        [SerializeField] private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform;

        public void Construct(int grade)
        {
            for (int i = 0; i < _gradeStarImages.Length; i++)
            {
                bool isOn = i < grade;
                _gradeStarImages[i].gameObject.SetActive(isOn);
            }
        }

        public void Destruct()
        {
            for (int i = 0; i < _gradeStarImages.Length; i++)
            {
                bool isOn = false;
                _gradeStarImages[i].gameObject.SetActive(isOn);
            }
        }
    }
}