using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoliceAgent : MonoBehaviour
{
    private Rigidbody rb;
    private float speed = 30f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
    }

    //ランダムな方向に移動する。
    void randomMove() {
        float x = Random.Range(-1f, 1f);
        float z = Random.Range(-1f, 1f);
        Vector3 direction = new Vector3(x, 0, z);
        rb.AddForce(direction * speed);
    }
}
