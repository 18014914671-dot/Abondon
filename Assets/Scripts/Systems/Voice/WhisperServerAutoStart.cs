using UnityEngine;

public class WhisperServerAutoStart : MonoBehaviour
{
    public WhisperServerProcess server;

    private void Awake()
    {
        if (server != null)
            server.EnsureRunning();
    }
}
