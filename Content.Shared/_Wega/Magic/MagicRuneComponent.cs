namespace Content.Shared._Wega.Magic;

/// <summary>
/// Маркер для магических рун, спавнящихся со свитка рун (ScrollRunes): FlashRune,
/// ExplosionRune, IgniteRune, StunRune и т.п. (все наследуют BaseRune). Используется,
/// чтобы подчищать оставшиеся на карте руны после завершения раунда.
/// </summary>
[RegisterComponent]
public sealed partial class MagicRuneComponent : Component;
