using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingPlayer : MonoBehaviour
{
    public Camera playerCamera;
    
    public GameObject shootEffect;

    private int damage = 10;

    private TakeDmage TakeDmage_;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)) 
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Vector3 shootDirectiom = playerCamera.transform.forward;

        Vector3 rayOrigin = playerCamera.transform.position;

        RaycastHit hitInfo;

        bool hit = Physics.Raycast(rayOrigin, shootDirectiom, out hitInfo);

        if (hit)
        {
            Debug.Log("Попал в " +  hitInfo.collider.gameObject.name);
        }

        if(shootEffect != null)
        {
            Quaternion impactRotation = Quaternion.LookRotation(hitInfo.normal);
            Instantiate(shootEffect, hitInfo.point, impactRotation);
        }

        if (hitInfo.collider.gameObject.name == "dummy")
        {
            TakeDmage_ = hitInfo.collider.gameObject.GetComponent<TakeDmage>();
            // Если компонент есть, наносим урон
            TakeDmage_.GetDamage(damage);
            
        }
    }
}
