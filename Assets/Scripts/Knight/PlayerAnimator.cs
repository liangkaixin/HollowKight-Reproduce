using HutongGames.PlayMaker.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{

    public class PlayerAnimator : MonoBehaviour
    {
        private Animator anima;
        private PlayerController _player;
        public GameObject hitEffectPrefab;
        public GameObject hitCrackPrefab;

        float t = 0, t2 = 0;
        private float HurtCoolTime = 2;     //���˵���ȴʱ��,��ʼ����Ҫ��ȴ
        private float atkCoolTime = 2;


        void Start()
        {
            anima = GetComponent<Animator>();
            _player = GetComponent<PlayerController>();
            
        }

        // Update is called once per frame
        void Update()
        {
            if (_player.Input.X < 0)
            {
                transform.localScale = new Vector3(1, 1, 1); //flip
            }
            else if (_player.Input.X > 0)
            {
                transform.localScale = new Vector3(-1, 1, 1); //flip
            }

            float speed = Mathf.Abs(_player.Velocity.x);
            //ˮƽ�ٶȴ���0.5f������
            anima.SetFloat("Speed", speed);
            //�ڵذ��ϣ����Ұ���JumpDown
            if (_player.Input.JumpDown && _player.Grounded)
            {
                anima.ResetTrigger("Land");
                anima.SetTrigger("Jump");
            }
            // ���֡������ض���
            if (_player.LandingThisFrame && Time.time > 0.5f)
            {
                anima.SetTrigger("Land");
                
            }
            
            if (_player.hurtthisFrame && HurtCoolTime > 1f)
            {
                t = Time.time;
                anima.SetTrigger("Hurt");
                var hcp = Instantiate(hitCrackPrefab, transform.position - Vector3.up * 0.52f, Quaternion.identity);
                var hep = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                hcp.transform.SetParent(transform, true);   // ����Ч����Player
                hep.transform.SetParent(transform, true);
            }
            
            if (_player.Input.Attack && atkCoolTime > 0.15f)
            {
                t2 = Time.time;
                anima.SetTrigger("Atk");
            }
            atkCoolTime = Mathf.Abs(Time.time - t2);
            HurtCoolTime = Mathf.Abs(Time.time - t);
        }
    }

}



