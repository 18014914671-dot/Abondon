using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

// ✅ 避免 Debug / Application 冲突
using UDebug = UnityEngine.Debug;
using UApp = UnityEngine.Application;

[DisallowMultipleComponent]
public class WhisperServerProcess : MonoBehaviour
{
    [Header("Server Address")]
    public string host = "127.0.0.1";
    public int port = 8080;
    public string BaseUrl => $"http://{host}:{port}";

    [Header("Packaged Runtime (StreamingAssets)")]
    [Tooltip("在 Assets/StreamingAssets 下的相对路径（不要以 / 开头）")]
    public string streamingFolder = "Whisper";

    [Tooltip("StreamingAssets/Whisper/ 下的 server 文件名")]
    public string serverExeName = "whisper-server.exe";

    [Tooltip("StreamingAssets/Whisper/ 下的模型文件名")]
    public string modelFileName = "ggml-base-q5_1.bin";

    [Header("Args")]
    [Tooltip("额外参数（可留空）。例如：--threads 4")]
    public string extraArgs = "";

    [Header("Options")]
    public bool createNoWindow = true;
    public bool logOutput = false;

    // 运行时解包到这里（可写目录）
    private string _runtimeDir;
    private string _runtimeExePath;
    private string _runtimeModelPath;

    private Process _proc;

    public bool IsRunning
    {
        get
        {
            try { return _proc != null && !_proc.HasExited; }
            catch { return false; }
        }
    }

    private void Awake()
    {
        // 建议：每次运行都准备一份到 persistentDataPath（朋友电脑也一定有这个目录）
        _runtimeDir = Path.Combine(UApp.persistentDataPath, "WhisperRuntime");
        _runtimeExePath = Path.Combine(_runtimeDir, serverExeName);
        _runtimeModelPath = Path.Combine(_runtimeDir, modelFileName);
    }

    private void OnDestroy()
    {
        StopServer();
    }

    public void EnsureRunning()
    {
        if (IsRunning) return;

        if (!PrepareRuntimeFiles())
        {
            UDebug.LogWarning("[WhisperServerProcess] PrepareRuntimeFiles failed.");
            return;
        }

        StartServer();
    }

    private bool PrepareRuntimeFiles()
    {
        try
        {
            // StreamingAssets/Whisper
            string srcDir = Path.Combine(UApp.streamingAssetsPath, streamingFolder);

            // StreamingAssets 在 Android/部分平台是压缩包/URL，这套主要为 Windows/Mac 编辑器与桌面 Build
            // 你现在目标是朋友 PC 可用，所以这里 OK。
            if (!Directory.Exists(srcDir))
            {
                UDebug.LogWarning($"[WhisperServerProcess] StreamingAssets folder not found: {srcDir}\n" +
                                  $"请确认你已创建: Assets/StreamingAssets/{streamingFolder}/ 并放入 exe+model+依赖");
                return false;
            }

            Directory.CreateDirectory(_runtimeDir);

            // 复制整个 Whisper 目录（包括 dll 等依赖）
            CopyDirectory(srcDir, _runtimeDir);

            if (!File.Exists(_runtimeExePath))
            {
                UDebug.LogWarning($"[WhisperServerProcess] server exe not found after copy: {_runtimeExePath}");
                return false;
            }

            if (!File.Exists(_runtimeModelPath))
            {
                UDebug.LogWarning($"[WhisperServerProcess] model not found after copy: {_runtimeModelPath}");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            UDebug.LogWarning($"[WhisperServerProcess] PrepareRuntimeFiles exception: {e.Message}");
            return false;
        }
    }

    public void StartServer()
    {
        if (IsRunning) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _runtimeExePath,
                Arguments = BuildArgs(_runtimeModelPath),
                UseShellExecute = false,
                CreateNoWindow = createNoWindow,
                RedirectStandardOutput = logOutput,
                RedirectStandardError = logOutput,
                WorkingDirectory = _runtimeDir, // ✅ 很重要：dll/依赖会从工作目录找
            };

            _proc = new Process();
            _proc.StartInfo = psi;
            _proc.EnableRaisingEvents = true;
            _proc.Exited += (_, __) =>
            {
                if (logOutput) UDebug.Log("[WhisperServerProcess] server exited.");
            };

            bool started = _proc.Start();
            if (!started)
            {
                UDebug.LogWarning("[WhisperServerProcess] Failed to start process.");
                _proc = null;
                return;
            }

            if (logOutput)
            {
                _proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) UDebug.Log("[whisper] " + e.Data); };
                _proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) UDebug.LogWarning("[whisper] " + e.Data); };
                _proc.BeginOutputReadLine();
                _proc.BeginErrorReadLine();
            }

            UDebug.Log($"[WhisperServerProcess] Started. {BaseUrl}\nExe: {_runtimeExePath}\nModel: {_runtimeModelPath}\nArgs: {psi.Arguments}");
        }
        catch (Exception e)
        {
            UDebug.LogWarning($"[WhisperServerProcess] StartServer exception: {e.Message}");
            StopServer();
        }
    }

    public void StopServer()
    {
        try
        {
            if (_proc != null)
            {
                if (!_proc.HasExited)
                {
                    _proc.Kill();
                    _proc.WaitForExit(2000);
                }
                _proc.Dispose();
            }
        }
        catch { }
        finally
        {
            _proc = null;
        }
    }

    private string BuildArgs(string modelPath)
    {
        // whisper.cpp server 常见参数：--host --port -m
        string args = $"--host {host} --port {port} -m \"{modelPath}\"";
        if (!string.IsNullOrWhiteSpace(extraArgs))
            args += " " + extraArgs.Trim();
        return args;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string name = Path.GetFileName(file);
            string dest = Path.Combine(targetDir, name);
            File.Copy(file, dest, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string name = Path.GetFileName(dir);
            string dest = Path.Combine(targetDir, name);
            CopyDirectory(dir, dest);
        }
    }
}
