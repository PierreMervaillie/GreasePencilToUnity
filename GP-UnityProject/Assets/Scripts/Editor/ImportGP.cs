using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
 using System.Globalization;

public class ImportGP : EditorWindow
{
    public Object source;
    public float deltaTime = 0.1f;
    public bool loop;
    public Shader shader;

    [MenuItem("GameObject/ImportGP")]
    public static void ShowWindow()
    {
        GetWindow<ImportGP>(false, "Grease Pencil Importer", true);
    }

    void OnGUI()
    {
        var style = GUI.skin.GetStyle("label");
        style.fontSize = 14; // whatever you set
        style.contentOffset = new Vector2(10f, 0f);




        GUILayout.Label("Grease Pencil Object", style);
        source = EditorGUILayout.ObjectField(source, typeof(Object), true);

        GUILayout.Label("Create Corresponding Animation", style);

        /* Not Working as intended
        //EditorGUILayout.FloatField("Delta Time", deltaTime);
        if (GUILayout.Button("Loop Animation"))
        {
            loop = !loop;
        }
        EditorGUILayout.Toggle("Loop Animation", loop);
        */

        GUILayout.Label("Create Animation");
        if (GUILayout.Button("Create Animation"))
        {
            string objPath = AssetDatabase.GetAssetPath(source);
            objPath = objPath.Substring(0, objPath.Length - 4);

            CreateAnimation(objPath);

        }

        GUILayout.Label("Create Corresponding Materials");
        shader = EditorGUILayout.ObjectField(shader, typeof(Shader), true) as Shader;

        if (GUILayout.Button("Create Materials"))
        {
            string objPath = AssetDatabase.GetAssetPath(source);
            objPath = objPath.Substring(0, objPath.Length - 4); ;

            CreateMaterials(objPath);

        }

        /*if (GUILayout.Button("Refresh Materials"))  ##TODO
        {
            string objPath = AssetDatabase.GetAssetPath(source);
            objPath = objPath.Substring(0, objPath.Length - 4); ;

            RefreshMaterials(objPath);

        }*/
    }

    void CreateAnimation(string pathID)
    {
        // get txt file (ID + "Keys")
        // get Data

        string path = pathID + "_Keys.txt";
        StreamReader reader = new StreamReader(path);
        string read = reader.ReadToEnd();
        reader.Close();

        string[] arr = read.Split('#');

        AnimationClip clip = new AnimationClip();

        // Set Loop
        if (loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

        }

        int i = 0;
        // create array for layers
        foreach (string ar in arr)
        {
            //Debug.Log(ar);
            i += 1;

            string[] ark = ar.Split(',');
            string layerName = ark[0];

            int j = 0;
            //for each key frame : add keyframe visibility
            foreach (string str in ark)
            {

                if (j != 0) {

                    AnimationCurve curve;
                    Keyframe[] keys;
                    if (j == 1)
                    {

                        keys = new Keyframe[2];
                        int keyTime = 0;
                        bool res = int.TryParse(str, out keyTime);
                        //keyTime = int.Parse(str);

                        keys[0] = new Keyframe((keyTime) * deltaTime, 1f);
                        keys[1] = new Keyframe((keyTime + 1) * deltaTime, 0f);

                    }
                    else
                    {

                        keys = new Keyframe[3];
                        int keyTime = 0;
                        bool res = int.TryParse(str, out keyTime);
                        //keyTime = int.Parse(str);

                        keys[0] = new Keyframe((keyTime - 1) * deltaTime, 0f);
                        keys[1] = new Keyframe((keyTime) * deltaTime, 1f);
                        keys[2] = new Keyframe((keyTime + 1) * deltaTime, 0f);

                    }


                    curve = new AnimationCurve(keys);
                    Debug.Log(layerName + "." + str+"check");
                    // Calculate constant tangent for animation curve
                    setTangent(curve);
                    clip.SetCurve(layerName + "." + str, typeof(GameObject), "m_IsActive", curve);


                }
                j += 1;

            }

        }

        Debug.Log(pathID);
        AssetDatabase.CreateAsset(clip, pathID + ".anim");
        AssetDatabase.SaveAssets();


    }

    void setTangent(AnimationCurve curve)
    {
        for (int i = 0; i < curve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }
        // remove last key
        //curve.RemoveKey(curve.keys.Length-1);
    }

    void CreateMaterials(string pathID)
    {

        string path = pathID + "_Materials.txt";
        // get txt file (ID + "Materials")
        StreamReader reader = new StreamReader(path);
        string read = reader.ReadToEnd();
        Debug.Log(read);
        reader.Close();

        string[] arr = read.Split('#');
        string textureName = "";

        // create material if they doesn't exist

        foreach (string ar in arr)
        {
            if (ar != "") {

                string[] arm = ar.Split(',');
                //Debug.Log( arm[1] + "__" + arm[2]);
                string materialName = arm[0];
                float materialColR = float.Parse(arm[1], CultureInfo.InvariantCulture);
                float materialColG = float.Parse(arm[2], CultureInfo.InvariantCulture);
                float materialColB = float.Parse(arm[3], CultureInfo.InvariantCulture);
                float materialColA = float.Parse(arm[4], CultureInfo.InvariantCulture);

                if (arm.Length > 5) {
                    textureName = arm[5];
                }
                bool materialExist = false;
                //check if it esist
                string[] materialsguid = AssetDatabase.FindAssets(materialName + ".mat" + " t:material");

                foreach (string materialguid in materialsguid)
                {

                    Debug.Log("Material  " + materialguid + "  already exists, use refresh to update it");
                    materialExist = true;

                }



                //Create Material if it doesnt
                if (materialExist == false)
                {
                    Material material = new Material(shader);

                    Color col = new Color(materialColR, materialColG, materialColB, materialColA);

                    material.SetColor("_Color", col);

                    if (arm.Length > 5)
                    {
                        textureName = textureName.Substring(0, textureName.Length - 4);
                        string[] texturesguid = AssetDatabase.FindAssets(textureName + " t:texture");
                        Debug.Log("textures " + texturesguid[0] + "__" + textureName);


                        if (texturesguid[0] != null) {

                            string texturePath = AssetDatabase.GUIDToAssetPath(texturesguid[0]);
                            Texture materialTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D));

                            Debug.Log(materialTexture);
                            material.SetTexture("_MainTex", materialTexture);
                        }
                    }

                    AssetDatabase.CreateAsset(material, pathID + materialName + ".mat");
                }

            }
        }

        Debug.Log(pathID);

    }


    void RefreshMaterials(string pathID)
    {

        Debug.Log("Refresh");

    }
}