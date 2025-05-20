using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

namespace ChoseWay.Interaction.Button
{
    public class CW_ButtonController : MonoBehaviour
    {
        Transform Trans_ButtonPic;
        Material mat_ButtonPic;
        Collider collider_Button;
        [Header("属性")]
        public bool isEnabled = true;
        /// <summary>
        /// 是否是一次性的
        /// </summary>
        public bool isThrowaway = true;
        public bool isHide = false;
        [Header("跟随摄像机")]
        public bool isForwardCamera = true;
        Transform target;
        public float speed_ForwardCamera=5;

        [Header("动画效果")]
        public float duration = 0.1f;
        float size_Enlarge = 1.1f;
        public Color color_Normal = Color.white;
        public Color color_OnEnter = new Color(0, 0.618f, 0, 1);
        public Color color_OnPressed = new Color(0.618f, 0.618f, 0, 1);
        public Color color_OnSelected = new Color(0.618f, 0.618f, 0.618f, 1);
        public Color color_OnDisable = new Color(0, 0, 0, 1);
        public UnityEvent Event_OnPressed;
        void Start()
        {
            target = ChoseWay.Manager.CW_PlayerManager.instance.trans_PlayerCamera;
            //取Transform对象
            if (transform.childCount > 0)
            {
                Trans_ButtonPic = transform.GetChild(0);
            }
            else
            {
                Trans_ButtonPic = transform;
            }
            //取Material对象
            if (Trans_ButtonPic.TryGetComponent<Renderer>(out Renderer renderer))
            {
                mat_ButtonPic = renderer.material;
            }
            collider_Button = transform.GetComponent<Collider>();
        }

        void Update()
        {
            transform.LookAt(target.position, Vector3.up);
            CalculateDistance();
        }
        public void InitButton()
        {
            //gameObject.SetActive(true);
            transform.localScale = Vector3.zero;
            transform.DOScale(1, duration).OnComplete(() => collider_Button.enabled = true);
            isEnabled = true;
        }
        public void HideButton()
        {
            transform.DOScale(0, duration).OnComplete(() => collider_Button.enabled = false);
            isEnabled = false;

        }
        float distance_Camera;
        public void CalculateDistance()
        {
            distance_Camera = Vector3.Distance(transform.position, target.position);
            if (distance_Camera > 10 && isEnabled)
            {
                HideButton();
            }
            else if (distance_Camera < 8 && !isEnabled)
            {
                InitButton();
            }
        }



        public void OnEnter()
        {
            if (isEnabled)
            {
                //Debug.Log(gameObject.name + "  EnterButton");
                transform.DOScale(size_Enlarge, duration);
                mat_ButtonPic.DOColor(color_OnEnter, duration);
            }
        }
        public void OnExit()
        {
            if (isEnabled)
            {
                //Debug.Log(gameObject.name + "   ExitButton");
                transform.DOScale(1, duration);
                mat_ButtonPic.DOColor(color_Normal, duration);
            }
        }
        public void OnStay()
        {

        }
        public void OnPressed()
        {
            if (isEnabled)
            {
                //Debug.Log(gameObject.name + "   ExitButton");
                Sequence sequence = DOTween.Sequence();
                sequence.Append(transform.DOScale(1, duration));
                sequence.Insert(0, mat_ButtonPic.DOColor(color_OnPressed, duration));
                sequence.Append(transform.DOScale(size_Enlarge, duration));
                sequence.Insert(duration, mat_ButtonPic.DOColor(color_OnSelected, duration));
                if (isThrowaway)
                {
                    collider_Button.enabled = false;
                    if (isHide)
                    {
                        sequence.Append(transform.DOScale(0, duration));
                    }
                }
                isEnabled = false;
            }
        }
        public void Excute()
        {
            OnPressed();
            Event_OnPressed.Invoke();
        }
    }


}