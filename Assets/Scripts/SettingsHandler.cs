using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsHandler : MonoBehaviour
{
    public Image videoBtnImage;

    public Sprite videoOn;
    public Sprite videoOff;

    public bool videoScreenAvaiable = true;
    public RawImage videoScreen;

    public GameObject settingsScreen;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("escape")){
            Exit();
        }
    }

    public void ToggleVideo(){
        videoScreenAvaiable = !videoScreenAvaiable;
        if(videoScreenAvaiable){
            videoScreen.color = new Color32(255,255,255,255);
            videoBtnImage.sprite = videoOn;
        }else{
            videoScreen.color = new Color32(255,255,255,0);
            videoBtnImage.sprite = videoOff;
        }
    }

    public void ActivateSetings(){
        settingsScreen.SetActive(true);
        Camera.main.cullingMask = (1 << LayerMask.NameToLayer("TransparentFX")) | (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("UI"));
    }

    public void DeactivateSettings(){
        Camera.main.cullingMask = (1 << LayerMask.NameToLayer("TransparentFX")) | (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("UI")) | (1 << LayerMask.NameToLayer("GameAsset1"));
        settingsScreen.SetActive(false);
    }

    public void Exit(){
        Application.Quit();
    }

    
}
