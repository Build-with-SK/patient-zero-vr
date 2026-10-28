#!/usr/bin/env bash
set -euo pipefail
version=8.0.427
install_dir="${PZ_DOTNET_DIR:-/workspace/tools/dotnet}"
mkdir -p "$install_dir"
if [[ ! -x "$install_dir/dotnet" ]] || [[ "$("$install_dir/dotnet" --version)" != "$version" ]]; then
    archive="$(mktemp /tmp/pz-dotnet.XXXXXX.tar.gz)"
    trap 'rm -f "$archive"' EXIT
    curl --fail --location --proto '=https' --tlsv1.2 \
        "https://builds.dotnet.microsoft.com/dotnet/Sdk/$version/dotnet-sdk-$version-linux-x64.tar.gz" -o "$archive"
    expected=85725692f465f5024b9b42a159a29d9f73e8c57ad9e8bfe1e7028a505d18031d68931e8aa3f9de44145b08170dde897c8a78e1aa18d4219698b6891af7095d39
    printf '%s  %s\n' "$expected" "$archive" | sha512sum --check --status
    tar -xzf "$archive" -C "$install_dir"
fi
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${PZ_DOTNET_STATE_DIR:-/workspace/tools/dotnet-home}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
"$install_dir/dotnet" run --project Tests/Portable -c Release
python3 tools/validate_assets.py
