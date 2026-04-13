using UnityEngine;
using System.Collections.Generic;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;

    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(popupPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public void ShowPopup(Vector3 worldPos, float value, bool isCrit, bool isHeal = false)
    {
        if (pool.Count == 0) return;
        GameObject obj = pool.Dequeue();
        obj.SetActive(true);
        obj.transform.position = worldPos + Vector3.up * 1.5f;
        obj.GetComponent<DamagePopup>().Setup(value, isCrit, isHeal, this);
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}