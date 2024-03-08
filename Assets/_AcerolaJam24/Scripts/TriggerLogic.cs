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

public class TriggerLogic : MonoBehaviour
{
    [SerializeField] TaskMan _taskMan;
    [SerializeField] bool _oneShot = true;

    private void OnTriggerEnter(Collider other)
    {
        // Has the player entered this trigger, if so tell the task manager about it...
        if (other.CompareTag("Player"))
        {
            _taskMan.PlayerEnteredTrigger(transform);
            if (_oneShot)
                gameObject.SetActive(false);
        }
    }
}
