using UnityEditor;

/// <summary>
/// Visible entry point — Unity 6 Window menu is category-based and hides some Window/* items.
/// </summary>
public static class UnityMcpMenuShortcut
{
    [MenuItem("Tools/Unity MCP %#m", false, 0)]
    public static void OpenUnityMcpWindow()
    {
        // Same path as package MenuItem("Window/Unity MCP")
        if (!EditorApplication.ExecuteMenuItem("Window/Unity MCP"))
        {
            EditorUtility.DisplayDialog(
                "Unity MCP",
                "Menu 'Window/Unity MCP' is missing.\n\n" +
                "Usually the package failed to compile (check Console for CS0619 / UnityMCP errors).\n" +
                "After scripts compile successfully, try again.",
                "OK");
        }
    }
}
