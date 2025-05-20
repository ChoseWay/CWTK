using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChoseWay.Interaction.Button;

namespace ChoseWay.Interaction.Ray
{
    [RequireComponent(typeof(LineRenderer))]
    public class CW_Ray : MonoBehaviour
    {
        public LineRenderer line;
        /// <summary>
        /// 是否渲染射线
        /// </summary>
        public bool isShowLine = true;
        /// <summary>
        /// 是否开启射线
        /// </summary>
        public bool isOpenRay = true;

        UnityEngine.Ray ray;
        RaycastHit hit;
        GameObject obj_LastHit;
        GameObject obj_CurrentHit;

        void Start()
        {
            line = GetComponent<LineRenderer>();
        }

        void Update()
        {
            //如果开启射线检测
            if (isOpenRay)
            {
                //生成射线
                ray = new UnityEngine.Ray(transform.position, transform.forward * 100);
                //检测到射线碰撞
                if (Physics.Raycast(ray, out hit, 10))
                {
                    //储存CurrentHit
                    obj_CurrentHit = hit.collider.gameObject;
                    //若LastHit和CurrentHit不同
                    if (obj_LastHit != obj_CurrentHit)
                    {
                        Debug.LogWarning("last:" + obj_LastHit + "+   current:" + obj_CurrentHit);
                        //若LastHit已储存
                        if (obj_LastHit != null)
                        {
                            ColliderCheck(obj_LastHit, false);
                        }
                        //更新LastHit对象
                        obj_LastHit = obj_CurrentHit;
                        ColliderCheck(obj_CurrentHit, true);
                    }

                    //检测用户输入
                    InputCheck(obj_CurrentHit);
                }
                else
                {
                    if (obj_CurrentHit != null)
                    {
                        ColliderCheck(obj_LastHit, false);
                        obj_LastHit = obj_CurrentHit;
                        obj_CurrentHit = null;
                        Debug.Log("No Hit");
                    }
                }

                //如果渲染射线
                if (isShowLine)
                {
                    //如果射线碰撞到物体
                    if (hit.collider != null)
                    {
                        line.SetPosition(0, gameObject.transform.position);
                        line.SetPosition(1, hit.point);
                    }
                    //没碰到时
                    else
                    {
                        line.SetPosition(0, gameObject.transform.position);
                        line.SetPosition(1, gameObject.transform.position + 5 * gameObject.transform.forward);
                    }
                }
                //不渲染射线时
                else
                {
                    line.enabled = false;
                }
            }
            //关闭射线检测时
            else
            {
                line.enabled = false;
            }
        }


        public void ColliderCheck(GameObject obj_Hit, bool isHit)
        {
            //当检测为按钮时
            if (obj_Hit != null && obj_Hit.tag == "Button")
            {
                if (isHit)
                {
                    obj_Hit.GetComponent<CW_ButtonController>().OnEnter();
                }
                else
                {
                    obj_Hit.GetComponent<CW_ButtonController>().OnExit();
                }
            }
        }
        public void InputCheck(GameObject obj_Hit)
        {
            //使用Oculus SDK射线输入的方法
            //if (OVRInput.Get(OVRInput.RawButton.LIndexTrigger)
            //     ||
            //     OVRInput.Get(OVRInput.RawButton.RIndexTrigger)
            //     ||
            //     Input.GetKeyDown(KeyCode.Space))
            //{
            //    if (obj_Hit != null && obj_Hit.tag == "Button")
            //    {
            //        obj_Hit.GetComponent<CW_ButtonController>().Excute();
            //        Debug.Log("Press Trigger");
            //    }
            //}
        }
    }
}