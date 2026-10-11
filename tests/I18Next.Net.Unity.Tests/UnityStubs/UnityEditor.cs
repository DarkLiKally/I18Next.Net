using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.Build
{
    using UnityEditor.Build.Reporting;
    using UnityEditor.UnityLinker;

    public interface IOrderedCallback
    {
        int callbackOrder { get; }
    }

    public interface IUnityLinkerProcessor : IOrderedCallback
    {
        string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data);
    }
}

namespace UnityEditor.Build.Reporting
{
    public sealed class BuildReport : UnityEngine.Object
    {
    }
}

namespace UnityEditor.UnityLinker
{
    public sealed class UnityLinkerBuildPipelineData
    {
    }
}

namespace UnityEditor.PackageManager
{
    public sealed class PackageInfo
    {
        internal static Dictionary<Assembly, PackageInfo> Packages { get; } = new Dictionary<Assembly, PackageInfo>();

        internal PackageInfo(string resolvedPath)
        {
            this.resolvedPath = resolvedPath;
        }

        public string resolvedPath { get; }

        public static PackageInfo FindForAssembly(Assembly assembly)
        {
            return Packages.TryGetValue(assembly, out var package) ? package : null;
        }
    }
}
