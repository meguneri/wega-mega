# Full Arsenal Crate — Список предметов и цены

Ящик `CrateSyndicateFullArsenal`. Бюджет: **40 TC**. Контекст: **арена, дуэль 1 на 1**.

Лимиты категорий за одно открытие:
- Броня (`FullArsenalArmor`): **макс. 2**
- Шлемы/противогазы (`FullArsenalHead`): **макс. 2**
- Гранаты (`FullArsenalGrenade`): **макс. 3**
- Патроны (`FullArsenalAmmo`): **макс. 4**
- Пустые сумки/рюкзаки/вещмешки (`FullArsenalBag`): **макс. 1** (готовые комплекты-вещмешки с оружием/скафандрами сюда не входят)

Броня и шлемы зафиксированы (2 / 2) во всех четырёх ящиках — с размером ящика они не растут.
Увеличенные варианты (тот же пул, отдельные текстуры) масштабируют только гранаты/патроны:
- `CrateSyndicateFullArsenalPlus` (аплинк **60 TC**) — бюджет до 60 TC; броня 2 / шлемы 2 / гранаты 5 / патроны 6.
- `CrateSyndicateFullArsenalMega` (аплинк **120 TC**) — бюджет до 120 TC; броня 2 / шлемы 2 / гранаты 9 / патроны 12.

⚠️ В любой ящик влезает **не более 30 предметов** (`EntityStorage.Capacity`). Генерация набора (`SurplusBundle.MaxItems = 30`) останавливается на 30 предметах, даже если бюджет не израсходован — поэтому большие ящики не всегда тратят весь лимит ТК, но и не сыплют лишнее на пол.

Цены калиброваны по аплинку: предметы из аплинка = цена аплинка; предметы из бандлей ≈ цена бандля минус ~2 TC за упаковку.

---

## Ближний бой

| Предмет | Entity | Цена |
|---|---|---|
| Бейсбольная бита | `BaseBallBat` | 1 TC |
| Копьё | `Spear` | 1 TC |
| Костяное копьё | `SpearBone` | 1 TC |
| Усиленное копьё | `SpearReinforced` | 2 TC |
| Плазменное копьё | `SpearPlasma` | 3 TC |
| Урановое копьё | `SpearUranium` | 3 TC |
| Копьё из зуба акулы-мальки | `SpearSharkMinnow` | 3 TC |
| Глефа-дробилка | `WeaponCrusherGlaive` | 5 TC |
| Магмитовая глефа-дробилка | `WeaponMagmiteCrusherGlaive` | 6 TC |
| Мачете | `Machete` | 1 TC |
| Пожарный топор | `FireAxe` | 1 TC |
| Пылающий пожарный топор | `FireAxeFlaming` | 4 TC |
| Багет | `WeaponBaguette` | 2 TC |
| Боевой нож | `CombatKnife` | 1 TC |
| Катласс | `Cutlass` | 2 TC |
| Энергетический кинжал | `EnergyDagger` | 2 TC |
| Катана | `Katana` | 3 TC |
| Энергокатана ниндзя | `EnergyKatana` | 9 TC |
| Сабля капитана (с ножнами) | `ClothingBeltSheathFilled` | 3 TC |
| Проклятая катана | `WeaponCursedKatana` | 4 TC |
| Клеймор | `Claymore` | 4 TC |
| Нулевой жезл (с ножнами) | `ClothingBeltSheathChaplinFilled` | 2 TC |
| Силовой меч | `WeaponForceSword` | 5 TC |
| Дробилка | `WeaponCrusher` | 5 TC |
| Цепной меч | `WeaponChainsword` | 6 TC |
| Сталь Хандзо | `WeaponHanzoSteel` | 6 TC |
| Клинок смерти | `WeaponDeathBlade` | 7 TC |
| Высокочастотный клинок | `WeaponHighFrequencyBlade` | 7 TC |
| Энергетический меч | `EnergySword` | 8 TC |
| Двойной энергетический меч | `EnergySwordDouble` | 16 TC |
| Рапира Синдиката (с ножнами) | `ClothingBeltSheathSyndicateFilled` | 8 TC |

---

## Пистолеты и револьверы

| Предмет | Entity | Цена |
|---|---|---|
| Флинтлок | `WeaponPistolFlintlock` | 2 TC |
| Пистолет Mk58 | `WeaponPistolMk58` | 2 TC |
| Пистолет Viper | `WeaponPistolViper` | 2 TC |
| Синий лазерный пистолет | `WeaponBlueLaserPistol` | 3 TC |
| Пистолет N1984 | `WeaponPistolN1984` | 8 TC |
| Пистолет Cobra | `WeaponPistolCobra` | 3 TC |
| Антикварный лазер | `WeaponAntiqueLaser` | 3 TC |
| Лазерный пистолет (HoS) | `WeaponLaserGun` | 4 TC |
| Пистолет CHIMP | `WeaponPistolCHIMP` | 4 TC |
| Пульсар | `WeaponEnergyPulsar` | 4 TC |
| Револьвер Python | `WeaponRevolverPython` | 4 TC |
| Инспектор | `WeaponRevolverInspector` | 3 TC |
| Револьвер Mateba | `WeaponRevolverMateba` | 4 TC |
| Улучшенный лазер | `WeaponAdvancedLaser` | 5 TC |
| Энергетический арбалет | `WeaponEnergyCrossbow` | 5 TC |
| Револьвер Python AP | `WeaponRevolverPythonAP` | 6 TC |
| Revolving Mateba AP | `WeaponRevolverMatebaAP` | 8 TC |
| Desert Eagle | `WeaponPistolDesertEagle` | 4 TC |
| Desert Eagle AP | `WeaponPistolDesertEagleAP` | 6 TC |
| Dominator (три режима) | `WeaponDominator` | 8 TC |
| Импульсный пистолет | `WeaponPulsePistol` | 20 TC |

---

## Пистолеты-пулемёты

| Предмет | Entity | Цена |
|---|---|---|
| ПП Дрозд | `WeaponSubMachineGunDrozd` | 4 TC |
| ПП Donksoft | `WeaponSubMachineGunDonksoft` | 1 TC |
| ПП WT550 | `WeaponSubMachineGunWt550` | 5 TC |
| ПП Berkut | `WeaponSubmachinegunBerkut` | 6 TC |
| ПП C-20r | `WeaponSubMachineGunC20r` | 10 TC |

---

## Дробовики

| Предмет | Entity | Цена |
|---|---|---|
| Самодельный дробовик | `WeaponShotgunHandmade` | 1 TC |
| Двустволка | `WeaponShotgunDoubleBarreled` | 2 TC |
| Двустволка (резина) | `WeaponShotgunDoubleBarreledRubber` | 2 TC |
| Kammerer | `WeaponShotgunKammerer` | 3 TC |
| Enforcer (резина) | `WeaponShotgunEnforcerRubber` | 4 TC |
| Enforcer | `WeaponShotgunEnforcer` | 5 TC |
| Hushpup | `WeaponShotgunHushpup` | 5 TC |
| Bulldog | `WeaponShotgunBulldog` | 9 TC |

---

## Винтовки, снайперки, пулемёты

| Предмет | Entity | Цена |
|---|---|---|
| Снайперка Мосин | `WeaponSniperMosin` | 1 TC |
| Импровизированный лазер | `WeaponMakeshiftLaser` | 2 TC |
| Сигнальный пистолет | `WeaponFlareGun` | 2 TC |
| Сигнальный пистолет (охрана) | `WeaponFlareGunSecurity` | 3 TC |
| Лазерный карабин | `WeaponLaserCarbine` | 3 TC |
| Сойка | `WeaponRifleJay` | 10 TC |
| Винтовка AK | `WeaponRifleAk` | 6 TC |
| Ионный карабин | `WeaponIonCarabine` | 5 TC |
| Винтовка Lecter | `WeaponRifleLecter` | 5 TC |
| Снайперка Estoc | `WeaponRifleEstoc` | 5 TC |
| Лазер Svalinn | `WeaponLaserSvalinn` | 6 TC |
| Снайперка Hristov | `WeaponSniperHristov` | 8 TC |
| Пулемёт L6C (компакт) | `WeaponLightMachineGunL6C` | 10 TC |
| Снайперка Hristov МК2 | `WeaponSniperHristovAdvanced` | 12 TC |
| Пулемёт L6 | `WeaponLightMachineGunL6` | 12 TC |
| Импульсный карабин | `WeaponPulseCarbine` | 20 TC |
| Импульсный дробовик | `WeaponPulseShotgun` | 35 TC |
| Импульсная снайперская винтовка | `WeaponPulseSniper` | 35 TC |

---

## Тяжёлое и специальное оружие

| Предмет | Entity | Цена |
|---|---|---|
| Крюк-кошка | `WeaponGrapplingGun` | 3 TC |
| Температурный пистолет | `WeaponTemperatureGun` | 3 TC |
| Пушка Форс | `WeaponForceGun` | 5 TC |
| Замедлитель частиц | `WeaponParticleDecelerator` | 5 TC |
| Рентгеновская пушка | `WeaponXrayCannon` | 8 TC |
| Миниган | `WeaponMinigun` | 12 TC |
| ORG430 (гранатомёт) | `WeaponORG430` | 18 TC |

---

## Станнеры и дестабилизаторы

| Предмет | Entity | Цена |
|---|---|---|
| Электродубинка | `Stunbaton` | 2 TC |
| Дубинка | `Truncheon` | 2 TC |
| Телескопическая дубинка | `Telescopicbaton` | 3 TC |
| Стан-прод | `Stunprod` | 3 TC |
| Дубинка офицера Блюшилд | `StunbatonBlueshield` | 4 TC |
| Тазер | `WeaponTaser` | 4 TC |
| Дизаблер | `WeaponDisabler` | 3 TC |
| Супер-тазер | `WeaponTaserSuper` | 10 TC |
| Дизаблер-ПП | `WeaponDisablerSMG` | 5 TC |
| Кастет с оглушением | `ClothingHandsKnuckleDustersStun` | 5 TC |

---

## Щиты

| Предмет | Entity | Цена |
|---|---|---|
| Противоударный щит | `RiotShield` | 2 TC |
| Противолазерный щит | `RiotLaserShield` | 4 TC |
| Пулезащитный щит | `RiotBulletShield` | 4 TC |
| Телескопический щит | `TelescopicShield` | 5 TC |
| Тактический жилет Web (щит) | `ClothingOuterVestWeb` | 6 TC |
| Энергетический щит | `EnergyShield` | 8 TC |
| Элитный тактический жилет | `ClothingOuterVestWebElite` | 8 TC |
| Зеркальный щит | `MirrorShield` | 9 TC |
| Щит Синего щита | `BlueSheildSheild` | 9 TC |
| Деревянный баклер | `WoodenBuckler` | 1 TC |
| Картонный щит | `CardShield` | 1 TC |
| Самодельный щит | `MakeshiftShield` | 1 TC |
| Паутинный щит | `WebShield` | 1 TC |
| Часовой щит | `ClockworkShield` | 3 TC |
| Сломанный энергощит | `BrokenEnergyShield` | 1 TC |
| Сломанный щит СЩ | `BrokenBlueSheildSheild` | 1 TC |

---

## Броня (лимит 2 шт. за открытие)

### Обычная броня и одежда с бронёй

| Предмет | Entity | Цена |
|---|---|---|
| Базовая броня | `ClothingOuterArmorBasic` | 2 TC |
| Мусорная броня | `ClothingOuterArmorScrap` | 4 TC |
| Противоударная броня | `ClothingOuterArmorRiot` | 3 TC |
| Отражающая броня | `ClothingOuterArmorReflective` | 4 TC |
| Пулестойкая броня | `ClothingOuterArmorBulletproof` | 5 TC |
| Культистская броня | `ClothingOuterArmorCult` | 5 TC |
| Броня смотрителя | `ClothingOuterArmorWarden` | 6 TC |
| Рейдовая броня | `ClothingOuterArmorRaid` | 6 TC |
| Тяжёлая броня | `ClothingOuterArmorHeavy` | 18 TC |
| Тяжёлая броня (зелёная) | `ClothingOuterArmorHeavyGreen` | 18 TC |
| Тяжёлая броня (красная) | `ClothingOuterArmorHeavyRed` | 18 TC |
| Хитиновый панцирь | `ClothingOuterArmorChangeling` | 4 TC |
| Костяная броня | `ClothingOuterArmorBone` | 12 TC |
| Броня Pod Wars | `ClothingOuterArmorPodWars` | 8 TC |
| Жилет детектива | `ClothingOuterVestDetective` | 2 TC |
| Тренч детектива | `ClothingOuterCoatDetective` | 3 TC |
| Тренч детектива (стандартный) | `ClothingOuterCoatDetectiveLoadout` | 3 TC |
| Тренч детектива (нуар) | `ClothingOuterCoatDetectiveLoadoutGrey` | 3 TC |
| Тренч детектива (тёмный) | `ClothingOuterCoatDetectiveDark` | 3 TC |
| Тренч ГСБ | `ClothingOuterCoatHoSTrench` | 4 TC |
| Зимняя куртка ГСБ | `ClothingOuterWinterHoS` | 4 TC |
| Бронированное пальто ГСБ | `ClothingOuterCoatHoSGreatcoat` | 5 TC |
| Торжественная куртка капитана | `ClothingOuterCoatCaptain` | 4 TC |
| Куртка главы персонала | `ClothingOuterCoatHOP` | 2 TC |
| Бронированное пальто синдиката | `ClothingOuterCoatSyndieCapArmored` | 4 TC |
| Зимнее бронепальто синдиката | `ClothingOuterWinterSyndieCapArmored` | 4 TC |
| Зимнее пальто Синий Щит | `ClothingOuterWinterBlueShield` | 4 TC |
| Зимнее бронепальто Синий Щит | `ClothingOuterWinterBlueShieldAlt` | 4 TC |
| Куртка смотрителя | `ClothingOuterCoatWarden` | 6 TC |
| Куртка смотрителя (тёмно-синяя) | `ClothingOuterCoatWardenAlt` | 6 TC |
| Зимняя бронекуртка пилота | `ClothingOuterWinterPilot` | 3 TC |
| Шинель службы безопасности | `ClothingOuterCoatSecurityOvercoat` | 3 TC |
| Тактический жилет | `ClothingOuterVestWeb` | 6 TC |
| Элитный тактический жилет | `ClothingOuterVestWebElite` | 8 TC |
| Тактический жилет наёмника | `ClothingOuterVestWebMerc` | 3 TC |
| Тренч | `ClothingOuterCoatTrench` | 1 TC |
| Доги | `ClothingOuterDogi` | 2 TC |
| Бронированный медицинский халат | `ClothingOuterCoatAMG` | 2 TC |
| Одеяние флаггелянта (не под лимитом брони) | `ClothingOuterFlagellantRobe` | 2 TC |

### Скафандры

| Предмет | Entity | Цена |
|---|---|---|
| EVA | `ClothingOuterHardsuitEVA` | 3 TC |
| Инженерный | `ClothingOuterHardsuitEngineering` | 4 TC |
| Синдикат (базовый) | `ClothingOuterHardsuitSyndie` | 7 TC |
| ERT Уборщик | `ClothingOuterHardsuitERTJanitor` | 5 TC |
| Инженерный (белый) | `ClothingOuterHardsuitEngineeringWhite` | 6 TC |
| Атмосферный | `ClothingOuterHardsuitAtmos` | 6 TC |
| Пиратский капитан | `ClothingOuterHardsuitPirateCap` | 8 TC |
| Медицинский | `ClothingOuterHardsuitMedical` | 8 TC |
| Охранника | `ClothingOuterHardsuitSecurity` | 8 TC |
| Охранника (красный) | `ClothingOuterHardsuitSecurityRed` | 8 TC |
| Надзирателя | `ClothingOuterHardsuitWarden` | 8 TC |
| Бригмедик | `ClothingOuterHardsuitBrigmedic` | 8 TC |
| Пустотный парамедик | `ClothingOuterHardsuitVoidParamed` | 8 TC |
| Древний EVA | `ClothingOuterHardsuitAncientEVA` | 8 TC |
| Синдикат Медик | `ClothingOuterHardsuitSyndieMedic` | 7 TC |
| Преобразователь кровавого-красного скафандра | `HardsuitOperSelector` | 7 TC |
| Преобразователь медицинского кровавого-красного скафандра | `HardsuitOperMedSelector` | 7 TC |
| Синдикат Элита | `ClothingOuterHardsuitSyndieElite` | 12 TC |
| Джаггернаут | `ClothingOuterHardsuitJuggernaut` | 10 TC |
| Пилот охраны | `ClothingOuterHardsuitSecurityPilot` | 10 TC |
| НД | `ClothingOuterHardsuitRd` | 10 TC |
| Максим | `ClothingOuterHardsuitMaxim` | 10 TC |
| Волшебник | `ClothingOuterHardsuitWizard` | 10 TC |
| ERT Инженер | `ClothingOuterHardsuitERTEngineer` | 10 TC |
| ERT Медик | `ClothingOuterHardsuitERTMedical` | 10 TC |
| ERT Капеллан | `ClothingOuterHardsuitERTChaplain` | 10 TC |
| Капитанский | `ClothingOuterHardsuitCap` | 12 TC |
| ERT Охрана | `ClothingOuterHardsuitERTSecurity` | 12 TC |
| ERT Лидер | `ClothingOuterHardsuitERTLeader` | 15 TC |
| Синдикат Командир | `ClothingOuterHardsuitSyndieCommander` | 15 TC |
| CBURN | `ClothingOuterHardsuitCBURN` | 15 TC |
| CBURN Лидер | `ClothingOuterHardsuitCBURNLeader` | 18 TC |
| Отряд смерти | `ClothingOuterHardsuitDeathsquad` | 20 TC |

### МОД-скафандры (предсобранные)

| Предмет | Entity | Цена |
|---|---|---|
| МОД Стандартный | `ClothingModularControllerStandardPreassembled` | 3 TC |
| МОД Гражданский | `ClothingModularControllerCivilianPreassembled` | 3 TC |
| МОД Космохонк | `ClothingModularControllerCosmohonkPreassembled` | 3 TC |
| МОД Погрузочный | `ClothingModularControllerLoaderPreassembled` | 4 TC |
| МОД Прототип | `ClothingModularControllerPrototypePreassembled` | 4 TC |
| МОД Шахтёрский | `ClothingModularControllerMiningPreassembled` | 4 TC |
| МОД Атмосферный | `ClothingModularControllerAtmosphericPreassembled` | 6 TC |
| МОД Астероидный | `ClothingModularControllerAsteroidPreassembled` | 6 TC |
| МОД Научный | `ClothingModularControllerResearchPreassembled` | 6 TC |
| МОД Синдикат | `ClothingModularControllerSyndicatePreassembled` | 7 TC |
| МОД Инженерный | `ClothingModularControllerEngineeringPreassembled` | 8 TC |
| МОД Продвинутый | `ClothingModularControllerAdvancedPreassembled` | 8 TC |
| МОД Магнатский | `ClothingModularControllerMagnatePreassembled` | 8 TC |
| МОД Охрана | `ClothingModularControllerSecurityPreassembled` | 10 TC |
| МОД Медицинский | `ClothingModularControllerMedicalPreassembled` | 10 TC |
| МОД Спасательный | `ClothingModularControllerRescuePreassembled` | 11 TC |
| МОД Элита | `ClothingModularControllerElitePreassembled` | 12 TC |
| МОД Страж | `ClothingModularControllerSafeguardPreassembled` | 12 TC |
| МОД Ответный (ОБР) | `ClothingModularControllerResponsoryPreassembled` | 12 TC |
| МОД Инквизиторский | `ClothingModularControllerInquisitoryPreassembled` | 12 TC |
| МОД Корпоративный | `ClothingModularControllerCorporatePreassembled` | 14 TC |
| МОД Апокрифический | `ClothingModularControllerApocryphalPreassembled` | 35 TC |

---

## Медицина

| Предмет | Entity | Цена |
|---|---|---|
| Пластырь от ушибов | `Brutepack` | 1 TC |
| Мазь | `Ointment` | 1 TC |
| Медицинские швы | `MedicatedSuture` | 1 TC |
| Бинт | `Gauze` | 1 TC |
| Пакет крови | `Bloodpack` | 1 TC |
| Регенеративная сетка | `RegenerativeMesh` | 2 TC |
| Аптечка (ушибы) | `MedkitBruteFilled` | 2 TC |
| Аптечка (ожоги) | `MedkitBurnFilled` | 2 TC |
| Аптечка (яды) | `MedkitToxinFilled` | 2 TC |
| Аптечка (кислород) | `MedkitOxygenFilled` | 2 TC |
| Аптечка (радиация) | `MedkitRadiationFilled` | 2 TC |
| Боевая аптечка | `MedkitCombatFilled` | 5 TC |
| Продвинутая аптечка | `MedkitAdvancedFilled` | 5 TC |
| Синдикатский дефибриллятор | `DefibrillatorSyndicate` | 12 TC |
| Кремовый банановый пирог | `FoodPieBananaCream` | 5 TC |

---

## Утилита

| Предмет | Entity | Цена |
|---|---|---|
| Нескользящие ботинки | `ClothingShoesChameleonNoSlips` | 2 TC |
| Наручники | `Handcuffs` | 1 TC |
| Фонарик | `FlashlightSeclite` | 1 TC |
| Дымовая граната | `SmokeGrenade` | 1 TC |
| Флэш | `Flash` | 1 TC |
| Омега-мыло | `SoapOmega` | 2 TC |
| Одноразовая баллистическая турель | `ToolboxElectricalTurretFilled` | 4 TC |

---

## Гранаты (лимит 3 за открытие)

| Предмет | Entity | Цена |
|---|---|---|
| Светошумовая | `GrenadeFlashBang` | 1 TC |
| Слезоточивый газ | `TearGasGrenade` | 1 TC |
| ЭМИ | `EmpGrenade` | 2 TC |
| Дубинка-граната | `GrenadeBaton` | 2 TC |
| Клинада | `GrenadeCleanade` | 1 TC |
| Осколочная | `ExGrenade` | 3 TC |
| Шрапнельная | `GrenadeShrapnel` | 3 TC |
| Зажигательная | `GrenadeIncendiary` | 3 TC |
| Кассетная светошумовая | `ClusterBang` | 3 TC |
| Святая ручная граната | `HolyHandGrenade` | 3 TC |
| Кассетная | `ClusterGrenade` | 4 TC |
| Кассетная светошумовая (полная) | `ClusterBangFull` | 4 TC |
| Мини-бомба синдиката | `SyndieMiniBomb` | 4 TC |
| Хитрая бомба синдиката | `SyndieTrickyBomb` | 5 TC |

---

## Импланты

| Предмет | Entity | Цена |
|---|---|---|
| Имплант свободы | `FreedomImplanter` | 2 TC |
| Имплант SCRAM | `ScramImplanter` | 2 TC |
| ЭМИ-имплант | `EmpImplanter` | 2 TC |
| Микробомба | `MicroBombImplanter` | 4 TC |
| Хранилище (имплант) | `StorageImplanter` | 6 TC |

---

## Гаджеты и инструменты

| Предмет | Entity | Цена |
|---|---|---|
| Синдикатские челюсти жизни | `SyndicateJawsOfLife` | 2 TC |
| Стелс-коробка | `StealthBox` | 5 TC |
| Гипоручка (коробка) | `HypopenBox` | 6 TC |
| Взрывающаяся ручка (коробка) | `PenExplodingBox` | 1 TC |
| Гипо-дарт (коробка) | `HypoDartBox` | 1 TC |
| Гипоспрей | `Hypospray` | 2 TC |
| Горлекс-гипоспрей | `SyndiHypo` | 2 TC |
| Гипоручка | `Hypopen` | 2 TC |
| Ручка Киберсан | `CyberPen` | 1 TC |
| Стимпак (гиперзин, полный) | `Stimpack` | 4 TC |
| Микроинъектор гиперзина | `StimpackMini` | 1 TC |
| Вех-автошприц | `WehMedipen` | 1 TC |
| Настоящий молочный коктейль (30 ед.) | `DrinkTrueMilkshakeFilled` | 5 TC |

---

## Ящики с патронами (по 1 TC каждый)

| Предмет | Entity |
|---|---|
| Пистолетные патроны | `MagazineBoxPistol` |
| Лёгкие винтовочные патроны | `MagazineBoxLightRifle` |
| Винтовочные патроны | `MagazineBoxRifle` |
| Магнум патроны | `MagazineBoxMagnum` |
| Безгильзовые патроны | `MagazineBoxCaselessRifle` |
| Противоматериальные патроны | `MagazineBoxAntiMateriel` |
| Дробовые патроны (стандарт) | `MagazineBoxShotgun` |

---

## Спецпатроны (по 2 TC каждый)

| Предмет | Entity |
|---|---|
| Магазин пистолетный зажигательный | `MagazinePistolIncendiary` |
| Магазин пистолетный урановый | `MagazinePistolUranium` |
| Магазин ПП зажигательный | `MagazinePistolSubMachineGunIncendiary` |
| Магазин ПП урановый | `MagazinePistolSubMachineGunUranium` |
| Магазин винтовочный зажигательный | `MagazineRifleIncendiary` |
| Магазин винтовочный урановый | `MagazineRifleUranium` |
| Магазин лёгкой винтовки зажигательный | `MagazineLightRifleIncendiary` |
| Магазин лёгкой винтовки урановый | `MagazineLightRifleUranium` |
| Обойма магнум бронебойная | `SpeedLoaderMagnumAP` |
| Обойма магнум зажигательная | `SpeedLoaderMagnumIncendiary` |
| Обойма магнум урановая | `SpeedLoaderMagnumUranium` |
| Магазин Desert Eagle ББ | `MagazineEagleAP` |
| Магазин дробовика (пули) | `MagazineShotgunSlug` |
| Магазин дробовика (резина) | `MagazineShotgunBeanbag` |
| Магазин дробовика (зажигательный) | `MagazineShotgunIncendiary` |

---

## Оружейные бандли (чемоданы)

| Предмет | Entity | Цена |
|---|---|---|
| Снайперский набор | `BriefcaseSyndieSniperBundleFilled` | 6 TC |
| Чемодан-автомат (C-20K) | `WeaponSubMachineGunBriefcase` | 10 TC |
| Набор лоббиста | `BriefcaseSyndieLobbyingBundleFilled` | 2 TC |

---

## Оружейные бандли (вещмешки)

| Предмет | Entity | Цена |
|---|---|---|
| Вещмешок винтовка | `ClothingBackpackDuffelSyndicateFilledRifle` | 8 TC |
| Вещмешок револьвер | `ClothingBackpackDuffelSyndicateFilledRevolverStandard` | 8 TC |
| Вещмешок патроны | `ClothingBackpackDuffelSyndicateAmmoFilled` | 10 TC |
| Вещмешок Hushpup | `ClothingBackpackDuffelSyndicateFilledHushpup` | 10 TC |
| Вещмешок ПП (SMG) | `ClothingBackpackDuffelSyndicateFilledSMG` | 13 TC |
| Вещмешок карабин | `ClothingBackpackDuffelSyndicateFilledCarbine` | 15 TC |
| Вещмешок Hristov МК2 | `ClothingBackpackDuffelSyndicateFilledHristov` | 18 TC |
| Вещмешок XC67 | `ClothingBackpackDuffelSyndicateFilledXC67` | 19 TC |
| Вещмешок дробовик | `ClothingBackpackDuffelSyndicateFilledShotgun` | 20 TC |
| Вещмешок медицина | `ClothingBackpackDuffelSyndicateMedicalBundleFilled` | 24 TC |
| Вещмешок гранатомёт | `ClothingBackpackDuffelSyndicateFilledGrenadeLauncher` | 25 TC |
| Вещмешок пулемёт LMG | `ClothingBackpackDuffelSyndicateFilledLMG` | 30 TC |
| Вещмешок стартовый набор | `ClothingBackpackDuffelSyndicateFilledStarterKit` | 40 TC |
| Вещмешок C-4 | `ClothingBackpackDuffelSyndicateC4tBundle` | 8 TC |
| Вещмешок скафандр Синдиката | `ClothingBackpackDuffelSyndicateHardsuitBundle` | 8 TC |
| Вещмешок элитный скафандр | `ClothingBackpackDuffelSyndicateEliteHardsuitBundle` | 13 TC |
| Армейская РПС | `ClothingBeltMilitaryWebbing` | 2 TC |
| РПС шахтёра | `ClothingBeltSalvageWebbing` | 2 TC |
| РПС охраны | `ClothingBeltSecurityWebbing` | 2 TC |
| Вещмешок костюм клоуна | `ClothingBackpackDuffelSyndicateCostumeClown` | 1 TC |

---

## Пустые сумки и рюкзаки (лимит 1 за открытие — `FullArsenalBag`, все по 1 TC)

| Предмет | Entity | Цена |
|---|---|---|
| Рюкзак Синдиката | `ClothingBackpackSyndicate` | 1 TC |
| Вещмешок Синдиката | `ClothingBackpackDuffelSyndicate` | 1 TC |
| Рюкзак охраны | `ClothingBackpackSecurity` | 1 TC |
| Рюкзак наёмника | `ClothingBackpackMerc` | 1 TC |
| Рюкзак | `ClothingBackpack` | 1 TC |
| Кожаная сумка | `ClothingBackpackSatchelLeather` | 1 TC |
| Сумка | `ClothingBackpackSatchel` | 1 TC |
| Вещмешок | `ClothingBackpackDuffel` | 1 TC |

Не считаются «сумкой» (отдельный слот, под лимит не идут):
- Кожаная поясная сумка | `ClothingBeltStorageWaistbag` | 1 TC — слот пояса.
- Скороходы (с заряженной малой батареей) | `ClothingShoesBootsSpeedFilled` | 4 TC — **только в FullArsenal-ящиках**, не в мелейном. Батарею можно вынуть, на ходу разряжается.

---

## Шлемы (лимит 1 за открытие)

| Предмет | Entity | Цена |
|---|---|---|
| Бронированная рабочая каска | `ClothingHeadHatHardhatArmored` | 2 TC |
| Базовый шлем | `ClothingHeadHelmetBasic` | 1 TC |
| Мусорный шлем | `ClothingHeadHelmetScrap` | 1 TC |
| Противоударный шлем | `ClothingHeadHelmetRiot` | 2 TC |
| Флэш-шлем | `ClothingHeadHelmetFlash` | 2 TC |
| Шлем наёмника | `ClothingHeadHelmetMerc` | 2 TC |
| Костяной шлем | `ClothingHeadHelmetBone` | 2 TC |
| Шлем ERT Уборщика | `ClothingHeadHelmetERTJanitor` | 2 TC |
| Культистский шлем | `ClothingHeadHelmetCult` | 3 TC |
| Шлем синдиката | `ClothingHeadHelmetSyndicate` | 3 TC |
| Шлем SWAT | `ClothingHeadHelmetSwat` | 3 TC |
| Шлем SWAT Синдиката | `ClothingHeadHelmetSwatSyndicate` | 3 TC |
| Рейдовый шлем | `ClothingHeadHelmetRaid` | 3 TC |
| Шлем Pod Wars | `ClothingHeadHelmetPodWars` | 3 TC |
| Шлем медика охраны | `ClothingHeadHelmetSecurityMedic` | 3 TC |
| Шлем Громовой арены | `ClothingHeadHelmetThunderdome` | 3 TC |
| Шлем пустотного парамедика | `ClothingHeadHelmetVoidParamed` | 4 TC |
| Шлем тамплиера | `ClothingHeadHelmetTemplar` | 4 TC |
| Шлем Космического ниндзя | `ClothingHeadHelmetSpaceNinja` | 4 TC |
| Шлем ERT Инженера | `ClothingHeadHelmetERTEngineer` | 4 TC |
| Шлем ERT Медика | `ClothingHeadHelmetERTMedic` | 4 TC |
| Шлем ERT Охраны | `ClothingHeadHelmetERTSecurity` | 4 TC |
| Древний шлем | `ClothingHeadHelmetAncient` | 5 TC |
| Шлем ERT Лидера | `ClothingHeadHelmetERTLeader` | 5 TC |

---

## Противогазы (лимит 1 за открытие, общий с шлемами)

| Предмет | Entity | Цена |
|---|---|---|
| Противогаз | `ClothingMaskGas` | 1 TC |
| Противогаз атмосферника | `ClothingMaskGasAtmos` | 2 TC |
| Противогаз охраны | `ClothingMaskGasSecurity` | 2 TC |
| Противогаз наёмника | `ClothingMaskGasMerc` | 2 TC |
| Противогаз SWAT | `ClothingMaskGasSwat` | 2 TC |
| Противогаз синдиката | `ClothingMaskGasSyndicate` | 2 TC |
| Противогаз ERT | `ClothingMaskGasERT` | 3 TC |
| Противогаз Отряда смерти | `ClothingMaskGasDeathSquad` | 4 TC |

---

## Безделушки

| Предмет | Entity | Цена |
|---|---|---|
| Арбузная шляпа | `ClothingHeadHatWatermelon` | 1 TC |
| Синдикатское мыло | `SoapSyndie` | 1 TC |
| Набор курильщика Интердайн | `InterdyneSmokerKit` | 2 TC |
| Синдикатский плеер | `SyndiePlayerInstrument` | 1 TC |
| Синдикатский кошелёк | `PouchSyndie` | 1 TC |
| Ящик инструментов Синдиката | `ToolboxSyndicateFilled` | 2 TC |
| Сегвей Синдиката (ящик) | `CrateFunSyndicateSegway` | 2 TC |
| Воздушный шарик Синдиката | `BalloonSyn` | 10 TC |
| Кроличьи ушки | `ClothingHeadHatBunny` | 5 TC |
| Собачьи ушки | `ClothingHeadHatDogEars` | 10 TC |
| Кошачьи ушки | `ClothingHeadHatCatEars` | 26 TC |
| Элегантное платье горничной | `ClothingUniformJumpskirtElegantMaid` | 30 TC |
| Кубик судьбы | `DiceOfFate` | 10 TC |
| Кость войны (боевой d20) | `DiceOfWar` | 10 TC |
| Пистолет-пугач | `RevolverCapGun` | 10 TC |
| Пистолет-пугач (фальшивый) | `RevolverCapGunFake` | 10 TC |
| Игрушечный световой меч | `ToySword` | 8 TC |

---

## Книги заклинаний

| Предмет | Entity | Цена |
|---|---|---|
| Призыв существ | `SpawnSpellbook` | 10 TC |
| Нокаут (Knock) | `KnockSpellbook` | 10 TC |
| Самовозгорание | `FireSelfSpellbook` | 10 TC |
| Мигание (Blink) | `BlinkBook` | 10 TC |
| Стена силы | `ForceWallSpellbook` | 10 TC |
| Руны | `ScrollRunes` | 10 TC |
| Огненный шар | `FireballSpellbook` | 10 TC |
| Гримуар волшебника | `WizardsGrimoireNoRefund` | 30 TC |
