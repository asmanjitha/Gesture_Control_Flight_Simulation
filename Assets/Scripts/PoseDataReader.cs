using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoseDataReader : MonoBehaviour
{
    public Transform leftWrist;
    public Transform leftHip; 
    public Transform leftShoulder;

    public Transform rightWrist;
    public Transform rightHip;
    public Transform rightShoulder;

    public Text valText;

    public static float HandReading = 0.0f;

    public static float leftAngle = 0.0f;
    public static float rightAngle = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        CalculteHandData();
        CalculateHandAngle();
    }

    void CalculteHandData(){
        float leftDist = Vector3.Distance(leftHip.localPosition, leftWrist.localPosition);
        float rightDist = Vector3.Distance(rightHip.localPosition, rightWrist.localPosition);
        float val = (rightDist - leftDist)/500.0f ; 
        if(val > 1){
            val = 1;
        }else if (val < -1){
            val = -1;
        }

        // valText.text = val.ToString();
        HandReading = val;
    }

    void CalculateHandAngle(){
        Vector2 leftNorm = leftHip.localPosition - leftShoulder.localPosition;
        Vector2 leftVec = leftWrist.localPosition - leftShoulder.localPosition;
        leftAngle = Vector2.Angle(leftNorm, leftVec);

        Vector2 rightNorm = rightHip.localPosition - rightShoulder.localPosition;
        Vector2 rightVec = rightWrist.localPosition - rightShoulder.localPosition;
        rightAngle = Vector2.Angle(rightNorm, rightVec);

        // valText.text = leftAngle.ToString() + ", " + rightAngle.ToString();
    }

}
