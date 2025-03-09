using UnityEngine;

public class AcidPoolController : MonoBehaviour
{
    public float damagePerSecond = 100f;
    public float sinkSpeed = 2f;

    private bool isPlayerInside = false;
    private PlayerController player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                player.SetInFluid(true, sinkSpeed);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            player.SetInFluid(false, sinkSpeed);
            player = null;
        }
    }

    private void Update()
    {
        if (isPlayerInside && player != null)
        {
            // Apply damage over time
            player.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }
}
