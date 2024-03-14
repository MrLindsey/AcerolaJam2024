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

public class DroneSectionLogic : MonoBehaviour
{
    [SerializeField] private DroneLogic _drone;
    [SerializeField] private SubDroneMan _droneMan;
    [SerializeField] private AudioSource _audio;

    public void StartAudio() { _audio.Stop(); _audio.Play(); }

    public void FlyToHome() { _drone.FlyToHome(); }
    public void ActivateDrones() { _droneMan.ActivateDrones(true); }
}
