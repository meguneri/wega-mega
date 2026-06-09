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
