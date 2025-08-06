using DT_PODSystem.Data;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using DT_PODSystemWorker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace DT_PODSystemWorker.Services
{
    public interface IFileDiscoveryService
    {
        Task<List<FileProcessInfo>> DiscoverMatchingFilesAsync();
    }

    public class FileDiscoveryService : IFileDiscoveryService
    {
        private readonly ILogger<FileDiscoveryService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly WorkerSettings _settings;

        public FileDiscoveryService(
            ILogger<FileDiscoveryService> logger,
            ApplicationDbContext context,
            IOptions<WorkerSettings> settings)
        {
            _logger = logger;
            _context = context;
            _settings = settings.Value;
        }


        private TemplateMatchResult ValidateAndReturnResult(string fileName, string prefix, List<(string date, string format)> foundDates)
        {
            if (foundDates.Count == 0)
            {
                _logger.LogWarning($"❌ NO DATE found in file '{fileName}' after prefix '{prefix}'. " +
                                  $"Expected formats: yyyy-MM, yyyyMM, yyyy_MM, yyyy.MM, MM-yyyy, etc.");
                return new TemplateMatchResult { IsMatch = false };
            }

            if (foundDates.Count > 1)
            {
                var distinctDates = foundDates.Select(d => d.date).Distinct().ToList();

                if (distinctDates.Count > 1)
                {
                    _logger.LogError($"🚨 MULTIPLE DIFFERENT DATES found in file '{fileName}': " +
                                   $"{string.Join(", ", foundDates.Select(d => $"{d.date} ({d.format})"))}. " +
                                   $"File naming must contain exactly ONE unique date!");
                    return new TemplateMatchResult { IsMatch = false };
                }
                else
                {
                    _logger.LogInformation($"ℹ️ Multiple date patterns found in '{fileName}' but they resolve to the same date: {distinctDates[0]}");
                }
            }

            var finalDate = foundDates.First().date;
            var usedFormat = foundDates.First().format;

            _logger.LogInformation($"✅ Successfully matched file '{fileName}' -> Prefix: '{prefix}', Date: '{finalDate}' (detected as {usedFormat})");

            return new TemplateMatchResult
            {
                IsMatch = true,
                PeriodId = finalDate
            };
        }

        private string NormalizePeriod(string period)
        {
            // Remove all separators and normalize to yyyyMM format
            var cleaned = period.Replace("-", "").Replace("_", "").Replace(".", "").Replace(" ", "");

            // Handle different input formats
            if (cleaned.Length == 6)
            {
                // Could be yyyyMM or MMyyyy
                var firstPart = cleaned.Substring(0, 4);
                var secondPart = cleaned.Substring(4, 2);

                // If first part looks like a year (>= 2000), assume yyyyMM
                if (int.TryParse(firstPart, out var possibleYear) && possibleYear >= 2000)
                {
                    return cleaned; // Already yyyyMM
                }
                // If second part looks like a year, assume MMyyyy -> convert to yyyyMM  
                else if (int.TryParse(secondPart + firstPart.Substring(0, 2), out var possibleYear2) && possibleYear2 >= 2000)
                {
                    return secondPart + firstPart.Substring(0, 2) + firstPart.Substring(2, 2);
                }
            }

            return cleaned;
        }

        private bool IsValidPeriod(string period)
        {
            if (period.Length != 6) return false;

            if (!int.TryParse(period.Substring(0, 4), out var year) ||
                !int.TryParse(period.Substring(4, 2), out var month))
            {
                return false;
            }

            var currentYear = DateTime.Now.Year;
            return year >= 2000 &&
                   year <= currentYear + 2 &&
                   month >= 1 &&
                   month <= 12;
        }

        public async Task<List<FileProcessInfo>> DiscoverMatchingFilesAsync()
        {
            var discoveredFiles = new List<FileProcessInfo>();

            try
            {
                _logger.LogInformation($"🔍 STARTING FILE DISCOVERY");
                _logger.LogInformation($"📂 CONFIGURED ROOT PATH: '{_settings.RootFolderPath}'");
                _logger.LogInformation($"📂 ABSOLUTE ROOT PATH: '{Path.GetFullPath(_settings.RootFolderPath)}'");
                _logger.LogInformation($"📂 DIRECTORY EXISTS: {Directory.Exists(_settings.RootFolderPath)}");

                if (!Directory.Exists(_settings.RootFolderPath))
                {
                    _logger.LogWarning($"❌ Root directory does not exist: {_settings.RootFolderPath}");
                    Directory.CreateDirectory(_settings.RootFolderPath);
                    _logger.LogInformation($"📁 Created root directory: {_settings.RootFolderPath}");
                }

                // Log detailed directory information
                var dirInfo = new DirectoryInfo(_settings.RootFolderPath);
                _logger.LogInformation($"📁 DIRECTORY INFO:");
                _logger.LogInformation($"   - Full Name: '{dirInfo.FullName}'");
                _logger.LogInformation($"   - Exists: {dirInfo.Exists}");
                _logger.LogInformation($"   - Attributes: {dirInfo.Attributes}");

                // Get active templates with detailed logging
                var activeTemplates = await _context.PdfTemplates
                    .Where(t => t.IsActive && t.Status == TemplateStatus.Active)
                    .Include(t => t.Category)
                    .Include(t => t.Vendor)
                    .ToListAsync();

                _logger.LogInformation($"📋 Found {activeTemplates.Count} active templates");

                foreach (var template in activeTemplates)
                {
                    _logger.LogInformation($"📝 Template '{template.Name}' with prefix: '{template.NamingConvention}'");
                }

                if (!activeTemplates.Any())
                {
                    _logger.LogWarning("⚠️ No active templates found - no files will be processed");
                    return discoveredFiles;
                }

                // DETAILED FILE SEARCH LOGGING
                _logger.LogInformation($"🔎 SEARCHING FOR PDF FILES...");
                _logger.LogInformation($"   - Search Path: '{_settings.RootFolderPath}'");
                _logger.LogInformation($"   - Search Pattern: '*.pdf'");
                _logger.LogInformation($"   - Search Option: AllDirectories (recursive)");

                try
                {
                    var pdfFiles = Directory.GetFiles(_settings.RootFolderPath, "*.pdf", SearchOption.AllDirectories);
                    _logger.LogInformation($"📄 FOUND {pdfFiles.Length} PDF FILES");

                    if (pdfFiles.Length == 0)
                    {
                        // Check what files ARE in the directory
                        _logger.LogInformation($"🔍 CHECKING WHAT FILES EXIST IN DIRECTORY:");
                        var allFiles = Directory.GetFiles(_settings.RootFolderPath, "*.*", SearchOption.AllDirectories);
                        _logger.LogInformation($"📁 Total files in directory: {allFiles.Length}");

                        if (allFiles.Length > 0)
                        {
                            _logger.LogInformation($"📋 FILES FOUND (showing first 10):");
                            foreach (var file in allFiles.Take(10))
                            {
                                var fileInfo = new FileInfo(file);
                                _logger.LogInformation($"   - {fileInfo.Name} ({fileInfo.Extension}) - {fileInfo.Length} bytes");
                            }

                            if (allFiles.Length > 10)
                            {
                                _logger.LogInformation($"   ... and {allFiles.Length - 10} more files");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"📁 DIRECTORY IS COMPLETELY EMPTY!");
                        }

                        // Check subdirectories
                        var subdirectories = Directory.GetDirectories(_settings.RootFolderPath, "*", SearchOption.AllDirectories);
                        if (subdirectories.Length > 0)
                        {
                            _logger.LogInformation($"📂 SUBDIRECTORIES FOUND:");
                            foreach (var subdir in subdirectories)
                            {
                                var subdirInfo = new DirectoryInfo(subdir);
                                var subFiles = Directory.GetFiles(subdir, "*.*");
                                _logger.LogInformation($"   - {subdirInfo.Name} ({subFiles.Length} files)");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"📋 PDF FILES FOUND:");
                        foreach (var pdfFile in pdfFiles)
                        {
                            var fileInfo = new FileInfo(pdfFile);
                            _logger.LogInformation($"   - {fileInfo.Name} in '{fileInfo.DirectoryName}'");
                        }
                    }

                    // Process each PDF file
                    foreach (var filePath in pdfFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var fullFileName = Path.GetFileName(filePath);

                        _logger.LogInformation($"🔎 ANALYZING FILE: '{fileName}' (full: '{fullFileName}')");
                        _logger.LogInformation($"   - Full Path: '{filePath}'");

                        bool fileMatched = false;

                        foreach (var template in activeTemplates)
                        {
                            _logger.LogDebug($"🔍 Testing against template '{template.Name}' with prefix '{template.NamingConvention}'");

                            var matchResult = TryMatchTemplate(fileName, template);

                            if (matchResult.IsMatch)
                            {
                                _logger.LogInformation($"✅ MATCH FOUND: '{fileName}' matches template '{template.Name}'");

                                // Check if already processed
                                var alreadyProcessed = await _context.ProcessedFiles
                                    .AnyAsync(pf => pf.TemplateId == template.Id &&
                                                  pf.PeriodId == matchResult.PeriodId &&
                                                  pf.OriginalFileName == fullFileName);

                                if (!alreadyProcessed)
                                {
                                    discoveredFiles.Add(new FileProcessInfo
                                    {
                                        FileName = fullFileName,
                                        FilePath = filePath,
                                        TemplateId = template.Id,
                                        PeriodId = matchResult.PeriodId,
                                        Category = template.Category?.Name ?? "Unknown",
                                        Vendor = template.Vendor?.Name ?? "Unknown"
                                    });

                                    _logger.LogInformation($"🎯 ADDED TO PROCESSING QUEUE: '{fileName}' -> Template: '{template.Name}', Period: {matchResult.PeriodId}");
                                    fileMatched = true;
                                    break; // One template per file
                                }
                                else
                                {
                                    _logger.LogInformation($"⏭️ ALREADY PROCESSED: '{fileName}' (Template: {template.Id}, Period: {matchResult.PeriodId})");
                                    fileMatched = true;
                                    break;
                                }
                            }
                            else
                            {
                                _logger.LogDebug($"❌ No match: '{fileName}' vs prefix '{template.NamingConvention}'");
                            }
                        }

                        if (!fileMatched)
                        {
                            _logger.LogWarning($"❌ NO TEMPLATE MATCH: File '{fileName}' doesn't match any active template prefixes");

                            // Show what prefixes were tested
                            _logger.LogInformation($"📋 Available template prefixes:");
                            foreach (var template in activeTemplates)
                            {
                                _logger.LogInformation($"   - '{template.NamingConvention}' (Template: {template.Name})");
                            }
                        }
                    }
                }
                catch (DirectoryNotFoundException ex)
                {
                    _logger.LogError($"💥 DIRECTORY NOT FOUND: {ex.Message}");
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogError($"🚫 ACCESS DENIED: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"💥 ERROR ACCESSING DIRECTORY: {_settings.RootFolderPath}");
                    throw;
                }

                _logger.LogInformation($"📊 DISCOVERY COMPLETE:");
                _logger.LogInformation($"   - Total PDF files found: {Directory.GetFiles(_settings.RootFolderPath, "*.pdf", SearchOption.AllDirectories).Length}");
                _logger.LogInformation($"   - Files ready for processing: {discoveredFiles.Count}");
                _logger.LogInformation($"   - Active templates available: {activeTemplates.Count}");

                return discoveredFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 FATAL ERROR during file discovery");
                return discoveredFiles;
            }
        }

        private TemplateMatchResult TryMatchTemplate(string fileName, PdfTemplate template)
        {
            try
            {
                // Template.NamingConvention now contains just the prefix
                // Example: "EAJ5017198_INVENTORY_MANAGEMENT_SLA"
                var prefix = template.NamingConvention.Trim();

                _logger.LogDebug($"🔍 Matching file '{fileName}' against prefix '{prefix}'");

                // Check if filename starts with the prefix
                if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug($"❌ File '{fileName}' doesn't start with prefix '{prefix}'");
                    return new TemplateMatchResult { IsMatch = false };
                }

                // Extract the suffix (everything after the prefix)
                var suffix = fileName.Substring(prefix.Length);
                _logger.LogDebug($"📄 Extracted suffix: '{suffix}'");

                // Define comprehensive date patterns to search for in suffix
                var datePatterns = new[]
                {
            // Various separators and formats
            new { Pattern = @"[-_\s](\d{4}[-_\.]\d{2})(?:[-_\s]|$)", Name = "yyyy-MM or yyyy_MM or yyyy.MM" },
            new { Pattern = @"[-_\s](\d{6})(?:[-_\s]|$)", Name = "yyyyMM" },
            new { Pattern = @"[-_\s](\d{4})[-_\s](\d{2})(?:[-_\s]|$)", Name = "yyyy MM separate" },
            new { Pattern = @"[-_\s](\d{2}[-_\.]\d{4})(?:[-_\s]|$)", Name = "MM-yyyy or MM_yyyy" },
            new { Pattern = @"[-_\s](\d{2})[-_\s](\d{4})(?:[-_\s]|$)", Name = "MM yyyy separate" }
        };

                var foundDates = new List<(string date, string format)>();

                // Search for all possible date patterns
                foreach (var datePattern in datePatterns)
                {
                    var regex = new Regex(datePattern.Pattern, RegexOptions.IgnoreCase);
                    var matches = regex.Matches(suffix);

                    foreach (Match match in matches)
                    {
                        string extractedDate;

                        if (match.Groups.Count == 2)
                        {
                            // Single capture group
                            extractedDate = match.Groups[1].Value;
                        }
                        else if (match.Groups.Count == 3)
                        {
                            // Two capture groups (year and month separate)
                            var part1 = match.Groups[1].Value;
                            var part2 = match.Groups[2].Value;

                            // Determine if it's yyyy MM or MM yyyy
                            if (part1.Length == 4)
                                extractedDate = $"{part1}{part2}"; // yyyy MM -> yyyyMM
                            else
                                extractedDate = $"{part2}{part1}"; // MM yyyy -> yyyyMM
                        }
                        else
                        {
                            continue;
                        }

                        var normalizedDate = NormalizePeriod(extractedDate);

                        if (IsValidPeriod(normalizedDate))
                        {
                            foundDates.Add((normalizedDate, datePattern.Name));
                            _logger.LogDebug($"✅ Found valid date '{extractedDate}' -> '{normalizedDate}' using pattern '{datePattern.Name}'");
                        }
                        else
                        {
                            _logger.LogDebug($"⚠️ Found date '{extractedDate}' but it's invalid (normalized: '{normalizedDate}')");
                        }
                    }
                }

                // Validate uniqueness and return result
                return ValidateAndReturnResult(fileName, prefix, foundDates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 Error matching template '{template.Name}' against file '{fileName}'");
                return new TemplateMatchResult { IsMatch = false };
            }
        }

    }
}