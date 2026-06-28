using Robust.Shared.GameObjects;

namespace Content.Shared._Wega.Raid.Components;

/// <summary>
/// Авто-перезарядка скава: когда держимый магазинный ствол пустеет, скав сам достаёт совместимый
/// заряженный магазин из карманов/сумки/рук и меняет его (с короткой паузой). Кончились запасные
/// магазины — ствол остаётся пустым, и HTN переходит к ближнему бою. Поведение чисто серверное;
/// боезапас читается через событие GetAmmoCountEvent, без доступа к закрытым компонентам.
/// </summary>
[RegisterComponent]
public sealed partial class RaidSkavAutoReloadComponent : Component
{
    /// <summary>Как часто (сек) проверять пустоту ствола.</summary>
    [DataField]
    public float CheckInterval = 1f;

    /// <summary>Пауза «перезарядки» (сек) после успешной смены магазина.</summary>
    [DataField]
    public float ReloadDelay = 2f;

    /// <summary>Рантайм: не раньше этого времени следующая проверка/перезарядка.</summary>
    [ViewVariables]
    public TimeSpan NextCheck;
}
