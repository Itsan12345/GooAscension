using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float health = 30f;
    [SerializeField] private GameObject deathEffect; // Optional explosion for enemy

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log("Enemy hit! Health: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    [SerializeField] private float energyReward = 20f;
    void Die()
    {
        // Find the player and give them energy
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerEnergy energy = player.GetComponent<PlayerEnergy>();
            if (energy != null)
            {
                energy.GainEnergy(energyReward);
            }
        }

        if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}