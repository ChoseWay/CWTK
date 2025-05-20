using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ChoseWay.UI
{
    public class CW_UI_Window : MonoBehaviour
    {
        public bool isShowInActive = false;
        Vector3 scale_Origin;
        public float duration = 0.2f;


        [Header("内容")]
        public Text text_Title;
        public Image image;
        public Text text_Content;
        [TextArea(3, 10)]
        [SerializeField]
        public string string_Content;
        void Start()
        {
            scale_Origin = transform.localScale;
            gameObject.SetActive(isShowInActive);
            text_Content.text = string_Content;
        }

        void Update()
        {

        }
        public void OpenWindow()
        {
            gameObject.SetActive(true);
            transform.localScale = Vector3.zero;
            transform.DOScale(scale_Origin, duration);
        }
        public void CloseWindow()
        {
            transform.DOScale(0, duration).OnComplete(() => gameObject.SetActive(true));
        }
    }
}