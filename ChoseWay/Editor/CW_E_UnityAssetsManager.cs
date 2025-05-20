using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

namespace ChoseWay.Editor
{
    public class CW_E_UnityAssetsManager : EditorWindow
    {
        [MenuItem("水熊工具箱/资源包预览工具")]
        public static void ShowWindow()
        {
            GetWindow<CW_E_UnityAssetsManager>("资源包预览工具");
        }

        private string packagePath = "";
        private List<PreviewItem> previewItems = new List<PreviewItem>();
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private int selectedTab = 0;
        private string[] tabNames = { "全部", "模型", "贴图", "材质", "脚本", "文本", "其他" };
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        
        // 包图标相关变量
        private Texture2D packageIcon;
        private string packageName;
        
        // 分页相关变量
        private int currentPage = 0;
        private int[] pageSizeOptions = { 10, 20, 50 };
        private int selectedPageSizeIndex = 1; // 默认选择20
        private int totalPages = 1;
        private List<PreviewItem> currentPageItems = new List<PreviewItem>();
        
        // 预览相关变量
        private PreviewItem selectedItem;
        private UnityEditor.Editor previewEditor;
        private Vector2 previewScrollPosition;
        private float previewZoom = 1.0f;
        private Vector2 previewDragPosition;
        private Quaternion previewRotation = Quaternion.identity;
        private bool isDragging = false;
        private string tempExtractPath; // 临时解压路径
        private GUIStyle buttonHeaderStyle; // 用于按钮标题的样式

        // 添加临时资源路径管理
        private List<string> tempImportedAssets = new List<string>();
        private string tempAssetDir = "Assets/Temp_UnityPackageViewer";

        // 添加TextEditor组件用于脚本预览
        private TextEditor scriptTextEditor;
        private bool scriptPreviewNeedsRefresh = false;

        private class PreviewItem
        {
            public string Path;
            public string Name;
            public string Extension;
            public string Type;
            public Texture2D Preview;
            public string AssetInfoPath; // 存储资源数据的路径，而不是直接加载
            public Object LoadedObject;
            public bool IsLoaded;
            
            // 网格信息
            public int MeshCount;
            public int VertexCount;
            public int TriangleCount;
        }

        /// <summary>
        /// 窗口启用时初始化
        /// </summary>
        private void OnEnable()
        {
            // 初始化分页数据
            currentPage = 0;
            UpdatePagination();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 左侧资源列表区域
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
            DrawResourceList();
            EditorGUILayout.EndVertical();
            
            // 右侧预览区域
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
            DrawPreviewArea();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制资源列表界面
        /// </summary>
        void DrawResourceList()
        {
            EditorGUILayout.BeginVertical();

            // 拖放区域和功能按钮水平排列
            EditorGUILayout.BeginHorizontal();
            
            // 包图标区域
            if (packageIcon != null)
            {
                // 设置图标区域背景
                GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(80), GUILayout.Height(60));
                
                // 计算适合的图标尺寸
                float iconSize = 40;
                
                // 绘制图标
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                iconRect.x = (80 - iconSize) / 2; // 居中显示
                iconRect.y += (60 - iconSize - 16) / 2; // 给名称留出空间
                GUI.DrawTexture(iconRect, packageIcon, ScaleMode.ScaleToFit);
                
                // 绘制包名
                GUIStyle packageNameStyle = new GUIStyle(EditorStyles.miniLabel);
                packageNameStyle.alignment = TextAnchor.MiddleCenter;
                packageNameStyle.wordWrap = true;
                
                Rect nameRect = new Rect(0, iconRect.y + iconSize + 2, 80, 14);
                GUI.Label(nameRect, packageName, packageNameStyle);
                
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white;
            }
            
            // 拖放区域
            GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(60), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("将Unity资源包拖放到此处", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("支持.unitypackage格式", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            
            // 获取拖放区域的矩形
            Rect dropArea = GUILayoutUtility.GetLastRect();//#2bb72b

            // 清理按钮 - 使用富文本标签放大文字并设置为绿色
            GUIStyle bigButtonStyle = new GUIStyle(GUI.skin.button);
            bigButtonStyle.richText = true;
            
            // 设置清理按钮为绿色
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // 绿色
            if (GUILayout.Button("<size=24><b><color=#12B900>清理</color></b></size>\n临时文件", bigButtonStyle, GUILayout.Height(60), GUILayout.Width(80)))
            {
                CleanupAllTempFiles();
            }
            
            // 卸载按钮 - 使用富文本标签放大文字并设置为红色
            GUI.enabled = !string.IsNullOrEmpty(packagePath); // 只有当加载了package时按钮才可用
            GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f); // 红色
            if (GUILayout.Button("<size=24><b><color=#B80700>卸载</color></b></size>\n当前资源包", bigButtonStyle, GUILayout.Height(60), GUILayout.Width(80)))
            {
                UnloadCurrentPackage();
            }
            GUI.enabled = true; // 恢复按钮状态
            GUI.backgroundColor = originalColor; // 恢复原始颜色
            
            EditorGUILayout.EndHorizontal();

            UnityEngine.Event evt = UnityEngine.Event.current;
            
            // 处理拖放事件
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    // 检查鼠标是否在拖放区域内
                    if (dropArea.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            
                            foreach (string path in DragAndDrop.paths)
                            {
                                if (path.EndsWith(".unitypackage"))
                                {
                                    packagePath = path;
                                    LoadPackage(path);
                                    // 重置页码
                                    currentPage = 0;
                                    UpdatePagination();
                                    break;
                                }
                            }
                        }
                        
                        evt.Use();
                    }
                    break;
            }

            // 显示当前加载的包路径
            if (!string.IsNullOrEmpty(packagePath))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("当前加载的包:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(packagePath, EditorStyles.wordWrappedLabel);
            }

            // 搜索框
            if (previewItems.Count > 0)
            {
                EditorGUILayout.Space();
                
                // 检查搜索框变化
                string newSearchFilter = EditorGUILayout.TextField("搜索:", searchFilter);
                if (newSearchFilter != searchFilter)
                {
                    searchFilter = newSearchFilter;
                    currentPage = 0; // 重置到第一页
                    UpdatePagination();
                }
                
                // 标签页
                int newSelectedTab = GUILayout.Toolbar(selectedTab, tabNames);
                if (newSelectedTab != selectedTab)
                {
                    selectedTab = newSelectedTab;
                    currentPage = 0; // 重置到第一页
                    UpdatePagination();
                }
                
                EditorGUILayout.Space();
                
                // 显示资源列表
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                // 使用分页后的资源
                var groupedItems = currentPageItems
                    .GroupBy(item => Path.GetDirectoryName(item.Path).Replace("\\", "/"))
                    .OrderBy(g => g.Key);

                foreach (var group in groupedItems)
                {
                    if (!foldoutStates.ContainsKey(group.Key))
                    {
                        foldoutStates[group.Key] = true;
                    }
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    foldoutStates[group.Key] = EditorGUILayout.Foldout(foldoutStates[group.Key], group.Key, true);
                    
                    if (foldoutStates[group.Key])
                    {
                        foreach (var item in group)
                        {
                            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                            
                            // 预览图
                            if (GUILayout.Button(item.Preview, GUILayout.Width(64), GUILayout.Height(64)))
                            {
                                SelectItem(item);
                            }
                            
                            EditorGUILayout.BeginVertical();
                            if (GUILayout.Button(item.Name, EditorStyles.boldLabel))
                            {
                                SelectItem(item);
                            }
                            EditorGUILayout.LabelField($"类型: {item.Type}", EditorStyles.miniLabel);
                            
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
                
                // 分页控制
                DrawPaginationControls();
            }

            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 绘制分页控制界面
        /// </summary>
        void DrawPaginationControls()
        {
            if (totalPages <= 0) return;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // 上一页按钮
            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("上一页", GUILayout.Width(80)))
            {
                currentPage--;
                UpdatePagination();
            }
            
            // 页码显示
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"第 {currentPage + 1} 页，共 {totalPages} 页，总计 {GetFilteredItemsCount()} 个资源", 
                EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            
            // 下一页按钮
            GUI.enabled = currentPage < totalPages - 1;
            if (GUILayout.Button("下一页", GUILayout.Width(80)))
            {
                currentPage++;
                UpdatePagination();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            // 每页显示数量选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("每页显示:", GUILayout.Width(60));
            int newPageSizeIndex = EditorGUILayout.Popup(selectedPageSizeIndex, 
                new string[] { "10个", "20个", "50个" }, GUILayout.Width(80));
                
            if (newPageSizeIndex != selectedPageSizeIndex)
            {
                selectedPageSizeIndex = newPageSizeIndex;
                currentPage = 0; // 重置到第一页
                UpdatePagination();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 更新分页数据
        /// </summary>
        private void UpdatePagination()
        {
            int filteredCount = GetFilteredItemsCount();
            int pageSize = pageSizeOptions[selectedPageSizeIndex];
            
            // 计算总页数
            totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredCount / pageSize));
            
            // 确保当前页在有效范围内
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
            
            // 获取当前页的项目
            UpdateCurrentPageItems();
            
            Repaint(); // 刷新界面
        }
        
        /// <summary>
        /// 获取筛选后的资源总数
        /// </summary>
        private int GetFilteredItemsCount()
        {
            return previewItems.Count(item => 
                FilterByTab(item) && 
                (string.IsNullOrEmpty(searchFilter) || 
                 item.Name.ToLower().Contains(searchFilter.ToLower()))
            );
        }
        
        /// <summary>
        /// 更新当前页显示的资源项目
        /// </summary>
        private void UpdateCurrentPageItems()
        {
            currentPageItems.Clear();
            
            int pageSize = pageSizeOptions[selectedPageSizeIndex];
            int startIndex = currentPage * pageSize;
            
            // 获取筛选后的所有项目
            var filteredItems = previewItems
                .Where(item => FilterByTab(item) && 
                       (string.IsNullOrEmpty(searchFilter) || 
                        item.Name.ToLower().Contains(searchFilter.ToLower())))
                .OrderBy(item => Path.GetDirectoryName(item.Path))
                .ThenBy(item => item.Name)
                .ToList();
            
            // 提取当前页的项目
            for (int i = startIndex; i < filteredItems.Count && i < startIndex + pageSize; i++)
            {
                currentPageItems.Add(filteredItems[i]);
            }
        }

        /// <summary>
        /// 绘制预览区域界面
        /// </summary>
        void DrawPreviewArea()
        {
            // 预览区域标题
            EditorGUILayout.LabelField("预览窗口", EditorStyles.boldLabel);
            
            // 预览区域背景
            Rect previewRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, 
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
            
            // 处理预览区域的事件
            HandlePreviewEvents(previewRect);
            
            if (selectedItem == null)
            {
                // 没有选中项时显示提示
                GUI.Label(previewRect, "选择一个资源进行预览", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // 如果资源未加载，先加载
            if (!selectedItem.IsLoaded)
            {
                LoadAssetForPreview(selectedItem);
            }
            
            // 根据资源类型显示不同的预览
            switch (selectedItem.Type)
            {
                case "模型":
                    DrawModelPreview(previewRect);
                    break;
                case "贴图":
                    DrawTexturePreview(previewRect);
                    break;
                case "材质":
                    DrawMaterialPreview(previewRect);
                    break;
                case "脚本":
                case "文本":
                    DrawScriptPreview(previewRect);
                    
                    // 特别处理脚本预览的事件，确保滚动功能正常
                    if (UnityEngine.Event.current.type == EventType.ScrollWheel &&
                        previewRect.Contains(UnityEngine.Event.current.mousePosition))
                    {
                        // 确保滚轮事件被消费
                        UnityEngine.Event.current.Use();
                        // 标记窗口需要重绘
                        Repaint();
                    }
                    break;
                default:
                    DrawDefaultPreview(previewRect);
                    break;
            }
            
            // 显示资源信息
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"名称: {selectedItem.Name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"路径: {selectedItem.Path}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"类型: {selectedItem.Type}", EditorStyles.miniLabel);
            
            // 根据资源类型显示额外信息
            switch (selectedItem.Type)
            {
                case "贴图":
                    if (selectedItem.LoadedObject is Texture2D texture)
                    {
                        EditorGUILayout.LabelField($"尺寸: {texture.width} x {texture.height}");
                        EditorGUILayout.LabelField($"格式: {texture.format}");
                    }
                    break;
                case "模型":
                    EditorGUILayout.LabelField($"网格数量: {selectedItem.MeshCount}");
                    if (selectedItem.VertexCount > 0)
                    {
                        EditorGUILayout.LabelField($"顶点数: {selectedItem.VertexCount}");
                        EditorGUILayout.LabelField($"三角形数: {selectedItem.TriangleCount}");
                    }
                    break;
                case "脚本":
                case "文本":
                    if (selectedItem.LoadedObject is TextAsset textAsset)
                    {
                        int lineCount = textAsset.text.Split('\n').Length;
                        EditorGUILayout.LabelField($"行数: {lineCount}");
                        
                        // 显示文本文件特定信息
                        if (selectedItem.Type == "文本")
                        {
                            string fileFormat = Path.GetExtension(selectedItem.Name).ToLower();
                            if (!string.IsNullOrEmpty(fileFormat))
                            {
                                EditorGUILayout.LabelField($"格式: {fileFormat.TrimStart('.')}");
                            }
                            
                            // 显示文件大小
                            if (File.Exists(selectedItem.AssetInfoPath))
                            {
                                long fileSize = new FileInfo(selectedItem.AssetInfoPath).Length;
                                string sizeStr = fileSize < 1024 ? $"{fileSize} 字节" :
                                               fileSize < 1024 * 1024 ? $"{fileSize / 1024.0f:F2} KB" :
                                               $"{fileSize / (1024.0f * 1024.0f):F2} MB";
                                EditorGUILayout.LabelField($"大小: {sizeStr}");
                            }
                        }
                    }
                    break;
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 处理预览区域中的鼠标和键盘事件
        /// </summary>
        /// <param name="previewRect">预览区域的矩形范围</param>
        void HandlePreviewEvents(Rect previewRect)
        {
            UnityEngine.Event evt = UnityEngine.Event.current;
            
            if (!previewRect.Contains(evt.mousePosition))
                return;
            
            // 处理鼠标滚轮缩放
            if (evt.type == EventType.ScrollWheel)
            {
                previewZoom = Mathf.Clamp(previewZoom - evt.delta.y * 0.05f, 0.5f, 5f);
                evt.Use();
                Repaint();
            }
            
            // 处理鼠标左键拖动旋转
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                isDragging = true;
                previewDragPosition = evt.mousePosition;
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                isDragging = false;
                evt.Use();
            }
            else if (evt.type == EventType.MouseDrag && isDragging)
            {
                // 计算鼠标移动的距离
                Vector2 delta = evt.mousePosition - previewDragPosition;
                previewDragPosition = evt.mousePosition;
                
                // 应用旋转
                if (selectedItem != null && (selectedItem.Type == "模型" || selectedItem.Type == "材质"))
                {
                    // 旋转预览对象
                    previewRotation *= Quaternion.Euler(delta.y, -delta.x, 0);
                    Repaint();
                }
                
                evt.Use();
            }
        }

        /// <summary>
        /// 绘制模型资源的预览
        /// </summary>
        /// <param name="previewRect">预览区域的矩形范围</param>
        void DrawModelPreview(Rect previewRect)
        {
            if (selectedItem.LoadedObject is GameObject model)
            {
                if (previewEditor == null || previewEditor.target != model)
                {
                    if (previewEditor != null)
                    {
                        UnityEngine.Object.DestroyImmediate(previewEditor);
                    }
                    previewEditor = UnityEditor.Editor.CreateEditor(model);
                }
                
                // 绘制预览背景
                GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
                
                // 使用Editor的OnInteractivePreviewGUI方法绘制模型预览
                previewEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.helpBox);
            }
            else
            {
                GUI.Label(previewRect, "无法预览此模型", EditorStyles.centeredGreyMiniLabel);
            }
        }

        /// <summary>
        /// 绘制贴图资源的预览
        /// </summary>
        /// <param name="previewRect">预览区域的矩形范围</param>
        void DrawTexturePreview(Rect previewRect)
        {
            if (selectedItem.LoadedObject is Texture2D texture)
            {
                // 计算纹理绘制区域
                float textureRatio = (float)texture.width / texture.height;
                float rectRatio = previewRect.width / previewRect.height;
                
                Rect drawRect = previewRect;
                
                if (textureRatio > rectRatio)
                {
                    // 纹理更宽
                    float height = previewRect.width / textureRatio;
                    drawRect.y += (previewRect.height - height) / 2;
                    drawRect.height = height;
                }
                else
                {
                    // 纹理更高
                    float width = previewRect.height * textureRatio;
                    drawRect.x += (previewRect.width - width) / 2;
                    drawRect.width = width;
                }
                
                // 应用缩放
                drawRect.width *= previewZoom;
                drawRect.height *= previewZoom;
                
                // 居中
                drawRect.x = previewRect.x + (previewRect.width - drawRect.width) / 2;
                drawRect.y = previewRect.y + (previewRect.height - drawRect.height) / 2;
                
                EditorGUI.DrawPreviewTexture(drawRect, texture);
            }
            else
            {
                GUI.Label(previewRect, "无法预览此贴图", EditorStyles.centeredGreyMiniLabel);
            }
        }

        /// <summary>
        /// 绘制材质资源的预览
        /// </summary>
        /// <param name="previewRect">预览区域的矩形范围</param>
        void DrawMaterialPreview(Rect previewRect)
        {
            if (selectedItem.LoadedObject is Material material)
            {
                if (previewEditor == null || previewEditor.target != material)
                {
                    if (previewEditor != null)
                    {
                        UnityEngine.Object.DestroyImmediate(previewEditor);
                    }
                    previewEditor = UnityEditor.Editor.CreateEditor(material);
                }
                
                // 绘制预览背景
                GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
                
                // 使用Editor的OnPreviewGUI方法绘制材质预览
                previewEditor.OnPreviewGUI(previewRect, EditorStyles.helpBox);
            }
            else
            {
                GUI.Label(previewRect, "无法预览此材质", EditorStyles.centeredGreyMiniLabel);
            }
        }

        /// <summary>
        /// 绘制脚本和文本资源的预览
        /// </summary>
        /// <param name="previewRect">预览区域的矩形范围</param>
        void DrawScriptPreview(Rect previewRect)
        {
            if (selectedItem == null || (selectedItem.Type != "脚本" && selectedItem.Type != "文本"))
            {
                GUI.Box(previewRect, "请选择一个脚本或文本文件", EditorStyles.helpBox);
                scriptTextEditor = null; // 清除编辑器
                return;
            }

            // 确保TextEditor已初始化
            if (scriptTextEditor == null || scriptPreviewNeedsRefresh)
            {
                // 重新加载脚本内容
                string scriptContent = LoadScriptContent(selectedItem);
                if (string.IsNullOrEmpty(scriptContent))
                {
                    GUI.Box(previewRect, "无法加载脚本内容", EditorStyles.helpBox);
                    return;
                }

                // 创建新的TextEditor
                scriptTextEditor = new TextEditor
                {
                    text = scriptContent,
                    multiline = true
                };
                
                // 重置滚动位置
                scriptTextEditor.MoveTextStart();
                scriptPreviewNeedsRefresh = false;
            }

            // 绘制背景
            GUI.Box(previewRect, "", EditorStyles.helpBox);

            // 创建实际的文本区域（留出边距）
            Rect textRect = new Rect(
                previewRect.x + 5,
                previewRect.y + 5,
                previewRect.width - 10,
                previewRect.height - 10
            );

            // 将TextEditor与GUI.TextArea集成
            GUI.SetNextControlName("ScriptPreviewTextArea");
            string newText = GUI.TextArea(textRect, scriptTextEditor.text, EditorStyles.textArea);
            
            // 确保文本内容不变（只读模式）
            if (newText != scriptTextEditor.text)
            {
                scriptTextEditor.text = newText;
            }
            
            // 获取并处理TextEditor的事件
            if (GUI.GetNameOfFocusedControl() == "ScriptPreviewTextArea")
            {
                // 当文本区域获得焦点时同步TextEditor
                scriptTextEditor.OnFocus();
                scriptTextEditor.text = newText;
                
                // 处理键盘事件
                HandleTextEditorKeyboard();
            }
            
            // 强制重绘，确保滚动条正常工作
            if (UnityEngine.Event.current.type == EventType.MouseDown || 
                UnityEngine.Event.current.type == EventType.ScrollWheel)
            {
                Repaint();
            }
        }
        
        /// <summary>
        /// 处理TextEditor的键盘事件
        /// </summary>
        private void HandleTextEditorKeyboard()
        {
            UnityEngine.Event evt = UnityEngine.Event.current;
            
            // 处理键盘事件
            if (evt.type == EventType.KeyDown)
            {
                bool used = false;
                
                // 导航键
                if (evt.keyCode == KeyCode.Home)
                {
                    scriptTextEditor.MoveLineStart();
                    used = true;
                }
                else if (evt.keyCode == KeyCode.End)
                {
                    scriptTextEditor.MoveLineEnd();
                    used = true;
                }
                else if (evt.keyCode == KeyCode.PageUp)
                {
                    // 模拟向上翻页（移动约20行）
                    for (int i = 0; i < 20; i++)
                        scriptTextEditor.MoveUp();
                    used = true;
                }
                else if (evt.keyCode == KeyCode.PageDown)
                {
                    // 模拟向下翻页（移动约20行）
                    for (int i = 0; i < 20; i++)
                        scriptTextEditor.MoveDown();
                    used = true;
                }
                
                // 如果处理了事件，消耗掉它并重绘
                if (used)
                {
                    evt.Use();
                    Repaint();
                }
            }
        }
        
        /// <summary>
        /// 加载脚本或文本内容
        /// </summary>
        private string LoadScriptContent(PreviewItem item)
        {
            if (item == null || (item.Type != "脚本" && item.Type != "文本"))
                return "";
                
            // 首先检查已加载的对象
            if (item.IsLoaded && item.LoadedObject is TextAsset textAsset)
            {
                return textAsset.text;
            }
            
            // 如果未加载或者无效，尝试从文件加载
            if (File.Exists(item.AssetInfoPath))
            {
                try
                {
                    // 读取脚本数据
                    byte[] assetData = File.ReadAllBytes(item.AssetInfoPath);
                    string scriptContent = System.Text.Encoding.UTF8.GetString(assetData);
                    
                    // 保存为TextAsset对象
                    TextAsset newTextAsset = new TextAsset(scriptContent);
                    item.LoadedObject = newTextAsset;
                    item.IsLoaded = true;
                    
                    return scriptContent;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"加载脚本内容失败: {item.Name}, 错误: {ex.Message}");
                    return $"// 无法加载脚本内容: {ex.Message}";
                }
            }
            
            return "// 找不到脚本文件";
        }

        /// <summary>
        /// 绘制默认资源预览（用于不能识别的资源类型）
        /// </summary>
        /// <param name="previewRect">预览区域的矩形范围</param>
        void DrawDefaultPreview(Rect previewRect)
        {
            GUI.DrawTexture(
                new Rect(previewRect.x + previewRect.width / 2 - 32, 
                         previewRect.y + previewRect.height / 2 - 32, 
                         64, 64),
                selectedItem.Preview, ScaleMode.ScaleToFit);
            
            GUI.Label(
                new Rect(previewRect.x, previewRect.y + previewRect.height / 2 + 40, 
                         previewRect.width, 20),
                "无法预览此类型资源", EditorStyles.centeredGreyMiniLabel);
        }

        /// <summary>
        /// 选择预览项目并准备显示
        /// </summary>
        /// <param name="item">要选择的预览项目</param>
        void SelectItem(PreviewItem item)
        {
            // 如果选择了新的项目，清除之前的预览编辑器和加载的对象
            if (selectedItem != item)
            {
                UnloadCurrentPreview();
                
                // 标记脚本预览需要刷新
                scriptPreviewNeedsRefresh = true;
                scriptTextEditor = null;
            }
            
            selectedItem = item;
            previewZoom = 1.0f;
            previewRotation = Quaternion.identity;
            previewScrollPosition = Vector2.zero;
            Repaint();
        }

        /// <summary>
        /// 卸载当前预览的资源，释放内存
        /// </summary>
        void UnloadCurrentPreview()
        {
            if (previewEditor != null)
            {
                UnityEngine.Object.DestroyImmediate(previewEditor);
                previewEditor = null;
            }
            
            // 卸载所有已加载的资源
            foreach (var item in previewItems)
            {
                if (item.IsLoaded && item.LoadedObject != null)
                {
                    // 对于不同类型的资源，需要不同的卸载方式
                    if (item.Type == "模型" && item.LoadedObject is GameObject gameObj)
                    {
                        UnityEngine.Object.DestroyImmediate(gameObj);
                    }
                    else if (item.Type == "材质" && item.LoadedObject is Material mat)
                    {
                        UnityEngine.Object.DestroyImmediate(mat);
                    }
                    else if (item.Type == "贴图" && item.LoadedObject is Texture2D tex)
                    {
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                    
                    item.LoadedObject = null;
                    item.IsLoaded = false;
                }
            }
        }

        /// <summary>
        /// 加载资源用于预览
        /// </summary>
        /// <param name="item">需要加载的预览项</param>
        void LoadAssetForPreview(PreviewItem item)
        {
            if (item.IsLoaded || !File.Exists(item.AssetInfoPath))
                return;
            
            try
            {
                byte[] assetData = File.ReadAllBytes(item.AssetInfoPath);
                
                switch (item.Type)
                {
                    case "模型":
                        LoadModelForPreview(item, assetData);
                        break;
                    case "贴图":
                        LoadTextureForPreview(item, assetData);
                        break;
                    case "材质":
                        LoadMaterialForPreview(item, assetData);
                        break;
                                    case "脚本":
                case "文本":
                    // 脚本或文本创建为TextAsset
                    string scriptContent = System.Text.Encoding.UTF8.GetString(assetData);
                    TextAsset textAsset = new TextAsset(scriptContent);
                    item.LoadedObject = textAsset;
                    item.IsLoaded = true;
                    break;
                    default:
                        // 其他类型不做特殊处理
                        item.IsLoaded = true;
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"加载资源失败: {item.Name}, 错误: {e.Message}");
            }
        }

        /// <summary>
        /// 加载模型资源用于预览
        /// </summary>
        /// <param name="item">模型预览项</param>
        /// <param name="assetData">模型二进制数据</param>
        void LoadModelForPreview(PreviewItem item, byte[] assetData)
        {
            try
            {
                // 确保临时目录存在
                if (!Directory.Exists(tempAssetDir))
                {
                    Directory.CreateDirectory(tempAssetDir);
                }
                
                // 获取规范化的扩展名
                string extension = item.Extension;
                if (extension.Contains("_"))
                {
                    // 尝试提取真实扩展名
                    foreach (string ext in new[] { ".fbx", ".obj", ".3ds" })
                    {
                        if (extension.StartsWith(ext))
                        {
                            extension = ext;
                            break;
                        }
                    }
                }
                
                // 创建唯一的临时文件名，使用规范化的扩展名
                string safeGuid = SanitizePath(Guid.NewGuid().ToString(), true);
                string tempFileName = "temp_" + safeGuid + extension;
                string tempAssetPath = Path.Combine(tempAssetDir, tempFileName);
                
                // 写入模型数据
                File.WriteAllBytes(tempAssetPath, assetData);
                
                // 刷新资源数据库以便Unity识别新文件
                AssetDatabase.Refresh();
                
                // 导入模型
                AssetImporter importer = AssetImporter.GetAtPath(tempAssetPath);
                if (importer is ModelImporter modelImporter)
                {
                    // 设置导入选项，使其快速导入
                    modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                    modelImporter.importAnimation = false;
                    
                    // 重新导入以应用设置
                    AssetDatabase.ImportAsset(tempAssetPath, ImportAssetOptions.ForceUpdate);
                }
                
                // 加载模型资源 - 直接使用资源对象进行预览，不实例化到场景中
                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tempAssetPath);
                if (modelPrefab != null)
                {
                    // 直接使用预制体作为预览对象，不在场景中实例化
                    item.LoadedObject = modelPrefab;
                    
                    // 记录临时资源路径以便后续清理
                    tempImportedAssets.Add(tempAssetPath);
                    
                    // 更新网格信息
                    UpdateMeshInfo(item);
                }
                else
                {
                    Debug.LogWarning($"无法加载模型: {item.Name}");
                }
                
                item.IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载模型失败: {item.Name}, 错误: {ex.Message}");
                item.IsLoaded = true;
            }
        }
        
        /// <summary>
        /// 加载贴图资源用于预览
        /// </summary>
        /// <param name="item">贴图预览项</param>
        /// <param name="assetData">贴图二进制数据</param>
        void LoadTextureForPreview(PreviewItem item, byte[] assetData)
        {
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(assetData))
            {
                item.LoadedObject = texture;
                item.IsLoaded = true;
            }
        }

        /// <summary>
        /// 加载材质资源用于预览
        /// </summary>
        /// <param name="item">材质预览项</param>
        /// <param name="assetData">材质二进制数据</param>
        void LoadMaterialForPreview(PreviewItem item, byte[] assetData)
        {
            try
            {
                // 确保临时目录存在
                if (!Directory.Exists(tempAssetDir))
                {
                    Directory.CreateDirectory(tempAssetDir);
                }
                
                // 获取规范化的扩展名
                string extension = item.Extension;
                if (extension.Contains("_"))
                {
                    // 对于材质文件，确保使用.mat扩展名
                    if (extension.StartsWith(".mat"))
                    {
                        extension = ".mat";
                    }
                }
                
                // 创建唯一的临时文件名，使用规范化的扩展名
                string safeGuid = SanitizePath(Guid.NewGuid().ToString(), true);
                string tempFileName = "temp_" + safeGuid + extension;
                string tempAssetPath = Path.Combine(tempAssetDir, tempFileName);
                
                // 写入材质数据
                File.WriteAllBytes(tempAssetPath, assetData);
                
                // 刷新资源数据库以便Unity识别新文件
                AssetDatabase.Refresh();
                
                // 加载材质资源
                Material material = AssetDatabase.LoadAssetAtPath<Material>(tempAssetPath);
                if (material != null)
                {
                    // 设置为预览对象
                    item.LoadedObject = material;
                    
                    // 记录临时资源路径以便后续清理
                    tempImportedAssets.Add(tempAssetPath);
                }
                else
                {
                    // 如果无法加载，创建一个基本材质
                    Material defaultMaterial = new Material(Shader.Find("Standard"));
                    defaultMaterial.name = "Preview_" + SanitizePath(item.Name, true);
                    defaultMaterial.color = new Color(
                        UnityEngine.Random.value,
                        UnityEngine.Random.value,
                        UnityEngine.Random.value
                    );
                    
                    item.LoadedObject = defaultMaterial;
                }
                
                item.IsLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载材质失败: {item.Name}, 错误: {ex.Message}");
                item.IsLoaded = true;
            }
        }

        /// <summary>
        /// 更新模型的网格信息
        /// </summary>
        /// <param name="item">模型预览项</param>
        private void UpdateMeshInfo(PreviewItem item)
        {
            if (item.LoadedObject is GameObject gameObject)
            {
                // 获取所有MeshFilter组件，不需要使用GetComponentsInChildren，因为现在是直接使用预制体
                MeshFilter[] meshFilters;
                
                // 检查是否是预制体
                if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
                {
                    // 对于预制体，我们需要查找其内部的所有MeshFilter
                    meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
                }
                else
                {
                    // 如果不是预制体，可能是普通对象
                    meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
                }
                
                // 存储网格数量信息
                item.MeshCount = meshFilters.Length;
                
                // 计算总顶点数和三角形数
                item.VertexCount = 0;
                item.TriangleCount = 0;
                
                foreach (MeshFilter filter in meshFilters)
                {
                    if (filter.sharedMesh != null)
                    {
                        item.VertexCount += filter.sharedMesh.vertexCount;
                        item.TriangleCount += filter.sharedMesh.triangles.Length / 3;
                    }
                }
            }
        }

        /// <summary>
        /// 根据标签页过滤资源项
        /// </summary>
        /// <param name="item">要检查的预览项</param>
        /// <returns>如果项目应在当前标签页显示，则返回true</returns>
        private bool FilterByTab(PreviewItem item)
        {
            switch (selectedTab)
            {
                case 0: // 全部
                    return true;
                case 1: // 模型
                    return item.Type == "模型";
                case 2: // 贴图
                    return item.Type == "贴图";
                case 3: // 材质
                    return item.Type == "材质";
                case 4: // 脚本
                    return item.Type == "脚本";
                case 5: // 文本
                    return item.Type == "文本";
                case 6: // 其他
                    return item.Type == "其他";
                default:
                    return true;
            }
        }

        /// <summary>
        /// 加载Unity资源包并解析其内容
        /// </summary>
        /// <param name="packagePath">资源包的完整路径</param>
        private void LoadPackage(string packagePath)
        {
            // 清理之前的资源
            UnloadCurrentPreview();
            previewItems.Clear();
            selectedItem = null;
            
            // 清除当前图标
            packageIcon = null;
            packageName = Path.GetFileNameWithoutExtension(packagePath);
            
            try
            {
                // 创建临时目录，清理文件名中的非法字符
                string safePackageName = SanitizePath(Path.GetFileNameWithoutExtension(packagePath), true);
                tempExtractPath = Path.Combine(Path.GetTempPath(), "UnityPackagePreview_" + safePackageName + "_" + Guid.NewGuid().ToString());
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);
                
                Directory.CreateDirectory(tempExtractPath);
                
                // 解压.unitypackage文件（实际上是一个tar.gz文件）
                try
                {
                    // 处理路径中可能的特殊字符
                    string extractCommand = $"-xzf \"{packagePath}\" -C \"{tempExtractPath}\"";
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "tar";
                    process.StartInfo.Arguments = extractCommand;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true; // 捕获错误输出
                    process.StartInfo.CreateNoWindow = true;
                    
                    process.Start();
                    
                    // 读取输出，避免死锁
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit();
                    
                    // 检查是否有错误
                    if (process.ExitCode != 0)
                    {
                        Debug.LogWarning($"解压过程中出现警告：{error}");
                    }
                    
                    // 检查是否成功提取了任何文件
                    string[] extractedFolders = Directory.GetDirectories(tempExtractPath);
                    if (extractedFolders.Length == 0)
                    {
                        throw new Exception("解压资源包失败，未能提取任何有效文件。可能不是有效的Unity资源包格式。");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"解压资源包失败: {ex.Message}");
                }
                
                // 查找并加载包图标
                LoadPackageIcon();
                
                // 遍历解压后的文件夹，但不加载资源内容
                foreach (string folder in Directory.GetDirectories(tempExtractPath))
                {
                    string assetInfoPath = Path.Combine(folder, "asset");
                    string pathnamePath = Path.Combine(folder, "pathname");
                    string previewPath = Path.Combine(folder, "preview.png");
                    
                    if (File.Exists(pathnamePath) && File.Exists(assetInfoPath))
                    {
                        try
                        {
                            // 读取原始路径
                            string originalPath = File.ReadAllText(pathnamePath).Trim();
                            
                            // 先规范化路径（处理旧版本Unity资源包的特殊格式）
                            string normalizedPath = NormalizeUnityAssetPath(originalPath);
                            
                            // 然后清理路径中的非法字符，保留路径结构
                            string assetPath = SanitizePath(normalizedPath, false);
                            
                            PreviewItem item = new PreviewItem
                            {
                                Path = assetPath,
                                Name = Path.GetFileName(assetPath),
                                Extension = Path.GetExtension(assetPath).ToLower(),
                                AssetInfoPath = assetInfoPath,
                                IsLoaded = false
                            };
                            
                            // 设置资源类型
                            SetAssetType(item);
                            
                            // 加载预览图
                            if (File.Exists(previewPath))
                            {
                                try
                                {
                                    Texture2D previewTexture = new Texture2D(2, 2);
                                    previewTexture.LoadImage(File.ReadAllBytes(previewPath));
                                    item.Preview = previewTexture;
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"加载预览图失败: {item.Name}, {ex.Message}");
                                    item.Preview = GetDefaultPreviewForType(item.Type);
                                }
                            }
                            else
                            {
                                item.Preview = GetDefaultPreviewForType(item.Type);
                            }
                            
                            previewItems.Add(item);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"处理资源项失败: {Path.GetFileName(folder)}, {ex.Message}");
                            // 继续处理下一个资源
                            continue;
                        }
                    }
                }
                
                // 更新分页数据
                currentPage = 0;
                UpdatePagination();
                
                Debug.Log($"成功加载资源包，共 {previewItems.Count} 个资源");
            }
            catch (Exception e)
            {
                Debug.LogError($"加载资源包失败: {e.Message}");
                Debug.LogError($"错误详情: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// 查找并加载包图标
        /// </summary>
        private void LoadPackageIcon()
        {
            try
            {
                // 寻找.icon.png文件(位于最外层)
                string iconPath = null;
                
                // 查找直接位于根目录的.icon.png文件
                foreach (var file in Directory.GetFiles(tempExtractPath, "*.png"))
                {
                    if (Path.GetFileName(file).EndsWith(".icon.png"))
                    {
                        iconPath = file;
                        break;
                    }
                }
                
                // 如果在根目录没找到，查找是否有asset文件对应的pathname是.icon.png
                if (iconPath == null)
                {
                    foreach (string folder in Directory.GetDirectories(tempExtractPath))
                    {
                        string pathnamePath = Path.Combine(folder, "pathname");
                        string assetPath = Path.Combine(folder, "asset");
                        
                        if (File.Exists(pathnamePath) && File.Exists(assetPath))
                        {
                            string path = File.ReadAllText(pathnamePath).Trim();
                            if (path.EndsWith(".icon.png") || path == ".icon.png")
                            {
                                iconPath = assetPath;
                                break;
                            }
                        }
                    }
                }
                
                // 加载图标
                if (iconPath != null && File.Exists(iconPath))
                {
                    packageIcon = new Texture2D(2, 2);
                    packageIcon.LoadImage(File.ReadAllBytes(iconPath));
                    Debug.Log($"已加载资源包图标: {iconPath}");
                }
                else
                {
                    Debug.Log("此资源包没有图标");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"加载包图标失败: {ex.Message}");
                packageIcon = null;
            }
        }

        /// <summary>
        /// 清理路径中的非法字符
        /// </summary>
        /// <param name="path">原始路径</param>
        /// <returns>清理后的安全路径</returns>
        private string SanitizePath(string path)
        {
            return SanitizePath(path, false);
        }
        
        /// <summary>
        /// 清理路径中的非法字符，可选择是否保留路径分隔符
        /// </summary>
        /// <param name="path">原始路径</param>
        /// <param name="isFileName">是否为文件名（true则替换所有非法字符，false则保留路径分隔符）</param>
        /// <returns>清理后的安全路径</returns>
        private string SanitizePath(string path, bool isFileName)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // 替换Windows文件系统中的非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string result = path;
            
            // 如果是文件名，替换所有非法字符
            if (isFileName)
            {
                foreach (char c in invalidChars)
                {
                    result = result.Replace(c, '_');
                }
                
                // 特别处理一些常见的问题字符
                result = result.Replace(':', '_')
                             .Replace('/', '_')
                             .Replace('\\', '_')
                             .Replace('*', '_')
                             .Replace('?', '_')
                             .Replace('"', '_')
                             .Replace('<', '_')
                             .Replace('>', '_')
                             .Replace('|', '_');
            }
            // 如果是路径，保留路径分隔符
            else
            {
                foreach (char c in invalidChars)
                {
                    // 保留路径分隔符
                    if (c != '/' && c != '\\')
                    {
                        result = result.Replace(c, '_');
                    }
                }
                
                // 特别处理一些常见的问题字符，但保留路径分隔符
                result = result.Replace(':', '_')
                             .Replace('*', '_')
                             .Replace('?', '_')
                             .Replace('"', '_')
                             .Replace('<', '_')
                             .Replace('>', '_')
                             .Replace('|', '_');
                             
                // 统一路径分隔符为'/'（Unity内部使用）
                result = result.Replace('\\', '/');
            }
                         
            return result;
        }

        /// <summary>
        /// 规范化Unity资源路径，处理老版本Unity可能添加的版本后缀
        /// </summary>
        /// <param name="path">从pathname文件读取的原始路径</param>
        /// <returns>规范化后的路径</returns>
        private string NormalizeUnityAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
                
            // 定义已知的Unity资源扩展名（根据需要扩展此列表）
            string[] knownExtensions = new string[]
            {
                ".unity", ".prefab", ".fbx", ".obj", ".mat", ".asset",
                ".cs", ".js", ".shader", ".cginc", ".compute",
                ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".exr", ".hdr",
                ".mp3", ".wav", ".ogg", ".aiff",
                ".mp4", ".mov", ".avi", ".mkv",
                ".ttf", ".otf", ".controller", ".anim", ".mask"
            };
            
            // 检查路径是否包含版本后缀（如 _00）
            foreach (string ext in knownExtensions)
            {
                // 检查常见的后缀模式，如 .fbx_00, .png_12, 等
                string pattern = ext + "_[0-9][0-9]";
                if (System.Text.RegularExpressions.Regex.IsMatch(path, pattern))
                {
                    // 替换为标准扩展名
                    string fixedPath = System.Text.RegularExpressions.Regex.Replace(
                        path, 
                        pattern, 
                        ext);
                    
                    Debug.Log($"修正资源路径: {path} → {fixedPath}");
                    return fixedPath;
                }
            }
            
            return path;
        }

        /// <summary>
        /// 根据文件扩展名设置资源类型
        /// </summary>
        /// <param name="item">要设置类型的预览项</param>
        private void SetAssetType(PreviewItem item)
        {
            // 获取标准化的扩展名
            string extension = item.Extension;
            
            // 处理可能带有版本号的扩展名（如 .fbx_00）
            if (extension.Contains("_"))
            {
                // 尝试提取真实扩展名
                foreach (string ext in new[] { ".fbx", ".obj", ".3ds", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".mat", ".cs", ".js", ".txt", ".json", ".xml" })
                {
                    if (extension.StartsWith(ext))
                    {
                        extension = ext;
                        break;
                    }
                }
            }
            
            // 根据扩展名分类
            switch (extension)
            {
                case ".fbx":
                case ".obj":
                case ".3ds":
                    item.Type = "模型";
                    break;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                    item.Type = "贴图";
                    break;
                case ".mat":
                    item.Type = "材质";
                    break;
                case ".cs":
                case ".js":
                    item.Type = "脚本";
                    break;
                case ".txt":
                case ".json":
                case ".xml":
                case ".html":
                case ".htm":
                case ".css":
                case ".md":
                case ".log":
                case ".config":
                case ".yaml":
                case ".yml":
                    item.Type = "文本";
                    break;
                default:
                    item.Type = "其他";
                    break;
            }
        }

        /// <summary>
        /// 获取指定资源类型的默认预览图标
        /// </summary>
        /// <param name="type">资源类型</param>
        /// <returns>对应类型的默认图标</returns>
        private Texture2D GetDefaultPreviewForType(string type)
        {
            switch (type)
            {
                case "模型":
                    return EditorGUIUtility.FindTexture("PrefabModel Icon");
                case "贴图":
                    return EditorGUIUtility.FindTexture("Texture Icon");
                case "材质":
                    return EditorGUIUtility.FindTexture("Material Icon");
                case "脚本":
                    return EditorGUIUtility.FindTexture("cs Script Icon");
                case "文本":
                    // 可以根据不同的文本类型返回不同的图标
                    if (selectedItem != null)
                    {
                        string ext = Path.GetExtension(selectedItem.Name).ToLowerInvariant();
                        switch (ext)
                        {
                            case ".json":
                                return EditorGUIUtility.FindTexture("TextAsset Icon");
                            case ".xml":
                                return EditorGUIUtility.FindTexture("TextAsset Icon");
                            case ".txt":
                            default:
                                return EditorGUIUtility.FindTexture("TextScriptImporter Icon");
                        }
                    }
                    return EditorGUIUtility.FindTexture("TextScriptImporter Icon");
                default:
                    return EditorGUIUtility.FindTexture("DefaultAsset Icon");
            }
        }
        
        /// <summary>
        /// 清理所有临时导入的资源文件
        /// </summary>
        void CleanupTempAssets()
        {
            // 清理已导入的临时资源
            foreach (string assetPath in tempImportedAssets)
            {
                if (File.Exists(assetPath) || Directory.Exists(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
            
            // 清空列表
            tempImportedAssets.Clear();
            
            // 如果临时目录存在，尝试删除
            if (Directory.Exists(tempAssetDir))
            {
                try
                {
                    // 删除目录中的所有文件
                    string[] files = Directory.GetFiles(tempAssetDir);
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }
                    
                    // 删除目录
                    AssetDatabase.DeleteAsset(tempAssetDir);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"清理临时目录失败: {ex.Message}");
                }
            }
            
            // 刷新资源数据库
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 清理所有临时文件
        /// </summary>
        private void CleanupAllTempFiles()
        {
            // 清理临时资源
            CleanupTempAssets();
            
            // 清理临时目录
            string tempPath = Path.Combine(Path.GetTempPath(), "UnityPackagePreview_*");
            string[] tempDirs = Directory.GetDirectories(Path.GetTempPath(), "UnityPackagePreview_*");
            foreach (string dir in tempDirs)
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"清理临时目录失败: {dir}, 错误: {e.Message}");
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log("临时资源清理完成");
            
            // 重新加载当前包（如果有）
            if (!string.IsNullOrEmpty(packagePath) && File.Exists(packagePath))
            {
                LoadPackage(packagePath);
            }
            else
            {
                // 清空预览项
                previewItems.Clear();
                selectedItem = null;
                
                // 清除包图标
                if (packageIcon != null)
                {
                    UnityEngine.Object.DestroyImmediate(packageIcon);
                    packageIcon = null;
                }
                packageName = "";
                
                // 更新分页
                currentPage = 0;
                UpdatePagination();
            }
        }

        /// <summary>
        /// 卸载当前加载的资源包并清理临时文件
        /// </summary>
        private void UnloadCurrentPackage()
        {
            if (string.IsNullOrEmpty(packagePath))
                return;
                
            // 先卸载当前预览
            UnloadCurrentPreview();
            
            // 清理临时资源
            CleanupTempAssets();
            
            // 清空预览项列表
            previewItems.Clear();
            selectedItem = null;
            
            // 清除包图标
            if (packageIcon != null)
            {
                UnityEngine.Object.DestroyImmediate(packageIcon);
                packageIcon = null;
            }
            packageName = "";
            
            // 记录路径以便显示卸载信息
            string unloadedPath = packagePath;
            
            // 清空包路径
            packagePath = "";
            
            // 显示卸载信息
            Debug.Log($"已卸载资源包: {unloadedPath}");
            
            // 更新分页
            currentPage = 0;
            UpdatePagination();
            
            // 刷新界面
            Repaint();
        }

        /// <summary>
        /// 窗口销毁时执行清理操作
        /// </summary>
        void OnDestroy()
        {
            // 清理预览编辑器
            UnloadCurrentPreview();
            
            // 清理临时资源
            CleanupTempAssets();
            
            // 清除包图标
            if (packageIcon != null)
            {
                UnityEngine.Object.DestroyImmediate(packageIcon);
                packageIcon = null;
            }
            
            // 清理临时目录
            if (!string.IsNullOrEmpty(tempExtractPath) && Directory.Exists(tempExtractPath))
            {
                try
                {
                    Directory.Delete(tempExtractPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"清理临时目录失败: {e.Message}");
                }
            }
        }
    }
}