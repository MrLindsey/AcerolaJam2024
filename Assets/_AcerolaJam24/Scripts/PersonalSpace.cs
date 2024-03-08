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

public class PersonalSpace : MonoBehaviour
{
    private int _layerToCheck;
    private Character _character;
    private int _numObjects = 0;    // Number of object in the personal space
    private bool _personalSpaceBreached = false;

    public void SetCharacter(Character character)
    {
        _character = character;
        _layerToCheck = LayerMask.NameToLayer("Grabbable");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == _layerToCheck)
        {
            if (_numObjects == 0)
                SetPersonalSpaceBreached(true);

            _numObjects++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == _layerToCheck)
        {
            _numObjects--;
            if (_numObjects <= 0)
            {
                _numObjects = 0;
                SetPersonalSpaceBreached(false);
            }
        }
    }

    private void SetPersonalSpaceBreached(bool onOff)
    {
        if (_personalSpaceBreached != onOff)
        {
            if (_character)
                _character.PersonalSpaceBreached(onOff);

            _personalSpaceBreached = onOff;
        }
    }
}
