using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealityFractures.EditorTools
{
    public static class TestSceneSave
    {
        [MenuItem("Reality Fractures/Test Direct Scene Save")]
        public static void SaveTest()
        {
            string p1 = "Assets/_RealityFractures/Scenes/0_Splash.unity";
            Debug.Log($"[TestSceneSave] DataPath: '{Application.dataPath}' | AbsolutePath: '{Path.GetFullPath(p1)}'");
        }
    }
}
