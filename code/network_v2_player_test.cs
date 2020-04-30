﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class network_v2_player_test : networked_v2
{
    public bool local;
    private void Update()
    {
        if (local)
        {
            if (Input.GetKey(KeyCode.W)) networked_position += Vector3.forward * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) networked_position -= Vector3.forward * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) networked_position += Vector3.right * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) networked_position -= Vector3.right * Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space))
                client.create(transform.position, "network_v2_test/bomb");
        }
        else
        {
            if (Input.GetKey(KeyCode.I)) networked_position += Vector3.forward * Time.deltaTime;
            if (Input.GetKey(KeyCode.K)) networked_position -= Vector3.forward * Time.deltaTime;
            if (Input.GetKey(KeyCode.L)) networked_position += Vector3.right * Time.deltaTime;
            if (Input.GetKey(KeyCode.J)) networked_position -= Vector3.right * Time.deltaTime;
        }
    }
}
