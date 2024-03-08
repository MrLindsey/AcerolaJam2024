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

public class SocketLogic : MonoBehaviour
{
    [SerializeField] private CursorGrabInteractor _grabInteractor;
    [SerializeField] private string _acceptTag;
    [SerializeField] private bool _oneShot;
    [SerializeField] private Transform[] _socketAttachments;
    [SerializeField] private bool _showPreview = true;

    private Transform _fusedObject;
    private List<Transform> _currentObjectsWithinSocket = new List<Transform>();
    private List<Transform> _fusedObjects = new List<Transform>();

    private int _numObjectsInSocket;

    public bool IsConnected() { return (_fusedObjects.Count>0); }
    public int GetNumFusedObjects() { return _fusedObjects.Count; }
    public bool IsSocketFull() { return (_fusedObjects.Count >= _socketAttachments.Length); }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == _acceptTag)
        {
            bool allowAttach = true;

            // Do we have enough attachments for this object?
            if (_socketAttachments.Length > 0)
                allowAttach = _currentObjectsWithinSocket.Count < _socketAttachments.Length;

            if (allowAttach)
                _currentObjectsWithinSocket.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_currentObjectsWithinSocket.Contains(other.transform))
        {
            _currentObjectsWithinSocket.Remove(other.transform);
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (_currentObjectsWithinSocket.Count > 0)
        {
            Transform lastReleased = _grabInteractor.GetLastReleasedObject();
            if (lastReleased != null)
            {
                // Fuse the object together by setting the grabbed object as a child of this
                int objectIndex = _currentObjectsWithinSocket.IndexOf(lastReleased);

                if ((objectIndex != -1) || (lastReleased == transform))
                {
                    if (objectIndex == -1)
                        objectIndex = _currentObjectsWithinSocket.Count - 1;

                    Transform objectInSocket = _currentObjectsWithinSocket[objectIndex];

                    // Are we already fused?
                    if (!_fusedObjects.Contains(objectInSocket))
                    {
                        // Fuse the object
                        objectInSocket.SetParent(transform);
                        Rigidbody physics = objectInSocket.GetComponent<Rigidbody>();

                        physics.velocity = Vector3.zero;
                        physics.isKinematic = true;

                        Collider collision = objectInSocket.GetComponent<Collider>();
                        collision.enabled = false;

                        _fusedObjects.Add(objectInSocket);
                    }
                }
            }
        }

        // The objects are now fused together
        if (_fusedObjects.Count > 0)
        {
            if (_socketAttachments.Length > 0)
            {
                for (int i = 0; i < _fusedObjects.Count; ++i)
                {
                    _fusedObjects[i].localPosition = _socketAttachments[i].localPosition;
                    _fusedObjects[i].localRotation = _socketAttachments[i].localRotation;
                }
            }
            else
            {
                for (int i = 0; i < _fusedObjects.Count; ++i)
                {
                    _fusedObjects[i].localPosition = transform.localPosition;
                    _fusedObjects[i].localRotation = transform.localRotation;
                }
            }
        }
    }
}
