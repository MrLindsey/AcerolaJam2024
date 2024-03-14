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

public class SubDroneMan : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private SubDroneLogic[] _subDrones;
    [SerializeField] private Transform[] _flyPoints;
    [SerializeField] private DroneLogic _mainDrone;

    private int _droneDeadCount;
    private AudioSource _audio;

    public void PlayAudioClip(AudioClip clip)
    {
        _audio.PlayOneShot(clip);

    }
    public void ActivateDrones(bool onOff)
    {
        _droneDeadCount = 0;
        foreach (SubDroneLogic drone in _subDrones)
            drone.ActivateDrone(onOff);
    }
    public Transform GetRandomTarget()
    {
        return _flyPoints[Random.Range(0, _flyPoints.Length)];
    }

    public void DroneDied()
    {
        _droneDeadCount++;
        if (_droneDeadCount >= _subDrones.Length)
        {
            // All the sub drones are dead
            _mainDrone.DroneSplitCompleted();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _audio = GetComponent<AudioSource>();
        foreach (SubDroneLogic drone in _subDrones)
            drone.Setup(this, _player);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
