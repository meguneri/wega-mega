#!/usr/bin/env bash
# Пакует чистый dev-билд wega-mega в zip для передачи мапперу (без git-возни).
# Маппер распаковывает, ставит .NET SDK и запускает run_mapping.* — всё.
#
# Запуск из корня репозитория:  bash Tools/pack_dev_build.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/../wega-mega-dev.zip"

echo "Пакую dev-билд из $ROOT"
echo "Исключаю: .git, bin/obj, кэши, секреты, прошлые релизы."

cd "$ROOT"
rm -f "$OUT"

# Архивируем рабочее дерево, выкидывая всё что мапперу не нужно и что
# раздувает архив. RobustToolbox оставляем — это сабмодуль движка, нужен для сборки.
zip -r -q "$OUT" . \
  -x '*/.git/*' '.git/*' \
  -x '*/bin/*' '*/obj/*' \
  -x 'bin/*' 'obj/*' \
  -x 'release/*' 'Release/*' \
  -x '*.binlog' \
  -x 'Secrets/*' \
  -x 'Resources/Prototypes/CorvaxSecrets/*' \
  -x 'Resources/Prototypes/CorvaxSecretsServer/*' \
  -x 'Resources/Textures/CorvaxSecrets/*' \
  -x 'Resources/Audio/CorvaxSecrets/*' \
  -x '*/.vs/*' '*/.idea/*' '*/.DS_Store'

SIZE=$(du -h "$OUT" | cut -f1)
echo "Готово: $OUT ($SIZE)"
echo "Передай этот zip мапперу вместе с инструкцией ниже."
