// Temporary: keeps the MCP-for-Unity bridge alive (auto-restarts it when it stops).
// Safe to delete when the Claude Code session is finished.
using UnityEditor;
using MCPForUnity.Editor.Services;

[InitializeOnLoad]
public static class McpReconnectKick
{
    static double s_NextCheck;

    static McpReconnectKick()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (EditorApplication.timeSinceStartup < s_NextCheck) return;
        s_NextCheck = EditorApplication.timeSinceStartup + 30.0;
        try
        {
            if (!MCPServiceLocator.Bridge.IsRunning)
            {
                UnityEngine.Debug.Log("[McpReconnectKick] bridge not running; restarting...");
                _ = MCPServiceLocator.Bridge.StartAsync();
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning("[McpReconnectKick] " + ex.Message);
        }
    }
}
