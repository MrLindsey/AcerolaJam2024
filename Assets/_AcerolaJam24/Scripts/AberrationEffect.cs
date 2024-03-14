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

public class AberrationEffect : MonoBehaviour
{
    [SerializeField] private float _distance = 0.1f;
    [SerializeField] private float _effectTime = 0.5f;
    [SerializeField] private Material _leftMaterial;
    [SerializeField] private Material _rightMaterial;
    [SerializeField] private Material _upMaterial;

    private float _timer;
    private Mesh _mesh;
    private Transform _camera;

    public void DoAberrationEffect()
    {
        _timer = _effectTime;
    }

    // Start is called before the first frame update
    void Start()
    {
        _mesh = GetComponent<MeshFilter>().sharedMesh;
        _camera = Camera.main.transform;

        // For testing...
        // InvokeRepeating("DoAberrationEffect", 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (_timer > 0.0f)
        {
            _timer -= Time.deltaTime;

            if (_timer < 0.0f)
                _timer = 0.0f;

            Vector3 pos = transform.position;
            pos -= _camera.forward * (_distance*2.0f);
            float scalar = _timer / _effectTime;
            float seperation = _distance * scalar;

            Vector3 offset = _camera.right;
            offset *= seperation;

            _rightMaterial.SetFloat("_Brightness", scalar);
            _leftMaterial.SetFloat("_Brightness", scalar);
            _upMaterial.SetFloat("_Brightness", scalar);

            Graphics.DrawMesh(_mesh, pos + offset, transform.rotation, _rightMaterial, 0);
            Graphics.DrawMesh(_mesh, pos - offset, transform.rotation, _leftMaterial, 0);
            Graphics.DrawMesh(_mesh, pos + (_camera.up* seperation), transform.rotation, _upMaterial, 0);

        }
    }
}
