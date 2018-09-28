using System;
using System.Collections;
using System.Collections.Generic;

using System.IO;

using System.Text;
using UnityEngine;

class SceneConvertor
{
    public static int vertexOffset = 0;
    public static int normalOffset = 0;
    public static int uvOffset = 0;

    public static bool ExportSceneToFile(string path)
    {
        vertexOffset = normalOffset = uvOffset = 0;

        Transform[] transforms = (Transform[])UnityEngine.Object.FindObjectsOfType(typeof(Transform));

        int exportObjNum = 0;

        ArrayList objArr = new ArrayList();
        for (int i = 0; i < transforms.Length; ++i) 
        {
            CollectPhysxMeshToExport(transforms[i], ref objArr);
        }

        exportObjNum = objArr.Count;

        MeshCollider[] meshColliders = new MeshCollider[exportObjNum];
        for (int i = 0; i < exportObjNum; ++i)
            meshColliders[i] = (MeshCollider)objArr[i];

        return SaveMeshesToObj(meshColliders, path);
    }

    protected static void CollectPhysxMeshToExport(Transform trans, ref ArrayList arrList)
    {   
        MeshCollider meshCollider = trans.GetComponent<MeshCollider>();        
        if( meshCollider && meshCollider.enabled && meshCollider.sharedMesh )
            arrList.Add(meshCollider);

        //if(trans.childCount > 0 )
        //{
        //    for (int i = 0; i < trans.childCount; ++i)
        //        CollectPhysxMeshToExport(trans.GetChild(i), ref arrList);            
        //}
    }

    protected static bool SaveMeshesToObj(MeshCollider[] meshes, string file)
    {
        //Dictionary<string, ObjMaterial> materialList = new Dictionary<string, ObjMaterial>();

        if (meshes.Length == 0) return false;

        using (StreamWriter sw = new StreamWriter(file)) 
        {
            for (int i = 0; i < meshes.Length; ++i) 
            {
                sw.Write(MeshToString(meshes[i]));
            }
        }

        return true;
    }

    protected static string MeshToString(MeshCollider meshCol)
    {
        Mesh mesh = meshCol.sharedMesh;
        if (!mesh) return "";

        StringBuilder sb = new StringBuilder();
        sb.Append("g ").Append(mesh.name).Append("\n");

        foreach (Vector3 lVert in mesh.vertices) 
        {
            Vector3 wVert = meshCol.transform.TransformPoint(lVert);
            sb.Append(string.Format("v {0} {1} {2}\n", wVert.x, wVert.y, -wVert.z));
        }

        sb.Append("\n");

        for (int i = 0; i < mesh.subMeshCount; ++i) 
        {
            sb.Append("\n");

            int[] triangles = mesh.GetTriangles(i);

            for (int j = 0; j < triangles.Length; j += 3) 
            {
                sb.Append(string.Format("f {2} {1} {0}\n",
                   triangles[j] + 1 + vertexOffset,
                   triangles[j + 1] + 1 + vertexOffset,
                   triangles[j + 2] + 1 + vertexOffset));
            }
        }

        vertexOffset += mesh.vertices.Length;
        normalOffset += mesh.normals.Length;
        uvOffset += mesh.uv.Length;

        return sb.ToString();
    }
}