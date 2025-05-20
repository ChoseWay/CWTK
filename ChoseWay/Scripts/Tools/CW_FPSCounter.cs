using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChoseWay.Manager;
namespace ChoseWay.Tools 
{
    public class CW_FPSCounter : MonoBehaviour
    {
        float deltaTime = 0.0f;
        TextMesh textMesh;
        // Start is called before the first frame update
        void Start()
        {
            textMesh = CW_PlayerManager.instance.textHUD;
        }

        // Update is called once per frame
        void Update()
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            textMesh.text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        }
    }
}