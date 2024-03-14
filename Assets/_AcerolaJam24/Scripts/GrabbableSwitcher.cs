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

public class GrabbableSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject[] _grabbableObjects;

    private int _defaultLayer;
    private int _grabbableLayer;

    public void SetGrabbable(bool onOff)
    {
        foreach(GameObject grabbable in _grabbableObjects)
        {
            if (onOff)
                grabbable.layer = _grabbableLayer;
            else
                grabbable.layer = _defaultLayer;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _defaultLayer = LayerMask.NameToLayer("Default");
        _grabbableLayer = LayerMask.NameToLayer("Grabbable");
    }

}
