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
            if (IsSeenByPlayer())
            {
                isDisappearing = true;
                if (IsServer)
                {
                    Destroy(gameObject);
                }
                yield break;
            }
            if (Vector3.Distance(transform.position, player.position) > disappearDistance)
            {
                if (IsServer)
                {
                    Destroy(gameObject);
                }
                yield break;
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }

    bool IsSeenByPlayer()
    {
        if (Camera.main == null) return false;
        Vector3 toEnemy = (transform.position - Camera.main.transform.position).normalized;
        float dot = Vector3.Dot(Camera.main.transform.forward, toEnemy);
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
