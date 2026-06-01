using Content.Server._Wega.Duel.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class DuelScoreResetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    public string Command => "duelscorereset";
    public string Description => "Обнуляет накопленный счёт побед на всех дуэльных аренах.";
    public string Help => "Usage: duelscorereset";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError("Invalid arguments. Usage: duelscorereset");
            return;
        }

        var cleared = _sysMan.GetEntitySystem<DuelArenaSystem>().ResetAllScores();
        shell.WriteLine($"Счёт обнулён на аренах: {cleared}.");
    }
}
