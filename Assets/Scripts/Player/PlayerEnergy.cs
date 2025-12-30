    using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    [SerializeField] private float energyRegenRate = 5f; // Passive regen over time
    
    [Header("Kill Rewards")]
    [SerializeField] private float energyGainOnKill = 15f; // Energy gained when player kills an enemy
    [SerializeField] private bool showKillEnergyGainDebug = true;

    [Header("UI Reference")]
    public Slider energySlider;

    private void Start()
    {
        currentEnergy = maxEnergy;
        UpdateUI();
    }

    private void Update()
    {
        // Optional: Passive regeneration so the player isn't stuck forever
        if (currentEnergy < maxEnergy)
        {
            GainEnergy(energyRegenRate * Time.deltaTime);
        }
    }

    // Call this from PlayerMovement.cs when transforming
    public bool CanAffordTransform(float cost)
    {
        return currentEnergy >= cost;
    }

    public void SpendEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    public void GainEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (energySlider != null)
        {
            energySlider.value = currentEnergy / maxEnergy;
        }
    }
    
    // Called when player kills an enemy
    public void OnEnemyKilled()
    {
        float previousEnergy = currentEnergy;
        GainEnergy(energyGainOnKill);
        
        if (showKillEnergyGainDebug)
        {
            Debug.Log($"⚡ ENERGY REWARD: Gained {energyGainOnKill} energy for enemy kill! Energy: {previousEnergy:F1} → {currentEnergy:F1}");
        }
    }
}