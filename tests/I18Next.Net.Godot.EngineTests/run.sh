#!/usr/bin/env bash
# Usage: run.sh [godot-version] [godot-executable]
set -euo pipefail

version="${1:-4.3.0}"
godot="${2:-}"
project="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -z "$godot" ]; then
    release="${version%.0}-stable"
    directory="$(mktemp -d)"
    curl -fsSL -o "$directory/godot.zip" \
        "https://github.com/godotengine/godot/releases/download/$release/Godot_v${release}_mono_linux_x86_64.zip"
    unzip -q "$directory/godot.zip" -d "$directory"
    godot="$directory/Godot_v${release}_mono_linux_x86_64/Godot_v${release}_mono_linux.x86_64"
fi

dotnet build "$project/I18Next.Net.Godot.EngineTests.csproj" -p:GodotVersion="$version"
timeout 300 "$godot" --headless --path "$project"
