// ✅ NEW SERVICE: QueryProcessorService - Handles Query calculations separately from PDF extraction
using DT_PODSystemWorker.Models;
using DT_PODSystem.Data;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace DT_PODSystemWorker.Services
{
    /// <summary>
    /// QueryProcessorService - Background service for Query calculations
    /// Runs after DocumentProcessorService has extracted fields
    /// </summary>
    public class QueryProcessorService : BackgroundService
    {
        private readonly ILogger<QueryProcessorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkerSettings _settings;

        public QueryProcessorService(
            ILogger<QueryProcessorService> logger,
            IServiceProvider serviceProvider,
            IOptions<WorkerSettings> settings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 QueryProcessorService started - Query Calculations");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingQueries(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error in query processing batch");
                }

                await Task.Delay(_settings.ProcessingIntervalMinutes * 60 * 1000, stoppingToken);
            }

            _logger.LogInformation("🛑 QueryProcessorService stopped");
        }

        private async Task ProcessPendingQueries(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                _logger.LogInformation("🔍 Looking for processed files ready for query calculations...");

                // Find ProcessedFiles that have successful field extraction but no query results yet
                var readyForCalculation = await context.ProcessedFiles
                    .Where(pf => pf.Status == "Success" &&
                                !context.QueryResults.Any(qr => qr.ProcessedFileId == pf.Id))
                    .Include(pf => pf.Template)
                    .ToListAsync();

                if (!readyForCalculation.Any())
                {
                    _logger.LogDebug("📭 No files ready for query calculations");
                    return;
                }

                _logger.LogInformation($"📋 Found {readyForCalculation.Count} files ready for query calculations");

                foreach (var processedFile in readyForCalculation)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await ProcessQueriesForFile(processedFile, context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in ProcessPendingQueries");
            }
        }

        private async Task ProcessQueriesForFile(ProcessedFile processedFile, ApplicationDbContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation($"🔄 Processing queries for file: {processedFile.OriginalFileName} (Period: {processedFile.PeriodId})");

                // Find active queries that can work with this template's field mappings
                var availableQueries = await context.Queries
                    .Where(q => q.Status == QueryStatus.Active && q.IsActive)
                    .Include(q => q.QueryOutputs)
                    .ToListAsync();

                if (!availableQueries.Any())
                {
                    _logger.LogDebug("📭 No active queries found");
                    return;
                }

                // Get extracted field data for this processed file
                var extractedFields = await GetExtractedFieldsAsync(processedFile.Id, context);

                if (!extractedFields.Any())
                {
                    _logger.LogWarning($"⚠️ No extracted fields found for ProcessedFile {processedFile.Id}");
                    return;
                }

                _logger.LogDebug($"📥 Found {extractedFields.Count} extracted fields for calculations");

                // Process each query
                foreach (var query in availableQueries)
                {
                    await ProcessSingleQuery(query, processedFile, extractedFields);
                }

                stopwatch.Stop();
                _logger.LogInformation($"✅ Completed query processing for {processedFile.OriginalFileName} in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"💥 Failed to process queries for {processedFile.OriginalFileName} after {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private async Task ProcessSingleQuery(Query query, ProcessedFile processedFile,
            Dictionary<string, object> extractedFields)
        {
            var stopwatch = Stopwatch.StartNew();

            // ✅ SAME PATTERN: Each query gets completely separate scope and services
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var calculationService = scope.ServiceProvider.GetRequiredService<IFormulaCalculationService>();

            try
            {
                _logger.LogDebug($"🧮 Processing Query: {query.Name} for file: {processedFile.OriginalFileName}");

                // Execute calculations using FormulaCalculationService
                var calculationResult = await calculationService.CalculateQueryOutputsAsync(query.Id, extractedFields);

                if (calculationResult.Success)
                {
                    // Save calculated outputs
                    await SaveQueryResults(query.Id, processedFile.Id, processedFile.PeriodId, calculationResult, stopwatch.ElapsedMilliseconds, context);

                    // Update Query execution stats
                    var queryToUpdate = await context.Queries.FindAsync(query.Id);
                    if (queryToUpdate != null)
                    {
                        queryToUpdate.LastExecutedDate = DateTime.UtcNow;
                        queryToUpdate.ExecutionCount++;
                        await context.SaveChangesAsync();
                    }

                    _logger.LogInformation($"✅ Query {query.Name}: {calculationResult.CalculatedOutputs.Count} outputs calculated");
                }
                else
                {
                    // Handle calculation failure - save error records
                    await SaveQueryFailure(query.Id, processedFile.Id, processedFile.PeriodId, calculationResult.ErrorMessage ?? "Unknown error", stopwatch.ElapsedMilliseconds, context);
                    _logger.LogWarning($"❌ Query {query.Name} failed: {calculationResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 Error processing query {query.Name}");
                await SaveQueryFailure(query.Id, processedFile.Id, processedFile.PeriodId, $"Unexpected error: {ex.Message}", stopwatch.ElapsedMilliseconds, context);
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private async Task<Dictionary<string, object>> GetExtractedFieldsAsync(int processedFileId, ApplicationDbContext context)
        {
            var extractedFields = new Dictionary<string, object>();

            var processedFields = await context.ProcessedFields
                .Where(pf => pf.ProcessedFileId == processedFileId && pf.IsValid)
                .ToListAsync();

            foreach (var field in processedFields)
            {
                if (!string.IsNullOrEmpty(field.OutputValue))
                {
                    // Convert based on data type
                    var value = ConvertFieldValue(field.OutputValue, field.OutputDataType);
                    extractedFields[field.FieldName] = value;
                }
            }

            return extractedFields;
        }

        private object ConvertFieldValue(string value, string dataType)
        {
            try
            {
                return dataType.ToLower() switch
                {
                    "number" => decimal.Parse(value),
                    "currency" => decimal.Parse(value.Replace("$", "").Replace(",", "")),
                    "date" => DateTime.Parse(value),
                    "boolean" => bool.Parse(value),
                    _ => value
                };
            }
            catch
            {
                return value; // Return as string if conversion fails
            }
        }

        private async Task SaveQueryResults(int queryId, int processedFileId, string periodId,
            CalculationResult calculationResult, long executionTimeMs, ApplicationDbContext context)
        {
            try
            {
                var query = await context.Queries
                    .Include(q => q.QueryOutputs)
                    .FirstOrDefaultAsync(q => q.Id == queryId);

                if (query == null)
                {
                    _logger.LogError($"Query {queryId} not found");
                    return;
                }

                var queryOutputs = query.QueryOutputs.ToDictionary(qo => qo.Name, qo => qo);
                var queryResults = new List<QueryResult>();

                foreach (var output in calculationResult.CalculatedOutputs)
                {
                    if (queryOutputs.TryGetValue(output.Key, out var queryOutput))
                    {
                        var queryResult = new QueryResult
                        {
                            QueryId = queryId,
                            QueryOutputId = queryOutput.Id,
                            ProcessedFileId = processedFileId,
                            PeriodId = periodId,
                            OutputName = output.Key,
                            CalculatedValue = output.Value?.ToString(),
                            OutputDataType = queryOutput.DataType.ToString(),
                            OriginalFormula = queryOutput.FormulaExpression,
                            ProcessedFormula = calculationResult.CalculationDetails?.GetValueOrDefault(output.Key) ?? "N/A",
                            ExecutedDate = DateTime.UtcNow,
                            ExecutionTimeMs = executionTimeMs,
                            CalculationConfidence = 0.95m,
                            IsValid = output.Value != null,
                            HasFinancialData = queryOutput.DataType == DataTypeEnum.Currency
                        };

                        queryResults.Add(queryResult);
                        _logger.LogDebug($"✅ Mapped output {output.Key} = {output.Value}");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ No QueryOutput found for calculated result: {output.Key}");
                    }
                }

                if (queryResults.Any())
                {
                    context.QueryResults.AddRange(queryResults);
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"💾 Saved {queryResults.Count} query results");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving query results for Query {queryId}");
                throw;
            }
        }

        private async Task SaveQueryFailure(int queryId, int processedFileId, string periodId,
            string errorMessage, long executionTimeMs, ApplicationDbContext context)
        {
            try
            {
                // Create a single error record
                var errorResult = new QueryResult
                {
                    QueryId = queryId,
                    QueryOutputId = 0, // No specific output
                    ProcessedFileId = processedFileId,
                    PeriodId = periodId,
                    OutputName = "ERROR",
                    CalculatedValue = null,
                    OutputDataType = "Error",
                    OriginalFormula = "N/A",
                    ProcessedFormula = errorMessage,
                    ExecutedDate = DateTime.UtcNow,
                    ExecutionTimeMs = executionTimeMs,
                    CalculationConfidence = 0,
                    IsValid = false,
                    ValidationErrors = errorMessage
                };

                context.QueryResults.Add(errorResult);
                await context.SaveChangesAsync();
                _logger.LogInformation($"💾 Saved query failure record");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving query failure for Query {queryId}");
            }
        }
    }
}