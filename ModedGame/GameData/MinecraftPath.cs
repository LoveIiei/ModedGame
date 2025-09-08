using System.IO;

namespace ModedGame.GameData;
public static class MinecraftPaths
{
    public static string MinecraftRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ".minecraft"
    );

    public static string ModsFolder => Path.Combine(MinecraftRoot, "mods");
    public static string ResourcePacksFolder => Path.Combine(MinecraftRoot, "resourcepacks");
    public static string ShadersFolder => Path.Combine(MinecraftRoot, "shaderpacks");
    public static string SavesFolder => Path.Combine(MinecraftRoot, "saves");
    public static string VersionsFolder => Path.Combine(MinecraftRoot, "versions");
    public static string ScreenshotsFolder => Path.Combine(MinecraftRoot, "screenshots");

    public static bool MinecraftExists => Directory.Exists(MinecraftRoot);
}