using UnityEngine;

public class ActionManager : MonoBehaviour 
{
    public static ActionManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void UseSpell(Spell spell, GameObject from, GameObject to)
    {
        int roll = Random.Range(0, 101);
        if (roll > spell.accuracy)
        {
            Debug.Log("Spell whiffed");
            return;
        }


        //string actionLog = "";
        //if (spell.damage > 0)
        //{
        //    to.GetComponent<Stats>().takeDamage(spell.damage);
        //    actionLog += from.name + " dealt " + spell.damage + " to " + to.name + ". ";
        //}
        //if (spell.selfDamage > 0)
        //{
        //    from.GetComponent<Stats>().takeDamage(spell.selfDamage);
        //    actionLog += from.name + " dealt " + spell.selfDamage + " to itself. ";
        //}

        SpellFunction.Instance.SetVariables(spell, from, to);
        SpellFunction.Instance.CustomSpellFunction();

    }
}


