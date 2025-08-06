// wizard-shared.js - Fixed: No Save on Step 1 Navigation
// ========================================================

let wizardData = {};
let isSaving = false;

// Updated initialization
$(document).ready(function () {
    wizardData = window.serverWizardData;
    initializeCurrentStep();
    setupEventHandlers();
    updateUI();

    // Initialize change detection after a small delay
    setTimeout(() => {
        if (typeof initializeChangeDetection === 'function') {
            initializeChangeDetection();
        }
    }, 500);
});

// *** UPDATED: Skip saving for Step 1 ***
async function nextStep() {
    console.log('🔄 Next clicked - validating step', wizardData.currentStep);

    if (isSaving) {
        console.log('⏳ Already saving, ignoring click');
        return;
    }

    try {
        // 1. Validate current step
        const isValid = validateCurrentStep();
        if (!isValid) {
            console.log('❌ Validation failed - canceling navigation');
            return;
        }

        // 2. *** SKIP SAVING FOR STEP 1 *** - Files already saved via AJAX
        if (wizardData.currentStep === 1) {
            console.log('📁 [STEP1] Skipping save on Next - files already saved via AJAX');
        } else {
            // Save for other steps (Step 2, Step 3, etc.)
            console.log('💾 Saving step', wizardData.currentStep, 'before navigation');
            const saveSuccess = await saveCurrentStepToDatabase();
            if (!saveSuccess) {
                console.log('❌ Save failed - canceling navigation');
                return;
            }
        }

        // 3. Reset change detection before navigation
        if (typeof resetChangeDetection === 'function') {
            resetChangeDetection();
        }

        // 4. Navigate to next step
        const nextStepNumber = wizardData.currentStep + 1;
        if (nextStepNumber <= wizardData.totalSteps) {
            console.log('✅ Validation succeeded - navigating to step', nextStepNumber);
            window.location.href = `/Template/Wizard?step=${nextStepNumber}&id=${wizardData.templateId}`;
        }

    } catch (error) {
        console.error('❌ Error in nextStep:', error);
        alert.error('An error occurred while proceeding to the next step');
    }
}

// Updated previousStep function
function previousStep() {
    const prevStepNumber = wizardData.currentStep - 1;
    if (prevStepNumber >= 1) {
        // Reset change detection before navigation
        if (typeof resetChangeDetection === 'function') {
            resetChangeDetection();
        }
        window.location.href = `/Template/Wizard?step=${prevStepNumber}&id=${wizardData.templateId}`;
    }
}

// Updated setupEventHandlers function
function setupEventHandlers() {
    // Use the updated button IDs
    $('#next-step-btn').off('click').on('click', nextStep);
    $('#prev-step-btn').off('click').on('click', previousStep);
    $('#finish-btn').off('click').on('click', finishWizard);
    $('#save-exit-btn').off('click').on('click', saveAndExit);

    // Breadcrumb navigation
    $('.nav-wizards-1 .nav-link').off('click').on('click', function (e) {
        e.preventDefault();
        const step = parseInt($(this).data('step'));
        const isAccessible = $(this).data('accessible') === true;

        if (isAccessible && step && step !== wizardData.currentStep) {
            // Reset change detection before navigation
            if (typeof resetChangeDetection === 'function') {
                resetChangeDetection();
            }
            navigateToStep(step);
        }
    });
}

// *** UPDATED: Skip saving for Step 1 ***
async function saveAndExit() {
    if (isSaving) return;

    try {
        $('#save-exit-btn').prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-2"></i>Saving...');

        // Skip saving for Step 1 - files already saved via AJAX
        if (wizardData.currentStep === 1) {
            console.log('📁 [STEP1] Skipping save on Exit - files already saved via AJAX');
        } else {
            const saveSuccess = await saveCurrentStepToDatabase();
            if (!saveSuccess) {
                alert.error('Failed to save progress.');
                $('#save-exit-btn').prop('disabled', false).html('<i class="fa fa-save me-2"></i>Save & Exit');
                return;
            }
        }

        // Reset change detection
        if (typeof resetChangeDetection === 'function') {
            resetChangeDetection();
        }

        alert.success('Progress saved successfully!');
        setTimeout(() => {
            window.location.href = '/Template';
        }, 1000);

    } catch (error) {
        console.error('Error saving and exiting:', error);
        alert.error('An error occurred while saving.');
    } finally {
        $('#save-exit-btn').prop('disabled', false).html('<i class="fa fa-save me-2"></i>Save & Exit');
    }
}

// Updated finishWizard function - now handles Step 3 finalization
async function finishWizard() {
    if (isSaving) return;

    try {
        $('#finish-btn').prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-2"></i>Creating...');

        // ✅ Step 3 validation
        if (wizardData.currentStep === 3 && typeof validateStep3Final === 'function') {
            console.log('🔍 Validating Step 3...');
            const validationResult = validateStep3Final();

            if (!validationResult.isValid) {
                console.log('❌ Step 3 validation failed:', validationResult.errors);

                // Show validation errors
                let errorMessage = 'Template Validation Failed:\n\n';
                validationResult.errors.forEach((error) => {
                    errorMessage += `• ${error}\n`;
                });

                alert.error(errorMessage);
                $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
                return; // ✅ Exit early - don't continue
            }
        } else {
            // Regular validation for other steps
            const isValid = validateCurrentStep();
            if (!isValid) {
                console.log('❌ Regular validation failed');
                $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
                return; // ✅ Exit early - don't continue
            }
        }

        console.log('✅ Validation passed, saving step data...');

        // ✅ Save current step data
        const saveSuccess = await saveCurrentStepToDatabase();
        if (!saveSuccess) {
            console.log('❌ Failed to save step data');
            alert.error('Failed to save template data');
            $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
            return; // ✅ Exit early - don't continue
        }

        console.log('✅ Step data saved, finalizing template...');

        // ✅ Finalize template
        const response = await fetch('/Template/FinalizeTemplate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                TemplateId: wizardData.templateId
            })
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.log('❌ HTTP error during finalization:', response.status, errorText);
            throw new Error(`Server error: ${response.status} - ${errorText.substring(0, 100)}`);
        }

        const result = await response.json();
        console.log('🔍 Finalization response:', result);

        if (result.success) {
            isSaving = true; // ✅ Block any further processing
            alert.success('Template created successfully and is now active!');
            setTimeout(() => {
                window.location.href = '/Template';
            }, 1000); // Shorter timeout
            return; // ✅ EXIT IMMEDIATELY
        } else {
            // ✅ Server returned success: false
            console.log('❌ Server returned failure:', result.message);            
            $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
        }

    } catch (error) {
        // ✅ CATCH: Only for actual exceptions/network errors
        console.error('💥 Exception during template creation:', error);
        alert.error('Network error occurred: ' + error.message);
        $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
    }
}

// Initialize step-specific functionality - removed Step 4
function initializeCurrentStep() {
    switch (wizardData.currentStep) {
        case 1:
            if (typeof initializeStep1 === 'function') initializeStep1();
            break;
        case 2:
            if (typeof initializeStep2 === 'function') initializeStep2();
            break;
        case 3:
            if (typeof initializeStep3 === 'function') initializeStep3();
            break;
    }
}

async function navigateToStep(targetStep) {
    window.location.href = `/Template/Wizard?step=${targetStep}&id=${wizardData.templateId}`;
}

// Validate current step - calls step-specific validation, removed Step 4
function validateCurrentStep() {
    switch (wizardData.currentStep) {
        case 1:
            return typeof validateStep1Custom === 'function' ? validateStep1Custom() : true;
        case 2:
            return typeof validateStep2Custom === 'function' ? validateStep2Custom() : true;
        case 3:
            return typeof validateStep3Custom === 'function' ? validateStep3Custom() : true;
        default:
            return true;
    }
}

// *** UPDATED: Skip saving for Step 1 ***
async function saveCurrentStepToDatabase() {
    if (isSaving) return false;

    // *** Skip saving for Step 1 - files already saved via AJAX ***
    if (wizardData.currentStep === 1) {
        console.log('📁 [STEP1] Skipping database save - files already saved via AJAX upload/delete');
        return true; // Always return success for Step 1
    }

    isSaving = true;
    try {
        const stepSaveFunction = getStepSaveFunction();
        if (stepSaveFunction) {
            console.log(`💾 Saving step ${wizardData.currentStep} data...`);
            const result = await stepSaveFunction();
            console.log(`💾 Step ${wizardData.currentStep} save result:`, result);
            return result;
        }

        console.log(`⚠️ No save function found for step ${wizardData.currentStep}`);
        return true; // Allow progression if no save function defined
    } catch (error) {
        console.error(`❌ Error saving step ${wizardData.currentStep}:`, error);
        alert.error('Error saving step data');
        return false;
    } finally {
        isSaving = false;
    }
}

// Get step-specific save function - removed Step 4
function getStepSaveFunction() {
    switch (wizardData.currentStep) {
        case 1:
            return null; // *** No save function for Step 1 ***
        case 2:
            return typeof saveStep2Data === 'function' ? saveStep2Data : null;
        case 3:
            return typeof saveStep3Data === 'function' ? saveStep3Data : null;
        default:
            return null;
    }
}

// Updated updateUI function
function updateUI() {
    $('#prev-step-btn').toggle(wizardData.currentStep > 1);
    $('#next-step-btn').toggle(wizardData.currentStep < wizardData.totalSteps);
    $('#finish-btn').toggle(wizardData.currentStep === wizardData.totalSteps);
    updateBreadcrumbs();
}

// Update breadcrumb navigation
function updateBreadcrumbs() {
    $('.nav-wizards-1 .nav-item').each(function (index) {
        const stepNum = index + 1;
        const $link = $(this).find('.nav-link');

        $link.removeClass('completed active pending disabled');

        if (stepNum < wizardData.currentStep) {
            $link.addClass('completed');
        } else if (stepNum === wizardData.currentStep) {
            $link.addClass('active');
        } else {
            $link.addClass('disabled');
        }

        $link.data('step', stepNum);
        $link.data('accessible', stepNum <= wizardData.currentStep);
    });
}

// Global exports
window.wizardData = wizardData;
window.navigateToStep = navigateToStep;
window.nextStep = nextStep;
window.previousStep = previousStep;
window.finishWizard = finishWizard;
window.saveCurrentStepToDatabase = saveCurrentStepToDatabase;