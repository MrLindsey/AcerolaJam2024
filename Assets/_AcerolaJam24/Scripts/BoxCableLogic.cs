using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxCableLogic : MonoBehaviour
{
    [SerializeField] private Transform _start;
    [SerializeField] private Transform _end;

    // Start is called before the first frame update
    void Start()
    {
        Transform parent = transform.parent;
        PlugLogic plug = parent.GetComponent<PlugLogic>();

        if (_start == null)
            _start = parent;

        if (_end == null)
            _end = plug.GetCableEnd();
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate the mid-point and length of the cable
        Vector3 mid = (_end.position - _start.position) * 0.5f;
        transform.position = _start.position + mid;

        // Rotate the cable to the end point
        transform.LookAt(_end);

        // Scale the cable to the right size
        Vector3 scale = transform.localScale;
        scale.z = mid.magnitude * 2.0f;
        transform.localScale = scale;

    }
}
