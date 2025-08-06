// ================================================================
// BULK FIELDS DRAWING - Complete File
// File: wizard-step3-drawing.js
// ================================================================

// Global variables for bulk mode (declare only once)
var bulkFieldsMode = false;
var bulkSelection = null;

// ✅ Toggle bulk fields mode
function toggleBulkFieldsMode() {
    bulkFieldsMode = !bulkFieldsMode;

    if (bulkFieldsMode) {
        $('#add-bulk-fields').removeClass('btn-outline-primary').addClass('btn-warning active');
        $('#add-bulk-fields').html('<i class="fa fa-stop me-1"></i>Cancel Bulk');

        // Switch to field mode if not already
        if (currentMappingMode !== 'field') {
            switchMappingMode('field');
        }

        // Show instruction
        alert.info('Select an area over table rows to auto-create fields', { popup: false });

        // Change cursor
        if (canvas) {
            $(canvas).css('cursor', 'copy');
        }
    } else {
        $('#add-bulk-fields').removeClass('btn-warning active').addClass('btn-outline-primary');
        $('#add-bulk-fields').html('<i class="fa fa-table me-1"></i>Add Bulk Fields');

        // Reset cursor
        if (canvas) {
            $(canvas).css('cursor', getCurrentModeCursor());
        }
    }
}

// ✅ Handle bulk fields selection
function handleBulkFieldsSelection(selection) {
    if (bulkFieldsMode && selection.width > 10 && selection.height > 10) {
        console.log('📋 [BULK MODE] Selection made, extracting text...');

        // Extract text from selected area
        extractTextFromArea(selection).then(extractedLines => {
            if (extractedLines && extractedLines.length > 0) {
                showBulkFieldsModal(extractedLines, selection);
            } else {
                alert.warning('No text found in selected area. Try selecting over text content.');
            }
        }).catch(error => {
            console.error('Error in bulk extraction:', error);
            alert.error('Error extracting text from selection');
        });

        return true; // Handled
    }
    return false; // Not handled
}

// ✅ Simple direct coordinate conversion (FIXED Y-AXIS)
function convertScreenToPdfSimple(selection) {
    // Get canvas position relative to container
    const canvasRect = canvas.getBoundingClientRect();
    const containerRect = $('#pdf-viewer-container')[0].getBoundingClientRect();

    // Calculate relative position within canvas
    const relativeX = selection.x;
    const relativeY = selection.y;

    console.log('🔍 [SIMPLE] Canvas rect:', canvasRect);
    console.log('🔍 [SIMPLE] Container rect:', containerRect);
    console.log('🔍 [SIMPLE] Selection relative to canvas:', relativeX, relativeY);

    // Convert canvas pixels to PDF coordinates using canvas actual dimensions
    const canvasDisplayWidth = parseFloat(canvas.style.width);
    const canvasDisplayHeight = parseFloat(canvas.style.height);

    console.log('🔍 [SIMPLE] Canvas display size:', canvasDisplayWidth, 'x', canvasDisplayHeight);

    // PDF page size (from viewport)
    return pdfDoc.getPage(currentPage).then(function (page) {
        const viewport = page.getViewport({ scale: 1.0 });

        console.log('🔍 [SIMPLE] PDF viewport:', viewport.width, 'x', viewport.height);

        // Direct proportion calculation
        const scaleX = viewport.width / canvasDisplayWidth;
        const scaleY = viewport.height / canvasDisplayHeight;

        console.log('🔍 [SIMPLE] Scale factors:', scaleX, scaleY);

        const pdfX = relativeX * scaleX;
        // ✅ FIX: PDF Y coordinates are inverted (0 = bottom, not top)
        const pdfY = viewport.height - ((relativeY + selection.height) * scaleY);
        const pdfWidth = selection.width * scaleX;
        const pdfHeight = selection.height * scaleY;

        console.log('🔍 [SIMPLE] Before Y inversion - relativeY:', relativeY, 'selection.height:', selection.height);
        console.log('🔍 [SIMPLE] After Y inversion - pdfY:', pdfY);
        console.log('🔍 [SIMPLE] Final PDF coords:', pdfX, pdfY, pdfWidth, pdfHeight);

        return {
            x: parseFloat(pdfX.toFixed(2)),
            y: parseFloat(pdfY.toFixed(2)),
            width: parseFloat(pdfWidth.toFixed(2)),
            height: parseFloat(pdfHeight.toFixed(2))
        };
    });
}

// ✅ Extract text from PDF selection area (WITH COORDINATE DEBUG AND TEXT POSITIONING)
async function extractTextFromArea(selectionArea) {
    if (!pdfDoc || !canvas) {
        console.error('PDF not loaded');
        return null;
    }

    try {
        console.log('📄 [BULK EXTRACT] Starting text extraction...');
        console.log('📄 [BULK EXTRACT] Selection area (screen):', selectionArea);

        // ✅ DEBUG: Log current zoom and canvas info
        console.log('🔍 [DEBUG] Current zoom:', currentZoom);
        console.log('🔍 [DEBUG] Canvas dimensions:', canvas.width, 'x', canvas.height);
        console.log('🔍 [DEBUG] Canvas style dimensions:', canvas.style.width, 'x', canvas.style.height);

        const container = $('#pdf-viewer-container');
        console.log('🔍 [DEBUG] Container dimensions:', container.width(), 'x', container.height());
        console.log('🔍 [DEBUG] Container scroll:', container.scrollLeft(), ',', container.scrollTop());

        // Convert screen selection to PDF coordinates
        const pdfCoords = await screenToPdfCoordinates(
            selectionArea.x,
            selectionArea.y,
            selectionArea.width,
            selectionArea.height
        );

        console.log('📐 [BULK COORDS] PDF coordinates:', pdfCoords);
        console.log('📦 [SELECTION BOUNDS] From (', pdfCoords.x, ',', pdfCoords.y, ') to (',
            pdfCoords.x + pdfCoords.width, ',', pdfCoords.y + pdfCoords.height, ')');

        // ✅ DEBUG: Show the conversion ratio
        const scaleX = pdfCoords.width / selectionArea.width;
        const scaleY = pdfCoords.height / selectionArea.height;
        console.log('🔍 [DEBUG] Conversion scale - X:', scaleX.toFixed(4), 'Y:', scaleY.toFixed(4));

        // Get the current page
        const page = await pdfDoc.getPage(currentPage);
        const viewport = page.getViewport({ scale: 1.0 });

        console.log('📄 [PAGE INFO] Page dimensions:', {
            width: viewport.width,
            height: viewport.height
        });

        // ✅ NEW: Use simple direct conversion
        const simplePdfCoords = await convertScreenToPdfSimple(selectionArea);
        console.log('🎯 [SIMPLE] Simple conversion:', simplePdfCoords);

        // Use the simple conversion method
        const finalCoords = simplePdfCoords;

        console.log('🎯 [USING] Final coordinates:', finalCoords);

        // Get text content with position information
        const textContent = await page.getTextContent();

        console.log('📝 [BULK TEXT] Found', textContent.items.length, 'text items on page');

        // Extract text items with multiple fallback strategies
        let textItems = [];
        let strategy = '';

        // Define selection boundaries using final coordinates
        const selectionLeft = finalCoords.x;
        const selectionRight = finalCoords.x + finalCoords.width;
        const selectionTop = finalCoords.y;
        const selectionBottom = finalCoords.y + finalCoords.height;

        // Strategy 1: Strict bounds - text completely within selection
        console.log('🎯 [STRATEGY 1] Trying strict bounds...');
        textContent.items.forEach((item) => {
            if (!item.transform || item.transform.length < 6) return;
            if (!item.str || !item.str.trim()) return;

            const textX = item.transform[4];
            const textY = item.transform[5];
            const textWidth = item.width || (item.str.length * 6);
            const textHeight = item.height || 12;

            const textLeft = textX;
            const textRight = textX + textWidth;
            const textTop = textY;
            const textBottom = textY + textHeight;

            const isCompletelyInside = (
                textLeft >= selectionLeft &&
                textRight <= selectionRight &&
                textTop >= selectionTop &&
                textBottom <= selectionBottom
            );

            if (isCompletelyInside) {
                console.log(`✅ [FOUND] "${item.str}" at (${textX.toFixed(1)}, ${textY.toFixed(1)})`);
                textItems.push({
                    text: item.str.trim(),
                    x: textX,
                    y: textY,
                    width: textWidth,
                    height: textHeight
                });
            }
        });

        if (textItems.length > 0) {
            strategy = 'Strict bounds';
            console.log(`✅ [STRATEGY 1] Found ${textItems.length} items with strict bounds`);
        } else {
            // Strategy 2: Center point within selection
            console.log('🎯 [STRATEGY 2] Trying center-point check...');
            textContent.items.forEach((item) => {
                if (!item.transform || item.transform.length < 6) return;
                if (!item.str || !item.str.trim()) return;

                const textX = item.transform[4];
                const textY = item.transform[5];
                const textWidth = item.width || (item.str.length * 6);
                const textHeight = item.height || 12;

                const centerX = textX + textWidth / 2;
                const centerY = textY + textHeight / 2;

                const centerInside = (
                    centerX >= selectionLeft && centerX <= selectionRight &&
                    centerY >= selectionTop && centerY <= selectionBottom
                );

                if (centerInside) {
                    console.log(`✅ [CENTER] "${item.str}" at (${textX.toFixed(1)}, ${textY.toFixed(1)})`);
                    textItems.push({
                        text: item.str.trim(),
                        x: textX,
                        y: textY,
                        width: textWidth,
                        height: textHeight
                    });
                }
            });

            if (textItems.length > 0) {
                strategy = 'Center point';
                console.log(`✅ [STRATEGY 2] Found ${textItems.length} items with center-point method`);
            } else {
                // Strategy 3: Overlap with tolerance
                console.log('🎯 [STRATEGY 3] Trying overlap with tolerance...');
                const tolerance = 10;

                textContent.items.forEach((item) => {
                    if (!item.transform || item.transform.length < 6) return;
                    if (!item.str || !item.str.trim()) return;

                    const textX = item.transform[4];
                    const textY = item.transform[5];
                    const textWidth = item.width || (item.str.length * 6);
                    const textHeight = item.height || 12;

                    const overlapsX = textX < (selectionRight + tolerance) && (textX + textWidth) > (selectionLeft - tolerance);
                    const overlapsY = textY < (selectionBottom + tolerance) && (textY + textHeight) > (selectionTop - tolerance);

                    if (overlapsX && overlapsY) {
                        console.log(`✅ [OVERLAP] "${item.str}" at (${textX.toFixed(1)}, ${textY.toFixed(1)})`);
                        textItems.push({
                            text: item.str.trim(),
                            x: textX,
                            y: textY,
                            width: textWidth,
                            height: textHeight
                        });
                    }
                });

                strategy = textItems.length > 0 ? 'Overlap with tolerance' : 'No strategy worked';
            }
        }

        if (textItems.length === 0) {
            console.error('❌ [NO TEXT] No text found with any strategy');
            console.log('🔍 [DEBUG] Try showing some nearby text for reference...');

            // Show first 10 text items for debugging
            textContent.items.slice(0, 10).forEach((item, i) => {
                if (item.transform && item.transform.length >= 6 && item.str.trim()) {
                    console.log(`📍 [NEARBY ${i}] "${item.str}" at (${item.transform[4].toFixed(1)}, ${item.transform[5].toFixed(1)})`);
                }
            });

            return null;
        }

        console.log(`📋 [SUCCESS] Using strategy: ${strategy}`);

        // ✅ USE ACTUAL TEXT POSITIONS: Calculate bounds from found text, not selection
        console.log('📊 [TEXT BOUNDS] Calculating actual text area from found items...');

        const textYPositions = textItems.map(item => item.y);
        const textXPositions = textItems.map(item => item.x);
        const textWidths = textItems.map(item => item.width);
        const textHeights = textItems.map(item => item.height);

        const actualTextBounds = {
            minX: Math.min(...textXPositions),
            maxX: Math.max(...textXPositions.map((x, i) => x + textWidths[i])),
            minY: Math.min(...textYPositions),
            maxY: Math.max(...textYPositions.map((y, i) => y + textHeights[i])),
        };

        actualTextBounds.width = actualTextBounds.maxX - actualTextBounds.minX;
        actualTextBounds.height = actualTextBounds.maxY - actualTextBounds.minY;

        console.log('📊 [TEXT BOUNDS] Actual text area:', actualTextBounds);
        console.log('📊 [TEXT BOUNDS] Y range:', actualTextBounds.minY, 'to', actualTextBounds.maxY);

        // ✅ EXCEL COLUMN MODE: Group text items by Y position with tight tolerance
        const lineGroups = {};
        const yTolerance = 3; // Very tight tolerance for Excel-like precision

        console.log('📊 [EXCEL MODE] Grouping text with tight Y-tolerance:', yTolerance);

        textItems.forEach(item => {
            // Round Y position to nearest tolerance for grouping
            const lineKey = Math.round(item.y / yTolerance) * yTolerance;
            if (!lineGroups[lineKey]) {
                lineGroups[lineKey] = [];
            }
            lineGroups[lineKey].push(item);
            console.log(`📊 [GROUP] "${item.text}" assigned to line ${lineKey.toFixed(1)}`);
        });

        // Sort lines by Y position (top to bottom in PDF coordinates)
        const sortedLineKeys = Object.keys(lineGroups)
            .map(key => parseFloat(key))
            .sort((a, b) => b - a); // PDF Y: higher = top

        console.log('📊 [LINES] Found', sortedLineKeys.length, 'distinct lines at Y positions:', sortedLineKeys.map(y => y.toFixed(1)));

        // ✅ STORE TEXT WITH ACTUAL Y POSITIONS: Process each line and store actual coordinates
        const textLinesWithPositions = [];
        sortedLineKeys.forEach((lineY, lineIndex) => {
            const lineItems = lineGroups[lineY];

            // Sort items in line by X position (left to right)
            lineItems.sort((a, b) => a.x - b.x);

            // ✅ EXCEL MODE: Only combine text that's horizontally close
            const combinedItems = [];
            let currentGroup = [lineItems[0]];

            for (let i = 1; i < lineItems.length; i++) {
                const currentItem = lineItems[i];
                const previousItem = lineItems[i - 1];

                // Check horizontal distance between items
                const horizontalGap = currentItem.x - (previousItem.x + previousItem.width);
                const maxGap = 10; // Maximum gap to consider same "cell"

                if (horizontalGap <= maxGap) {
                    // Items are close, add to current group
                    currentGroup.push(currentItem);
                } else {
                    // Gap too large, finish current group and start new one
                    if (currentGroup.length > 0) {
                        const groupText = currentGroup.map(item => item.text).join(' ').trim();
                        if (groupText) {
                            // ✅ STORE ACTUAL POSITION: Use the actual Y position of the text
                            const avgHeight = currentGroup.reduce((sum, item) => sum + item.height, 0) / currentGroup.length;
                            textLinesWithPositions.push({
                                text: groupText,
                                y: lineY,
                                height: avgHeight
                            });
                        }
                    }
                    currentGroup = [currentItem];
                }
            }

            // Don't forget the last group
            if (currentGroup.length > 0) {
                const groupText = currentGroup.map(item => item.text).join(' ').trim();
                if (groupText) {
                    const avgHeight = currentGroup.reduce((sum, item) => sum + item.height, 0) / currentGroup.length;
                    textLinesWithPositions.push({
                        text: groupText,
                        y: lineY,
                        height: avgHeight
                    });
                }
            }
        });

        // Extract just the text for the return value (keeping original behavior)
        const textLines = textLinesWithPositions.map(line => line.text);

        textLinesWithPositions.forEach((line, index) => {
            console.log(`📝 [LINE ${index}] Y:${line.y.toFixed(1)} H:${line.height.toFixed(1)} "${line.text}"`);
        });

        console.log(`📋 [FINAL] Extracted ${textLines.length} text lines in Excel column mode`);

        // ✅ STORE POSITIONS: Store the text positions for field creation
        if (textLines.length > 0) {
            window.lastTextExtractionData = {
                textLines: textLines,
                textPositions: textLinesWithPositions,
                actualBounds: actualTextBounds,
                selectionBounds: finalCoords
            };
        }

        return textLines.length > 0 ? textLines : null;

    } catch (error) {
        console.error('❌ [BULK EXTRACT] Error:', error);
        return null;
    }
}

// ✅ Show bulk fields modal
function showBulkFieldsModal(extractedLines, selectionArea) {
    bulkSelection = selectionArea;

    const extractedText = extractedLines.join('\n');

    $('#bulk-modal-title').text('Bulk Fields - Edit Extracted Text');
    $('#bulk-extracted-text').val(extractedText);
    $('#bulk-line-count').text(extractedLines.length);

    $('#bulk-fields-modal').modal('show');
    setTimeout(() => $('#bulk-extracted-text').focus(), 300);
}

// ✅ FIXED: Clean field name to capitalize and preserve spaces, only remove special chars
function cleanFieldName(rawText) {
    if (!rawText || !rawText.trim()) return 'Field';

    // 1. Trim whitespace
    let cleaned = rawText.trim();

    // 2. Remove special characters but keep spaces and alphanumeric
    cleaned = cleaned.replace(/[^a-zA-Z0-9\s]/g, '');

    // 3. Capitalize first letter of each word
    cleaned = cleaned.replace(/\b\w+/g, function (word) {
        return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
    });

    // 4. Ensure it's not empty after cleaning
    if (!cleaned.trim()) {
        cleaned = 'Field';
    }

    console.log(`🏷️ [FIELD NAME] "${rawText}" → "${cleaned}"`);
    return cleaned;
}

// ✅ FINAL WORKING VERSION - Replace your existing createBulkFields function
async function createBulkFields() {
    const editedText = $('#bulk-extracted-text').val().trim();
    const lines = editedText.split('\n').filter(line => line.trim());

    if (lines.length === 0) {
        alert.warning('Please enter field names (one per line)');
        return;
    }

    if (!bulkSelection) {
        alert.error('Selection area lost. Please try again.');
        return;
    }

    try {
        $('#create-bulk-fields').prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-1"></i>Creating...');

        console.log('🏗️ [BULK CREATE] Creating', lines.length, 'fields');

        // ✅ USE SAME METHOD AS REGULAR FIELDS: Convert screen to PDF coordinates
        const pdfCoords = await screenToPdfCoordinates(
            bulkSelection.x,
            bulkSelection.y,
            bulkSelection.width,
            bulkSelection.height
        );

        console.log('📐 [BULK COORDS] Using SAME method as regular fields:', pdfCoords);

        // ✅ EXACT HEIGHT DIVISION: Divide the selected area equally
        const fieldWidth = pdfCoords.width;
        const fieldHeight = pdfCoords.height / lines.length;

        console.log('📐 [BULK DIMS] Field size:', fieldWidth, 'x', fieldHeight);

        // ✅ Track created field IDs for auto-selection
        const createdFieldIds = [];

        // Create fields using the SAME coordinate system as regular fields
        for (let i = 0; i < lines.length; i++) {
            // ✅ FIXED: Use clean field name function
            const rawText = lines[i].trim();
            const cleanedName = cleanFieldName(rawText);
            const fieldY = pdfCoords.y + (i * fieldHeight);

            const fieldData = {
                FieldName: cleanedName,
                DisplayName: cleanedName,
                DataType: 'number',
                Description: `Auto-generated from bulk selection (line ${i + 1})`,
                X: pdfCoords.x,
                Y: fieldY,
                Width: fieldWidth,
                Height: fieldHeight,
                PageNumber: currentPage,
                IsRequired: false,
                BorderColor: '#A54EE1',
                IsVisible: true
            };

            console.log(`🏗️ [BULK FIELD ${i + 1}] Creating "${cleanedName}" at (${fieldData.X}, ${fieldData.Y})`);

            const result = await addFieldMappingAjax(fieldData);

            if (result.success) {
                const serverField = result.data;

                const newField = {
                    id: serverField.Id,
                    fieldName: serverField.FieldName,
                    displayName: serverField.DisplayName,
                    dataType: (serverField.DataType || 'number').toLowerCase(),
                    description: serverField.Description || '',
                    x: serverField.X || fieldData.X,
                    y: serverField.Y || fieldData.Y,
                    width: serverField.Width || fieldData.Width,
                    height: serverField.Height || fieldData.Height,
                    pageNumber: serverField.PageNumber || fieldData.PageNumber,
                    isRequired: serverField.IsRequired || false,
                    borderColor: serverField.BorderColor || '#A54EE1',
                    isVisible: serverField.IsVisible !== false,
                    coordinateType: 'pdf-absolute'
                };

                fieldMappings.push(newField);
                createdFieldIds.push(newField.id);
                console.log(`✅ [BULK CREATED ${i + 1}] Field "${cleanedName}" created at: (${newField.x}, ${newField.y})`);
            } else {
                console.error(`❌ [BULK FIELD ${i + 1}] Failed:`, result.message);
                alert.error(`Failed to create field "${cleanedName}": ${result.message}`);
                break;
            }
        }

        // ✅ FORCE UI UPDATE: Ensure overlays are rendered first
        renderFieldOverlays();
        updateFieldCount();
        updateFieldsTable();

        // ✅ WAIT FOR DOM: Ensure overlays exist before selection
        await new Promise(resolve => setTimeout(resolve, 150));

        // ✅ AUTO-SELECTION: Use existing functions with proper timing
        if (createdFieldIds.length > 0) {
            console.log('🎯 [AUTO-SELECT] Selecting', createdFieldIds.length, 'new bulk fields');

            // Clear existing selections using existing function
            if (typeof clearMultiSelection === 'function') {
                clearMultiSelection();
            }

            // Wait a bit more for clear to complete
            await new Promise(resolve => setTimeout(resolve, 50));

            // Add each field to multi-selection using existing function
            createdFieldIds.forEach((fieldId, index) => {
                console.log(`🎯 [SELECT ${index + 1}] Adding field ${fieldId} to multi-selection`);

                // Use existing addToMultiSelection function
                if (typeof addToMultiSelection === 'function') {
                    addToMultiSelection('field', fieldId);
                } else {
                    // Fallback: direct manipulation if function doesn't exist
                    if (typeof multiSelection !== 'undefined') {
                        multiSelection.selectedFields.add(fieldId);
                        multiSelection.isActive = true;
                    }

                    // Apply styling directly
                    $(`.field-overlay[data-field-id="${fieldId}"]`).addClass('multi-selected');
                }
            });

            // Update UI using existing function
            setTimeout(() => {
                if (typeof updateMultiSelectionUI === 'function') {
                    updateMultiSelectionUI();
                }

                // ✅ VERIFY AND FIX STYLING: Ensure all fields are properly styled
                const expectedCount = createdFieldIds.length;
                const actualCount = $('.field-overlay.multi-selected').length;

                console.log(`✅ [VERIFY] Expected: ${expectedCount}, Actual: ${actualCount} styled fields`);

                if (actualCount !== expectedCount) {
                    console.warn('⚠️ [FIXING] Styling mismatch, applying direct styling...');

                    // Force apply styling to any missing fields
                    createdFieldIds.forEach(fieldId => {
                        const overlay = $(`.field-overlay[data-field-id="${fieldId}"]`);
                        if (!overlay.hasClass('multi-selected')) {
                            overlay.addClass('multi-selected');
                            console.log(`🔧 [FIXED] Added styling to field ${fieldId}`);
                        }
                    });
                }

                // Final verification
                const finalCount = $('.field-overlay.multi-selected').length;
                console.log(`✅ [FINAL] ${finalCount} fields properly styled and selected`);

            }, 100);

            console.log('✅ [AUTO-SELECT] All bulk fields processed for selection');
        }

        // Hide modal and reset
        $('#bulk-fields-modal').modal('hide');
        toggleBulkFieldsMode();
        bulkSelection = null;

        alert.success(`Created ${lines.length} fields successfully and selected for editing!`);

    } catch (error) {
        console.error('❌ [BULK CREATE] Error:', error);
        alert.error('Error creating bulk fields');
    } finally {
        $('#create-bulk-fields').prop('disabled', false).html('<i class="fa fa-magic me-1"></i>Create Fields');
    }
}

// ✅ Make functions globally available
window.toggleBulkFieldsMode = toggleBulkFieldsMode;
window.createBulkFields = createBulkFields;
window.handleBulkFieldsSelection = handleBulkFieldsSelection;
window.cleanFieldName = cleanFieldName;