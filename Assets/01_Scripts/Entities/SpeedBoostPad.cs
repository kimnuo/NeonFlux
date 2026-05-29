using UnityEngine;

public class SpeedBoostPad : MonoBehaviour
{
    [SerializeField] private float speedIncrease = 5f;
    [SerializeField] private bool disableAfterUse = true;

    private bool _used;
    private bool _initialActive;

    private void Awake()
    {
        _initialActive = gameObject.activeSelf;
    }

    private void Reset()
    {
        Collider padCollider = GetComponent<Collider>();
        if (padCollider != null)
        {
            padCollider.isTrigger = true;
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

        player.maxForwardSpeed += speedIncrease;
        _used = true;

        if (disableAfterUse)
        {
            gameObject.SetActive(false);
        }
    }

    public void ResetForReplay()
    {
        _used = false;
        gameObject.SetActive(_initialActive);
    }
}
