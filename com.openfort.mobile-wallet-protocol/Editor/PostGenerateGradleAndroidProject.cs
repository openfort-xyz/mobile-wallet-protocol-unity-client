using System;
using System.IO;
using UnityEditor.Android;

namespace MobileWalletProtocol.Editor
{
    class PostGenerateGradleAndroidProject : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            AddUseAndroidX(path);
        }

        private void AddUseAndroidX(string path)
        {
            var parentDir = Directory.GetParent(path).FullName;
            var gradlePath = $"{parentDir}/gradle.properties";

            if (!File.Exists(gradlePath))
            {
                throw new Exception("gradle.properties does not exist");
            }

            var text = File.ReadAllText(gradlePath);

            text += "\nandroid.useAndroidX=true\nandroid.enableJetifier=true";

            File.WriteAllText(gradlePath, text);
        }
    }
}
