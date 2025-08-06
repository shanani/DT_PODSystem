// wizard-changes-detection.js - Change Detection System
// ====================================================

let originalData = {};
let hasUnsavedChanges = false;
let changeDetectionInitialized = false;

// Initialize change detection when DOM ready
$(document).ready(function () {
    if (!changeDetectionInitialized) {
        initializeChangeDetection();
        changeDetectionInitialized = true;
    }
});

// Initialize change detection system
function initializeChangeDetection() {
    console.log('🎯 Initializing change detection...');

    // Capture initial state
    captureInitialState();

    // Setup event listeners for all form elements
    setupChangeListeners();

    // Setup save progress button handler
    setupSaveProgressButton();

    // Initial UI update
    updateSaveProgressUI();

    console.log('✅ Change detection initialized');
}

// Capture initial state of all form data
function captureInitialState() {
    originalData = {
        step1: captureStep1Data(),
        step2: captureStep2Data(),
        step3: captureStep3Data(),
        step4: captureStep4Data()
    };

    console.log('📸 Initial state captured:', originalData);
}

// Step-specific data capture functions
function captureStep1Data() {
    return {
        uploadedFiles: $('#uploaded-files-list .file-item').length,
        fileNames: $('#uploaded-files-list .file-item').map(function () {
            return $(this).find('.file-name').text();
        }).get()
    };
}

function captureStep2Data() {
    return {
        name: $('#Name').val() || '',
        description: $('#Description').val() || '',
        categoryId: $('#CategoryId').val() || '',
        departmentId: $('#DepartmentId').val() || '',
        vendorId: $('#VendorId').val() || '',
        requiresApproval: $('#RequiresApproval').is(':checked'),
        isFinancialData: $('#IsFinancialData').is(':checked'),
        processingPriority: $('#ProcessingPriority').val() || ''
    };
}

function captureStep3Data() {
    return {
        fieldMappings: window.fieldMappings ? JSON.stringify(window.fieldMappings) : '[]',
        currentPage: window.currentPage || 1
    };
}

function captureStep4Data() {
    return {
        variables: window.wizardData?.Step4?.Variables ? JSON.stringify(window.wizardData.Step4.Variables) : '[]',
        calculatedFields: window.wizardData?.Step4?.CalculatedFields ? JSON.stringify(window.wizardData.Step4.CalculatedFields) : '[]',
        canvasElements: window.canvasElements ? JSON.stringify(window.canvasElements) : '[]'
    };
}

// Setup change listeners for all form elements
function setupChangeListeners() {
    // Generic form inputs (input, select, textarea)
    $(document).on('input change', 'input, select, textarea', function () {
        setTimeout(checkForChanges, 100); // Slight delay to ensure value is updated
    });

    // Checkbox and radio buttons
    $(document).on('change', 'input[type="checkbox"], input[type="radio"]', function () {
        setTimeout(checkForChanges, 100);
    });

    // File uploads (for step 1)
    $(document).on('addedfile removedfile', '#dropzone', function () {
        setTimeout(checkForChanges, 100);
    });

    // PDF field mappings (for step 3)
    $(document).on('fieldMappingAdded fieldMappingRemoved fieldMappingUpdated', function () {
        setTimeout(checkForChanges, 100);
    });

    // Canvas changes (for step 4)
    $(document).on('canvasElementAdded canvasElementRemoved canvasConnectionAdded canvasConnectionRemoved', function () {
        setTimeout(checkForChanges, 100);
    });

    console.log('👂 Change listeners setup complete');
}

// Check if current data differs from original
function checkForChanges() {
    const currentData = {
        step1: captureStep1Data(),
        step2: captureStep2Data(),
        step3: captureStep3Data(),
        step4: captureStep4Data()
    };

    // Compare current data with original
    const hasChanges = !deepEqual(originalData, currentData);

    if (hasChanges !== hasUnsavedChanges) {
        hasUnsavedChanges = hasChanges;
        updateSaveProgressUI();

        if (hasChanges) {
            console.log('📝 Changes detected');
        } else {
            console.log('✅ No changes detected');
        }
    }
}

// Deep comparison function
function deepEqual(obj1, obj2) {
    if (obj1 === obj2) return true;

    if (obj1 == null || obj2 == null) return obj1 === obj2;

    if (typeof obj1 !== typeof obj2) return false;

    if (typeof obj1 !== 'object') return obj1 === obj2;

    const keys1 = Object.keys(obj1);
    const keys2 = Object.keys(obj2);

    if (keys1.length !== keys2.length) return false;

    for (let key of keys1) {
        if (!keys2.includes(key)) return false;
        if (!deepEqual(obj1[key], obj2[key])) return false;
    }

    return true;
}

// Update Save Progress button visibility and state
function updateSaveProgressUI() {
    const $saveBtn = $('#save-progress-btn');
    const $saveStatus = $('#save-status');

    if (hasUnsavedChanges) {
        $saveBtn.show().prop('disabled', false);
        $saveStatus.hide();
    } else {
        $saveBtn.hide();
        $saveStatus.hide();
    }
}

// Setup Save Progress button handler
function setupSaveProgressButton() {
    $(document).off('click', '#save-progress-btn').on('click', '#save-progress-btn', async function () {
        await saveProgressData();
    });
}

// Save progress function
async function saveProgressData() {
    const $saveBtn = $('#save-progress-btn');
    const $saveStatus = $('#save-status');

    try {
        // Update button state
        $saveBtn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-2"></i>Saving...');

        // Call the existing save function for current step
        const saveSuccess = await saveCurrentStepToDatabase();

        if (saveSuccess) {
            // Update original data to current state (reset change detection)
            captureInitialState();
            hasUnsavedChanges = false;

            // Show success feedback
            $saveStatus.text('✓ Progress saved').removeClass('text-danger').addClass('text-success').show();

            // Hide save button and status after delay
            setTimeout(() => {
                updateSaveProgressUI();
            }, 2000);

            console.log('✅ Progress saved successfully');
        } else {
            // Show error feedback
            $saveStatus.text('✗ Save failed').removeClass('text-success').addClass('text-danger').show();

            setTimeout(() => {
                $saveStatus.hide();
            }, 3000);

            console.log('❌ Progress save failed');
        }

    } catch (error) {
        console.error('❌ Error saving progress:', error);
        $saveStatus.text('✗ Save error').removeClass('text-success').addClass('text-danger').show();

        setTimeout(() => {
            $saveStatus.hide();
        }, 3000);
    } finally {
        // Reset button state
        $saveBtn.prop('disabled', false).html('<i class="fa fa-save me-2"></i>Save Progress');
    }
}

// Manual trigger for change detection (for custom events)
function triggerChangeDetection() {
    setTimeout(checkForChanges, 100);
}

// Reset change detection (call after successful navigation)
function resetChangeDetection() {
    captureInitialState();
    hasUnsavedChanges = false;
    updateSaveProgressUI();
    console.log('🔄 Change detection reset');
}

// Export functions for global use
window.triggerChangeDetection = triggerChangeDetection;
window.resetChangeDetection = resetChangeDetection;
window.hasUnsavedChanges = () => hasUnsavedChanges;