using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SanitySystem : NetworkBehaviour
{
    [Header("Sanity Settings")]
    public float maxSanity = 100f;
    public float sanityLossIdle = 1f;
    public float sanityLoss = 3f;
    public float timeLossInterval = 2f;
    public float timeLossIntervalIdle = 30f;

    private float currentSanity;
    private int enemiesChasing = 0;
    private Coroutine sanityLossCoroutine;


    public override void OnNetworkSpawn()
    {
        currentSanity = maxSanity;
    }

    private void Start()
    {
        if (sanityLossCoroutine == null)
        {
            sanityLossCoroutine = StartCoroutine(LoseSanityOverTime());
        }
    }

    private void OnSanityChanged(float oldValue, float newValue)
    {
        if (newValue <= 0)
        {
            OnSanityDepleted();
        }
    }

    public void AddChasingEnemy()
    {
        enemiesChasing++;

        if (sanityLossCoroutine == null)
        {
            sanityLossCoroutine = StartCoroutine(LoseSanityOverTime());
        }
    }


    public void RemoveChasingEnemy()
    {
        enemiesChasing = Mathf.Max(0, enemiesChasing - 1);

        if (enemiesChasing == 0 && sanityLossCoroutine != null)
        {
            StopCoroutine(sanityLossCoroutine);
            sanityLossCoroutine = null;
        }
    }

    private System.Collections.IEnumerator LoseSanityOverTime()
    {
        while (currentSanity > 0)
        {
            yield return new WaitForSeconds(timeLossInterval);
            currentSanity = Mathf.Max(0, currentSanity - sanityLoss);
            OnSanityChanged(currentSanity + sanityLossIdle, currentSanity);

        }
        sanityLossCoroutine = null;
    }

    private System.Collections.IEnumerator LoseSanityEnemyOnRange()
    {
        while (enemiesChasing > 0 && currentSanity > 0)
        {
            yield return new WaitForSeconds(timeLossInterval);
            currentSanity = Mathf.Max(0, currentSanity - sanityLoss);
            OnSanityChanged(currentSanity + sanityLoss, currentSanity);
        }
        sanityLossCoroutine = null;
    }

    public void RestoreSanity(float amount)
    {
        if (amount <= 0) return;

        float oldSanity = currentSanity;
        currentSanity = Mathf.Min(maxSanity, currentSanity + amount);
        OnSanityChanged(oldSanity, currentSanity);
    }


    private void OnSanityDepleted()
    {

        NetworkObject playerNetworkObject = GetComponentInParent<NetworkObject>();

        if (playerNetworkObject != null)
        {
            Debug.Log($"Player {playerNetworkObject.OwnerClientId} sanity depleted - destroying player!");

            if (IsServer)
            {

                playerNetworkObject.Despawn(true);
            }
            else
            {
                RequestPlayerDestroyServerRpc();
            }
        }
        else
        {
            Debug.LogError("NetworkObject not found in parent!");
        }
        
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayerDestroyServerRpc()
    {
        NetworkObject playerNetworkObject = GetComponentInParent<NetworkObject>();
        if (playerNetworkObject != null)
        {
            playerNetworkObject.Despawn(true);
        }
    }

    public float GetSanity()
    {
        return currentSanity;
    }

    public float GetSanityPercentage()
    {
        return currentSanity / maxSanity;
    }
}