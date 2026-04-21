using System.Text.Json;
using MTGDeckAnalyzer.Application.Models;

namespace MTGDeckAnalyzer.Infrastructure.Archidekt;

public partial class ArchidektService
{
    private async Task<List<PreconDeck>?> LoadPreconsFromFileCacheAsync()
    {
        try
        {
            var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), CacheDirectory);
            var indexFile = Path.Combine(cacheDir, CacheIndexFile);

            if (!File.Exists(indexFile))
            {
                _logger.LogInformation("Index file not found: {IndexFile}", indexFile);
                return null;
            }

            // Don't check expiration for now - just load the cached data
            _logger.LogInformation("Loading precons from file cache: {IndexFile}", indexFile);

            var indexContent = await File.ReadAllTextAsync(indexFile);
            var preconFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(indexContent);
            
            if (preconFiles == null)
            {
                _logger.LogWarning("Failed to deserialize index file");
                return null;
            }

            var precons = new List<PreconDeck>();
            
            foreach (var (preconName, fileName) in preconFiles)
            {
                try
                {
                    var filePath = Path.Combine(cacheDir, fileName);
                    if (File.Exists(filePath))
                    {
                        var preconJson = await File.ReadAllTextAsync(filePath);
                        
                        // Use JsonSerializer with custom options for flexibility
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true
                        };
                        
                        // Use the flexible JSON model first
                        var preconJsonModel = JsonSerializer.Deserialize<PreconJsonModel>(preconJson, options);
                        if (preconJsonModel != null)
                        {
                            var precon = preconJsonModel.ToPreconDeck();
                            
                            // Ensure name is set if missing
                            if (string.IsNullOrEmpty(precon.Name))
                            {
                                precon.Name = preconName;
                            }
                            
                            precons.Add(precon);
                            _logger.LogDebug("Loaded precon: {Name} (Year: {Year})", precon.Name, precon.Year);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Precon file not found: {FilePath} for {PreconName}", filePath, preconName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load cached precon file: {FileName} for {PreconName}", fileName, preconName);
                }
            }

            _logger.LogInformation("Loaded {Count} precons from file cache", precons.Count);
            return precons.Count > 0 ? precons : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load precons from file cache");
            return null;
        }
    }

    private async Task SavePreconsToFileCacheAsync(List<PreconDeck> precons)
    {
        try
        {
            var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), CacheDirectory);
            Directory.CreateDirectory(cacheDir);

            var preconFiles = new Dictionary<string, string>();
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            foreach (var precon in precons)
            {
                try
                {
                    var sanitizedName = SanitizeFileName(precon.Name ?? "Unknown");
                    var fileName = $"{sanitizedName}_{precon.Year}.json";
                    var filePath = Path.Combine(cacheDir, fileName);
                    
                    var preconJson = JsonSerializer.Serialize(precon, options);
                    await File.WriteAllTextAsync(filePath, preconJson);
                    
                    preconFiles[precon.Name ?? "Unknown"] = fileName;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save precon to cache: {PreconName}", precon.Name);
                }
            }

            // Save index file
            var indexFile = Path.Combine(cacheDir, CacheIndexFile);
            var indexJson = JsonSerializer.Serialize(preconFiles, options);
            await File.WriteAllTextAsync(indexFile, indexJson);
            
            _logger.LogInformation("Saved {Count} precons to file cache", precons.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save precons to file cache");
        }
    }

    private void ClearFileCache()
    {
        try
        {
            var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), CacheDirectory);
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
                _logger.LogInformation("Cleared file cache directory");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear file cache directory");
        }
    }
}
