 
using DT_PODSystem.Data;
using DT_PODSystem.Models.Entities;
using DT_PODSystemWorker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DT_PODSystemWorker.Services
{
    public interface IPdfExtractionService
    {
        Task<ExtractionResult> ExtractFieldsAsync(FileProcessInfo fileInfo);
    }

    public class PdfExtractionService : IPdfExtractionService
    {
        private readonly ILogger<PdfExtractionService> _logger;
        private readonly ApplicationDbContext _context;

        public PdfExtractionService(
            ILogger<PdfExtractionService> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }


        // 🎯 UPDATED ANCHOR EXTRACTION METHODS - Replace in your PdfExtractionService.cs

        /// <summary>
        /// Extract anchor text using exact coordinates and calculate text-based confidence
        /// </summary>
        private AnchorMatch? ExtractAnchorWithTextMatch(PdfTextData pdfTextData, TemplateAnchor anchor)
        {
            try
            {
                if (!pdfTextData.PageTexts.ContainsKey(anchor.PageNumber))
                {
                    _logger.LogWarning($"⚠️ [ANCHOR PAGE NOT FOUND] Page {anchor.PageNumber} not available for anchor '{anchor.ReferenceText}'");
                    return null;
                }

                var pageData = pdfTextData.PageTexts[anchor.PageNumber];

                _logger.LogInformation($"🎯 [ANCHOR EXTRACT] '{anchor.ReferenceText}' at ({anchor.X:F1}, {anchor.Y:F1}) size ({anchor.Width:F1}×{anchor.Height:F1})");

                // 🎯 DEFINE EXACT EXTRACTION RECTANGLE
                var extractionRect = new Rectangle
                {
                    X = anchor.X,
                    Y = anchor.Y,
                    Width = anchor.Width,
                    Height = anchor.Height
                };

                // 🎯 EXTRACT TEXT FROM EXACT COORDINATES (same as field extraction)
                var extractedText = ExtractTextFromExactCoordinates(pageData, extractionRect, $"Anchor-{anchor.Name}");

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning($"❌ [ANCHOR NO TEXT] No text found in coordinates for anchor '{anchor.ReferenceText}'");
                    LogNearbyText(pageData, extractionRect, $"Anchor-{anchor.Name}");
                    return null;
                }

                // 🎯 CALCULATE TEXT SIMILARITY CONFIDENCE
                var textSimilarity = CalculateTextSimilarity(anchor.ReferenceText, extractedText);
                var isTextMatch = textSimilarity >= 0.7; // 70% minimum threshold

                _logger.LogInformation($"📝 [ANCHOR TEXT MATCH] '{anchor.ReferenceText}' vs '{extractedText}'");
                _logger.LogInformation($"🎯 [SIMILARITY] {textSimilarity:P1} - {(isTextMatch ? "MATCH" : "NO MATCH")}");

                if (!isTextMatch)
                {
                    _logger.LogWarning($"⚠️ [ANCHOR TEXT MISMATCH] Expected '{anchor.ReferenceText}' but found '{extractedText}' (similarity: {textSimilarity:P1})");
                }

                // 🎯 CREATE ANCHOR MATCH WITH TEXT CONFIDENCE
                var anchorMatch = new AnchorMatch
                {
                    Anchor = anchor,
                    ConfiguredPosition = new Point { X = anchor.X, Y = anchor.Y },
                    FoundPosition = new Point { X = anchor.X, Y = anchor.Y }, // Using same position since we extract by coordinates
                    PageNumber = anchor.PageNumber,
                    ExtractedText = extractedText,
                    TextSimilarity = textSimilarity,
                    IsTextMatch = isTextMatch
                };

                _logger.LogInformation($"✅ [ANCHOR SUCCESS] '{anchor.ReferenceText}' extracted with {textSimilarity:P1} text confidence");

                return anchorMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 [ANCHOR EXTRACT ERROR] Error extracting anchor '{anchor.ReferenceText}'");
                return null;
            }
        }

        /// <summary>
        /// Calculate text similarity using enhanced algorithm
        /// </summary>
        private double CalculateTextSimilarity(string referenceText, string extractedText)
        {
            if (string.IsNullOrWhiteSpace(referenceText) || string.IsNullOrWhiteSpace(extractedText))
                return 0.0;

            // Normalize text for comparison
            var ref1 = NormalizeTextForComparison(referenceText);
            var ext1 = NormalizeTextForComparison(extractedText);

            // Method 1: Exact match (100% score)
            if (string.Equals(ref1, ext1, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug($"📝 [EXACT MATCH] '{referenceText}' == '{extractedText}'");
                return 1.0;
            }

            // Method 2: Contains match (high score)
            if (ext1.Contains(ref1, StringComparison.OrdinalIgnoreCase) || ref1.Contains(ext1, StringComparison.OrdinalIgnoreCase))
            {
                var containsScore = Math.Min(ref1.Length, ext1.Length) / (double)Math.Max(ref1.Length, ext1.Length);
                _logger.LogDebug($"📝 [CONTAINS MATCH] Score: {containsScore:P1}");
                return Math.Max(containsScore, 0.8); // Minimum 80% for contains
            }

            // Method 3: Word-based comparison
            var refWords = ref1.Split(new char[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var extWords = ext1.Split(new char[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (refWords.Length > 1 || extWords.Length > 1)
            {
                var wordScore = CalculateWordBasedSimilarity(refWords, extWords);
                _logger.LogDebug($"📝 [WORD MATCH] Score: {wordScore:P1}");
                if (wordScore > 0.6) return wordScore;
            }

            // Method 4: Character-based similarity (Levenshtein-like)
            var charScore = CalculateCharacterSimilarity(ref1, ext1);
            _logger.LogDebug($"📝 [CHAR MATCH] Score: {charScore:P1}");

            return charScore;
        }

        /// <summary>
        /// Normalize text for better comparison
        /// </summary>
        private string NormalizeTextForComparison(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            return text
                .ToLowerInvariant()
                .Trim()
                .Replace("  ", " ") // Multiple spaces to single
                .Replace("\t", " ")
                .Replace("\n", " ")
                .Replace("\r", "");
        }

        /// <summary>
        /// Calculate similarity based on matching words
        /// </summary>
        private double CalculateWordBasedSimilarity(string[] refWords, string[] extWords)
        {
            if (!refWords.Any() || !extWords.Any()) return 0.0;

            var matchingWords = 0;
            var totalWords = Math.Max(refWords.Length, extWords.Length);

            foreach (var refWord in refWords)
            {
                if (extWords.Any(extWord => string.Equals(refWord, extWord, StringComparison.OrdinalIgnoreCase)))
                {
                    matchingWords++;
                }
            }

            return (double)matchingWords / totalWords;
        }

        /// <summary>
        /// Calculate character-based similarity
        /// </summary>
        private double CalculateCharacterSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0.0;

            var maxLength = Math.Max(text1.Length, text2.Length);
            var minLength = Math.Min(text1.Length, text2.Length);

            var matchingChars = 0;
            for (int i = 0; i < minLength; i++)
            {
                if (text1[i] == text2[i])
                    matchingChars++;
            }

            return (double)matchingChars / maxLength;
        }

        /// <summary>
        /// UPDATED: Calculate coordinate transformation using text-based confidence
        /// </summary>
        private CoordinateTransformation CalculateCoordinateTransformation(PdfTemplate template, PdfTextData pdfTextData)
        {
            var transformation = new CoordinateTransformation();

            try
            {
                if (!template.TemplateAnchors.Any())
                {
                    _logger.LogInformation($"📐 [NO ANCHORS] No calibration anchors defined, using identity transformation");
                    return transformation;
                }

                _logger.LogInformation($"🛡️ [GUARDIAN CALIBRATION] Starting with {template.TemplateAnchors.Count} configured anchors");

                // Step 1: Log all configured anchors
                foreach (var anchor in template.TemplateAnchors.OrderBy(a => a.DisplayOrder))
                {
                    _logger.LogInformation($"   📍 [ANCHOR CONFIG] '{anchor.ReferenceText}' at ({anchor.X:F1}, {anchor.Y:F1}) size ({anchor.Width:F1}×{anchor.Height:F1})");
                }

                // Step 2: Extract anchor text using exact coordinates
                var extractedAnchors = new List<AnchorMatch>();

                foreach (var anchor in template.TemplateAnchors.OrderBy(a => a.DisplayOrder))
                {
                    var anchorMatch = ExtractAnchorWithTextMatch(pdfTextData, anchor);

                    if (anchorMatch != null)
                    {
                        extractedAnchors.Add(anchorMatch);

                        _logger.LogInformation($"✅ [ANCHOR EXTRACTED] '{anchor.ReferenceText}':");
                        _logger.LogInformation($"   📍 [COORDINATES] ({anchor.X:F1}, {anchor.Y:F1}) size ({anchor.Width:F1}×{anchor.Height:F1})");
                        _logger.LogInformation($"   📝 [EXTRACTED] '{anchorMatch.ExtractedText}'");
                        _logger.LogInformation($"   🎯 [TEXT MATCH] {anchorMatch.TextSimilarity:P1} - {(anchorMatch.IsTextMatch ? "MATCH" : "NO MATCH")}");
                    }
                    else
                    {
                        _logger.LogWarning($"❌ [ANCHOR FAILED] Could not extract text for '{anchor.ReferenceText}'");
                    }
                }

                // Step 3: Calculate text-based confidence (success rate of reading anchors)
                if (extractedAnchors.Any())
                {
                    // Text-based confidence: percentage of anchors that match their reference text
                    var textMatchCount = extractedAnchors.Count(a => a.IsTextMatch);
                    var textBasedConfidence = (decimal)textMatchCount / template.TemplateAnchors.Count;

                    // Average text similarity across all extracted anchors
                    var avgTextSimilarity = (decimal)extractedAnchors.Average(a => a.TextSimilarity);

                    // Combined confidence (weighted average)
                    var extractionRate = (decimal)extractedAnchors.Count / template.TemplateAnchors.Count;
                    var combinedConfidence = (extractionRate * 0.3m) + (textBasedConfidence * 0.4m) + (avgTextSimilarity * 0.3m);

                    transformation.Confidence = Math.Max(combinedConfidence, 0.1m); // Minimum 10%
                    transformation.AnchorMatches = extractedAnchors;

                    _logger.LogInformation($"📊 [ANCHOR CONFIDENCE] Text Match Rate: {textBasedConfidence:P1} ({textMatchCount}/{template.TemplateAnchors.Count})");
                    _logger.LogInformation($"📊 [ANCHOR CONFIDENCE] Avg Text Similarity: {avgTextSimilarity:P1}");
                    _logger.LogInformation($"📊 [ANCHOR CONFIDENCE] Extraction Rate: {extractionRate:P1} ({extractedAnchors.Count}/{template.TemplateAnchors.Count})");
                    _logger.LogInformation($"📊 [FINAL CONFIDENCE] Combined: {transformation.Confidence:P1}");

                    // ✅ SAFETY CHECK: Low confidence = identity transformation
                    if (transformation.Confidence < 0.5m) // 50% threshold
                    {
                        _logger.LogWarning($"🚨 [LOW CONFIDENCE] Text confidence {transformation.Confidence:P1} below 50% threshold");
                        _logger.LogWarning($"🛡️ [GUARDIAN SAFETY] Using identity transformation (no coordinate adjustment)");
                        transformation.Confidence = 0.1m; // Signal low confidence
                        return transformation;
                    }

                    _logger.LogInformation($"🛡️ [GUARDIAN SUCCESS] Anchor calibration completed with {transformation.Confidence:P1} confidence");
                }
                else
                {
                    _logger.LogWarning($"⚠️ [CALIBRATION FAILED] No anchors could be extracted - using identity transformation");
                    transformation.Confidence = 0.1m;
                }

                return transformation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [GUARDIAN ERROR] Error during anchor calibration");
                return new CoordinateTransformation(); // Fallback to identity
            }
        }

        // 🎯 NEW PHRASE DETECTION METHODS - Add these to your PdfExtractionService class

        #region Phrase Detection Methods

        /// <summary>
        /// Build phrases from individual words based on spatial proximity
        /// </summary>
        private List<DetectedPhrase> BuildPhrasesFromSpacing(List<WordData> words)
        {
            var phrases = new List<DetectedPhrase>();
            var currentPhrase = new List<WordData>();

            if (!words.Any())
                return phrases;

            _logger.LogDebug($"🔤 [PHRASE BUILDER] Processing {words.Count} words into phrases");

            // Sort words by reading order (top-to-bottom, left-to-right)
            var sortedWords = words
                .OrderBy(w => Math.Round(w.Y / 5) * 5) // Group by line (5px tolerance)
                .ThenBy(w => w.X)                      // Left to right
                .ToList();

            _logger.LogDebug($"📖 [READING ORDER] Sorted {sortedWords.Count} words by position");

            for (int i = 0; i < sortedWords.Count; i++)
            {
                var currentWord = sortedWords[i];

                if (currentPhrase.Any())
                {
                    var lastWord = currentPhrase.Last();

                    // Check if words should be part of same phrase
                    if (ShouldContinuePhrase(lastWord, currentWord))
                    {
                        // Continue current phrase
                        currentPhrase.Add(currentWord);
                        _logger.LogTrace($"📝 [PHRASE CONTINUE] Added '{currentWord.Text}' to current phrase");
                    }
                    else
                    {
                        // End current phrase, start new one
                        var completedPhrase = CreateDetectedPhrase(currentPhrase);
                        phrases.Add(completedPhrase);

                        _logger.LogDebug($"✅ [PHRASE COMPLETED] '{completedPhrase.Text}' ({completedPhrase.Words.Count} words)");

                        currentPhrase = new List<WordData> { currentWord };
                    }
                }
                else
                {
                    // Start first phrase
                    currentPhrase.Add(currentWord);
                    _logger.LogTrace($"🚀 [PHRASE START] Starting new phrase with '{currentWord.Text}'");
                }
            }

            // Add final phrase
            if (currentPhrase.Any())
            {
                var finalPhrase = CreateDetectedPhrase(currentPhrase);
                phrases.Add(finalPhrase);
                _logger.LogDebug($"✅ [FINAL PHRASE] '{finalPhrase.Text}' ({finalPhrase.Words.Count} words)");
            }

            _logger.LogInformation($"🎯 [PHRASE DETECTION] Built {phrases.Count} phrases from {words.Count} words");

            // Log top 10 phrases for debugging
            var topPhrases = phrases.Take(10).ToList();
            foreach (var phrase in topPhrases)
            {
                _logger.LogDebug($"   📝 [PHRASE] '{phrase.Text}' at ({phrase.TopLeft.X:F1}, {phrase.TopLeft.Y:F1})");
            }

            return phrases;
        }

        /// <summary>
        /// Determine if current word should continue the current phrase
        /// </summary>
        private bool ShouldContinuePhrase(WordData lastWord, WordData currentWord)
        {
            // Check 1: Same line (Y-coordinate proximity)
            if (!IsSameLine(lastWord, currentWord))
            {
                _logger.LogTrace($"📏 [LINE BREAK] '{lastWord.Text}' vs '{currentWord.Text}' - different lines");
                return false;
            }

            // Check 2: Reasonable horizontal spacing
            if (!HasReasonableSpacing(lastWord, currentWord))
            {
                _logger.LogTrace($"📏 [SPACE BREAK] '{lastWord.Text}' vs '{currentWord.Text}' - too much spacing");
                return false;
            }

            // Check 3: Not crossing major layout boundaries (optional)
            if (CrossesMajorBoundary(lastWord, currentWord))
            {
                _logger.LogTrace($"📏 [LAYOUT BREAK] '{lastWord.Text}' vs '{currentWord.Text}' - crosses boundary");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if two words are on the same line
        /// </summary>
        private bool IsSameLine(WordData word1, WordData word2)
        {
            const double LINE_TOLERANCE = 8.0; // pixels
            var yDifference = Math.Abs(word1.Y - word2.Y);
            return yDifference < LINE_TOLERANCE;
        }

        /// <summary>
        /// Check if spacing between words is reasonable for same phrase
        /// </summary>
        private bool HasReasonableSpacing(WordData lastWord, WordData currentWord)
        {
            const double MAX_WORD_GAP = 50.0; // pixels
            const double MIN_WORD_GAP = -5.0;  // Allow slight overlap

            var gap = currentWord.X - (lastWord.X + lastWord.Width);
            return gap >= MIN_WORD_GAP && gap <= MAX_WORD_GAP;
        }

        /// <summary>
        /// Check if words cross major layout boundaries (tables, columns, etc.)
        /// </summary>
        private bool CrossesMajorBoundary(WordData lastWord, WordData currentWord)
        {
            const double MAJOR_GAP_THRESHOLD = 100.0; // pixels

            // Large horizontal gap might indicate column break
            var horizontalGap = currentWord.X - (lastWord.X + lastWord.Width);
            if (horizontalGap > MAJOR_GAP_THRESHOLD)
                return true;

            // Large vertical shift might indicate section break
            var verticalShift = Math.Abs(currentWord.Y - lastWord.Y);
            if (verticalShift > 20.0) // More than typical line height variation
                return true;

            return false;
        }

        /// <summary>
        /// Create DetectedPhrase object from list of words
        /// </summary>
        private DetectedPhrase CreateDetectedPhrase(List<WordData> words)
        {
            if (!words.Any())
                throw new ArgumentException("Cannot create phrase from empty word list");

            // Calculate phrase text
            var phraseText = string.Join(" ", words.Select(w => w.Text.Trim()));

            // Calculate bounding box
            var minX = words.Min(w => w.X);
            var minY = words.Min(w => w.Y);
            var maxX = words.Max(w => w.X + w.Width);
            var maxY = words.Max(w => w.Y + w.Height);

            return new DetectedPhrase
            {
                Text = phraseText,
                Words = new List<WordData>(words),
                TopLeft = new Point { X = minX, Y = minY },
                BoundingBox = new Rectangle
                {
                    X = minX,
                    Y = minY,
                    Width = maxX - minX,
                    Height = maxY - minY
                }
            };
        }

        /// <summary>
        /// Find anchor text within detected phrases using flexible matching
        /// </summary>
        private Point? FindAnchorInPhrases(List<DetectedPhrase> phrases, string anchorText, string anchorName)
        {
            _logger.LogInformation($"🔍 [PHRASE SEARCH] Looking for anchor '{anchorText}' in {phrases.Count} detected phrases");

            // Method 1: Exact phrase match (highest priority)
            var exactMatch = phrases.FirstOrDefault(p =>
                string.Equals(p.Text.Trim(), anchorText.Trim(), StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                _logger.LogInformation($"✅ [EXACT PHRASE MATCH] Found '{anchorText}' as complete phrase at ({exactMatch.TopLeft.X:F1}, {exactMatch.TopLeft.Y:F1})");
                _logger.LogInformation($"   📝 [PHRASE CONTENT] '{exactMatch.Text}'");
                _logger.LogInformation($"   🔤 [WORD COUNT] {exactMatch.Words.Count} words");
                return exactMatch.TopLeft;
            }

            // Method 2: Phrase contains anchor (anchor is part of longer phrase)
            var containsMatch = phrases.FirstOrDefault(p =>
                p.Text.Contains(anchorText, StringComparison.OrdinalIgnoreCase));

            if (containsMatch != null)
            {
                _logger.LogInformation($"✅ [PHRASE CONTAINS MATCH] Found '{anchorText}' within phrase at ({containsMatch.TopLeft.X:F1}, {containsMatch.TopLeft.Y:F1})");
                _logger.LogInformation($"   📝 [FULL PHRASE] '{containsMatch.Text}'");
                _logger.LogInformation($"   🎯 [MATCH TYPE] Anchor is part of longer phrase");
                return containsMatch.TopLeft;
            }

            // Method 3: Anchor contains phrase (partial word matching)
            var partialMatch = phrases.FirstOrDefault(p =>
                anchorText.Contains(p.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                p.Text.Trim().Length > 2); // Avoid matching very short words

            if (partialMatch != null)
            {
                _logger.LogInformation($"✅ [PARTIAL PHRASE MATCH] Found phrase '{partialMatch.Text}' matching part of anchor '{anchorText}' at ({partialMatch.TopLeft.X:F1}, {partialMatch.TopLeft.Y:F1})");
                return partialMatch.TopLeft;
            }

            // Method 4: Fuzzy phrase matching (Levenshtein distance)
            var fuzzyMatch = phrases
                .Select(p => new { Phrase = p, Similarity = CalculateSimilarity(p.Text, anchorText) })
                .Where(x => x.Similarity >= 0.7) // 70% similarity threshold
                .OrderByDescending(x => x.Similarity)
                .FirstOrDefault();

            if (fuzzyMatch != null)
            {
                _logger.LogInformation($"✅ [FUZZY PHRASE MATCH] Found similar phrase '{fuzzyMatch.Phrase.Text}' ({fuzzyMatch.Similarity:P0} similar) at ({fuzzyMatch.Phrase.TopLeft.X:F1}, {fuzzyMatch.Phrase.TopLeft.Y:F1})");
                return fuzzyMatch.Phrase.TopLeft;
            }

            // No matches found - log available phrases for debugging
            _logger.LogWarning($"❌ [PHRASE NOT FOUND] Could not locate anchor '{anchorText}' in any detected phrases");
            LogAvailablePhrases(phrases, anchorText);

            return null;
        }

        /// <summary>
        /// Calculate text similarity using simple character-based approach
        /// </summary>
        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0.0;

            text1 = text1.ToLowerInvariant().Trim();
            text2 = text2.ToLowerInvariant().Trim();

            if (text1 == text2)
                return 1.0;

            var maxLength = Math.Max(text1.Length, text2.Length);
            var minLength = Math.Min(text1.Length, text2.Length);

            var matchingChars = 0;
            for (int i = 0; i < minLength; i++)
            {
                if (text1[i] == text2[i])
                    matchingChars++;
            }

            return (double)matchingChars / maxLength;
        }

        /// <summary>
        /// Log available phrases for debugging when anchor not found
        /// </summary>
        private void LogAvailablePhrases(List<DetectedPhrase> phrases, string searchText)
        {
            _logger.LogInformation($"🔍 [DEBUG] Available phrases on page (showing top 15):");

            var relevantPhrases = phrases
                .Where(p => p.Text.Length > 2) // Skip very short phrases
                .OrderByDescending(p => CalculateSimilarity(p.Text, searchText))
                .Take(15)
                .ToList();

            foreach (var phrase in relevantPhrases)
            {
                var similarity = CalculateSimilarity(phrase.Text, searchText);
                _logger.LogInformation($"   📝 '{phrase.Text}' at ({phrase.TopLeft.X:F1}, {phrase.TopLeft.Y:F1}) - {similarity:P0} similar");
            }
        }

        #endregion

        // 🎯 UPDATED SearchForAnchorInDocument METHOD - Replace your existing method with this

        /// <summary>
        /// Search for anchor using phrase detection approach
        /// </summary>
        private Point? SearchForAnchorInDocument(PdfTextData pdfTextData, TemplateAnchor anchor)
        {
            try
            {
                if (!pdfTextData.PageTexts.ContainsKey(anchor.PageNumber))
                {
                    _logger.LogWarning($"⚠️ [PAGE NOT FOUND] Page {anchor.PageNumber} not available for anchor search");
                    return null;
                }

                var pageData = pdfTextData.PageTexts[anchor.PageNumber];
                var searchText = anchor.ReferenceText.Trim();

                _logger.LogInformation($"🔍 [PHRASE-BASED SEARCH] Looking for anchor '{searchText}' on page {anchor.PageNumber}");
                _logger.LogDebug($"📄 [PAGE INFO] Page has {pageData.Words.Count} words available");

                // 🎯 STEP 1: Build phrases from all words on the page
                var detectedPhrases = BuildPhrasesFromSpacing(pageData.Words);

                _logger.LogInformation($"📝 [PHRASE DETECTION] Built {detectedPhrases.Count} phrases from {pageData.Words.Count} words");

                // 🎯 STEP 2: Search for anchor in detected phrases
                var anchorPosition = FindAnchorInPhrases(detectedPhrases, searchText, anchor.Name);

                if (anchorPosition != null)
                {
                    _logger.LogInformation($"✅ [PHRASE SEARCH SUCCESS] Found anchor '{searchText}' at TOP-LEFT ({anchorPosition.X:F1}, {anchorPosition.Y:F1})");
                    return anchorPosition;
                }

                // 🎯 FALLBACK: Try original word-based search if phrase search fails
                _logger.LogWarning($"⚠️ [PHRASE SEARCH FAILED] Falling back to original word-based search");
                return OriginalWordBasedSearch(pageData, searchText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 [PHRASE SEARCH ERROR] Error searching for anchor '{anchor.ReferenceText}'");
                return null;
            }
        }

        /// <summary>
        /// Fallback to original word-based search method
        /// </summary>
        private Point? OriginalWordBasedSearch(PageTextData pageData, string searchText)
        {
            _logger.LogInformation($"🔄 [FALLBACK SEARCH] Using original word-based search for '{searchText}'");

            // Method 1: Exact single word match
            if (!searchText.Contains(' '))
            {
                var exactWord = pageData.Words.FirstOrDefault(w =>
                    string.Equals(w.Text.Trim(), searchText, StringComparison.OrdinalIgnoreCase));

                if (exactWord != null)
                {
                    _logger.LogInformation($"✅ [FALLBACK EXACT] Found '{searchText}' at ({exactWord.X:F1}, {exactWord.Y:F1})");
                    return new Point { X = exactWord.X, Y = exactWord.Y };
                }
            }

            // Method 2: First word of multi-word search
            var searchWords = searchText.Split(new char[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (searchWords.Length > 1)
            {
                var firstWordMatch = pageData.Words.FirstOrDefault(w =>
                    string.Equals(w.Text.Trim(), searchWords[0], StringComparison.OrdinalIgnoreCase));

                if (firstWordMatch != null)
                {
                    _logger.LogInformation($"✅ [FALLBACK FIRST WORD] Found first word '{searchWords[0]}' at ({firstWordMatch.X:F1}, {firstWordMatch.Y:F1})");
                    return new Point { X = firstWordMatch.X, Y = firstWordMatch.Y };
                }
            }

            // Method 3: Partial word match (with distance validation)
            var partialMatches = pageData.Words
                .Where(w => w.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                           searchText.Contains(w.Text, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (partialMatches.Any())
            {
                // If we have expected position, choose closest match
                var bestMatch = partialMatches.OrderBy(w => w.X).First(); // Choose leftmost as fallback

                _logger.LogWarning($"⚠️ [FALLBACK PARTIAL] Found partial match '{bestMatch.Text}' at ({bestMatch.X:F1}, {bestMatch.Y:F1})");
                _logger.LogWarning($"   🚨 [WARNING] This may not be the intended anchor position");

                return new Point { X = bestMatch.X, Y = bestMatch.Y };
            }

            _logger.LogError($"❌ [FALLBACK FAILED] Could not find '{searchText}' using any search method");
            return null;
        }



        // 🎯 VISUAL EXTRACTION: Extract text that falls EXACTLY within rectangle coordinates
        private object? ExtractFieldValueWithTransformation(PdfTextData pdfTextData, FieldMapping mapping, CoordinateTransformation transformation)
        {
            try
            {
                if (!pdfTextData.PageTexts.ContainsKey(mapping.PageNumber))
                {
                    _logger.LogWarning($"⚠️ [PAGE NOT FOUND] Page {mapping.PageNumber} not available for field {mapping.FieldName}");
                    return null;
                }

                var pageData = pdfTextData.PageTexts[mapping.PageNumber];

                // ✅ APPLY GUARDIAN TRANSFORMATION TO COORDINATES
                var adjustedX = (mapping.X * transformation.ScaleX) + transformation.OffsetX;
                var adjustedY = (mapping.Y * transformation.ScaleY) + transformation.OffsetY;
                var adjustedWidth = mapping.Width * transformation.ScaleX;
                var adjustedHeight = mapping.Height * transformation.ScaleY;

                _logger.LogInformation($"🎯 [VISUAL EXTRACT] Field '{mapping.FieldName}':");
                _logger.LogInformation($"   📐 [ORIGINAL] ({mapping.X:F1}, {mapping.Y:F1}) size ({mapping.Width:F1}×{mapping.Height:F1})");
                _logger.LogInformation($"   🛡️ [CALIBRATED] ({adjustedX:F1}, {adjustedY:F1}) size ({adjustedWidth:F1}×{adjustedHeight:F1})");
                _logger.LogInformation($"   🔧 [TRANSFORM] Offset:({transformation.OffsetX:+0.0;-0.0},{transformation.OffsetY:+0.0;-0.0}) Scale:({transformation.ScaleX:F3},{transformation.ScaleY:F3})");

                // 🎯 DEFINE EXACT EXTRACTION RECTANGLE
                var extractionRect = new Rectangle
                {
                    X = adjustedX,
                    Y = adjustedY,
                    Width = adjustedWidth,
                    Height = adjustedHeight
                };

                _logger.LogInformation($"   📦 [EXTRACT AREA] ({extractionRect.X:F1}, {extractionRect.Y:F1}) to ({extractionRect.X + extractionRect.Width:F1}, {extractionRect.Y + extractionRect.Height:F1})");

                // 🎯 VISUAL EXTRACTION: Get all text that falls within exact coordinates
                var extractedText = ExtractTextFromExactCoordinates(pageData, extractionRect, mapping.FieldName);

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    var numericValue = ConvertToNumericValue(extractedText, mapping.FieldName);

                    _logger.LogInformation($"✅ [VISUAL SUCCESS] Field '{mapping.FieldName}':");
                    _logger.LogInformation($"   📄 [RAW TEXT] '{extractedText}'");
                    _logger.LogInformation($"   🔢 [NUMERIC VALUE] {numericValue}");

                    return numericValue;
                }
                else
                {
                    _logger.LogWarning($"❌ [NO TEXT] No text found in exact coordinates for field '{mapping.FieldName}'");

                    // 🔍 DEBUG: Show what's nearby
                    LogNearbyText(pageData, extractionRect, mapping.FieldName);

                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 [VISUAL EXTRACT ERROR] Error extracting field {mapping.FieldName}");
                return null;
            }
        }

        // 🎯 CORE METHOD: Extract text from exact coordinates (like image crop)
        private string ExtractTextFromExactCoordinates(PageTextData pageData, Rectangle rect, string fieldName)
        {
            var extractedParts = new List<ExtractedTextPart>();

            _logger.LogDebug($"🔍 [COORDINATE SCAN] Scanning for text within exact rectangle for '{fieldName}'");

            // 🎯 SCAN ALL WORDS: Check each word's position against exact rectangle
            foreach (var word in pageData.Words)
            {
                // Convert word coordinates to top-left if needed
                var wordX = word.X;
                var wordY = word.Y;
                var wordWidth = word.Width;
                var wordHeight = word.Height;

                // 🎯 CHECK IF WORD IS INSIDE RECTANGLE (VISUAL INTERSECTION)
                var intersection = CalculateIntersection(wordX, wordY, wordWidth, wordHeight, rect);

                if (intersection.HasIntersection)
                {
                    var coveragePercent = (intersection.IntersectionArea / (wordWidth * wordHeight)) * 100;

                    _logger.LogDebug($"📍 [WORD FOUND] '{word.Text}' at ({wordX:F1}, {wordY:F1}) - Coverage: {coveragePercent:F1}%");

                    // 🎯 INCLUDE WORD BASED ON COVERAGE THRESHOLD
                    if (coveragePercent >= 30) // At least 30% of word must be inside rectangle
                    {
                        extractedParts.Add(new ExtractedTextPart
                        {
                            Text = word.Text,
                            X = wordX,
                            Y = wordY,
                            Coverage = coveragePercent,
                            IntersectionArea = intersection.IntersectionArea
                        });

                        _logger.LogDebug($"✅ [INCLUDED] '{word.Text}' (Coverage: {coveragePercent:F1}%)");
                    }
                    else
                    {
                        _logger.LogDebug($"❌ [EXCLUDED] '{word.Text}' (Coverage too low: {coveragePercent:F1}%)");
                    }
                }
            }

            // 🎯 COMBINE EXTRACTED PARTS IN READING ORDER
            if (extractedParts.Any())
            {
                // Sort by position (top-to-bottom, left-to-right)
                var sortedParts = extractedParts
                    .OrderBy(p => Math.Round(p.Y / 5) * 5) // Group by line (5px tolerance)
                    .ThenBy(p => p.X)
                    .ToList();

                var combinedText = string.Join(" ", sortedParts.Select(p => p.Text)).Trim();

                _logger.LogInformation($"✅ [VISUAL RESULT] Extracted '{combinedText}' from {extractedParts.Count} words");

                return combinedText;
            }

            _logger.LogWarning($"❌ [EMPTY RESULT] No text found within exact coordinates");
            return string.Empty;
        }

        // 🎯 GEOMETRIC CALCULATION: Word-Rectangle intersection
        private (bool HasIntersection, double IntersectionArea) CalculateIntersection(
            double wordX, double wordY, double wordWidth, double wordHeight,
            Rectangle rect)
        {
            // Calculate intersection rectangle
            var intersectLeft = Math.Max(wordX, rect.X);
            var intersectRight = Math.Min(wordX + wordWidth, rect.X + rect.Width);
            var intersectTop = Math.Max(wordY, rect.Y);
            var intersectBottom = Math.Min(wordY + wordHeight, rect.Y + rect.Height);

            // Check if there's actual intersection
            if (intersectLeft < intersectRight && intersectTop < intersectBottom)
            {
                var intersectionArea = (intersectRight - intersectLeft) * (intersectBottom - intersectTop);
                return (true, intersectionArea);
            }

            return (false, 0);
        }

        // 🔍 DEBUG: Show nearby text when extraction fails
        private void LogNearbyText(PageTextData pageData, Rectangle rect, string fieldName)
        {
            _logger.LogInformation($"🔍 [DEBUG] Nearby text for '{fieldName}':");

            var centerX = rect.X + rect.Width / 2;
            var centerY = rect.Y + rect.Height / 2;

            var nearbyWords = pageData.Words
                .Select(w => new
                {
                    Word = w,
                    Distance = Math.Sqrt(Math.Pow(w.X + w.Width / 2 - centerX, 2) + Math.Pow(w.Y + w.Height / 2 - centerY, 2))
                })
                .Where(x => x.Distance <= 100) // Within 100px
                .OrderBy(x => x.Distance)
                .Take(5)
                .ToList();

            foreach (var nearby in nearbyWords)
            {
                _logger.LogInformation($"   📍 '{nearby.Word.Text}' at ({nearby.Word.X:F1}, {nearby.Word.Y:F1}) - Distance: {nearby.Distance:F1}px");
            }
        }

        // 🎯 ENHANCED PDF EXTRACTION: Extract words with precise coordinates
        private PdfTextData ExtractPdfTextWithCoordinates(string filePath)
        {
            var result = new PdfTextData();

            try
            {
                _logger.LogInformation($"📖 [PDF EXTRACTION] Opening PDF file: {filePath}");

                using (var document = PdfDocument.Open(filePath))
                {
                    _logger.LogInformation($"📄 [PDF INFO] Pages: {document.NumberOfPages}");

                    for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
                    {
                        try
                        {
                            var page = document.GetPage(pageNum);
                            var pageHeight = (double)page.Height;

                            _logger.LogDebug($"📄 [PAGE {pageNum}] Dimensions: {page.Width:F1} × {pageHeight:F1}");

                            var pageData = new PageTextData
                            {
                                PageNumber = pageNum,
                                Words = new List<WordData>()
                            };

                            // 🎯 EXTRACT ALL WORDS WITH PRECISE COORDINATES
                            var pdfWords = page.GetWords().ToList();

                            foreach (var word in pdfWords)
                            {
                                // Convert from PDF bottom-left to top-left coordinates
                                var topLeftY = pageHeight - word.BoundingBox.Top;

                                var wordData = new WordData
                                {
                                    Text = word.Text,
                                    X = word.BoundingBox.Left,
                                    Y = topLeftY, // Top-left Y coordinate
                                    Width = word.BoundingBox.Width,
                                    Height = word.BoundingBox.Height
                                };

                                pageData.Words.Add(wordData);
                            }

                            result.PageTexts[pageNum] = pageData;

                            _logger.LogInformation($"✅ [PAGE {pageNum}] Extracted {pdfWords.Count} words with coordinates");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"💥 [PAGE {pageNum} ERROR] Failed to extract page");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 [PDF ERROR] Could not extract from {filePath}");
                return null;
            }

            return result;
        }



        // 🎯 Calculate robust transformation using multiple anchors
        private CoordinateTransformation CalculateRobustTransformation(List<AnchorMatch> anchors)
        {
            var transformation = new CoordinateTransformation();

            try
            {
                // Algorithm 1: Translation (Average Offset Method)
                var offsetsX = anchors.Select(a => a.FoundPosition.X - a.ConfiguredPosition.X).ToList();
                var offsetsY = anchors.Select(a => a.FoundPosition.Y - a.ConfiguredPosition.Y).ToList();

                transformation.OffsetX = offsetsX.Average();
                transformation.OffsetY = offsetsY.Average();

                _logger.LogInformation($"📊 [TRANSLATION] Average offset: ({transformation.OffsetX:F2}, {transformation.OffsetY:F2})");

                // Algorithm 2: Scale Detection (Distance Ratio Analysis)
                if (anchors.Count >= 2)
                {
                    var scaleFactors = CalculateScaleFactors(anchors);
                    transformation.ScaleX = scaleFactors.scaleX;
                    transformation.ScaleY = scaleFactors.scaleY;

                    _logger.LogInformation($"📏 [SCALING] Scale factors: X={transformation.ScaleX:F3}, Y={transformation.ScaleY:F3}");
                }

                // Algorithm 3: Distortion Detection
                var distortionLevel = CalculateDistortionLevel(anchors);
                transformation.DistortionLevel = distortionLevel;

                if (distortionLevel > 0.1) // 10% threshold
                {
                    _logger.LogWarning($"⚠️ [DISTORTION DETECTED] Level: {distortionLevel:P1} - Document may have irregular scaling");
                }

                // Algorithm 4: Outlier Detection
                var outliers = DetectOutlierAnchors(anchors);
                if (outliers.Any())
                {
                    _logger.LogWarning($"⚠️ [OUTLIERS] {outliers.Count} anchor(s) detected as outliers");
                    foreach (var outlier in outliers)
                    {
                        _logger.LogWarning($"   📍 [OUTLIER] '{outlier.Anchor.ReferenceText}' has unusual offset");
                    }
                }

                return transformation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [TRANSFORM ERROR] Error calculating robust transformation");
                return new CoordinateTransformation(); // Identity fallback
            }
        }

        // 🎯 Calculate scale factors using anchor distance ratios
        private (double scaleX, double scaleY) CalculateScaleFactors(List<AnchorMatch> anchors)
        {
            double scaleX = 1.0, scaleY = 1.0;

            try
            {
                var scaleFactorsX = new List<double>();
                var scaleFactorsY = new List<double>();

                // Compare distances between all anchor pairs
                for (int i = 0; i < anchors.Count - 1; i++)
                {
                    for (int j = i + 1; j < anchors.Count; j++)
                    {
                        var anchor1 = anchors[i];
                        var anchor2 = anchors[j];

                        // Calculate original distance
                        var originalDistanceX = Math.Abs(anchor2.ConfiguredPosition.X - anchor1.ConfiguredPosition.X);
                        var originalDistanceY = Math.Abs(anchor2.ConfiguredPosition.Y - anchor1.ConfiguredPosition.Y);

                        // Calculate found distance
                        var foundDistanceX = Math.Abs(anchor2.FoundPosition.X - anchor1.FoundPosition.X);
                        var foundDistanceY = Math.Abs(anchor2.FoundPosition.Y - anchor1.FoundPosition.Y);

                        // Calculate scale factors
                        if (originalDistanceX > 10) // Minimum distance threshold
                        {
                            scaleFactorsX.Add(foundDistanceX / originalDistanceX);
                        }

                        if (originalDistanceY > 10)
                        {
                            scaleFactorsY.Add(foundDistanceY / originalDistanceY);
                        }
                    }
                }

                // Use median for robustness against outliers
                if (scaleFactorsX.Any())
                {
                    scaleX = scaleFactorsX.OrderBy(x => x).Skip(scaleFactorsX.Count / 2).First();
                }

                if (scaleFactorsY.Any())
                {
                    scaleY = scaleFactorsY.OrderBy(y => y).Skip(scaleFactorsY.Count / 2).First();
                }

                _logger.LogDebug($"📏 [SCALE CALC] X factors: [{string.Join(", ", scaleFactorsX.Select(x => x.ToString("F3")))}] → {scaleX:F3}");
                _logger.LogDebug($"📏 [SCALE CALC] Y factors: [{string.Join(", ", scaleFactorsY.Select(y => y.ToString("F3")))}] → {scaleY:F3}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [SCALE ERROR] Error calculating scale factors");
            }

            return (scaleX, scaleY);
        }

        // 🎯 Calculate document distortion level
        private double CalculateDistortionLevel(List<AnchorMatch> anchors)
        {
            try
            {
                if (anchors.Count < 2) return 0.0;

                var offsetsX = anchors.Select(a => a.FoundPosition.X - a.ConfiguredPosition.X).ToList();
                var offsetsY = anchors.Select(a => a.FoundPosition.Y - a.ConfiguredPosition.Y).ToList();

                var avgOffsetX = offsetsX.Average();
                var avgOffsetY = offsetsY.Average();

                var varianceX = offsetsX.Select(x => Math.Pow(x - avgOffsetX, 2)).Average();
                var varianceY = offsetsY.Select(y => Math.Pow(y - avgOffsetY, 2)).Average();

                var standardDeviationX = Math.Sqrt(varianceX);
                var standardDeviationY = Math.Sqrt(varianceY);

                var maxDeviation = Math.Max(standardDeviationX, standardDeviationY);

                // Normalize to percentage (100px deviation = 100% distortion)
                return Math.Min(maxDeviation / 100.0, 1.0);
            }
            catch
            {
                return 0.0;
            }
        }

        // 🎯 Detect outlier anchors using Z-score method
        private List<AnchorMatch> DetectOutlierAnchors(List<AnchorMatch> anchors)
        {
            var outliers = new List<AnchorMatch>();

            try
            {
                if (anchors.Count < 3) return outliers; // Need at least 3 points for outlier detection

                var offsetsX = anchors.Select(a => a.FoundPosition.X - a.ConfiguredPosition.X).ToList();
                var offsetsY = anchors.Select(a => a.FoundPosition.Y - a.ConfiguredPosition.Y).ToList();

                var meanX = offsetsX.Average();
                var meanY = offsetsY.Average();
                var stdX = Math.Sqrt(offsetsX.Select(x => Math.Pow(x - meanX, 2)).Average());
                var stdY = Math.Sqrt(offsetsY.Select(y => Math.Pow(y - meanY, 2)).Average());

                for (int i = 0; i < anchors.Count; i++)
                {
                    var zScoreX = stdX > 0 ? Math.Abs((offsetsX[i] - meanX) / stdX) : 0;
                    var zScoreY = stdY > 0 ? Math.Abs((offsetsY[i] - meanY) / stdY) : 0;

                    if (zScoreX > 2.0 || zScoreY > 2.0) // 2 standard deviations threshold
                    {
                        outliers.Add(anchors[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [OUTLIER ERROR] Error detecting outliers");
            }

            return outliers;
        }

        // 🎯 Calculate simple translation for single anchor
        private CoordinateTransformation CalculateSimpleTranslation(AnchorMatch anchor)
        {
            return new CoordinateTransformation
            {
                OffsetX = anchor.FoundPosition.X - anchor.ConfiguredPosition.X,
                OffsetY = anchor.FoundPosition.Y - anchor.ConfiguredPosition.Y,
                ScaleX = 1.0,
                ScaleY = 1.0,
                Confidence = 0.7m // Lower confidence for single anchor
            };
        }

        // 🎯 Calculate overall calibration confidence
        private decimal CalculateCalibrationConfidence(List<AnchorMatch> foundAnchors, ICollection<TemplateAnchor> totalAnchors)
        {
            if (!totalAnchors.Any()) return 1.0m;

            var baseConfidence = (decimal)foundAnchors.Count / totalAnchors.Count;

            // Adjust confidence based on anchor quality
            if (foundAnchors.Any())
            {
                var avgDistance = foundAnchors.Average(a =>
                {
                    var dx = a.FoundPosition.X - a.ConfiguredPosition.X;
                    var dy = a.FoundPosition.Y - a.ConfiguredPosition.Y;
                    return Math.Sqrt(dx * dx + dy * dy);
                });

                // Lower confidence if anchors are far from expected positions
                var distancePenalty = Math.Min(avgDistance / 100.0, 0.5); // Max 50% penalty
                baseConfidence *= (decimal)(1.0 - distancePenalty);
            }

            return Math.Max(baseConfidence, 0.1m); // Minimum 10% confidence
        }

        // 🔢 NUMERIC CONVERSION WITH PERCENTAGE NORMALIZATION
        private object? ConvertToNumericValue(string text, string fieldName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                // 🎯 EXTRACT PERCENTAGE VALUES AND NORMALIZE TO 0-1 RANGE
                var percentMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*%");
                if (percentMatch.Success)
                {
                    if (decimal.TryParse(percentMatch.Groups[1].Value, out var percentValue))
                    {
                        // Convert percentage to normalized decimal (0-1 range)
                        var normalizedValue = percentValue / 100m;

                        _logger.LogInformation($"✅ [PERCENT NORMALIZED] {fieldName}: '{text}' → {percentValue}% → {normalizedValue:F3} (normalized)");
                        return normalizedValue;
                    }
                }

                // Extract any numeric value (non-percentage)
                var numberMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)");
                if (numberMatch.Success)
                {
                    if (decimal.TryParse(numberMatch.Groups[1].Value, out var numericValue))
                    {
                        _logger.LogInformation($"✅ [NUMBER] {fieldName}: '{text}' → {numericValue}");
                        return numericValue;
                    }
                }

                _logger.LogWarning($"⚠️ [NO CONVERSION] Could not extract numeric value from '{text}'");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 [CONVERSION ERROR] Error converting '{text}' to numeric");
                return null;
            }
        }

        /// <summary>
        /// Main extraction method - Updated to return anchor calibration results
        /// </summary>
        public async Task<ExtractionResult> ExtractFieldsAsync(FileProcessInfo fileInfo)
        {
            var result = new ExtractionResult();

            try
            {
                _logger.LogInformation($"🎯 [VISUAL EXTRACTION START] File: {fileInfo.FileName}");

                // Get template with anchors
                var template = await _context.PdfTemplates
                    .Include(t => t.FieldMappings.Where(fm => fm.IsActive))
                    .Include(t => t.TemplateAnchors.Where(ta => ta.IsActive))
                    .FirstOrDefaultAsync(t => t.Id == fileInfo.TemplateId);

                if (template == null)
                {
                    result.ErrorMessage = "Template not found";
                    return result;
                }

                //_logger.LogInformation($"✅ [TEMPLATE] {template.Name} - {template.FieldMappings.Count} fields, {template.TemplateAnchors.Count} anchors");

                // Extract PDF text with coordinates
                var pdfTextData = ExtractPdfTextWithCoordinates(fileInfo.FilePath);
                if (pdfTextData == null)
                {
                    result.ErrorMessage = "Could not extract PDF text";
                    return result;
                }

                // 🛡️ GUARDIAN ANCHOR CALIBRATION with text-based confidence
                var transformation = CalculateCoordinateTransformation(template, pdfTextData);

                // 🎯 VISUAL EXTRACTION: Process each field
                foreach (var mapping in template.FieldMappings.OrderBy(fm => fm.DisplayOrder))
                {
                    _logger.LogInformation($"🎯 [PROCESSING] Field: {mapping.FieldName}");

                    var extractedValue = ExtractFieldValueWithTransformation(pdfTextData, mapping, transformation);

                    if (extractedValue != null)
                    {
                        result.ExtractedFields[mapping.FieldName] = extractedValue;
                        _logger.LogInformation($"✅ [SUCCESS] {mapping.FieldName}: {extractedValue}");
                    }
                    else
                    {
                        _logger.LogWarning($"❌ [FAILED] {mapping.FieldName}");
                    }
                }

                result.Success = result.ExtractedFields.Any();
                result.CalibrationConfidence = transformation.Confidence;

                // 🆕 CREATE ANCHOR CALIBRATION RESULTS
                result.AnchorResults = new AnchorCalibrationResult
                {
                    Confidence = transformation.Confidence,
                    AnchorsFound = transformation.AnchorMatches.Count,
                    AnchorsTotal = template.TemplateAnchors.Count,
                    AnchorsMatched = transformation.AnchorMatches.Count(a => a.IsTextMatch),
                    CoordinateOffsetX = (decimal)transformation.OffsetX,
                    CoordinateOffsetY = (decimal)transformation.OffsetY,
                    CoordinateScaleX = (decimal)transformation.ScaleX,
                    CoordinateScaleY = (decimal)transformation.ScaleY,
                    CoordinatesAdjusted = Math.Abs(transformation.OffsetX) > 0.1 || Math.Abs(transformation.OffsetY) > 0.1 ||
                                        Math.Abs(transformation.ScaleX - 1.0) > 0.001 || Math.Abs(transformation.ScaleY - 1.0) > 0.001,
                    Details = System.Text.Json.JsonSerializer.Serialize(transformation.AnchorMatches.Select(a => new
                    {
                        AnchorName = a.Anchor.Name,
                        ReferenceText = a.Anchor.ReferenceText,
                        ExtractedText = a.ExtractedText,
                        TextSimilarity = Math.Round(a.TextSimilarity, 4),
                        IsMatch = a.IsTextMatch,
                        MatchQuality = a.MatchQuality,
                        ConfiguredX = Math.Round(a.ConfiguredPosition.X, 2),
                        ConfiguredY = Math.Round(a.ConfiguredPosition.Y, 2),
                        FoundX = Math.Round(a.FoundPosition.X, 2),
                        FoundY = Math.Round(a.FoundPosition.Y, 2),
                        PageNumber = a.PageNumber
                    }).ToList(), new JsonSerializerOptions { WriteIndented = true })
                };

                if (result.Success)
                {
                    _logger.LogInformation($"🎯 [EXTRACTION COMPLETE] Successfully extracted {result.ExtractedFields.Count}/{template.FieldMappings.Count} fields");
                    _logger.LogInformation($"🛡️ [ANCHOR SUMMARY] {result.AnchorResults.AnchorsMatched}/{result.AnchorResults.AnchorsTotal} anchors matched with {result.AnchorResults.Confidence:P1} confidence");
                }
                else
                {
                    _logger.LogError($"❌ [NO EXTRACTIONS] No fields could be extracted using visual method");
                    result.ErrorMessage = "No fields could be extracted using visual coordinate method";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 [VISUAL EXTRACTION ERROR] {fileInfo.FileName}");
                result.ErrorMessage = ex.Message;

                // 🆕 Create empty anchor results on error
                result.AnchorResults = new AnchorCalibrationResult
                {
                    Confidence = 0.0m,
                    Details = $"Extraction failed: {ex.Message}"
                };
            }

            return result;
        }
    }



    // 🎯 DATA CLASSES
    public class PdfTextData
    {
        public Dictionary<int, PageTextData> PageTexts { get; set; } = new();
    }



    public class PageTextData
    {
        public int PageNumber { get; set; }
        public List<WordData> Words { get; set; } = new();
    }

    public class WordData
    {
        public string Text { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class ExtractedTextPart
    {
        public string Text { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Coverage { get; set; }
        public double IntersectionArea { get; set; }
    }

    public class CoordinateTransformation
    {
        public double OffsetX { get; set; } = 0;
        public double OffsetY { get; set; } = 0;
        public double ScaleX { get; set; } = 1.0;
        public double ScaleY { get; set; } = 1.0;
        public decimal Confidence { get; set; } = 1.0m;
        public double DistortionLevel { get; set; } = 0.0;
        public List<AnchorMatch> AnchorMatches { get; set; } = new List<AnchorMatch>();
    }

    public class Rectangle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    // 🛡️ GUARDIAN ANCHOR CALIBRATION CLASSES
    public class AnchorMatch
    {
        public TemplateAnchor Anchor { get; set; } = null!;
        public Point ConfiguredPosition { get; set; } = new Point();
        public Point FoundPosition { get; set; } = new Point();
        public int PageNumber { get; set; }

        // 🎯 NEW: Text-based properties
        public string ExtractedText { get; set; } = string.Empty;
        public double TextSimilarity { get; set; } = 0.0;
        public bool IsTextMatch { get; set; } = false;

        // Legacy coordinate-based distance (now less important)
        public double Distance => Math.Sqrt(Math.Pow(FoundPosition.X - ConfiguredPosition.X, 2) + Math.Pow(FoundPosition.Y - ConfiguredPosition.Y, 2));

        // 🎯 NEW: Text matching quality indicator
        public string MatchQuality
        {
            get
            {
                if (TextSimilarity >= 0.95) return "Excellent";
                if (TextSimilarity >= 0.8) return "Good";
                if (TextSimilarity >= 0.7) return "Fair";
                return "Poor";
            }
        }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    // 🎯 DATA CLASS: Add this class to your existing data classes section
    public class DetectedPhrase
    {
        public string Text { get; set; } = string.Empty;
        public List<WordData> Words { get; set; } = new List<WordData>();
        public Point TopLeft { get; set; } = new Point();
        public Rectangle BoundingBox { get; set; } = new Rectangle();

        public int WordCount => Words.Count;
        public double Width => BoundingBox.Width;
        public double Height => BoundingBox.Height;
    }
}