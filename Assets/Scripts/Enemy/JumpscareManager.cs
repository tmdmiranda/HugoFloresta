using UnityEngine;
using System.Collections;

public class JumpscareManager : MonoBehaviour
{
    public GameObject jumpscareEnemyPrefab;
    public float spawnRadius = 5f;
    public float checkInterval = 10f;
    [Range(0, 100)] public float chancePercent = 100f;

    void Start()
    {
        StartCoroutine(JumpscareRoutine());
    }

    IEnumerator JumpscareRoutine()
    {
        Debug.Log("intevral: " + checkInterval);
        int countdown = Mathf.RoundToInt(checkInterval);
        GameObject jumpscareInstance = null;
        while (true)
        {
            // Log do countdown
            for (int i = countdown; i > 0; i--)
            {
                Debug.Log($"Jumpscare countdown: {i} seconds remaining");
                yield return new WaitForSeconds(1f);
            }
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in players)
            {
                // Só spawna se não existir um jumpscare ativo
                if (jumpscareInstance == null)
                {
                    float roll = Random.Range(0f, 100f);
                    if (roll <= chancePercent)
                    {
                        // Calcula a posição atrás do jogador
                        Vector3 spawnDir = -player.transform.forward;
                        Vector3 spawnPos = player.transform.position + spawnDir * spawnRadius;
                        jumpscareInstance = Instantiate(jumpscareEnemyPrefab, spawnPos, Quaternion.LookRotation(spawnDir));
                        Debug.Log($"Jumpscare enemy spawned behind player {player.name} at {spawnPos}");
                    }
                }
            }
            // Espera até o jumpscare ser destruído antes de reiniciar o ciclo
            while (jumpscareInstance != null)
            {
                yield return null;
            }
        }
    }
}