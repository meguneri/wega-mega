#!/usr/bin/env python3
"""Генерирует иконки способностей катаны Рэнгоку.

База — нарисованный icon катаны (rengoku_katana.rsi/icon.png) поверх радиального
огненного свечения. Две иконки:
  * action-first  — мягкое оранжевое свечение (Первая форма)
  * action-ninth  — интенсивное красно-оранжевое зарево (Девятая форма, ульта)
Чистый радиальный градиент, без рисования от руки.

Запуск из корня репозитория:  python3 Tools/gen_rengoku_action_icons.py
"""
import math
import os

from PIL import Image

RSI = os.path.join(os.path.dirname(__file__), "..", "Resources", "Textures",
                   "_Wega", "Objects", "Weapons", "Melee", "rengoku_katana.rsi")
ICON = os.path.join(RSI, "icon.png")
N = 32
C = (N - 1) / 2.0


def glow(inner, outer, radius, intensity):
    """Радиальный градиент inner->outer->прозрачность."""
    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    px = img.load()
    for y in range(N):
        for x in range(N):
            d = math.hypot(x - C, y - C) / radius
            if d >= 1.0:
                continue
            t = d  # 0 в центре, 1 на краю
            r = int(inner[0] * (1 - t) + outer[0] * t)
            g = int(inner[1] * (1 - t) + outer[1] * t)
            b = int(inner[2] * (1 - t) + outer[2] * t)
            a = int(intensity * (1 - t) ** 1.6)
            px[x, y] = (r, g, b, a)
    return img


def build(glow_img, katana, brighten=1.0):
    out = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    out.alpha_composite(glow_img)
    k = katana
    if brighten != 1.0:
        px = k.load()
        k = k.copy()
        kp = k.load()
        for y in range(N):
            for x in range(N):
                r, g, b, a = px[x, y]
                if a == 0:
                    continue
                kp[x, y] = (min(255, int(r * brighten)),
                            min(255, int(g * brighten)),
                            min(255, int(b * brighten)), a)
    out.alpha_composite(k)
    return out


def main():
    if not os.path.exists(ICON):
        raise SystemExit(f"Нет {ICON}")
    katana = Image.open(ICON).convert("RGBA")

    first = build(glow((255, 160, 40), (180, 40, 0), radius=15, intensity=150), katana)
    ninth = build(glow((255, 210, 90), (200, 20, 0), radius=18, intensity=230), katana,
                  brighten=1.15)

    first.save(os.path.join(RSI, "action-first.png"))
    ninth.save(os.path.join(RSI, "action-ninth.png"))
    print("written action-first.png, action-ninth.png")


if __name__ == "__main__":
    main()
