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

public class FuseMan : MonoBehaviour
{
    [SerializeField] CharacterMan _characterMan;
    [SerializeField] TaskMan _taskMan;
    [SerializeField] Transform[] _fuseSpawners;
    [SerializeField] Transform[] _fuses;
    [SerializeField] float _respawnTime = 4.0f;
    [SerializeField] float _respawnDecreasePerFuse = 0.25f;
    [SerializeField] SocketLogic _fuseBoxSocket;
    [SerializeField] ActingClip _sneezeClip;
    [SerializeField] AudioClipDef[] _sneezeAudioDefs;

    private float _respawnTimer;
    private List<Transform> _activeFuses = new List<Transform>();
    private List<Transform> _randomisedSpawners = new List<Transform>();

    private bool _isActive = false;
    private bool _fulfilledTask1 = false;
    private bool _fulfilledTask2 = false;

    private int _numFusesAttached;
    private float _respawnTimeModified;

    private Character _character;

    public void Activate()
    {
        _respawnTimer = _respawnTime;
        _isActive = true;

        _character = _characterMan.GetCurrentCharacter();
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform fuse in _fuses)
            _activeFuses.Add(fuse);

        foreach (Transform spawner in _fuseSpawners)
            _randomisedSpawners.Add(spawner);

        SpawnFuses();
        _respawnTimer = 0;
        _respawnTimeModified = _respawnTime;
    }

    void SpawnFuses()
    {
        ShuffleList(_randomisedSpawners);
        for (int i=0;i< _activeFuses.Count;++i)
        {
            Transform fuse = _activeFuses[i];
            Transform spawner = _randomisedSpawners[i];

            Rigidbody physics = fuse.GetComponent<Rigidbody>();
            if (physics.freezeRotation == false)
            {
                fuse.position = spawner.position;
                fuse.localRotation = spawner.localRotation;
            }
        }
        _respawnTimer = _respawnTimeModified;

        if (_character != null)
        {
            AudioClipDef sneeze = _sneezeAudioDefs[Random.Range(0, _sneezeAudioDefs.Length)];
            _character.PlayActingClip(_sneezeClip, sneeze);
        }
    }

    // Function to shuffle the list using the Fisher-Yates shuffle algorithm
    void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        // Debug
        if (Input.GetKeyDown(KeyCode.E))
        {
            _isActive = true;
            SpawnFuses();
        }
#endif

        if (_isActive)
        {
            if (_respawnTimer > 0.0f)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0.0f)
                    SpawnFuses();
            }
        }

        // Change the fuse spawn rate depending on how many fuses there are in the box
        if (_numFusesAttached != _fuseBoxSocket.GetNumFusedObjects())
        {
            _numFusesAttached = _fuseBoxSocket.GetNumFusedObjects();
            _respawnTimeModified = _respawnTime - (_respawnDecreasePerFuse * _numFusesAttached);
        }

        // Check if we've fulfilled the tasks
        if (_fulfilledTask1 == false)
        {
            if (_fuseBoxSocket.GetNumFusedObjects() > 0)
            {
                _taskMan.CompleteFromScript("FuseInBox");
                _fulfilledTask1 = true;
            }
        }
        else
        {
            if (_fulfilledTask2 == false)
            {
                if (_fuseBoxSocket.IsSocketFull())
                {
                    _taskMan.CompleteFromScript("FillFusebox");
                    _fulfilledTask2 = true;
                    _isActive = false;
                }
            }
        }
    }
}
