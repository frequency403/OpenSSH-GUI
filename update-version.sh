#!/usr/bin/env bash
set -euo pipefail

PROPS="Directory.Build.props"
VERSION=$(grep -oP '(?<=<BaseVersion>)[^<]+' "${PROPS}")

echo "→ Version: ${VERSION}"

PKGBUILD_BIN="openssh-gui-bin/PKGBUILD"
sed -i "s/^pkgver=.*/pkgver=${VERSION}/" "${PKGBUILD_BIN}"
sed -i "s/^pkgrel=.*/pkgrel=1/"         "${PKGBUILD_BIN}"
(cd openssh-gui-bin && updpkgsums)

echo "✓ Done – ${PKGBUILD_BIN} → ${VERSION}"