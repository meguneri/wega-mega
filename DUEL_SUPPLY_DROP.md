# 📦 Дуэльный ящик снаряжения — Supply Drop

Спавнится **в центре арены** каждые **60 секунд**, исчезает через **59 секунд**.  
При появлении: **колокол** слышен всем на станции, ящик **светится оранжевым**.

Цены калиброваны по FULL_ARSENAL_PRICES: предметы из аплинка = цена аплинка; остальные — по аналогам.

---

## ⚙️ Механика

Ящик содержит **5 независимых слотов** (orGroup). Каждый слот выдаёт **ровно один** предмет из своего пула — все варианты равновероятны (`prob: 1`).

| Слот | Содержимое | Пустой | Ожидаемая стоимость |
|------|-----------|--------|-------------------|
| **Medicine** | Медицинский предмет | никогда | ~2–3 TC |
| **ChemBottle** | Бутылочка с реагентом | никогда | ~1 TC |
| **Ammo** | Пачка боеприпасов | никогда | ~1–2 TC |
| **Utility** | Граната / зарядник / стимпак | ~17% | ~2 TC |
| **Melee** | Холодное оружие | ~14% | ~1 TC |
| | | **Итого:** | **~7–9 TC** |

---

## 💊 Medicine — Медицина (61 вариант)

### Медипены

| Предмет | Эффект | ТК |
|---------|--------|----|
| `EmergencyMedipen` | Бикаридин + дексалин + эпинефрин | 2 |
| `SurvivalMedipen` | Трикордразин + келотан | 2 |
| `CombatMedipen` | Бикаридин + стимуляторы | 4 |
| `AntiPoisonMedipen` | Диловен + активированный уголь | 2 |
| `SpaceMedipen` | Дексалин+ + гиронолин | 2 |
| `WehMedipen` | Wega-специфичный медипен | 1 |

### Аптечки (заполненные)

| Предмет | Содержимое | ТК |
|---------|-----------|-----|
| `MedkitFilled` | Стандартная аптечка | 1 |
| `MedkitCombatFilled` | Боевая аптечка | 5 |
| `MedkitBruteFilled` | Травмы | 2 |
| `MedkitBurnFilled` | Ожоги | 2 |
| `MedkitAdvancedFilled` | Продвинутая | 5 |
| `MedkitOxygenFilled` | Кислород / асфиксия | 2 |
| `MedkitRadiationFilled` | Радиация | 2 |
| `MedkitToxinFilled` | Токсины | 2 |

### Перевязочные материалы и расходники

| Предмет | Эффект | ТК |
|---------|--------|----|
| `Brutepack` | Лечит физические травмы | 1 |
| `Ointment` | Лечит ожоги | 1 |
| `AloeCream` | Лечит ожоги (растительное) | 1 |
| `Gauze` | Останавливает кровотечение | 1 |
| `MedicatedSuture` | Продвинутые швы | 1 |
| `RegenerativeMesh` | Ускоренное заживление | 2 |
| `Bloodpack` | Восполняет кровь | 1 |

### Шприцы

| Предмет | Реагент | ТК |
|---------|---------|-----|
| `SyringeAmbuzol` | Амбузол | 1 |
| `SyringeBicaridine` | Бикаридин | 1 |
| `SyringeDermaline` | Дермалин | 1 |
| `SyringeEphedrine` | Эфедрин | 1 |
| `SyringeEthylredoxrazine` | Этилредоксразин | 1 |
| `SyringeHyronalin` | Гиронолин | 1 |
| `SyringePhalanximine` | Фаланксимин | 1 |
| `SyringeRomerol` | Ромерол ⚠️ зомбирование | 1 |
| `SyringeSaline` | Физраствор | 1 |
| `SyringeSpaceacillin` | Спейсацилин | 1 |
| `SyringeStimulants` | Стимуляторы | 1 |
| `SyringeTranexamicAcid` | Транексамовая кислота | 1 |

### Контейнеры с таблетками

| Предмет | Реагент | ТК |
|---------|---------|-----|
| `PillCanisterAmbuzol` | Амбузол | 1 |
| `PillCanisterArithrazine` | Аритразин | 1 |
| `PillCanisterBicaridine` | Бикаридин | 1 |
| `PillCanisterBruzin` | Брузин | 1 |
| `PillCanisterCopper` | Медь | 1 |
| `PillCanisterDermaline` | Дермалин | 1 |
| `PillCanisterDylomet` | Дилолмет | 1 |
| `PillCanisterExMedipen` | Расширенный медипен | 2 |
| `PillCanisterFresium` | Фрезий | 1 |
| `PillCanisterHyronalin` | Гиронолин | 1 |
| `PillCanisterIron` | Железо | 1 |
| `PillCanisterKelotane` | Келотан | 1 |
| `PillCanisterLacerinol` | Лацеринол | 1 |
| `PillCanisterLeporazine` | Лепоразин | 1 |
| `PillCanisterMutadon` | Мутадон | 1 |
| `PillCanisterOmnizine` | Омнизин | 2 |
| `PillCanisterPhalanximine` | Фаланксимин | 1 |
| `PillCanisterPotassiumIodide` | Йодид калия | 1 |
| `PillCanisterPuncturase` | Пунктураза | 1 |
| `PillCanisterPyrazine` | Пиразин | 1 |
| `PillCanisterSaline` | Физраствор | 1 |
| `PillCanisterTricordrazine` | Трикордразин | 1 |

---

## 🧪 ChemBottle — Бутылочки с реагентами (30 вариантов)

| Предмет | Реагент | ТК |
|---------|---------|-----|
| `ChemistryBottleBicaridine` | Бикаридин — травмы | 1 |
| `ChemistryBottleBruizine` | Брузин — ушибы и травмы | 1 |
| `ChemistryBottleLacerinol` | Лацеринол — порезы | 1 |
| `ChemistryBottleUltravasculine` | Ультраваскулин — травмы + ожоги | 1 |
| `ChemistryBottleDoxarubixadone` | Доксарубиксадон — универсал | 1 |
| `ChemistryBottleCryoxadone` | Криоксадон — мощное лечение при холоде | 2 |
| `ChemistryBottleOmnizine` | Омнизин — все виды урона | 2 |
| `ChemistryBottleTricordrazine` | Трикордразин — травмы + ожоги | 1 |
| `ChemistryBottleDermaline` | Дермалин — ожоги | 1 |
| `ChemistryBottleKelotane` | Келотан — ожоги | 1 |
| `ChemistryBottleSaline` | Физраствор — кровь | 1 |
| `ChemistryBottleEpinephrine` | Эпинефрин — реанимация из крита | 2 |
| `ChemistryBottleEthylredoxrazine` | Этилредоксразин — выводит алкоголь | 1 |
| `ChemistryBottleLeporazine` | Лепоразин — стабилизирует температуру | 1 |
| `ChemistryBottlePyrazine` | Пиразин — восстановление органов | 1 |
| `ChemistryBottlePuncturase` | Пунктураза — внутренние повреждения | 1 |
| `ChemistryBottlePhalanximine` | Фаланксимин — клеточные повреждения | 1 |
| `ChemistryBottleSiderlac` | Сидерлак — лечебный реагент | 1 |
| `ChemistryBottleBarozine` | Барозин — тушит огонь на теле | 1 |
| `ChemistryBottleEphedrine` | Эфедрин — стимулятор | 1 |
| `ChemistryBottleEthyloxyephedrine` | Этилоксиэфедрин — стимулятор | 1 |
| `ChemistryBottleSynaptizine` | Синаптизин — убирает замедление | 1 |
| `ChemistryBottleTranexamicAcid` | Транексамовая кислота — кровотечение | 1 |

---

## 🔫 Ammo — Боеприпасы (20 вариантов)

| Предмет | Тип | ТК |
|---------|-----|-----|
| `MagazineBoxPistol` | Пистолетные | 1 |
| `MagazineBoxPistolIncendiary` | Пистолетные зажигательные | 2 |
| `MagazineBoxPistolUranium` | Пистолетные урановые | 2 |
| `MagazineBoxMagnum` | Магнум | 1 |
| `MagazineBoxMagnumAP` | Магнум бронебойные | 2 |
| `MagazineBoxMagnumIncendiary` | Магнум зажигательные | 2 |
| `MagazineBoxMagnumUranium` | Магнум урановые | 2 |
| `MagazineBoxRifle` | Винтовочные .20 | 1 |
| `MagazineBoxRifleBig` | Винтовочные .20 (крупная пачка) | 1 |
| `MagazineBoxRifleIncendiary` | Винтовочные зажигательные | 2 |
| `MagazineBoxRifleUranium` | Винтовочные урановые | 2 |
| `MagazineBoxLightRifle` | Лёгкая винтовка .30 | 1 |
| `MagazineBoxLightRifleBig` | Лёгкая винтовка .30 (крупная пачка) | 1 |
| `MagazineBoxLightRifleIncendiary` | Лёгкая винтовка зажигательные | 2 |
| `MagazineBoxLightRifleUranium` | Лёгкая винтовка урановые | 2 |
| `MagazineBoxCaselessRifle` | Безгильзовая винтовка | 1 |
| `MagazineBoxAntiMateriel` | Противоматериальные | 1 |
| `BoxLethalshot` | Дробь летальная | 1 |
| `BoxShotgunSlug` | Пуля для дробовика | 1 |
| `BoxShotgunIncendiary` | Зажигательная картечь | 2 |
| `BoxShotgunUranium` | Урановая картечь | 2 |

---

## 🎒 Utility — Утилита (~83% шанс, 5 вариантов)

| Предмет | Эффект | ТК |
|---------|--------|----|
| `SmokeGrenade` | Дымовая граната | 1 |
| `Flash` | Вспышка — ослепляет | 1 |
| `Stimpack` | Стимпак (гиперзин, полный) | 4 |
| `StimpackMini` | Микроинъектор гиперзина | 1 |
| `PortableRecharger` | Переносной зарядник для энергооружия | 2 |

> ~17% — слот пустой.

---

## 🗡️ Melee — Холодное оружие (~86% шанс, 6 вариантов)

| Предмет | Особенность | ТК |
|---------|-------------|-----|
| `CombatKnife` | Быстрый | 1 |
| `ThrowingKnife` | Метательный | 2 |
| `Machete` | Высокий урон | 1 |
| `Spear` | Колющий, длинная рука | 1 |
| `SurvivalKnife` | Универсальный | 2 |
| `EnergyDagger` | Энергетический, не нужны патроны | 2 |

> ~14% — слот пустой.
