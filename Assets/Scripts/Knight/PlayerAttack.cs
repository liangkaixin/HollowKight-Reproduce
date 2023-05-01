using Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public ActionEffect prefabSlashImpact;          // ������Ч
    public ActionEffect prefabSwordHit;             // ����������Ч
    private Transform player;
    private void Awake()
    {
        player = GameObject.Find("Player").GetComponent<Transform>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Boss")
        {
            collision.GetComponent<Hornet>().takedamage(1);
            var impact = Instantiate(prefabSlashImpact, transform.position, Quaternion.identity);
            var attack = Instantiate(prefabSwordHit, transform.position, Quaternion.identity);
            
            // player��ʼlocalScale.x = -1
            if (player.localScale.x > 0)
            {
                impact.transform.localScale = new Vector3(-1, 1, 1);
                attack.transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }
}
