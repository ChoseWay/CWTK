using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using ChoseWay.Manager;
namespace ChoseWay.Player
{
    public class CW_PlayerController : MonoBehaviour
    {
        Transform trans_Player;
        private void Awake()
        {
        }
        void Start()
        {
            CW_PlayerManager.instance.trans_Player = transform;
            if (trans_Player == null)
            {
                trans_Player = transform;
            }
        }
        void Update()
        {




        }

    }
}