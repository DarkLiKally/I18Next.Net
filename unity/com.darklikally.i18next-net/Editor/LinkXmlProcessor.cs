using System.IO;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace I18Next.Net.Unity.Editor
{
    /// <summary>
    ///     Passes the <c>link.xml</c> of the package to the Unity linker, which only reads <c>link.xml</c> files in the
    ///     <c>Assets</c> folder.
    /// </summary>
    internal sealed class LinkXmlProcessor : IUnityLinkerProcessor
    {
        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var package = PackageInfo.FindForAssembly(typeof(LinkXmlProcessor).Assembly);

            if (package == null)
                return null;

            var path = Path.Combine(package.resolvedPath, "Runtime", "link.xml");

            return File.Exists(path) ? path : null;
        }
    }
}
