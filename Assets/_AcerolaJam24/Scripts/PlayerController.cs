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

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 1000;
    [SerializeField] private float _inputIgnoreValue = 100.0f;
    [SerializeField] private Transform _camera;
    [SerializeField] private float _camYOffset = 0.5f;
    [SerializeField] private float _speed = 12.0f;
    [SerializeField] private float _sprintSpeed = 2.0f;

    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundDistance = 0.4f;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _gravity = -9.81f;

    private Vector2 _rotation;
    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    private bool _allowAiming = true;
    private bool _allowMovement = true;

    public void SetAllowAiming(bool onOff) { _allowAiming = onOff; }
    public void SetAllowMovement(bool onOff) { _allowMovement = onOff; }

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundMask);
        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -2.0f;

        if (_allowMovement)
        {
            float speed = _speed;

            // Deal with sprint
            if (Input.GetKey(KeyCode.LeftShift))
                speed = _speed * _sprintSpeed;

            // Deal with movement
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 move = (transform.right * moveX) + (transform.forward * moveZ);
            _controller.Move(move * speed * Time.deltaTime);
        }

        if (_allowAiming)
        {
            // Deal with look rotation
            float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * _sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * _sensitivity;

            // Cap out any big values (these can happen at the start)
            if (Mathf.Abs(mouseX) > _inputIgnoreValue)
                mouseX = 0.0f;

            if (Mathf.Abs(mouseY) > _inputIgnoreValue)
                mouseY = 0.0f;

            _rotation.y += mouseX;
            _rotation.x -= mouseY;
            _rotation.x = Mathf.Clamp(_rotation.x, -90.0f, 90f);

            transform.rotation = Quaternion.Euler(0.0f, _rotation.y, 0.0f);

            // Deal with the camera
            Vector3 camPos = _camera.position;
            camPos = transform.position;
            camPos.y += _camYOffset;
            _camera.position = camPos;
            _camera.rotation = Quaternion.Euler(_rotation.x, _rotation.y, 0.0f);
        }

        // Apply Gravity
        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}
