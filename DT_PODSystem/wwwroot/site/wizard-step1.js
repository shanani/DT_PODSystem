// wizard-step1.js - Template Details Step (Updated with File Name Prefix)
// ========================================================================

// Initialize Step 1
function initializeStep1() {
    console.log('🟢 Initializing Step 1: Template Details');

    // Load existing data from server
    loadServerData();

    // Setup event handlers (NO auto-saving)
    setupStep1EventHandlers();

    // Update preview displays
    updateAllPreviews();
}

// Save Step 1 data to database (ONLY called on Next button)
async function saveStep1Data() {
    try {
        const step1Data = getStep1FormData();

        console.log('💾 [STEP1] Saving step 1 data:', step1Data);

        const response = await fetch('/Template/SaveStep1', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                TemplateId: wizardData.templateId,
                Data: step1Data
            })
        });

        const result = await response.json();

        if (result.success) {
            console.log('💾 [STEP1] ✅ Step 1 saved successfully');
            return true;
        } else {
            console.error('💾 [STEP1] ❌ Save failed:', result.message);
            alert.error(result.message || 'Failed to save step 1 data');
            return false;
        }
    } catch (error) {
        console.error('💾 [STEP1] ❌ Save error:', error);
        alert.error('Error saving step 1 data');
        return false;
    }
}

 

// Setup event handlers for Step 1 (ONLY for UI updates, NO saving)
function setupStep1EventHandlers() {
    // Template name input with preview update ONLY
    $('#template-name').on('input', function () {
        const name = $(this).val();
        const preview = name;
        $('#preview-name').text(preview || 'TEMPLATE_NAME');

        // Auto-update file prefix based on template name
        updateFilePrefixFromName(name);
        updateNamingConvention();
    });

    // File name prefix input
    $('#file-name-prefix').on('input', function () {
        updateNamingConvention();
    });

    // Description input (UI only)
    $('#template-description').on('input', function () {
        // NO triggerAutoSave() - removed
    });

    // Category selection (UI only)
    $('#category-id').on('change', function () {
        updatePreviewCategory();
    });

    // Department selection (UI only)
    $('#department-id').on('change', function () {
        updatePreviewDepartment();
    });

    // Vendor selection (UI only)
    $('#vendor-id').on('change', function () {
        updatePreviewVendor();
    });

    // Priority range (UI only)
    $('#processing-priority').on('input', function () {
        const priority = parseInt($(this).val());
        const priorityText = getPriorityText(priority);
        $('#preview-priority').text(priorityText);
        $('#priority-label').text(priority);
    });

    // Checkboxes (UI only)
    $('#requires-approval, #is-financial').on('change', function () {  // ✅ FIX: Changed to is-financial
        updatePreviewFlags();
    });
}

// Auto-update file prefix based on template name
function updateFilePrefixFromName(templateName) {
    if (templateName && templateName.trim()) {
        const prefix = templateName.trim();
             
        $('#file-name-prefix').val(prefix);
    }
}

// Update naming convention based on file prefix
function updateNamingConvention() {
    const prefix = $('#file-name-prefix').val().trim();
    if (prefix) {
        const convention = `${prefix}_yyyyMM`;
        $('#preview-convention').text(convention);
    } else {
        $('#preview-convention').text('DOC_POD_yyyyMM');
    }
}

// Update all preview displays
function updateAllPreviews() {
    updatePreviewCategory();
    updatePreviewDepartment();
    updatePreviewVendor();
    updatePreviewPriority();
    updatePreviewFlags();
    updateNamingConvention();

    // Update template name preview
    const name = $('#template-name').val();
    if (name) {
        const preview = name;
        $('#preview-name').text(preview);
    }

    // Initialize priority display
    initializePriorityDisplay();
}

// Initialize priority display on page load
function initializePriorityDisplay() {
    const priority = parseInt($('#processing-priority').val()) || 5;
    const priorityText = getPriorityText(priority);

    $('#priority-label').text(priority);
    $('#priority-text').text(priorityText);
    $('#preview-priority').removeClass('bg-secondary bg-primary bg-danger bg-warning')
        .addClass(getPriorityBadgeClass(priority))
        .text(priorityText);
}

// Helper functions for preview updates
function updatePreviewCategory() {
    const categoryText = $('#category-id option:selected').text();
    $('#preview-category').text(categoryText !== 'Select Domain (NOC, Finance, etc.)' ? categoryText : 'Not selected');
}

function updatePreviewDepartment() {
    const deptText = $('#department-id option:selected').text();
    $('#preview-department').text(deptText !== 'Select Department' ? deptText : 'Not selected');
}

function updatePreviewVendor() {
    const vendorText = $('#vendor-id option:selected').text();
    $('#preview-vendor').text(vendorText !== 'Select Template Owner' ? vendorText : 'Not selected');
}

function updatePreviewPriority() {
    const priority = parseInt($('#processing-priority').val()) || 5;
    const priorityText = getPriorityText(priority);

    $('#priority-label').text(priority);
    $('#priority-text').text(priorityText);
    $('#preview-priority').removeClass('bg-secondary bg-primary bg-danger bg-warning')
        .addClass(getPriorityBadgeClass(priority))
        .text(priorityText);
}

// Get priority badge class
function getPriorityBadgeClass(priority) {
    if (priority <= 2) return 'bg-danger';   // Highest - Red
    if (priority <= 4) return 'bg-warning';  // High - Orange  
    if (priority <= 6) return 'bg-primary';  // Normal - Blue
    if (priority <= 8) return 'bg-secondary'; // Low - Gray
    return 'bg-secondary';                    // Lowest - Gray
}

function updatePreviewFlags() {
    const requiresApproval = $('#requires-approval').is(':checked');
    const isFinancialData = $('#is-financial').is(':checked');  // ✅ FIX: Changed to is-financial

    let flags = [];
    if (requiresApproval) flags.push('Requires Approval');
    if (isFinancialData) flags.push('Financial Data');

    $('#preview-flags').text(flags.length > 0 ? flags.join(', ') : 'None');
}

// Get priority text from number
function getPriorityText(priority) {
    if (priority <= 2) return 'Highest';
    if (priority <= 4) return 'High';
    if (priority <= 6) return 'Normal';
    if (priority <= 8) return 'Low';
    return 'Lowest';
}



// Load existing data from server (FIXED - No need to strip suffix)
function loadServerData() {
    const serverData = window.serverWizardData?.Step1;

    if (serverData) {
        // Populate form fields with server data
        if (serverData.Name) $('#template-name').val(serverData.Name);
        if (serverData.Description) $('#template-description').val(serverData.Description);
        if (serverData.NamingConvention) {
            // ✅ FIXED: NamingConvention now contains only prefix, use as-is
            $('#file-name-prefix').val(serverData.NamingConvention);
        }
        if (serverData.CategoryId) $('#category-id').val(serverData.CategoryId);
        if (serverData.DepartmentId) $('#department-id').val(serverData.DepartmentId);
        if (serverData.VendorId) $('#vendor-id').val(serverData.VendorId);
        if (serverData.ProcessingPriority) {
            $('#processing-priority').val(serverData.ProcessingPriority);
        }

        // Handle checkboxes
        if (serverData.RequiresApproval !== undefined) {
            $('#requires-approval').prop('checked', serverData.RequiresApproval);
        }
        if (serverData.IsFinancialData !== undefined) {
            $('#is-financial').prop('checked', serverData.IsFinancialData);
        }

        console.log('📥 Loaded Step 1 data from server (fixed)');
    }
}

// Get Step 1 form data for saving (FIXED - Save only prefix)
function getStep1FormData() {
    // Helper function to safely get form field values
    const safeGetValue = (selector) => {
        const element = $(selector);
        return element.length > 0 ? (element.val() || '') : '';
    };

    const safeGetChecked = (selector) => {
        const element = $(selector);
        return element.length > 0 ? element.is(':checked') : false;
    };

    const safeGetInt = (selector, defaultValue = null) => {
        const value = safeGetValue(selector);
        const parsed = parseInt(value);
        return !isNaN(parsed) ? parsed : defaultValue;
    };

    console.log('🔍 [DEBUG] Collecting Step 1 form data...');

    const filePrefix = safeGetValue('#file-name-prefix').trim() || 'DOC_POD';

    const formData = {
        name: safeGetValue('#template-name').trim(),
        description: safeGetValue('#template-description').trim(),
        namingConvention: filePrefix,  // ✅ FIXED: Save only prefix, not full pattern
        categoryId: safeGetInt('#category-id'),
        departmentId: safeGetInt('#department-id'),
        vendorId: safeGetInt('#vendor-id'),
        requiresApproval: safeGetChecked('#requires-approval'),
        isFinancialData: safeGetChecked('#is-financial'),
        processingPriority: safeGetInt('#processing-priority', 5)
    };

    console.log('🔍 [DEBUG] Collected form data (FIXED):', formData);
    return formData;
}

// Custom validation for Step 1 (called on Next button)
function validateStep1Custom() {
    const templateName = $('#template-name').val().trim();
    const filePrefix = $('#file-name-prefix').val().trim();
    const categoryId = $('#category-id').val();
    const departmentId = $('#department-id').val();

    // Clear previous validation styles
    $('#template-name, #file-name-prefix, #category-id, #department-id').removeClass('is-invalid');

    let isValid = true;
    const errors = [];

    if (!templateName || templateName.length < 3) {
        errors.push('Template name must be at least 3 characters');
        $('#template-name').addClass('is-invalid');
        isValid = false;
    }

    if (!filePrefix || filePrefix.length < 3) {
        errors.push('File name prefix must be at least 3 characters');
        $('#file-name-prefix').addClass('is-invalid');
        isValid = false;
    } else if (!/^[^<>:"/\\|?*\x00-\x1f]+$/.test(filePrefix)) {
        errors.push('File name prefix contains invalid characters. Cannot contain: < > : " / \\ | ? *');
        $('#file-name-prefix').addClass('is-invalid');
        isValid = false;
    }

    if (!categoryId) {
        errors.push('Please select a category');
        $('#category-id').addClass('is-invalid');
        isValid = false;
    }

    if (!departmentId) {
        errors.push('Please select a department');
        $('#department-id').addClass('is-invalid');
        isValid = false;
    }

    if (!isValid) {
        const errorMessage = errors.length === 1 ? errors[0] :
            `Please fix the following issues:\n• ${errors.join('\n• ')}`;
        alert.warning(errorMessage);

        // Focus on first invalid field
        if (!templateName || templateName.length < 3) {
            $('#template-name').focus();
        } else if (!filePrefix || filePrefix.length < 3 || !/^[A-Z][A-Z0-9_]*$/.test(filePrefix)) {
            $('#file-name-prefix').focus();
        } else if (!categoryId) {
            $('#category-id').focus();
        } else if (!departmentId) {
            $('#department-id').focus();
        }
    }

    return isValid;
}

// Export functions for global access
window.getStep1FormData = getStep1FormData;
window.validateStep1Custom = validateStep1Custom;
window.saveStep1Data = saveStep1Data;
window.initializeStep1 = initializeStep1;

// Debug function to check current form values
window.debugStep1Values = function () {
    console.log('🔍 [DEBUG] Current form values:', {
        templateName: $('#template-name').val(),
        description: $('#template-description').val(),
        filePrefix: $('#file-name-prefix').val(),
        processingPriority: $('#processing-priority').val(),
        priorityType: typeof $('#processing-priority').val(),
        priorityExists: $('#processing-priority').length > 0,
        priorityParsed: parseInt($('#processing-priority').val()),
        categoryId: $('#category-id').val(),
        departmentId: $('#department-id').val(),
        vendorId: $('#vendor-id').val(),
        requiresApproval: $('#requires-approval').is(':checked'),
        isFinancialData: $('#is-financial').is(':checked')  // ✅ FIX: Changed to is-financial
    });
};