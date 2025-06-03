using UnityEngine;
using UnityEngine.Events;


public class RoletaSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] spawnPositions;
    [SerializeField] private GameObject Roleta;
    private UnityEvent<Vector3,Quaternion, bool> OnObjectSpawned;
    private int startObjectIndex;
    bool isSpawned, wasUsed;
    private Vector3 startPos;
    private Quaternion startRot;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (OnObjectSpawned == null)
            OnObjectSpawned = new UnityEvent<Vector3, Quaternion, bool>();

        OnObjectSpawned?.AddListener(SpawnM);
        startObjectIndex = Random.Range(0, spawnPositions.Length);

        startPos = spawnPositions[startObjectIndex].transform.position;
        startRot = spawnPositions[startObjectIndex].transform.rotation;

        Debug.Log("" + startObjectIndex);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            OnObjectSpawned?.Invoke(startPos, startRot, isSpawned);
    }

    void SpawnM(Vector3 obj, Quaternion Rot, bool isSpawned)
    {
        Instantiate(Roleta, obj, Rot);

        Debug.Log($"pos: {obj}, rot: {Rot}, bool: {isSpawned}");
    }


}
