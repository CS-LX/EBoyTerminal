using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;
using EBoyTerminal.Runtime;

namespace EBoyTerminal.System.Contributors;

/// <summary>当前世界/环境的只读信息（时间、天气、模式等），不提供改世界或改玩家能力。</summary>
public sealed class WorldLuaSystemApiContributor : ILuaSystemApiContributor {
    public string ModuleName => "sys.world";

    public void Contribute(LuaScriptApiBuildContext context, IDictionary<string, DynValue> members) {
        Project project = context.Project;

        AddReader(members, "readWorldName", context, (_, _) => DynValue.NewString(ReadWorldName(project)));
        AddReader(members, "readWorldSeed", context, (_, _) => DynValue.NewNumber(ReadWorldSeed(project)));
        AddReader(members, "readGameMode", context, (_, _) => DynValue.NewString(ReadGameMode(project)));
        AddReader(members, "readElapsedSeconds", context, (_, _) => DynValue.NewNumber(ReadElapsedSeconds(project)));
        AddReader(members, "readDay", context, (_, _) => DynValue.NewNumber(ReadDay(project)));
        AddReader(members, "readTimeOfDay", context, (_, _) => DynValue.NewNumber(ReadTimeOfDay(project)));
        AddReader(members, "readSeason", context, (_, _) => DynValue.NewString(ReadSeason(project)));
        AddReader(members, "readIsPrecipitating", context, (_, _) => DynValue.NewBoolean(ReadIsPrecipitating(project)));
        AddReader(members, "readPrecipitationIntensity", context, (_, _) => DynValue.NewNumber(ReadPrecipitationIntensity(project)));
        AddReader(members, "readTickSeconds", context, (_, _) => DynValue.NewNumber(ReadTickSeconds(project)));
    }

    static void AddReader(
        IDictionary<string, DynValue> members,
        string name,
        LuaScriptApiBuildContext api,
        Func<ScriptExecutionContext, CallbackArguments, DynValue> handler) {
        members[name] = api.Callback(handler);
    }

    static SubsystemGameInfo? FindGameInfo(Project project) =>
        project.FindSubsystem<SubsystemGameInfo>(throwOnError: false);

    static SubsystemTimeOfDay? FindTimeOfDay(Project project) =>
        project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: false);

    static SubsystemSeasons? FindSeasons(Project project) =>
        project.FindSubsystem<SubsystemSeasons>(throwOnError: false);

    static SubsystemWeather? FindWeather(Project project) =>
        project.FindSubsystem<SubsystemWeather>(throwOnError: false);

    static string ReadWorldName(Project project) =>
        FindGameInfo(project)?.WorldSettings.Name ?? string.Empty;

    static double ReadWorldSeed(Project project) =>
        FindGameInfo(project)?.WorldSeed ?? 0d;

    static string ReadGameMode(Project project) =>
        FindGameInfo(project)?.WorldSettings.GameMode.ToString() ?? "Unknown";

    static double ReadElapsedSeconds(Project project) =>
        FindGameInfo(project)?.TotalElapsedGameTime ?? 0d;

    static double ReadDay(Project project) {
        SubsystemTimeOfDay? timeOfDay = FindTimeOfDay(project);
        return timeOfDay != null ? Math.Floor(timeOfDay.Day) : 0d;
    }

    static double ReadTimeOfDay(Project project) =>
        FindTimeOfDay(project)?.TimeOfDay ?? 0d;

    static string ReadSeason(Project project) =>
        FindSeasons(project)?.Season.ToString() ?? "Unknown";

    static bool ReadIsPrecipitating(Project project) =>
        FindWeather(project)?.IsPrecipitationStarted ?? false;

    static double ReadPrecipitationIntensity(Project project) =>
        FindWeather(project)?.PrecipitationIntensity ?? 0d;

    static double ReadTickSeconds(Project project) =>
        FindGameInfo(project)?.TotalElapsedGameTimeDelta ?? 0d;
}
