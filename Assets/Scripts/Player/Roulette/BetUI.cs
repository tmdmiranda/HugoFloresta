using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class RoletaSystem : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject roletaPrefab;
    private GameObject _spawnedRoleta;
    private bool _isRoletaSpawned;

    private bool _isWaiting;
    public UnityEvent<bool> OnRoletaUse;
    
    private int _prevIndex = -1;


    void Start()
    {
        SpawnNewRoleta();
    }

    void Update()
    {
        if (!_isRoletaSpawned && !_isWaiting)
        {
            StartCoroutine(WaitAndSpawn());
        }
    }

    public void SpawnNewRoleta()
    {

        if (roletaPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Missing references!");
            return;
        }

        if (!_isRoletaSpawned)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            while (randomIndex == _prevIndex && spawnPoints.Length > 1)
            {
                randomIndex = Random.Range(0, spawnPoints.Length);
            }

            Transform selectedSpawnPoint = spawnPoints[randomIndex];
            _spawnedRoleta = Instantiate(roletaPrefab, selectedSpawnPoint.position, selectedSpawnPoint.rotation);
            _spawnedRoleta.name = $"{roletaPrefab.name}_SpawnPoint_{randomIndex}";
            Debug.Log($"Spawned '{roletaPrefab.name}' at '{selectedSpawnPoint.name}'.");

            _isRoletaSpawned = true;
            _prevIndex = randomIndex;
        }
        else
        {
            Destroy(_spawnedRoleta);
            Debug.Log("Destroyed roleta.");
            _isRoletaSpawned = false;
        }
    }

    IEnumerator WaitAndSpawn()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(5f);
        SpawnNewRoleta();
        _isWaiting = false;
    }
}