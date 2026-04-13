using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSmoothing : MonoBehaviourPun, IPunObservable
{
    [Header("平滑速度")]
    [SerializeField] private float positionLerpSpeed = 15f;
    [SerializeField] private float rotationLerpSpeed = 15f;
    [SerializeField] private float teleportThreshold = 5f; // 超过此距离直接传送

    // 网络接收的目标值
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (photonView.IsMine)
        {
            // 本地玩家不进行插值，禁用此脚本节省性能
            enabled = false;
            return;
        }

        // 初始化目标值为当前值
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 本地玩家：发送自己的位置、旋转、动画状态
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            // 发送行走动画状态（用于远程玩家显示正确动画）
            if (animator != null)
            {
                bool isWalking = animator.GetBool("isWalking");
                stream.SendNext(isWalking);
            }
            else
            {
                stream.SendNext(false);
            }
        }
        else
        {
            // 远程玩家：接收数据，存入目标值
            targetPosition = (Vector3)stream.ReceiveNext();
            targetRotation = (Quaternion)stream.ReceiveNext();

            if (animator != null)
            {
                bool isWalking = (bool)stream.ReceiveNext();
                animator.SetBool("isWalking", isWalking);
            }
        }
    }

    private void Update()
    {
        // 仅远程玩家执行平滑移动
        if (photonView.IsMine) return;

        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance > teleportThreshold)
        {
            // 重生或瞬移时直接设置位置
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
        }
    }
}