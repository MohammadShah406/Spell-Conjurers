using UnityEngine;

public class RoundFinishedView : MonoBehaviour
{
    public static RoundFinishedView Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoToShop()
    {
        UIController.Instance.SwitchUI(UIIndex.UnitEditCurrentSpell);
    }

    public void NextRound()
    {
        GameManager.Instance.roundNo += 1;
        GameManager.Instance.CleanUpPlayer();
        UIController.Instance.SwitchUI(UIIndex.Game);
    }
}
