using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, 180f);
    [SerializeField] private bool followY;

    private float _fixedY;

    private void Awake()
    {
        _fixedY = transform.position.y;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 nextPosition = target.position + offset;
        if (!followY) nextPosition.y = _fixedY;

        transform.position = nextPosition;
    }
}
