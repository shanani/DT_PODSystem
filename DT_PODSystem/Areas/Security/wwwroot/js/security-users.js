// ============================================================================================
// Areas/Security/wwwroot/js/security-users.js
// JavaScript for Security Users Management
// ============================================================================================

/**
 * Security Users Management JavaScript Module
 * Handles DataTables, AJAX operations, and user interactions
 */
var SecurityUsersModule = (function () {
    'use strict';

    // Private variables
    var usersTable;
    var selectedUsers = [];
    var config = {
        ajaxUrls: {
            getUsersData: '',
            toggleLock: '',
            toggleStatus: '',
            delete: '',
            bulkOperation: '',
            export: ''
        },
        selectors: {
            usersTable: '#usersTable',
            roleFilter: '#roleFilter',
            statusFilter: '#statusFilter',
            departmentFilter: '#departmentFilter',
            searchInput: '#searchInput',
            recordCount: '#recordCount',
            selectedCount: '#selectedCount',
            bulkActionsPanel: '#bulkActionsPanel'
        }
    };

    /**
     * Initialize the module
     */
    function init(urls) {
        config.ajaxUrls = Object.assign(config.ajaxUrls, urls);
        initializeDataTable();
        setupEventHandlers();
        setupFilters();
        setupBulkOperations();
    }

    /**
     * Initialize DataTables
     */
    function initializeDataTable() {
        usersTable = $(config.selectors.usersTable).DataTable({
            processing: true,
            serverSide: true,
            responsive: true,
            pageLength: 25,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[1, 'asc']], // Sort by Full Name
            ajax: {
                url: config.ajaxUrls.getUsersData,
                type: 'POST',
                data: function (d) {
                    // Add custom filters
                    d.roleFilter = $(config.selectors.roleFilter).val();
                    d.statusFilter = $(config.selectors.statusFilter).val();
                    d.departmentFilter = $(config.selectors.departmentFilter).val();
                },
                error: function (xhr, error, thrown) {
                    console.error('DataTable AJAX error:', error);
                    showAlert('error', 'Error loading users data');
                }
            },
            columns: [
                {
                    data: null,
                    name: 'checkbox',
                    orderable: false,
                    searchable: false,
                    className: 'text-center',
                    width: '30px',
                    render: function (data, type, row) {
                        return `<input type="checkbox" class="user-checkbox" value="${row.id}" data-user-name="${row.fullName}">`;
                    }
                },
                { data: 'code', name: 'Code', className: 'fw-bold' },
                { data: 'fullName', name: 'FullName' },
                { data: 'email', name: 'Email' },
                { data: 'department', name: 'Department' },
                {
                    data: 'roles',
                    name: 'Roles',
                    orderable: false,
                    render: function (data, type, row) {
                        if (!data || data === 'N/A') {
                            return '<span class="text-muted">No roles</span>';
                        }
                        var roles = data.split(', ');
                        var badges = roles.map(role => `<span class="badge bg-info me-1">${role}</span>`);
                        return badges.join('');
                    }
                },
                {
                    data: 'statusBadge',
                    name: 'Status',
                    orderable: false,
                    className: 'text-center',
                    searchable: false
                },
                {
                    data: 'lastLoginAt',
                    name: 'LastLoginAt',
                    render: function (data, type, row) {
                        if (data === 'Never') {
                            return '<span class="text-muted">Never</span>';
                        }
                        return data;
                    }
                },
                {
                    data: 'actions',
                    name: 'Actions',
                    orderable: false,
                    searchable: false,
                    className: 'text-center',
                    width: '180px'
                }
            ],
            columnDefs: [
                { targets: [0, 5, 6, 8], orderable: false },
                { targets: [0, 6, 8], searchable: false }
            ],
            drawCallback: function (settings) {
                updateRecordCount(settings.json.recordsTotal, settings.json.recordsFiltered);
                setupRowEventHandlers();
                updateBulkActionsState();
            },
            language: {
                processing: '<i class="fa fa-spinner fa-spin"></i> Loading users...',
                emptyTable: 'No users found',
                zeroRecords: 'No matching users found',
                info: 'Showing _START_ to _END_ of _TOTAL_ users',
                infoEmpty: 'Showing 0 to 0 of 0 users',
                infoFiltered: '(filtered from _MAX_ total users)'
            },
            dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                '<"row"<"col-sm-12"tr>>' +
                '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
            buttons: [
                {
                    extend: 'excel',
                    text: '<i class="fa fa-file-excel"></i> Export Excel',
                    className: 'btn btn-success btn-sm'
                },
                {
                    extend: 'pdf',
                    text: '<i class="fa fa-file-pdf"></i> Export PDF',
                    className: 'btn btn-danger btn-sm'
                }
            ]
        });
    }

    /**
     * Setup event handlers
     */
    function setupEventHandlers() {
        // Filter change handlers
        $(config.selectors.roleFilter + ', ' +
            config.selectors.statusFilter + ', ' +
            config.selectors.departmentFilter).on('change', function () {
                applyFilters();
            });

        // Search input handler with debounce
        var searchTimeout;
        $(config.selectors.searchInput).on('keyup', function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(function () {
                usersTable.search($(config.selectors.searchInput).val()).draw();
            }, 500);
        });

        // Master checkbox handler
        $(document).on('change', '#selectAllUsers', function () {
            var isChecked = $(this).is(':checked');
            $('.user-checkbox').prop('checked', isChecked);
            updateSelectedUsers();
        });

        // Individual checkbox handler
        $(document).on('change', '.user-checkbox', function () {
            updateSelectedUsers();
            updateMasterCheckbox();
        });

        // Refresh button
        $(document).on('click', '.btn-refresh', function () {
            refreshTable();
        });

        // Clear filters button
        $(document).on('click', '.btn-clear-filters', function () {
            clearFilters();
        });
    }

    /**
     * Setup row-specific event handlers
     */
    function setupRowEventHandlers() {
        // Remove any existing handlers to prevent duplicates
        $(document).off('click', '.btn-toggle-lock');
        $(document).off('click', '.btn-toggle-status');
        $(document).off('click', '.btn-delete-user');

        // Toggle lock button
        $(document).on('click', '.btn-toggle-lock', function (e) {
            e.preventDefault();
            var userId = $(this).data('user-id');
            var isLocked = $(this).data('is-locked');
            toggleUserLock(userId, isLocked);
        });

        // Toggle status button
        $(document).on('click', '.btn-toggle-status', function (e) {
            e.preventDefault();
            var userId = $(this).data('user-id');
            var isActive = $(this).data('is-active');
            toggleUserStatus(userId, isActive);
        });

        // Delete user button
        $(document).on('click', '.btn-delete-user', function (e) {
            e.preventDefault();
            var userId = $(this).data('user-id');
            var userName = $(this).data('user-name');
            confirmDeleteUser(userId, userName);
        });
    }

    /**
     * Setup filters functionality
     */
    function setupFilters() {
        // Advanced filters toggle
        $(document).on('click', '.btn-advanced-filters', function () {
            $('#advancedFiltersPanel').slideToggle();
        });

        // Quick filter buttons
        $(document).on('click', '.quick-filter', function () {
            var filterType = $(this).data('filter-type');
            var filterValue = $(this).data('filter-value');

            switch (filterType) {
                case 'status':
                    $(config.selectors.statusFilter).val(filterValue);
                    break;
                case 'role':
                    $(config.selectors.roleFilter).val(filterValue);
                    break;
            }

            applyFilters();
        });
    }

    /**
     * Setup bulk operations
     */
    function setupBulkOperations() {
        // Bulk action buttons
        $(document).on('click', '.btn-bulk-activate', function () {
            performBulkOperation('activate');
        });

        $(document).on('click', '.btn-bulk-deactivate', function () {
            performBulkOperation('deactivate');
        });

        $(document).on('click', '.btn-bulk-lock', function () {
            performBulkOperation('lock');
        });

        $(document).on('click', '.btn-bulk-unlock', function () {
            performBulkOperation('unlock');
        });

        $(document).on('click', '.btn-bulk-delete', function () {
            performBulkOperation('delete');
        });

        $(document).on('click', '.btn-bulk-assign-role', function () {
            showBulkRoleAssignmentModal();
        });
    }

    /**
     * Apply filters and refresh table
     */
    function applyFilters() {
        usersTable.ajax.reload();
    }

    /**
     * Clear all filters
     */
    function clearFilters() {
        $(config.selectors.roleFilter).val('');
        $(config.selectors.statusFilter).val('');
        $(config.selectors.departmentFilter).val('');
        $(config.selectors.searchInput).val('');
        usersTable.search('').ajax.reload();
    }

    /**
     * Refresh table data
     */
    function refreshTable() {
        usersTable.ajax.reload(null, false);
        showAlert('info', 'Table refreshed', { popup: false });
    }

    /**
     * Update record count display
     */
    function updateRecordCount(total, filtered) {
        var countText = total === filtered ?
            `${total} users` :
            `${filtered} of ${total} users`;
        $(config.selectors.recordCount).text(countText);
    }

    /**
     * Update selected users array and UI
     */
    function updateSelectedUsers() {
        selectedUsers = [];
        $('.user-checkbox:checked').each(function () {
            selectedUsers.push({
                id: parseInt($(this).val()),
                name: $(this).data('user-name')
            });
        });

        updateSelectedCount();
        updateBulkActionsState();
    }

    /**
     * Update selected count display
     */
    function updateSelectedCount() {
        var count = selectedUsers.length;
        $(config.selectors.selectedCount).text(count);

        if (count > 0) {
            $(config.selectors.bulkActionsPanel).removeClass('d-none');
        } else {
            $(config.selectors.bulkActionsPanel).addClass('d-none');
        }
    }

    /**
     * Update master checkbox state
     */
    function updateMasterCheckbox() {
        var totalCheckboxes = $('.user-checkbox').length;
        var checkedCheckboxes = $('.user-checkbox:checked').length;

        if (checkedCheckboxes === 0) {
            $('#selectAllUsers').prop('indeterminate', false).prop('checked', false);
        } else if (checkedCheckboxes === totalCheckboxes) {
            $('#selectAllUsers').prop('indeterminate', false).prop('checked', true);
        } else {
            $('#selectAllUsers').prop('indeterminate', true);
        }
    }

    /**
     * Update bulk actions state
     */
    function updateBulkActionsState() {
        var hasSelection = selectedUsers.length > 0;
        $('.bulk-action-btn').prop('disabled', !hasSelection);
    }

    /**
     * Toggle user lock status
     */
    function toggleUserLock(userId, isCurrentlyLocked) {
        var action = isCurrentlyLocked ? 'unlock' : 'lock';
        var confirmMessage = `Are you sure you want to ${action} this user account?`;

        if (confirm(confirmMessage)) {
            $.ajax({
                url: config.ajaxUrls.toggleLock,
                type: 'POST',
                data: { id: userId },
                success: function (response) {
                    if (response.success) {
                        showAlert('success', response.message, { popup: false });
                        usersTable.ajax.reload(null, false);
                    } else {
                        showAlert('error', response.message, { popup: false });
                    }
                },
                error: function () {
                    showAlert('error', 'Error updating user status', { popup: false });
                }
            });
        }
    }

    /**
     * Toggle user active status
     */
    function toggleUserStatus(userId, isCurrentlyActive) {
        var action = isCurrentlyActive ? 'deactivate' : 'activate';
        var confirmMessage = `Are you sure you want to ${action} this user account?`;

        if (confirm(confirmMessage)) {
            $.ajax({
                url: config.ajaxUrls.toggleStatus,
                type: 'POST',
                data: { id: userId },
                success: function (response) {
                    if (response.success) {
                        showAlert('success', response.message, { popup: false });
                        usersTable.ajax.reload(null, false);
                    } else {
                        showAlert('error', response.message, { popup: false });
                    }
                },
                error: function () {
                    showAlert('error', 'Error updating user status', { popup: false });
                }
            });
        }
    }

    /**
     * Confirm and delete user
     */
    function confirmDeleteUser(userId, userName) {
        var confirmMessage = `Are you sure you want to delete the user "${userName}"? This action cannot be undone.`;

        if (confirm(confirmMessage)) {
            var finalConfirm = confirm('This is your final warning. The user will be permanently deleted. Continue?');
            if (finalConfirm) {
                deleteUser(userId);
            }
        }
    }

    /**
     * Delete user
     */
    function deleteUser(userId) {
        $.ajax({
            url: config.ajaxUrls.delete,
            type: 'POST',
            data: { id: userId },
            success: function (response) {
                if (response.success) {
                    showAlert('success', response.message, { popup: false });
                    usersTable.ajax.reload();
                } else {
                    showAlert('error', response.message, { popup: false });
                }
            },
            error: function () {
                showAlert('error', 'Error deleting user', { popup: false });
            }
        });
    }

    /**
     * Perform bulk operation
     */
    function performBulkOperation(operation) {
        if (selectedUsers.length === 0) {
            showAlert('warning', 'Please select users to perform this operation', { popup: false });
            return;
        }

        var confirmMessage = getBulkOperationConfirmMessage(operation, selectedUsers.length);

        if (confirm(confirmMessage)) {
            var userIds = selectedUsers.map(user => user.id);

            $.ajax({
                url: config.ajaxUrls.bulkOperation,
                type: 'POST',
                data: {
                    operation: operation,
                    userIds: userIds
                },
                success: function (response) {
                    if (response.success) {
                        showAlert('success', response.message, { popup: false });
                        usersTable.ajax.reload();
                        clearSelection();
                    } else {
                        showAlert('error', response.message, { popup: false });
                    }
                },
                error: function () {
                    showAlert('error', 'Error performing bulk operation', { popup: false });
                }
            });
        }
    }

    /**
     * Get confirmation message for bulk operations
     */
    function getBulkOperationConfirmMessage(operation, count) {
        var userText = count === 1 ? 'user' : 'users';

        switch (operation) {
            case 'activate':
                return `Are you sure you want to activate ${count} ${userText}?`;
            case 'deactivate':
                return `Are you sure you want to deactivate ${count} ${userText}?`;
            case 'lock':
                return `Are you sure you want to lock ${count} ${userText}?`;
            case 'unlock':
                return `Are you sure you want to unlock ${count} ${userText}?`;
            case 'delete':
                return `Are you sure you want to delete ${count} ${userText}? This action cannot be undone.`;
            default:
                return `Are you sure you want to perform this operation on ${count} ${userText}?`;
        }
    }

    /**
     * Show bulk role assignment modal
     */
    function showBulkRoleAssignmentModal() {
        if (selectedUsers.length === 0) {
            showAlert('warning', 'Please select users to assign roles', { popup: false });
            return;
        }

        // Load and show the modal (implementation depends on your modal system)
        $('#bulkRoleAssignmentModal').modal('show');
        updateBulkRoleModalUserList();
    }

    /**
     * Update bulk role modal user list
     */
    function updateBulkRoleModalUserList() {
        var userList = selectedUsers.map(user => `<li>${user.name}</li>`).join('');
        $('#bulkRoleUserList').html(`<ul>${userList}</ul>`);
        $('#bulkRoleUserCount').text(selectedUsers.length);
    }

    /**
     * Clear selection
     */
    function clearSelection() {
        $('.user-checkbox').prop('checked', false);
        $('#selectAllUsers').prop('checked', false).prop('indeterminate', false);
        selectedUsers = [];
        updateSelectedCount();
        updateBulkActionsState();
    }

    /**
     * Show alert message
     */
    function showAlert(type, message, options = {}) {
        if (typeof alert !== 'undefined' && alert[type]) {
            alert[type](message, options);
        } else {
            // Fallback to browser alert
            window.alert(message);
        }
    }

    /**
     * Export users data
     */
    function exportUsers(format) {
        var params = {
            format: format,
            roleFilter: $(config.selectors.roleFilter).val(),
            statusFilter: $(config.selectors.statusFilter).val(),
            departmentFilter: $(config.selectors.departmentFilter).val(),
            searchTerm: usersTable.search()
        };

        var queryString = Object.keys(params)
            .filter(key => params[key])
            .map(key => `${key}=${encodeURIComponent(params[key])}`)
            .join('&');

        window.open(`${config.ajaxUrls.export}?${queryString}`, '_blank');
    }

    /**
     * Setup advanced search
     */
    function setupAdvancedSearch() {
        $('#advancedSearchForm').on('submit', function (e) {
            e.preventDefault();

            var searchParams = {
                searchTerm: $('#advancedSearchTerm').val(),
                roleFilter: $('#advancedRoleFilter').val(),
                statusFilter: $('#advancedStatusFilter').val(),
                departmentFilter: $('#advancedDepartmentFilter').val(),
                createdFrom: $('#createdFromDate').val(),
                createdTo: $('#createdToDate').val(),
                lastLoginFrom: $('#lastLoginFromDate').val(),
                lastLoginTo: $('#lastLoginToDate').val()
            };

            // Apply search parameters to DataTable
            usersTable.ajax.reload();

            // Hide advanced search panel
            $('#advancedSearchPanel').slideUp();
        });
    }

    /**
     * Setup keyboard shortcuts
     */
    function setupKeyboardShortcuts() {
        $(document).on('keydown', function (e) {
            // Ctrl/Cmd + R: Refresh table
            if ((e.ctrlKey || e.metaKey) && e.key === 'r') {
                e.preventDefault();
                refreshTable();
            }

            // Ctrl/Cmd + A: Select all
            if ((e.ctrlKey || e.metaKey) && e.key === 'a' && e.target.tagName !== 'INPUT') {
                e.preventDefault();
                $('#selectAllUsers').click();
            }

            // Escape: Clear selection
            if (e.key === 'Escape') {
                clearSelection();
            }
        });
    }

    /**
     * Setup real-time notifications
     */
    function setupRealTimeNotifications() {
        // If SignalR is available, setup real-time updates
        if (typeof signalR !== 'undefined') {
            var connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub")
                .build();

            connection.start().then(function () {
                console.log('SignalR Connected');
            }).catch(function (err) {
                console.error('SignalR Connection Error: ', err.toString());
            });

            // Listen for user updates
            connection.on("UserUpdated", function (userId, action) {
                // Refresh the specific row or entire table
                usersTable.ajax.reload(null, false);

                // Show notification
                showAlert('info', `User ${action} successfully`, { popup: false });
            });
        }
    }

    /**
     * Utility function to format dates
     */
    function formatDate(date, format = 'yyyy-MM-dd HH:mm') {
        if (!date) return 'N/A';

        var d = new Date(date);
        if (isNaN(d.getTime())) return 'Invalid Date';

        // Simple date formatting (you might want to use a library like moment.js)
        var year = d.getFullYear();
        var month = String(d.getMonth() + 1).padStart(2, '0');
        var day = String(d.getDate()).padStart(2, '0');
        var hours = String(d.getHours()).padStart(2, '0');
        var minutes = String(d.getMinutes()).padStart(2, '0');

        switch (format) {
            case 'yyyy-MM-dd':
                return `${year}-${month}-${day}`;
            case 'yyyy-MM-dd HH:mm':
                return `${year}-${month}-${day} ${hours}:${minutes}`;
            default:
                return d.toLocaleDateString();
        }
    }

    /**
     * Utility function to debounce function calls
     */
    function debounce(func, wait, immediate) {
        var timeout;
        return function () {
            var context = this, args = arguments;
            var later = function () {
                timeout = null;
                if (!immediate) func.apply(context, args);
            };
            var callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func.apply(context, args);
        };
    }

    // Public API
    return {
        init: init,
        refreshTable: refreshTable,
        clearFilters: clearFilters,
        applyFilters: applyFilters,
        exportUsers: exportUsers,
        getSelectedUsers: function () { return selectedUsers; },
        clearSelection: clearSelection,
        toggleUserLock: toggleUserLock,
        toggleUserStatus: toggleUserStatus,
        performBulkOperation: performBulkOperation,
        formatDate: formatDate
    };
})();

// Auto-initialize when DOM is ready
$(document).ready(function () {
    // Initialize the module if the users table exists
    if ($('#usersTable').length > 0) {
        // URLs should be set by the view
        var urls = window.SecurityUsersUrls || {};
        SecurityUsersModule.init(urls);
    }
});

// Additional utility functions for form validation
var SecurityUsersFormValidation = (function () {
    'use strict';

    /**
     * Validate user creation form
     */
    function validateCreateForm() {
        var form = $('#createUserForm');
        var isValid = true;
        var errors = [];

        // Clear previous errors
        $('.validation-error').remove();
        $('.is-invalid').removeClass('is-invalid');

        // Validate required fields
        var requiredFields = ['Code', 'FirstName', 'LastName'];
        requiredFields.forEach(function (field) {
            var input = $(`#${field}`);
            if (!input.val().trim()) {
                isValid = false;
                errors.push(`${field.replace(/([A-Z])/g, ' $1').trim()} is required`);
                input.addClass('is-invalid');
                input.after(`<div class="validation-error text-danger small">${field.replace(/([A-Z])/g, ' $1').trim()} is required</div>`);
            }
        });

        // Validate email if provided
        var email = $('#Email');
        if (email.val().trim() && !isValidEmail(email.val())) {
            isValid = false;
            errors.push('Please enter a valid email address');
            email.addClass('is-invalid');
            email.after('<div class="validation-error text-danger small">Please enter a valid email address</div>');
        }

        // Validate role selection
        var selectedRoles = $('input[name="SelectedRoleIds"]:checked').length;
        if (selectedRoles === 0) {
            isValid = false;
            errors.push('Please select at least one role for the user');
            $('.role-selection').addClass('border-danger');
            $('.role-selection').after('<div class="validation-error text-danger small">Please select at least one role for the user</div>');
        }

        return {
            isValid: isValid,
            errors: errors
        };
    }

    /**
     * Validate email format
     */
    function isValidEmail(email) {
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    /**
     * Validate user code uniqueness
     */
    function validateUserCodeUniqueness(userCode, currentUserId = null) {
        return new Promise(function (resolve, reject) {
            $.ajax({
                url: '/Security/SecurityUsers/CheckUserCodeExists',
                type: 'POST',
                data: {
                    code: userCode,
                    currentUserId: currentUserId
                },
                success: function (response) {
                    resolve(!response.exists);
                },
                error: function () {
                    reject(false);
                }
            });
        });
    }

    /**
     * Setup real-time validation
     */
    function setupRealTimeValidation() {
        // User code validation
        $('#Code').on('blur', function () {
            var userCode = $(this).val().trim();
            if (userCode) {
                validateUserCodeUniqueness(userCode).then(function (isUnique) {
                    if (!isUnique) {
                        $('#Code').addClass('is-invalid');
                        $('#Code').siblings('.validation-error').remove();
                        $('#Code').after('<div class="validation-error text-danger small">This user code already exists</div>');
                    } else {
                        $('#Code').removeClass('is-invalid');
                        $('#Code').siblings('.validation-error').remove();
                    }
                });
            }
        });

        // Email validation
        $('#Email').on('blur', function () {
            var email = $(this).val().trim();
            if (email && !isValidEmail(email)) {
                $(this).addClass('is-invalid');
                $(this).siblings('.validation-error').remove();
                $(this).after('<div class="validation-error text-danger small">Please enter a valid email address</div>');
            } else {
                $(this).removeClass('is-invalid');
                $(this).siblings('.validation-error').remove();
            }
        });

        // Role selection validation
        $('input[name="SelectedRoleIds"]').on('change', function () {
            var selectedRoles = $('input[name="SelectedRoleIds"]:checked').length;
            if (selectedRoles > 0) {
                $('.role-selection').removeClass('border-danger');
                $('.role-selection').siblings('.validation-error').remove();
            }
        });
    }

    // Public API
    return {
        validateCreateForm: validateCreateForm,
        validateUserCodeUniqueness: validateUserCodeUniqueness,
        setupRealTimeValidation: setupRealTimeValidation,
        isValidEmail: isValidEmail
    };
})();

// Initialize form validation when DOM is ready
$(document).ready(function () {
    if ($('#createUserForm, #editUserForm').length > 0) {
        SecurityUsersFormValidation.setupRealTimeValidation();

        // Setup form submission validation
        $('#createUserForm, #editUserForm').on('submit', function (e) {
            var validation = SecurityUsersFormValidation.validateCreateForm();
            if (!validation.isValid) {
                e.preventDefault();
                var errorMessage = 'Please correct the following errors:\n' + validation.errors.join('\n');
                alert(errorMessage);
                return false;
            }
        });
    }
});