using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// CWTK编辑器工具导出窗口
/// </summary>
public class CW_E_ExportCWTK : EditorWindow
{
    // 版本信息
    private string currentVersion = "1.0.0";
    private string newVersion = "1.0.0";
    private string updateContent = "";
    
    // 更新记录文件路径
    private string changelogPath = "Assets/ChoseWay/CWTK_Changelog.txt";
    
    // 导出路径
    private string exportPath = "";
    
    // 要导出的文件夹路径
    private List<string> foldersToExport = new List<string>();
    private List<bool> foldersToExportToggles = new List<bool>();
    
    // 默认要导出的文件夹
    private readonly string[] defaultFolders = new string[] {
        "Assets/ChoseWay",
        "Assets/Plugins",
        "Assets/QFramework",
        "Assets/Resources",
    };
    
    // GitHub相关设置
    private string githubRepoUrl = "";
    private string localRepoPath = "";
    private string commitMessage = "";
    private bool useGitLFS = false;
    
    // 上传进度相关
    private bool isUploading = false;
    private string currentProgressStep = "";
    private float uploadProgress = 0f;
    private string progressDetails = "";
    
    // 滚动视图位置
    private Vector2 scrollPosition;
    private Vector2 githubScrollPosition;

    [MenuItem("CWTK/导出工具包")]
    public static void ShowWindow()
    {
        GetWindow<CW_E_ExportCWTK>("CWTK导出工具");
    }

    private void OnEnable()
    {
        // 初始化导出文件夹
        InitExportFolders();
        
        // 加载当前版本号
        LoadCurrentVersion();
        
        // 设置默认导出路径
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = Path.GetDirectoryName(Application.dataPath);
        }
        
        // 加载GitHub设置
        LoadGitHubSettings();
        
        // 确保在编辑器更新时刷新进度UI
        EditorApplication.update += Repaint;
    }
    
    private void OnDisable()
    {
        // 移除更新回调
        EditorApplication.update -= Repaint;
    }

    private void InitExportFolders()
    {
        foldersToExport.Clear();
        foldersToExportToggles.Clear();
        
        foreach (var folder in defaultFolders)
        {
            if (Directory.Exists(folder))
            {
                foldersToExport.Add(folder);
                foldersToExportToggles.Add(true);
            }
        }
    }

    private void LoadCurrentVersion()
    {
        if (File.Exists(changelogPath))
        {
            string content = File.ReadAllText(changelogPath);
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                string firstLine = lines[0];
                if (firstLine.StartsWith("v"))
                {
                    currentVersion = firstLine.Substring(1).Trim();
                    newVersion = currentVersion;
                }
            }
        }
        else
        {
            // 如果更新日志不存在，创建一个初始文件
            Directory.CreateDirectory(Path.GetDirectoryName(changelogPath));
            File.WriteAllText(changelogPath, $"v{currentVersion} ({DateTime.Now.ToString("yyyy-MM-dd")})\n初始版本\n");
        }
    }
    
    private void LoadGitHubSettings()
    {
        githubRepoUrl = EditorPrefs.GetString("CWTK_GitHubRepoUrl", "");
        localRepoPath = EditorPrefs.GetString("CWTK_LocalRepoPath", "");
        useGitLFS = EditorPrefs.GetBool("CWTK_UseGitLFS", false);
    }
    
    private void SaveGitHubSettings()
    {
        EditorPrefs.SetString("CWTK_GitHubRepoUrl", githubRepoUrl);
        EditorPrefs.SetString("CWTK_LocalRepoPath", localRepoPath);
        EditorPrefs.SetBool("CWTK_UseGitLFS", useGitLFS);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("CWTK导出工具", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(10);
        
        // 显示上传进度条（如果正在上传）
        if (isUploading)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"正在上传: {currentProgressStep}", EditorStyles.boldLabel);
            
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), uploadProgress, 
                $"{Mathf.RoundToInt(uploadProgress * 100)}%");
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(progressDetails, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            
            // 如果上传过程中，禁用其他控件
            EditorGUI.BeginDisabledGroup(true);
        }
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("版本信息", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("当前版本:", GUILayout.Width(70));
        EditorGUILayout.LabelField(currentVersion);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("新版本:", GUILayout.Width(70));
        newVersion = EditorGUILayout.TextField(newVersion);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("更新内容:");
        updateContent = EditorGUILayout.TextArea(updateContent, GUILayout.Height(60));
        
        if (GUILayout.Button("更新版本信息"))
        {
            UpdateVersionInfo();
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("要导出的文件夹", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
        for (int i = 0; i < foldersToExport.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            foldersToExportToggles[i] = EditorGUILayout.Toggle(foldersToExportToggles[i], GUILayout.Width(20));
            EditorGUILayout.LabelField(foldersToExport[i]);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加文件夹", GUILayout.Width(100)))
        {
            string folder = EditorUtility.OpenFolderPanel("选择要导出的文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(folder))
            {
                // 转换为相对路径
                if (folder.StartsWith(Application.dataPath))
                {
                    folder = "Assets" + folder.Substring(Application.dataPath.Length);
                }
                if (!foldersToExport.Contains(folder))
                {
                    foldersToExport.Add(folder);
                    foldersToExportToggles.Add(true);
                }
            }
        }
        
        if (GUILayout.Button("移除选中", GUILayout.Width(100)))
        {
            for (int i = foldersToExport.Count - 1; i >= 0; i--)
            {
                if (foldersToExportToggles[i])
                {
                    foldersToExport.RemoveAt(i);
                    foldersToExportToggles.RemoveAt(i);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("导出设置", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("导出路径:", GUILayout.Width(70));
        EditorGUILayout.LabelField(exportPath, EditorStyles.textField);
        if (GUILayout.Button("浏览...", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("选择导出路径", exportPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                exportPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("导出UnityPackage", GUILayout.Height(30)))
        {
            ExportPackage();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // GitHub上传设置
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("GitHub上传设置", EditorStyles.boldLabel);
        
        githubScrollPosition = EditorGUILayout.BeginScrollView(githubScrollPosition, GUILayout.Height(150));
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("GitHub仓库URL:", GUILayout.Width(120));
        githubRepoUrl = EditorGUILayout.TextField(githubRepoUrl);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("本地仓库路径:", GUILayout.Width(120));
        EditorGUILayout.LabelField(localRepoPath, EditorStyles.textField);
        if (GUILayout.Button("浏览...", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("选择本地Git仓库路径", localRepoPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                localRepoPath = path;
                SaveGitHubSettings();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("使用Git LFS:", GUILayout.Width(120));
        bool newUseGitLFS = EditorGUILayout.Toggle(useGitLFS);
        if (newUseGitLFS != useGitLFS)
        {
            useGitLFS = newUseGitLFS;
            SaveGitHubSettings();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("提交信息:", GUILayout.Width(120));
        commitMessage = EditorGUILayout.TextField(commitMessage);
        EditorGUILayout.EndHorizontal();
        
        if (string.IsNullOrEmpty(commitMessage))
        {
            commitMessage = $"更新CWTK工具包到v{currentVersion}";
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("初始化/克隆仓库"))
        {
            InitializeRepository();
        }
        
        if (GUILayout.Button("检查仓库状态"))
        {
            CheckRepositoryStatus();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("上传到GitHub", GUILayout.Height(30)))
        {
            UploadToGitHub();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndVertical();
        
        if (isUploading)
        {
            EditorGUI.EndDisabledGroup();
        }
    }

    private void UpdateVersionInfo()
    {
        // 检查版本号格式
        if (string.IsNullOrEmpty(newVersion) || !IsValidVersion(newVersion))
        {
            EditorUtility.DisplayDialog("错误", "请输入有效的版本号 (例如: 1.0.0)", "确定");
            return;
        }
        
        // 更新版本记录文件
        try
        {
            StringBuilder sb = new StringBuilder();
            string header = $"v{newVersion} ({DateTime.Now.ToString("yyyy-MM-dd")})";
            sb.AppendLine(header);
            sb.AppendLine(updateContent);
            sb.AppendLine();
            
            // 如果已有更新记录，则追加
            if (File.Exists(changelogPath))
            {
                string existingContent = File.ReadAllText(changelogPath);
                sb.Append(existingContent);
            }
            
            File.WriteAllText(changelogPath, sb.ToString());
            
            currentVersion = newVersion;
            updateContent = "";
            
            EditorUtility.DisplayDialog("成功", "版本信息已更新", "确定");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("错误", $"无法更新版本信息: {ex.Message}", "确定");
        }
    }

    private bool IsValidVersion(string version)
    {
        try
        {
            // 检查版本号格式 (例如 1.0.0)
            string[] parts = version.Split('.');
            if (parts.Length < 2)
                return false;
                
            foreach (string part in parts)
            {
                if (!int.TryParse(part, out _))
                    return false;
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ExportPackage()
    {
        List<string> selectedPaths = new List<string>();
        
        // 检查是否至少选择了一个文件夹
        bool hasSelected = false;
        for (int i = 0; i < foldersToExport.Count; i++)
        {
            if (foldersToExportToggles[i])
            {
                hasSelected = true;
                selectedPaths.Add(foldersToExport[i]);
            }
        }
        
        if (!hasSelected)
        {
            EditorUtility.DisplayDialog("错误", "请至少选择一个要导出的文件夹", "确定");
            return;
        }
        
        // 添加更新记录文件
        if (File.Exists(changelogPath) && !selectedPaths.Contains(Path.GetDirectoryName(changelogPath)))
        {
            selectedPaths.Add(changelogPath);
        }
        
        string fileName = $"CWTK_{currentVersion}.unitypackage";
        string filePath = Path.Combine(exportPath, fileName);
        
        try
        {
            AssetDatabase.ExportPackage(
                selectedPaths.ToArray(),
                filePath,
                ExportPackageOptions.Recurse | ExportPackageOptions.Interactive
            );
            
            if (File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("成功", $"导出成功: {filePath}", "确定");
                EditorUtility.RevealInFinder(filePath);
            }
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("错误", $"导出失败: {ex.Message}", "确定");
        }
    }
    
    private bool IsGitInstalled()
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = "--version";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    private void InitializeRepository()
    {
        if (string.IsNullOrEmpty(githubRepoUrl))
        {
            EditorUtility.DisplayDialog("错误", "请输入GitHub仓库URL", "确定");
            return;
        }
        
        if (string.IsNullOrEmpty(localRepoPath))
        {
            EditorUtility.DisplayDialog("错误", "请选择本地仓库路径", "确定");
            return;
        }
        
        if (!IsGitInstalled())
        {
            EditorUtility.DisplayDialog("错误", "未检测到Git，请先安装Git", "确定");
            return;
        }
        
        UpdateProgress("初始化仓库", 0f, "正在检查仓库状态...");
        
        bool isExistingRepo = Directory.Exists(Path.Combine(localRepoPath, ".git"));
        
        if (isExistingRepo)
        {
            // 已有仓库，拉取最新代码
            if (EditorUtility.DisplayDialog("确认", "检测到现有Git仓库，是否拉取最新代码？", "是", "否"))
            {
                UpdateProgress("更新仓库", 0.1f, "正在拉取最新代码...");
                ExecuteGitCommand("pull", "正在拉取最新代码...");
                ResetProgress();
            }
        }
        else
        {
            // 初始化新仓库
            if (Directory.Exists(localRepoPath) && Directory.GetFileSystemEntries(localRepoPath).Length > 0)
            {
                if (!EditorUtility.DisplayDialog("警告", "所选目录不为空，继续操作可能会覆盖现有文件。是否继续？", "继续", "取消"))
                {
                    return;
                }
            }
            
            try
            {
                UpdateProgress("初始化仓库", 0.1f, "正在创建目录...");
                Directory.CreateDirectory(localRepoPath);
                
                // 配置Git行结束符设置
                ConfigureGitLineEndings();
                
                UpdateProgress("初始化仓库", 0.2f, "正在克隆仓库...");
                
                // 克隆仓库
                Process process = new Process();
                process.StartInfo.FileName = "git";
                process.StartInfo.Arguments = $"clone {githubRepoUrl} \"{localRepoPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(localRepoPath);
                
                StringBuilder output = new StringBuilder();
                process.OutputDataReceived += (sender, args) => { 
                    if (args.Data != null) {
                        output.AppendLine(args.Data);
                        progressDetails = args.Data;
                        Repaint();
                    }
                };
                process.ErrorDataReceived += (sender, args) => { 
                    if (args.Data != null) {
                        output.AppendLine(args.Data);
                        progressDetails = args.Data;
                        Repaint();
                    }
                };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                
                if (process.ExitCode == 0)
                {
                    UpdateProgress("初始化仓库", 0.9f, "仓库克隆完成");
                    
                    // 如果需要使用LFS，初始化LFS
                    if (useGitLFS)
                    {
                        UpdateProgress("初始化仓库", 0.95f, "正在初始化Git LFS...");
                        ExecuteGitCommand("lfs install", "正在初始化Git LFS...");
                    }
                    
                    EditorUtility.DisplayDialog("成功", "仓库克隆成功", "确定");
                    ResetProgress();
                }
                else
                {
                    ResetProgress();
                    EditorUtility.DisplayDialog("错误", $"仓库克隆失败: \n{output}", "确定");
                }
            }
            catch (Exception ex)
            {
                ResetProgress();
                EditorUtility.DisplayDialog("错误", $"初始化仓库失败: {ex.Message}", "确定");
            }
        }
    }
    
    // 配置Git行结束符设置，避免LF/CRLF警告
    private void ConfigureGitLineEndings()
    {
        try
        {
            // 设置全局行结束符配置
            ExecuteGitCommand("config --global core.autocrlf false", "配置行结束符...");
            ExecuteGitCommand("config --global core.safecrlf false", "配置行结束符安全检查...");
            
            // 为当前仓库创建.gitattributes文件
            string gitattributesPath = Path.Combine(localRepoPath, ".gitattributes");
            if (!File.Exists(gitattributesPath))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# 设置默认行为，所有文件保持原有的行结束符");
                sb.AppendLine("* text=auto");
                sb.AppendLine();
                sb.AppendLine("# 明确声明应该规范化的文本文件");
                sb.AppendLine("*.cs text");
                sb.AppendLine("*.txt text");
                sb.AppendLine("*.md text");
                sb.AppendLine("*.json text");
                sb.AppendLine("*.xml text");
                sb.AppendLine("*.shader text");
                sb.AppendLine();
                sb.AppendLine("# 二进制文件不应被修改");
                sb.AppendLine("*.png binary");
                sb.AppendLine("*.jpg binary");
                sb.AppendLine("*.jpeg binary");
                sb.AppendLine("*.gif binary");
                sb.AppendLine("*.tif binary");
                sb.AppendLine("*.tiff binary");
                sb.AppendLine("*.ico binary");
                sb.AppendLine("*.unity binary");
                sb.AppendLine("*.asset binary");
                sb.AppendLine("*.prefab binary");
                sb.AppendLine("*.fbx binary");
                sb.AppendLine("*.wav binary");
                sb.AppendLine("*.mp3 binary");
                
                File.WriteAllText(gitattributesPath, sb.ToString());
                UnityEngine.Debug.Log("已创建.gitattributes文件");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"配置Git行结束符失败: {ex.Message}");
        }
    }
    
    private void CheckRepositoryStatus()
    {
        if (string.IsNullOrEmpty(localRepoPath) || !Directory.Exists(localRepoPath))
        {
            EditorUtility.DisplayDialog("错误", "请选择有效的本地仓库路径", "确定");
            return;
        }
        
        ExecuteGitCommand("status", "正在检查仓库状态...");
    }
    
    private async void UploadToGitHubAsync()
    {
        List<string> selectedPaths = GetSelectedPaths();
        
        if (selectedPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "请至少选择一个要上传的文件夹", "确定");
            return;
        }
        
        if (string.IsNullOrEmpty(localRepoPath) || !Directory.Exists(localRepoPath))
        {
            EditorUtility.DisplayDialog("错误", "请选择有效的本地仓库路径", "确定");
            return;
        }
        
        if (!Directory.Exists(Path.Combine(localRepoPath, ".git")))
        {
            if (EditorUtility.DisplayDialog("警告", "所选路径不是Git仓库，是否初始化仓库？", "是", "否"))
            {
                InitializeRepository();
                if (!Directory.Exists(Path.Combine(localRepoPath, ".git")))
                {
                    return; // 如果初始化失败，直接返回
                }
            }
            else
            {
                return;
            }
        }
        
        try
        {
            // 配置Git行结束符设置
            UpdateProgress("准备上传", 0.02f, "正在配置Git行结束符设置...");
            ConfigureGitLineEndings();
            await Task.Delay(300);
            
            // 确保清理仓库内容前先创建package.json文件的备份
            string tempPackageJson = "";
            string packageJsonPath = Path.Combine(localRepoPath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                tempPackageJson = File.ReadAllText(packageJsonPath);
                UnityEngine.Debug.Log("已备份现有package.json文件");
            }
            
            // 清空仓库目录下的内容(除了.git目录)
            UpdateProgress("准备上传", 0.10f, "正在清理仓库目录...");
            ClearRepositoryContent();
            await Task.Delay(300);
            
            // 更新package.json文件
            UpdateProgress("准备上传", 0.05f, "正在创建package.json...");
            CreatePackageJson();
            await Task.Delay(300);
            
            // 复制选中的文件夹到仓库
            UpdateProgress("准备上传", 0.15f, "正在复制文件...");
            await CopySelectedFoldersToRepoAsync(selectedPaths);
            
            // 再次确认package.json存在
            if (!File.Exists(packageJsonPath))
            {
                UnityEngine.Debug.LogWarning("package.json未被创建，尝试重新创建");
                CreatePackageJson();
                await Task.Delay(100);
                
                if (!File.Exists(packageJsonPath))
                {
                    UnityEngine.Debug.LogError("无法创建package.json文件");
                    EditorUtility.DisplayDialog("错误", "无法创建package.json文件，请检查权限或路径是否正确", "确定");
                    ResetProgress();
                    return;
                }
            }
            
            // Git添加
            UpdateProgress("Git操作", 0.70f, "正在添加文件到Git...");
            if (await ExecuteGitCommandAsync("add -A", "正在添加文件..."))
            {
                // 确认添加了package.json
                await ExecuteGitCommandAsync("status", "检查文件状态...");
                
                // Git提交
                UpdateProgress("Git操作", 0.80f, "正在提交更改...");
                bool commitSuccess = await ExecuteGitCommandAsync($"commit -m \"{commitMessage}\" --no-verify", "正在提交更改...");
                
                if (commitSuccess)
                {
                    // Git推送
                    UpdateProgress("Git操作", 0.90f, "正在推送到GitHub...");
                    bool pushSuccess = await ExecuteGitCommandAsync("push --force", "正在推送到GitHub...");
                    
                    UpdateProgress("完成", 1.0f, pushSuccess ? "上传成功！" : "推送失败，但本地提交已完成。");
                    await Task.Delay(2000); // 显示完成信息2秒
                    
                    if (pushSuccess)
                    {
                        // 打开文件夹查看结果
                        EditorUtility.RevealInFinder(localRepoPath);
                        EditorUtility.DisplayDialog("成功", "文件已成功上传到GitHub仓库", "确定");
                    }
                }
                else
                {
                    // 如果提交失败，尝试使用-f强制提交
                    UpdateProgress("Git操作", 0.85f, "尝试强制提交...");
                    if (await ExecuteGitCommandAsync($"commit -m \"{commitMessage}\" --no-verify -f", "正在强制提交..."))
                    {
                        UpdateProgress("Git操作", 0.90f, "正在推送到GitHub...");
                        bool pushSuccess = await ExecuteGitCommandAsync("push --force", "正在推送到GitHub...");
                        
                        UpdateProgress("完成", 1.0f, pushSuccess ? "上传成功！" : "推送失败，但本地提交已完成。");
                        await Task.Delay(2000);
                        
                        if (pushSuccess)
                        {
                            // 打开文件夹查看结果
                            EditorUtility.RevealInFinder(localRepoPath);
                            EditorUtility.DisplayDialog("成功", "文件已成功上传到GitHub仓库", "确定");
                        }
                    }
                }
            }
            
            ResetProgress();
        }
        catch (Exception ex)
        {
            ResetProgress();
            EditorUtility.DisplayDialog("错误", $"上传过程中发生错误: {ex.Message}", "确定");
        }
    }
    
    // 创建package.json文件
    private void CreatePackageJson()
    {
        // package.json应放在仓库根目录
        string packageJsonPath = Path.Combine(localRepoPath, "package.json");
        bool isNewFile = !File.Exists(packageJsonPath);
        
        // 创建或更新package.json
        try
        {
            string packageName = "";
            
            // 从仓库URL中提取包名
            if (!string.IsNullOrEmpty(githubRepoUrl))
            {
                // 提取仓库名称
                string[] parts = githubRepoUrl.TrimEnd('/').Split('/');
                if (parts.Length > 0)
                {
                    packageName = parts[parts.Length - 1].Replace(".git", "");
                }
            }
            
            // 如果无法从URL提取，使用默认包名
            if (string.IsNullOrEmpty(packageName))
            {
                packageName = "com.choseway.cwtk";
            }
            else if (!packageName.StartsWith("com."))
            {
                // 确保包名符合UPM命名规范 (com.组织名.包名)
                packageName = "com.choseway." + packageName.ToLower();
            }
            
            string displayName = "CWTK Unity工具包";
            string description = "专为Unity开发的工具包，提供多种实用功能";
            string author = "ChoseWay";
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"name\": \"{packageName}\",");
            sb.AppendLine($"  \"version\": \"{currentVersion}\",");
            sb.AppendLine($"  \"displayName\": \"{displayName}\",");
            sb.AppendLine($"  \"description\": \"{description}\",");
            sb.AppendLine($"  \"unity\": \"2020.3\",");
            sb.AppendLine($"  \"author\": {{");
            sb.AppendLine($"    \"name\": \"{author}\"");
            sb.AppendLine($"  }},");
            sb.AppendLine($"  \"keywords\": [");
            sb.AppendLine($"    \"cwtk\",");
            sb.AppendLine($"    \"unity\",");
            sb.AppendLine($"    \"tools\"");
            sb.AppendLine($"  ]");
            sb.AppendLine("}");
            
            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(packageJsonPath));
            
            // 写入文件
            File.WriteAllText(packageJsonPath, sb.ToString());
            
            // 验证文件是否已创建
            if (File.Exists(packageJsonPath))
            {
                UnityEngine.Debug.Log($"{(isNewFile ? "创建" : "更新")}package.json成功: {packageJsonPath}");
            }
            else
            {
                UnityEngine.Debug.LogError($"package.json文件未能创建: {packageJsonPath}");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"更新package.json失败: {ex.Message}");
        }
    }
    
    private void ClearRepositoryContent()
    {
        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(localRepoPath);
            
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Name != ".gitignore" && !file.Name.StartsWith(".git"))
                {
                    file.Delete();
                }
            }
            
            foreach (var dir in dirInfo.GetDirectories())
            {
                if (dir.Name != ".git")
                {
                    dir.Delete(true);
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"清理仓库内容失败: {ex.Message}");
        }
    }
    
    private async Task CopySelectedFoldersToRepoAsync(List<string> selectedPaths)
    {
        int totalPaths = selectedPaths.Count;
        int processed = 0;
        
        foreach (string sourcePath in selectedPaths)
        {
            try
            {
                processed++;
                float progress = 0.15f + (0.55f * processed / totalPaths);
                UpdateProgress("准备上传", progress, $"正在复制: {sourcePath}");
                
                string relativePath = sourcePath;
                if (sourcePath.StartsWith("Assets/"))
                {
                    // 对于UPM包，我们将Assets/下的内容直接复制到根目录
                    relativePath = sourcePath.Substring(7); // 移除"Assets/"前缀
                }
                
                string targetPath = Path.Combine(localRepoPath, relativePath);
                
                if (File.Exists(sourcePath))
                {
                    // 复制单个文件
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    File.Copy(sourcePath, targetPath, true);
                }
                else if (Directory.Exists(sourcePath))
                {
                    // 复制文件夹
                    await CopyDirectoryAsync(sourcePath, targetPath);
                }
                
                // 每个文件/文件夹后稍微延迟，让UI能够更新
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"复制 {sourcePath} 失败: {ex.Message}");
                progressDetails = $"错误: {ex.Message}";
                Repaint();
            }
        }
        
        // 创建UPM所需的特殊文件
        await CreateUPMSpecialFilesAsync();
        
        UnityEngine.Debug.Log("所有选定文件已复制到仓库");
    }
    
    private async Task CreateUPMSpecialFilesAsync()
    {
        try
        {
            UpdateProgress("准备上传", 0.65f, "正在创建UPM所需文件...");
            
            // 首先确保package.json存在
            string packageJsonPath = Path.Combine(localRepoPath, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                UnityEngine.Debug.Log("在CreateUPMSpecialFilesAsync中创建package.json");
                CreatePackageJson();
                await Task.Delay(100);
            }
            
            // 创建README.md文件（如果不存在）
            string readmePath = Path.Combine(localRepoPath, "README.md");
            if (!File.Exists(readmePath))
            {
                StringBuilder readmeSb = new StringBuilder();
                readmeSb.AppendLine("# CWTK Unity工具包");
                readmeSb.AppendLine();
                readmeSb.AppendLine("## 简介");
                readmeSb.AppendLine("CWTK是一个专为Unity开发的工具包，提供多种实用功能。");
                readmeSb.AppendLine();
                readmeSb.AppendLine("## 安装方法");
                readmeSb.AppendLine("### 通过Unity Package Manager安装");
                readmeSb.AppendLine("1. 打开Unity项目");
                readmeSb.AppendLine("2. 打开Package Manager (Window > Package Manager)");
                readmeSb.AppendLine("3. 点击左上角的 \"+\" 按钮，选择 \"Add package from git URL...\"");
                readmeSb.AppendLine($"4. 输入: `{githubRepoUrl}`");
                readmeSb.AppendLine("5. 点击 \"Add\" 按钮");
                readmeSb.AppendLine();
                readmeSb.AppendLine("## 当前版本");
                readmeSb.AppendLine($"v{currentVersion}");
                
                File.WriteAllText(readmePath, readmeSb.ToString());
                UnityEngine.Debug.Log("创建README.md成功");
            }
            
            // 创建CHANGELOG.md文件（如果不存在或有更新）
            string changelogMdPath = Path.Combine(localRepoPath, "CHANGELOG.md");
            if (File.Exists(changelogPath) && (!File.Exists(changelogMdPath) || 
                File.GetLastWriteTime(changelogPath) > File.GetLastWriteTime(changelogMdPath)))
            {
                // 将TXT格式的更新日志转换为Markdown格式
                string txtContent = File.ReadAllText(changelogPath);
                StringBuilder mdContent = new StringBuilder();
                mdContent.AppendLine("# 更新日志");
                mdContent.AppendLine();
                
                string[] lines = txtContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool isVersionHeader = true;
                
                foreach (string line in lines)
                {
                    if (line.StartsWith("v") && line.Contains("("))
                    {
                        if (!isVersionHeader)
                        {
                            mdContent.AppendLine();
                        }
                        mdContent.AppendLine($"## {line}");
                        isVersionHeader = false;
                    }
                    else
                    {
                        mdContent.AppendLine($"- {line}");
                    }
                }
                
                File.WriteAllText(changelogMdPath, mdContent.ToString());
            }
            
            // 创建LICENSE文件（如果不存在）
            string licensePath = Path.Combine(localRepoPath, "LICENSE");
            if (!File.Exists(licensePath))
            {
                StringBuilder licenseSb = new StringBuilder();
                licenseSb.AppendLine("MIT License");
                licenseSb.AppendLine();
                licenseSb.AppendLine($"Copyright (c) {DateTime.Now.Year} ChoseWay");
                licenseSb.AppendLine();
                licenseSb.AppendLine("Permission is hereby granted, free of charge, to any person obtaining a copy");
                licenseSb.AppendLine("of this software and associated documentation files (the \"Software\"), to deal");
                licenseSb.AppendLine("in the Software without restriction, including without limitation the rights");
                licenseSb.AppendLine("to use, copy, modify, merge, publish, distribute, sublicense, and/or sell");
                licenseSb.AppendLine("copies of the Software, and to permit persons to whom the Software is");
                licenseSb.AppendLine("furnished to do so, subject to the following conditions:");
                licenseSb.AppendLine();
                licenseSb.AppendLine("The above copyright notice and this permission notice shall be included in all");
                licenseSb.AppendLine("copies or substantial portions of the Software.");
                licenseSb.AppendLine();
                licenseSb.AppendLine("THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR");
                licenseSb.AppendLine("IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,");
                licenseSb.AppendLine("FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE");
                licenseSb.AppendLine("AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER");
                licenseSb.AppendLine("LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,");
                licenseSb.AppendLine("OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE");
                licenseSb.AppendLine("SOFTWARE.");
                
                File.WriteAllText(licensePath, licenseSb.ToString());
            }
            
            // 等待一小段时间，让UI更新
            await Task.Delay(200);
            
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"创建UPM特殊文件失败: {ex.Message}");
        }
    }
    
    private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        
        // 复制文件
        string[] files = Directory.GetFiles(sourceDir);
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string fileName = Path.GetFileName(file);
            string targetFile = Path.Combine(targetDir, fileName);
            
            progressDetails = $"复制文件: {fileName}";
            Repaint();
            
            File.Copy(file, targetFile, true);
            
            // 每10个文件更新一次，避免UI过于频繁刷新
            if (i % 10 == 0)
            {
                await Task.Delay(1);
            }
        }
        
        // 递归复制子目录
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            
            // 跳过.git目录
            if (dirName == ".git") continue;
            
            progressDetails = $"复制目录: {dirName}";
            Repaint();
            
            string targetSubDir = Path.Combine(targetDir, dirName);
            await CopyDirectoryAsync(dir, targetSubDir);
        }
    }
    
    private async Task<bool> ExecuteGitCommandAsync(string arguments, string progressTitle)
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = localRepoPath;
            
            StringBuilder output = new StringBuilder();
            
            process.OutputDataReceived += (sender, args) => { 
                if (args.Data != null) {
                    output.AppendLine(args.Data);
                    progressDetails = args.Data;
                    Repaint();
                }
            };
            
            process.ErrorDataReceived += (sender, args) => { 
                if (args.Data != null) {
                    output.AppendLine(args.Data);
                    // 如果是行结束符警告，不要阻止进程
                    if (args.Data.Contains("LF will be replaced by CRLF") || 
                        args.Data.Contains("CRLF will be replaced by LF"))
                    {
                        UnityEngine.Debug.LogWarning("Git行结束符警告: " + args.Data);
                    }
                    progressDetails = args.Data;
                    Repaint();
                }
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // 设置超时时间，防止无限等待
            bool hasExited = false;
            int timeoutCounter = 0;
            int timeoutLimit = 300; // 30秒超时
            
            while (!hasExited && timeoutCounter < timeoutLimit)
            {
                hasExited = process.WaitForExit(100);
                timeoutCounter++;
                await Task.Delay(100);
                Repaint();
                
                // 如果进度信息包含行结束符警告，则继续执行
                if (progressDetails.Contains("LF will be replaced by CRLF") || 
                    progressDetails.Contains("CRLF will be replaced by LF"))
                {
                    // 刷新进度显示
                    progressDetails += "\n(这是正常的行结束符警告，正在继续处理...)";
                    Repaint();
                }
            }
            
            if (!hasExited)
            {
                // 如果超时，尝试终止进程
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch { }
                
                EditorUtility.DisplayDialog("警告", "Git命令执行超时，可能是因为需要用户交互。请尝试在终端中手动执行Git命令。", "确定");
                return false;
            }
            
            UnityEngine.Debug.Log($"Git命令 '{arguments}' 输出:\n{output}");
            
            // 即使有行结束符警告也视为成功
            if (process.ExitCode != 0)
            {
                string outputStr = output.ToString();
                if (outputStr.Contains("LF will be replaced by CRLF") || 
                    outputStr.Contains("CRLF will be replaced by LF"))
                {
                    UnityEngine.Debug.LogWarning("Git命令有行结束符警告，但继续执行: " + outputStr);
                    return true;
                }
                
                EditorUtility.DisplayDialog("Git错误", $"命令执行失败: git {arguments}\n\n{output}", "确定");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("错误", $"执行Git命令失败: {ex.Message}", "确定");
            return false;
        }
    }
    
    private bool ExecuteGitCommand(string arguments, string progressTitle)
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = localRepoPath;
            
            StringBuilder output = new StringBuilder();
            process.OutputDataReceived += (sender, args) => { 
                if (args.Data != null) {
                    output.AppendLine(args.Data);
                    if (isUploading)
                    {
                        progressDetails = args.Data;
                        Repaint();
                    }
                }
            };
            process.ErrorDataReceived += (sender, args) => { 
                if (args.Data != null) {
                    output.AppendLine(args.Data);
                    if (isUploading)
                    {
                        progressDetails = args.Data;
                        Repaint();
                    }
                }
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            
            UnityEngine.Debug.Log($"Git命令 '{arguments}' 输出:\n{output}");
            
            if (process.ExitCode != 0)
            {
                EditorUtility.DisplayDialog("Git错误", $"命令执行失败: git {arguments}\n\n{output}", "确定");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("错误", $"执行Git命令失败: {ex.Message}", "确定");
            return false;
        }
    }
    
    private void UpdateProgress(string step, float progress, string details)
    {
        isUploading = true;
        currentProgressStep = step;
        uploadProgress = progress;
        progressDetails = details;
        Repaint();
    }
    
    private void ResetProgress()
    {
        isUploading = false;
        currentProgressStep = "";
        uploadProgress = 0f;
        progressDetails = "";
        Repaint();
    }

    private List<string> GetSelectedPaths()
    {
        List<string> selectedPaths = new List<string>();
        
        for (int i = 0; i < foldersToExport.Count; i++)
        {
            if (foldersToExportToggles[i])
            {
                selectedPaths.Add(foldersToExport[i]);
            }
        }
        
        // 添加更新记录文件
        if (File.Exists(changelogPath) && !selectedPaths.Contains(Path.GetDirectoryName(changelogPath)))
        {
            selectedPaths.Add(changelogPath);
        }
        
        return selectedPaths;
    }
    
    private void UploadToGitHub()
    {
        // 启动异步上传过程
        UploadToGitHubAsync();
    }
} 