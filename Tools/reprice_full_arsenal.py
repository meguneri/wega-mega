#!/usr/bin/env python3
"""Точечная переоценка full_arsenal_pool по listing ID.

Поднимает цены топ-аутлаеров по силе (LMG/миниган/энергооружие/сильный баллист),
чтобы разнести кластер 10-12 TC по тирам. Меняет ТОЛЬКО число Telecrystal у
указанных листингов; остальной файл (комменты, порядок, прочие позиции) не трогает.
"""
import os
import re

POOL = os.path.join(os.path.dirname(__file__), "..",
                    "Resources", "Prototypes", "_Wega", "Catalog", "full_arsenal_pool.yml")

# listing id -> новая цена TC. Анкор: Desert Eagle 8, XC67 15 в аплинке, бюджет 40.
REPRICE = {
    # --- Подавляющий sustained-fire / LMG ---
    "FullArsenalLMGL6":            20,  # 12 -> L6 SAW, топ DPS
    "FullArsenalLMGDP28":          15,  # 11 -> LMG послабее
    # --- Энергооружие (нет экономики патронов) ---
    "FullArsenalEnergyBlackMamba": 38,  # 60 -> изи вин, но слабее DPS чем PulseCarbine
    "FullArsenalPulsePistol":      30,  # 24 -> hitscan изи вин
    "FullArsenalPulseCarbine":     35,  # 28 -> hitscan изи вин
    "FullArsenalPulseShotgun":     46,  # 40 -> ваншот в упор
    "FullArsenalPulseSniper":      42,  # 35 -> 90 урона hitscan
    "FullArsenalTeslaGun":         18,  # 12
    # --- Сильный баллист, вытащенный из кластера 12 ---
    "FullArsenalSingularityHammer":18,  # 12 -> сильное мили
    "FullArsenalShotgunMinotaur":  15,  # 12
    "FullArsenalRifleAsh12":       15,  # 12
    "FullArsenalDuffelAsh12":      20,  # 15 -> дюфель = голая +5 (как Minotaur/P90)
    "FullArsenalRifleBauer127":    15,  # 12 -> снайперка
    "FullArsenalSniperHristovAdvanced": 15,  # 12
    "FullArsenalRifleM52":         13,  # 11 -> rate 8, топ DPS среди винтовок
    "FullArsenalRifleAR2":         11,  # 14 -> слабее G36/FAMAS, Heat-урон
    # --- Иконный сильный SMG вытащен из полосы 10 ---
    "FullArsenalSMGC20r":          15,  # 10 -> C20r
    "FullArsenalSMGAtreides":      18,  # 11 -> одноручная, rate 10
    # --- Сильное мили со станом ---
    "FullArsenalMjollnir":         18,  # 15 -> 25 Blunt + 3 сек стан на удар
    # --- Броня-универсал без слабостей ---
    "FullArsenalArmorCult":         7,  # 5 -> флэт 0.5 на всё, нет дыры как у спец-брони
}


def main():
    path = os.path.abspath(POOL)
    with open(path, encoding="utf-8") as f:
        lines = f.readlines()

    current = None
    changed = []
    already_ok = []
    id_re = re.compile(r"^  id:\s*(\S+)\s*$")
    tc_re = re.compile(r"^(\s*Telecrystal:\s*)(\d+)(\s*)$")

    for i, line in enumerate(lines):
        m = id_re.match(line)
        if m:
            current = m.group(1)
            continue
        if current in REPRICE:
            t = tc_re.match(line)
            if t:
                old = int(t.group(2))
                new = REPRICE[current]
                if old != new:
                    lines[i] = f"{t.group(1)}{new}{t.group(3)}"
                    if not lines[i].endswith("\n"):
                        lines[i] += "\n"
                    changed.append((current, old, new))
                else:
                    already_ok.append((current, old))
                current = None  # цена найдена, ждём следующий листинг

    with open(path, "w", encoding="utf-8") as f:
        f.writelines(lines)

    print(f"изменено {len(changed)} из {len(REPRICE)} запрошенных:")
    for iid, old, new in changed:
        print(f"  {iid:<34} {old:>3} -> {new}")
    if already_ok:
        print(f"уже актуальны ({len(already_ok)}):")
        for iid, price in already_ok:
            print(f"  {iid:<34} = {price}")
    found = {c[0] for c in changed} | {a[0] for a in already_ok}
    missing = set(REPRICE) - found
    if missing:
        print("НЕ НАЙДЕНЫ (проверь id):", missing)


if __name__ == "__main__":
    main()
