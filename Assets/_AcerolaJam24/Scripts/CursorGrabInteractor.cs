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
using UnityEngine.Events;
using UnityEngine.UI;

public class CursorGrabInteractor : MonoBehaviour
{
    [SerializeField] private TaskMan _taskMan;
    [SerializeField] private Transform _camera;
    [SerializeField] private LayerMask _grabbableLayers;
    [SerializeField] private float _scrollWheelSensitivity = 500.0f;
    [SerializeField] private float _rotateSpeed = 5.0f;
    [SerializeField] private float _maxGrabDistance = 5.0f;
    [SerializeField] private float _minGrabDistance = 0.1f;
    [SerializeField] private float _keyMoveDistance = 0.05f;
    [SerializeField] private float _throwPressTime = 0.1f;
    [SerializeField] private float _throwForce = 1.0f;
    [SerializeField] private float _maxThrowForce = 2.0f;
    [SerializeField] private float _throwYOffset = 0.1f;
    [SerializeField] private Image _mainCursor;
    [SerializeField] private Image _powerCursor;

    private float _buttonHeldDownTimer = 0.0f;
    private bool _isGrabbing = false;
    private Vector3 _distanceInput;
    private Transform _grabbedObject;
    private Rigidbody _grabbedPhysics;
    private float _currentGrabDistance;
    private Ray _ray;
    private Vector3 _hitOffsetLocal;
    private Quaternion _rotationDifference;
    private bool _mouseReleased;
    private bool _isThrowable;

    private RigidbodyInterpolation _initialInterpolationSetting;
    private bool _initialKinematic;
    private CollisionDetectionMode _initialCollisionMode;
    private Transform _lastReleasedObject;
    private bool _allowGrabbing = true;

    public Transform GetLastReleasedObject() { return _lastReleasedObject; }

    public void AllowGrabbing(bool onOff)
    {
        if (onOff != _allowGrabbing)
        {
            if (onOff)
            {
                // Turn grabbing on
                _allowGrabbing = true;
            }
            else
            {
                // Turn grabbing off
                ReleaseObject(0.0f);
                _allowGrabbing = false;
            }
            _mainCursor.enabled = _allowGrabbing;
        }
    }

    public void ForceReleaseGrab()
    {
        ReleaseObject(0.0f);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (_camera == null)
            _camera = Camera.main.transform;

        _ray = new Ray();
        _powerCursor.fillAmount = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        // Don't allow any grabbing if it's turned off
        if (_allowGrabbing == false)
            return;

        if (_isGrabbing)
        {
            float direction = Input.GetAxis("Mouse ScrollWheel");

            // Keyboard inputs
            if (Input.GetKey(KeyCode.R))
                direction = _keyMoveDistance;
            else if (Input.GetKey(KeyCode.F))
                direction = -_keyMoveDistance;

            if (Mathf.Abs(direction) > 0 && CheckObjectDistance(direction))
            {
                _distanceInput += _camera.forward * _scrollWheelSensitivity * direction;
            }

            if (_isThrowable)
            {
                if (_mouseReleased)
                {
                    if (Input.GetMouseButton(0))
                    {
                        _buttonHeldDownTimer += Time.deltaTime;
                        if (_buttonHeldDownTimer > _maxThrowForce)
                            _buttonHeldDownTimer = _maxThrowForce;

                        _powerCursor.fillAmount = _buttonHeldDownTimer / _maxThrowForce;
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        // Drop the object
                        if (_buttonHeldDownTimer < _throwPressTime)
                        {
                            if (Input.GetMouseButtonUp(0))
                                ReleaseObject(0.0f);
                        }
                        else
                        {
                            if (Input.GetMouseButtonUp(0))
                                ReleaseObject(_buttonHeldDownTimer);
                        }
                    }
                }

                if (Input.GetMouseButtonUp(0))
                    _mouseReleased = true;
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                    ReleaseObject(0.0f);
            }
        }
        else
        {
            RaycastHit hit;

            // Do a raycast from the cursor (center of the screen) and see if we can grab an object
            _ray.direction = _camera.forward;
            _ray.origin = _camera.transform.position;

            if (Physics.Raycast(_ray, out hit, _maxGrabDistance, _grabbableLayers))
            {
                if (hit.collider != null)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        _buttonHeldDownTimer = 0.0f;

                        // Tell the taskman we grabbed this object
                        _taskMan.PlayerGrabbedObject(hit.transform);

                        bool isGrabbable = true;
                        GrabbableObjectController grabController = hit.transform.GetComponent<GrabbableObjectController>();
                        if (grabController != null)
                        {
                            // Do we have extra logic on this grabbable object?
                            isGrabbable = grabController.OnGrabObject();
                        }

                        if (isGrabbable)
                        { 
                            // Pick up the object
                            _isGrabbing = true;
                            GrabObject(hit);
                        }
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // Only allow the last released object cache to exist for one physics frame
        if (_lastReleasedObject != null)
            _lastReleasedObject = null;

        if (_grabbedObject != null)
        {
            _ray.direction = _camera.forward;
            _ray.origin = _camera.transform.position;

            // Get the destination point for the point on the object we grabbed
            Vector3 holdPoint = _ray.GetPoint(_currentGrabDistance) + _distanceInput;
            Vector3 centerDestination = holdPoint - _grabbedObject.TransformVector(_hitOffsetLocal);

#if UNITY_EDITOR
            Debug.DrawLine(_ray.origin, holdPoint, Color.blue, Time.fixedDeltaTime);
#endif
            // Find vector from current position to destination
            Vector3 toDestination = centerDestination - _grabbedObject.position;
            Vector3 force = toDestination / Time.fixedDeltaTime * 0.3f / _grabbedPhysics.mass;

            // Remove any existing velocity and add force to move to final position
            _grabbedPhysics.velocity = Vector3.zero;
            _grabbedPhysics.AddForce(force, ForceMode.VelocityChange);

            Vector3 targetDirection = _camera.position - _grabbedObject.position;
            targetDirection.y = 0f;
            Quaternion targetRotation = _camera.rotation * _rotationDifference;

            Quaternion newRotation = Quaternion.Lerp(_grabbedPhysics.rotation, targetRotation, _rotateSpeed * Time.fixedDeltaTime);
            _grabbedPhysics.MoveRotation(newRotation);

            //We need to recalculte the grabbed distance as the object distance from the player has been changed
            _currentGrabDistance = Vector3.Distance(_ray.origin, holdPoint);
            _distanceInput = Vector3.zero;
        }
    }

    private void GrabObject(RaycastHit hit)
    {
        _isThrowable = false;

        // Don't allow objects to be grabbed if they are kinematic
        if (hit.rigidbody.isKinematic == true)
            return;

        // Grab the object and setup it's physics
        _grabbedPhysics = hit.rigidbody;
        _grabbedObject = _grabbedPhysics.transform;

        _initialKinematic = _grabbedPhysics.isKinematic;
        _initialInterpolationSetting = _grabbedPhysics.interpolation;
        _initialCollisionMode = _grabbedPhysics.collisionDetectionMode;

        _hitOffsetLocal = Vector3.zero;
        _rotationDifference = Quaternion.Inverse(_camera.rotation) * _grabbedPhysics.rotation;

        _grabbedPhysics.isKinematic = false;
        _grabbedPhysics.freezeRotation = true;
        _currentGrabDistance = hit.distance;
        _grabbedPhysics.interpolation = RigidbodyInterpolation.Interpolate;
        _grabbedPhysics.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (_grabbedObject.CompareTag("Throwable"))
            _isThrowable = true;

        _isGrabbing = true;
        _lastReleasedObject = null;
        _mouseReleased = false;
    }

    private void ReleaseObject(float throwAmount)
    {
        if (_grabbedPhysics)
        {
            _lastReleasedObject = _grabbedPhysics.transform;

            // Reset the rigidbody to how it was before we grabbed it
            _grabbedPhysics.interpolation = _initialInterpolationSetting;
            _grabbedPhysics.isKinematic = _initialKinematic;
            _grabbedPhysics.collisionDetectionMode = _initialCollisionMode;

            if (throwAmount > 0.0f)
            {
                if (throwAmount > _maxThrowForce)
                    throwAmount = _maxThrowForce;

                // Apply a bit of an offset vertically when throwing
                Vector3 pos = transform.position + transform.forward;
                pos.y += _throwYOffset;
                pos = pos - transform.position;
                pos = pos.normalized;

                _grabbedPhysics.AddForce(pos * (throwAmount * _throwForce));
            }

            _grabbedPhysics.freezeRotation = false;
            _grabbedPhysics = null;
            _grabbedObject = null;
            _distanceInput = Vector3.zero;

            _isGrabbing = false;
            _powerCursor.fillAmount = 0.0f;
        }
    }

    private bool CheckObjectDistance(float direction)
    {
        var distance = Vector3.Distance(_camera.position, _grabbedPhysics.position);
        if (direction > 0)
            return distance <= _maxGrabDistance;

        if (direction < 0)
            return distance >= _minGrabDistance;

        return false;
    }
}
