using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
 using System.Globalization;
using System.Linq;
using UnityEditor.Rendering;
using UnityEditor.Animations;

public class ImportGreasePencil : EditorWindow
{
    public GameObject source;
    public MonoScript meshSwapScript;
    public float deltaTime = 0.1f;
    public bool animated;
    public bool loop;
    private List<GameObject> keyObjects;

    [MenuItem("GameObject/ImportGreasePencil")]
    public static void ShowWindow()
    {
        GetWindow<ImportGreasePencil>(false, "Grease Pencil Importer", true);
    }
    // The process of the script is to use Animation events to send mesh data that needs to be swapped in the newly created prefab
    void OnGUI()  
    {
        var style = GUI.skin.GetStyle("label");
        //style.fontSize = 14; //whatever you set
        //style.contentOffset = new Vector2(10f, 0f);

        GUILayout.Label("Grease Pencil Object", style);
        source = EditorGUILayout.ObjectField(source, typeof(Object), true) as GameObject;

        GUILayout.Label("GP_Importer_MeshSwap Script", style);
        meshSwapScript = EditorGUILayout.ObjectField(meshSwapScript, typeof(MonoScript), false) as MonoScript; // TODO try to find it if empty

        GUILayout.Label("Import Grease Pencil", style);
        if (GUILayout.Button("Create Prefab"))
        {           
            string objPath = AssetDatabase.GetAssetPath(source);
            objPath = objPath.Substring(0, objPath.Length - 4);

            GameObject prefab = PrefabUtility.InstantiatePrefab(source) as GameObject;    

            //add script on prefab
            prefab.AddComponent(meshSwapScript.GetClass());

            keyObjects.Clear();

            string nameLayer = "";
            List<GameObject> objsToDestroy = new List<GameObject>();


            //save objects to send them in the animation
            foreach (Transform saveChild in source.transform)
            {
                if(saveChild.childCount == 0){
                    keyObjects.Add(saveChild.gameObject); //replace by source?
                }
            }
            //find first object layer, delete other
            foreach (Transform child in prefab.transform)
            {
                if(child.childCount == 0){//TODO check if child with no child

                    string childName = child.name;
                    string[] childNameSplit = childName.Split("@");
                    string childLayerName = childNameSplit[0];
                    Debug.Log(childLayerName + "  " + nameLayer);

                    if (childLayerName != nameLayer){
                        nameLayer = childLayerName;
                        child.name = childLayerName;
                    } else {
                        objsToDestroy.Add(child.gameObject);                        
                    }
                }
            }

            foreach (GameObject objToDestroy in objsToDestroy)
            {
                Object.DestroyImmediate(objToDestroy);
            }
            
            CreateAnimation(objPath, prefab);

            PrefabUtility.SaveAsPrefabAsset(prefab, objPath + ".prefab");
        }
    }

    void CreateAnimation(string pathID, GameObject prefab)
    {
        List<AnimationEvent> animEvents = new List<AnimationEvent>();

        AnimationClip clip = new AnimationClip();
        
        foreach (GameObject keyObj in keyObjects)
        {

            string[] keyNameSplit = keyObj.name.Split("@");
            string layerName = keyNameSplit[0];
            int keyTime = int.Parse(keyNameSplit[1]);

            // use Animation Event Array bc we can't animate mesh swap directly in the animation data
            AnimationEvent animEvent = new AnimationEvent()
            {
            time = keyTime/clip.frameRate,
            objectReferenceParameter = keyObj,
            functionName = "SwapMesh",
            };

            animEvents.Add(animEvent);
        }  

        AnimationEvent[] animEventsArray = animEvents.ToArray();
        AnimationUtility.SetAnimationEvents(clip, animEventsArray);
             
        AssetDatabase.CreateAsset(clip, pathID + ".anim");
        AssetDatabase.SaveAssets();


        //Create Animator
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(pathID +".controller");
        //Add additive custom layer for Grease Pencil
        var gpLayer = new AnimatorControllerLayer
        {
            name = "Grease Pencil",
            blendingMode = AnimatorLayerBlendingMode.Additive,
            defaultWeight = 1.0f,
            stateMachine = new AnimatorStateMachine()
        };
        controller.AddLayer(gpLayer);
        var gpState = gpLayer.stateMachine.AddState("Grease Pencil", new Vector2(300, 200));
        gpState.motion = clip;

        Animator animator = prefab.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;  // Does not work
    }
}