using Content.Shared.Actions;

namespace Content.Shared._Wega.Magic.Telekinesis;

public sealed partial class TelekinesisGrabSpellEvent : EntityTargetActionEvent { }

public sealed partial class TelekinesisThrowSpellEvent : WorldTargetActionEvent { }

public sealed partial class TelekinesisReleaseSpellEvent : InstantActionEvent { }
