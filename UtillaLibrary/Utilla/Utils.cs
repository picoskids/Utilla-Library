using GorillaGameModes;
using GorillaLibrary.Behaviours;
using GorillaLibrary.Models;
using GorillaLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilla.Models;

namespace Utilla.Utils
{
    public static class GameModeUtils
    {
        private static readonly Dictionary<GameModeWrapper, Gamemode> mirroredGamemodes = [];

        public static Gamemode CurrentGamemode => Mirror(GameModeUtility.CurrentGameMode);

        public static Gamemode FindGamemodeInString(string gmString) => Mirror(GameModeUtility.FindGameModeInString(gmString));

        public static Gamemode GetGamemodeFromId(string id) => Mirror(GameModeUtility.FindGameModeFromId(id));

        public static Gamemode GetGamemode(Func<Gamemode, bool> predicate)
        {
            if (!GameModeManager.HasInstance) return null;

            return GameModeManager.Instance.Gamemodes.Select(Mirror).LastOrDefault(predicate);
        }

        public static string GetGameModeName(GameModeType gameModeType) => GameModeUtility.GetGameModeName(gameModeType);

        public static GorillaGameManager GetGameModeInstance(GameModeType gameModeType) => GameModeUtility.GetGameModeInstance(gameModeType);

        public static bool IsSuperGameMode(this GameModeType gameMode) => GameModeUtility.IsSuperGameMode(gameMode);

        internal static Gamemode Mirror(GameModeWrapper wrapper)
        {
            if (wrapper is null) return null;

            if (!mirroredGamemodes.TryGetValue(wrapper, out Gamemode gamemode))
            {
                gamemode = new Gamemode(wrapper);
                mirroredGamemodes[wrapper] = gamemode;
            }

            return gamemode;
        }
    }

    [Obsolete]
    public static class RoomUtils
    {
        public static string RoomCode;
    }
}
