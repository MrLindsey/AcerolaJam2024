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

public class ParticleLogic : MonoBehaviour
{
    [SerializeField] float _particleTime = 4.0f;

    // Start is called before the first frame update
    void Start()
    { 
        Invoke("KillEffect", _particleTime);
    }

    private void KillEffect()
    {
        Destroy(gameObject);
    }

   
}
