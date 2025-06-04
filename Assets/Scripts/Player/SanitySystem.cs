using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SanitySystem : NetworkBehaviour
{
    [Header("Sanity Settings")]
    public float maxSanity = 100f;
    public float sanityLoss = 1f;  
    public float timeLossInterval = 1f; 
    
    private NetworkVariable<float> currentSanity = new NetworkVariable<float>(100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
        
    private int enemiesChasing = 0;
    private Coroutine sanityLossCoroutine;
      public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            currentSanity.Value = maxSanity;
            currentSanity.OnValueChanged += OnSanityChanged;
        }
    }
    
    private void OnSanityChanged(float oldValue, float newValue)
    {
        Debug.Log($"Player {OwnerClientId} sanity: {oldValue} -> {newValue}");
        
        if (newValue <= 0)
        {
            OnSanityDepleted();
        }
    }
    
    public void AddChasingEnemy()
    {
        enemiesChasing++;
        Debug.Log($"Player {OwnerClientId} - Enemies chasing: {enemiesChasing}");
        
        if (enemiesChasing == 1 && sanityLossCoroutine == null)
        {
            sanityLossCoroutine = StartCoroutine(LoseSanityOverTime());
        }
    }
    
    public void RemoveChasingEnemy()
    {
        enemiesChasing = Mathf.Max(0, enemiesChasing - 1);
        Debug.Log($"Player {OwnerClientId} - Enemies chasing: {enemiesChasing}");
        
        if (enemiesChasing == 0 && sanityLossCoroutine != null)
        {
            StopCoroutine(sanityLossCoroutine);
            sanityLossCoroutine = null;
        }
    }
    
    private System.Collections.IEnumerator LoseSanityOverTime()
    {
        while (enemiesChasing > 0 && currentSanity.Value > 0)
        {
            yield return new WaitForSeconds(timeLossInterval);
            
            if (IsOwner && enemiesChasing > 0)
            {
                float newSanity = Mathf.Max(0, currentSanity.Value - sanityLoss);
                currentSanity.Value = newSanity;
            }
        }
        sanityLossCoroutine = null;
    }
    
    private void OnSanityDepleted()
    {
        if (IsOwner)
        {
            Debug.Log($"Player {OwnerClientId} sanity depleted!");
            // Implementar consequências aqui
        }
    }
    
    public float GetSanity()
    {
        return currentSanity.Value;
    }
    
    public float GetSanityPercentage()
    {
        return currentSanity.Value / maxSanity;
    }
}