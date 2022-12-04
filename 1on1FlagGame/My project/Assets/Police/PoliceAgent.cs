using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoliceAgent1 : MonoBehaviour
{
    private Rigidbody rb;
    private float speed = 30f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb.velocity.magnitude < 10)
        {
            //指定したスピードから現在の速度を引いて加速力を求める
            float currentSpeed = speed - rb.velocity.magnitude;
            //調整された加速力で力を加える
            rb.AddForce(new Vector3(0, 0, currentSpeed));
        }
    }
}
