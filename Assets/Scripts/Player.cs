using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public List<ICustomBehavior> skills = new List<ICustomBehavior>();
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float moveSpeed = 5f;

    //void Start()
    //{
    //    behavior?.Start(this);
    //}

    void Update()
    {
        //behavior?.Update(this);
        foreach (var skill in skills)
        {
            skill?.Update(this);
        }
    }

    //public void SetBehavior(ICustomBehavior newBehavior)
    //{
    //    behavior = newBehavior;
    //    behavior?.Start(this);
    //}
    public void AddSkill(ICustomBehavior newSkill)
    {
        if (newSkill != null)
        {
            skills.Add(newSkill);
            newSkill.Start(this); // optional: initialize it
            Debug.Log($"Added new skill. Total skills: {skills.Count}");
        }
    }
}
