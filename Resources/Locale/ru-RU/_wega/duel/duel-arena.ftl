# Анонс переключения спавнера снабжения (SpawnerSignalControlSystem).
# { $seconds } подставляется из реального интервала спавнера — число не хардкодится.
spawner-signal-control-enabled = Сброс снаряжения активирован. Ящики будут появляться в центре арены каждые { $seconds } секунд.
spawner-signal-control-disabled = Сброс снаряжения остановлен.

# Анонсы дуэльной арены (DuelArenaSystem)

duel-arena-not-started-no-fighters = Дуэль не началась: в зоне нет бойцов.
duel-arena-not-started-need-two = Дуэль не началась: нужно минимум 2 бойца.

duel-arena-started = Дуэль началась! { $fighters }

duel-arena-scores-reset = Счёт дуэльной арены обнулён.

duel-arena-cleaned = Арена очищена: выданное снаряжение убрано.

duel-arena-losers-fallback = противники

duel-arena-concluded-winner = Дуэль завершена! Победитель: { $winner }{ $streak ->
        [0] { "" }
        [1] { "" }
       *[other] { " " }(побед подряд: { $streak })
    }! { $losers } { $loserCount ->
        [one] потерял сознание
       *[other] потеряли сознание
    }. Снаряжение убрано.

duel-arena-concluded-draw = Ничья! { $fighters } потеряли сознание. Снаряжение убрано.

# Общий накопленный счёт арены, дописывается к итогу боя
duel-arena-scoreboard = Общий счёт: { $scores }

# Ready-check кнопки дуэли (DuelArenaSystem.Ready) — старт только когда готовы оба бойца
duel-ready-already-active = Бой уже идёт.
duel-ready-fighter-ready = { $name } готов! ({ $count }/{ $total })
duel-ready-fighter-unready = { $name } отменил готовность. ({ $count }/{ $total })

# Шторм (battle-royale): сужающаяся безопасная зона
arena-storm-incoming = Шторм наступает! Безопасная зона сужается — держитесь центра арены.
arena-storm-cancelled = Шторм отменён: зона больше не сужается.

# Соединители для перечисления имён бойцов
duel-arena-connector-vs = против
duel-arena-connector-and = и

# Админ-команда duelscorereset
cmd-duelscorereset-desc = Обнуляет накопленный счёт побед на всех дуэльных аренах.
cmd-duelscorereset-help = Использование: { $command }
cmd-duelscorereset-invalid-args = Неверные аргументы. Использование: { $command }
cmd-duelscorereset-result = Счёт обнулён на аренах: { $count }.

# Команда arenastorm — управление сужающейся зоной
cmd-arenastorm-desc = Управляет штормом (сужающейся зоной) на дуэльных аренах: on — запустить, off — отменить.
cmd-arenastorm-help = Использование: { $command } <on|off>
cmd-arenastorm-invalid-args = Неверные аргументы. Использование: { $command } <on|off>
cmd-arenastorm-off-result = Шторм отменён на аренах: { $count }.
cmd-arenastorm-on-result = Шторм запущен на аренах: { $count }.
