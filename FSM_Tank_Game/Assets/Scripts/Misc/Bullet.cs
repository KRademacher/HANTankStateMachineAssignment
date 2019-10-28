using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    //Explosion Effect
    public GameObject Explosion;

    public float Speed = 600.0f;
    public float LifeTime = 3.0f;
    public int damage = 25;

    void Start()
    {
        Destroy(gameObject, LifeTime);
    }

    void Update()
    {
        transform.position += 
			transform.forward * Speed * Time.deltaTime;       
    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        GameObject explosion = Instantiate(Explosion, contact.point, Quaternion.identity);
        Destroy(gameObject);
        Destroy(explosion, 2f);
    }
}