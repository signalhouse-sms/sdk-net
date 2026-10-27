#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

SDK=net
PACKAGE_ID=SignalHouseSms.Sdk
fail() { echo "::error::$SDK: $1" >&2; exit 1; }
run() { if [ "${PUBLISH_DRY_RUN:-}" = 1 ]; then printf '+ (dry run) would run:'; printf ' %q' "$@"; echo; else "$@"; fi; }
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# Exit 0 = published, 1 = absent; anything else fails. The flat container uses lowercase ids and versions.
published() {
	local id ver body
	id=$(tr '[:upper:]' '[:lower:]' <<<"$1") ver=$(tr '[:upper:]' '[:lower:]' <<<"$2")
	body=$(curl -sS -w '\n%{http_code}' --retry 3 "https://api.nuget.org/v3-flatcontainer/$id/index.json") || fail "registry check $1 $2"
	case ${body##*$'\n'} in
		200) grep -q "\"$ver\"" <<<"${body%$'\n'*}" ;;
		404) return 1 ;;
		*) fail "registry check $1 $2: HTTP ${body##*$'\n'}" ;;
	esac
}

V=$(sed -nE 's#^ *<Version>([^<]+)</Version> *$#\1#p' SignalHouse.SDK/SignalHouse.SDK.csproj)
[ -n "$V" ] || fail "read <Version>"
if published "$PACKAGE_ID" "$V"; then echo "$PACKAGE_ID $V exists, skipping"; exit 0; fi

T=$(mktemp -d)
trap 'rm -rf "$T"' EXIT
run dotnet pack SignalHouse.SDK/SignalHouse.SDK.csproj -c Release --artifacts-path "$T/artifacts" -o "$T/pkg" || fail "dotnet pack"
if [ "${PUBLISH_DRY_RUN:-}" = 1 ]; then
	echo "+ (dry run) would run: dotnet nuget push $T/pkg/$PACKAGE_ID.$V.nupkg --skip-duplicate -k \$NUGET_API_KEY -s https://api.nuget.org/v3/index.json"
else
	[ -n "${NUGET_API_KEY:-}" ] || fail "NUGET_API_KEY unset"
	dotnet nuget push "$T/pkg/$PACKAGE_ID.$V.nupkg" --skip-duplicate -k "$NUGET_API_KEY" -s https://api.nuget.org/v3/index.json || fail "dotnet nuget push"
fi
