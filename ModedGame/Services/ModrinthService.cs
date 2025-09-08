// Service for interacting with Modrinth API
using ModedGame.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;


namespace ModedGame.Services;
public class ModrinthService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.modrinth.com/v2";

    public ModrinthService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ModedGame/1.0");
    }

    // Search for mods/projects
    public async Task<ModrinthSearchResponse> SearchProjectsAsync(
        string query = "",
        string projectType = "mod",
        string categories = "",
        string versions = "",
        string loaders = "",
        int limit = 20,
        int offset = 0,
        string sortBy = "relevance")
    {
        try
        {
            var facets = new List<string>();

            // Add project type filter
            if (!string.IsNullOrEmpty(projectType))
                facets.Add($"[\"project_type:{projectType}\"]");

            // Add categories filter
            if (!string.IsNullOrEmpty(categories))
                facets.Add($"[\"categories:{categories}\"]");

            // Add version filter
            if (!string.IsNullOrEmpty(versions))
                facets.Add($"[\"versions:{versions}\"]");

            // Add loader filter (fabric, forge, etc.)
            if (!string.IsNullOrEmpty(loaders))
                facets.Add($"[\"categories:{loaders}\"]");

            var facetsParam = facets.Count > 0 ? $"&facets=[{string.Join(",", facets)}]" : "";

            var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}" +
                     $"&limit={limit}&offset={offset}&index={sortBy}" +
                     facetsParam;

            var response = await _httpClient.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<ModrinthSearchResponse>(response, options);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to search projects: {ex.Message}", ex);
        }
    }

    // Get project versions
    public async Task<List<ModrinthVersion>> GetProjectVersionsAsync(string projectId,
        string gameVersions = "", string loaders = "")
    {
        try
        {
            var url = $"{BaseUrl}/project/{projectId}/version";

            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(gameVersions))
                queryParams.Add($"game_versions=[\"{gameVersions}\"]");
            if (!string.IsNullOrEmpty(loaders))
                queryParams.Add($"loaders=[\"{loaders}\"]");

            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<ModrinthVersion>>(response, options);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get project versions: {ex.Message}", ex);
        }
    }

    // Download a mod file
    public async Task<bool> DownloadModAsync(ModrinthFile file, string downloadPath,
        IProgress<int> progress = null)
    {
        try
        {
            using var response = await _httpClient.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var totalBytesRead = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            var filePath = Path.Combine(downloadPath, file.Filename);

            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                if (progress != null && totalBytes > 0)
                {
                    var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                    progress.Report(progressPercentage);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download mod: {ex.Message}", ex);
        }
    }

    // Get popular mods with pagination
    public async Task<ModrinthSearchResponse> GetPopularModsAsync(int limit = 20, int offset = 0)
    {
        return await SearchProjectsAsync("", "mod", "", "", "", limit, offset, "downloads");
    }

    // Get mods by category with pagination
    public async Task<ModrinthSearchResponse> GetModsByCategoryAsync(string category, int limit = 20, int offset = 0)
    {
        return await SearchProjectsAsync("", "mod", category, "", "", limit, offset, "downloads");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}