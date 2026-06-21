#!/usr/bin/env bash
#
# Build and package the ODC External Library for upload to the ODC Portal.
# Produces ExternalLibrary_<tfm>.zip at the repo root.
#
# Usage:  ./build/generate_upload_package.sh [tfm]      (default tfm: net8.0)
#
set -euo pipefail

TFM="${1:-net8.0}"
RID="linux-x64"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/src/OutSystems.QuestPdf/OutSystems.QuestPdf.csproj"
PUBDIR="$ROOT/src/OutSystems.QuestPdf/bin/Release/$TFM/$RID/publish"
OUT="$ROOT/ExternalLibrary_${TFM}.zip"

echo "==> Publishing ($TFM / $RID, framework-dependent)"
dotnet publish "$PROJECT" -c Release -r "$RID" --self-contained false -f "$TFM"

echo "==> Checking the native rendering engine is in the publish output"
# QuestPDF flattens its native engine to the publish root (libQuestPdfSkia.so,
# libqpdf.so), not under runtimes/<rid>/native.
if ! find "$PUBDIR" \( -iname '*questpdfskia*' -o -iname 'libskiasharp*' \) 2>/dev/null | grep -q .; then
  echo "    WARNING: no native rendering library found in the publish output."
  echo "             The library would throw DllNotFoundException at runtime in ODC."
  echo "             Make sure QuestPDF is referenced directly and you published with -r $RID."
fi

echo "==> Zipping publish output -> $OUT"
rm -f "$OUT"
( cd "$PUBDIR" && zip -r -q "$OUT" . )

echo "==> Package contents (native libs):"
unzip -l "$OUT" | grep -iE 'questpdfskia|\.so' || echo "    (none — investigate before uploading)"

echo "==> Done: $OUT"
echo "    Upload it in the ODC Portal under External Logic."
