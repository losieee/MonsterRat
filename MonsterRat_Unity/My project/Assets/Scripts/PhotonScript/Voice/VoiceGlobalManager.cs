using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.Unity;


public class VoiceGlobalManager : MonoBehaviour
{

    public static VoiceGlobalManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(this.gameObject);
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
