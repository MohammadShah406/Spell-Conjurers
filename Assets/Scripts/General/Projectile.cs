using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject target;
    public Stats targetStats;
    public Vector3 targetTransform;
    public bool isDestroyed = false;

    void Update()
    {
        if (target == null || targetStats == null || targetStats.isDead)
        {
            if(Vector3.Distance(transform.position, targetTransform) < 0.05f)
                Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        isDestroyed = true;
    }
}
