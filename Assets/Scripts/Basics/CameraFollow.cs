using UnityEngine;
using Photon.Pun;

public class CameraFollow : MonoBehaviour
{
    public Vector3 offset = new Vector3(7, 10, -7);
    public float smoothSpeed = 1f;
    private Transform target;

    void LateUpdate()
    {
        // 如果目标为空，则尝试获取
        if (target == null)
        {
            if (PhotonNetwork.InRoom)
            {
                // 联机模式：通过单例获取本地玩家
                if (PlayerMain.Instance != null)
                    target = PlayerMain.Instance.transform;
            }
            else
            {
                // 单人模式：通过标签查找玩家
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    target = playerObj.transform;
            }
            // 如果仍为空，跳过本帧
            if (target == null) return;
        }

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}