using System.IO;
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityWebBrowser.Editor
{
    class IdentityPostprocess : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result is BuildResult.Failed || report.summary.result is BuildResult.Cancelled)
                return;

            var buildTarget = report.summary.platform;
            var buildFullOutputPath = report.summary.outputPath;
            var buildAppName = Path.GetFileNameWithoutExtension(buildFullOutputPath);
            var buildOutputPath = Path.GetDirectoryName(buildFullOutputPath);

            if (buildTarget == BuildTarget.iOS)
            {
                var projPath = $"{buildOutputPath}/{buildAppName}" + "/Unity-iPhone.xcodeproj/project.pbxproj";
                var type = Type.GetType("UnityEditor.iOS.Xcode.PBXProject, UnityEditor.iOS.Extensions.Xcode");

                if (type == null)
                {
                    Debug.LogError("unitywebview: failed to get PBXProject. please install iOS build support.");
                    return;
                }

                var src = File.ReadAllText(projPath);
                var proj = type.GetConstructor(Type.EmptyTypes).Invoke(null);
                {
                    var method = type.GetMethod("ReadFromString");
                    method.Invoke(proj, new object[] { src });
                }

                var target = string.Empty;
#if UNITY_2019_3_OR_NEWER
                {
                    var method = type.GetMethod("GetUnityFrameworkTargetGuid");
                    target = (string)method.Invoke(proj, null);
                }
#else
                {
                    var method = type.GetMethod("TargetGuidByName");
                    target = (string)method.Invoke(proj, new object[]{"Unity-iPhone"});
                }
#endif
                {
                    var method = type.GetMethod("AddFrameworkToProject");
                    method.Invoke(proj, new object[] { target, "WebKit.framework", false });
                }
                {
                    var method = type.GetMethod("AddFrameworkToProject");
                    method.Invoke(proj, new object[] { target, "AuthenticationServices.framework", false });
                }

                var cflags = string.Empty;
                if (EditorUserBuildSettings.development)
                {
                    cflags += " -DUNITYWEBVIEW_DEVELOPMENT";
                }

                cflags = cflags.Trim();

                if (!string.IsNullOrEmpty(cflags))
                {
                    var method = type.GetMethod("AddBuildProperty", new Type[] { typeof(string), typeof(string), typeof(string) });
                    method.Invoke(proj, new object[] { target, "OTHER_CFLAGS", cflags });
                }

                var dst = string.Empty;
                {
                    var method = type.GetMethod("WriteToString");
                    dst = (string)method.Invoke(proj, null);
                }

                File.WriteAllText(projPath, dst);
            }
        }
    }
}