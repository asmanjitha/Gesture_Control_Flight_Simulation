using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingBehaviour : MonoBehaviour
{
    public static RingBehaviour instance;
    public bool setToDelete = false;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        float dist = Vector3.Distance(AirPlaneControllerASM.position, transform.position);
        if(dist <= 5){
            if(Mathf.Abs(AirPlaneControllerASM.position.z - transform.position.z) < 0.5f && !setToDelete){
                AirPlaneControllerASM.AddPoints();
                RingPoolBehaviour.DeleteRing();
                setToDelete = true;
            }
        }

        if(AirPlaneControllerASM.position.z > transform.position.z && setToDelete== false){
            RingPoolBehaviour.DeleteRing();
        }
    }


}
