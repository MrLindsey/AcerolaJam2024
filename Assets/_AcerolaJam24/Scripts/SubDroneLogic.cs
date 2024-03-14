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

public class SubDroneLogic : MonoBehaviour
{
    [SerializeField] private float _minDamageForce;

    [SerializeField] private float _smoothTime = 0.3f;
    [SerializeField] private float _rotationDamping = 0.4f;
    [SerializeField] private float _moveRate = 8.0f;
    [SerializeField] private string _damageBoxName = "RedBox";
    [SerializeField] private Transform _deathParticleEffect;
    [SerializeField] private Material _effectMaterial;
    [SerializeField] private AudioClip _failHitSfx;
    [SerializeField] private AudioClip _deathSfx;

    private bool _isActive = false;
    private SubDroneMan _manager;
    private Transform _player;
    private Transform _currentTarget;
    private float _moveTimer;
    private Vector3 _velocity;
    private AudioSource _audio;

    public void ActivateDrone(bool onOff)
    { 
        _isActive = onOff;
        gameObject.SetActive(true);
        MoveToRandomTarget();
        _audio = GetComponent<AudioSource>();
    }

    public void Setup(SubDroneMan manager, Transform player)
    {
        _manager = manager;
        _player = player;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't allow to be hit if the player is still holding the grabbable
        if (collision.rigidbody)
        {
            if (collision.rigidbody.freezeRotation == false)
            {
                // Have we been hit by a throwable object?
                if (collision.collider.CompareTag("Throwable"))
                {
                    if (collision.relativeVelocity.magnitude > _minDamageForce)
                    {
                        if (collision.transform.name == _damageBoxName)
                        {
                            // Do a hit sound effect

                            // Calculate the force of collision
                            float collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;
                            if (collisionForce > 0)
                                KillSubDrone();
                        }
                        else
                        {
                            // Do a fail sound effect
                            _audio.PlayOneShot(_failHitSfx);
                        }
                    }
                }
            }
        }
    }

    private void KillSubDrone()
    {
        _manager.DroneDied();
        gameObject.SetActive(false);

        Transform effect = Instantiate(_deathParticleEffect, transform.position, transform.rotation);
        effect.GetComponent<ParticleSystem>();
        effect.GetComponent<Renderer>().material = _effectMaterial;

        // Need to play this on the manager as this object is disabled now...
        _manager.PlayAudioClip(_deathSfx);
    }

    // Start is called before the first frame update
    void Start()
    {
        _audio = GetComponent<AudioSource>();
    }

    void MoveToRandomTarget()
    {
        _currentTarget = _manager.GetRandomTarget();
        _moveTimer = _moveRate;
    }

    // Update is called once per frame
    void Update()
    {
        if (_isActive)
        {
            // Move logic
            if (_moveTimer > 0.0f)
            {
                _moveTimer -= Time.deltaTime;
                if (_moveTimer <= 0.0f)
                {
                    // Move to the next target
                    MoveToRandomTarget();
                }
            }

            // Move towards the current target
            transform.position = Vector3.SmoothDamp(transform.position, _currentTarget.position, ref _velocity, _smoothTime);

            // Smoothly rotation towards the player
            Vector3 direction = _currentTarget.position - _player.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationDamping * Time.deltaTime);
        }
    }
}
