using UnityEngine;
using System.Collections;
using System.Net;
using System.Text.RegularExpressions;

public class ConsoleUI : MonoBehaviour 
{
    static ConsoleUI mInstance = null;

    public GameObject[] StartupBtns = null;

    public UnityEngine.UI.InputField inputField = null;

    public static ConsoleUI instance
    {
        get
        {
            return mInstance;
        }
    }

    void Awake()
    {
        mInstance = this;
    }

	// Use this for initialization
	void Start () 
    {
    
	}

	// Update is called once per frame
	void Update () 
    {

	}

    public void SetIpAddress(string ipAddress)
    {
        inputField.text = ipAddress;
    }

    public void RunServer()
    {
        System.Net.IPAddress ipaddress;
        if (System.Net.IPAddress.TryParse(inputField.text, out ipaddress))
        {
            Launcher.instance.ipAddress = inputField.text;

            for (int i = 0; i < StartupBtns.Length; ++i)
            {
                StartupBtns[i].SetActive(false);
            }

            inputField.gameObject.SetActive(false);

            Launcher.instance.stats.Show(true);

            Launcher.instance.RunServer();
        }
    }

    public void RunClient()
    {
        for (int i = 0; i < StartupBtns.Length; ++i)
        {
            StartupBtns[i].SetActive(false);
        }

        Launcher.instance.RunClient();
    }
}
