using System.Data;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Host)]
    sealed class ForceMapCommand : IConsoleCommand
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        // SS220 additional command log
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public string Command => "forcemap";
        public string Description => Loc.GetString("forcemap-command-description");
        public string Help => Loc.GetString("forcemap-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("forcemap-command-need-one-argument"));
                return;
            }

            var gameMap = IoCManager.Resolve<IGameMapManager>();
            var name = args[0];

            // An empty string clears the forced map
            if (!string.IsNullOrEmpty(name) && !gameMap.CheckMapExists(name))
            {
                shell.WriteLine(Loc.GetString("forcemap-command-map-not-found", ("map", name)));
                return;
            }

            _configurationManager.SetCVar(CCVars.GameMap, name);

            if (string.IsNullOrEmpty(name))
                shell.WriteLine(Loc.GetString("forcemap-command-cleared"));
            else
                shell.WriteLine(Loc.GetString("forcemap-command-success", ("map", name)));

            // SS220 admin action log
            LogAdminAction(shell, name);
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = IoCManager.Resolve<IPrototypeManager>()
                    .EnumeratePrototypes<GameMapPrototype>()
                    .Select(p => new CompletionOption(p.ID, p.MapName))
                    .OrderBy(p => p.Value);

                return CompletionResult.FromHintOptions(options, Loc.GetString("forcemap-command-arg-map"));
            }

            return CompletionResult.Empty;
        }

        //SS220 admin action log start
        private void LogAdminAction(IConsoleShell shell, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (shell.Player is { } player)
                {
                    _adminLogger.Add(LogType.AdminCommand, $"{player} cleared the forced map setting");
                }
                else
                {
                    _adminLogger.Add(LogType.EventStarted, $"Unknown cleared the forced map setting");
                }
            }
            else
            {
                if (shell.Player is { } player)
                {
                    _adminLogger.Add(LogType.AdminCommand, $"{player} forced the game to start with map [{name}] next round");
                }
                else
                {
                    _adminLogger.Add(LogType.EventStarted, $"Unknown forced the game to start with map [{name}] next round");
                }
            }
        }
        //SS220 admin action log end
    }
}
