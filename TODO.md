# TODO — wega-mega

## Катана Рэнгоку (ArenaRengokuKatana)

- [ ] **Переделать анимации эффектов способностей.** Текущие построены на
  `Effects/explosion.rsi` + `Effects/fire.rsi` (см.
  `Resources/Prototypes/_Wega/Entities/Effects/rengoku_katana.yml`) — выглядят
  «так себе». Нужны нормальные кастомные кадры/анимация под огненные приёмы
  (Первая форма — взмах пламени, Девятая форма — рывок + взрыв), а не свод
  существующих ванильных эффектов. Спавнятся уже корректно (на задетых целях,
  `RengokuKatanaSystem`).
- [ ] **Переделать звук удара.** Текущий `Audio/_Wega/Weapons/rengoku_slash.ogg`
  — авто-свод `bladeslice` + `fire` (см. `Tools/gen_rengoku_slash_sound.sh`).
  Хочется нормальный звук пореза+огня, а не миксованный на коленке.
