using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using I18Next.Net.Unity.Editor;

using Shouldly;

using Xunit;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace I18Next.Net.Unity.Tests
{
    [Collection(nameof(I18NextUnity))]
    public class PackageFixture : IDisposable
    {
        private static readonly string RepositoryPath = FindRepositoryPath();
        private static readonly string PackagePath = Path.Combine(RepositoryPath, "unity", "com.darklikally.i18next-net");

        public PackageFixture()
        {
            UnityTestEnvironment.Reset();
        }

        public void Dispose()
        {
            UnityTestEnvironment.Reset();
        }

        private static string FindRepositoryPath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "I18Next.Net.slnx")))
                directory = directory.Parent;

            return directory?.FullName ?? throw new DirectoryNotFoundException("The repository was not found.");
        }

        private static IEnumerable<string> GetAssets(string path)
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                var name = Path.GetFileName(entry);

                if (name.StartsWith(".", StringComparison.Ordinal) || name.EndsWith("~", StringComparison.Ordinal) ||
                    name.EndsWith(".meta", StringComparison.Ordinal))
                    continue;

                yield return entry;

                if (Directory.Exists(entry))
                {
                    foreach (var child in GetAssets(entry))
                        yield return child;
                }
            }
        }

        private static JsonElement ReadJson(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));

            return document.RootElement.Clone();
        }

        [Fact]
        public void Assets_ShouldHaveMetaFilesWithUniqueGuids()
        {
            var guids = new HashSet<string>();

            foreach (var asset in GetAssets(PackagePath))
            {
                var metaPath = asset + ".meta";

                File.Exists(metaPath).ShouldBeTrue(metaPath);

                var guid = Regex.Match(File.ReadAllText(metaPath), "^guid: ([0-9a-f]{32})$", RegexOptions.Multiline).Groups[1].Value;

                guid.ShouldNotBeEmpty(metaPath);
                guids.Add(guid).ShouldBeTrue(metaPath);
            }
        }

        [Fact]
        public void MetaFiles_ShouldBelongToAssets()
        {
            foreach (var metaPath in Directory.EnumerateFiles(PackagePath, "*.meta", SearchOption.AllDirectories))
            {
                if (metaPath.Contains("~" + Path.DirectorySeparatorChar))
                    continue;

                var asset = metaPath.Substring(0, metaPath.Length - ".meta".Length);

                (File.Exists(asset) || Directory.Exists(asset)).ShouldBeTrue(metaPath);
            }
        }

        [Fact]
        public void Manifest_ShouldMatchLibrary()
        {
            var manifest = ReadJson(Path.Combine(PackagePath, "package.json"));
            var version = XDocument.Load(Path.Combine(RepositoryPath, "Directory.Build.props")).Descendants("Version").First().Value;

            manifest.GetProperty("name").GetString().ShouldBe("com.darklikally.i18next-net");
            manifest.GetProperty("version").GetString().ShouldBe(version);
            manifest.GetProperty("unity").GetString().ShouldBe("2021.3");

            foreach (var sample in manifest.GetProperty("samples").EnumerateArray())
                Directory.Exists(Path.Combine(PackagePath, sample.GetProperty("path").GetString())).ShouldBeTrue();
        }

        [Fact]
        public void AssemblyDefinitions_ShouldReferenceLibraryAssemblies()
        {
            var assemblies = new[] { typeof(I18NextNet).Assembly.GetName().Name + ".dll", typeof(II18Next).Assembly.GetName().Name + ".dll" };

            foreach (var path in Directory.EnumerateFiles(Path.Combine(PackagePath, "Runtime"), "*.asmdef", SearchOption.AllDirectories))
            {
                var definition = ReadJson(path);

                definition.GetProperty("overrideReferences").GetBoolean().ShouldBeTrue(path);
                definition.GetProperty("precompiledReferences").EnumerateArray().Select(e => e.GetString()).ShouldBe(assemblies, path);
            }
        }

        [Fact]
        public void LinkXml_ShouldPreserveLibraryAssemblies()
        {
            var assemblies = XDocument.Load(Path.Combine(PackagePath, "Runtime", "link.xml")).Descendants("assembly")
                .Select(e => (string)e.Attribute("fullname"));

            assemblies.ShouldBe(new[] { typeof(I18NextNet).Assembly.GetName().Name, typeof(II18Next).Assembly.GetName().Name });
        }

        [Fact]
        public void LinkXmlProcessor_Package_ShouldReturnLinkXml()
        {
            PackageInfo.Packages[typeof(LinkXmlProcessor).Assembly] = new PackageInfo(PackagePath);

            var path = new LinkXmlProcessor().GenerateAdditionalLinkXmlFile(null, null);

            path.ShouldBe(Path.Combine(PackagePath, "Runtime", "link.xml"));
        }

        [Fact]
        public void LinkXmlProcessor_NoPackage_ShouldReturnNull()
        {
            new LinkXmlProcessor().GenerateAdditionalLinkXmlFile(null, null).ShouldBeNull();
        }
    }
}
