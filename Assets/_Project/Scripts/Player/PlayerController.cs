using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float rightTurnAngle = 45f;
    [SerializeField] private float turnSpeed = 180f;

    private Quaternion _defaultRotation;
    private Quaternion _rightRotation;

    // Start is called before the first frame update
    void Start()
    {
        _defaultRotation = transform.rotation;
        _rightRotation = _defaultRotation * Quaternion.Euler(0f, rightTurnAngle, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion targetRotation = IsHoldingInput() ? _rightRotation : _defaultRotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private bool IsHoldingInput()
    {
        if (Input.GetMouseButton(0))
        {
            return true;
        }

        if (Input.touchCount <= 0)
        {
            return false;
        }

        Touch touch = Input.GetTouch(0);
        return touch.phase == TouchPhase.Began
               || touch.phase == TouchPhase.Moved
               || touch.phase == TouchPhase.Stationary;
    }
}
