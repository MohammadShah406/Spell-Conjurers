using UnityEngine;

public class ManaCrystalMovement : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;

    private void OnEnable()
    {
        target = GameManager.Instance.players[0].transform;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 3f);
    }

    // Update is called once per frame
    void Update()
    {
        MoveTowardsTarget();
    }

    public void MoveTowardsTarget()
    {
        if (target == null)
        {
            return;
        }
        if(Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            Destroy(gameObject);
        }


        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

    }
}
