using GorillaGameModes;
using System;
using Utilla.Models;

namespace Utilla.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModdedGamemodeAttribute : Attribute
    {
        public readonly Gamemode gamemode;

        public ModdedGamemodeAttribute()
        {
            gamemode = null;
        }

        public ModdedGamemodeAttribute(string id, string displayName, GameModeType gameModeType)
        {
            gamemode = new Gamemode(id, displayName, gameModeType);
        }

        [Obsolete]
        public ModdedGamemodeAttribute(string id, string displayName, BaseGamemode baseGamemode)
        {
            gamemode = new Gamemode(id, displayName, baseGamemode == BaseGamemode.None || !Enum.TryParse(baseGamemode.ToString(), true, out GameModeType gameModeType) ? null : gameModeType);
        }

        public ModdedGamemodeAttribute(string id, string displayName, Type gameManager)
        {
            gamemode = new Gamemode(id, displayName, gameManager);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ModdedGamemodeJoinAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ModdedGamemodeLeaveAttribute : Attribute
    {
    }
}
