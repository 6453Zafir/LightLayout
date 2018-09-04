using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TryCubemap : MonoBehaviour {
    public ReflectionProbe rp;
    public Camera cenCam;
    public Cubemap testCubemap;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.H)) {
            rp.RenderProbe();
            this.GetComponent<Renderer>().material.SetTexture("_Cube", rp.bakedTexture);
            print(rp.bakedTexture.name);
            cenCam.RenderToCubemap(testCubemap);

}
	}
}
