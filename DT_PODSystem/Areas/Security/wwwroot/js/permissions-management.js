/**
 * Permissions Management JavaScript
 * Handles CRUD operations for permissions with DataTables, AJAX, and SweetAlert
 */

$(document).ready(function () {
    'use strict';

    // Initialize permissions management
    PermissionsManager.init();
});

const PermissionsManager = {

    // Configuration
    config: {
        tableId: '#permissionsTable',
        ajaxUrl: '/Security/Permissions/',
        deleteUrl: '/Security/Permissions/Delete/',
        toggleStatusUrl: '/Security/Permissions/ToggleStatus/',
        bulkActionUrl: '/Security/Permissions/BulkAction/'
    },

    // Initialize the module
    init: function () {
        this.initDataTable();
        this.bindEvents();
        this.initFilters();
        this.initTooltips();
    },

    // Initialize DataTable
    initDataTable: function () {
        const self = this;

        if ($(self.config.tableId).length) {
            self.dataTable = $(self.config.tableId).DataTable({
                processing: true,
                serverSide: true,
                responsive: true,
                ajax: {
                    url: self.config.ajaxUrl + 'GetPermissions',
                    type: 'POST',
                    data: function (d) {
                        // Add filter parameters
                        d.permissionTypeFilter = $('#permissionTypeFilter').val();
                        d.statusFilter = $('#statusFilter').val();
                        d.systemTypeFilter = $('#systemTypeFilter').val();
                    }
                },
                columns: [
                    {
                        data: null,
                        orderable: false,
                        searchable: false,
                        width: "5%",
                        render: function (data, type, row) {
                            return `<div class="form-check">
                                        <input class="form-check-input permission-checkbox" 
                                               type="checkbox" value="${row.id}">
                                    </div>`;
                        }
                    },
                    {
                        data: 'name',
                        width: "20%",
                        render: function (data, type, row) {
                            return `<div class="d-flex align-items-center">
                                        <i class="${row.icon} fas me-2 text-${row.color}"></i>
                                        <div>
                                            <div class="fw-bold">${data}</div>
                                            <small class="text-muted">${row.systemName}</small>
                                        </div>
                                    </div>`;
                        }
                    },
                    {
                        data: 'permissionType',
                        width: "15%",
                        render: function (data, type, row) {
                            return `<span class="badge bg-${data.color}">
                                        <i class="${data.icon} me-1"></i>${data.name}
                                    </span>`;
                        }
                    },
                    {
                        data: 'description',
                        width: "25%",
                        render: function (data, type, row) {
                            if (data && data.length > 50) {
                                return `<span data-bs-toggle="tooltip" title="${data}">
                                            ${data.substring(0, 50)}...
                                        </span>`;
                            }
                            return data || '';
                        }
                    },
                    {
                        data: 'isActive',
                        width: "10%",
                        render: function (data, type, row) {
                            if (data) {
                                return `<span class="badge bg-success">
                                            <i class="fas fa-check me-1"></i>Active
                                        </span>`;
                            } else {
                                return `<span class="badge bg-secondary">
                                            <i class="fas fa-times me-1"></i>Inactive
                                        </span>`;
                            }
                        }
                    },
                    {
                        data: 'isSystemPermission',
                        width: "10%",
                        render: function (data, type, row) {
                            if (data) {
                                return `<span class="badge bg-info">
                                            <i class="fas fa-cog me-1"></i>System
                                        </span>`;
                            } else {
                                return `<span class="badge bg-primary">
                                            <i class="fas fa-user me-1"></i>Custom
                                        </span>`;
                            }
                        }
                    },
                    {
                        data: 'createdAt',
                        width: "10%",
                        render: function (data, type, row) {
                            return `<small>${new Date(data).toLocaleDateString()}</small>`;
                        }
                    },
                    {
                        data: null,
                        orderable: false,
                        searchable: false,
                        width: "5%",
                        render: function (data, type, row) {
                            let actions = `<div class="btn-group" role="group">
                                             <a href="/Security/Permissions/Details/${row.id}" 
                                                class="btn btn-sm btn-outline-info" 
                                                data-bs-toggle="tooltip" title="View Details">
                                                <i class="fas fa-eye"></i>
                                             </a>
                                             <a href="/Security/Permissions/Edit/${row.id}" 
                                                class="btn btn-sm btn-outline-warning" 
                                                data-bs-toggle="tooltip" title="Edit Permission">
                                                <i class="fas fa-edit"></i>
                                             </a>`;

                            if (!row.isSystemPermission) {
                                actions += `<button type="button" 
                                                   class="btn btn-sm btn-outline-danger delete-permission" 
                                                   data-permission-id="${row.id}"
                                                   data-permission-name="${row.name}"
                                                   data-bs-toggle="tooltip" title="Delete Permission">
                                                   <i class="fas fa-trash"></i>
                                            </button>`;
                            }

                            actions += `<button type="button" 
                                               class="btn btn-sm btn-outline-secondary toggle-status" 
                                               data-permission-id="${row.id}"
                                               data-current-status="${row.isActive}"
                                               data-bs-toggle="tooltip" title="Toggle Status">`;

                            if (row.isActive) {
                                actions += `<i class="fas fa-toggle-on text-success"></i>`;
                            } else {
                                actions += `<i class="fas fa-toggle-off text-secondary"></i>`;
                            }

                            actions += `</button></div>`;

                            return actions;
                        }
                    }
                ],
                dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rtip',
                pageLength: 25,
                lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
                language: {
                    processing: '<i class="fas fa-spinner fa-spin"></i> Loading...',
                    emptyTable: 'No permissions found',
                    zeroRecords: 'No matching permissions found'
                },
                drawCallback: function () {
                    // Reinitialize tooltips after table redraw
                    self.initTooltips();

                    // Update bulk actions visibility
                    self.updateBulkActionsVisibility();
                }
            });
        }
    },

    // Bind event handlers
    bindEvents: function () {
        const self = this;

        // Select all checkbox
        $(document).on('change', '#selectAll', function () {
            const isChecked = $(this).is(':checked');
            $('.permission-checkbox').prop('checked', isChecked);
            self.updateBulkActionsVisibility();
        });

        // Individual checkbox change
        $(document).on('change', '.permission-checkbox', function () {
            self.updateBulkActionsVisibility();

            // Update select all checkbox state
            const totalCheckboxes = $('.permission-checkbox').length;
            const checkedCheckboxes = $('.permission-checkbox:checked').length;

            if (checkedCheckboxes === 0) {
                $('#selectAll').prop('indeterminate', false).prop('checked', false);
            } else if (checkedCheckboxes === totalCheckboxes) {
                $('#selectAll').prop('indeterminate', false).prop('checked', true);
            } else {
                $('#selectAll').prop('indeterminate', true);
            }
        });

        // Delete permission
        $(document).on('click', '.delete-permission', function () {
            const permissionId = $(this).data('permission-id');
            const permissionName = $(this).data('permission-name');
            self.deletePermission(permissionId, permissionName);
        });

        // Toggle status
        $(document).on('click', '.toggle-status', function () {
            const permissionId = $(this).data('permission-id');
            const currentStatus = $(this).data('current-status');
            self.togglePermissionStatus(permissionId, !currentStatus);
        });

        // Bulk actions
        $(document).on('click', '#bulkActivate', function () {
            self.bulkAction('activate');
        });

        $(document).on('click', '#bulkDeactivate', function () {
            self.bulkAction('deactivate');
        });

        $(document).on('click', '#bulkDelete', function () {
            self.bulkAction('delete');
        });
    },

    // Initialize filters
    initFilters: function () {
        const self = this;

        // Filter change handlers
        $('#permissionTypeFilter, #statusFilter, #systemTypeFilter').on('change', function () {
            if (self.dataTable) {
                self.dataTable.ajax.reload();
            }
        });
    },

    // Initialize tooltips
    initTooltips: function () {
        $('[data-bs-toggle="tooltip"]').tooltip('dispose').tooltip();
    },

    // Update bulk actions visibility
    updateBulkActionsVisibility: function () {
        const checkedCount = $('.permission-checkbox:checked').length;

        if (checkedCount > 0) {
            $('#bulkActions').removeClass('d-none');
            $('#selectedCount').text(checkedCount);
        } else {
            $('#bulkActions').addClass('d-none');
        }
    },

    // Delete permission
    deletePermission: function (permissionId, permissionName) {
        const self = this;

        Swal.fire({
            title: 'Delete Permission',
            text: `Are you sure you want to delete "${permissionName}"? This action cannot be undone.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                self.performDelete(permissionId);
            }
        });
    },

    // Perform delete operation
    performDelete: function (permissionId) {
        const self = this;

        $.ajax({
            url: self.config.deleteUrl,
            type: 'POST',
            data: { id: permissionId },
            beforeSend: function () {
                Swal.fire({
                    title: 'Deleting...',
                    allowOutsideClick: false,
                    didOpen: () => {
                        Swal.showLoading();
                    }
                });
            },
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        title: 'Deleted!',
                        text: 'Permission has been deleted successfully.',
                        icon: 'success',
                        timer: 2000,
                        showConfirmButton: false
                    });

                    // Reload table
                    if (self.dataTable) {
                        self.dataTable.ajax.reload();
                    }
                } else {
                    Swal.fire({
                        title: 'Error!',
                        text: response.message || 'Failed to delete permission.',
                        icon: 'error'
                    });
                }
            },
            error: function (xhr, status, error) {
                Swal.fire({
                    title: 'Error!',
                    text: 'An error occurred while deleting the permission.',
                    icon: 'error'
                });
            }
        });
    },

    // Toggle permission status
    togglePermissionStatus: function (permissionId, newStatus) {
        const self = this;

        $.ajax({
            url: self.config.toggleStatusUrl,
            type: 'POST',
            data: {
                id: permissionId,
                isActive: newStatus
            },
            beforeSend: function () {
                // Show loading state on button
                const button = $(`.toggle-status[data-permission-id="${permissionId}"]`);
                button.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i>');
            },
            success: function (response) {
                if (response.success) {
                    // Show success message
                    const statusText = newStatus ? 'activated' : 'deactivated';

                    // Use toast notification for quick feedback
                    alert.success(`Permission ${statusText} successfully!`, { popup: false });

                    // Reload table
                    if (self.dataTable) {
                        self.dataTable.ajax.reload();
                    }
                } else {
                    alert.error(response.message || 'Failed to update permission status.');
                }
            },
            error: function (xhr, status, error) {
                alert.error('An error occurred while updating the permission status.');
            },
            complete: function () {
                // Re-enable button
                const button = $(`.toggle-status[data-permission-id="${permissionId}"]`);
                button.prop('disabled', false);
            }
        });
    },

    // Bulk actions
    bulkAction: function (action) {
        const self = this;
        const selectedIds = $('.permission-checkbox:checked').map(function () {
            return $(this).val();
        }).get();

        if (selectedIds.length === 0) {
            alert.warning('Please select at least one permission.');
            return;
        }

        let title, text, confirmButtonText;

        switch (action) {
            case 'activate':
                title = 'Activate Permissions';
                text = `Are you sure you want to activate ${selectedIds.length} selected permissions?`;
                confirmButtonText = 'Yes, activate them!';
                break;
            case 'deactivate':
                title = 'Deactivate Permissions';
                text = `Are you sure you want to deactivate ${selectedIds.length} selected permissions?`;
                confirmButtonText = 'Yes, deactivate them!';
                break;
            case 'delete':
                title = 'Delete Permissions';
                text = `Are you sure you want to delete ${selectedIds.length} selected permissions? This action cannot be undone.`;
                confirmButtonText = 'Yes, delete them!';
                break;
            default:
                return;
        }

        Swal.fire({
            title: title,
            text: text,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: action === 'delete' ? '#d33' : '#3085d6',
            cancelButtonColor: '#6c757d',
            confirmButtonText: confirmButtonText,
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                self.performBulkAction(action, selectedIds);
            }
        });
    },

    // Perform bulk action
    performBulkAction: function (action, selectedIds) {
        const self = this;

        $.ajax({
            url: self.config.bulkActionUrl,
            type: 'POST',
            data: {
                action: action,
                permissionIds: selectedIds
            },
            beforeSend: function () {
                Swal.fire({
                    title: 'Processing...',
                    allowOutsideClick: false,
                    didOpen: () => {
                        Swal.showLoading();
                    }
                });
            },
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        title: 'Success!',
                        text: response.message,
                        icon: 'success',
                        timer: 2000,
                        showConfirmButton: false
                    });

                    // Clear selections
                    $('#selectAll').prop('checked', false);
                    $('.permission-checkbox').prop('checked', false);
                    self.updateBulkActionsVisibility();

                    // Reload table
                    if (self.dataTable) {
                        self.dataTable.ajax.reload();
                    }
                } else {
                    Swal.fire({
                        title: 'Error!',
                        text: response.message || 'Failed to perform bulk action.',
                        icon: 'error'
                    });
                }
            },
            error: function (xhr, status, error) {
                Swal.fire({
                    title: 'Error!',
                    text: 'An error occurred while performing the bulk action.',
                    icon: 'error'
                });
            }
        });
    },

    // Utility function to reload table
    reloadTable: function () {
        if (this.dataTable) {
            this.dataTable.ajax.reload();
        }
    }
};