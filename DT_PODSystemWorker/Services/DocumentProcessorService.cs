using DT_PODSystemWorker.Models;
using DT_PODSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using DT_PODSystem.Models.Entities;

namespace DT_PODSystemWorker.Services
{
    /// <summary>
    /// ✅ SIMPLIFIED: PDF Field Extraction Service (NO Calculations)
    /// Step 1: Extract mapped fields from PDFs
    /// Step 2: Save extracted data to ProcessedFiles/ProcessedFields
    /// Step 3: Organize processed files
    /// </summary>
    public class DocumentProcessorService : BackgroundService
    {
        private readonly ILogger<DocumentProcessorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkerSettings _settings;
        private readonly string _lockFilePath;

        public DocumentProcessorService(
               ILogger<DocumentProcessorService> logger,
               IServiceProvider serviceProvider,
               IOptions<WorkerSettings> settings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settings = settings.Value;

            // ✅ FIXED: Use RootFolderPath since ProcessingDirectory doesn't exist in WorkerSettings
            _lockFilePath = Path.Combine(_settings.RootFolderPath, "processor.lock");
        }



        /// <summary>
        /// ✅ UPDATED: Process single document with anchor confidence tracking
        /// </summary>
        private async Task ProcessSingleDocument(FileProcessInfo fileInfo)
        {
            var stopwatch = Stopwatch.StartNew();
            ProcessedFile? processedFile = null;

            // Each file gets completely separate scope and services
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pdfExtraction = scope.ServiceProvider.GetRequiredService<IPdfExtractionService>();
            var fileOrganization = scope.ServiceProvider.GetRequiredService<IFileOrganizationService>();

            try
            {
                _logger.LogInformation($"🔄 Processing: {fileInfo.FileName} (Template: {fileInfo.TemplateId}, Period: {fileInfo.PeriodId})");

                // Initialize processed file record
                processedFile = new ProcessedFile
                {
                    TemplateId = fileInfo.TemplateId,
                    PeriodId = fileInfo.PeriodId,
                    OriginalFileName = fileInfo.FileName,
                    OriginalFilePath = fileInfo.FilePath,
                    Status = "Processing",
                    ProcessedDate = DateTime.UtcNow,
                    ProcessingMessage = "Started field extraction"
                };

                context.ProcessedFiles.Add(processedFile);
                await context.SaveChangesAsync(); // Save to get ID

                // ✅ EXTRACT MAPPED FIELDS WITH ANCHOR CALIBRATION
                _logger.LogDebug($"📄 Extracting mapped fields from {fileInfo.FileName}...");
                var extractionResult = await pdfExtraction.ExtractFieldsAsync(fileInfo);

                if (!extractionResult.Success)
                {
                    // 🆕 Save anchor results even on failure
                    await UpdateProcessedFileWithAnchorResults(processedFile, extractionResult, context);

                    await HandleProcessingFailure(processedFile, $"Field extraction failed: {extractionResult.ErrorMessage}",
                        fileInfo, fileOrganization, context);
                    return;
                }

                _logger.LogDebug($"✅ Extracted {extractionResult.ExtractedFields.Count} fields from {fileInfo.FileName}");

                // ✅ Save extracted field data with anchor confidence
                await SaveExtractedFieldsWithAnchors(processedFile.Id, extractionResult, context);

                // 🆕 Update processed file with anchor calibration results
                await UpdateProcessedFileWithAnchorResults(processedFile, extractionResult, context);

                // ✅ Organize file into structured folders
                _logger.LogDebug($"📁 Organizing {fileInfo.FileName}...");
                var organizedPath = await fileOrganization.OrganizeFileAsync(fileInfo, processedFile.Id);

                // ✅ Update processed file record with success
                processedFile.Status = "Success";
                processedFile.ProcessingMessage = $"Successfully extracted {extractionResult.ExtractedFields.Count} fields";
                processedFile.OrganizedFilePath = organizedPath;

                await context.SaveChangesAsync();

                stopwatch.Stop();
                _logger.LogInformation($"✅ Successfully processed {fileInfo.FileName} in {stopwatch.ElapsedMilliseconds}ms");

                // 🆕 Log anchor summary
                if (extractionResult.AnchorResults != null)
                {
                    _logger.LogInformation($"🛡️ [ANCHOR SUMMARY] {extractionResult.AnchorResults.AnchorsMatched}/{extractionResult.AnchorResults.AnchorsTotal} anchors matched ({extractionResult.AnchorResults.Confidence:P1} confidence)");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"💥 Failed to process {fileInfo.FileName} after {stopwatch.ElapsedMilliseconds}ms");

                if (processedFile != null)
                {
                    // 🆕 Set anchor confidence to 0 on error
                    processedFile.AnchorConfidence = 0.0m;
                    processedFile.AnchorDetails = $"Processing failed: {ex.Message}";

                    await HandleProcessingFailure(processedFile, $"Unexpected error: {ex.Message}",
                        fileInfo, fileOrganization, context);
                }
                else
                {
                    // If processedFile wasn't created, still move to error folder
                    await fileOrganization.MoveToErrorFolderAsync(fileInfo, $"Unexpected error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 🆕 NEW: Update ProcessedFile with anchor calibration results
        /// </summary>
        private async Task UpdateProcessedFileWithAnchorResults(
            ProcessedFile processedFile,
            ExtractionResult extractionResult,
            ApplicationDbContext context)
        {
            try
            {
                if (extractionResult.AnchorResults != null)
                {
                    processedFile.AnchorConfidence = extractionResult.AnchorResults.Confidence;
                    processedFile.AnchorsFound = extractionResult.AnchorResults.AnchorsFound;
                    processedFile.AnchorsConfigured = extractionResult.AnchorResults.AnchorsTotal;
                    processedFile.AnchorsMatched = extractionResult.AnchorResults.AnchorsMatched;
                    processedFile.AnchorDetails = extractionResult.AnchorResults.Details;

                    _logger.LogInformation($"🛡️ [ANCHOR RESULTS] File: {processedFile.OriginalFileName}");
                    _logger.LogInformation($"   📊 Confidence: {processedFile.AnchorConfidence:P1}");
                    _logger.LogInformation($"   📍 Anchors: {processedFile.AnchorsMatched}/{processedFile.AnchorsConfigured} matched");
                    _logger.LogInformation($"   🎯 Quality: {GetAnchorQualityStatus(processedFile.AnchorConfidence)}");
                }
                else
                {
                    // No anchor results available
                    processedFile.AnchorConfidence = 1.0m; // Default to perfect if no anchors configured
                    processedFile.AnchorsFound = 0;
                    processedFile.AnchorsConfigured = 0;
                    processedFile.AnchorsMatched = 0;
                    processedFile.AnchorDetails = "No anchors configured for this template";

                    _logger.LogInformation($"📍 [NO ANCHORS] Template has no calibration anchors configured");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating processed file with anchor results");

                // Set safe defaults on error
                processedFile.AnchorConfidence = 0.0m;
                processedFile.AnchorDetails = $"Error processing anchor results: {ex.Message}";
            }
        }

        /// <summary>
        /// ✅ UPDATED: Save extracted field data with anchor awareness
        /// </summary>
        private async Task SaveExtractedFieldsWithAnchors(
            int processedFileId,
            ExtractionResult extractionResult,
            ApplicationDbContext context)
        {
            try
            {
                // Get the ProcessedFile to find the TemplateId
                var processedFile = await context.ProcessedFiles.FindAsync(processedFileId);
                if (processedFile == null)
                {
                    _logger.LogError($"ProcessedFile {processedFileId} not found");
                    return;
                }

                // ✅ Get FieldMappings for this template
                var fieldMappings = await context.FieldMappings
                    .Where(fm => fm.TemplateId == processedFile.TemplateId && fm.IsActive)
                    .ToDictionaryAsync(fm => fm.FieldName, fm => new { fm.Id, fm.DataType });

                var extractedFields = new List<ProcessedField>();

                // 🆕 Calculate field confidence based on anchor confidence
                var baseFieldConfidence = Math.Max(extractionResult.CalibrationConfidence * 0.95m, 0.85m);

                foreach (var extractedField in extractionResult.ExtractedFields)
                {
                    // Find the corresponding FieldMapping by name
                    if (fieldMappings.TryGetValue(extractedField.Key, out var fieldInfo))
                    {
                        var processedField = new ProcessedField
                        {
                            ProcessedFileId = processedFileId,
                            FieldMappingId = fieldInfo.Id,
                            FieldName = extractedField.Key,
                            OutputValue = extractedField.Value?.ToString(),
                            OutputDataType = fieldInfo.DataType.ToString(),
                            IsValid = extractedField.Value != null,
                            // 🆕 Field confidence influenced by anchor confidence
                            ExtractionConfidence = extractedField.Value != null ? baseFieldConfidence : 0.0m,
                            CalculationConfidence = 0, // No calculations done here
                            ValidationErrors = null
                        };

                        extractedFields.Add(processedField);
                        _logger.LogDebug($"✅ Mapped field {extractedField.Key} (FieldMappingId: {fieldInfo.Id}) = {extractedField.Value}");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ No FieldMapping found for extracted field: {extractedField.Key}");
                    }
                }

                if (extractedFields.Any())
                {
                    context.ProcessedFields.AddRange(extractedFields);
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"💾 Saved {extractedFields.Count} extracted fields to database");
                }
                else
                {
                    _logger.LogWarning("No extracted fields to save - no matching FieldMappings found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving extracted fields for processed file {processedFileId}");
                throw; // Re-throw to handle upstream
            }
        }

        /// <summary>
        /// 🆕 NEW: Get anchor quality status for logging
        /// </summary>
        private string GetAnchorQualityStatus(decimal confidence)
        {
            return confidence switch
            {
                >= 0.95m => "Excellent",
                >= 0.85m => "Good",
                >= 0.70m => "Fair",
                _ => "Poor - Requires Review"
            };
        }

        /// <summary>
        /// ✅ UPDATED: Handle processing failures with anchor confidence tracking
        /// </summary>
        private async Task HandleProcessingFailure(
            ProcessedFile processedFile,
            string errorMessage,
            FileProcessInfo fileInfo,
            IFileOrganizationService fileOrganization,
            ApplicationDbContext context)
        {
            try
            {
                processedFile.Status = "Failed";
                processedFile.ProcessingMessage = errorMessage;

                // 🆕 Ensure anchor confidence is set to 0 on failure if not already set
                if (processedFile.AnchorConfidence == 1.0m) // Default value, meaning not set
                {
                    processedFile.AnchorConfidence = 0.0m;
                    processedFile.AnchorDetails = $"Processing failed: {errorMessage}";
                }

                await context.SaveChangesAsync();

                await fileOrganization.MoveToErrorFolderAsync(fileInfo, errorMessage);
                _logger.LogWarning($"❌ Failed to process {fileInfo.FileName}: {errorMessage}");
                _logger.LogWarning($"🛡️ [ANCHOR STATUS] Confidence set to {processedFile.AnchorConfidence:P1}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling processing failure for {fileInfo.FileName}");
            }
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 DocumentProcessorService started - PDF Field Extraction Only");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDocumentBatch(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error in document processing batch");
                }

                await Task.Delay(_settings.ProcessingIntervalMinutes * 60 * 1000, stoppingToken);
            }

            _logger.LogInformation("🛑 DocumentProcessorService stopped");
        }

        private async Task ProcessDocumentBatch(CancellationToken cancellationToken)
        {
            if (await IsAnotherInstanceRunning())
            {
                _logger.LogDebug("🔒 Another instance is running, skipping this cycle");
                return;
            }

            await CreateLockFile();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fileDiscovery = scope.ServiceProvider.GetRequiredService<IFileDiscoveryService>();

                _logger.LogInformation("🔍 Starting document discovery...");
                var files = await fileDiscovery.DiscoverMatchingFilesAsync();

                if (!files.Any())
                {
                    _logger.LogDebug("📭 No files found for processing");
                    return;
                }

                _logger.LogInformation($"📋 Found {files.Count} files to process");

                // Process files one by one to avoid memory issues
                foreach (var fileInfo in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await ProcessSingleDocument(fileInfo);
                }

                _logger.LogInformation($"✅ Completed processing batch of {files.Count} files");
            }
            finally
            {
                await RemoveLockFile();
            }
        }





        #region Lock File Management

        private async Task<bool> IsAnotherInstanceRunning()
        {
            try
            {
                if (!File.Exists(_lockFilePath))
                    return false;

                var lockInfo = await File.ReadAllTextAsync(_lockFilePath);
                if (DateTime.TryParse(lockInfo, out var lockTime))
                {
                    // Consider lock stale after 30 minutes
                    if (DateTime.UtcNow - lockTime < TimeSpan.FromMinutes(30))
                        return true;
                }

                // Remove stale lock file
                await RemoveLockFile();
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task CreateLockFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_lockFilePath)!);
                await File.WriteAllTextAsync(_lockFilePath, DateTime.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create lock file");
            }
        }

        private async Task RemoveLockFile()
        {
            try
            {
                if (File.Exists(_lockFilePath))
                    File.Delete(_lockFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove lock file");
            }
        }

        #endregion
    }

    /// <summary>
    /// ✅ UPDATED: Configuration for worker service (aligned with appsettings.json)
    /// </summary>
    public class WorkerSettings
    {
        public string RootFolderPath { get; set; } = @"C:\Users\engsh\Documents\PDFs\ToProcess";
        public string ProcessedFolderPath { get; set; } = @"C:\Users\engsh\Documents\PDFs\Processed";
        public string ErrorFolderPath { get; set; } = @"C:\Users\engsh\Documents\PDFs\Errors";
        public int ProcessingIntervalMinutes { get; set; } = 1;
        public int MaxConcurrentFiles { get; set; } = 10;
        public List<string> SupportedExtensions { get; set; } = new List<string> { ".pdf" };
        public int MaxFileSizeMB { get; set; } = 50;
    }

    /// <summary>
    /// ✅ UPDATED: File organization settings (aligned with appsettings.json)
    /// </summary>
    public class FileOrganizationSettings
    {
        public bool CreateCategoryFolders { get; set; } = true;
        public bool CreateVendorFolders { get; set; } = true;
        public bool CreateMonthlyFolders { get; set; } = true;
        public string FolderStructure { get; set; } = "{Category}/{Vendor}/{PeriodId}";
    }
}
