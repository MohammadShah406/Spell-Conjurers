using UnityEngine;

public class Player : MonoBehaviour
{
    private ICustomBehavior behavior;
    private Currency currency;

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
