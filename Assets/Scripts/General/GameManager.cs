using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public List<GameObject> player = new List<GameObject>();
    public List<GameObject> enemies = new List<GameObject>();




    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;          
            DontDestroyOnLoad(gameObject);
        }      
    }


}
