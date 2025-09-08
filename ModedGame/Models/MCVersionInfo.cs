// Add these classes to properly deserialize the version JSON
using System.Text.Json.Serialization;
namespace ModedGame.Models;
public class MCVersionInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    public string InheritsFrom { get; set; }
    public string MainClass { get; set; }
    public McArguments Arguments { get; set; }
    public List<McLibrary> Libraries { get; set; }
    public AssetIndexInfo AssetIndex { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    // Add other properties you might need, like 'releaseTime', 'time', etc.
}

public class McArguments
{
    public List<object> Game { get; set; }
    public List<object> Jvm { get; set; }
}

public class McLibrary
{
    public string Name { get; set; }
    public Dictionary<string, string> Natives { get; set; }

    // This is the critical change: A List of rules, not a single rule object.
    public List<LibraryRule> Rules { get; set; }

    // You might also have a 'downloads' property for libraries from Forge, etc.
    // public LibraryDownloads Downloads { get; set; } 
}

// Renamed for clarity from LibraryRules to LibraryRule (singular)
public class LibraryRule
{
    public string Action { get; set; }

    // The 'Os' property might not be present on every rule, 
    // so it can be null. The '?' makes it optional.
    public OsRule? Os { get; set; }
}

public class OsRule
{
    public string Name { get; set; }
}

public class AssetIndexInfo
{
    public string Id { get; set; }
    public string Url { get; set; }
}
