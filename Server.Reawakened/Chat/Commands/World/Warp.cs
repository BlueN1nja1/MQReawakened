using Server.Base.Accounts.Enums;
using Server.Reawakened.Chat.Models;
using Server.Reawakened.Core.Configs;
using Server.Reawakened.Players;
using Server.Reawakened.Rooms.Services;
using Server.Reawakened.XMLs.Bundles.Base;
using Server.Reawakened.XMLs.Data.Commands;
using WorldGraphDefines;

namespace Server.Reawakened.Chat.Commands.World;
public class Warp : SlashCommand
{
    public override string CommandName => "/warp";

    public override string CommandDescription => "Allows you to warp to a new level.";

    public override List<ParameterModel> Parameters =>
    [
        new ParameterModel()
        {
            Name = "inGameLevelName",
            Description = "The in-game level name to warp to.",
            Optional = false
        }
    ];

    public override AccessLevel AccessLevel => AccessLevel.Moderator;

    public WorldGraph WorldGraph { get; set; }
    public ServerRConfig ServerRConfig { get; set; }
    public WorldHandler WorldHandler { get; set; }

    public override void Execute(Player player, string[] args)
    {
        if (args.Length < 2)
        {
            Log($"Please specify a valid level name.", player);
            return;
        }

        var levelInfo = new LevelInfo();
        var levelName = string.Empty;
        foreach (var arg in args.Skip(1))
            levelName += arg;

        foreach (var levelPrefabName in ServerRConfig.LoadedAssets)
        {
            var levelInGameName = WorldGraph.GetInfoLevel(levelPrefabName).InGameName;
            foreach (var stringToRemove in ServerRConfig.RemovedWarpCmdStrings)
            {
                levelInGameName = levelInGameName.Replace(stringToRemove, "", StringComparison.OrdinalIgnoreCase);
                levelName = levelName.Replace(stringToRemove, "", StringComparison.OrdinalIgnoreCase);
            }

            if (levelInGameName.ToLower() == levelName.ToLower())
            {
                levelInfo = WorldGraph.GetInfoLevel(levelPrefabName);
                break;
            }
        }

        if (string.IsNullOrEmpty(levelInfo.Name) || !ServerRConfig.LoadedAssets.Contains(levelInfo.Name))
        {
            Log($"Please specify a valid level.", player);
            return;
        }

        if (!WorldHandler.TryChangePlayerRoom(player, levelInfo.LevelId))
        {
            Log($"Please specify a valid level.", player);
            return;
        }

        Log(
            $"Successfully set character {player.Character.Id}'s level to {levelInfo.LevelId} '{levelInfo.InGameName}' ({levelInfo.Name})",
            player
        );

        Log($"{player.Character.CharacterName} changed to level {levelInfo.LevelId}", player);
    }
}
