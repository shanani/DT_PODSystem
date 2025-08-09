// ================================================================
// UNIFIED & STABLE wizard-step3.js - Fixed Zooming, Panning & Coordinates
// ================================================================

// Global variables (restored original structure)
var canvas, ctx, pdfDoc, currentPage = 1, totalPages = 1, currentZoom = 1.0;
var fieldMappings = [], anchorPoints = [];
var isSelecting = false, startPos = { x: 0, y: 0 }, currentSelection = { x: 0, y: 0, width: 0, height: 0 };
var selectedFieldId = null, selectedAnchorId = null, currentRender = null;
var isDragging = false, isResizing = false, dragStartX = 0, dragStartY = 0, resizeHandle = '', selectedField = null, dragOffset = { x: 0, y: 0 };
var isEditingField = false;
 
var currentMappingMode = 'field';  
var anchorsVisible = true;
var isEditingAnchor = false;
var selectedAnchor = null;
var isDraggingAnchor = false;
var isResizingAnchor = false;
var originalAnchorState = null;
var originalFieldState = null;
// ================================================================
// ✅ RESTORED: STABLE COORDINATE CONVERSION FUNCTIONS
// ================================================================

function screenToPdfCoordinates(screenX, screenY, screenWidth, screenHeight) {
    return getIntrinsicPdfDimensions().then(function (pdfDimensions) {
        const containerWidth = $('#pdf-viewer-container').width() - 40;
        const baseScale = containerWidth / pdfDimensions.width;
        const actualScale = baseScale * currentZoom;

        // ✅ ORIGINAL STABLE: Convert to ABSOLUTE PDF coordinates (not normalized)
        const pdfX = screenX / actualScale;
        const pdfY = screenY / actualScale;
        const pdfWidth = screenWidth / actualScale;
        const pdfHeight = screenHeight / actualScale;

        return {
            x: parseFloat(pdfX.toFixed(2)),      // ← ABSOLUTE pixels, not normalized
            y: parseFloat(pdfY.toFixed(2)),      // ← ABSOLUTE pixels, not normalized
            width: parseFloat(pdfWidth.toFixed(2)),   // ← ABSOLUTE pixels
            height: parseFloat(pdfHeight.toFixed(2)), // ← ABSOLUTE pixels
            pdfWidth: pdfDimensions.width,
            pdfHeight: pdfDimensions.height
        };
    });
}

function pdfToScreenCoordinates(pdfAbsoluteX, pdfAbsoluteY, pdfAbsoluteWidth, pdfAbsoluteHeight) {
    return getIntrinsicPdfDimensions().then(function (pdfDimensions) {
        const containerWidth = $('#pdf-viewer-container').width() - 40;
        const baseScale = containerWidth / pdfDimensions.width;
        const actualScale = baseScale * currentZoom;

        // ✅ ORIGINAL STABLE: Input is already absolute PDF coordinates
        const screenX = pdfAbsoluteX * actualScale;
        const screenY = pdfAbsoluteY * actualScale;
        const screenWidth = pdfAbsoluteWidth * actualScale;
        const screenHeight = pdfAbsoluteHeight * actualScale;

        return {
            x: screenX,
            y: screenY,
            width: screenWidth,
            height: screenHeight
        };
    });
}

function getIntrinsicPdfDimensions() {
    if (!pdfDoc || !canvas) return Promise.resolve({ width: 0, height: 0 });

    return pdfDoc.getPage(currentPage).then(function (page) {
        const viewport = page.getViewport({ scale: 1.0 });
        return {
            width: viewport.width,
            height: viewport.height
        };
    });
}

// ================================================================
// ✅ RESTORED: STABLE ZOOM & PAN SYSTEM
// ================================================================

function updateZoomWithoutScroll(preserveScrollTop, preserveScrollLeft) {
    if (!pdfDoc || !canvas) return;

    // Cancel any existing render
    if (currentRender) {
        currentRender.cancel();
        currentRender = null;
    }

    pdfDoc.getPage(currentPage).then(function (page) {
        const container = $('#pdf-viewer-container');
        const containerWidth = container.width() - 40;
        const baseScale = containerWidth / page.getViewport({ scale: 1.0 }).width;
        const actualScale = baseScale * currentZoom;
        const viewport = page.getViewport({ scale: actualScale });

        canvas.width = viewport.width;
        canvas.height = viewport.height;
        canvas.style.width = viewport.width + 'px';
        canvas.style.height = viewport.height + 'px';

        const renderContext = { canvasContext: canvas.getContext('2d'), viewport: viewport };
        currentRender = page.render(renderContext);

        currentRender.promise.then(function () {
            currentRender = null;
            $('#zoom-level').text(Math.round(currentZoom * 100));

            if (typeof preserveScrollTop !== 'undefined') {
                container.scrollTop(preserveScrollTop);
            }
            if (typeof preserveScrollLeft !== 'undefined') {
                container.scrollLeft(preserveScrollLeft);
            }

            // ✅ UNIFIED: Always render both overlays (STABLE)
            renderFieldOverlays();
            renderAnchorOverlays();
        }).catch(function (error) {
            if (error.name !== 'RenderingCancelledException') {
                console.error('PDF render error:', error);
            }
        });
    });
}

 

 

function getCurrentModeCursor() {
    if (currentMappingMode === 'anchor') {
        return 'crosshair';
    } else {
        return 'crosshair';
    }
}
 

// ================================================================
// ✅ RESTORED: STABLE FIELD DRAG & RESIZE SYSTEM
// ================================================================


// ✅ FIX: Complete setupFieldDragResize function
function setupFieldDragResize() {
    console.log('🔧 [DRAG] Setting up field drag & resize handlers');

    $(document).on('mousedown', '.field-overlay', function (e) {
        e.stopPropagation();
        e.preventDefault();

        const fieldId = parseInt($(this).data('field-id'));
        selectedField = fieldMappings.find(f => f.id === fieldId);
        if (!selectedField) {
            console.error('🔧 [DRAG] Field not found:', fieldId);
            return;
        }

        console.log('🔧 [DRAG] Starting drag for field:', selectedField.fieldName);

        // Capture original state
        originalFieldState = {
            x: selectedField.x,
            y: selectedField.y,
            width: selectedField.width,
            height: selectedField.height
        };

        const rect = this.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        // Check if clicking on resize handles
        if (x >= rect.width - 10 && y >= rect.height - 10) {
            isResizing = true;
            resizeHandle = 'se';
            console.log('🔧 [DRAG] Starting SE resize');
        } else if (x >= rect.width - 10) {
            isResizing = true;
            resizeHandle = 'e';
            console.log('🔧 [DRAG] Starting E resize');
        } else if (y >= rect.height - 10) {
            isResizing = true;
            resizeHandle = 's';
            console.log('🔧 [DRAG] Starting S resize');
        } else {
            isDragging = true;
            dragOffset.x = x;
            dragOffset.y = y;
            console.log('🔧 [DRAG] Starting drag');
        }

        dragStartX = e.clientX;
        dragStartY = e.clientY;
        $(this).addClass('field-dragging');

        // ✅ FIX: Prevent text selection during drag
        $('body').addClass('dragging').css('user-select', 'none');
    });

    $(document).on('mousemove', function (e) {
        if (isDragging && selectedField) {
            const deltaX = e.clientX - dragStartX;
            const deltaY = e.clientY - dragStartY;

            getIntrinsicPdfDimensions().then(function (pdfDimensions) {
                const containerWidth = $('#pdf-viewer-container').width() - 40;
                const baseScale = containerWidth / pdfDimensions.width;
                const actualScale = baseScale * currentZoom;

                const pdfDeltaX = deltaX / actualScale;
                const pdfDeltaY = deltaY / actualScale;

                // Apply bounds constraints
                const minX = 0;
                const minY = 0;
                const maxX = pdfDimensions.width - selectedField.width;
                const maxY = pdfDimensions.height - selectedField.height;

                let newX = selectedField.x + pdfDeltaX;
                let newY = selectedField.y + pdfDeltaY;

                newX = Math.max(minX, Math.min(maxX, newX));
                newY = Math.max(minY, Math.min(maxY, newY));

                selectedField.x = parseFloat(newX.toFixed(2));
                selectedField.y = parseFloat(newY.toFixed(2));

                updateFieldOverlay(selectedField.id);
            });

            dragStartX = e.clientX;
            dragStartY = e.clientY;

        } else if (isResizing && selectedField) {
            const deltaX = e.clientX - dragStartX;
            const deltaY = e.clientY - dragStartY;

            getIntrinsicPdfDimensions().then(function (pdfDimensions) {
                const containerWidth = $('#pdf-viewer-container').width() - 40;
                const baseScale = containerWidth / pdfDimensions.width;
                const actualScale = baseScale * currentZoom;

                const pdfDeltaX = deltaX / actualScale;
                const pdfDeltaY = deltaY / actualScale;

                const minWidth = 7;
                const minHeight = 7;
                const maxWidth = pdfDimensions.width - selectedField.x;
                const maxHeight = pdfDimensions.height - selectedField.y;

                if (resizeHandle.includes('e')) {
                    let newWidth = selectedField.width + pdfDeltaX;
                    newWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
                    selectedField.width = parseFloat(newWidth.toFixed(2));
                }

                if (resizeHandle.includes('s')) {
                    let newHeight = selectedField.height + pdfDeltaY;
                    newHeight = Math.max(minHeight, Math.min(maxHeight, newHeight));
                    selectedField.height = parseFloat(newHeight.toFixed(2));
                }

                updateFieldOverlay(selectedField.id);
            });

            dragStartX = e.clientX;
            dragStartY = e.clientY;
        }
    });

    // ✅ FIX: Enhanced mouseup handler with immediate data capture
    $(document).on('mouseup', function (e) {
        console.log('🔧 [DRAG] Mouse up - isDragging:', isDragging, 'isResizing:', isResizing);

        if (isDragging || isResizing) {
            console.log('🔧 [DRAG] Ending drag/resize operation');

            // ✅ FIX: Clean up UI classes immediately
            $('.field-overlay').removeClass('field-dragging');
            $('body').removeClass('dragging').css('user-select', '');

            if (selectedField && (isDragging || isResizing) && originalFieldState) {
                // Check for actual changes
                const hasPositionChanged = (
                    Math.abs(originalFieldState.x - selectedField.x) > 1 ||
                    Math.abs(originalFieldState.y - selectedField.y) > 1
                );

                const hasSizeChanged = (
                    Math.abs(originalFieldState.width - selectedField.width) > 1 ||
                    Math.abs(originalFieldState.height - selectedField.height) > 1
                );

                const hasActualChanges = hasPositionChanged || hasSizeChanged;

                if (hasActualChanges) {
                    console.log('📤 [DRAG] Actual changes detected for field:', selectedField.id);

                    // ✅ FIX: Capture field data immediately before timeout
                    const fieldToUpdate = {
                        id: selectedField.id,
                        fieldName: selectedField.fieldName || 'Unnamed_Field',
                        displayName: selectedField.displayName || selectedField.fieldName || 'Unnamed_Field',
                        dataType: selectedField.dataType || 'number',
                        description: selectedField.description || '',
                        x: selectedField.x,
                        y: selectedField.y,
                        width: selectedField.width,
                        height: selectedField.height,
                        pageNumber: selectedField.pageNumber || currentPage || 1,
                        isRequired: selectedField.isRequired || false,
                        borderColor: selectedField.borderColor || '#A54EE1',
                        isVisible: selectedField.isVisible !== false
                    };

                    // Clear any existing timeout for this field
                    clearTimeout(selectedField.updateTimeout);

                    // Debounced AJAX update using captured data
                    const timeoutId = setTimeout(async () => {
                        try {
                            console.log('📤 [DRAG] Sending update to server for field:', fieldToUpdate.id);

                            const updateData = {
                                FieldName: fieldToUpdate.fieldName,
                                DisplayName: fieldToUpdate.displayName,
                                DataType: fieldToUpdate.dataType,
                                Description: fieldToUpdate.description,
                                X: fieldToUpdate.x,
                                Y: fieldToUpdate.y,
                                Width: fieldToUpdate.width,
                                Height: fieldToUpdate.height,
                                PageNumber: fieldToUpdate.pageNumber,
                                IsRequired: fieldToUpdate.isRequired,
                                BorderColor: fieldToUpdate.borderColor,
                                IsVisible: fieldToUpdate.isVisible
                            };

                            await updateFieldMappingAjax(fieldToUpdate.id, updateData, true);
                        } catch (error) {
                            console.error('Error updating field position:', error);
                        }
                    }, 300);

                    // Store timeout ID for cleanup
                    if (selectedField && selectedField.id) {
                        selectedField.updateTimeout = timeoutId;
                    }
                } else {
                    console.log('📤 [DRAG] No actual changes detected');
                }
            }

            // ✅ FIX: Reset ALL drag state variables
            isDragging = false;
            isResizing = false;
            resizeHandle = '';
            selectedField = null;
            originalFieldState = null;
            dragStartX = 0;
            dragStartY = 0;
            dragOffset = { x: 0, y: 0 };

            console.log('🔧 [DRAG] Drag state reset complete');
        }
    });

    // ✅ FIX: Also handle mouse leave to prevent stuck drags
    $(document).on('mouseleave', function () {
        if (isDragging || isResizing) {
            console.log('🔧 [DRAG] Mouse left document, forcing drag end');
            $(document).trigger('mouseup');
        }
    });

    console.log('✅ [DRAG] Field drag & resize handlers setup complete');
}

// ================================================================
// ✅ UNIFIED: ANCHOR DRAG & RESIZE (USING STABLE FIELD LOGIC)
// ================================================================
function setupAnchorDragResize() {
    // ✅ Variables are now global, so don't declare them here
    let dragStartX = 0;
    let dragStartY = 0;
    let resizeHandle = '';

    $(document).on('mousedown', '.anchor-overlay', function (e) {
        e.stopPropagation();

        const anchorId = parseInt($(this).data('anchor-id'));
        selectedAnchor = anchorPoints.find(a => a.id === anchorId);
        if (!selectedAnchor) return;

        // ✅ UNIFIED: Same state capture as fields
        originalAnchorState = {
            x: selectedAnchor.x,
            y: selectedAnchor.y,
            width: selectedAnchor.width || 200,
            height: selectedAnchor.height || 50
        };

        const rect = this.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        // Check resize handles (same logic as fields)
        if (x >= rect.width - 10 && y >= rect.height - 10) {
            isResizingAnchor = true;
            resizeHandle = 'se';
        } else if (x >= rect.width - 10) {
            isResizingAnchor = true;
            resizeHandle = 'e';
        } else if (y >= rect.height - 10) {
            isResizingAnchor = true;
            resizeHandle = 's';
        } else {
            isDraggingAnchor = true;
        }

        dragStartX = e.clientX;
        dragStartY = e.clientY;
        $(this).addClass('anchor-dragging');
        e.preventDefault();
    });

    $(document).on('mousemove', function (e) {
        if (isDraggingAnchor && selectedAnchor) {
            const deltaX = e.clientX - dragStartX;
            const deltaY = e.clientY - dragStartY;

            // ✅ UNIFIED: Same coordinate logic as fields (STABLE)
            getIntrinsicPdfDimensions().then(function (pdfDimensions) {
                const containerWidth = $('#pdf-viewer-container').width() - 40;
                const baseScale = containerWidth / pdfDimensions.width;
                const actualScale = baseScale * currentZoom;

                const pdfDeltaX = deltaX / actualScale;
                const pdfDeltaY = deltaY / actualScale;

                const minX = 0;
                const minY = 0;
                const maxX = pdfDimensions.width - (selectedAnchor.width || 200);
                const maxY = pdfDimensions.height - (selectedAnchor.height || 50);

                let newX = selectedAnchor.x + pdfDeltaX;
                let newY = selectedAnchor.y + pdfDeltaY;

                newX = Math.max(minX, Math.min(maxX, newX));
                newY = Math.max(minY, Math.min(maxY, newY));

                selectedAnchor.x = parseFloat(newX.toFixed(2));
                selectedAnchor.y = parseFloat(newY.toFixed(2));

                updateAnchorOverlay(selectedAnchor.id);
            });

            dragStartX = e.clientX;
            dragStartY = e.clientY;

        } else if (isResizingAnchor && selectedAnchor) {
            const deltaX = e.clientX - dragStartX;
            const deltaY = e.clientY - dragStartY;

            // ✅ UNIFIED: Same resize logic as fields
            getIntrinsicPdfDimensions().then(function (pdfDimensions) {
                const containerWidth = $('#pdf-viewer-container').width() - 40;
                const baseScale = containerWidth / pdfDimensions.width;
                const actualScale = baseScale * currentZoom;

                const pdfDeltaX = deltaX / actualScale;
                const pdfDeltaY = deltaY / actualScale;

                const minWidth = 20;
                const minHeight = 15;
                const maxWidth = pdfDimensions.width - selectedAnchor.x;
                const maxHeight = pdfDimensions.height - selectedAnchor.y;

                if (resizeHandle.includes('e')) {
                    let newWidth = (selectedAnchor.width || 200) + pdfDeltaX;
                    newWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
                    selectedAnchor.width = parseFloat(newWidth.toFixed(2));
                }

                if (resizeHandle.includes('s')) {
                    let newHeight = (selectedAnchor.height || 50) + pdfDeltaY;
                    newHeight = Math.max(minHeight, Math.min(maxHeight, newHeight));
                    selectedAnchor.height = parseFloat(newHeight.toFixed(2));
                }

                updateAnchorOverlay(selectedAnchor.id);
            });

            dragStartX = e.clientX;
            dragStartY = e.clientY;
        }
    });

    $(document).on('mouseup', function () {
        if (isDraggingAnchor || isResizingAnchor) {
            $('.anchor-overlay').removeClass('anchor-dragging');

            if (selectedAnchor && (isDraggingAnchor || isResizingAnchor) && originalAnchorState) {
                // ✅ UNIFIED: Same change detection as fields
                const hasPositionChanged = (
                    Math.abs(originalAnchorState.x - selectedAnchor.x) > 1 ||
                    Math.abs(originalAnchorState.y - selectedAnchor.y) > 1
                );

                const hasSizeChanged = (
                    Math.abs((originalAnchorState.width || 200) - (selectedAnchor.width || 200)) > 1 ||
                    Math.abs((originalAnchorState.height || 50) - (selectedAnchor.height || 50)) > 1
                );

                const hasActualChanges = hasPositionChanged || hasSizeChanged;

                if (hasActualChanges) {
                    console.log('📤 [DRAG] Actual changes detected for anchor:', selectedAnchor.id);

                    // ✅ FIX: Capture anchor data immediately before timeout with fallbacks
                    const anchorToUpdate = {
                        id: selectedAnchor.id,
                        name: selectedAnchor.name || 'Unnamed_Anchor',
                        templateId: selectedAnchor.templateId || parseInt(wizardData?.templateId) || 0,
                        pageNumber: selectedAnchor.pageNumber || currentPage || 1,
                        description: selectedAnchor.description || '',
                        x: selectedAnchor.x,
                        y: selectedAnchor.y,
                        width: selectedAnchor.width || 200,
                        height: selectedAnchor.height || 50,
                        tolerance: selectedAnchor.tolerance || 10,
                        referenceText: selectedAnchor.referenceText || 'Reference Text Required',
                        referencePattern: selectedAnchor.referencePattern || '',
                        isRequired: selectedAnchor.isRequired !== false,
                        confidenceThreshold: selectedAnchor.confidenceThreshold || 0.8,
                        searchRadius: selectedAnchor.searchRadius || 200,
                        displayOrder: selectedAnchor.displayOrder || 0,
                        color: selectedAnchor.color || '#00C48C',
                        borderColor: selectedAnchor.borderColor || '#00C48C',
                        isVisible: selectedAnchor.isVisible !== false
                    };

                    clearTimeout(selectedAnchor.updateTimeout);
                    selectedAnchor.updateTimeout = setTimeout(async () => {
                        try {
                            const updateData = {
                                Name: anchorToUpdate.name,
                                TemplateId: anchorToUpdate.templateId,
                                PageNumber: anchorToUpdate.pageNumber,
                                Description: anchorToUpdate.description,
                                X: anchorToUpdate.x,
                                Y: anchorToUpdate.y,
                                Width: anchorToUpdate.width,
                                Height: anchorToUpdate.height,
                                Tolerance: anchorToUpdate.tolerance,
                                ReferenceText: anchorToUpdate.referenceText,
                                ReferencePattern: anchorToUpdate.referencePattern,
                                IsRequired: anchorToUpdate.isRequired,
                                ConfidenceThreshold: anchorToUpdate.confidenceThreshold,
                                SearchRadius: anchorToUpdate.searchRadius,
                                DisplayOrder: anchorToUpdate.displayOrder,
                                Color: anchorToUpdate.color,
                                BorderColor: anchorToUpdate.borderColor,
                                IsVisible: anchorToUpdate.isVisible
                            };

                            console.log('📤 [DRAG] Updating anchor with captured data:', updateData);
                            await updateAnchorPointAjax(anchorToUpdate.id, updateData);
                        } catch (error) {
                            console.error('Error updating anchor position:', error);
                        }
                    }, 300);
                }
            }

            isDraggingAnchor = false;
            isResizingAnchor = false;
            resizeHandle = '';
            selectedAnchor = null;
            originalAnchorState = null;
        }
    });

    console.log('✅ [DRAG] Anchor drag & resize handlers setup');
}

 

 

function navigateToPage(pageNum) {
    if (pageNum < 1 || pageNum > totalPages) return;
    loadPage(pageNum);
}

 


// ================================================================
// ✅ UNIFIED: MODAL SYSTEM (KEEPS BOTH FIELD & ANCHOR MODALS)
// ================================================================

function openFieldModal(mode, fieldId = null) {
    isEditingField = (mode === 'edit');

    if (isEditingField && fieldId) {
        const field = fieldMappings.find(f => f.id === fieldId);
        if (!field) {
            alert.error('Field not found');
            return;
        }

        $('#field-modal-title').text('Edit Field Mapping');
        $('#save-field').html('<i class="fa fa-save me-1"></i>Update Field');

        $('#field-name').val(field.fieldName);
        $('#field-description').val(field.description || '');
        $('#field-required').prop('checked', field.isRequired || false);
        $('#field-page').val(field.pageNumber);

        // Show ABSOLUTE coordinates in pixels
        $('#field-x').val(field.x.toFixed(1) + 'px');
        $('#field-y').val(field.y.toFixed(1) + 'px');
        $('#field-width').val(field.width.toFixed(1) + 'px');
        $('#field-height').val(field.height.toFixed(1) + 'px');

        $('#field-position-display').show();
        $('#field-size-display').show();
        $('#field-edit-note').show();
        $('#field-modal').data('editing-id', fieldId);
    } else {
        // Add new field mode
        $('#field-modal-title').text('Add New Field Mapping');
        $('#save-field').html('<i class="fa fa-save me-1"></i>Save Field');

        $('#field-name').val('');
        $('#field-description').val('');
        $('#field-required').prop('checked', false);
        $('#field-page').val(currentPage);

        if (currentSelection && currentSelection.width > 0) {
            screenToPdfCoordinates(
                currentSelection.x,
                currentSelection.y,
                currentSelection.width,
                currentSelection.height
            ).then(function (pdfCoords) {
                $('#field-x').val(pdfCoords.x.toFixed(1) + 'px');
                $('#field-y').val(pdfCoords.y.toFixed(1) + 'px');
                $('#field-width').val(pdfCoords.width.toFixed(1) + 'px');
                $('#field-height').val(pdfCoords.height.toFixed(1) + 'px');
            });

            $('#field-position-display').show();
            $('#field-size-display').show();
        }

        $('#field-edit-note').hide();
        $('#field-modal').removeData('editing-id');
    }

    $('#field-modal').modal('show');
    setTimeout(() => $('#field-name').focus(), 300);
}

function openAnchorModal(mode, anchorId = null) {
    isEditingAnchor = (mode === 'edit');

    if (isEditingAnchor && anchorId) {
        const anchor = anchorPoints.find(a => a.id === anchorId);
        if (!anchor) {
            alert.error('Anchor not found');
            return;
        }

        $('#anchor-modal-title').text('Edit Anchor Point');
        $('#save-anchor').html('<i class="fa fa-save me-1"></i>Update Anchor');

        $('#anchor-name').val(anchor.name);
        $('#anchor-reference-text').val(anchor.referenceText);
        $('#anchor-description').val(anchor.description);
        $('#anchor-required').prop('checked', anchor.isRequired);
        $('#anchor-page').val(anchor.pageNumber);

        // Show rectangle coordinates
        $('#anchor-x').val(anchor.x.toFixed(1) + 'px');
        $('#anchor-y').val(anchor.y.toFixed(1) + 'px');
        $('#anchor-width').val((anchor.width || 200).toFixed(1) + 'px');
        $('#anchor-height').val((anchor.height || 50).toFixed(1) + 'px');

        $('#anchor-position-display').show();
        $('#anchor-size-display').show();
        $('#anchor-modal').data('editing-id', anchorId);
    } else {
        // Add new anchor
        $('#anchor-modal-title').text('Add New Anchor Point');
        $('#save-anchor').html('<i class="fa fa-anchor me-1"></i>Save Anchor');

        $('#anchor-name').val('');
        $('#anchor-reference-text').val('');
        $('#anchor-description').val('');
        $('#anchor-required').prop('checked', true);
        $('#anchor-page').val(currentPage);

        if (currentSelection && currentSelection.width > 0) {
            screenToPdfCoordinates(
                currentSelection.x,
                currentSelection.y,
                currentSelection.width,
                currentSelection.height
            ).then(function (pdfCoords) {
                $('#anchor-x').val(pdfCoords.x.toFixed(1) + 'px');
                $('#anchor-y').val(pdfCoords.y.toFixed(1) + 'px');
                $('#anchor-width').val(pdfCoords.width.toFixed(1) + 'px');
                $('#anchor-height').val(pdfCoords.height.toFixed(1) + 'px');
            });

            $('#anchor-position-display').show();
            $('#anchor-size-display').show();
        }

        $('#anchor-modal').removeData('editing-id');
    }

    $('#anchor-modal').modal('show');
    setTimeout(() => $('#anchor-name').focus(), 300);
}

// ================================================================
// ✅ REPLACE: setupZoomHandlers function
// ================================================================
function setupZoomHandlers() {
    // Remove all existing handlers first
    $('.zoom-in-btn, .zoom-out-btn, .zoom-fit-btn').off('click');

    // Zoom In - all toolbars
    $('.zoom-in-btn').on('click', function (e) {
        e.preventDefault();
        const container = $('#pdf-viewer-container');
        const scrollTop = container.scrollTop();
        const scrollLeft = container.scrollLeft();
        currentZoom = Math.min(currentZoom + 0.25, 3.0);
        updateZoomAndRender(scrollTop, scrollLeft);
    });

    // Zoom Out - all toolbars
    $('.zoom-out-btn').on('click', function (e) {
        e.preventDefault();
        const container = $('#pdf-viewer-container');
        const scrollTop = container.scrollTop();
        const scrollLeft = container.scrollLeft();
        currentZoom = Math.max(currentZoom - 0.25, 0.25);
        updateZoomAndRender(scrollTop, scrollLeft);
    });

    // Zoom Fit - all toolbars
    $('.zoom-fit-btn').on('click', function (e) {
        e.preventDefault();
        currentZoom = 1.0;
        updateZoomAndRender(0, 0);
    });

    // Mouse wheel zoom (unchanged)
    $('#pdf-viewer-container').on('wheel', function (e) {
        if (e.ctrlKey || e.metaKey) {
            e.preventDefault();
            const scrollTop = $(this).scrollTop();
            const scrollLeft = $(this).scrollLeft();
            const delta = e.originalEvent.deltaY;

            if (delta < 0) {
                currentZoom = Math.min(currentZoom + 0.1, 3.0);
            } else {
                currentZoom = Math.max(currentZoom - 0.1, 0.25);
            }
            updateZoomAndRender(scrollTop, scrollLeft);
        }
    });
}

 

// ================================================================
// ✅ REPLACE: setupPageNavigation function
// ================================================================
function setupPageNavigation() {
    // Remove all existing handlers
    $('.nav-prev-btn, .nav-next-btn').off('click');

    // Previous page - all toolbars
    $('.nav-prev-btn').on('click', function (e) {
        e.preventDefault();
        if (currentPage > 1) navigateToPage(currentPage - 1);
    });

    // Next page - all toolbars
    $('.nav-next-btn').on('click', function (e) {
        e.preventDefault();
        if (currentPage < totalPages) navigateToPage(currentPage + 1);
    });
}

// ================================================================
// ✅ REPLACE: updateNavigationButtons function
// ================================================================
function updateNavigationButtons() {
    // Update all navigation buttons
    $('.nav-prev-btn').prop('disabled', currentPage <= 1);
    $('.nav-next-btn').prop('disabled', currentPage >= totalPages);

    // Update all page info displays
    $('.page-info-display').text(`Page ${currentPage} of ${totalPages}`);
    $('#current-page').text(currentPage);
    $('#total-pages').text(totalPages);
}

// ================================================================
// ✅ REPLACE: switchMappingMode function
// ================================================================
function switchMappingMode(mode) {
    console.log('🔄 Switching mapping mode to:', mode);
    currentMappingMode = mode;

    // Update ALL mode buttons (all toolbars)
    if (mode === 'field') {
        $('.mode-field-btn').removeClass('btn-outline-secondary').addClass('btn-primary active');
        $('.mode-anchor-btn').removeClass('btn-green active').addClass('btn-outline-secondary');
        $('body').removeClass('mapping-mode-anchor').addClass('mapping-mode-field');

        // Update all mode indicators - Purple background for fields
        $('.mode-indicator').removeClass('bg-green').addClass('bg-primary')
            .html('<i class="fa fa-square-o me-1"></i>Drawing Fields');

        // Show field instructions, hide anchor instructions
        $('.field-mode-instructions').show();
        $('.anchor-mode-instructions').hide();

    } else if (mode === 'anchor') {
        $('.mode-anchor-btn').removeClass('btn-outline-secondary').addClass('btn-green active');
        $('.mode-field-btn').removeClass('btn-primary active').addClass('btn-outline-secondary');
        $('body').removeClass('mapping-mode-field').addClass('mapping-mode-anchor');

        // Update all mode indicators - Green background for anchors
        $('.mode-indicator').removeClass('bg-primary').addClass('bg-green')
            .html('<i class="fa fa-anchor me-1"></i>Drawing Anchors');

        // Show anchor instructions, hide field instructions
        $('.anchor-mode-instructions').show();
        $('.field-mode-instructions').hide();

        // Update anchor display
        updateAnchorCount();
        updateAnchorTable();
        renderAnchorOverlays();
    }

    // Update cursor immediately
    if (canvas) {
        $(canvas).css('cursor', getCurrentModeCursor());
    }

    console.log(`✅ Switched to ${mode} mapping mode`);
}

 

// ✅ RESTORED: Original stable initialization enhanced with unified features
function initializeStep3Enhanced() {
    setupZoomHandlers();

    $('#pdf-viewer-container').css({
        'overflow': 'auto',
        'position': 'relative',
        'user-select': 'none'
    });

    $('#pdf-canvas').on('contextmenu', function (e) {
        e.preventDefault();
        return false;
    });
}

function setupAllEventHandlers() {
    setupPageNavigation();
    initializeStep3Enhanced();
    setupMappingEventHandlers();
    setupFieldDragResize();
    setupAnchorDragResize();
    setupAnchorEventHandlers(); // ✅ Make sure this is called
}

 



// ================================================================
// UNIFIED & STABLE wizard-step3.js - Part 2: Missing Functions
// ================================================================

// ✅ RESTORED: Original stable server data loading functions
function loadServerFieldMappings() {
    console.log('📥 [DEBUG] loadServerFieldMappings() called');

    const serverMappings = window.serverWizardData?.step3?.fieldMappings ||
        window.serverWizardData?.Step3?.FieldMappings ||
        [];

    if (serverMappings && serverMappings.length > 0) {
        fieldMappings = serverMappings.map((mapping, index) => {
            return {
                id: mapping.Id || mapping.id || (index + 1),
                fieldName: mapping.FieldName || mapping.fieldName || '',
                displayName: mapping.DisplayName || mapping.displayName || mapping.FieldName || mapping.fieldName || '',
                dataType: mapping.DataType || mapping.dataType || 'number',
                description: mapping.Description || mapping.description || '',
                x: mapping.X || mapping.x || 0,
                y: mapping.Y || mapping.y || 0,
                width: mapping.Width || mapping.width || 100,
                height: mapping.Height || mapping.height || 30,
                pageNumber: mapping.PageNumber || mapping.pageNumber || 1,
                isRequired: mapping.IsRequired || mapping.isRequired || false,
                borderColor: mapping.BorderColor || mapping.borderColor || '#A54EE1',
                isVisible: true,
                coordinateType: 'pdf-absolute'
            };
        });

        console.log('📥 [DEBUG] ✅ Converted field mappings:', fieldMappings);

        setTimeout(() => {
            updateFieldCount();
            updateFieldsTable();
            renderFieldOverlays();
        }, 500);
    } else {
        fieldMappings = [];
    }
}
 

function loadServerAnchorPoints() {
    console.log('📥 [DEBUG] Loading anchor points from server data');

    // ✅ FIX: Use the correct path from your debug output
    const serverAnchors = window.serverWizardData?.step3?.templateAnchors ||
        window.serverWizardData?.Step3?.TemplateAnchors ||
        [];

    console.log('📥 [DEBUG] Found server anchors:', serverAnchors);

    if (serverAnchors && serverAnchors.length > 0) {
        anchorPoints = serverAnchors.map((anchor, index) => {
            console.log(`📥 [DEBUG] Processing server anchor ${index}:`, anchor);

            // ✅ FIX: The server data uses camelCase properties directly
            const converted = {
                id: anchor.id || (index + 1),
                templateId: anchor.templateId || parseInt(wizardData?.templateId) || 0,
                pageNumber: anchor.pageNumber || 1,
                name: anchor.name || `Anchor_${index + 1}`,  // ✅ Direct access, no fallback
                description: anchor.description || '',
                x: parseFloat(anchor.x || 0),
                y: parseFloat(anchor.y || 0),
                width: parseFloat(anchor.width || 200),
                height: parseFloat(anchor.height || 50),
                referenceText: anchor.referenceText || '',  // ✅ Direct access, no fallback
                referencePattern: anchor.referencePattern || '',
                isRequired: anchor.isRequired !== false,
                tolerance: parseFloat(anchor.tolerance || 10),
                confidenceThreshold: parseFloat(anchor.confidenceThreshold || 0.8),
                searchRadius: parseFloat(anchor.searchRadius || 200),
                displayOrder: parseInt(anchor.displayOrder || 0),
                color: anchor.color || '#00C48C',
                borderColor: anchor.borderColor || '#00C48C',
                isVisible: anchor.isVisible !== false
            };

            console.log(`📥 [DEBUG] Converted anchor ${index}:`, converted);
            return converted;
        });

        console.log('📥 [DEBUG] Final anchorPoints array:', anchorPoints);
        console.log('📥 Loaded', anchorPoints.length, 'anchor points from server');

        updateAnchorCount();
        updateAnchorTable();

        setTimeout(() => {
            if (currentMappingMode === 'anchor' || anchorsVisible) {
                console.log('📥 [DEBUG] Rendering anchors after load...');
                renderAnchorOverlays();
            }
        }, 1000);
    } else {
        console.log('📥 No server anchor points found');
        anchorPoints = [];
        updateAnchorCount();
        updateAnchorTable();
    }
}


function getPrimaryPdfFile() {
    console.log('🔍 [DEBUG] getPrimaryPdfFile() called');

    const step2Data = window.serverWizardData?.step2;
    if (step2Data) {
        if (step2Data.primaryFileName || step2Data.PrimaryFileName) {
            const primaryFile = step2Data.primaryFileName || step2Data.PrimaryFileName;
            console.log('✅ [DEBUG] Found primary file:', primaryFile);
            return primaryFile;
        }

        const uploadedFiles = step2Data.uploadedFiles || step2Data.UploadedFiles;
        if (uploadedFiles && uploadedFiles.length > 0) {
            const firstFile = uploadedFiles[0];
            const fileName = firstFile.savedFileName ||
                firstFile.SavedFileName ||
                firstFile.fileName ||
                firstFile.FileName ||
                firstFile.name;

            if (fileName) {
                console.log('✅ [DEBUG] Found file in uploadedFiles:', fileName);
                return fileName;
            }
        }
    }

    if (wizardData?.step2) {
        const step2WizardData = wizardData.step2;

        if (step2WizardData.primaryFileName || step2WizardData.PrimaryFileName) {
            const primaryFile = step2WizardData.primaryFileName || step2WizardData.PrimaryFileName;
            console.log('✅ [DEBUG] Found primary file in wizardData:', primaryFile);
            return primaryFile;
        }

        const files = step2WizardData.uploadedFiles || step2WizardData.UploadedFiles;
        if (files && files.length > 0) {
            const firstFile = files[0];
            const fileName = firstFile.savedFileName ||
                firstFile.SavedFileName ||
                firstFile.fileName ||
                firstFile.FileName ||
                firstFile.name ||
                (firstFile.data && (firstFile.data.savedFileName || firstFile.data.SavedFileName));

            if (fileName) {
                console.log('✅ [DEBUG] Found file in wizardData:', fileName);
                return fileName;
            }
        }
    }

    console.error('❌ [DEBUG] No primary PDF file found in any source');
    return null;
}

// ✅ RESTORED: Original stable PDF loading
function loadPdfDocument() {
    let primaryFile = getPrimaryPdfFile();
    if (!primaryFile) {
        console.error('No primary file available for PDF loading');
        return;
    }

    const pdfUrl = `/Upload/DownloadFile?fileName=${encodeURIComponent(primaryFile)}`;

    if (typeof pdfjsLib !== 'undefined') {
        pdfjsLib.getDocument({ url: pdfUrl, verbosity: 1 }).promise.then(function (pdf) {
            pdfDoc = pdf;
            totalPages = pdf.numPages;
            $('#total-pages').text(totalPages);

            const pageToLoad = currentPage || 1;
            loadPage(pageToLoad);

            $('#pdf-loading').hide();
            canvas.style.display = 'block';
            console.log('PDF loaded successfully:', totalPages, 'pages');
        }).catch(function (error) {
            console.error('PDF loading error:', error);
            alert.error('Failed to load PDF document', 'error');
        });
    } else {
        console.error('PDF.js library not loaded');
        alert.error('PDF viewer not available', 'error');
    }
}

 
 

function createFieldOverlay(field, screenCoords) {
    const overlayContainer = $('#field-overlays');

    const overlay = $(`
        <div class="field-overlay" data-field-id="${field.id}"
             style="position: absolute;
                    left: ${screenCoords.x}px;
                    top: ${screenCoords.y}px;
                    width: ${screenCoords.width}px;
                    height: ${screenCoords.height}px;
                    border: 2px solid ${field.borderColor || '#A54EE1'};
                    background: rgba(165,78,225,0.1);
                    cursor: move;
                    pointer-events: auto;
                    z-index: 10;">
            <div class="field-label" style="background: ${field.borderColor || '#A54EE1'};
                                            color: white;
                                            padding: 2px 6px;
                                            font-size: 11px;
                                            position: absolute;
                                            top: -20px;
                                            left: 0;
                                            white-space: nowrap;">
                ${field.fieldName}
            </div>
            <div class="resize-handle resize-e" style="position: absolute;
                                                        right: -3px;
                                                        top: 0;
                                                        width: 6px;
                                                        height: 100%;
                                                        cursor: e-resize;
                                                        background: transparent;"></div>
            <div class="resize-handle resize-s" style="position: absolute;
                                                        bottom: -3px;
                                                        left: 0;
                                                        width: 100%;
                                                        height: 6px;
                                                        cursor: s-resize;
                                                        background: transparent;"></div>
            <div class="resize-handle resize-se" style="position: absolute;
                                                         right: -3px;
                                                         bottom: -3px;
                                                         width: 6px;
                                                         height: 6px;
                                                         cursor: se-resize;
                                                         background: ${field.borderColor || '#A54EE1'};"></div>
        </div>
    `);

    overlay.on('click', function () {
        selectField(field.id);
    });

    overlay.on('dblclick', function () {
        editField(field.id);
    });

    overlayContainer.append(overlay);
}

function createAnchorOverlay(anchor, screenCoords) {
    const overlayContainer = $('#field-overlays');

    const overlay = $(`
        <div class="anchor-overlay" data-anchor-id="${anchor.id}"
             style="position: absolute;
                    left: ${screenCoords.x}px;
                    top: ${screenCoords.y}px;
                    width: ${screenCoords.width}px;
                    height: ${screenCoords.height}px;
                    border: 2px solid ${anchor.borderColor || '#00C48C'};
                    background: rgba(0,196,140,0.1);
                    cursor: move;
                    pointer-events: auto;
                    z-index: 20;
                    border-radius: 4px;">
            <div class="anchor-label" style="background: ${anchor.color || '#00C48C'};
                                            color: white;
                                            padding: 2px 6px;
                                            font-size: 11px;
                                            position: absolute;
                                            top: -20px;
                                            left: 0;
                                            white-space: nowrap;
                                            border-radius: 3px;">
                📍 ${anchor.name}
            </div>
            <div class="resize-handle resize-e" style="position: absolute;
                                                        right: -3px;
                                                        top: 0;
                                                        width: 6px;
                                                        height: 100%;
                                                        cursor: e-resize;
                                                        background: transparent;"></div>
            <div class="resize-handle resize-s" style="position: absolute;
                                                        bottom: -3px;
                                                        left: 0;
                                                        width: 100%;
                                                        height: 6px;
                                                        cursor: s-resize;
                                                        background: transparent;"></div>
            <div class="resize-handle resize-se" style="position: absolute;
                                                         right: -3px;
                                                         bottom: -3px;
                                                         width: 6px;
                                                         height: 6px;
                                                         cursor: se-resize;
                                                         background: ${anchor.color || '#00C48C'};
                                                         border-radius: 2px;"></div>
        </div>
    `);

    overlay.on('click', function (e) {
        e.stopPropagation();
        selectAnchor(anchor.id);
    });

    overlay.on('dblclick', function (e) {
        e.stopPropagation();
        editAnchor(anchor.id);
    });

    overlayContainer.append(overlay);
}

function updateFieldOverlay(fieldId) {
    const field = fieldMappings.find(f => f.id === fieldId);
    if (!field || field.pageNumber !== currentPage) return;

    pdfToScreenCoordinates(field.x, field.y, field.width, field.height)
        .then(function (screenCoords) {
            const overlay = $(`.field-overlay[data-field-id="${fieldId}"]`);
            if (overlay.length) {
                overlay.css({
                    left: screenCoords.x + 'px',
                    top: screenCoords.y + 'px',
                    width: screenCoords.width + 'px',
                    height: screenCoords.height + 'px'
                });
            }
        })
        .catch(function (error) {
            console.error('Error updating field overlay:', error);
        });
}

function updateAnchorOverlay(anchorId) {
    const anchor = anchorPoints.find(a => a.id === anchorId);
    if (!anchor || anchor.pageNumber !== currentPage) return;

    pdfToScreenCoordinates(anchor.x, anchor.y, anchor.width || 200, anchor.height || 50)
        .then(function (screenCoords) {
            const overlay = $(`.anchor-overlay[data-anchor-id="${anchorId}"]`);
            if (overlay.length) {
                overlay.css({
                    left: screenCoords.x + 'px',
                    top: screenCoords.y + 'px',
                    width: screenCoords.width + 'px',
                    height: screenCoords.height + 'px'
                });
            }
        })
        .catch(function (error) {
            console.error('Error updating anchor overlay:', error);
        });
}

// ================================================================
// ✅ UI UPDATE FUNCTIONS
// ================================================================

function updateFieldCount() {
    const count = fieldMappings ? fieldMappings.length : 0;
    $('#mapped-fields-count').text(count);
    $('#field-count-display').text(count);

    const noFieldsMsg = $('#no-fields-message');
    if (noFieldsMsg.length) {
        noFieldsMsg.toggle(count === 0);
    }
}

 

function updateAnchorCount() {
    const count = anchorPoints ? anchorPoints.length : 0;
    $('#anchor-count-display').text(count);
    $('#anchor-count-display-tab').text(count);

    const noAnchorsMsg = $('#no-anchors-message');
    if (noAnchorsMsg.length) {
        noAnchorsMsg.toggle(count === 0);
    }
}
 

 

function editField(fieldId) {
    const field = fieldMappings.find(f => f.id === fieldId);
    if (!field) {
        alert.error('Field not found');
        return;
    }

    if (field.pageNumber !== currentPage) {
        navigateToPage(field.pageNumber);
        setTimeout(() => openFieldModal('edit', fieldId), 500);
    } else {
        openFieldModal('edit', fieldId);
    }
}

function editAnchor(anchorId) {
    const anchor = anchorPoints.find(a => a.id === anchorId);
    if (!anchor) {
        alert.error('Anchor not found');
        return;
    }

    if (anchor.pageNumber !== currentPage) {
        navigateToPage(anchor.pageNumber);
        setTimeout(() => openAnchorModal('edit', anchorId), 500);
    } else {
        openAnchorModal('edit', anchorId);
    }
}

// ================================================================
// ✅ MODAL SAVE FUNCTIONS
// ================================================================

// ✅ FIX: Save field from modal (handles both add and edit)
async function saveFieldFromModal() {
    const fieldName = $('#field-name').val().trim();

    if (!fieldName) {
        alert.warning('Please enter a field name');
        $('#field-name').focus();
        return;
    }

    const editingId = $('#field-modal').data('editing-id');

    // Check for duplicate field names (excluding current field if editing)
    const duplicateField = fieldMappings.find(f =>
        f.fieldName.toLowerCase() === fieldName.toLowerCase() &&
        (!editingId || f.id !== editingId)
    );

    if (duplicateField) {
        alert.warning('Field name already exists');
        $('#field-name').focus();
        return;
    }

    try {
        $('#save-field').prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-1"></i>Saving...');

        if (isEditingField && editingId) {
            // Update existing field
            await updateExistingField(editingId);
        } else {
            // Add new field
            await addNewField();
        }

    } catch (error) {
        console.error('Error saving field:', error);
        alert.error('Error saving field mapping');
    } finally {
        $('#save-field').prop('disabled', false);
        if (isEditingField) {
            $('#save-field').html('<i class="fa fa-save me-1"></i>Update Field');
        } else {
            $('#save-field').html('<i class="fa fa-save me-1"></i>Save Field');
        }
    }
}

 

// ================================================================
// ✅ ADD FUNCTIONS
// ================================================================

async function addNewField() {
    if (!currentSelection || currentSelection.width <= 0) {
        alert.error('No area selected. Please select an area on the PDF first.');
        return;
    }

    const pdfCoords = await screenToPdfCoordinates(
        currentSelection.x,
        currentSelection.y,
        currentSelection.width,
        currentSelection.height
    );

    const fieldData = {
        FieldName: $('#field-name').val().trim(),
        DisplayName: $('#field-name').val().trim(),
        DataType: 'number',
        Description: $('#field-description').val().trim(),
        X: pdfCoords.x,
        Y: pdfCoords.y,
        Width: pdfCoords.width,
        Height: pdfCoords.height,
        PageNumber: currentPage,
        IsRequired: $('#field-required').is(':checked'),
        BorderColor: '#A54EE1',
        IsVisible: true
    };

    const ajaxResult = await addFieldMappingAjax(fieldData);

    if (ajaxResult.success) {
        const serverField = ajaxResult.data;

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
        renderFieldOverlays();
        updateFieldCount();
        updateFieldsTable();
        $('#field-modal').modal('hide');
        alert.success('Field mapping added successfully', { popup: false });
    }
}

// ✅ FIX: Complete saveAnchorFromModal function
async function saveAnchorFromModal() {
    console.log('🔗 [SAVE] Save anchor button clicked');

    const anchorName = $('#anchor-name').val().trim();
    const referenceText = $('#anchor-reference-text').val().trim();

    if (!anchorName) {
        alert.warning('Please enter an anchor name');
        $('#anchor-name').focus();
        return;
    }

    if (!referenceText) {
        alert.warning('Please enter reference text');
        $('#anchor-reference-text').focus();
        return;
    }

    const editingId = $('#anchor-modal').data('editing-id');
    console.log('🔗 [SAVE] Editing ID:', editingId, 'isEditingAnchor:', isEditingAnchor);

    // Check for duplicate names (excluding current anchor if editing)
    const duplicate = anchorPoints.find(a =>
        a.name.toLowerCase() === anchorName.toLowerCase() &&
        (!editingId || a.id !== editingId)
    );

    if (duplicate) {
        alert.warning('Anchor name already exists');
        $('#anchor-name').focus();
        return;
    }

    try {
        $('#save-anchor').prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-1"></i>Saving...');

        if (isEditingAnchor && editingId) {
            console.log('🔗 [SAVE] Updating existing anchor:', editingId);
            await updateExistingAnchor(editingId);
        } else {
            console.log('🔗 [SAVE] Adding new anchor');
            await addNewAnchor();
        }

    } catch (error) {
        console.error('Error saving anchor:', error);
        alert.error('Error saving anchor point: ' + error.message);
    } finally {
        $('#save-anchor').prop('disabled', false);
        if (isEditingAnchor) {
            $('#save-anchor').html('<i class="fa fa-save me-1"></i>Update Anchor');
        } else {
            $('#save-anchor').html('<i class="fa fa-anchor me-1"></i>Save Anchor');
        }
    }
}

// ✅ FIX: Make sure the event handler is properly attached
function setupAnchorEventHandlers() {
    console.log('🔗 Setting up anchor event handlers...');

    // ✅ FIX: Anchor modal save button handler (with explicit event prevention)
    $('#save-anchor').off('click').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        console.log('🔗 [SAVE] Save anchor button clicked via event handler');
        saveAnchorFromModal();
    });

    // Mode switching buttons
    $('#field-mapping-mode').off('click').on('click', function () {
        switchMappingMode('field');
    });

    $('#anchor-mode').off('click').on('click', function () {
        switchMappingMode('anchor');
    });

    // Toggle anchor visibility
    $('#toggle-anchor-visibility').off('click').on('click', function () {
        anchorsVisible = !anchorsVisible;
        if (anchorsVisible) {
            $(this).html('<i class="fa fa-eye-slash me-1"></i>Hide');
            renderAnchorOverlays();
        } else {
            $(this).html('<i class="fa fa-eye me-1"></i>Show');
            $('.anchor-overlay').hide();
        }
    });

    console.log('🔗 Anchor event handlers setup complete');
}

// ✅ FIX: Complete addNewAnchor function
async function addNewAnchor() {
    console.log('🔗 [ADD] Adding new anchor');

    if (!currentSelection || currentSelection.width <= 0) {
        alert.error('No area selected. Please draw an area on the PDF first.');
        return;
    }

    const pdfCoords = await screenToPdfCoordinates(
        currentSelection.x,
        currentSelection.y,
        currentSelection.width,
        currentSelection.height
    );

    console.log('🔗 [ADD] PDF coordinates:', pdfCoords);

    const anchorData = {
        TemplateId: parseInt(wizardData.templateId),
        PageNumber: currentPage || 1,
        Name: $('#anchor-name').val().trim(),
        Description: $('#anchor-description').val().trim() || '',
        X: pdfCoords.x,
        Y: pdfCoords.y,
        Width: pdfCoords.width,
        Height: pdfCoords.height,
        ReferenceText: $('#anchor-reference-text').val().trim(),
        ReferencePattern: '',
        IsRequired: $('#anchor-required').is(':checked') || false,
        Tolerance: parseFloat($('#anchor-tolerance').val()) || 10,
        ConfidenceThreshold: 0.8,
        SearchRadius: 200.0,
        DisplayOrder: anchorPoints.length,
        Color: '#00C48C',
        BorderColor: '#00C48C',
        IsVisible: true
    };

    console.log('🔗 [ADD] Sending anchor data:', anchorData);

    const ajaxResult = await addAnchorPointAjax(anchorData);

    if (ajaxResult.success) {
        console.log('🔗 [ADD] Anchor added successfully:', ajaxResult.data);

        const serverAnchor = ajaxResult.data;

        const newAnchor = {
            id: serverAnchor.Id || serverAnchor.id,
            templateId: serverAnchor.TemplateId || serverAnchor.templateId,
            pageNumber: serverAnchor.PageNumber || serverAnchor.pageNumber,
            name: serverAnchor.Name || serverAnchor.name,
            description: serverAnchor.Description || serverAnchor.description || '',
            x: parseFloat(serverAnchor.X || serverAnchor.x),
            y: parseFloat(serverAnchor.Y || serverAnchor.y),
            width: parseFloat(serverAnchor.Width || serverAnchor.width),
            height: parseFloat(serverAnchor.Height || serverAnchor.height),
            referenceText: serverAnchor.ReferenceText || serverAnchor.referenceText,
            referencePattern: serverAnchor.ReferencePattern || serverAnchor.referencePattern || '',
            isRequired: serverAnchor.IsRequired || serverAnchor.isRequired || false,
            tolerance: parseFloat(serverAnchor.Tolerance || serverAnchor.tolerance) || 10,
            confidenceThreshold: parseFloat(serverAnchor.ConfidenceThreshold || serverAnchor.confidenceThreshold) || 0.8,
            searchRadius: parseFloat(serverAnchor.SearchRadius || serverAnchor.searchRadius) || 200,
            displayOrder: parseInt(serverAnchor.DisplayOrder || serverAnchor.displayOrder) || anchorPoints.length,
            color: serverAnchor.Color || serverAnchor.color || '#00C48C',
            borderColor: serverAnchor.BorderColor || serverAnchor.borderColor || '#00C48C',
            isVisible: serverAnchor.IsVisible !== false && serverAnchor.isVisible !== false
        };

        anchorPoints.push(newAnchor);
        updateAnchorCount();
        updateAnchorTable();
        renderAnchorOverlays();
        $('#anchor-modal').modal('hide');
        alert.success('Anchor point added successfully', { popup: false });
    } else {
        console.error('🔗 [ADD] Failed to add anchor:', ajaxResult.message);
        alert.error('Failed to add anchor: ' + (ajaxResult.message || 'Unknown error'));
    }
}

// ================================================================
// ✅ UPDATE FUNCTIONS
// ================================================================

// ✅ FIX: Update existing field (complete function)
async function updateExistingField(fieldId) {
    const field = fieldMappings.find(f => f.id === fieldId);
    if (!field) {
        alert.error('Field not found');
        return;
    }

    console.log('📝 [UPDATE] Updating field:', fieldId);

    // Update local field data with form values
    field.fieldName = $('#field-name').val().trim();
    field.displayName = $('#field-name').val().trim();
    field.description = $('#field-description').val().trim();
    field.isRequired = $('#field-required').is(':checked');

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

    console.log('📝 [UPDATE] Sending update data:', updateData);

    const result = await updateFieldMappingAjax(fieldId, updateData, true);

    if (result.success) {
        console.log('✅ [UPDATE] Field updated successfully');

        // Update UI
        updateFieldsTable();
        renderFieldOverlays();

        // Hide modal
        $('#field-modal').modal('hide');

        alert.success('Field updated successfully', { popup: false });
    } else {
        console.error('❌ [UPDATE] Update failed:', result.message);
        alert.error('Failed to update field: ' + (result.message || 'Unknown error'));
    }
}

async function updateExistingAnchor(anchorId) {
    const anchor = anchorPoints.find(a => a.id === anchorId);
    if (!anchor) {
        alert.error('Anchor not found');
        return;
    }

    anchor.name = $('#anchor-name').val().trim();
    anchor.referenceText = $('#anchor-reference-text').val().trim();
    anchor.description = $('#anchor-description').val().trim();
    anchor.isRequired = $('#anchor-required').is(':checked');

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

    const result = await updateAnchorPointAjax(anchorId, updateData);

    if (result.success) {
        updateAnchorTable();
        renderAnchorOverlays();
        $('#anchor-modal').modal('hide');
        alert.success('Anchor point updated successfully', { popup: false });
    }
}

// ================================================================
// ✅ AJAX FUNCTIONS (PLACEHOLDERS - TO BE IMPLEMENTED)
// ================================================================

// ✅ DEBUG: Add logging to updateFieldMappingAjax
async function updateFieldMappingAjax(fieldId, fieldData, showSuccessMessage = false) {
    try {
        console.log('📤 [AJAX] Updating field mapping:', fieldId, fieldData);

        if (!fieldId || isNaN(fieldId)) {
            throw new Error('Invalid field ID: ' + fieldId);
        }

        const response = await fetch('/Template/UpdateFieldMapping', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                FieldMappingId: fieldId,
                FieldMapping: fieldData
            })
        });

        console.log('📤 [AJAX] Response status:', response.status);

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();
        console.log('📤 [AJAX] Update response:', result);

        if (result.success) {
            console.log('✅ [AJAX] Field mapping updated successfully');
            if (showSuccessMessage) {
                //alert.success('Field updated successfully', { popup: false });
            }
            return { success: true, data: result.data };
        } else {
            console.error('❌ [AJAX] Failed to update field:', result.message);
            alert.error(result.message || 'Failed to update field mapping');
            return { success: false, message: result.message };
        }
    } catch (error) {
        console.error('❌ [AJAX] Error updating field:', error);
        alert.error('Error updating field mapping: ' + error.message);
        return { success: false, message: error.message };
    }
}

async function addFieldMappingAjax(fieldData) {
    try {
        console.log('📤 [AJAX] Adding field mapping:', fieldData);

        const templateId = parseInt(wizardData.templateId);
        if (isNaN(templateId) || templateId <= 0) {
            console.error('❌ [AJAX] Invalid template ID:', wizardData.templateId);
            alert.error('Invalid template ID');
            return { success: false, message: 'Invalid template ID' };
        }

        const response = await fetch('/Template/AddFieldMapping', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                TemplateId: templateId,
                FieldMapping: fieldData
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log('📤 [AJAX] Add response:', result);

        if (result.success) {
            console.log('✅ [AJAX] Field mapping added successfully:', result.data);

            // Normalize the server response
            const serverField = result.data;
            const normalizedField = {
                Id: serverField.Id || serverField.id,
                FieldName: serverField.FieldName || serverField.fieldName,
                DisplayName: serverField.DisplayName || serverField.displayName || serverField.FieldName || serverField.fieldName,
                DataType: serverField.DataType || serverField.dataType,
                Description: serverField.Description || serverField.description,
                X: serverField.X || serverField.x,
                Y: serverField.Y || serverField.y,
                Width: serverField.Width || serverField.width,
                Height: serverField.Height || serverField.height,
                PageNumber: serverField.PageNumber || serverField.pageNumber,
                IsRequired: serverField.IsRequired || serverField.isRequired,
                BorderColor: serverField.BorderColor || serverField.borderColor,
                IsVisible: serverField.IsVisible !== false && serverField.isVisible !== false
            };

            return { success: true, data: normalizedField };
        } else {
            console.error('❌ [AJAX] Server returned error:', result.message);
            alert.error(result.message || 'Failed to add field mapping');
            return { success: false, message: result.message };
        }
    } catch (error) {
        console.error('❌ [AJAX] Error adding field:', error);
        alert.error('Error adding field mapping');
        return { success: false, message: error.message };
    }
}

async function updateAnchorPointAjax(anchorId, anchorData) {
    try {
        console.log('📤 [AJAX] Updating anchor:', anchorId, anchorData);

        const response = await fetch('/Template/UpdateTemplateAnchor', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                TemplateAnchorId: anchorId,
                TemplateAnchor: anchorData
            })
        });

        const result = await response.json();

        if (result.success) {
            console.log('✅ [AJAX] Anchor updated successfully');
            return { success: true, data: result.data };
        } else {
            console.error('❌ [AJAX] Update failed:', result.message);
            alert.error(result.message || 'Failed to update anchor point');
            return { success: false, message: result.message };
        }
    } catch (error) {
        console.error('❌ [AJAX] Error updating anchor:', error);
        alert.error('Error updating anchor point');
        return { success: false, message: error.message };
    }
}

async function addAnchorPointAjax(anchorData) {
    try {
        const response = await fetch('/Template/AddTemplateAnchor', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                TemplateId: parseInt(wizardData.templateId),
                TemplateAnchor: anchorData
            })
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText.substring(0, 200)}`);
        }

        const result = await response.json();

        if (result.success && result.data) {
            return { success: true, data: result.data };
        } else {
            const errorMsg = result.message || 'Failed to add anchor point';
            alert.error(errorMsg);
            return { success: false, message: errorMsg };
        }
    } catch (error) {
        alert.error(`Error adding anchor point: ${error.message}`);
        return { success: false, message: error.message };
    }
}

async function loadFieldMappingsAjax(templateId) {
    try {
        console.log('📥 [AJAX] Loading field mappings for template:', templateId);

        const response = await fetch(`/Template/GetFieldMappings?templateId=${templateId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success) {
            console.log('✅ [AJAX] Loaded', result.count, 'field mappings');

            fieldMappings = result.data.map((serverField, index) => ({
                id: parseInt(serverField.Id || serverField.id || 0),
                fieldName: String(serverField.FieldName || serverField.fieldName || ''),
                displayName: String(serverField.DisplayName || serverField.displayName || serverField.FieldName || serverField.fieldName || ''),
                dataType: String(serverField.DataType || serverField.dataType || 'number').toLowerCase(),
                description: String(serverField.Description || serverField.description || ''),
                x: parseFloat(serverField.X || serverField.x || 0),
                y: parseFloat(serverField.Y || serverField.y || 0),
                width: parseFloat(serverField.Width || serverField.width || 0.1),
                height: parseFloat(serverField.Height || serverField.height || 0.1),
                pageNumber: parseInt(serverField.PageNumber || serverField.pageNumber || 1),
                isRequired: Boolean(serverField.IsRequired || serverField.isRequired),
                borderColor: String(serverField.BorderColor || serverField.borderColor || '#A54EE1'),
                isVisible: serverField.IsVisible !== false && serverField.isVisible !== false,
                coordinateType: 'pdf-relative'
            }));

            setTimeout(() => {
                updateFieldCount();
                updateFieldsTable();
                if (canvas && ctx && pdfDoc && currentPage) {
                    renderFieldOverlays();
                }
            }, 200);

            return { success: true, data: fieldMappings };
        } else {
            throw new Error(result.message || 'Server returned error');
        }
    } catch (error) {
        console.error('❌ [AJAX] Error loading fields:', error);
        return { success: false, message: error.message };
    }
}

async function loadAnchorPointsAjax(templateId) {
    try {
        console.log('📥 [AJAX] Loading anchor points for template:', templateId);

        const response = await fetch(`/Template/GetTemplateAnchors?templateId=${templateId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success) {
            console.log('✅ [AJAX] Loaded', result.count, 'anchor points');

            anchorPoints = result.data.map(serverAnchor => ({
                id: serverAnchor.Id,
                name: serverAnchor.Name,
                templateId: serverAnchor.TemplateId,
                pageNumber: serverAnchor.PageNumber,
                description: serverAnchor.Description,
                x: serverAnchor.X,
                y: serverAnchor.Y,
                width: serverAnchor.Width,
                height: serverAnchor.Height,
                tolerance: serverAnchor.Tolerance,
                referenceText: serverAnchor.ReferenceText,
                referencePattern: serverAnchor.ReferencePattern,
                isRequired: serverAnchor.IsRequired,
                confidenceThreshold: serverAnchor.ConfidenceThreshold,
                searchRadius: serverAnchor.SearchRadius,
                displayOrder: serverAnchor.DisplayOrder,
                color: serverAnchor.Color,
                borderColor: serverAnchor.BorderColor,
                isVisible: serverAnchor.IsVisible
            }));

            updateAnchorCount();
            updateAnchorTable();
            if (currentMappingMode === 'anchor') {
                renderAnchorOverlays();
            }

            return { success: true, data: anchorPoints };
        } else {
            throw new Error(result.message || 'Server returned error');
        }
    } catch (error) {
        console.error('❌ [AJAX] Error loading anchor points:', error);
        return { success: false, message: error.message };
    }
}

// ================================================================
// ✅ DELETE FUNCTIONS
// ================================================================

async function deleteFieldWithAjax(fieldId) {
    try {
        console.log('🗑️ [DELETE] Starting delete for field ID:', fieldId);

        if (!fieldId || isNaN(parseInt(fieldId))) {
            console.error('❌ [DELETE] Invalid field ID provided:', fieldId);
            alert.error('Invalid field ID');
            return;
        }

        const field = fieldMappings.find(f => f.id === parseInt(fieldId));
        if (!field) {
            console.error('❌ [DELETE] Field not found:', fieldId);
            alert.error('Field not found');
            return;
        }

        const confirmed = await showConfirmDialog(
            'Delete Field Mapping',
            `Are you sure you want to delete the field "${field.fieldName}"?`,
            'Delete',
            'Cancel'
        );

        if (!confirmed) return;

        const ajaxResult = await removeFieldMappingAjax(parseInt(fieldId));
        // removeFieldMappingAjax handles UI updates

    } catch (error) {
        console.error('❌ [DELETE] Error in deleteFieldWithAjax:', error);
        alert.error('Error deleting field mapping');
    }
}

async function deleteAnchorWithAjax(anchorId) {
    try {
        const anchor = anchorPoints.find(a => a.id === anchorId);
        if (!anchor) {
            alert.error('Anchor not found');
            return;
        }

        const confirmed = await showConfirmDialog(
            'Delete Anchor Point',
            `Are you sure you want to delete the anchor "${anchor.name}"?`,
            'Delete',
            'Cancel'
        );

        if (!confirmed) return;

        const result = await removeAnchorPointAjax(anchor.templateId, anchorId);
        // removeAnchorPointAjax handles UI updates

    } catch (error) {
        console.error('Error deleting anchor:', error);
        alert.error('Error deleting anchor point');
    }
}

async function removeFieldMappingAjax(fieldId) {
    try {
        console.log('📤 [AJAX] Removing field mapping:', fieldId);

        const templateId = parseInt(wizardData.templateId);
        if (isNaN(templateId) || templateId <= 0) {
            console.error('❌ [AJAX] Invalid template ID:', wizardData.templateId);
            alert.error('Invalid template ID');
            return { success: false, message: 'Invalid template ID' };
        }

        const response = await fetch('/Template/RemoveFieldMapping', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                TemplateId: templateId,
                FieldMappingId: fieldId
            })
        });

        const result = await response.json();

        if (result.success) {
            console.log('✅ [AJAX] Field mapping removed successfully');

            // Remove from local array
            fieldMappings = fieldMappings.filter(f => f.id !== fieldId);

            // Update UI
            renderFieldOverlays();
            updateFieldCount();
            updateFieldsTable();

            alert.success('Field mapping removed successfully', { popup: false });
            return { success: true };
        } else {
            console.error('❌ [AJAX] Failed to remove field:', result.message);
            alert.error(result.message || 'Failed to remove field mapping');
            return { success: false, message: result.message };
        }
    } catch (error) {
        console.error('❌ [AJAX] Error removing field:', error);
        alert.error('Error removing field mapping');
        return { success: false, message: error.message };
    }
}

async function removeAnchorPointAjax(templateId, anchorId) {
    try {
        const response = await fetch('/Template/RemoveTemplateAnchor', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                TemplateId: templateId,
                TemplateAnchorId: anchorId
            })
        });

        const result = await response.json();

        if (result.success) {
            anchorPoints = anchorPoints.filter(a => a.id !== anchorId);

            updateAnchorCount();
            updateAnchorTable();
            renderAnchorOverlays();

            alert.success('Anchor point removed successfully', { popup: false });
            return { success: true };
        } else {
            alert.error(result.message || 'Failed to remove anchor point');
            return { success: false, message: result.message };
        }
    } catch (error) {
        console.error('❌ [AJAX] Error removing anchor:', error);
        alert.error('Error removing anchor point');
        return { success: false, message: error.message };
    }
}

// ================================================================
// ✅ UTILITY FUNCTIONS
// ================================================================

function showConfirmDialog(title, message, confirmText = 'Delete', cancelText = 'Cancel') {
    return new Promise((resolve) => {
        if (typeof swal !== 'undefined') {
            swal({
                title: title,
                text: message,
                icon: 'warning',
                dangerMode: true,
                buttons: {
                    cancel: {
                        text: cancelText,
                        value: false,
                        visible: true,
                        className: 'btn btn-default',
                        closeModal: true
                    },
                    confirm: {
                        text: confirmText,
                        value: true,
                        visible: true,
                        className: 'btn btn-danger',
                        closeModal: true
                    }
                },
                closeOnClickOutside: false,
                closeOnEsc: true
            }).then(function (result) {
                resolve(result === true);
            }).catch(function (error) {
                resolve(false);
            });
        } else {
            const result = confirm(`${title}\n\n${message}`);
            resolve(result);
        }
    });
}

// ================================================================
// ✅ WIZARD INTEGRATION FUNCTIONS
// ================================================================

function getStep3FormData() {
    if (!fieldMappings || fieldMappings.length === 0) {
        return {
            FieldMappings: [],
            AnchorPoints: []
        };
    }

    const formattedFieldMappings = fieldMappings.map(field => ({
        Id: field.id || 0,
        FieldName: field.fieldName || '',
        DisplayName: field.displayName || field.fieldName || '',
        DataType: field.dataType || 'number',
        Description: field.description || '',
        X: field.x || 0,
        Y: field.y || 0,
        Width: field.width || 100,
        Height: field.height || 30,
        PageNumber: field.pageNumber || 1,
        IsRequired: field.isRequired || false,
        UseOCR: false,
        ValidationPattern: '',
        IsVisible: true,
        BorderColor: field.borderColor || '#A54EE1',
        CoordinateType: field.coordinateType || 'pdf-relative'
    }));

    const formattedAnchorPoints = anchorPoints.map(anchor => ({
        Id: anchor.id || 0,
        TemplateId: anchor.templateId || 0,
        PageNumber: anchor.pageNumber || 1,
        Name: anchor.name || '',
        Description: anchor.description || '',
        X: anchor.x || 0,
        Y: anchor.y || 0,
        Width: anchor.width || 200,
        Height: anchor.height || 50,
        ReferenceText: anchor.referenceText || '',
        ReferencePattern: anchor.referencePattern || '',
        IsRequired: anchor.isRequired || false,
        ConfidenceThreshold: anchor.confidenceThreshold || 0.8,
        SearchRadius: anchor.searchRadius || 200,
        DisplayOrder: anchor.displayOrder || 0,
        Color: anchor.color || '#00C48C',
        BorderColor: anchor.borderColor || '#00C48C',
        IsVisible: anchor.isVisible !== false
    }));

    return {
        FieldMappings: formattedFieldMappings,
        AnchorPoints: formattedAnchorPoints
    };
}

function validateStep3Custom() {
    const fieldCount = fieldMappings ? fieldMappings.length : 0;
    if (fieldCount === 0) {
        alert.warning('Please map at least one field before proceeding');
        return false;
    }

    let hasValidFields = true;
    if (fieldMappings) {
        fieldMappings.forEach(field => {
            if (!field.fieldName || field.fieldName.trim().length < 2) {
                hasValidFields = false;
            }
        });
    }

    if (!hasValidFields) {
        alert.warning('All mapped fields must have valid names');
        return false;
    }

    return true;
}

async function saveStep3Data() {
    return true;
}
// ================================================================
// CENTRALIZED SELECTION SYSTEM FIX
// Add this to your main wizard-step3.js file
// ================================================================

// ✅ FIX 1: Update your existing renderFieldOverlays function
// Replace the existing renderFieldOverlays function with this enhanced version
function renderFieldOverlays() {
    console.log('🎨 [UI] Rendering field overlays for page', currentPage);

    const overlayContainer = $('#field-overlays');
    if (!overlayContainer.length || !pdfDoc || !canvas || !ctx) return;

    // Remove only field overlays
    $('.field-overlay').remove();

    const currentPageFields = fieldMappings.filter(f => f.pageNumber === currentPage);
    if (currentPageFields.length === 0) return;

    currentPageFields.forEach(function (field) {
        pdfToScreenCoordinates(field.x, field.y, field.width, field.height)
            .then(function (screenCoords) {
                createFieldOverlay(field, screenCoords);

                // ✅ CENTRALIZED: Re-apply selection styling after overlay creation
                setTimeout(() => {
                    restoreFieldSelectionStyling(field.id);
                }, 10);
            })
            .catch(function (error) {
                console.error('🎨 [UI] Error converting coordinates for', field.fieldName, ':', error);
            });
    });
}

// ✅ FIX 2: Add centralized selection styling restoration
function restoreFieldSelectionStyling(fieldId) {
    // Check if field is in multi-selection
    if (typeof multiSelection !== 'undefined' && multiSelection.selectedFields && multiSelection.selectedFields.has(fieldId)) {
        const overlay = $(`.field-overlay[data-field-id="${fieldId}"]`);
        if (overlay.length > 0 && !overlay.hasClass('multi-selected')) {
            overlay.addClass('multi-selected');
            console.log(`🔄 [RESTORE] Re-applied multi-selection styling to field ${fieldId}`);
        }
    }

    // Check if field is single-selected
    if (typeof selectedFieldId !== 'undefined' && selectedFieldId === fieldId) {
        const overlay = $(`.field-overlay[data-field-id="${fieldId}"]`);
        if (overlay.length > 0 && !overlay.hasClass('selected')) {
            overlay.addClass('selected');
            console.log(`🔄 [RESTORE] Re-applied single selection styling to field ${fieldId}`);
        }
    }
}

// ✅ FIX 3: Add centralized selection styling restoration for anchors
function restoreAnchorSelectionStyling(anchorId) {
    // Check if anchor is in multi-selection
    if (typeof multiSelection !== 'undefined' && multiSelection.selectedAnchors && multiSelection.selectedAnchors.has(anchorId)) {
        const overlay = $(`.anchor-overlay[data-anchor-id="${anchorId}"]`);
        if (overlay.length > 0 && !overlay.hasClass('multi-selected')) {
            overlay.addClass('multi-selected');
            console.log(`🔄 [RESTORE] Re-applied multi-selection styling to anchor ${anchorId}`);
        }
    }

    // Check if anchor is single-selected
    if (typeof selectedAnchorId !== 'undefined' && selectedAnchorId === anchorId) {
        const overlay = $(`.anchor-overlay[data-anchor-id="${anchorId}"]`);
        if (overlay.length > 0 && !overlay.hasClass('selected')) {
            overlay.addClass('selected');
            console.log(`🔄 [RESTORE] Re-applied single selection styling to anchor ${anchorId}`);
        }
    }
}

// ✅ FIX 4: Update your existing renderAnchorOverlays function
// Replace the existing renderAnchorOverlays function with this enhanced version
function renderAnchorOverlays() {
    console.log('🔗 Rendering anchor overlays for page', currentPage);

    const overlayContainer = $('#field-overlays');
    if (!overlayContainer.length || !pdfDoc || !canvas || !ctx) return;

    // Remove only anchor overlays
    $('.anchor-overlay').remove();

    if (!anchorPoints || anchorPoints.length === 0 || !anchorsVisible) {
        return;
    }

    const currentPageAnchors = anchorPoints.filter(a => a.pageNumber === currentPage);
    if (currentPageAnchors.length === 0) return;

    currentPageAnchors.forEach(function (anchor) {
        pdfToScreenCoordinates(anchor.x, anchor.y, anchor.width || 200, anchor.height || 50)
            .then(function (screenCoords) {
                createAnchorOverlay(anchor, screenCoords);

                // ✅ CENTRALIZED: Re-apply selection styling after overlay creation
                setTimeout(() => {
                    restoreAnchorSelectionStyling(anchor.id);
                }, 10);
            })
            .catch(function (error) {
                console.error('🔗 Error converting anchor coordinates:', error);
            });
    });
}

// ✅ FIX 5: Add centralized selection restoration function
function restoreAllSelectionStyling() {
    console.log('🔄 [RESTORE] Restoring all selection styling after DOM changes');

    // Restore field selections
    if (fieldMappings && fieldMappings.length > 0) {
        fieldMappings.forEach(field => {
            if (field.pageNumber === currentPage) {
                restoreFieldSelectionStyling(field.id);
            }
        });
    }

    // Restore anchor selections
    if (anchorPoints && anchorPoints.length > 0) {
        anchorPoints.forEach(anchor => {
            if (anchor.pageNumber === currentPage) {
                restoreAnchorSelectionStyling(anchor.id);
            }
        });
    }

    // Update multi-selection UI
    if (typeof updateMultiSelectionUI === 'function') {
        updateMultiSelectionUI();
    }
}

// ✅ FIX 6: Update your existing updateZoomAndRender function
// Find this function in your wizard-step3.js and replace it with this version
function updateZoomAndRender(preserveScrollTop, preserveScrollLeft) {
    if (!pdfDoc || !canvas) return;

    // Cancel any existing render
    if (currentRender) {
        currentRender.cancel();
        currentRender = null;
    }

    pdfDoc.getPage(currentPage).then(function (page) {
        const container = $('#pdf-viewer-container');
        const containerWidth = container.width() - 40;
        const baseScale = containerWidth / page.getViewport({ scale: 1.0 }).width;
        const actualScale = baseScale * currentZoom;
        const viewport = page.getViewport({ scale: actualScale });

        canvas.width = viewport.width;
        canvas.height = viewport.height;
        canvas.style.width = viewport.width + 'px';
        canvas.style.height = viewport.height + 'px';

        const renderContext = { canvasContext: canvas.getContext('2d'), viewport: viewport };
        currentRender = page.render(renderContext);

        currentRender.promise.then(function () {
            currentRender = null;
            $('#zoom-level').text(Math.round(currentZoom * 100));

            if (typeof preserveScrollTop !== 'undefined') {
                container.scrollTop(preserveScrollTop);
            }
            if (typeof preserveScrollLeft !== 'undefined') {
                container.scrollLeft(preserveScrollLeft);
            }

            // Re-render overlays
            renderFieldOverlays();
            renderAnchorOverlays();

            // ✅ CENTRALIZED: Restore all selection styling after zoom
            setTimeout(() => {
                restoreAllSelectionStyling();
            }, 50);

        }).catch(function (error) {
            if (error.name !== 'RenderingCancelledException') {
                console.error('PDF render error:', error);
            }
        });
    });
}

// ✅ FIX 7: Update your existing loadPage function
// Find this function in your wizard-step3.js and add selection restoration
function loadPage(pageNum) {
    if (!pdfDoc || pageNum < 1 || pageNum > totalPages) return;

    // Cancel any existing render
    if (currentRender) {
        currentRender.cancel();
        currentRender = null;
    }

    pdfDoc.getPage(pageNum).then(function (page) {
        const actualViewport = page.getViewport({ scale: 1.0 });
        console.log(`📏 [PAGE SIZE] Page ${pageNum} - PDF Metadata:`, {
            width: actualViewport.width,
            height: actualViewport.height,
            userUnit: page.userUnit,
            rotate: page.rotate
        });

        const containerWidth = $('#pdf-viewer-container').width() - 40;
        const baseScale = containerWidth / actualViewport.width;
        const finalScale = baseScale * currentZoom;
        const scaledViewport = page.getViewport({ scale: finalScale });

        canvas.width = scaledViewport.width;
        canvas.height = scaledViewport.height;
        canvas.style.width = scaledViewport.width + 'px';
        canvas.style.height = scaledViewport.height + 'px';

        const renderContext = { canvasContext: ctx, viewport: scaledViewport };
        currentRender = page.render(renderContext);

        currentRender.promise.then(function () {
            currentRender = null;
            currentPage = pageNum;
            $('#current-page').text(currentPage);
            $('#page-info').text(`Page ${currentPage} of ${totalPages}`);
            updateNavigationButtons();

            // Render overlays
            setTimeout(() => {
                renderFieldOverlays();
                renderAnchorOverlays();

                // ✅ CENTRALIZED: Restore all selection styling after page change
                setTimeout(() => {
                    restoreAllSelectionStyling();
                }, 100);
            }, 100);
        }).catch(function (error) {
            if (error.name !== 'RenderingCancelledException') {
                console.error('PDF render error:', error);
            }
        });
    });
}

// ✅ FIX 8: Enhanced bulk fields creation with centralized selection
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

        const pdfCoords = await screenToPdfCoordinates(
            bulkSelection.x,
            bulkSelection.y,
            bulkSelection.width,
            bulkSelection.height
        );

        const fieldWidth = pdfCoords.width;
        const fieldHeight = pdfCoords.height / lines.length;
        const createdFieldIds = [];

        // Create fields
        for (let i = 0; i < lines.length; i++) {
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
            }
        }

        // Force UI update
        renderFieldOverlays();
        updateFieldCount();
        updateFieldsTable();

        // Wait for DOM
        await new Promise(resolve => setTimeout(resolve, 200));

        // ✅ CENTRALIZED AUTO-SELECTION: Use existing multi-selection system
        if (createdFieldIds.length > 0) {
            console.log('🎯 [BULK AUTO-SELECT] Using centralized selection for', createdFieldIds.length, 'fields');

            // Clear existing selections using centralized method
            if (typeof clearMultiSelection === 'function') {
                clearMultiSelection();
            }

            // Add to multi-selection using centralized method
            createdFieldIds.forEach(fieldId => {
                if (typeof addToMultiSelection === 'function') {
                    addToMultiSelection('field', fieldId);
                } else {
                    // Fallback: direct addition
                    if (typeof multiSelection !== 'undefined') {
                        multiSelection.selectedFields.add(fieldId);
                        multiSelection.isActive = true;
                    }
                }
            });

            // Update UI using centralized method
            if (typeof updateMultiSelectionUI === 'function') {
                updateMultiSelectionUI();
            }

            // ✅ ENSURE STYLING: Force restoration of selection styling
            setTimeout(() => {
                restoreAllSelectionStyling();
                console.log('✅ [BULK] All bulk fields selected with centralized styling');
            }, 100);
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



// ================================================================
// MODAL CANCEL RESET FIX - Add to wizard-step3.js
// ================================================================

// ✅ ADD: Reset function to clear drawing state
function resetDrawingState() {
    console.log('🔄 [RESET] Clearing drawing state after cancel');

    // Clear selection area
    currentSelection = { x: 0, y: 0, width: 0, height: 0 };

    // Hide selection box
    $('#selection-box').hide();

    // Reset selection flags
    isSelecting = false;

    // Clear start position
    startPos = { x: 0, y: 0 };

    console.log('✅ [RESET] Drawing state cleared');
}

// ✅ ENHANCED: Modal event handlers - Add these to your modal setup
function setupModalEventHandlers() {
    console.log('🔧 [MODAL] Setting up modal cancel handlers');

    // ✅ Field modal cancel/close handlers
    $('#field-modal').on('hidden.bs.modal', function () {
        console.log('🔄 [MODAL] Field modal closed - resetting drawing state');
        resetDrawingState();
    });

    $('#field-modal .btn-close, #field-modal [data-bs-dismiss="modal"]').on('click', function () {
        console.log('🔄 [MODAL] Field modal cancelled - resetting drawing state');
        resetDrawingState();
    });

    // ✅ Anchor modal cancel/close handlers
    $('#anchor-modal').on('hidden.bs.modal', function () {
        console.log('🔄 [MODAL] Anchor modal closed - resetting drawing state');
        resetDrawingState();
    });

    $('#anchor-modal .btn-close, #anchor-modal [data-bs-dismiss="modal"]').on('click', function () {
        console.log('🔄 [MODAL] Anchor modal cancelled - resetting drawing state');
        resetDrawingState();
    });

    // ✅ Bulk fields modal cancel/close handlers
    $('#bulk-fields-modal').on('hidden.bs.modal', function () {
        console.log('🔄 [MODAL] Bulk fields modal closed - resetting drawing state');
        resetDrawingState();
    });

    $('#bulk-fields-modal .btn-close, #bulk-fields-modal [data-bs-dismiss="modal"]').on('click', function () {
        console.log('🔄 [MODAL] Bulk fields modal cancelled - resetting drawing state');
        resetDrawingState();
    });

    console.log('✅ [MODAL] Modal cancel handlers setup complete');
}

// ✅ ENHANCED: setupMappingEventHandlers - Add reset to canvas mouseup
function setupMappingEventHandlers() {
    let isPanning = false;
    let lastPanX = 0;
    let lastPanY = 0;

    // Setup mode switching for ALL toolbars using classes
    $('.mode-field-btn').off('click').on('click', function () {
        switchMappingMode('field');
    });

    $('.mode-anchor-btn').off('click').on('click', function () {
        switchMappingMode('anchor');
    });

    // Toggle anchor visibility
    $('#toggle-anchor-visibility').off('click').on('click', function () {
        anchorsVisible = !anchorsVisible;

        if (anchorsVisible) {
            $(this).html('<i class="fa fa-eye-slash me-1"></i>Hide');
            renderAnchorOverlays();
        } else {
            $(this).html('<i class="fa fa-eye me-1"></i>Show');
            $('.anchor-overlay').hide();
        }
    });

    $('#save-field').off('click').on('click', function (e) {
        e.preventDefault();
        console.log('💾 [SAVE] Save field button clicked');
        saveFieldFromModal();
    });

    // Canvas mouse events
    $(canvas).on('mousedown', function (e) {
        if ($(e.target).closest('.anchor-overlay, .field-overlay').length > 0) {
            return;
        }

        if (e.ctrlKey || e.metaKey) {
            isPanning = true;
            lastPanX = e.clientX;
            lastPanY = e.clientY;
            $(this).css('cursor', 'grabbing');
            e.preventDefault();
            return false;
        }

        if ((currentMappingMode === 'field' || currentMappingMode === 'anchor') &&
            !$(e.target).hasClass('field-overlay') &&
            !$(e.target).hasClass('anchor-overlay')) {

            const rect = canvas.getBoundingClientRect();
            startPos.x = e.clientX - rect.left;
            startPos.y = e.clientY - rect.top;
            isSelecting = true;

            const selectionColor = currentMappingMode === 'anchor' ? '#00C48C' : '#A54EE1';
            const selectionBg = currentMappingMode === 'anchor' ? 'rgba(0,196,140,0.1)' : 'rgba(165,78,225,0.1)';

            $('#selection-box').show().css({
                left: startPos.x + 'px',
                top: startPos.y + 'px',
                width: '0px',
                height: '0px',
                'border-color': selectionColor,
                'background-color': selectionBg
            });

            console.log(`📋 [CANVAS] Started ${currentMappingMode} selection at:`, startPos);
        }
    });

    $(canvas).on('mousemove', function (e) {
        if (isPanning) {
            e.preventDefault();
            const container = $('#pdf-viewer-container');
            const deltaX = lastPanX - e.clientX;
            const deltaY = lastPanY - e.clientY;

            container.scrollLeft(container.scrollLeft() + deltaX);
            container.scrollTop(container.scrollTop() + deltaY);

            lastPanX = e.clientX;
            lastPanY = e.clientY;
            return false;
        }

        if (isSelecting && (currentMappingMode === 'field' || currentMappingMode === 'anchor')) {
            const rect = canvas.getBoundingClientRect();
            const currentX = e.clientX - rect.left;
            const currentY = e.clientY - rect.top;

            currentSelection = {
                x: Math.min(startPos.x, currentX),
                y: Math.min(startPos.y, currentY),
                width: Math.abs(currentX - startPos.x),
                height: Math.abs(currentY - startPos.y)
            };

            $('#selection-box').css({
                left: currentSelection.x + 'px',
                top: currentSelection.y + 'px',
                width: currentSelection.width + 'px',
                height: currentSelection.height + 'px'
            });
        }
    });

    $(canvas).on('mouseup', function (e) {
        if (isPanning) {
            isPanning = false;
            $(this).css('cursor', getCurrentModeCursor());
            return false;
        }

        if (isSelecting && (currentMappingMode === 'field' || currentMappingMode === 'anchor')) {
            isSelecting = false;
            $('#selection-box').hide();

            if (currentSelection.width > 10 && currentSelection.height > 10) {
                console.log(`📋 [CANVAS] Valid ${currentMappingMode} selection made:`, currentSelection);

                // Check for bulk fields mode first
                if (handleBulkFieldsSelection && handleBulkFieldsSelection(currentSelection)) {
                    return; // Bulk mode handled the selection
                }

                // Regular single field/anchor creation
                if (currentMappingMode === 'anchor') {
                    openAnchorModal('add');
                } else {
                    openFieldModal('add');
                }
            } else {
                console.log(`📋 [CANVAS] Selection too small, ignoring`);
                // ✅ FIX: Reset state even for small selections
                resetDrawingState();
            }
        }
    });

    $(canvas).on('mouseenter mousemove', function (e) {
        if ($(e.target).closest('.field-overlay, .anchor-overlay').length > 0) {
            return;
        }

        if (e.ctrlKey || e.metaKey) {
            $(this).css('cursor', 'grab');
        } else {
            $(this).css('cursor', getCurrentModeCursor());
        }
    });

    $(canvas).css('cursor', getCurrentModeCursor());

    console.log('✅ [EVENTS] Stable mapping event handlers with unified modes setup complete');
}

// ✅ ENHANCED: Add modal handlers to initialization
function initializeStep3() {
    console.log('🚀 Initializing Unified Step 3: PDF Mapping with Stable Systems');

    // Load field mappings from server data first
    loadServerFieldMappings();
    loadServerAnchorPoints();

    // If no fields loaded from server data, try AJAX as fallback
    if ((!fieldMappings || fieldMappings.length === 0) && wizardData?.templateId) {
        loadFieldMappingsAjax(wizardData.templateId);
    }

    // If no anchors loaded from server data, try AJAX as fallback
    if ((!anchorPoints || anchorPoints.length === 0) && wizardData?.templateId) {
        loadAnchorPointsAjax(wizardData.templateId);
    }

    // Get primary PDF file
    let primaryFile = getPrimaryPdfFile();
    if (!primaryFile) {
        alert.warning('No PDF files available. Please go back to Step 1 and upload a file.');
        return;
    }

    // Initialize canvas and PDF viewer
    setTimeout(() => {
        canvas = document.getElementById('pdf-canvas');
        if (canvas) {
            ctx = canvas.getContext('2d');
            setupAllEventHandlers();

            // ✅ NEW: Setup modal cancel handlers
            setupModalEventHandlers();

            loadPdfDocument();

            // Update UI after PDF loads
            setTimeout(() => {
                updateFieldCount();
                updateFieldsTable();
                updateAnchorCount();
                updateAnchorTable();
                renderFieldOverlays();
                renderAnchorOverlays();
            }, 1000);
        } else {
            console.error('PDF canvas not found');
        }
    }, 500);

    // Set default mode
    switchMappingMode('field');
}

// ================================================================
// TABLE-PDF SELECTION SYNC V2 - Fast Animations + Auto-Scroll
// Add to wizard-step3.js
// ================================================================

// ✅ ENHANCED: updateFieldsTable - Add click handlers for sync
function updateFieldsTable() {
    console.log('📋 [UI] Updating fields table with', fieldMappings?.length || 0, 'fields');

    const tbody = $('#mapped-fields-table tbody');
    if (!tbody.length) {
        console.warn('📋 [UI] Table tbody not found');
        return;
    }

    tbody.empty();

    if (!fieldMappings || fieldMappings.length === 0) {
        tbody.append('<tr><td colspan="3" class="text-center text-muted">No fields mapped yet</td></tr>');
        return;
    }

    fieldMappings.forEach(function (field) {
        const fieldDescription = field.description || 'No description';

        const row = $(`
            <tr data-field-id="${field.id}" class="field-table-row" style="cursor: pointer;">
                <td>
                    <div class="fw-bold">${field.fieldName}</div>
                    <small class="text-muted">${fieldDescription}</small>
                </td>
                <td><span class="badge bg-info">Page ${field.pageNumber}</span></td>
                <td class="field-actions" style="white-space: nowrap; width: 80px;">
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-sm btn-outline-primary" 
                                onclick="editField(${field.id})" title="Edit">
                            <i class="fa fa-cog"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger"
                                onclick="deleteFieldWithAjax(${field.id})" title="Delete">
                            <i class="fa fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `);

        // ✅ NEW: Add click handler for table row sync
        row.on('click', function (e) {
            // Don't trigger on button clicks
            if ($(e.target).closest('.btn').length > 0) {
                return;
            }

            const fieldId = parseInt($(this).data('field-id'));
            const field = fieldMappings.find(f => f.id === fieldId);

            if (field) {
                console.log('📋 [TABLE-SYNC] Field row clicked:', fieldId);
                syncTableSelectionToPdf('field', fieldId, field.pageNumber);
            }
        });

        tbody.append(row);
    });

    console.log('📋 [UI] Table updated with', fieldMappings.length, 'rows');
}

// ✅ ENHANCED: updateAnchorTable - Add click handlers for sync
function updateAnchorTable() {
    console.log('📋 [UI] Updating anchor table with', anchorPoints?.length || 0, 'anchors');

    const tbody = $('#anchor-points-table tbody');
    if (!tbody.length) {
        return;
    }

    tbody.empty();

    if (!anchorPoints || anchorPoints.length === 0) {
        tbody.append('<tr><td colspan="3" class="text-center text-muted">No anchor points set</td></tr>');
        return;
    }

    anchorPoints.forEach(function (anchor, index) {
        const anchorName = anchor.name || `Anchor_${index + 1}`;
        const anchorPageNumber = anchor.pageNumber || 1;
        const anchorId = anchor.id || (index + 1);
        const referenceText = anchor.referenceText || 'No reference text';

        const row = $(`
            <tr data-anchor-id="${anchorId}" class="anchor-table-row" style="cursor: pointer;">
                <td>
                    <div class="fw-bold">${anchorName}</div>
                    <small class="text-muted">${referenceText}</small>
                </td>
                <td><span class="badge bg-info">Page ${anchorPageNumber}</span></td>
                <td class="anchor-actions" style="white-space: nowrap; width: 80px;">
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-sm btn-outline-primary" 
                                onclick="editAnchor(${anchorId})" title="Edit">
                            <i class="fa fa-cog"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger"
                                onclick="deleteAnchorWithAjax(${anchorId})" title="Delete">
                            <i class="fa fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `);

        // ✅ NEW: Add click handler for anchor table row sync
        row.on('click', function (e) {
            // Don't trigger on button clicks
            if ($(e.target).closest('.btn').length > 0) {
                return;
            }

            const anchorId = parseInt($(this).data('anchor-id'));
            const anchor = anchorPoints.find(a => a.id === anchorId);

            if (anchor) {
                console.log('📋 [TABLE-SYNC] Anchor row clicked:', anchorId);
                syncTableSelectionToPdf('anchor', anchorId, anchor.pageNumber);
            }
        });

        tbody.append(row);
    });
}

// ✅ NEW: Sync table selection to PDF with auto-scroll
function syncTableSelectionToPdf(type, itemId, pageNumber) {
    console.log(`🔄 [SYNC] Syncing ${type} ${itemId} to PDF (page ${pageNumber})`);

    // Clear existing table selections
    $('.field-table-row, .anchor-table-row').removeClass('table-active bg-primary text-white');

    // Highlight selected table row
    if (type === 'field') {
        $(`.field-table-row[data-field-id="${itemId}"]`).addClass('table-active bg-primary text-white');
    } else {
        $(`.anchor-table-row[data-anchor-id="${itemId}"]`).addClass('table-active bg-primary text-white');
    }

    // Navigate to page if needed
    if (pageNumber !== currentPage) {
        console.log(`🔄 [SYNC] Navigating to page ${pageNumber}`);
        navigateToPage(pageNumber);

        // Wait for page to load then select and scroll
        setTimeout(() => {
            selectAndScrollToItem(type, itemId);
        }, 600); // ✅ FASTER: Reduced from 800ms to 600ms
    } else {
        // Same page - select and scroll immediately
        selectAndScrollToItem(type, itemId);
    }
}

// ✅ NEW: Select item and scroll to it in PDF
function selectAndScrollToItem(type, itemId) {
    console.log(`🎯 [SCROLL] Selecting and scrolling to ${type} ${itemId}`);

    // Clear all selections first
    if (typeof clearMultiSelection === 'function') {
        clearMultiSelection();
    }

    // Select the item
    if (type === 'field') {
        selectField(itemId);

        // Find field coordinates for scrolling
        const field = fieldMappings.find(f => f.id === itemId);
        if (field) {
            scrollToItemInPdf(field.x, field.y, field.width, field.height);
        }
    } else {
        selectAnchor(itemId);

        // Find anchor coordinates for scrolling
        const anchor = anchorPoints.find(a => a.id === anchorId);
        if (anchor) {
            scrollToItemInPdf(anchor.x, anchor.y, anchor.width || 200, anchor.height || 50);
        }
    }
}

// ✅ NEW: Scroll PDF container to show item - FAST VERSION
function scrollToItemInPdf(pdfX, pdfY, pdfWidth, pdfHeight) {
    console.log(`📜 [SCROLL] Scrolling to PDF coordinates (${pdfX}, ${pdfY})`);

    // Convert PDF coordinates to screen coordinates
    pdfToScreenCoordinates(pdfX, pdfY, pdfWidth, pdfHeight)
        .then(function (screenCoords) {
            const container = $('#pdf-viewer-container');
            const containerWidth = container.width();
            const containerHeight = container.height();

            // Calculate center position
            const centerX = screenCoords.x + (screenCoords.width / 2);
            const centerY = screenCoords.y + (screenCoords.height / 2);

            // Calculate scroll position to center the item
            const scrollLeft = Math.max(0, centerX - (containerWidth / 2));
            const scrollTop = Math.max(0, centerY - (containerHeight / 2));

            console.log(`📜 [SCROLL] Scrolling container to (${scrollLeft}, ${scrollTop})`);

            // ✅ FASTER: Reduced animation time from 500ms to 250ms
            container.animate({
                scrollLeft: scrollLeft,
                scrollTop: scrollTop
            }, 250, 'swing', function () {
                console.log('📜 [SCROLL] Scroll animation complete');

                // ✅ FAST HIGHLIGHT: Reduced highlight duration
                highlightSelectedItem(screenCoords);
            });
        })
        .catch(function (error) {
            console.error('📜 [SCROLL] Error converting coordinates:', error);
        });
}

// ✅ NEW: Fast highlight effect for selected item
function highlightSelectedItem(screenCoords) {
    console.log('✨ [HIGHLIGHT] Adding fast highlight effect');

    // Create temporary highlight overlay
    const highlight = $(`
        <div class="temp-highlight" style="
            position: absolute;
            left: ${screenCoords.x - 5}px;
            top: ${screenCoords.y - 5}px;
            width: ${screenCoords.width + 10}px;
            height: ${screenCoords.height + 10}px;
            border: 3px solid #FFE923;
            background: rgba(255, 233, 35, 0.2);
            border-radius: 8px;
            z-index: 1000;
            pointer-events: none;
            animation: fastHighlightPulse 0.8s ease-in-out;
        ">
        </div>
    `);

    // ✅ FAST: Add fast highlight animation CSS
    if ($('#fast-highlight-animation-css').length === 0) {
        $('head').append(`
            <style id="fast-highlight-animation-css">
                @keyframes fastHighlightPulse {
                    0% { 
                        opacity: 0; 
                        transform: scale(1.1); 
                        box-shadow: 0 0 15px rgba(255, 233, 35, 0.8);
                    }
                    50% { 
                        opacity: 1; 
                        transform: scale(1); 
                        box-shadow: 0 0 10px rgba(255, 233, 35, 0.6);
                    }
                    100% { 
                        opacity: 0; 
                        transform: scale(1); 
                        box-shadow: 0 0 5px rgba(255, 233, 35, 0.4);
                    }
                }
            </style>
        `);
    }

    // Add to overlays container
    $('#field-overlays').append(highlight);

    // ✅ FASTER: Remove after shorter duration (800ms instead of 1500ms)
    setTimeout(() => {
        highlight.remove();
        console.log('✨ [HIGHLIGHT] Fast highlight effect removed');
    }, 800);
}

// ✅ ENHANCED: selectField - Update table selection
function selectField(fieldId) {
    // Clear all other single selections first
    selectedAnchorId = null;
    $('.anchor-overlay').removeClass('selected');
    $('.field-overlay').removeClass('selected');

    // Set new selection
    selectedFieldId = fieldId;
    $(`.field-overlay[data-field-id="${fieldId}"]`).addClass('selected');

    // ✅ NEW: Update table selection
    updateTableSelectionFromPdf('field', fieldId);

    console.log('🎯 Selected field:', fieldId);
}

// ✅ ENHANCED: selectAnchor - Update table selection
function selectAnchor(anchorId) {
    // Clear all other single selections first
    selectedFieldId = null;
    $('.field-overlay').removeClass('selected');
    $('.anchor-overlay').removeClass('selected');

    // Set new selection
    selectedAnchorId = anchorId;
    $(`.anchor-overlay[data-anchor-id="${anchorId}"]`).addClass('selected');

    // ✅ NEW: Update table selection
    updateTableSelectionFromPdf('anchor', anchorId);

    console.log('🎯 Selected anchor:', anchorId);
}

// ✅ NEW: Update table selection from PDF selection - FAST VERSION
function updateTableSelectionFromPdf(type, itemId) {
    console.log(`🔄 [SYNC] Updating table selection from PDF: ${type} ${itemId}`);

    // Clear existing table selections
    $('.field-table-row, .anchor-table-row').removeClass('table-active bg-primary text-white');

    // Highlight selected table row
    if (type === 'field') {
        $(`.field-table-row[data-field-id="${itemId}"]`).addClass('table-active bg-primary text-white');

        // Scroll table to show selected row
        scrollTableToRow(`.field-table-row[data-field-id="${itemId}"]`);
    } else {
        $(`.anchor-table-row[data-anchor-id="${itemId}"]`).addClass('table-active bg-primary text-white');

        // Scroll table to show selected row
        scrollTableToRow(`.anchor-table-row[data-anchor-id="${itemId}"]`);
    }
}

// ✅ NEW: Fast scroll table to show selected row
function scrollTableToRow(rowSelector) {
    const row = $(rowSelector);
    if (row.length === 0) return;

    const tableContainer = row.closest('.panel-body');
    if (tableContainer.length === 0) return;

    const rowTop = row.position().top;
    const rowHeight = row.outerHeight();
    const containerHeight = tableContainer.height();
    const currentScroll = tableContainer.scrollTop();

    // Check if row is visible
    if (rowTop < 0 || rowTop + rowHeight > containerHeight) {
        const scrollTo = currentScroll + rowTop - (containerHeight / 2) + (rowHeight / 2);

        console.log('📜 [TABLE-SCROLL] Fast scrolling table to show selected row');

        // ✅ FASTER: Reduced animation time from 300ms to 150ms
        tableContainer.animate({
            scrollTop: Math.max(0, scrollTo)
        }, 150);
    }
}

// ✅ Export new functions
window.syncTableSelectionToPdf = syncTableSelectionToPdf;
window.selectAndScrollToItem = selectAndScrollToItem;
window.scrollToItemInPdf = scrollToItemInPdf;
window.highlightSelectedItem = highlightSelectedItem;
window.updateTableSelectionFromPdf = updateTableSelectionFromPdf;
window.scrollTableToRow = scrollTableToRow;


window.syncTableSelectionToPdf = syncTableSelectionToPdf;
window.selectAndScrollToItem = selectAndScrollToItem;
window.scrollToItemInPdf = scrollToItemInPdf;
window.highlightSelectedItem = highlightSelectedItem;
window.updateTableSelectionFromPdf = updateTableSelectionFromPdf;
window.scrollTableToRow = scrollTableToRow;

// ✅ Export the new functions
window.resetDrawingState = resetDrawingState;
window.setupModalEventHandlers = setupModalEventHandlers;

window.restoreAllSelectionStyling = restoreAllSelectionStyling;
window.restoreFieldSelectionStyling = restoreFieldSelectionStyling;
window.restoreAnchorSelectionStyling = restoreAnchorSelectionStyling;
window.createBulkFields = createBulkFields;

console.log('✅ [CENTRALIZED] Centralized selection system with zoom-resistant styling loaded');

// Export additional functions for wizard integration
window.getStep3FormData = getStep3FormData;
window.validateStep3Custom = validateStep3Custom;
window.saveStep3Data = saveStep3Data;
window.deleteFieldWithAjax = deleteFieldWithAjax;
window.deleteAnchorWithAjax = deleteAnchorWithAjax;
window.editField = editField;
window.editAnchor = editAnchor;
window.openFieldModal = openFieldModal;
window.openAnchorModal = openAnchorModal;
window.saveFieldFromModal = saveFieldFromModal;
window.saveAnchorFromModal = saveAnchorFromModal;
 

// Export functions for global access
window.initializeStep3 = initializeStep3;
window.switchMappingMode = switchMappingMode;
window.fieldMappings = fieldMappings;
window.anchorPoints = anchorPoints;

// Export functions that were in the original files
window.updateFieldMappingAjax = updateFieldMappingAjax;
window.loadFieldMappingsAjax = loadFieldMappingsAjax;
window.updateAnchorPointAjax = updateAnchorPointAjax;
window.loadAnchorPointsAjax = loadAnchorPointsAjax;
// ... (Include other necessary AJAX functions from the original code)
 