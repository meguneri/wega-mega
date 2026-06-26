#!/usr/bin/env bash
# Генерирует звук удара катаны Рэнгоку — смесь пореза и пламени.
# Накладывает bladeslice (резкий порез, ведёт) на короткий огненный «вшух» с
# затуханием. Моно — обязательно для позиционного звука движка.
#
# Требуется: ffmpeg + oggenc (brew install ffmpeg vorbis-tools).
# Запуск из корня репозитория:  bash Tools/gen_rengoku_slash_sound.sh
#
# Подкрутка: volume пламени/пореза и atrim/afade меняй прямо в filter_complex.
set -euo pipefail
cd "$(dirname "$0")/.."

OUT="Resources/Audio/_Wega/Weapons/rengoku_slash.ogg"
TMP="$(mktemp -t rengoku_slash).wav"

ffmpeg -y -i Resources/Audio/Weapons/bladeslice.ogg -i Resources/Audio/Effects/fire.ogg \
  -filter_complex "[1:a]atrim=0:0.9,afade=t=in:st=0:d=0.05,afade=t=out:st=0.6:d=0.3,volume=1.6[fire];\
[0:a]volume=0.85[slash];\
[slash][fire]amix=inputs=2:duration=longest:normalize=0[out]" \
  -map "[out]" -ac 1 -ar 44100 "$TMP"

oggenc -q 5 "$TMP" -o "$OUT"
rm -f "$TMP"

echo "Готово: $OUT"
ffprobe -v error -show_entries format=duration:stream=channels,codec_name -of default=nw=1 "$OUT"
