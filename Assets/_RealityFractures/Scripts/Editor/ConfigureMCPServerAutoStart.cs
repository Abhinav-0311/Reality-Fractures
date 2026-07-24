using UnityEditor;

namespace RealityFractures.Editor
{
    [InitializeOnLoad]
    public static class ConfigureMCPServerAutoStart
    {
        static ConfigureMCPServerAutoStart()
        {
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorPrefs.SetBool("MCPForUnity.HttpServerLaunchConfirmed", true);
            EditorPrefs.SetBool("MCPForUnity.SetupCompleted", true);
            EditorPrefs.SetString("MCPForUnity.LastSelectedClientId", "antigravity");
        }
    }
}
