using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.Linq;

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
        
        bool isExistingRepo = Directory.Exists(Path.Combine(localRepoPath, ".git"));
        
        if (isExistingRepo)
        {
            // 已有仓库，拉取最新代码
            if (EditorUtility.DisplayDialog("确认", "检测到现有Git仓库，是否拉取最新代码？", "是", "否"))
            {
                ExecuteGitCommand("pull", "正在拉取最新代码...");
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
                Directory.CreateDirectory(localRepoPath);
                
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
                process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine(args.Data); };
                process.ErrorDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine(args.Data); };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                
                if (process.ExitCode == 0)
                {
                    EditorUtility.DisplayDialog("成功", "仓库克隆成功", "确定");
                    
                    // 如果需要使用LFS，初始化LFS
                    if (useGitLFS)
                    {
                        ExecuteGitCommand("lfs install", "正在初始化Git LFS...");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", $"仓库克隆失败: \n{output}", "确定");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"初始化仓库失败: {ex.Message}", "确定");
            }
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
    
    private void UploadToGitHub()
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
            }
            else
            {
                return;
            }
        }
        
        // 更新package.json文件
        UpdatePackageJson();
        
        // 清空仓库目录下的内容(除了.git目录)
        ClearRepositoryContent();
        
        // 复制选中的文件夹到仓库
        CopySelectedFoldersToRepo(selectedPaths);
        
        // Git添加、提交和推送
        if (ExecuteGitCommand("add .", "正在添加文件..."))
        {
            if (ExecuteGitCommand($"commit -m \"{commitMessage}\"", "正在提交更改..."))
            {
                ExecuteGitCommand("push", "正在推送到GitHub...");
            }
        }
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
    
    private void UpdatePackageJson()
    {
        string packageJsonPath = Path.Combine(localRepoPath, "package.json");
        bool isNewFile = !File.Exists(packageJsonPath);
        
        // 创建或更新package.json
        try
        {
            string packageName = Path.GetFileName(githubRepoUrl).Replace(".git", "");
            if (string.IsNullOrEmpty(packageName))
            {
                packageName = "com.choseway.cwtk";
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
            
            File.WriteAllText(packageJsonPath, sb.ToString());
            
            UnityEngine.Debug.Log($"{(isNewFile ? "创建" : "更新")}package.json成功");
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
    
    private void CopySelectedFoldersToRepo(List<string> selectedPaths)
    {
        foreach (string sourcePath in selectedPaths)
        {
            try
            {
                string relativePath = sourcePath;
                if (sourcePath.StartsWith("Assets/"))
                {
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
                    CopyDirectory(sourcePath, targetPath);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"复制 {sourcePath} 失败: {ex.Message}");
            }
        }
        
        UnityEngine.Debug.Log("所有选定文件已复制到仓库");
    }
    
    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        
        // 复制文件
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, true);
        }
        
        // 递归复制子目录
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            
            // 跳过.git目录
            if (dirName == ".git") continue;
            
            string targetSubDir = Path.Combine(targetDir, dirName);
            CopyDirectory(dir, targetSubDir);
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
            process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine(args.Data); };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine(args.Data); };
            
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
} 