// wizard-step1.js - Updated for POD-Template Architecture
// =========================================================

// Initialize Step 1
function initializeStep1() {
    console.log('🟢 Initializing Step 1: Template Details');

    // Load existing data from server (if in edit mode)
    loadServerData();

    // Setup event handlers
    setupStep1EventHandlers();

    // Update preview displays
    updateAllPreviews();

    // Initialize Select2 for POD selection
    initializePODSelection();
}

// *** NEW: Initialize POD selection with enhanced features ***
function initializePODSelection() {
    $('#pod-id').select2({
        placeholder: 'Search and select a POD',
        allowClear: true,
        width: '100%'
    });

    // Auto-select POD if PODId is provided in URL or model
    const urlParams = new URLSearchParams(window.location.search);
    const urlPodId = urlParams.get('podId');
    const modelPodId = wizardData?.step1?.podId;

    const podIdToSelect = urlPodId || modelPodId;

    if (podIdToSelect && podIdToSelect !== '0') {
        $('#pod-id').val(podIdToSelect).trigger('change');
        console.log('🎯 Auto-selected POD:', podIdToSelect);
    }

    // Update preview when POD changes
    $('#pod-id').on('change', function () {
        const selectedText = $(this).find('option:selected').text();
        $('#preview-pod').text(selectedText || 'Not Selected');
        updateValidationStatus('pod', $(this).val() !== '');

        // Update technical notes when POD is selected
        updateTechnicalNotesFromPOD();
    });

    // Initialize validation status
    updateValidationStatus('pod', $('#pod-id').val() !== '');
}

// *** NEW: Get Step 1 form data for template creation ***
function getStep1FormData() {
    // Safe value extraction with null checks
    const safeGetValue = (selector) => {
        const element = $(selector);
        return element.length > 0 ? (element.val() || '') : '';
    };

    const safeGetInt = (selector, defaultValue = null) => {
        const value = safeGetValue(selector);
        const parsed = parseInt(value);
        return !isNaN(parsed) ? parsed : defaultValue;
    };

    console.log('🔍 [DEBUG] Collecting Step 1 form data...');

    const formData = {
        // *** CRITICAL: POD ID is required for template creation ***
        podId: safeGetInt('#pod-id'),

        // Template technical configuration
        name: safeGetValue('#template-name').trim(),
        description: safeGetValue('#template-description').trim(),
        namingConvention: safeGetValue('#naming-convention').trim() || 'DOC_POD',
        technicalNotes: safeGetValue('#technical-notes').trim(),
        processingPriority: safeGetInt('#processing-priority', 5),

        // Template processing settings
        hasFormFields: $('#has-form-fields').is(':checked'),
        version: safeGetValue('#version').trim() || '1.0'
    };

    console.log('🔍 [DEBUG] Collected form data:', formData);
    return formData;
}

// *** UPDATED: Enhanced validation for POD-Template relationship ***
function validateStep1Custom() {
    const podId = $('#pod-id').val();
    const templateName = $('#template-name').val().trim();
    const namingConvention = $('#naming-convention').val().trim();

    // Clear previous validation styles
    $('#pod-id, #template-name, #naming-convention').removeClass('is-invalid');

    let isValid = true;
    const errors = [];

    // *** CRITICAL: POD selection is required ***
    if (!podId || podId <= 0) {
        errors.push('Please select a POD. Templates must belong to a POD.');
        $('#pod-id').addClass('is-invalid').focus();
        isValid = false;
    }

    // Template name validation
    if (!templateName || templateName.length < 3) {
        errors.push('Template name must be at least 3 characters');
        $('#template-name').addClass('is-invalid');
        if (isValid) $('#template-name').focus(); // Focus first invalid field
        isValid = false;
    } else if (templateName.length > 200) {
        errors.push('Template name cannot exceed 200 characters');
        $('#template-name').addClass('is-invalid');
        if (isValid) $('#template-name').focus();
        isValid = false;
    }

    // Naming convention validation
    if (!namingConvention || namingConvention.length < 3) {
        errors.push('Naming convention must be at least 3 characters');
        $('#naming-convention').addClass('is-invalid');
        if (isValid) $('#naming-convention').focus();
        isValid = false;
    } else if (!/^[A-Z][A-Z0-9_]*$/.test(namingConvention)) {
        errors.push('Naming convention must start with uppercase letter and contain only A-Z, 0-9, and underscore');
        $('#naming-convention').addClass('is-invalid');
        if (isValid) $('#naming-convention').focus();
        isValid = false;
    }

    if (!isValid) {
        const errorMessage = errors.length === 1 ? errors[0] :
            `Please fix the following issues:\n• ${errors.join('\n• ')}`;
        alert.warning(errorMessage);
    }

    return isValid;
}

// *** NO SAVE FUNCTION FOR STEP 1 - Template creation happens in wizard-shared.js ***
// This step only validates, the actual template creation happens on Next button

// Setup event handlers for Step 1
function setupStep1EventHandlers() {
    // Template name updates
    $('#template-name').on('input', function () {
        const value = $(this).val();
        $('#preview-name').text(value || 'Template Name');
        updateValidationStatus('name', value.length >= 3);
        updatePreviewConvention();
    });

    // Naming convention updates
    $('#naming-convention').on('input', function () {
        const value = $(this).val();
        updateValidationStatus('convention', value.length >= 3 && /^[A-Z][A-Z0-9_]*$/.test(value));
        updatePreviewConvention();
    });

    // Description updates
    $('#template-description').on('input', function () {
        const value = $(this).val();
        $('#preview-description').text(value || 'No description provided');
    });

    // Processing priority updates
    $('#processing-priority').on('change', function () {
        const value = parseInt($(this).val());
        const priorityText = getPriorityText(value);
        $('#preview-priority').text(priorityText).attr('class', `badge ${getPriorityClass(value)}`);
    });

    // Form field detection checkbox
    $('#has-form-fields').on('change', function () {
        const hasFields = $(this).is(':checked');
        $('#preview-form-fields').text(hasFields ? 'Yes' : 'No');
    });

    // Version input
    $('#version').on('input', function () {
        const value = $(this).val();
        $('#preview-version').text(value || '1.0');
    });

    console.log('✅ Step 1 event handlers setup complete');
}

// Update all preview displays
function updateAllPreviews() {
    // Update name preview
    const templateName = $('#template-name').val();
    $('#preview-name').text(templateName || 'Template Name');

    // Update description preview
    const description = $('#template-description').val();
    $('#preview-description').text(description || 'No description provided');

    // Update priority preview
    const priority = parseInt($('#processing-priority').val());
    if (!isNaN(priority)) {
        $('#preview-priority').text(getPriorityText(priority)).attr('class', `badge ${getPriorityClass(priority)}`);
    }

    // Update convention preview
    updatePreviewConvention();

    // Update form fields preview
    const hasFormFields = $('#has-form-fields').is(':checked');
    $('#preview-form-fields').text(hasFormFields ? 'Yes' : 'No');

    // Update version preview
    const version = $('#version').val();
    $('#preview-version').text(version || '1.0');

    console.log('🖼️ All previews updated');
}

// Update naming convention preview with dynamic pattern
function updatePreviewConvention() {
    const convention = $('#naming-convention').val() || 'DOC_POD';
    const today = new Date();
    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, '0');

    // Generate example with current date
    const example = `${convention}_${year}${month}_001.pdf`;
    $('#preview-convention').text(example);
}

// Load existing data from server (for edit mode)
function loadServerData() {
    if (wizardData && wizardData.step1) {
        const step1Data = wizardData.step1;

        // Load form values
        if (step1Data.podId) $('#pod-id').val(step1Data.podId);
        if (step1Data.name) $('#template-name').val(step1Data.name);
        if (step1Data.description) $('#template-description').val(step1Data.description);
        if (step1Data.namingConvention) $('#naming-convention').val(step1Data.namingConvention);
        if (step1Data.technicalNotes) $('#technical-notes').val(step1Data.technicalNotes);
        if (step1Data.processingPriority) $('#processing-priority').val(step1Data.processingPriority);
        if (step1Data.hasFormFields) $('#has-form-fields').prop('checked', step1Data.hasFormFields);
        if (step1Data.version) $('#version').val(step1Data.version);

        console.log('📥 Loaded existing Step 1 data:', step1Data);
    }
}

// Update technical notes when POD is selected
function updateTechnicalNotesFromPOD() {
    const podId = $('#pod-id').val();
    const podText = $('#pod-id').find('option:selected').text();

    if (podId && podText !== 'Search and select a POD') {
        const currentNotes = $('#technical-notes').val();
        if (!currentNotes) {
            $('#technical-notes').val(`Template created for ${podText} processing`);
        }
    }
}

// Helper functions for priority display
function getPriorityText(priority) {
    const priorityMap = {
        1: 'Critical', 2: 'High', 3: 'High',
        4: 'Medium-High', 5: 'Medium', 6: 'Medium',
        7: 'Medium-Low', 8: 'Low', 9: 'Low', 10: 'Very Low'
    };
    return priorityMap[priority] || 'Medium';
}

function getPriorityClass(priority) {
    if (priority <= 2) return 'bg-danger';
    if (priority <= 4) return 'bg-warning';
    if (priority <= 6) return 'bg-info';
    return 'bg-secondary';
}

// Update validation status indicators
function updateValidationStatus(field, isValid) {
    const item = $(`.validation-item[data-field="${field}"]`);
    const icon = item.find('i');

    if (isValid) {
        icon.removeClass('text-muted text-danger fa-circle')
            .addClass('text-success fa-check-circle');
    } else {
        icon.removeClass('text-success text-danger fa-check-circle')
            .addClass('text-muted fa-circle');
    }
}

// Export functions for global access
window.getStep1FormData = getStep1FormData;
window.validateStep1Custom = validateStep1Custom;
window.initializeStep1 = initializeStep1;

// Debug function for development
window.debugStep1Values = function () {
    console.log('🔍 [DEBUG] Current Step 1 values:', {
        podId: $('#pod-id').val(),
        templateName: $('#template-name').val(),
        description: $('#template-description').val(),
        namingConvention: $('#naming-convention').val(),
        technicalNotes: $('#technical-notes').val(),
        processingPriority: $('#processing-priority').val(),
        hasFormFields: $('#has-form-fields').is(':checked'),
        version: $('#version').val()
    });
};