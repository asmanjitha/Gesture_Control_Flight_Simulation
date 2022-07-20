using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AirPlaneControllerASM : MonoBehaviour
{
    public static  Vector3 position;
    public static int points = 0;
    public static ParticleSystem victory;

    public static bool pause = true;
    public Sprite playSprite;
    public Sprite pauseSprite;
    public Image pauseBtnImage;
    public  GameObject pausePanel;

    public Button settingsBtn;

    private bool pauseCallBegin = false;
    // private bool pauseCallFinished = false;

    private float nextTime = 0.0f;
    float timer = 0.0f;
    float seconds = 0.0f;


    // Start is called before the first frame update
    void Start()
    {
        position = transform.position;
        victory = GameObject.Find("Particle System").GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        position = transform.position;
        timer += Time.deltaTime;
        seconds = timer % 60;

        ReadPauseCommand();

    }

    void FixedUpdate(){
        if(!pause){
            #if UNITY_EDITOR
                float tilt = Input.GetAxis("Horizontal");
            #else
                float tilt = PoseDataReader.HandReading;
            #endif
            transform.eulerAngles = new Vector3(0, 0 , tilt * -25);
            transform.position = Vector3.Lerp(transform.position, transform.position + new Vector3(tilt * 5,0,15), Time.deltaTime);
            // transform.position = Vector3.MoveTowards(transform.position, transform.position + new Vector3(tilt * 5,0,15), Time.deltaTime * 20);
        }
        
    }

    void ReadPauseCommand(){
        float leftAngle = PoseDataReader.leftAngle;
        float rightAngle = PoseDataReader.rightAngle;

        if(nextTime < seconds){
            if(leftAngle > 85.0f && rightAngle > 85.0f && pauseCallBegin == false){
                pauseCallBegin = true;
                nextTime = seconds + 1.0f;
            }
        }

        if(leftAngle < 15.0f && rightAngle < 15.0f && pauseCallBegin == true){
            pauseCallBegin = false;
            ToggleSprite();
        }
    }

    public static void AddPoints(){
        points += 1;
        GameObject.Find("Points Text").GetComponent<Text>().text = points.ToString();
        victory.Play();
    }


    public static void TogglePause(){
        pause = !pause;
    }

    public void ToggleSprite(){
        if(pause){
            pauseBtnImage.sprite = pauseSprite;
            settingsBtn.interactable = false;
            pausePanel.SetActive(false);
            Camera.main.cullingMask = (1 << LayerMask.NameToLayer("TransparentFX")) | (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("UI")) | (1 << LayerMask.NameToLayer("GameAsset1")) | (1 << LayerMask.NameToLayer("GameAsset2"));
        }else{
            pauseBtnImage.sprite = playSprite;
            settingsBtn.interactable = true;
            pausePanel.SetActive(true);
            Camera.main.cullingMask = (1 << LayerMask.NameToLayer("TransparentFX")) | (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("UI")) | (1 << LayerMask.NameToLayer("GameAsset1"));
        }

        TogglePause();
    } 





    
}
