using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class StatusEffectManager : MonoBehaviourPun
{
    private class ActiveEffect
    {
        public string id;
        public float damagePerTick;
        public float tickInterval;
        public int remainingTicks;
        public float nextTickTime;
        public GameObject source;
        public DamageType type;
        public bool isHeal;
    }

    private List<ActiveEffect> effects = new List<ActiveEffect>();
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void Update()
    {
        float time = Time.time;
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            ActiveEffect e = effects[i];
            if (time >= e.nextTickTime)
            {
                ApplyEffectTick(e);
                e.remainingTicks--;
                if (e.remainingTicks <= 0)
                {
                    effects.RemoveAt(i);
                }
                else
                {
                    e.nextTickTime = time + e.tickInterval;
                }
            }
        }
    }

    private void ApplyEffectTick(ActiveEffect e)
    {
        if (health == null) return;
        if (e.isHeal)
        {
            health.Heal(e.damagePerTick);
        }
        else
        {
            // 通过 Health 的伤害接口，传入 source 确保击杀归属
            health.TakeDamage(e.damagePerTick, e.source, e.type);
        }
    }

    public void AddEffect(string id, float totalDamage, float duration, float tickInterval, GameObject source, DamageType type = DamageType.Physical, bool isHeal = false)
    {
        int ticks = Mathf.CeilToInt(duration / tickInterval);
        float damagePerTick = totalDamage / ticks;

        ActiveEffect effect = new ActiveEffect
        {
            id = id,
            damagePerTick = damagePerTick,
            tickInterval = tickInterval,
            remainingTicks = ticks,
            nextTickTime = Time.time + tickInterval,
            source = source,
            type = type,
            isHeal = isHeal
        };

        effects.Add(effect);

        // 网络同步（可选）
        if (photonView.IsMine && PhotonNetwork.InRoom)
        {
            int sourceViewID = source != null ? source.GetComponent<PhotonView>().ViewID : -1;
            photonView.RPC("RPC_AddEffect", RpcTarget.Others, id, totalDamage, duration, tickInterval, sourceViewID, (int)type, isHeal);
        }
    }

    [PunRPC]
    private void RPC_AddEffect(string id, float totalDamage, float duration, float tickInterval, int sourceViewID, int typeInt, bool isHeal)
    {
        GameObject source = sourceViewID != -1 ? PhotonView.Find(sourceViewID)?.gameObject : null;
        AddEffect(id, totalDamage, duration, tickInterval, source, (DamageType)typeInt, isHeal);
    }

    public void RemoveEffect(string id)
    {
        effects.RemoveAll(e => e.id == id);
    }
}