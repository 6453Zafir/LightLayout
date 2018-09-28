using UnityEngine;
using System.Collections;

public class test : MonoBehaviour 
{
    public GameObject bOX = null;
    public GameObject LIGHT = null;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame

    bool mValue = false;
	void Update () 
    {
	    if (Input.GetKeyDown(KeyCode.Space))
        {
            mValue = !mValue;
            if (mValue == false)
            {
                Shader.EnableKeyword("UNITY_ONLY_OUTPUT_GI");
            }
            else
            {
                Shader.DisableKeyword("UNITY_ONLY_OUTPUT_GI");
            }
            
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            bOX.transform.rotation *= Quaternion.Euler(new Vector3(0.0f, 0.5f, 0.5f));
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            bOX.transform.rotation *= Quaternion.Euler(new Vector3(0.0f, -0.5f, -0.5f));
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            LIGHT.transform.rotation *= Quaternion.Euler(new Vector3(0.0f, 0.5f, 0.0f));
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            LIGHT.transform.rotation *= Quaternion.Euler(new Vector3(0.0f, -0.5f, -0.0f));
        }
	}
}
