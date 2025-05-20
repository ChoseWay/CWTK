using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CsvDataLoader : MonoBehaviour
{
    public static string[] _NAME_ASSEMBLY;
    public static string[] _NAME_MODEL;
    public static string[] _TEXT;

    public static List<string> list_NAME_YYJ;
    public static List<string> list_NAME_MODEL;
    public static List<string> list_TEXT;
    public static Dictionary<string, string> dic_NAME_YYJ;
    public static Dictionary<string, string> dic_NAME_TEXT;
    void Awake()
    {
        //list_ID = new List<string>();
        //list_Name = new List<string>();
        //list_Type = new List<string>();
        dic_NAME_TEXT = new Dictionary<string, string>();
    }
    private void Start()
    {
        CSVLoadAssembly("/FishList.csv", _NAME_MODEL, _NAME_ASSEMBLY, _TEXT);
        
    }
    void Update()
    {

    }
    /// <summary>
    /// 从csv表格中加载数据/模型部件及介绍
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="numberFish">鱼类编号</param>
    /// <param name="nameFish">鱼类名称</param>
    /// <param name="textFish">文本介绍</param>
    void CSVLoadAssembly(string path, string[] numberFish, string[] nameFish, string[] textFish)
    {
        /* CSV文件路径 */
        string filePath = Application.streamingAssetsPath + path;
        /* 读取CSV文件，一行行读取 */
        string[] fileData = File.ReadAllLines(filePath);
        //条目总数=CSV行数-1
        Debug.Log(fileData.Length);
        numberFish = new string[fileData.Length - 1];
        nameFish = new string[fileData.Length - 1];
        textFish = new string[fileData.Length - 1];
        /* 第二行开始是数据 */
        for (int i = 1; i < fileData.Length; i++)
        {
            /* 每一行的内容都是逗号分隔，读取每一列的值 */
            string[] lineData = fileData[i].Split(',');

            numberFish[i - 1] = lineData[0].ToString();
            nameFish[i - 1] = lineData[1].ToString();
            textFish[i - 1] = lineData[2].ToString();
            Debug.Log(lineData[2].ToString());
            //TextContentManager.instance.dic_AssemblyNameToContent.Add(lineData[1], lineData[2]);

            //dic_NAME_TEXT.Add(nameAssembly[i - 1], textAssembly[i - 1]);
        }
        for (int i = 0; i < fileData.Length-1; i++)
        {
            //if (TextContentManager.instance.dic_AssemblyNameToContent.TryGetValue(nameAssembly[i], out string content))
            //{
            //    Debug.Log(content);
            //}
        }
    }
}
