using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;
using Sirenix.Utilities.Editor;

namespace ChoseWay.Editor
{
    public class CW_E_WelcomScreen : OdinEditorWindow
    {
        public const string VERSION = "v1.0.5.20250507";
        public static Texture texture;

        #region 标题
        [MenuItem("水熊工具箱/主页")]
        static void ShowWindow()
        {
            CW_E_WelcomScreen window = GetWindow<CW_E_WelcomScreen>();
            GUIContent content = new GUIContent();
            texture = (Texture)Resources.Load("CWTK_Recources/InspectorTexture_Logo");
            content.image = texture;
            window.maxSize = new Vector2(532, 1200);
            window.minSize = new Vector2(532, 650);
            window.titleContent = content;
            window.Show();
            CW_E_PackageManager.SearchPackage();
        }


        [PropertyOrder(0), HorizontalGroup("Title", MarginLeft = 10, MarginRight = 10)]
        [VerticalGroup("Title/Logo")]
        [OnInspectorGUI]
        private void ShowImage()
        {
            GUILayout.Label((Texture)Resources.Load("CWTK_Recources/CWTK_Logo"));
            GUILayout.Space(-25); // 向上偏移
            GUIStyle rightAlignStyle = new GUIStyle(GUI.skin.label);
            rightAlignStyle.alignment = TextAnchor.MiddleRight;
            rightAlignStyle.margin = new RectOffset(0, 10, 0, 0); // 右侧留出10像素（约2个字符）的安全边距
            GUILayout.Label(VERSION, rightAlignStyle);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Space(-25); // 向上偏移
            GUILayout.Label("主页", titleStyle);
        }


        #region 分割线
        [PropertyOrder(0)]
        [HorizontalGroup("分割线", MarginLeft = 10, MarginRight = 10)]
        [OnInspectorGUI]
        private void ShowDivider()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        #endregion

        #endregion 标题



        #region 色彩空间
        bool toggle_ColorSpaceIsLinear;
        [PropertyOrder(1), BoxGroup("第一步", centerLabel: true)]
        [HorizontalGroup("第一步/colorSpace")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_ColorSpace = $"转换色彩空间";

        [PropertyOrder(1), BoxGroup("第一步")]
        [HorizontalGroup("第一步/colorSpace")]
        [HideLabel]
        [DisplayAsString(false)]
        [DisableIf("toggle_ColorSpaceIsLinear")]
        [InlineButton("Excute_ColorSpaceToLinear", "   转换   ", ButtonColor = "green")]
        public string info_ColorSpace = $"当前色彩空间为";
        public void Excute_ColorSpaceToLinear()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Repaint(); // 刷新窗口显示更新后的色彩空间状态
        }
        [OnInspectorGUI]
        void CheckColorSpace()
        {
            toggle_ColorSpaceIsLinear = PlayerSettings.colorSpace == ColorSpace.Gamma ? false : true;
            info_ColorSpace = toggle_ColorSpaceIsLinear ? "当前色彩空间为Linear" : "当前色彩空间为Gamma";
        }
        #endregion

        #region 检查并导入初始package

        [OnInspectorGUI]
        private void CheckPackage()
        {
            #region 查询Package

            isPackage_TextMeshPro = CW_E_PackageManager.QueryPackage("com.unity.textmeshpro");
            info_InputPackage_TextMeshPro = isPackage_TextMeshPro ? "已导入" : "未导入";

            isPackage_Recorder = CW_E_PackageManager.QueryPackage("com.unity.recorder");
            info_InputPackage_Recorder = isPackage_Recorder ? "已导入" : "未导入";

            isPackage_PostProcessing = CW_E_PackageManager.QueryPackage("com.unity.postprocessing");
            info_InputPackage_PostProcessing = isPackage_PostProcessing ? "已导入" : "未导入";

            isPackage_CursorCompiler = CW_E_PackageManager.QueryPackage("com.boxqkrtm.ide.cursor");
            info_InputPackage_CursorCompiler = isPackage_CursorCompiler ? "已导入" : "未导入";

            isPackage_Graphy = CW_E_PackageManager.QueryPackage("com.tayx.graphy");
            info_InputPackage_Graphy = isPackage_Graphy ? "已导入" : "未导入";

            #endregion
        }

        private bool isPackage_TextMeshPro = false;
        [PropertyOrder(2), BoxGroup("第二步", centerLabel: true)]
        [HorizontalGroup("第二步/package_TextMeshPro")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_Package_TextMeshPro = $"TextMeshpro";

        [PropertyOrder(2), BoxGroup("第二步")]
        [HorizontalGroup("第二步/package_TextMeshPro")]
        [HideLabel]
        [DisplayAsString(false)]
        [DisableIf("isPackage_TextMeshPro")]
        [InlineButton("Excute_InputPackage_TextMeshPro", "   导入   ", ButtonColor = "green")]
        public string info_InputPackage_TextMeshPro = $"未导入";
        public void Excute_InputPackage_TextMeshPro()
        {
            string packageName = "com.unity.textmeshpro"; // 替换为实际的官方包名
            CW_E_PackageManager.StaticInputPackage(packageName);
        }


        private bool isPackage_Recorder = false;
        [PropertyOrder(2), BoxGroup("第二步", centerLabel: true)]
        [HorizontalGroup("第二步/package_Recorder")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_Package_Recorder = $"Recorder";

        [PropertyOrder(2), BoxGroup("第二步")]
        [HorizontalGroup("第二步/package_Recorder")]
        [HideLabel]
        [DisplayAsString(false)]
        [DisableIf("isPackage_Recorder")]
        [InlineButton("Excute_InputPackage_Recorder", "   导入   ", ButtonColor = "green")]
        public string info_InputPackage_Recorder = $"未导入";
        public void Excute_InputPackage_Recorder()
        {
            string packageName = "com.unity.recorder"; // 替换为实际的官方包名
            CW_E_PackageManager.StaticInputPackage(packageName);
        }

        private bool isPackage_PostProcessing = false;
        [PropertyOrder(2), BoxGroup("第二步", centerLabel: true)]
        [HorizontalGroup("第二步/package_PostProcessing")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_Package_PostProcessing = $"PostProcessing";

        [PropertyOrder(2), BoxGroup("第二步")]
        [HorizontalGroup("第二步/package_PostProcessing")]
        [HideLabel]
        [DisplayAsString(false)]
        [DisableIf("isPackage_PostProcessing")]
        [InlineButton("Excute_InputPackage_PostProcessing", "   导入   ", ButtonColor = "green")]
        public string info_InputPackage_PostProcessing = $"未导入";
        public void Excute_InputPackage_PostProcessing()
        {
            string packageName = "com.unity.postprocessing"; // 替换为实际的官方包名
            CW_E_PackageManager.StaticInputPackage(packageName);
        }

        private bool isPackage_CursorCompiler = false;
        [PropertyOrder(2), BoxGroup("第二步", centerLabel: true)]
        [HorizontalGroup("第二步/package_CursorCompiler")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_Package_CursorCompiler = $"Cursor编译器";

        [PropertyOrder(2), BoxGroup("第二步")]
        [HorizontalGroup("第二步/package_CursorCompiler")]
        [HideLabel]
        [DisplayAsString(false)]
        [DisableIf("isPackage_CursorCompiler")]
        [InlineButton("Excute_InputPackage_CursorCompiler", "   导入   ", ButtonColor = "green")]
        public string info_InputPackage_CursorCompiler = $"未导入";
        public void Excute_InputPackage_CursorCompiler()
        {
            string packageUrl = "https://github.com/boxqkrtm/com.unity.ide.cursor.git";
            CW_E_PackageManager.StaticInputPackage(packageUrl);
        }

        private bool isPackage_Graphy = false;
        [PropertyOrder(2), BoxGroup("第二步", centerLabel: true)]
        [HorizontalGroup("第二步/package_Graphy")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_Package_Graphy = $"Graphy-FPS统计";

        [PropertyOrder(2), BoxGroup("第二步")]
        [HorizontalGroup("第二步/package_Graphy")]
        [HideLabel]
        [DisplayAsString(false)]
        [DisableIf("isPackage_Graphy")]
        [InlineButton("Excute_InputPackage_Graphy", "   导入   ", ButtonColor = "green")]
        public string info_InputPackage_Graphy = $"未导入";
        public void Excute_InputPackage_Graphy()
        {
            string packageUrl = "https://github.com/Tayx94/graphy.git";
            CW_E_PackageManager.StaticInputPackage(packageUrl);
        }
        #endregion

        #region 项目初始化

        [PropertyOrder(3), BoxGroup("第三步", centerLabel: true)]
        [HorizontalGroup("第三步/project_init")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_CreateBasicFolder = $"创建基本目录";

        [PropertyOrder(3), BoxGroup("第三步")]
        [HorizontalGroup("第三步/project_init")]
        [HideLabel]
        [DisplayAsString(false)]
        [InlineButton("Excute_CreateBasicFolder", "   创建   ", ButtonColor = "green")]
        public string info_CreateBasicFolder = $"未创建";

        [OnInspectorGUI]
        private void CheckFolderStatus()
        {
            // 检查是否已经创建了基本目录
            bool foldersExist =
                System.IO.Directory.Exists(Application.dataPath + "/_AUDIO") &&
                System.IO.Directory.Exists(Application.dataPath + "/_ANIM") &&
                System.IO.Directory.Exists(Application.dataPath + "/_PREFAB") &&
                System.IO.Directory.Exists(Application.dataPath + "/_MODEL") &&
                System.IO.Directory.Exists(Application.dataPath + "/_MATERIAL") &&
                System.IO.Directory.Exists(Application.dataPath + "/_SCRIPT");

            info_CreateBasicFolder = foldersExist ? "已创建" : "未创建";
        }

        public void Excute_CreateBasicFolder()
        {
            // 由于GenerateFolder是私有方法，通过MenuItem调用
            EditorApplication.ExecuteMenuItem("水熊工具箱/1.初始化/2.创建基本目录");
            EditorUtility.DisplayDialog("创建基本目录", "基本目录创建完成！", "确定");
            // 刷新文件夹状态
            CheckFolderStatus();
            CheckMaterialStatus();
        }

        [PropertyOrder(3), BoxGroup("第三步", centerLabel: true)]
        [HorizontalGroup("第三步/basic_material")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_CreateBasicMaterial = $"创建基本材质球";

        [PropertyOrder(3), BoxGroup("第三步")]
        [HorizontalGroup("第三步/basic_material")]
        [HideLabel]
        [DisplayAsString(false)]
        [InlineButton("Excute_CreateBasicMaterial", "   创建   ", ButtonColor = "green")]
        public string info_CreateBasicMaterial = $"未创建";

        [OnInspectorGUI]
        private void CheckMaterialStatus()
        {
            // 检查是否已经创建了基本材质球
            bool materialsExist =
                System.IO.Directory.Exists(Application.dataPath + "/_MATERIAL/BasicColors") &&
                AssetDatabase.LoadAssetAtPath<Material>("Assets/_MATERIAL/BasicColors/red.mat") != null &&
                AssetDatabase.LoadAssetAtPath<Material>("Assets/_MATERIAL/BasicColors/blue.mat") != null;

            info_CreateBasicMaterial = materialsExist ? "已创建" : "未创建";
        }

        public void Excute_CreateBasicMaterial()
        {
            // 由于GenerateBasicMaterial是私有方法，通过MenuItem调用
            EditorApplication.ExecuteMenuItem("水熊工具箱/1.初始化/3.创建基本材质球");
            EditorUtility.DisplayDialog("创建基本材质球", "基本材质球创建完成！", "确定");
            // 刷新材质球状态
            CheckMaterialStatus();
        }

        [PropertyOrder(3), BoxGroup("第三步", centerLabel: true)]
        [HorizontalGroup("第三步/basic_hierarchy")]
        [HideLabel]
        [DisplayAsString(false)]
        public string title_CreateBasicHierarchy = $"创建基本场景层级";

        [PropertyOrder(3), BoxGroup("第三步")]
        [HorizontalGroup("第三步/basic_hierarchy")]
        [HideLabel]
        [DisplayAsString(false)]
        [InlineButton("Excute_CreateBasicHierarchy", "   创建   ", ButtonColor = "green")]
        public string info_CreateBasicHierarchy = $"未创建";

        public void Excute_CreateBasicHierarchy()
        {
            // 由于GenerateBasicHierachy是私有方法，通过MenuItem调用
            EditorApplication.ExecuteMenuItem("水熊工具箱/2.开始使用/1.创建基本场景层级");
            EditorUtility.DisplayDialog("创建基本场景层级", "基本场景层级创建完成！", "确定");

            // 场景层级创建后，标记为已创建
            info_CreateBasicHierarchy = "已创建";
        }

        #endregion




    }
}