using Content.Server._Wega.Duel.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class DuelScoreResetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    public string Command => "duelscorereset";
    public string Description => Loc.GetString("cmd-duelscorereset-desc");
    public string Help => Loc.GetString("cmd-duelscorereset-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("cmd-duelscorereset-invalid-args", ("command", Command)));
            return;
        }

        var cleared = _sysMan.GetEntitySystem<DuelArenaSystem>().ResetAllScores();
        shell.WriteLine(Loc.GetString("cmd-duelscorereset-result", ("count", cleared)));
    }
}
