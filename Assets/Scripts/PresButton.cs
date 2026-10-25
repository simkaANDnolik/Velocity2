using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PresButton : MonoBehaviour
{
    public GameObject dummy;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P) || Input.GetKeyUp(KeyCode.LeftControl))
        {
            dummy.SetActive(true);

            TakeDmage.Health = 100;
        }
    }

}
