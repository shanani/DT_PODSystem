// wizard-shared.js - Updated for Template Creation on Step 1 Validation
// =======================================================================

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

// *** UPDATED: Create template ID after Step 1 validation ***
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

        // 2. *** NEW: Create template ID after Step 1 validation ***
        if (wizardData.currentStep === 1 && (!wizardData.templateId || wizardData.templateId === 0)) {
            console.log('🆕 [STEP1] Creating new template after validation...');

            // Create template with POD ID from form
            const templateId = await createTemplateAfterStep1Validation();
            if (!templateId) {
                console.log('❌ Failed to create template - canceling navigation');
                return;
            }

            // Update wizard data with new template ID
            wizardData.templateId = templateId;
            console.log('✅ [STEP1] Template created with ID:', templateId);
        }
        // 3. Save current step data for existing templates (Step 2, Step 3, etc.)
        else if (wizardData.currentStep === 1 &&  wizardData.templateId && wizardData.templateId > 0) {
           
            const saveSuccess = await saveTemplateAfterStep1Validation();
            if (!saveSuccess) {
                console.log('❌ Save failed - canceling navigation');
                return;
            }
        }

        // 4. Reset change detection before navigation
        if (typeof resetChangeDetection === 'function') {
            resetChangeDetection();
        }

        // 5. Navigate to next step with template ID
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

// *** NEW: Create template after Step 1 validation ***
async function createTemplateAfterStep1Validation() {
    
    try {
        // Get Step 1 form data (POD ID and template details)
        const step1Data = getStep1FormData();

        if (!step1Data.podId || step1Data.podId <= 0) {
            alert.error('Please select a POD before proceeding');
            return null;
        }

        console.log('🆕 [CREATE] Creating template with data:', step1Data);

        // Call controller to create template for POD
        const response = await fetch('/Template/CreateTemplateForPOD', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                PODId: step1Data.podId,
                NamingConvention: step1Data.namingConvention || 'DOC_POD',
                Title: step1Data.name,
                Description: step1Data.description,
                TechnicalNotes: step1Data.technicalNotes,
                ProcessingPriority: step1Data.processingPriority || 5,
                HasFormFields: step1Data.hasFormFields || false  
            })
        });

        const result = await response.json();

        if (result.success && result.templateId) {
            console.log('✅ [CREATE] Template created successfully with ID:', result.templateId);
            return result.templateId;
        } else {
            console.error('❌ [CREATE] Failed to create template:', result.message);
            alert.error(result.message || 'Failed to create template');
            return null;
        }

    } catch (error) {
        console.error('❌ [CREATE] Error creating template:', error);
        alert.error('Error creating template. Please try again.');
        return null;
    }
}

 async function saveTemplateAfterStep1Validation() {
    try {
        const step1Data = getStep1FormData();

        if (!step1Data.podId || step1Data.podId <= 0) {
            alert.error('Please select a POD before proceeding');
            return false;
        }

        console.log('💾 [SAVE] Saving Step 1 changes to existing template:', wizardData.templateId);

        // ✅ CLEAN: Send ALL PdfTemplate entity fields
        const response = await fetch('/Template/SaveStep1', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                TemplateId: wizardData.templateId,
                Data: {
                    // All PdfTemplate entity fields
                    PODId: step1Data.podId, // Read-only, but include for completeness
                    Title: step1Data.name || 'Untitled Template', // Map form name to Title
                    NamingConvention: step1Data.namingConvention || 'DOC_POD',
                    Status: 0, // TemplateStatus.Draft
                    Version: step1Data.version || '1.0',
                    ProcessingPriority: step1Data.processingPriority || 5,
                    ApprovedBy: null, // Will be set during approval process
                    ApprovalDate: null,
                    LastProcessedDate: null, // Read-only tracking field
                    ProcessedCount: 0, // Read-only tracking field
                    TechnicalNotes: step1Data.technicalNotes || '',
                    HasFormFields: step1Data.hasFormFields || false,
                    ExpectedPdfVersion: step1Data.expectedPdfVersion || null,
                    ExpectedPageCount: step1Data.expectedPageCount || null,
                    IsActive: true
                }
            })
        });

        const result = await response.json();
        
        if (result.success) {
            console.log('✅ [SAVE] Step 1 changes saved successfully');
            return true;
        } else {
            console.error('❌ [SAVE] Failed to save Step 1 changes:', result.message);
            alert.error(result.message || 'Failed to save changes');
            return false;
        }

    } catch (error) {
        console.error('❌ [SAVE] Error saving Step 1 changes:', error);
        alert.error('Error saving changes. Please try again.');
        return false;
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

        // Navigate with template ID if available
        const templateParam = wizardData.templateId && wizardData.templateId > 0 ? `&id=${wizardData.templateId}` : '';
        window.location.href = `/Template/Wizard?step=${prevStepNumber}${templateParam}`;
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

// *** UPDATED: No auto-save for Step 1 on exit ***
async function saveAndExit() {
    if (isSaving) return;

    try {
        $('#save-exit-btn').prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-2"></i>Saving...');

        // Only save if template exists (Step 2+)
        if (wizardData.templateId && wizardData.templateId > 0 && wizardData.currentStep > 1) {
            const saveSuccess = await saveCurrentStepToDatabase();
            if (!saveSuccess) {
                alert.error('Failed to save progress.');
                $('#save-exit-btn').prop('disabled', false).html('<i class="fa fa-save me-2"></i>Save & Exit');
                return;
            }
        } else {
            console.log('📁 [STEP1] No save needed - template not created yet');
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

// Updated finishWizard function - handles Step 3 finalization
async function finishWizard() {
    if (isSaving) return;

    try {
        $('#finish-btn').prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-2"></i>Creating...');

        // Step 3 validation
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
                return;
            }
        } else {
            // Regular validation for other steps
            const isValid = validateCurrentStep();
            if (!isValid) {
                console.log('❌ Regular validation failed');
                $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
                return;
            }
        }

        console.log('✅ Validation passed, saving step data...');

        // Save current step data
        const saveSuccess = await saveCurrentStepToDatabase();
        if (!saveSuccess) {
            console.log('❌ Failed to save step data');
            alert.error('Failed to save template data');
            $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
            return;
        }

        console.log('🏁 Finalizing template...');

        // Finalize template (change status from Draft to Active)
        const finalizeResponse = await fetch('/Template/FinalizeTemplate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            body: JSON.stringify({
                TemplateId: wizardData.templateId
            })
        });

        const finalizeResult = await finalizeResponse.json();

        if (finalizeResult.success) {
            console.log('✅ Template finalized successfully');

            // Reset change detection
            if (typeof resetChangeDetection === 'function') {
                resetChangeDetection();
            }

            alert.success('Template created and activated successfully!');
            setTimeout(() => {
                window.location.href = '/Template';
            }, 1500);
        } else {
            console.error('❌ Failed to finalize template:', finalizeResult.message);
            alert.error(finalizeResult.message || 'Failed to finalize template');
            $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
        }

    } catch (error) {
        console.error('❌ Error finalizing template:', error);
        alert.error('An error occurred while finalizing the template');
        $('#finish-btn').prop('disabled', false).html('<i class="fa fa-check me-2"></i>Create Template');
    }
}

// Navigation to specific step
function navigateToStep(step) {
    if (step < 1 || step > wizardData.totalSteps) {
        console.log('❌ Invalid step number:', step);
        return;
    }

    // Reset change detection
    if (typeof resetChangeDetection === 'function') {
        resetChangeDetection();
    }

    // Navigate with template ID if available
    const templateParam = wizardData.templateId && wizardData.templateId > 0 ? `&id=${wizardData.templateId}` : '';
    window.location.href = `/Template/Wizard?step=${step}${templateParam}`;
}

// Initialize current step
function initializeCurrentStep() {
    const currentStep = wizardData.currentStep;

    switch (currentStep) {
        case 1:
            if (typeof initializeStep1 === 'function') {
                initializeStep1();
            }
            break;
        case 2:
            if (typeof initializeStep2 === 'function') {
                initializeStep2();
            }
            break;
        case 3:
            if (typeof initializeStep3 === 'function') {
                initializeStep3();
            }
            break;
    }
}

// Validate current step
function validateCurrentStep() {
    const currentStep = wizardData.currentStep;

    switch (currentStep) {
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
 

// Update UI elements
function updateUI() {
    // Update step indicator
    $('.nav-wizards-1 .nav-link').removeClass('active completed');
    for (let i = 1; i <= wizardData.totalSteps; i++) {
        const link = $(`.nav-wizards-1 .nav-link`).eq(i - 1);
        if (i < wizardData.currentStep) {
            link.addClass('completed');
        } else if (i === wizardData.currentStep) {
            link.addClass('active');
        }
    }

    // Update progress bar
    const progressPercent = (wizardData.currentStep * 100) / wizardData.totalSteps;
    $('.progress-bar').css('width', progressPercent + '%');
    $('.progress-text').text(`Step ${wizardData.currentStep} of ${wizardData.totalSteps}`);
    $('.progress-percent').text(`${Math.round(progressPercent)}% Complete`);
}

// Global error handling
window.addEventListener('unhandledrejection', function (event) {
    console.error('Unhandled promise rejection:', event.reason);
    alert.error('An unexpected error occurred. Please try again.');
});

// Export functions for global access
window.nextStep = nextStep;
window.previousStep = previousStep;
window.navigateToStep = navigateToStep;
window.saveAndExit = saveAndExit;
window.finishWizard = finishWizard;