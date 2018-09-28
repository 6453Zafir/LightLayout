using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum eGiMode
{
    Enlighten , 
    RSM , 
    LPV , 
    VPL
}

public enum eConsoleResolution
{
    W800H600 , 
}

public class Launcher : MonoBehaviour 
{
    static Launcher mInstance = null;

    public static Launcher instance
    {
        get
        {
            return mInstance;
        }
    }

    public string SceneName = "Main";

    public Stats StatComp = null;

    public eGiMode GIMode = eGiMode.Enlighten;

    public eConsoleResolution ConsoleResolution = eConsoleResolution.W800H600;

    public string serverAddress = "127.0.0.1";

    public int serverPort = 8080;

    int mConsoleWidth = 800;
    int mConsoleHeight = 600;

    ConnectionMgr mConnectionMgr = null;

    Camera mSceneCamera = null;

    List<Light> mDynLights = new List<Light>();

    public List<Light> dynLights
    {
        get
        {
            return mDynLights;
        }
    }

    public Camera sceneCamera
    {
        get
        {
            return mSceneCamera;
        }
    }

    public ConnectionMgr connectionMgr
    {
        get
        {
            return mConnectionMgr;
        }
    }

    public int consoleWidth
    {
        get
        {
            return mConsoleWidth;
        }
    }

    public int consoleHeight
    {
        get
        {
            return mConsoleHeight;
        }
    }

    public Stats stats
    {
        get
        {
            return StatComp;
        }
    }

    void Awake()
    {
        mInstance = this;

        Initialize();

        DontDestroyOnLoad(this.gameObject);
    }

    void Initialize()
    {
        switch (ConsoleResolution)
        {
            case eConsoleResolution.W800H600:
                mConsoleWidth = 800;
                mConsoleHeight = 600;
                break;

            default:
                break;
        }
    }

	void Start () 
    {
        StartCoroutine(SetupAsync());

        StartCoroutine(CheckInternetIpAddressAsync());
	}

    IEnumerator SetupAsync()
    {
        GameObject go = new GameObject("ConnectionMgr");
        go.transform.parent = this.transform;
        mConnectionMgr = go.AddComponent<ConnectionMgr>();

        yield return null;

        AsyncOperation loadAsync = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneName);

        yield return loadAsync;

        SetupTartgeScene();

        yield return new AsyncOperation();
    }

    void SetupTartgeScene()
    {
        // Setup camera
        mSceneCamera = Camera.main;
        mSceneCamera.enabled = false;
        mSceneCamera.clearFlags = CameraClearFlags.Color;
        mSceneCamera.backgroundColor = Color.black;

        // Setup all light
        if (GameObject.FindGameObjectWithTag("LCRS_Light0") != null)
        {
            mDynLights.Add(GameObject.FindGameObjectWithTag("LCRS_Light0").gameObject.GetComponent<Light>());
        }

        if (GameObject.FindGameObjectWithTag("LCRS_Light1") != null)
        {
            mDynLights.Add(GameObject.FindGameObjectWithTag("LCRS_Light1").gameObject.GetComponent<Light>());
        }

        List<GameObject> rootGameObjects = new List<GameObject>();
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(rootGameObjects);

        Shader lcrsShader = Resources.Load("Shader/CustomStandardShader/CustomStandardSpecular", typeof(Shader)) as Shader;

        for (int i = 0 ; i < rootGameObjects.Count ; ++i)
        {
            SetupLcrsMaterial(rootGameObjects[i].transform , lcrsShader);
        }
    }

    void SetupLcrsMaterial(Transform goTrans , Shader lcrsShader)
    {
        MeshRenderer[] mrs = goTrans.gameObject.GetComponents<MeshRenderer>();
        if (mrs != null)
        {
            for (int i = 0 ; i < mrs.Length ; ++i)
            {
                for (int j = 0 ;j < mrs[i].materials.Length ; ++j)
                {
                    mrs[i].materials[j].shader = lcrsShader;
                }
                
            }
        }

        for (int i = 0; i < goTrans.childCount; ++i)
        {
            SetupLcrsMaterial(goTrans.GetChild(i) , lcrsShader);
        }
    }

    public void RunServer()
    {
        GameObject go = new GameObject("NetworkServer");
        NetworkServer networkServer = go.AddComponent<NetworkServer>();
        networkServer.Startup(serverAddress, serverPort);
    }

    public void RunClient()
    {
        GameObject go = new GameObject("NetworkClient");
        NetworkClient networkClient = go.AddComponent<NetworkClient>();
        networkClient.Startup("127.0.0.1", serverPort);

        // Lock framerate of client
        Application.targetFrameRate = 27;

        // Enable client camera
        sceneCamera.enabled = true;
    }

    public IEnumerator CheckInternetIpAddressAsync()
    {
        string httpRequest = "http://pv.sohu.com/cityjson?ie=utf-8";

        WWW ret = new WWW(httpRequest);
        yield return ret;
        if (ret.error != null)
        {
            Debug.LogError("error:" + ret.error);
            yield break;
        }

        if (string.IsNullOrEmpty(ret.text))
        {
            yield break;
        }

        string prefix = "\"cip\": \"";
        int sIdx = ret.text.LastIndexOf(prefix);
        int eIdx = ret.text.LastIndexOf("\", \"cid\": \"");

        serverAddress = ret.text.Substring(sIdx + prefix.Length, eIdx - sIdx - prefix.Length);

        ConsoleUI.instance.SetIpAddress(serverAddress);
    }

    public string ipAddress
    {
        get
        {
            return serverAddress;
        }

        set
        {
            serverAddress = value;
        }
    }

    bool bGiKeywordState = false;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bGiKeywordState = !bGiKeywordState;
            if (bGiKeywordState == false)
            {
                Shader.EnableKeyword("UNITY_ONLY_OUTPUT_GI");
            }
            else
            {
                Shader.DisableKeyword("UNITY_ONLY_OUTPUT_GI");
            }
        }
    }
}
