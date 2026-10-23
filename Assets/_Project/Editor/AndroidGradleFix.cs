using System.IO;
using UnityEditor.Android;

namespace FirstHand.EditorTools
{
    /// <summary>
    /// ARCore ships two Android libraries with the same namespace (com.google.ar.core). Newer Android Gradle
    /// plugins reject that by default, so the generated project turns the unique namespace check off.
    /// </summary>
    public class AndroidGradleFix : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 999;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string props = Path.Combine(Path.GetDirectoryName(path.TrimEnd('/', '\\')), "gradle.properties");
            if (!File.Exists(props)) return;
            string text = File.ReadAllText(props);
            if (text.Contains("android.uniquePackageNames")) return;
            File.AppendAllText(props, "\nandroid.uniquePackageNames=false\n");
        }
    }
}
