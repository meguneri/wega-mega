# Катаны (порт из arcane-station / Lavaland)
ent-ArenaBaseKatana = базовая катана
    .desc = { "" }
ent-ArenaCryoKatana = крио-катана
    .desc = Кажется, что-то пошло не так, когда снежный голем гравировал этот клинок. Замораживает цель при ударе.
ent-ArenaLavaKatana = лавовая катана
    .desc = Нож в тысячу градусов, который каким-то чудом остаётся целым. Раскаляет цель при ударе.
ent-ArenaShadowKatana = теневая катана
    .desc = Легендарный меч, источающий саму тьму. С достаточной удачей и решимостью пробивает любую броню.

# Кинетическое оружие (порт из arcane-station / Lavaland)
ent-ArenaKineticDagger = кинетический кинжал
    .desc = Уменьшенная версия протокинетической дробилки. Кинетическая энергия заставляет лезвие вибрировать на высокой скорости.
ent-ArenaKineticClaws = кинетические когти
    .desc = Выпустите внутреннего эджлорда с этим одноручным когтем, который помещается в рюкзак.
ent-ArenaKineticMachete = кинетическое мачете
    .desc = Малая одноручная версия дробилки: позволяет бить с дистанции.

# Лук жёсткого света (порт из arcane-station / Goobstation)
ent-ArenaBowHardlight = лук жёсткого света
    .desc = Разработка Donk Co. на основе DT-12 «Законник»: этот лук генерирует стрелы разных типов под нужды стрелка. Тип стрелы переключается сменой режима огня.
ent-ArenaBaseHardlightArrow = стрела жёсткого света
    .desc = { "" }
ent-ArenaBaseHardlightEmbeddableArrow = стрела жёсткого света
    .desc = { "" }
ent-ArenaArrowEnergy = энергетическая стрела
    .desc = Стрела из жёсткого света.
ent-ArenaArrowDisabler = стрела-дизаблер
    .desc = Стрела из жёсткого света. Оглушает цель нелетально.
ent-ArenaArrowFiery = огненная стрела
    .desc = Стрела из жёсткого света. Поджигает цель.
ent-ArenaArrowFreeze = морозная стрела
    .desc = Стрела из жёсткого света. Замораживает цель.
ent-ArenaArrowExplosive = взрывная стрела
    .desc = Стрела из жёсткого света. Взрывается при попадании, но быстро рассеивается — далеко не улетит.
ent-ArenaArrowXray = рентгеновская стрела
    .desc = Стрела из жёсткого света. Пробивает всё насквозь.
ent-ArenaArrowIon = ионная стрела
    .desc = Стрела из жёсткого света. Вызывает ЭМИ при попадании.

# Кубик лучника и его залоченные на один режим варианты лука
ent-ArenaBowLuckDie = кубик лучника
    .desc = Гадальный семигранный кубик Donk Co. Брось его — и судьба выберет, каким лучом жёсткого света тебе сегодня воевать. От 1 до 7 — все типы стрел.
ent-ArenaBowHardlightDisabler = { ent-ArenaBowHardlight }
    .desc = { ent-ArenaBowHardlight.desc }
ent-ArenaBowHardlightFiery = { ent-ArenaBowHardlight }
    .desc = { ent-ArenaBowHardlight.desc }
ent-ArenaBowHardlightFreeze = { ent-ArenaBowHardlight }
    .desc = { ent-ArenaBowHardlight.desc }
ent-ArenaBowHardlightExplosive = { ent-ArenaBowHardlight }
    .desc = { ent-ArenaBowHardlight.desc }
ent-ArenaBowHardlightXray = { ent-ArenaBowHardlight }
    .desc = { ent-ArenaBowHardlight.desc }
ent-ArenaBowHardlightIon = { ent-ArenaBowHardlight }
    .desc = { ent-ArenaBowHardlight.desc }
ent-ArenaBowHardlightEnergy = { ent-ArenaBowHardlight }
    .desc = { ent-ArenaBowHardlight.desc }

# Попап при броске кубика лучника
arena-bow-die-rolled = Выпало: { $roll }!
