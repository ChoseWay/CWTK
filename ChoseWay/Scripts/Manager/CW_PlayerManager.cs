using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ChoseWay.Manager
{
    public class CW_PlayerManager : MonoBehaviour
    {
        public static CW_PlayerManager instance;
        private void Awake()
        {
            instance = this;
        }
        //玩家
        public Transform trans_Player;
        public Transform trans_PlayerCamera;
        public TextMesh textHUD;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}