using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEditor;
using UnityEngine;

namespace ChoseWay.Editor
{
    /// <summary>
    /// Unity包管理器工具类，用于查询、搜索和安装包
    /// </summary>
    public class CW_E_PackageManager
    {
        // 添加包的请求
        static AddRequest Request;
        // 搜索包的请求
        static SearchRequest searchRequest;
        // 存储包名称的列表
        static List<string> list = new List<string>();
        // 列出已安装包的请求
        static ListRequest listRequest=Client.List();

        /// <summary>
        /// 搜索已安装的包
        /// </summary>
        public static void SearchPackage()
        {
            listRequest = Client.List();
        }


        /// <summary>
        /// 查询指定名称的包是否已安装
        /// </summary>
        /// <param name="name_Package">包名称</param>
        /// <returns>如果包已安装返回true，否则返回false</returns>
        public static bool QueryPackage(string name_Package)
        {
            // 检查请求是否完成
            if (listRequest.IsCompleted)
            {
                if (listRequest.Status == StatusCode.Success)
                {
                    // 遍历已安装的包
                    foreach (var package in listRequest.Result)
                    {
                        if (package.name == name_Package)
                        {
                            //Debug.Log("yes");
                            return true;
                        }
                    }
                }
                else
                {
                    //Debug.Log("no find");
                    return false;
                }
            }
            //Debug.Log("no finish");
            return false;
        }


        /// <summary>
        /// 安装指定名称的包
        /// </summary>
        /// <param name="name_Package">要安装的包名称</param>
        public static void StaticInputPackage(string name_Package)
        {
            // 发起添加包的请求
            Request = Client.Add(name_Package);

            // 注册更新回调以监控安装进度
            EditorApplication.update += Progress;
        }

        /// <summary>
        /// 批量安装列表中的包
        /// </summary>
        static void RequestArray()
        {
            if (Request.IsCompleted)
            {
                for (int i = 1; i < list.Count; i++)
                {
                    // 发起添加包的请求
                    Request = Client.Add(list[i]);
                    // 注册更新回调以监控安装进度
                    EditorApplication.update += Progress;
                    // 如果是最后一个包，取消注册批量安装回调
                    if (i == list.Count - 1)
                    {
                        EditorApplication.update -= RequestArray;
                    }
                    continue;
                }
            }
        }


        /// <summary>
        /// 监控包安装进度的回调方法
        /// </summary>
        static void Progress()
        {
            if (Request.IsCompleted)
            {
                if (Request.Status == StatusCode.Success)
                    Debug.Log("Installed: " + Request.Result.packageId);
                else if (Request.Status >= StatusCode.Failure)
                    Debug.Log(Request.Error.message);

                // 安装完成后取消注册回调
                EditorApplication.update -= Progress;
            }
        }
    }
}