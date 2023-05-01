using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hornet : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Material defaultMaterial;
    private bool faceLeft = true;
    private PlayMakerFSM playmakerFsm;
    private Needle needle;
    private float evadeCoolTime = 2f;
    private float t = 0f;
    private bool evade = false;

    public Material hitMaterial;
    public GameObject SplatEffectPrefab;
    public GameObject GDashEffectPrefab;
    public GameObject AirDashEffectPrefab;

    public Needle NeedlePrefab;
    public int hp;

    private void Awake()
    {
        EventCenter.Instance.AddEventListener<Transform>("EventPlayerPosChange", CheckDirection);
    }
    // Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
        defaultMaterial = sprite.material;
        playmakerFsm = GetComponent<PlayMakerFSM>();
    }

    // Update is called once per frame
    void Update()
    {
        if (evade)
        {
            if (faceLeft)
            {
                transform.position += 10f * Vector3.right * Time.deltaTime;
            }
            else
            {
                transform.position += 10f * Vector3.left * Time.deltaTime;
            }
        }
    }

    public void throwNeedle()
    {
        if (faceLeft)
            needle = Instantiate(NeedlePrefab, new Vector3(transform.position.x - 1.64f, 
                transform.position.y - 0.86f, transform.position.z), Quaternion.identity);
        else if (!faceLeft)
        {
            needle = Instantiate(NeedlePrefab, new Vector3(transform.position.x + 1.64f,
                transform.position.y - 0.86f, transform.position.z), Quaternion.identity);
            //设置针的飞行方向
            needle.transform.right = Vector3.left;
            needle.moveLeft = false;
        }
    }

    public void takedamage(int damage)
    {
        hp -= damage;
        //flash
        Instantiate(SplatEffectPrefab, transform.position, Quaternion.identity);
        sprite.material = hitMaterial;
        //一段时间后恢复原有材质
        StartCoroutine(resetMaterial(0.1f));

    }

    //time秒内恢复原有材质
    IEnumerator resetMaterial(float time)
    {
        yield return new WaitForSeconds(time);
        sprite.material = defaultMaterial;
    }

    void CheckDirection(Transform playerpos)
    {
        if (playerpos.position.x > transform.position.x)
            faceLeft = false;
        else
            faceLeft = true;
        flip();
        //更新状态机player位置
        FsmVector3 myVector3 = playmakerFsm.Fsm.GetFsmVector3("playerpos");
        myVector3.Value = playerpos.position;
        FsmVector3 playerpos_x = playmakerFsm.Fsm.GetFsmVector3("playerpos_x");
        playerpos_x.Value = new Vector3(playerpos.position.x, -7.45f, playerpos.position.z); // 这个坐标是只获取了player的x值，-7.45是player在地面上的y轴。用于airdash定位目标。

        // 在此时判断安全距离
        if (Mathf.Abs(transform.position.x - playerpos.position.x) < 8f && evadeCoolTime >= 1.5f)
        {
            
            playmakerFsm.SendEvent("evade");
            if (playmakerFsm.ActiveStateName == "evade Antic") evade = true;
        }
        else if (Mathf.Abs(transform.position.x - playerpos.position.x) >= 8f)
        {
            playmakerFsm.SendEvent("evade finished");
            evade = false;
            t = Time.time;
        }
        evadeCoolTime = Mathf.Abs(Time.time - t);

    }

    void flip()
    {
        if (faceLeft)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x),
                transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                transform.localScale.y, transform.localScale.z);

    }

    public void GDashEffect()
    {
        if (faceLeft)
            Instantiate(GDashEffectPrefab, transform.position - 2.66f * Vector3.up, Quaternion.identity);
        else
            Instantiate(GDashEffectPrefab, transform.position - 2.66f * Vector3.up, Quaternion.Euler(0, 180, 0));
    }

    public void AirDashEffect()
    {
        if (faceLeft)
            Instantiate(AirDashEffectPrefab, transform.position - 2.66f * Vector3.up, Quaternion.identity);
        else
            Instantiate(AirDashEffectPrefab, transform.position - 2.66f * Vector3.up, Quaternion.Euler(0, 180, 0));

    }
}
