// ================================================================
// MULTI-SELECTION EXTENSION - Separate Move & Resize Operations
// File: wizard-step3-multiselect.js
// ================================================================

// ✅ Multi-selection specific variables
var multiSelection = {
    isActive: false,
    selectedFields: new Set(),
    selectedAnchors: new Set(),
    isDragging: false,
    isResizing: false,
    dragStartPos: { x: 0, y: 0 },
    originalStates: new Map(),
    operationMode: 'none' // 'drag' or 'resize' or 'none'
};

// ✅ Multi-selection visual feedback
var multiSelectStyles = `
    <style id="multi-select-styles">
        .field-overlay.multi-selected {
            border-color: #FF375E !important;
            border-width: 3px !important;
            box-shadow: 0 0 8px rgba(255, 55, 94, 0.5) !important;
            z-index: 100 !important;
        }
        
        .anchor-overlay.multi-selected {
            border-color: #EF7945 !important;
            border-width: 3px !important;
            box-shadow: 0 0 8px rgba(239, 121, 69, 0.5) !important;
            z-index: 101 !important;
        }
        
        .multi-select-info-panel {
            position: fixed;
            top: 10px;
            right: 10px;
            background: #20252A;
            color: white;
            padding: 8px 12px;
            border-radius: 6px;
            font-size: 12px;
            z-index: 1000;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
        }
        
        .ctrl-hint {
            position: fixed;
            bottom: 20px;
            left: 50%;
            transform: translateX(-50%);
            background: #4F008C;
            color: white;
            padding: 6px 12px;
            border-radius: 4px;
            font-size: 11px;
            z-index: 999;
            opacity: 0;
            transition: opacity 0.3s;
        }
        
        .ctrl-hint.show {
            opacity: 1;
        }
    </style>
`;

 
// ================================================================
// FIXES FOR wizard-step3-multiselect.js ONLY
// ================================================================

// ✅ REPLACE: clearMultiSelection function
function clearMultiSelection() {
    console.log('🔥 [MULTI] Clearing all selections (multi + single)');

    // Clear multi-selection state
    multiSelection.selectedFields.clear();
    multiSelection.selectedAnchors.clear();
    multiSelection.isActive = false;
    multiSelection.isDragging = false;
    multiSelection.isResizing = false;
    multiSelection.operationMode = 'none';
    multiSelection.originalStates.clear();

    $('.field-table-row, .anchor-table-row').removeClass('table-active bg-primary text-white');

    // ✅ FIX: Clear single selection state too
    if (typeof selectedFieldId !== 'undefined') {
        selectedFieldId = null;
    }
    if (typeof selectedAnchorId !== 'undefined') {
        selectedAnchorId = null;
    }

    // ✅ FIX: Remove ALL selection styling
    $('.field-overlay, .anchor-overlay').removeClass('multi-selected selected');

    updateMultiSelectionUI();
}

// ================================================================
// PDF-ONLY CLEAR SELECTION - Only clear when clicking inside PDF canvas
// Replace in your setupMultiSelectEventHandlers function
// ================================================================

function setupMultiSelectEventHandlers() {
    console.log('🔥 [MULTI] Setting up multi-select handlers - PDF-only clear');

    // ✅ All your existing field/anchor click handlers remain the same
    $(document).on('mousedown', '.field-overlay', function (e) {
        const fieldId = parseInt($(this).data('field-id'));

        if (e.ctrlKey || e.metaKey) {
            e.preventDefault();
            e.stopPropagation();

            if (!multiSelection.isActive && typeof selectedFieldId !== 'undefined' && selectedFieldId && selectedFieldId !== fieldId) {
                addToMultiSelection('field', selectedFieldId);
            }

            if (!multiSelection.isActive && typeof selectedAnchorId !== 'undefined' && selectedAnchorId) {
                addToMultiSelection('anchor', selectedAnchorId);
            }

            if (multiSelection.selectedFields.has(fieldId)) {
                removeFromMultiSelection('field', fieldId);
            } else {
                addToMultiSelection('field', fieldId);
            }

            updateMultiSelectionUI();
            return false;

        } else {
            const isMultiSelected = multiSelection.selectedFields.has(fieldId);
            const hasMultipleSelected = multiSelection.selectedFields.size > 1;

            if (isMultiSelected && hasMultipleSelected) {
                const rect = this.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;

                if (x >= rect.width - 10 && y >= rect.height - 10) {
                    multiSelection.operationMode = 'resize';
                    startMultiResize(e, fieldId, 'se');
                } else if (x >= rect.width - 10) {
                    multiSelection.operationMode = 'resize';
                    startMultiResize(e, fieldId, 'e');
                } else if (y >= rect.height - 10) {
                    multiSelection.operationMode = 'resize';
                    startMultiResize(e, fieldId, 's');
                } else {
                    multiSelection.operationMode = 'move';
                    startMultiMove(e, fieldId);
                }

                e.preventDefault();
                e.stopPropagation();
                return false;

            } else {
                if (multiSelection.selectedFields.size > 0 || multiSelection.selectedAnchors.size > 0) {
                    clearMultiSelection();
                }

                if (typeof selectField === 'function') {
                    selectField(fieldId);
                }
            }
        }
    });

    $(document).on('mousedown', '.anchor-overlay', function (e) {
        const anchorId = parseInt($(this).data('anchor-id'));

        if (e.ctrlKey || e.metaKey) {
            e.preventDefault();
            e.stopPropagation();

            if (!multiSelection.isActive && typeof selectedAnchorId !== 'undefined' && selectedAnchorId && selectedAnchorId !== anchorId) {
                addToMultiSelection('anchor', selectedAnchorId);
            }

            if (!multiSelection.isActive && typeof selectedFieldId !== 'undefined' && selectedFieldId) {
                addToMultiSelection('field', selectedFieldId);
            }

            if (multiSelection.selectedAnchors.has(anchorId)) {
                removeFromMultiSelection('anchor', anchorId);
            } else {
                addToMultiSelection('anchor', anchorId);
            }

            updateMultiSelectionUI();
            return false;

        } else {
            const isMultiSelected = multiSelection.selectedAnchors.has(anchorId);
            const hasMultipleSelected = multiSelection.selectedAnchors.size > 1;

            if (isMultiSelected && hasMultipleSelected) {
                const rect = this.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;

                if (x >= rect.width - 10 && y >= rect.height - 10) {
                    multiSelection.operationMode = 'resize';
                    startMultiResize(e, anchorId, 'se');
                } else if (x >= rect.width - 10) {
                    multiSelection.operationMode = 'resize';
                    startMultiResize(e, anchorId, 'e');
                } else if (y >= rect.height - 10) {
                    multiSelection.operationMode = 'resize';
                    startMultiResize(e, anchorId, 's');
                } else {
                    multiSelection.operationMode = 'move';
                    startMultiMove(e, anchorId);
                }

                e.preventDefault();
                e.stopPropagation();
                return false;

            } else {
                if (multiSelection.selectedFields.size > 0 || multiSelection.selectedAnchors.size > 0) {
                    clearMultiSelection();
                }

                if (typeof selectAnchor === 'function') {
                    selectAnchor(anchorId);
                }
            }
        }
    });

    // Mouse move and up handlers (unchanged)
    $(document).on('mousemove', function (e) {
        if (multiSelection.operationMode === 'move' && multiSelection.isDragging) {
            performMultiMove(e);
            e.stopPropagation();
            return false;
        } else if (multiSelection.operationMode === 'resize' && multiSelection.isResizing) {
            performMultiResize(e);
            e.stopPropagation();
            return false;
        }
    });

    $(document).on('mouseup', function (e) {
        if (multiSelection.operationMode !== 'none') {
            endMultiOperation();
            e.stopPropagation();
            return false;
        }
    });

    // ✅ FIXED: Only clear selections when clicking on PDF canvas empty area
    $(document).on('mousedown', '#pdf-canvas', function (e) {
        // ✅ PRECISE: Only if clicking directly on the canvas (not overlays)
        if (e.target === this && !e.ctrlKey && !e.metaKey) {

            // ✅ DOUBLE CHECK: Make sure no overlays are at this position
            const rect = this.getBoundingClientRect();
            const canvasX = e.clientX - rect.left;
            const canvasY = e.clientY - rect.top;

            // Check if there's any overlay at this position
            const overlayAtPoint = $('.field-overlay, .anchor-overlay').filter(function () {
                const overlayRect = this.getBoundingClientRect();
                const overlayX = e.clientX - overlayRect.left;
                const overlayY = e.clientY - overlayRect.top;

                return overlayX >= 0 && overlayX <= overlayRect.width &&
                    overlayY >= 0 && overlayY <= overlayRect.height;
            });

            // Only clear if no overlay was clicked
            if (overlayAtPoint.length === 0) {
                console.log('📋 [PDF-ONLY] Clearing selections - clicked on empty PDF canvas');
                clearMultiSelection();
            } else {
                console.log('📋 [PDF-ONLY] Not clearing - clicked on overlay');
            }
        }
    });

    // ✅ REMOVED: No more container click handler
    // ✅ REMOVED: No more document-wide click handler
    // Only PDF canvas clicks can clear selections now!
}

// ================================================================
// COMPLETE setupMultiSelectKeyboardHandlers - Replace your existing one
// ================================================================
function setupMultiSelectKeyboardHandlers() {
    $(document).on('keydown', function (e) {
        // Show Ctrl hint
        if (e.ctrlKey || e.metaKey) {
            $('#ctrl-hint').addClass('show');
        }

        // ESC - Clear selection
        if (e.keyCode === 27) {
            clearMultiSelection();
        }

        // Delete - Delete selected items
        if (e.keyCode === 46) {
            deleteMultiSelection();
        }

        // Ctrl+A - Select all
        if ((e.ctrlKey || e.metaKey) && e.keyCode === 65) {
            e.preventDefault();
            selectAllItems();
        }

        // ✅ NEW: Ctrl+M - Add current selection to multi-select
        if ((e.ctrlKey || e.metaKey) && e.keyCode === 77) {
            e.preventDefault();

            // Add currently selected field
            if (typeof selectedFieldId !== 'undefined' && selectedFieldId && !multiSelection.selectedFields.has(selectedFieldId)) {
                console.log('⌨️ [SHORTCUT] Adding current field to multi-selection:', selectedFieldId);
                addToMultiSelection('field', selectedFieldId);
            }

            // Add currently selected anchor
            if (typeof selectedAnchorId !== 'undefined' && selectedAnchorId && !multiSelection.selectedAnchors.has(selectedAnchorId)) {
                console.log('⌨️ [SHORTCUT] Adding current anchor to multi-selection:', selectedAnchorId);
                addToMultiSelection('anchor', selectedAnchorId);
            }

            updateMultiSelectionUI();
        }
    });

    $(document).on('keyup', function (e) {
        if (!e.ctrlKey && !e.metaKey) {
            $('#ctrl-hint').removeClass('show');
        }
    });
}

// ================================================================
// COMPLETE selectAllItems - Replace your existing one
// ================================================================
function selectAllItems() {
    console.log('🔥 [MULTI] Selecting all items on page', currentPage);

    clearMultiSelection();

    const currentPageFields = fieldMappings.filter(f => f.pageNumber === currentPage);
    currentPageFields.forEach(field => {
        addToMultiSelection('field', field.id);
    });

    const currentPageAnchors = anchorPoints.filter(a => a.pageNumber === currentPage);
    currentPageAnchors.forEach(anchor => {
        addToMultiSelection('anchor', anchor.id);
    });

    updateMultiSelectionUI();

    // Force styling restoration
    setTimeout(() => {
        if (typeof restoreAllSelectionStyling === 'function') {
            restoreAllSelectionStyling();
        }
    }, 50);
}

// ================================================================
// COMPLETE updateMultiSelectionUI - Replace your existing one
// ================================================================
function updateMultiSelectionUI() {
    const totalSelected = multiSelection.selectedFields.size + multiSelection.selectedAnchors.size;

    if (totalSelected > 1) {
        $('#multi-select-info').show();
        $('#multi-select-count').text(`${totalSelected} items selected`);
    } else {
        $('#multi-select-info').hide();
    }
}

// ================================================================
// COMPLETE initializeMultiSelection - Replace your existing one
// ================================================================
function initializeMultiSelection() {
    console.log('🔥 [MULTI] Initializing enhanced multi-selection system');

    // Add styles to head if not exists
    if ($('#multi-select-styles').length === 0) {
        $('head').append(`
            <style id="multi-select-styles">
                .field-overlay.multi-selected {
                    border-color: #FF375E !important;
                    border-width: 3px !important;
                    box-shadow: 0 0 8px rgba(255, 55, 94, 0.5) !important;
                    z-index: 100 !important;
                }
                
                .anchor-overlay.multi-selected {
                    border-color: #EF7945 !important;
                    border-width: 3px !important;
                    box-shadow: 0 0 8px rgba(239, 121, 69, 0.5) !important;
                    z-index: 101 !important;
                }
                
                .multi-select-info-panel {
                    position: fixed;
                    top: 10px;
                    right: 10px;
                    background: #20252A;
                    color: white;
                    padding: 8px 12px;
                    border-radius: 6px;
                    font-size: 12px;
                    z-index: 1000;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                }
                
                .ctrl-hint {
                    position: fixed;
                    bottom: 20px;
                    left: 50%;
                    transform: translateX(-50%);
                    background: #4F008C;
                    color: white;
                    padding: 6px 12px;
                    border-radius: 4px;
                    font-size: 11px;
                    z-index: 999;
                    opacity: 0;
                    transition: opacity 0.3s;
                    white-space: nowrap;
                }
                
                .ctrl-hint.show {
                    opacity: 1;
                }
            </style>
        `);
    }

    // Add hint element if not exists
    if ($('#ctrl-hint').length === 0) {
        $('body').append('<div id="ctrl-hint" class="ctrl-hint">Ctrl+Click: Multi-select | Ctrl+M: Add current | Ctrl+A: Select all</div>');
    }

    // Setup event handlers
    setupMultiSelectEventHandlers();
    setupMultiSelectKeyboardHandlers();

    console.log('✅ [MULTI] Enhanced multi-selection initialized with auto-include current selection');
}
 
 

// ✅ Start multi-move operation
function startMultiMove(e, itemId) {
    console.log('🚀 [MULTI-MOVE] Starting multi-move operation');

    multiSelection.isDragging = true;
    multiSelection.dragStartPos = { x: e.clientX, y: e.clientY };
    multiSelection.operationMode = 'move';

    captureOriginalStates();
    $('body').addClass('dragging').css('user-select', 'none');
}
 

// ✅ Start multi-resize operation
function startMultiResize(e, itemId, handle) {
    console.log('📏 [MULTI-RESIZE] Starting multi-resize operation');

    multiSelection.isResizing = true;
    multiSelection.dragStartPos = { x: e.clientX, y: e.clientY };
    multiSelection.operationMode = 'resize';
    multiSelection.resizeHandle = handle;

    captureOriginalStates();
    $('body').addClass('dragging').css('user-select', 'none');
}

 

// ✅ End multi-operation
function endMultiOperation() {
    console.log('🏁 [MULTI] Ending multi-operation, mode was:', multiSelection.operationMode);

    if (multiSelection.operationMode !== 'none') {
        saveMultiSelectionChanges();

        multiSelection.isDragging = false;
        multiSelection.isResizing = false;
        multiSelection.operationMode = 'none';
        multiSelection.resizeHandle = '';

        $('body').removeClass('dragging').css('user-select', '');
        updateMultiSelectionUI();
    }
}

// ✅ Separate move and resize detection (not used - we handle it directly)
function setupMultiSelectMoveDetection() {
    // Not needed - we handle move detection directly in event handlers
}

function setupMultiSelectResizeDetection() {
    // Not needed - we handle resize detection directly in event handlers
}

// ✅ Add item to multi-selection
function addToMultiSelection(type, id) {
    if (type === 'field') {
        multiSelection.selectedFields.add(id);
        applyMultiSelectStyle('field', id);
    } else if (type === 'anchor') {
        multiSelection.selectedAnchors.add(id);
        applyMultiSelectStyle('anchor', id);
    }

    multiSelection.isActive = true;
    console.log('🔥 [MULTI] Added to selection:', type, id);
}

// ✅ Remove item from multi-selection
function removeFromMultiSelection(type, id) {
    if (type === 'field') {
        multiSelection.selectedFields.delete(id);
        removeMultiSelectStyle('field', id);
    } else if (type === 'anchor') {
        multiSelection.selectedAnchors.delete(id);
        removeMultiSelectStyle('anchor', id);
    }

    if (multiSelection.selectedFields.size === 0 && multiSelection.selectedAnchors.size === 0) {
        multiSelection.isActive = false;
    }
}

// ✅ Apply multi-select styling
function applyMultiSelectStyle(type, id) {
    const selector = type === 'field' ?
        `.field-overlay[data-field-id="${id}"]` :
        `.anchor-overlay[data-anchor-id="${id}"]`;

    $(selector).addClass('multi-selected');
}

// ✅ Remove multi-select styling
function removeMultiSelectStyle(type, id) {
    const selector = type === 'field' ?
        `.field-overlay[data-field-id="${id}"]` :
        `.anchor-overlay[data-anchor-id="${id}"]`;

    $(selector).removeClass('multi-selected');
}

 
 

// ✅ Capture original states
function captureOriginalStates() {
    multiSelection.originalStates.clear();

    multiSelection.selectedFields.forEach(fieldId => {
        const field = fieldMappings.find(f => f.id === fieldId);
        if (field) {
            multiSelection.originalStates.set(`field-${fieldId}`, {
                x: field.x,
                y: field.y,
                width: field.width,
                height: field.height
            });
        }
    });

    multiSelection.selectedAnchors.forEach(anchorId => {
        const anchor = anchorPoints.find(a => a.id === anchorId);
        if (anchor) {
            multiSelection.originalStates.set(`anchor-${anchorId}`, {
                x: anchor.x,
                y: anchor.y,
                width: anchor.width || 200,
                height: anchor.height || 50
            });
        }
    });

    console.log('🔥 [MULTI] Captured', multiSelection.originalStates.size, 'original states');
}

 
 

// ✅ Delete selected items
function deleteMultiSelection() {
    if (multiSelection.selectedFields.size === 0 && multiSelection.selectedAnchors.size === 0) {
        return;
    }

    const totalSelected = multiSelection.selectedFields.size + multiSelection.selectedAnchors.size;

    showConfirmDialog(
        'Delete Selected Items',
        `Are you sure you want to delete ${totalSelected} selected items?`,
        'Delete All',
        'Cancel'
    ).then(async (confirmed) => {
        if (confirmed) {
            try {
                for (const fieldId of multiSelection.selectedFields) {
                    await removeFieldMappingAjax(fieldId);
                }

                for (const anchorId of multiSelection.selectedAnchors) {
                    const anchor = anchorPoints.find(a => a.id === anchorId);
                    if (anchor) {
                        await removeAnchorPointAjax(anchor.templateId, anchorId);
                    }
                }

                clearMultiSelection();
                alert.success(`${totalSelected} items deleted successfully`, { popup: false });

            } catch (error) {
                console.error('Error deleting selected items:', error);
                alert.error('Error deleting selected items');
            }
        }
    });
}

// ✅ Refresh multi-select styling (for zoom/page changes)
function refreshMultiSelectStyling() {
    $('.field-overlay, .anchor-overlay').removeClass('multi-selected');

    multiSelection.selectedFields.forEach(fieldId => {
        applyMultiSelectStyle('field', fieldId);
    });

    multiSelection.selectedAnchors.forEach(anchorId => {
        applyMultiSelectStyle('anchor', anchorId);
    });
}


// ================================================================
// IMMEDIATE SMOOTH MULTI-SELECTION - Direct Updates, No Throttling
// Replace these functions for butter-smooth operations
// ================================================================

// ✅ IMMEDIATE: Smooth multi-move with direct delta calculation
function performMultiMove(e) {
    if (!multiSelection.isDragging) return;

    // ✅ Calculate total delta from original start position (like normal single drag)
    const totalDeltaX = e.clientX - multiSelection.dragStartPos.x;
    const totalDeltaY = e.clientY - multiSelection.dragStartPos.y;

    getIntrinsicPdfDimensions().then(function (pdfDimensions) {
        const containerWidth = $('#pdf-viewer-container').width() - 40;
        const baseScale = containerWidth / pdfDimensions.width;
        const actualScale = baseScale * currentZoom;

        const pdfDeltaX = totalDeltaX / actualScale;
        const pdfDeltaY = totalDeltaY / actualScale;

        // ✅ Apply total delta to ALL selected fields from their ORIGINAL positions
        multiSelection.selectedFields.forEach(fieldId => {
            const field = fieldMappings.find(f => f.id === fieldId);
            const originalState = multiSelection.originalStates.get(`field-${fieldId}`);

            if (field && originalState) {
                const minX = 0;
                const minY = 0;
                const maxX = pdfDimensions.width - originalState.width;
                const maxY = pdfDimensions.height - originalState.height;

                // ✅ Calculate from original position + total delta
                let newX = originalState.x + pdfDeltaX;
                let newY = originalState.y + pdfDeltaY;

                newX = Math.max(minX, Math.min(maxX, newX));
                newY = Math.max(minY, Math.min(maxY, newY));

                field.x = parseFloat(newX.toFixed(2));
                field.y = parseFloat(newY.toFixed(2));

                updateFieldOverlay(field.id);
            }
        });

        // ✅ Apply total delta to ALL selected anchors from their ORIGINAL positions
        multiSelection.selectedAnchors.forEach(anchorId => {
            const anchor = anchorPoints.find(a => a.id === anchorId);
            const originalState = multiSelection.originalStates.get(`anchor-${anchorId}`);

            if (anchor && originalState) {
                const minX = 0;
                const minY = 0;
                const maxX = pdfDimensions.width - originalState.width;
                const maxY = pdfDimensions.height - originalState.height;

                // ✅ Calculate from original position + total delta
                let newX = originalState.x + pdfDeltaX;
                let newY = originalState.y + pdfDeltaY;

                newX = Math.max(minX, Math.min(maxX, newX));
                newY = Math.max(minY, Math.min(maxY, newY));

                anchor.x = parseFloat(newX.toFixed(2));
                anchor.y = parseFloat(newY.toFixed(2));

                updateAnchorOverlay(anchor.id);
            }
        });
    });
}

// ✅ IMMEDIATE: Smooth multi-resize with direct delta calculation
function performMultiResize(e) {
    if (!multiSelection.isResizing) return;

    // ✅ Calculate total delta from original start position
    const totalDeltaX = e.clientX - multiSelection.dragStartPos.x;
    const totalDeltaY = e.clientY - multiSelection.dragStartPos.y;

    getIntrinsicPdfDimensions().then(function (pdfDimensions) {
        const containerWidth = $('#pdf-viewer-container').width() - 40;
        const baseScale = containerWidth / pdfDimensions.width;
        const actualScale = baseScale * currentZoom;

        const pdfDeltaX = totalDeltaX / actualScale;
        const pdfDeltaY = totalDeltaY / actualScale;

        // ✅ Apply SAME delta to ALL selected fields from their ORIGINAL dimensions
        multiSelection.selectedFields.forEach(fieldId => {
            const field = fieldMappings.find(f => f.id === fieldId);
            const originalState = multiSelection.originalStates.get(`field-${fieldId}`);

            if (field && originalState) {
                const minWidth = 7;
                const minHeight = 7;
                const maxWidth = pdfDimensions.width - originalState.x;
                const maxHeight = pdfDimensions.height - originalState.y;

                // ✅ Apply SAME resize delta to all fields
                if (multiSelection.resizeHandle.includes('e')) {
                    let newWidth = originalState.width + pdfDeltaX;
                    newWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
                    field.width = parseFloat(newWidth.toFixed(2));
                }

                if (multiSelection.resizeHandle.includes('s')) {
                    let newHeight = originalState.height + pdfDeltaY;
                    newHeight = Math.max(minHeight, Math.min(maxHeight, newHeight));
                    field.height = parseFloat(newHeight.toFixed(2));
                }

                updateFieldOverlay(field.id);
            }
        });

        // ✅ Apply SAME delta to ALL selected anchors from their ORIGINAL dimensions
        multiSelection.selectedAnchors.forEach(anchorId => {
            const anchor = anchorPoints.find(a => a.id === anchorId);
            const originalState = multiSelection.originalStates.get(`anchor-${anchorId}`);

            if (anchor && originalState) {
                const minWidth = 20;
                const minHeight = 15;
                const maxWidth = pdfDimensions.width - originalState.x;
                const maxHeight = pdfDimensions.height - originalState.y;

                // ✅ Apply SAME resize delta to all anchors
                if (multiSelection.resizeHandle.includes('e')) {
                    let newWidth = originalState.width + pdfDeltaX;
                    newWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
                    anchor.width = parseFloat(newWidth.toFixed(2));
                }

                if (multiSelection.resizeHandle.includes('s')) {
                    let newHeight = originalState.height + pdfDeltaY;
                    newHeight = Math.max(minHeight, Math.min(maxHeight, newHeight));
                    anchor.height = parseFloat(newHeight.toFixed(2));
                }

                updateAnchorOverlay(anchor.id);
            }
        });
    });
}

 

// ✅ IMMEDIATE: Faster save with shorter debounce
function saveMultiSelectionChanges() {
    console.log('💾 [MULTI] Saving multi-selection changes...');

    clearTimeout(window.multiSelectSaveTimeout);

    // ✅ Shorter debounce for immediate responsiveness
    window.multiSelectSaveTimeout = setTimeout(async () => {
        try {
            console.log('💾 [MULTI] Actually saving to server...');

            // Save field changes
            for (const fieldId of multiSelection.selectedFields) {
                const field = fieldMappings.find(f => f.id === fieldId);
                if (field) {
                    const updateData = {
                        FieldName: field.fieldName,
                        DisplayName: field.displayName,
                        DataType: field.dataType,
                        Description: field.description,
                        X: field.x,
                        Y: field.y,
                        Width: field.width,
                        Height: field.height,
                        PageNumber: field.pageNumber,
                        IsRequired: field.isRequired,
                        BorderColor: field.borderColor,
                        IsVisible: field.isVisible
                    };

                    await updateFieldMappingAjax(fieldId, updateData, false);
                }
            }

            // Save anchor changes
            for (const anchorId of multiSelection.selectedAnchors) {
                const anchor = anchorPoints.find(a => a.id === anchorId);
                if (anchor) {
                    const updateData = {
                        Name: anchor.name,
                        TemplateId: anchor.templateId,
                        PageNumber: anchor.pageNumber,
                        Description: anchor.description,
                        X: anchor.x,
                        Y: anchor.y,
                        Width: anchor.width,
                        Height: anchor.height,
                        ReferenceText: anchor.referenceText,
                        ReferencePattern: anchor.referencePattern,
                        IsRequired: anchor.isRequired,
                        ConfidenceThreshold: anchor.confidenceThreshold,
                        SearchRadius: anchor.searchRadius,
                        DisplayOrder: anchor.displayOrder,
                        Color: anchor.color,
                        BorderColor: anchor.borderColor,
                        IsVisible: anchor.isVisible
                    };

                    await updateAnchorPointAjax(anchorId, updateData);
                }
            }

            console.log('✅ [MULTI] Multi-selection changes saved successfully');

        } catch (error) {
            console.error('❌ [MULTI] Error saving multi-selection changes:', error);
        }
    }, 300); // ✅ Shorter debounce for immediate responsiveness
}








// ✅ Export functions
window.initializeMultiSelection = initializeMultiSelection;
window.clearMultiSelection = clearMultiSelection;
window.selectAllItems = selectAllItems;
window.deleteMultiSelection = deleteMultiSelection;
window.refreshMultiSelectStyling = refreshMultiSelectStyling;

// ✅ Auto-initialize
$(document).ready(function () {
    setTimeout(() => {
        if (typeof fieldMappings !== 'undefined' && typeof anchorPoints !== 'undefined') {
            initializeMultiSelection();
            console.log('🔥 [MULTI] Auto-initialized multi-selection with separate operations');
        }
    }, 1000);
});

console.log('📁 [MULTI] Multi-selection extension with separate operations loaded');