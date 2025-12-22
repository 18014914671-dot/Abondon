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

    [Header("Server Exe")]
    [Tooltip("whisper-server.exe 完整路径（建议放在项目外）")]
    public string serverExePath = @"C:\Whisper\whisper.cpp\build\bin\Release\whisper-server.exe";

    [Header("Model")]
    [Tooltip("模型 .bin 完整路径")]
    public string modelPath = @"C:\Whisper\whisper.cpp\models\ggml-base-q5_1.bin";

    [Header("Args")]
    [Tooltip("额外参数（可留空）。例如：--threads 4")]
    public string extraArgs = "";

    [Header("Options")]
    [Tooltip("如果你看不到命令行窗口可以关掉它")]
    public bool createNoWindow = true;

    [Tooltip("把 stdout/stderr 打进 Unity Console（调试用）")]
    public bool logOutput = false;

    private Process _proc;

    public bool IsRunning
    {
        get
        {
            try { return _proc != null && !_proc.HasExited; }
            catch { return false; }
        }
    }

    private void OnDestroy()
    {
        StopServer();
    }

    public void EnsureRunning()
    {
        if (IsRunning) return;

        if (string.IsNullOrWhiteSpace(serverExePath) || !File.Exists(serverExePath))
        {
            UDebug.LogWarning($"[WhisperServerProcess] serverExePath invalid: {serverExePath}");
            return;
        }

        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            UDebug.LogWarning($"[WhisperServerProcess] modelPath invalid: {modelPath}");
            return;
        }

        StartServer();
    }

    public void StartServer()
    {
        if (IsRunning) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = serverExePath,
                Arguments = BuildArgs(),
                UseShellExecute = false,
                CreateNoWindow = createNoWindow,
                RedirectStandardOutput = logOutput,
                RedirectStandardError = logOutput,
                WorkingDirectory = Path.GetDirectoryName(serverExePath),
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

            UDebug.Log($"[WhisperServerProcess] Started. {BaseUrl}\nArgs: {psi.Arguments}");
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
                    _proc.Kill();               // ✅ Kill() 不要传参数
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

    private string BuildArgs()
    {
        // whisper.cpp server 常见参数：--host --port -m
        // 如果你编译的版本参数名不同，也至少能从 log 里看到启动失败原因
        string args = $"--host {host} --port {port} -m \"{modelPath}\"";

        if (!string.IsNullOrWhiteSpace(extraArgs))
            args += " " + extraArgs.Trim();

        return args;
    }
}
