using System.IO;
using UnityEditor.Android;

namespace FirstHand.EditorTools
{
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
