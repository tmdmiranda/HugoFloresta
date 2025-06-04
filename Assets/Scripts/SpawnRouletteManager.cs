using UnityEngine;

public class SpawnRouletteManager : MonoBehaviour
{
    [SerializeField] private GameObject roulettePrefab;
    [SerializeField] private Transform[] spawnPoints;

    private GameObject currentRoulette;

    public void SpawnRoulette()
    {
        if (currentRoulette != null) return;
 
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        currentRoulette = Instantiate(roulettePrefab, spawnPoint.position, spawnPoint.rotation);
    }

    public void DespawnRoulette()
    {
        if (currentRoulette != null)
        {
            Destroy(currentRoulette);
            currentRoulette = null;
        }
    }
}
