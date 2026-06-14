# CLAUDE.md

Guidance for working in this repository (a fork of ss14-wega / Space Station 14).

## Dependency injection convention (RA0049 / RA0051)

The RobustToolbox analyzer enforces how `[Dependency]` fields must be declared.
Every time `RobustToolbox` is updated from upstream, custom code that doesn't
follow this convention fails the build. Always write dependency-injected types
this way:

- The class **must be `partial`** (RA0049).
- `[Dependency]` fields **must not be `readonly`** (RA0051).

```csharp
public sealed partial class MySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;   // no readonly
}
```

This applies to `EntitySystem`s, `IConsoleCommand`s, `Overlay`s, and any other
type that uses `[Dependency]` fields. Upstream already follows this standard, so
keeping our `_Wega` / `_Starlight` / `_RMC14` / `_Sunrise` code aligned avoids
recurring errors after every upstream sync.

## Full Arsenal / arena price list

`FULL_ARSENAL_PRICES.md` is the human-readable price list for the Full Arsenal
arena crates (`CrateSyndicateFullArsenal*`). It must stay in sync with the
`FullArsenal` listings in
`Resources/Prototypes/_Wega/Catalog/full_arsenal_pool.yml`.

**Always update `FULL_ARSENAL_PRICES.md` whenever you touch the arena / Full
Arsenal pool** — adding, removing, or repricing any `FullArsenal` listing. Put
each item in the matching category table with its display name, entity id, and
TC cost.

`MELEE_ARSENAL_PRICES.md` is the same kind of price list for the Melee Arsenal
crate, kept in sync with the `MeleeArsenal` listings in
`Resources/Prototypes/_Wega/Catalog/melee_arsenal_pool.yml`.

**Always update `MELEE_ARSENAL_PRICES.md` whenever you touch the Melee Arsenal
pool**, the same way as the Full Arsenal list. Any new melee weapon, shield, or
armor added to the Full Arsenal pool must also be added to the Melee Arsenal
pool (melee/armor items belong in both crates) and to both price lists.

## Full Arsenal items must be translated (ru-RU)

Every item available in the Full Arsenal pool
(`Resources/Prototypes/_Wega/Catalog/full_arsenal_pool.yml`) **must have a
Russian name and description** — both the listing (`full-arsenal-*-name` /
`-desc` keys) and the product entity itself (`ent-<EntityId>` in a `ru-RU`
`.ftl`). When you add or port a weapon, bundle, or any other `productEntity`
into the pool, add its `ru-RU` locale in the same change. No Full Arsenal entry
should ever display an English name or description in game. Ported weapons keep
their model designation (e.g. `АС-12 «Минотавр»`, `АШ-12`), but the name and
description still get a `ru-RU` entry so nothing falls back to English.
