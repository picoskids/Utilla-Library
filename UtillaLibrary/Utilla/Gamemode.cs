using GorillaGameModes;
using GorillaLibrary.Models;
using System;

namespace Utilla.Models
{
    [Obsolete]
    public enum BaseGamemode
    {
        None,
        Infection,
        Casual,
        Hunt,
        Paintbrawl
    }

    public class Gamemode
    {
        public string DisplayName { get; }

        public string ID { get; }

        public GameModeType? BaseGamemode { get; }

        public Type GameManager { get; }

        public Gamemode(string id, string displayName, GameModeType? game_mode_type = null)
        {
            BaseGamemode = game_mode_type;

            ID = game_mode_type.HasValue && !id.Contains(game_mode_type.Value.ToString()) ? string.Concat(id, game_mode_type) : id;
            DisplayName = displayName;
        }

        public Gamemode(string id, string displayName, Type gameManager)
        {
            ID = id;
            DisplayName = displayName;
            GameManager = gameManager;
        }

        internal Gamemode(GameModeWrapper wrapper)
        {
            ID = wrapper.ID;
            DisplayName = wrapper.DisplayName;
            BaseGamemode = wrapper.BaseGameMode;
            GameManager = wrapper.GameManager;
        }
    }
}
