using Content.Server._Wega.Duel.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Управление «штормом» (сужающейся зоной) на дуэльных аренах из консоли:
/// <c>arenastorm off</c> — отменить сужение на всех аренах; <c>arenastorm on</c> — (пере)запустить
/// на всех аренах, где идёт бой.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed partial class ArenaStormCommand : IConsoleCommand
{
    [Dependency] private IEntitySystemManager _sysMan = default!;

    public string Command => "arenastorm";
    public string Description => Loc.GetString("cmd-arenastorm-desc");
    public string Help => Loc.GetString("cmd-arenastorm-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-arenastorm-invalid-args", ("command", Command)));
            return;
        }

        var storm = _sysMan.GetEntitySystem<ArenaStormSystem>();
        switch (args[0].ToLowerInvariant())
        {
            case "off":
                shell.WriteLine(Loc.GetString("cmd-arenastorm-off-result", ("count", storm.CancelAllStorms())));
                break;
            case "on":
                shell.WriteLine(Loc.GetString("cmd-arenastorm-on-result", ("count", storm.StartAllStorms())));
                break;
            default:
                shell.WriteError(Loc.GetString("cmd-arenastorm-invalid-args", ("command", Command)));
                break;
        }
    }
}
