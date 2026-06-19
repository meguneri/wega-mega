namespace Content.Shared._Wega.Duel;

/// <summary>
/// Что арена-гарпун делает с притянутой вплотную жертвой в момент «прилёта».
/// </summary>
public enum ArenaHarpoonFinisher : byte
{
    /// <summary>Только стан — обычный гарпун без добивания.</summary>
    None,

    /// <summary>Отрывает одну случайную конечность.</summary>
    Dismember,

    /// <summary>Сносит голову. Если на жертве шлем — вместо головы срывает шлем, и голова уцелела.</summary>
    Behead,
}
