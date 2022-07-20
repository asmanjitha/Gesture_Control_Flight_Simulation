using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingPoolBehaviour : MonoBehaviour
{
    public GameObject ringPrefab;
    private List<RingBehaviour> ringPool = new List<RingBehaviour>();
    public float planeLocation;
    public float nextPosition = 550;
    public static RingPoolBehaviour instance;
    public static bool deleting = false;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        SpawnFirstRings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnFirstRings(){
        for(int i = 0; i < 10; i++){

            GameObject ring = Instantiate(ringPrefab, new Vector3( Random.Range(-10,10),1.5f,i * 50 + 50), Quaternion.identity);
            ringPool.Add(ring.GetComponent<RingBehaviour>());
        }
    }

    public void SpawnNext(){
        GameObject ring = Instantiate(ringPrefab, new Vector3( Random.Range(-10,10),1.5f,nextPosition), Quaternion.identity);
        ringPool.Add(ring.GetComponent<RingBehaviour>());
        nextPosition += 50;
    }

    public static void DeleteRing(){
        RingBehaviour ring = instance.ringPool[0];
        instance.ringPool.Remove(ring);
        Destroy(ring.gameObject);
        instance.SpawnNext();
    }
}
