using UnityEngine;

public class ScorePickup : MonoBehaviour
{
    [SerializeField, Range(10, 50)] private int scoreAmount = 10;
    [SerializeField] private bool disableAfterUse = true;

    private bool _used;

    private void OnValidate()
    {
        scoreAmount = Mathf.Clamp(scoreAmount, 10, 50);
    }

    private void Reset()
    {
        Collider pickupCollider = GetComponent<Collider>();
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_used)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.AddScore(scoreAmount);
        _used = true;

        if (disableAfterUse)
        {
            gameObject.SetActive(false);
        }
    }
}
