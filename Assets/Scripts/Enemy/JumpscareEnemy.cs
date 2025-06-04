using UnityEngine;
using Unity.Netcode;

public class JumpscareEnemy : NetworkBehaviour
{
    public float disappearDistance = 30f;
    public float checkInterval = 0.1f;

    private Transform player;
    private bool isDisappearing = false;

    void Start()
    {
        player = ChooseRandomPlayer();
        StartCoroutine(CheckIfSeen());
    }

    System.Collections.IEnumerator CheckIfSeen()
    {
        while (!isDisappearing && player != null)
        {
            /*bool shouldDespawn = false;
            if (IsSeenByPlayer())
            {
                shouldDespawn = true;
            }
            else if (Vector3.Distance(transform.position, player.position) > disappearDistance)
            {
                shouldDespawn = true;
            }

            if (shouldDespawn)
            {
                isDisappearing = true;
                RequestDespawnServerRpc();
                yield break; 
            }*/
            yield return new WaitForSeconds(checkInterval);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDespawnServerRpc()
    {
        // Esta função é executada no servidor
        if (gameObject != null && gameObject.GetComponent<NetworkObject>() != null)
        {
            Debug.Log("Server received request to despawn JumpscareEnemy. Despawning now.");
            gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
        //Destroy(gameObject)
    }

    bool IsSeenByPlayer()
    {
        if (Camera.main == null) return false;
        Vector3 toEnemy = (transform.position - Camera.main.transform.position).normalized;
        float dot = Vector3.Dot(Camera.main.transform.forward, toEnemy);
        // Considera "visto" se estiver na frente da câmera
        if (dot > 0.7f)
        {
            Ray ray = new Ray(Camera.main.transform.position, toEnemy);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                if (hit.transform == transform)
                {
                    Debug.Log("Jumpscare enemy seen by player! Despawning...");
                    return true;
                }
            }
        }
        return false;
    }

    Transform ChooseRandomPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return null;
        int idx = Random.Range(0, players.Length);
        return players[idx].transform;
    }
}