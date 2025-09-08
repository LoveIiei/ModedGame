using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ModedGame.Models
{
    public class ModrinthSearchResponse
    {
        [JsonPropertyName("hits")]
        public List<ModrinthProject> Hits { get; set; } = new List<ModrinthProject>();

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total_hits")]
        public int TotalHits { get; set; }
    }

    public class ModrinthProject
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("project_type")]
        public string ProjectType { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new List<string>();

        [JsonPropertyName("versions")]
        public List<string> Versions { get; set; } = new List<string>();

        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }

        [JsonPropertyName("follows")]
        public int Follows { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("date_modified")]
        public DateTime DateModified { get; set; }

        [JsonPropertyName("latest_version")]
        public string LatestVersion { get; set; }

        [JsonPropertyName("license")]
        public string License { get; set; }

        [JsonPropertyName("gallery")]
        public List<string> Gallery { get; set; } = new List<string>();
    }

    public class ModrinthVersion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("author_id")]
        public string AuthorId { get; set; }

        [JsonPropertyName("featured")]
        public bool Featured { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version_number")]
        public string VersionNumber { get; set; }

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; }

        [JsonPropertyName("date_published")]
        public DateTime DatePublished { get; set; }

        [JsonPropertyName("downloads")]
        public int Downloads { get; set; }

        [JsonPropertyName("version_type")]
        public string VersionType { get; set; }

        [JsonPropertyName("files")]
        public List<ModrinthFile> Files { get; set; } = new List<ModrinthFile>();

        [JsonPropertyName("dependencies")]
        public List<ModrinthDependency> Dependencies { get; set; } = new List<ModrinthDependency>();

        [JsonPropertyName("game_versions")]
        public List<string> GameVersions { get; set; } = new List<string>();

        [JsonPropertyName("loaders")]
        public List<string> Loaders { get; set; } = new List<string>();
    }

    public class ModrinthFile
    {
        [JsonPropertyName("hashes")]
        public Dictionary<string, string> Hashes { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }
    }

    public class ModrinthDependency
    {
        [JsonPropertyName("version_id")]
        public string VersionId { get; set; }

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("dependency_type")]
        public string DependencyType { get; set; }
    }
}
