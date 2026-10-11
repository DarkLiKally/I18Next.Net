#!/usr/bin/env bash
# Usage: tools/unity/pack.sh [version] [output directory]
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
name="com.darklikally.i18next-net"
package="$root/unity/$name"
version="${1:-$(sed -n 's/^  "version": "\(.*\)",$/\1/p' "$package/package.json")}"
output="$(mkdir -p "${2:-$root/artifacts/unity}" && cd "${2:-$root/artifacts/unity}" && pwd)"

libraries=(I18Next.Net I18Next.Net.Abstractions)
system_text_json=(System.Text.Json System.Text.Encodings.Web Microsoft.Bcl.AsyncInterfaces)
unsafe=(System.Runtime.CompilerServices.Unsafe)
provided_by_unity=(System.Buffers System.Memory System.Numerics.Vectors System.Threading.Tasks.Extensions)

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

cat > "$work/UnityPlugins.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <EnableDefaultItems>false</EnableDefaultItems>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$root/src/I18Next.Net/I18Next.Net.csproj" />
  </ItemGroup>
</Project>
EOF

dotnet publish "$work/UnityPlugins.csproj" -c Release -o "$work/publish" \
  -p:Version="$version" -p:GeneratePackageOnBuild=false -p:ContinuousIntegrationBuild=true

for file in "$work"/publish/*.dll; do
  assembly="$(basename "$file" .dll)"
  case " UnityPlugins ${libraries[*]} ${system_text_json[*]} ${unsafe[*]} ${provided_by_unity[*]} " in
    *" $assembly "*) ;;
    *) echo "Unexpected dependency $assembly, add it to $0" >&2; exit 1 ;;
  esac
done

guid() {
  printf '%s' "$name/$1" | md5sum | cut -c1-32
}

write_folder_meta() {
  cat > "$stage/$1.meta" <<EOF
fileFormatVersion: 2
guid: $(guid "$1")
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
EOF
}

add_plugin() {
  local path="$1/$2.dll" constraint="${3:-}" constraints="  defineConstraints: []"

  if [[ -n "$constraint" ]]; then
    constraints="  defineConstraints:
  - '!$constraint'"
  fi

  cp "$work/publish/$2.dll" "$stage/$path"
  cat > "$stage/$path.meta" <<EOF
fileFormatVersion: 2
guid: $(guid "$path")
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
$constraints
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 1
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        DefaultValueInitialized: true
  - first:
      Windows Store Apps: WindowsStoreApps
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
  userData:
  assetBundleName:
  assetBundleVariant:
EOF
}

stage="$work/package"
cp -R "$package" "$stage"
sed -i "s/^  \"version\": \".*\",$/  \"version\": \"$version\",/" "$stage/package.json"

mkdir -p "$stage/Runtime/Plugins/Dependencies"
write_folder_meta Runtime/Plugins
write_folder_meta Runtime/Plugins/Dependencies

for assembly in "${libraries[@]}"; do
  add_plugin Runtime/Plugins "$assembly"
done

for assembly in "${system_text_json[@]}"; do
  add_plugin Runtime/Plugins/Dependencies "$assembly" I18NEXT_NET_EXCLUDE_SYSTEM_TEXT_JSON
done

for assembly in "${unsafe[@]}"; do
  add_plugin Runtime/Plugins/Dependencies "$assembly" I18NEXT_NET_EXCLUDE_UNSAFE
done

find "$stage" -type d -exec chmod 755 {} +
find "$stage" -type f -exec chmod 644 {} +

tarball="$output/$name-$version.tgz"
tar --sort=name --owner=0 --group=0 --numeric-owner \
  --mtime="@${SOURCE_DATE_EPOCH:-$(date +%s)}" -C "$work" -czf "$tarball" package

echo "$tarball"
