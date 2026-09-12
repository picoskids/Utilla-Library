using BepInEx;
using GorillaGameModes;
using GorillaLibrary.Models;
using BepInExPluginInfo = BepInEx.PluginInfo;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GorillaLibrary;

internal static class CompatBridge
{
    internal static Action<bool, string> RoomJoined;

    internal static Action<bool, string> RoomLeft;
}

internal static class AssemblyRedirect
{
    private static readonly string[] ShimmedAssemblies = ["Utilla", "GorillaLibrary"];

    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;

        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name;

        return Array.IndexOf(ShimmedAssemblies, name) >= 0 ? typeof(AssemblyRedirect).Assembly : null;
    }
}

internal static class IncompatibilityFilter
{
    private static readonly HashSet<string> BridgedGuids =
    [
        "org.legoandmars.gorillatag.utilla",
        "dev.gorillalibrary"
    ];

    private static bool applied;

    internal static void Apply(Harmony harmony)
    {
        if (applied) return;
        applied = true;

        try
        {
            harmony.Patch(
                AccessTools.PropertyGetter(typeof(BepInExPluginInfo), nameof(BepInExPluginInfo.Incompatibilities)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(IncompatibilityFilter), nameof(Filter))));
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("Could not hook the incompatibility check, so mods that block Utilla or GorillaLibrary will still refuse to load");
            Plugin.Logger.LogWarning(ex);
        }
    }

    private static void Filter(ref IEnumerable<BepInIncompatibility> __result)
    {
        if (__result is null) return;

        __result = __result.Where(incompatibility => !BridgedGuids.Contains(incompatibility.IncompatibilityGUID)).ToArray();
    }
}

public static class CompatibilityPatcher
{
    private const string IncompatibilityAttribute = "BepInEx.BepInIncompatibility";

    private static readonly HashSet<string> BridgedGuids = new(StringComparer.Ordinal)
    {
        "org.legoandmars.gorillatag.utilla",
        "dev.gorillalibrary"
    };

    public static IEnumerable<string> TargetDLLs
    {
        get
        {
            if (!Directory.Exists(PluginDirectory)) return Array.Empty<string>();

            return Directory.GetFiles(PluginDirectory, "*.dll", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string PluginDirectory =>
        Path.Combine(AppContext.BaseDirectory, "BepInEx", "plugins");

    public static void Patch(AssemblyDefinition assembly)
    {
        int removed = 0;

        foreach (TypeDefinition type in AllTypes(assembly.MainModule.Types))
        {
            for (int index = type.CustomAttributes.Count - 1; index >= 0; index--)
            {
                CustomAttribute attribute = type.CustomAttributes[index];
                if (attribute.AttributeType.FullName != IncompatibilityAttribute ||
                    !TargetsBridgedLibrary(attribute))
                {
                    continue;
                }

                type.CustomAttributes.RemoveAt(index);
                removed++;
            }
        }

        if (removed > 0)
        {
            Console.WriteLine($"Removed {removed} Utilla/GorillaLibrary incompatibility declaration(s) from {assembly.Name.Name}.");
        }
    }

    private static bool TargetsBridgedLibrary(CustomAttribute attribute)
    {
        if (attribute.ConstructorArguments.Count == 0) return false;

        CustomAttributeArgument argument = attribute.ConstructorArguments[0];
        return argument.Value is string guid && BridgedGuids.Contains(guid);
    }

    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (TypeDefinition type in types)
        {
            yield return type;

            foreach (TypeDefinition nested in AllTypes(type.NestedTypes))
            {
                yield return nested;
            }
        }
    }
}

internal static class ModdedGamemodeScanner
{
    private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly HashSet<string> GamemodeAttributeNames =
    [
        "ModdedGamemodeAttribute",
        "UnbannedGamemodeAttribute"
    ];

    private static readonly HashSet<string> JoinAttributeNames =
    [
        "ModdedGamemodeJoinAttribute",
        "UnbannedGamemodeJoinAttribute"
    ];

    private static readonly HashSet<string> LeaveAttributeNames =
    [
        "ModdedGamemodeLeaveAttribute",
        "UnbannedGamemodeLeaveAttribute"
    ];

    private static readonly string[] GamemodeMemberNames = ["gamemode", "Gamemode", "gameMode", "GameMode"];

    private static readonly Dictionary<object, GameModeWrapper> adoptedGamemodes = [];

    internal static bool IsGamemodeAttribute(Attribute attribute) => attribute is not null && GamemodeAttributeNames.Contains(attribute.GetType().Name);

    internal static bool IsJoinAttribute(Type attributeType) => attributeType is not null && JoinAttributeNames.Contains(attributeType.Name);

    internal static bool IsLeaveAttribute(Type attributeType) => attributeType is not null && LeaveAttributeNames.Contains(attributeType.Name);

    internal static bool HasAttribute(MethodInfo method, Func<Type, bool> predicate)
    {
        foreach (object attribute in method.GetCustomAttributes(true))
        {
            if (predicate(attribute.GetType())) return true;
        }

        return false;
    }

    internal static object GetGamemodePayload(Attribute attribute)
    {
        Type type = attribute.GetType();

        foreach (string name in GamemodeMemberNames)
        {
            if (type.GetField(name, MemberFlags) is FieldInfo field) return field.GetValue(attribute);
            if (type.GetProperty(name, MemberFlags) is PropertyInfo property && property.CanRead) return property.GetValue(attribute);
        }

        return null;
    }

    internal static GameModeWrapper Convert(object gamemode)
    {
        if (gamemode is null) return null;
        if (gamemode is GameModeWrapper wrapper) return wrapper;

        if (adoptedGamemodes.TryGetValue(gamemode, out GameModeWrapper adopted)) return adopted;

        Type type = gamemode.GetType();

        string id = Read<string>(gamemode, type, "ID") ?? Read<string>(gamemode, type, "Id");
        string displayName = Read<string>(gamemode, type, "DisplayName");
        Type gameManager = Read<Type>(gamemode, type, "GameManager");

        if (string.IsNullOrEmpty(id))
        {
            Plugin.Logger.LogWarning($"Ignoring gamemode of type {type.FullName}: no ID on it");
            return null;
        }

        displayName ??= id;

        adopted = gameManager is not null
            ? new GameModeWrapper(id, displayName, gameManager)
            : new GameModeWrapper(id, displayName, ReadBaseGamemode(gamemode, type));

        adoptedGamemodes[gamemode] = adopted;

        Plugin.Logger.LogInfo($"Adopted {type.FullName} gamemode \"{displayName}\" as {adopted.ID}");

        return adopted;
    }

    private static GameModeType? ReadBaseGamemode(object gamemode, Type type)
    {
        object value = ReadObject(gamemode, type, "BaseGamemode") ?? ReadObject(gamemode, type, "BaseGameMode");

        if (value is null) return null;
        if (value is GameModeType gameModeType) return gameModeType;

        string name = value.ToString();
        if (string.IsNullOrEmpty(name) || name == "None") return null;

        return Enum.TryParse(name, true, out GameModeType parsed) ? parsed : null;
    }

    private static T Read<T>(object instance, Type type, string name) where T : class => ReadObject(instance, type, name) as T;

    private static object ReadObject(object instance, Type type, string name)
    {
        if (type.GetProperty(name, MemberFlags) is PropertyInfo property && property.CanRead) return property.GetValue(instance);
        if (type.GetField(name, MemberFlags) is FieldInfo field) return field.GetValue(instance);

        return null;
    }
}
