using UnityEngine;
using System.Collections;

public class SceneRoot : MonoBehaviour 
{
    Bounds mBounds;

    bool mIsInitialized = false;

	// Use this for initialization
	void Start () 
    {
        BoundAndCenter();
	}

    void BoundAndCenter()
    {
        MeshRenderer[] mrs = this.GetComponentsInChildren<MeshRenderer>();

        for (int i = 0 ; i < mrs.Length ; ++i)
        {
            if (mIsInitialized == false)
            {
                mIsInitialized = true;

                mBounds.SetMinMax(mrs[i].bounds.min, mrs[i].bounds.max);
            }
            else
            {
                mBounds.Encapsulate(mrs[i].bounds);
            }
        }

        this.transform.position -= mBounds.center;

        LCRS.Log("===========Max: " + mBounds.max + ", " + mBounds.min);
    }
	
	// Update is called once per frame
	void Update () 
    {
	
	}
}
