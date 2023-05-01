using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Needle : MonoBehaviour
{
    public Thread prefabNeedleThread;
    public float speed;
    public bool moveLeft = true;
    private float initSpeed = 60f;
    private float acceleration = 90f;
    private float movedTime = 0;
    private bool hasThread = false;

    private void Awake()
    {
        speed = initSpeed;
    }
    private void FixedUpdate()
    {
        speed = initSpeed - acceleration * movedTime;
        Vector3 move = transform.right * Time.fixedDeltaTime * speed;
        transform.position -= move;
        movedTime += Time.fixedDeltaTime;
        if (Mathf.Abs(speed) < 1f && !hasThread)
        {
            hasThread = true;
            Thread thread = Instantiate(prefabNeedleThread, transform.position + transform.right * 8
                , Quaternion.identity);
            thread.transform.SetParent(transform);
            if (!moveLeft)
            {
                thread.transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }
    public void Destroy()
    {
        Destroy(gameObject);
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        //针碰到hornet，发送回收完毕事件
        if (collision.tag == "Boss")
        {
            collision.GetComponent<PlayMakerFSM>().SendEvent("needle_received");
            Destroy(gameObject);
        }
        
    }
}