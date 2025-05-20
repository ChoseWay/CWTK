using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChoseWay.Editor
{
    public class CW_E_InitProject
    {
#if UNITY_EDITOR
        private static void CreateBasicFolder()
        {
            GenerateFolder();
            Debug.Log("Folders Created");
        }

        private static void CreateBasicMaterial()
        {
            GenerateBasicMaterial();
            Debug.Log("Materials Created");
        }

        private static void CreateBasicHierachy()
        {
            GenerateBasicHierachy();
            Debug.Log("Hierachy Created");
        }



        public static void GenerateFolder()
        {
            // 文件路径
            string path = Application.dataPath + "/";
            Directory.CreateDirectory(path + "_AUDIO");
            Directory.CreateDirectory(path + "_ANIM");
            Directory.CreateDirectory(path + "_PREFAB");
            Directory.CreateDirectory(path + "_MODEL");
            Directory.CreateDirectory(path + "_MATERIAL");
            Directory.CreateDirectory(path + "_MISC");
            Directory.CreateDirectory(path + "_SCRIPT");
            Directory.CreateDirectory(path + "_SCENE");
            Directory.CreateDirectory(path + "_SHADER");
            Directory.CreateDirectory(path + "_TEXTURE");
            Directory.CreateDirectory(path + "_FX");
            Directory.CreateDirectory(path + "_UI");
            Directory.CreateDirectory(path + "_VIDEO");
            Directory.CreateDirectory(path + "Resources");
            Directory.CreateDirectory(path + "StreamingAssets");

            AssetDatabase.Refresh();
        }

        static List<Color> list_BasicColor;
        private static void GenerateBasicMaterial()
        {
            string dir = "Assets/_MATERIAL/BasicColors/";
            Directory.CreateDirectory(dir);
            Debug.Log(dir);

            Material black = new Material(Shader.Find("Standard"));
            black.color = Color.black;
            AssetDatabase.CreateAsset(black, dir + "black" + ".mat");

            Material white = new Material(Shader.Find("Standard"));
            white.color = Color.white;
            AssetDatabase.CreateAsset(white, dir + "white" + ".mat");

            Material red = new Material(Shader.Find("Standard"));
            red.color = Color.red;
            AssetDatabase.CreateAsset(red, dir + "red" + ".mat");

            Material yellow = new Material(Shader.Find("Standard"));
            yellow.color = Color.yellow;
            AssetDatabase.CreateAsset(yellow, dir + "yellow" + ".mat");

            Material green = new Material(Shader.Find("Standard"));
            green.color = Color.green;
            AssetDatabase.CreateAsset(green, dir + "green" + ".mat");

            Material blue = new Material(Shader.Find("Standard"));
            blue.color = Color.blue;
            AssetDatabase.CreateAsset(blue, dir + "blue" + ".mat");
        }

        private static void GenerateBasicHierachy()
        {
            GameObject go;
            go = new GameObject("-----        MANAGER        -----");
            go = new GameObject("-----     ENVIROMENT     -----");
            go = new GameObject("-----           PLAYER          -----");
            go = new GameObject("-----           MODEL           -----");
            go = new GameObject("-----                 UI               -----");
            go = new GameObject("-----            AUDIO            -----");
        }
#endif
    }
}