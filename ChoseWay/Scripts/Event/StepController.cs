using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ChoseWay.Event
{



    /// <summary>
    /// 结构体-Step事件
    /// </summary>
    [System.Serializable]
    public struct MyEvent
    {
        public string name;
        [SerializeField]
        private float delay;
        public float Delay
        {
            get
            {
                if (delay < 0)
                    return 0;
                else
                    return delay;
            }
            set
            {
                if (Delay < 0)
                    delay = 0;
                else
                    delay = Delay;
            }
        }
        public UnityEvent stepEvent;
    }
    /// <summary>
    /// Step事件控制器
    /// </summary>
    public class StepController : MonoBehaviour
    {
        [BoxGroup("使用指南")]
        [HideLabel]
        [DisplayAsString]
        public string Infomation = "点击最下面按钮可以在运行中直接执行该事件";



        [Header("是否在打开时自动执行")]
        public bool enableOnEnable;
        [Header("任务名称")]
        public string stepName;
        [Header("任务清单")]
        public List<MyEvent> list_MyEvent;
        [HideInInspector]
        public int index;


        private void Start()
        {
            if (enableOnEnable)
            {
                DoStep(0);
            }
        }


        [Button("执行",ButtonSizes.Large,ButtonStyle.FoldoutButton)]
        //[Button("@\"Time:\"+DataTime.Now.Tostring(\"HH:mm:ss\")")]
        /// <summary>
        /// 执行事件
        /// </summary>
        /// <param name="step">步骤编号(0为执行全部事件)</param>
        public void DoStep(int step)
        {
            Debug.Log("StepController:  "+this.name+"已执行");
            if (step == 0)
            {
                for (int i = 0; i < list_MyEvent.Count; i++)
                {
                    if (list_MyEvent[i].Delay > 0)
                    {
                        StartCoroutine(IEDelayDoStep(list_MyEvent[i], list_MyEvent[i].Delay));
                    }
                    else
                    {
                        list_MyEvent[i].stepEvent.Invoke();
                    }
                }
            }
            else if (step > 0)
            {
                Debug.Log(list_MyEvent.Count);
                if (list_MyEvent[step - 1].Delay > 0)
                {
                    StartCoroutine(IEDelayDoStep(list_MyEvent[step - 1], list_MyEvent[step - 1].Delay));
                }
                else
                {
                    list_MyEvent[step - 1].stepEvent.Invoke();
                }
            }
        }

        IEnumerator IEDelayDoStep(MyEvent myEvent, float delay)
        {
            yield return new WaitForSeconds(delay);
            myEvent.stepEvent.Invoke();
        }

    }
}