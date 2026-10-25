using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TakeDmage : MonoBehaviour
{
    public TextMeshProUGUI hp;
    static public int Health = 100;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GameObject targetObject = gameObject;
        hp.text = Health.ToString();

        if (Health <= 0)
        {
            Die();
        }
    }

    public void GetDamage(int damage)
    {
        Health -= damage;
    }

    void Die()
    {
        gameObject.SetActive(false);

    }
}
