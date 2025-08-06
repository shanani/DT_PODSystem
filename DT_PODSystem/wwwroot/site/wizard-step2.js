// wizard-step2.js - Step 2 Upload PDF (AJAX Only - No Save on Next)
// ===================================================================

let dropzoneInstance = null;

function initializeStep2() {
    console.log('🟢 [STEP2] Initializing Step 2 with AJAX-only approach...');

    // Clear existing data first
    $('#uploaded-files-table tbody').empty();
    wizardData.step2Data = wizardData.step2Data || { uploadedFiles: [] };

    // Load existing files from server data
    loadExistingFiles();

    // Initialize Dropzone with immediate upload
    initDropzoneForStep2();

    // Setup event handlers
    setupStep2EventHandlers();
}

function loadExistingFiles() {
    console.log('🟢 [STEP2] Loading existing files from server...');
    console.log('🟢 [STEP2] Full server data:', window.serverWizardData);

    if (window.serverWizardData) {
        // Try multiple possible locations for the files
        let serverFiles = null;
        let primaryFileName = null;

        // Check different possible locations
        if (window.serverWizardData.uploadedFiles && window.serverWizardData.uploadedFiles.length > 0) {
            serverFiles = window.serverWizardData.uploadedFiles;
            primaryFileName = window.serverWizardData.primaryFileName;
            console.log('🟢 [STEP2] Found files in root uploadedFiles:', serverFiles);
        } else if (window.serverWizardData.step2Data && window.serverWizardData.step2Data.uploadedFiles && window.serverWizardData.step2Data.uploadedFiles.length > 0) {
            serverFiles = window.serverWizardData.step2Data.uploadedFiles;
            primaryFileName = window.serverWizardData.step2Data.primaryFileName;
            console.log('🟢 [STEP2] Found files in step2Data.uploadedFiles:', serverFiles);
        } else if (window.serverWizardData.Step2 && window.serverWizardData.Step2.UploadedFiles && window.serverWizardData.Step2.UploadedFiles.length > 0) {
            serverFiles = window.serverWizardData.Step2.UploadedFiles;
            primaryFileName = window.serverWizardData.Step2.PrimaryFileName;
            console.log('🟢 [STEP2] Found files in Step2.UploadedFiles:', serverFiles);
        }

        if (serverFiles && serverFiles.length > 0) {
            console.log('🟢 [STEP2] Loading', serverFiles.length, 'files');
            console.log('🟢 [STEP2] Primary file should be:', primaryFileName);

            // Clear and repopulate wizard data
            wizardData.step2Data.uploadedFiles = [];

            // Display each file in table
            serverFiles.forEach((fileData, index) => {
                console.log('🟢 [STEP2] Adding file to table:', fileData);

                // Normalize file data format and ensure uploadedAt is included
                const normalizedFile = {
                    originalFileName: fileData.originalFileName || fileData.OriginalFileName || 'Unknown',
                    savedFileName: fileData.savedFileName || fileData.SavedFileName || 'unknown',
                    filePath: fileData.filePath || fileData.FilePath || '',
                    contentType: fileData.contentType || fileData.ContentType || 'application/pdf',
                    fileSize: fileData.fileSize || fileData.FileSize || 0,
                    pageCount: fileData.pageCount || fileData.PageCount || 0,
                    uploadedAt: fileData.uploadedAt || fileData.UploadedAt || new Date().toISOString(),
                    success: true
                };

                wizardData.step2Data.uploadedFiles.push(normalizedFile);

                // Check if this file should be primary
                const shouldBePrimary = primaryFileName ?
                    (normalizedFile.savedFileName === primaryFileName) :
                    (index === 0);

                addFileToTable(normalizedFile, shouldBePrimary);
            });

            // Set correct primary selection if specified
            if (primaryFileName) {
                setTimeout(() => {
                    const radioBtn = $(`input[name="primaryFile"][value="${primaryFileName}"]`);
                    if (radioBtn.length > 0) {
                        radioBtn.prop('checked', true);
                        console.log('🟢 [STEP2] Primary file restored:', primaryFileName);
                    } else {
                        console.warn('🟢 [STEP2] Primary file not found in table:', primaryFileName);
                        // If specified primary not found, select first file
                        $('#uploaded-files-table tbody tr:first input[type="radio"]').prop('checked', true);
                    }
                }, 100);
            } else {
                console.log('🟢 [STEP2] No primary file specified, using first file');
            }

            updateFilesCount();
            $('#no-files-message').hide();
        } else {
            console.log('🟢 [STEP2] No files found in any location');
            $('#no-files-message').show();
        }
    } else {
        console.log('🟢 [STEP2] No server wizard data found');
        $('#no-files-message').show();
    }
}

function initDropzoneForStep2() {
    if (typeof Dropzone !== 'undefined') {
        Dropzone.autoDiscover = false;
        const dropzoneElement = document.getElementById('template-dropzone');

        if (!dropzoneElement) {
            console.error('🟢 [STEP2] Dropzone element #template-dropzone not found');
            return;
        }

        // Clean up existing instance
        if (dropzoneInstance) {
            dropzoneInstance.destroy();
            dropzoneInstance = null;
        }
        if (dropzoneElement.dropzone) {
            dropzoneElement.dropzone.destroy();
            delete dropzoneElement.dropzone;
        }

        // Create new Dropzone instance with immediate upload
        dropzoneInstance = new Dropzone('#template-dropzone', {
            url: '/Upload/Upload',
            maxFilesize: 10,
            acceptedFiles: '.pdf',
            maxFiles: 5,
            addRemoveLinks: false,
            autoProcessQueue: true, // Upload immediately
            uploadMultiple: false,
            parallelUploads: 1,

            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },

            init: function () {
                this.on('success', function (file, response) {
                    console.log('🟢 [STEP2] Upload success:', response);

                    if (response.success) {
                        // IMMEDIATE: Process uploaded file
                        onFileUploadedAjax(response.data);
                    } else {
                        alert.error('Upload failed: ' + response.message);
                    }

                    // Remove from Dropzone UI
                    this.removeFile(file);
                });

                this.on('error', function (file, errorMessage) {
                    console.error('🟢 [STEP2] Upload error:', errorMessage);
                    alert.error('Upload error: ' + (typeof errorMessage === 'string' ? errorMessage : 'Upload failed'));
                    this.removeFile(file);
                });

                this.on('addedfile', function (file) {
                    console.log('🟢 [STEP2] File added to upload queue:', file.name);
                });
            }
        });

        console.log('🟢 [STEP2] Dropzone initialized successfully');
    } else {
        console.error('🟢 [STEP2] Dropzone library not available');
    }
}

function addFileToTable(fileData, isDefaultPrimary = false) {
    const tbody = $('#uploaded-files-table tbody');
    const isFirst = tbody.find('tr').length === 0 || isDefaultPrimary;

    const originalFileName = fileData.originalFileName || 'Unknown';
    const savedFileName = fileData.savedFileName || 'unknown';
    const fileSize = fileData.fileSize || 0;
    const pageCount = fileData.pageCount || 0;

    // Format upload date
    let uploadDateText = '';
    if (fileData.uploadedAt) {
        const uploadDate = new Date(fileData.uploadedAt);
        uploadDateText = uploadDate.toLocaleString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    const row = `
        <tr data-file="${savedFileName}">
            <td class="text-center">
                <input type="radio" name="primaryFile" value="${savedFileName}" ${isFirst ? 'checked' : ''} 
                       class="primary-file-radio" onchange="updatePrimaryFileAjax('${savedFileName}')">
            </td>
            <td>
                <div class="d-flex align-items-center">
                    <i class="fa fa-file-pdf text-danger me-2"></i>
                    <div>
                        <div class="fw-bold" title="${originalFileName}">
                            ${originalFileName.length > 25 ? originalFileName.substring(0, 25) + '...' : originalFileName}
                        </div>
                        <small class="text-muted">
                            ${(fileSize / 1024 / 1024).toFixed(2)} MB
                            ${pageCount ? ` • ${pageCount} pages` : ''}
                            ${uploadDateText ? ` • ${uploadDateText}` : ''}
                        </small>
                    </div>
                </div>
            </td>
            <td><span class="badge bg-success">Uploaded</span></td>
            <td class="file-actions">
                <div class="btn-group" role="group">
                    <button type="button" class="btn btn-sm btn-outline-primary"
                            onclick="previewFile('${savedFileName}')" title="Preview">
                        <i class="fa fa-eye"></i>
                    </button>
                    <button type="button" class="btn btn-sm btn-outline-danger"
                            onclick="removeFileAjax('${savedFileName}')" title="Remove">
                        <i class="fa fa-trash"></i>
                    </button>
                </div>
            </td>
        </tr>
    `;

    tbody.append(row);
}

// AJAX: Handle file upload success immediately
function onFileUploadedAjax(fileData) {
    console.log('🟢 [STEP2] File uploaded via AJAX:', fileData);

    // Normalize file data
    const normalizedFile = {
        originalFileName: fileData.originalFileName,
        savedFileName: fileData.savedFileName,
        filePath: fileData.filePath,
        contentType: fileData.contentType,
        fileSize: fileData.fileSize,
        pageCount: fileData.pageCount || 0,
        success: true
    };

    // IMMEDIATE: Add to wizard data
    wizardData.step2Data.uploadedFiles.push(normalizedFile);

    // IMMEDIATE: Add to table UI
    const isFirstFile = wizardData.step2Data.uploadedFiles.length === 1;
    addFileToTable(normalizedFile, isFirstFile);

    // IMMEDIATE: Update UI
    updateFilesCount();
    $('#no-files-message').hide();

    // IMMEDIATE: Set as primary if first file and update via AJAX
    if (isFirstFile && wizardData.templateId) {
        setTimeout(() => {
            updatePrimaryFileAjax(fileData.savedFileName);
        }, 100);
    }

    alert.success('File uploaded successfully!', { popup: false });
}

// AJAX: Update primary file selection immediately
let isPrimaryUpdateInProgress = false;

function updatePrimaryFileAjax(fileName) {
    console.log('🔄 [STEP2] Updating primary file via AJAX:', fileName);
    console.log('🔄 [STEP2] Template ID:', wizardData.templateId);

    if (!wizardData.templateId) {
        console.warn('🔄 [STEP2] No template ID available for primary file update');
        return;
    }

    // Prevent concurrent calls
    if (isPrimaryUpdateInProgress) {
        console.log('🔄 [STEP2] Primary update already in progress, skipping...');
        return;
    }

    isPrimaryUpdateInProgress = true;

    $.ajax({
        url: '/Template/UpdatePrimaryFile',
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        data: JSON.stringify({
            templateId: wizardData.templateId,
            primaryFileName: fileName
        }),
        success: function (response) {
            console.log('✅ [STEP2] UpdatePrimaryFile response:', response);
            if (response.success) {
                console.log('✅ [STEP2] Primary file updated successfully');
                console.log('✅ [STEP2] Attachments created:', response.data?.attachmentsCreated);
                console.log('✅ [STEP2] Attachments updated:', response.data?.attachmentsUpdated);
                // Update local wizard data
                wizardData.step2Data.primaryFile = fileName;
            } else {
                console.error('❌ [STEP2] Failed to update primary file:', response.message);
            }
        },
        error: function (xhr, status, error) {
            console.error('❌ [STEP2] AJAX error updating primary file:', error);
            console.error('❌ [STEP2] Response text:', xhr.responseText);
        },
        complete: function () {
            // Reset flag when request completes (success or error)
            isPrimaryUpdateInProgress = false;
        }
    });
}

// AJAX: Remove file immediately
function removeFileAjax(fileName) {
    swal({
        title: 'Remove File?',
        text: 'This will permanently remove the file from the template.',
        icon: 'warning',
        buttons: {
            cancel: { text: 'Cancel', className: 'btn btn-default' },
            confirm: { text: 'Remove', className: 'btn btn-danger' }
        }
    }).then((willDelete) => {
        if (willDelete) {
            console.log('🗑️ [STEP2] Removing file via AJAX:', fileName);

            // IMMEDIATE: Remove from server (try both endpoints for compatibility)
            $.ajax({
                url: '/Upload/DeleteFile', // New endpoint
                method: 'POST',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                data: { fileName: fileName },
                success: function (response) {
                    handleFileDeleteSuccess(fileName, response);
                },
                error: function (xhr, status, error) {
                    console.warn('❌ [STEP2] New endpoint failed, trying legacy endpoint...');
                    // Fallback to old endpoint
                    $.ajax({
                        url: '/Upload/DeleteTempFile', // Legacy endpoint
                        method: 'POST',
                        headers: {
                            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                        },
                        data: { fileName: fileName },
                        success: function (response) {
                            handleFileDeleteSuccess(fileName, response);
                        },
                        error: function (xhr, status, error) {
                            console.error('❌ [STEP2] Both delete endpoints failed:', error);
                            alert.error('Failed to delete file from server');
                        }
                    });
                }
            });
        }
    });
}

function handleFileDeleteSuccess(fileName, response) {
    if (response.success) {
        console.log('✅ [STEP2] File deleted from server successfully');

        // IMMEDIATE: Remove from UI
        $(`tr[data-file="${fileName}"]`).remove();

        // IMMEDIATE: Remove from wizard data
        wizardData.step2Data.uploadedFiles = wizardData.step2Data.uploadedFiles.filter(f => {
            return f.savedFileName !== fileName;
        });

        // IMMEDIATE: Update primary selection if needed
        if ($('input[name="primaryFile"]:checked').length === 0 && wizardData.step2Data.uploadedFiles.length > 0) {
            const firstFile = wizardData.step2Data.uploadedFiles[0];
            const firstFileName = firstFile.savedFileName;
            $(`input[name="primaryFile"][value="${firstFileName}"]`).prop('checked', true);
            updatePrimaryFileAjax(firstFileName);
        }

        updateFilesCount();
        alert.success('File removed successfully!', { popup: false });
    } else {
        console.error('❌ [STEP2] Delete failed:', response.message);
        alert.error(response.message || 'Failed to delete file');
    }
}

// Legacy remove function for backward compatibility
function removeFile(fileName) {
    removeFileAjax(fileName);
}

function previewFile(fileName) {
    window.open(`/Upload/Preview/${fileName}`, '_blank');
}

function downloadFile(fileName) {
    window.location.href = `/Upload/Download/${fileName}`;
}

function updateFilesCount() {
    const fileCount = wizardData.step2Data.uploadedFiles.length;
    $('#files-count').text(fileCount);

    if (fileCount > 0) {
        $('.upload-validation-error').hide();
        $('.upload-success-message').show();
        $('#no-files-message').hide();
    } else {
        $('.upload-validation-error').show();
        $('.upload-success-message').hide();
        $('#no-files-message').show();
    }
}

function setupStep2EventHandlers() {
    console.log('🎛️ [STEP2] Setting up event handlers');

    // Handle primary file selection change
    $(document).off('change', 'input[name="primaryFile"]').on('change', 'input[name="primaryFile"]', function () {
        const selectedFile = $(this).val();
        console.log('🟢 [STEP2] Primary file changed to:', selectedFile);

        // Update immediately via AJAX
        updatePrimaryFileAjax(selectedFile);

        // Update wizard data
        wizardData.step2Data.primaryFile = selectedFile;
    });
}

// Step 2 validation function
function validateStep2Custom() {
    const hasFiles = wizardData.step2Data.uploadedFiles && wizardData.step2Data.uploadedFiles.length > 0;
    const hasPrimary = $('input[name="primaryFile"]:checked').length > 0;

    if (!hasFiles) {
        alert.error('Please upload at least one PDF file.');
        return false;
    }

    if (!hasPrimary) {
        alert.error('Please select a primary file.');
        return false;
    }

    return true;
}

// Step 2 data collection function - For navigation only (not saving)
function getStep2FormData() {
    const primaryFile = $('input[name="primaryFile"]:checked').val();

    // Transform data to match Step2DataDto structure
    const uploadedFiles = (wizardData.step2Data.uploadedFiles || []).map(fileData => {
        return {
            OriginalFileName: fileData.originalFileName,
            SavedFileName: fileData.savedFileName,
            FilePath: fileData.filePath,
            FileSize: fileData.fileSize,
            ContentType: fileData.contentType
        };
    });

    return {
        UploadedFiles: uploadedFiles,
        PrimaryFileName: primaryFile || ''
    };
}

// *** REMOVED SAVE STEP 2 DATA FUNCTION ***
// Files are already saved via AJAX upload immediately
// Primary selection is already saved via AJAX immediately
// No need to save anything on "Next" button click

// Legacy save function for backward compatibility (does nothing now)
async function saveStep2Data() {
    console.log('💾 [STEP2] saveStep2Data called but doing nothing - files already saved via AJAX');
    return true; // Always return success since files are already saved
}

// Global exports
window.initializeStep2 = initializeStep2;
window.getStep2FormData = getStep2FormData;
window.validateStep2Custom = validateStep2Custom;
window.saveStep2Data = saveStep2Data; // Legacy compatibility
window.removeFile = removeFile; // Legacy compatibility
window.removeFileAjax = removeFileAjax;
window.updatePrimaryFileAjax = updatePrimaryFileAjax;
window.previewFile = previewFile;
window.downloadFile = downloadFile;