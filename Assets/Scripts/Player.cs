using UnityEngine;

public class Player : MonoBehaviour
{
    private ICustomBehavior behavior;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float moveSpeed = 5f;

    void Start()
    {
        behavior?.Start(this);
    }

    void Update()
    {
        behavior?.Update(this);
        
    }

    public void SetBehavior(ICustomBehavior newBehavior)
    {
        behavior = newBehavior;
        behavior?.Start(this);
    }
}
