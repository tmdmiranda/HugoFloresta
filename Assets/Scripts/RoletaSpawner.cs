using UnityEngine;
using UnityEngine.Events;


public class roletaSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject roletaPrefab;
    private UnityEvent<Vector3, Quaternion, bool> OnObjectSpawned;
    private int startObjectIndex;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       // if (OnObjectSpawned == null)
       //     OnObjectSpawned = new UnityEvent<Vector3, Quaternion, bool>();

       // OnObjectSpawned?.AddListener(SpawnM);
       // startObjectIndex = Random.Range(0, spawnPositions.Length);

       // startPos = spawnPositions[startObjectIndex].transform.position;
       // startRot = spawnPositions[startObjectIndex].transform.rotation;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            //        OnObjectSpawned?.Invoke(startPos, startRot, isSpawned);
            SpawnNewroleta();
    }

    void SpawnM(Vector3 obj, Quaternion Rot, bool isSpawned)
    {
        Instantiate(roletaPrefab, obj, Rot);

        Debug.Log($"pos: {obj}, rot: {Rot}, bool: {isSpawned}");
    }

    
    public void SpawnNewroleta()
    {
        if (roletaPrefab == null) return; // e debug.log

        if (spawnPoints == null || spawnPoints.Length == 0) return; // e debug.log
 
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedSpawnPoint = spawnPoints[randomIndex];

        GameObject newInstance = Instantiate(roletaPrefab, selectedSpawnPoint.position, selectedSpawnPoint.rotation);
        
        newInstance.name = $"{roletaPrefab.name}_SpawnPoint_{randomIndex}"; // acho que isto ordena no unity?? Pode n ser preciso

        Debug.Log($"spawned '{roletaPrefab.name}' '{selectedSpawnPoint.name}'.");
    }

}
