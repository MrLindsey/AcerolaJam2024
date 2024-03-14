//============================================================
// For Acerola Game Jam 2024
// --------------------------
// Copyright (c) 2024 Ian Lindsey
// This code is licensed under the MIT license.
// See the LICENSE file for details.
//============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlugLogic : MonoBehaviour
{
    [SerializeField] private Transform _cableEnd;
    [SerializeField] private CursorGrabInteractor _grabInteractor;
    [SerializeField] private float _maxRange = 2.0f;
    [SerializeField] private float _snapForce = 1.0f;

    private Rigidbody _physics;

    public Transform GetCableEnd() { return _cableEnd; }

    // Start is called before the first frame update
    void Start()
    {
        _physics = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_physics.isKinematic == false)
        {
            // Check if the cable is at full range
            Vector3 diff = transform.position - _cableEnd.position;
            float dist = Vector3.Magnitude(diff);
            if (dist >= _maxRange)
            {
                Vector3 force = diff.normalized * _snapForce;
                // Snap it back and force the player to let go
                _grabInteractor.ForceReleaseGrab();

                // Force the cable to be at the max stretch & push it back
                _physics.velocity = Vector3.zero;
                transform.position = _cableEnd.position + (diff.normalized * dist);

                _physics.AddForce(-force);
            }
        }
    }
}
