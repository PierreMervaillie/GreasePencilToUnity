using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GP_Importer_MeshSwap : MonoBehaviour
{
    private bool isSkinned = false;
    private  Mesh meshTarget;

    public void SwapMesh(GameObject key)
    {
        string[] keyNameSplit = key.name.Split("@");
        string keyLayerName = keyNameSplit[0];

        //retrieve mesh renderer on key
        if (key.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer meshSkinnedRenderer))
        {
            meshTarget = meshSkinnedRenderer.sharedMesh;
            isSkinned = true;
        }
        if (key.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
        {
            meshTarget =  meshFilter.mesh;
            isSkinned = true;
        }

        //swap mesh on same layer object
        foreach (Transform child in transform)
        {

            string childLayerName = child.name;

            if (childLayerName == keyLayerName)
            {
                if (isSkinned == true) {
                    SkinnedMeshRenderer meshSkinnedRendererChild = child.GetComponent<SkinnedMeshRenderer>();
                    meshSkinnedRendererChild.sharedMesh = meshTarget;
                }
                if (isSkinned == false) {
                    MeshFilter meshRendererChild = child.GetComponent<MeshFilter>();
                    meshRendererChild.sharedMesh = meshTarget;
                    
                }
            }

        }

    }

}
