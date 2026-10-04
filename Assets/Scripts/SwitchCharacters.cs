using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCharacters : MonoBehaviour
{
    static public int svitch = 0;
    public GameObject character1;
    public GameObject character2;

    void Start()
    {
        // Убедимся, что первый персонаж активен, а второй неактивен в начале
        if (character1 != null)
            character1.SetActive(true);
        if (character2 != null)
            character2.SetActive(false);
        svitch = 0;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.N))
        {
            if (svitch == 0)
            {
                if (character1 != null)
                    character1.SetActive(false);
                if (character2 != null)
                    character2.SetActive(true);
                svitch = 1;
            }
            else if (svitch == 1)
            {
                if (character2 != null)
                    character2.SetActive(false);
                if (character1 != null)
                    character1.SetActive(true);
                svitch = 0;
            }
        }
    }
}
