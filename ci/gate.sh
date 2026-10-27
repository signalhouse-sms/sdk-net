#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

SDK=net

# >>> version-files (S3; must match versionFiles("net") in ci/sdk-mirror/set-version.mjs)
VERSION_FILES=(
	SignalHouse.SDK/SignalHouse.SDK.csproj
)
# <<< version-files

fail() { echo "::error::$SDK: $1" >&2; exit 1; }
check() { local name=$1; shift; "$@" || fail "$name"; }

T=$(mktemp -d)
trap 'rm -rf "$T"' EXIT
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
# bin/ and obj/ go to a temp dir, never the tree.
ART=(--artifacts-path "$T/artifacts")

# Prints "<file> <field> <value>" per version field.
read_versions() {
	local f
	for f in "${VERSION_FILES[@]}"; do
		case $f in
			SignalHouse.SDK/SignalHouse.SDK.csproj) sed -nE "s#^ *<Version>([^<]+)</Version> *\$#$f <Version> \1#p" "$f" ;;
			*) fail "version-files: no reader for $f" ;;
		esac
	done
}

check_versions() {
	local lines want
	lines=$(read_versions) || return 1
	[ "$(grep -c . <<<"$lines")" -eq 1 ] || { echo "  expected one <Version>, got: $lines" >&2; return 1; }
	want=${EXPECTED_VERSION:-$(head -1 <<<"$lines" | awk '{print $NF}')}
	echo "$lines" | awk -v want="$want" '$NF != want { print "  " $0 " != " want; bad=1 } END { exit bad }'
}

check_tests() {
	local p
	p=$(find . -name '*Tests.csproj' -o -name '*.Test.csproj' | head -1)
	if [ -z "$p" ]; then echo "  no test project"; return 0; fi
	dotnet test -c Release --no-restore "${ART[@]}"
}

check_pack() {
	local v nupkg bad
	v=$(read_versions | awk '{print $NF}')
	dotnet pack -c Release --no-build "${ART[@]}" -o "$T/pkg" >"$T/pack.log" 2>&1 || { cat "$T/pack.log" >&2; return 1; }
	nupkg=$T/pkg/SignalHouseSms.Sdk.$v.nupkg
	[ -f "$nupkg" ] || { echo "  missing $nupkg" >&2; ls "$T/pkg" >&2; return 1; }
	unzip -Z1 "$nupkg" | sort >"$T/nupkg.list"
	for need in SignalHouseSms.Sdk.nuspec lib/net8.0/SignalHouse.SDK.dll README.md; do
		grep -qx "$need" "$T/nupkg.list" || { echo "  nupkg missing $need" >&2; return 1; }
	done
	bad=$(grep -E '^(legacy|ci|\.github)/|(^|/)LEGACY_[^/]*\.md$' "$T/nupkg.list" || true)
	[ -z "$bad" ] || { echo "$bad" >&2; return 1; }
	unzip -p "$nupkg" SignalHouseSms.Sdk.nuspec >"$T/nuspec"
	grep -q "<id>SignalHouseSms.Sdk</id>" "$T/nuspec" && grep -q "<version>$v</version>" "$T/nuspec" || { echo "  nuspec id/version" >&2; return 1; }
}

echo "== $SDK: versions"; check versions check_versions
echo "== $SDK: dotnet restore"; check "dotnet restore" dotnet restore "${ART[@]}"
echo "== $SDK: dotnet build"; check "dotnet build" dotnet build -c Release --no-restore -warnaserror "${ART[@]}"
echo "== $SDK: tests"; check tests check_tests
echo "== $SDK: pack"; check pack check_pack
echo "gate passed: $SDK"
