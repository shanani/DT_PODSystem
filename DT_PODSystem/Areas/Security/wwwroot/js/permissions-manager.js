/**
* Permission Manager JavaScript - Tree-based with Child Count Icons
* Enhanced with Bootstrap Select Picker for hierarchical dropdown
* FIXED: Edit permission data loading and parent selection issues
*/

$(document).ready(function () {
    'use strict';

    // Initialize Permission Manager
    if (window.permissionManagerConfig) {
        window.permissionManager = new PermissionManager(window.permissionManagerConfig);
    } else {
        console.error('permissionManagerConfig not found');
    }
});

/**
* Main Permission Manager Class
*/
class PermissionManager {
    constructor(options = {}) {
        this.config = {
            baseUrl: options.baseUrl || '/Security/Permissions',
            antiForgeryToken: options.antiForgeryToken || this.getAntiForgeryToken(),
            ...options
        };

        this.state = {
            selectedNode: null,
            isLoading: false
        };

        this.ui = new PermissionManagerUI(this);
        this.tree = new PermissionManagerTree(this);
        this.api = new PermissionManagerAPI(this);

        this.init();
    }

    async init() {
        try {
            console.log('Initializing Permission Manager...');

            await this.ui.init();
            await this.tree.init();
            await this.loadData();
            this.setupUnifiedModalHandlers();

            console.log('Permission Manager initialized successfully');
        } catch (error) {
            console.error('Failed to initialize Permission Manager:', error);
            this.ui.showError('Failed to initialize Permission Manager');
        }
    }

    setupUnifiedModalHandlers() {
        const self = this;



        // Permission Type Modal handlers
        $('#savePermissionTypeBtn').off('click').on('click', function () {
            self.saveOrUpdatePermissionType();
        });

        $('#permissionTypeModal').on('hidden.bs.modal', function () {
            self.resetPermissionTypeForm();
        });

        // Permission Modal handlers
        $('#savePermissionBtn').off('click').on('click', function () {
            self.saveOrUpdatePermission();
        });

        $('#permissionModal').on('hidden.bs.modal', function () {
            self.resetPermissionForm();
        });

        // 🆕 SIMPLIFIED: Populate parent dropdown when modal shows
        $('#permissionModal').on('show.bs.modal', function () {
            console.log('🔄 Permission modal showing...');
            console.log('📋 Current state before population:', {
                currentPermissionTypeId: self.state.currentPermissionTypeId,
                selectedParentId: self.state.selectedParentId,
                excludePermissionId: self.state.excludePermissionId
            });

            // Initialize permission type dropdown when modal shows
            setTimeout(async () => {
                console.log('🔄 Initializing dropdown and handlers in modal...');
                await self.initializePermissionTypeDropdown();
                self.handlePermissionTypeChange();

                // 🚨 FIX: Set the pre-selected permission type after dropdown is initialized
                if (self.state.currentPermissionTypeId) {
                    console.log('🔄 Setting pre-selected permission type:', self.state.currentPermissionTypeId);
                    console.log('🔧 State check:', {
                        needsServerData: self.state.needsServerData,
                        excludePermissionId: self.state.excludePermissionId,
                        selectedParentId: self.state.selectedParentId
                    });

                    $('#permissionTypeSelect').val(self.state.currentPermissionTypeId);
                    $('#permissionTypeId').val(self.state.currentPermissionTypeId);

                    // Show parent group first
                    $('#parentPermissionGroup').show();

                    // 🚨 ALWAYS fetch server data for edit mode to get correct parent
                    if (self.state.excludePermissionId) {
                        console.log('🔄 Fetching server data for accurate parent information...');
                        console.log('🔧 Fetching for permission ID:', self.state.excludePermissionId);

                        try {
                            const permissionDetails = await self.api.getPermissionDetails(self.state.excludePermissionId);
                            console.log('🔧 Server response:', permissionDetails);

                            if (permissionDetails && permissionDetails.success) {
                                const permission = permissionDetails.data;
                                console.log('✅ Got server data:', permission);

                                // Update state with server data
                                self.state.selectedParentId = permission.parentPermissionId;
                                self.state.currentPermissionTypeId = permission.permissionTypeId;

                                console.log('✅ Updated state with server data:', {
                                    permissionTypeId: permission.permissionTypeId,
                                    parentPermissionId: permission.parentPermissionId,
                                    originalType: self.state.currentPermissionTypeId
                                });

                                // Update form fields with server data
                                $('#permissionName').val(permission.name || '');
                                $('#permissionDescription').val(permission.description || '');
                                $('#permissionScope').val(permission.scope || 'Global');
                                $('#permissionAction').val(permission.action || 'Read');

                                // Update type dropdown if different
                                if (permission.permissionTypeId !== parseInt($('#permissionTypeSelect').val())) {
                                    console.log('🔄 Updating permission type from', $('#permissionTypeSelect').val(), 'to', permission.permissionTypeId);
                                    $('#permissionTypeSelect').val(permission.permissionTypeId);
                                    $('#permissionTypeId').val(permission.permissionTypeId);
                                    self.state.currentPermissionTypeId = permission.permissionTypeId;
                                }
                            } else {
                                console.error('❌ Failed to get server data:', permissionDetails);
                            }
                        } catch (error) {
                            console.error('❌ Error fetching server data:', error);
                        }
                    }

                    // Populate parent dropdown and THEN set the selected parent
                    console.log('🔄 Populating parent dropdown for type:', self.state.currentPermissionTypeId);
                    await self.populateParentDropdownOnShow();

                    // After parent dropdown is populated, set the selected parent
                    if (self.state.selectedParentId) {
                        console.log('🔄 Setting selected parent ID:', self.state.selectedParentId);
                        $('#parentPermissionSelect').val(self.state.selectedParentId.toString());

                        // Verify it was set
                        const actualValue = $('#parentPermissionSelect').val();
                        console.log('🔧 Parent dropdown value after setting:', actualValue);

                        if (actualValue === self.state.selectedParentId.toString()) {
                            console.log('✅ Selected parent set successfully');
                            // Trigger hierarchy preview update
                            self.updateHierarchyPreview();
                        } else {
                            console.warn('⚠️ Failed to set parent - value mismatch');
                        }
                    } else {
                        console.log('ℹ️ No parent to select (root permission)');
                    }
                }

                // 🆕 ADD: Button to fetch latest server data if needed
                if (self.state.excludePermissionId) { // Edit mode
                    const fetchButton = $(`
                       <button type="button" class="btn btn-sm btn-outline-info mb-3" id="fetchLatestDataBtn">
                           <i class="fas fa-sync"></i> Fetch Latest Data from Server
                       </button>
                   `);

                    $('#permission-form').prepend(fetchButton);

                    $('#fetchLatestDataBtn').on('click', async function () {
                        console.log('🔄 Fetching latest data from server...');
                        $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Loading...');

                        try {
                            const permissionDetails = await self.api.getPermissionDetails(self.state.excludePermissionId);
                            if (permissionDetails && permissionDetails.success) {
                                const permission = permissionDetails.data;
                                console.log('✅ Got latest server data:', permission);

                                // Update form with server data
                                $('#permissionName').val(permission.name || '');
                                $('#permissionDescription').val(permission.description || '');
                                $('#permissionScope').val(permission.scope || 'Global');
                                $('#permissionAction').val(permission.action || 'Read');

                                // Update type and parent if they changed
                                if (permission.permissionTypeId !== self.state.currentPermissionTypeId) {
                                    $('#permissionTypeSelect').val(permission.permissionTypeId);
                                    $('#permissionTypeSelect').trigger('change');
                                }

                                self.ui.showSuccess('Latest data loaded from server');
                            }
                        } catch (error) {
                            console.error('Error fetching latest data:', error);
                            self.ui.showError('Failed to fetch latest data');
                        } finally {
                            $(this).prop('disabled', false).html('<i class="fas fa-sync"></i> Fetch Latest Data from Server');
                        }
                    });
                }
            }, 100);
        });

        // 🆕 Parent Permission dropdown change handler (simplified)
        $('#parentPermissionSelect').off('change').on('change', function () {
            const selectedValue = $(this).val();
            console.log('🔄 Parent permission changed to:', selectedValue);
            self.updateHierarchyPreview();
        });

        // 🆕 Permission name input handler
        $('#permissionName').off('input').on('input', function () {
            if ($('#parentPermissionSelect').val()) {
                self.updateHierarchyPreview();
            }
        });

        $('#clear-search-btn').on('click', (e) => {
            e.preventDefault();
            $('#permission-search').val('');
            self.tree.treeInstance?.clear_search();
            localStorage.removeItem('permission_search_term');
            $('#clear-search-btn').addClass('hidden');
        });


        // 🔧 FIX: Search functionality with proper state management
        $('#permission-search').on('input', (e) => {
            const searchValue = $(e.target).val();

            if (searchValue.trim() === '') {
                $('#clear-search-btn').addClass('hidden');
            } else {
                $('#clear-search-btn').removeClass('hidden');
            }

            // Save search term to localStorage
            if (searchValue.trim() === '') {
                self.tree.treeInstance?.clear_search();
                localStorage.removeItem('permission_search_term');
            } else {
                self.tree.treeInstance?.search(searchValue);
                localStorage.setItem('permission_search_term', searchValue);
            }
        });

        // Alternative: If you want to use ID selectors instead
        $('#expandAllBtn').off('click').on('click', function (e) {
            e.preventDefault();
            self.expandAllPermissions();
        });

        $('#collapseAllBtn').off('click').on('click', function (e) {
            e.preventDefault();
            self.collapseAllPermissions();
        });


        // Initialize permission type dropdown on modal setup
        this.initializePermissionTypeDropdown();
    }

    /**
 * Expand all nodes in the permission tree
 */
    expandAllPermissions() {
        try {
            if (this.tree.treeInstance) {
                
                this.tree.treeInstance.open_all();

                // Update child count icons after expansion
                setTimeout(() => {
                    this.tree.applyChildCountIcons();
                }, 200);

                
            } else {
                console.warn('⚠️ Tree instance not available');
            }
        } catch (error) {
            
        }
    }

    /**
     * Collapse all nodes in the permission tree
     */
    collapseAllPermissions() {
        try {
            if (this.tree.treeInstance) {
                console.log('🔄 Collapsing all permissions...');
                this.tree.treeInstance.close_all();

                // Update child count icons after collapse
                setTimeout(() => {
                    this.tree.applyChildCountIcons();
                }, 200);

               
            } else {
                console.warn('⚠️ Tree instance not available');
            }
        } catch (error) {
            
        }
    }


    async loadData() {
        try {
            this.state.isLoading = true;
            this.ui.showLoadingState();

            const data = await this.api.getTreeData();

            if (data.success && data.data && data.data.length > 0) {
                // 🆕 Modify tree data to include IDs and child counts
                this.addIdsToTreeData(data.data);
                this.addChildCountsToTreeData(data.data);

                await this.tree.render(data.data);
                this.ui.showTreeView();

                // 🆕 Apply child count icons after tree renders
                this.tree.applyChildCountIcons();

                this.updateStatistics(data.statistics || {});
            } else {
                this.ui.showEmptyState();
            }
        } catch (error) {
            console.error('Error loading data:', error);
            this.ui.showError('Failed to load permission data');
            this.ui.showEmptyState();
        } finally {
            this.state.isLoading = false;
            this.ui.hideLoadingState();
        }
    }

    // 🆕 METHOD: Add child counts to tree data
    addChildCountsToTreeData(nodes) {
        nodes.forEach(node => {
            // Count direct children
            const childCount = (node.children && node.children.length) || 0;

            // Store child count in node data
            if (!node.data) node.data = {};
            node.data.childCount = childCount;

            // Add custom attributes for styling
            if (!node.li_attr) node.li_attr = {};
            node.li_attr['data-child-count'] = childCount;
            node.li_attr['data-has-children'] = childCount > 0 ? 'true' : 'false';

            // Recursively process children
            if (node.children && node.children.length > 0) {
                this.addChildCountsToTreeData(node.children);
            }
        });
    }

    // 🆕 METHOD: Add IDs to tree node display names
    addIdsToTreeData(nodes) {
        nodes.forEach(node => {
            if (node.data && node.data.id) {
                // Store original text for reference
                node.originalText = node.text;

                // Add ID to display text
                if (node.id.startsWith('type_')) {
                    // Permission Type nodes
                    node.text = `${node.text} <small class="text-muted">(ID: ${node.data.id})</small>`;
                } else if (node.id.startsWith('perm_')) {
                    // Permission nodes
                    node.text = `${node.text} <small class="text-muted">(ID: ${node.data.id})</small>`;
                }
            }

            // Recursively process children
            if (node.children && node.children.length > 0) {
                this.addIdsToTreeData(node.children);
            }
        });
    }

    async refreshData() {
        await this.loadData();
    }

    updateStatistics(stats) {
        $('#total-types').text(stats.totalTypes || 0);
        $('#total-permissions').text(stats.totalPermissions || 0);
        $('#active-permissions').text(stats.activePermissions || 0);
        $('#system-permissions').text(stats.systemPermissions || 0);
    }

    // 🆕 UNIFIED: Show Create Permission Type Modal
    showCreatePermissionTypeModal() {
        this.resetPermissionTypeForm();
        $('#permission-type-modal-title').text('Create Permission Type');
        $('#save-type-btn-text').text('Save Permission Type');
        $('#savePermissionTypeBtn').removeClass('btn-warning').addClass('btn-primary');
        $('#permissionTypeModal').modal('show');
    }

    // 🆕 UNIFIED: Show Edit Permission Type Modal
    async showEditPermissionTypeModal(nodeId) {
        const typeId = nodeId.replace('type_', '');

        // Get data from tree node
        const node = this.tree.getNode(nodeId);
        const nodeData = node.original?.data || {};

        // Reset and setup form for edit mode
        this.resetPermissionTypeForm();
        $('#typeId').val(typeId);
        $('#typeName').val(nodeData.name || node.originalText || node.text.replace(/<[^>]*>/g, '').split('(ID:')[0].trim());
        $('#typeDescription').val(nodeData.description || '');
        $('#typeIcon').val(nodeData.icon || 'fas fa-folder');
        $('#typeColor').val(nodeData.color || 'primary');

        // Update modal UI for edit mode
        $('#permission-type-modal-title').text('Edit Permission Type');
        $('#save-type-btn-text').text('Update Permission Type');
        $('#savePermissionTypeBtn').removeClass('btn-primary').addClass('btn-warning');

        $('#permissionTypeModal').modal('show');
    }

    // 🆕 SIMPLIFIED: Show Create Permission Modal
    async showCreatePermissionModal(nodeId) {
        console.log('Creating permission for nodeId:', nodeId);

        let permissionTypeId;
        let parentPermissionId = null;

        if (nodeId.startsWith('type_')) {
            // Creating permission directly under a permission type
            permissionTypeId = nodeId.replace('type_', '');
            console.log('Creating root permission under type:', permissionTypeId);
        } else if (nodeId.startsWith('perm_')) {
            // Creating permission under another permission (hierarchical)
            parentPermissionId = parseInt(nodeId.replace('perm_', ''));
            console.log('Creating child permission under parent:', parentPermissionId);

            // Get the permission type by traversing up the tree
            let currentNode = this.tree.getNode(nodeId);
            while (currentNode && currentNode.parent !== '#') {
                const parentNode = this.tree.getNode(currentNode.parent);
                if (parentNode && parentNode.id.startsWith('type_')) {
                    permissionTypeId = parentNode.id.replace('type_', '');
                    break;
                }
                currentNode = parentNode;
            }

            // If still not found, try to get from node data
            if (!permissionTypeId) {
                const nodeData = currentNode?.original?.data || {};
                permissionTypeId = nodeData.permissionTypeId;
            }
        }

        if (!permissionTypeId) {
            this.ui.showError('Could not determine permission type. Please try again.');
            return;
        }

        // Reset form and set basic values
        this.resetPermissionForm();

        // Store values in state first
        this.state.selectedParentId = parentPermissionId;
        this.state.currentPermissionTypeId = parseInt(permissionTypeId);
        this.state.excludePermissionId = null;

        console.log('🔄 Create permission state set:', {
            permissionTypeId: permissionTypeId,
            parentPermissionId: parentPermissionId,
            currentPermissionTypeId: this.state.currentPermissionTypeId
        });

        // Set modal title
        if (parentPermissionId) {
            $('#permission-modal-title').text('Create Child Permission');
        } else {
            $('#permission-modal-title').text('Create Permission');
        }

        $('#save-btn-text').text('Save Permission');
        $('#savePermissionBtn').removeClass('btn-warning').addClass('btn-primary');

        // Show modal (parent dropdown will be populated by the modal show event)
        $('#permissionModal').modal('show');
    }

    // 🆕 COMPLETELY REWRITTEN: Show Edit Permission Modal
    async showEditPermissionModal(nodeId) {
        const permissionId = nodeId.replace('perm_', '');

        try {
            console.log('🔧 Editing permission ID:', permissionId);

            // 🚨 FIX: Get data from tree node first (don't call server immediately)
            const node = this.tree.getNode(nodeId);
            const nodeData = node.original?.data || {};

            console.log('🔧 Using tree node data for initial load:', nodeData);

            // Find permission type and parent by traversing the tree structure
            let permissionTypeId = nodeData.permissionTypeId;
            let parentPermissionId = nodeData.parentPermissionId;

            console.log('🔍 Initial values from nodeData:', {
                permissionTypeId: permissionTypeId,
                parentPermissionId: parentPermissionId,
                nodeParent: node.parent,
                nodeParentType: typeof node.parent
            });

            // Get parent permission ID directly from tree structure
            if (node.parent && node.parent !== '#' && node.parent.startsWith('perm_')) {
                parentPermissionId = parseInt(node.parent.replace('perm_', ''));
                console.log('🔍 Found parent permission ID from tree parent:', parentPermissionId);
            } else if (node.parent && node.parent !== '#') {
                console.log('🔍 Parent is not a permission, checking if it\'s a type:', node.parent);
                // Parent is the permission type itself (root permission)
                parentPermissionId = null;
            }

            // Find permission type by traversing up the tree
            if (!permissionTypeId) {
                let currentNode = node;
                while (currentNode && currentNode.parent !== '#') {
                    const parentNode = this.tree.getNode(currentNode.parent);
                    console.log('🔍 Checking parent node:', parentNode?.id);

                    if (parentNode && parentNode.id.startsWith('type_')) {
                        permissionTypeId = parseInt(parentNode.id.replace('type_', ''));
                        console.log('🔍 Found permission type from tree traversal:', permissionTypeId);
                        break;
                    }
                    currentNode = parentNode;
                }
            }

            if (!permissionTypeId) {
                this.ui.showError('Could not determine permission type from tree structure.');
                return;
            }

            console.log('🔧 Tree analysis results:', {
                permissionTypeId: permissionTypeId,
                parentPermissionId: parentPermissionId,
                nodeData: nodeData,
                nodeParent: node.parent
            });

            // Reset and setup form for edit mode
            this.resetPermissionForm();
            $('#permissionId').val(permissionId);

            // Store values for modal show event (use tree data initially)
            this.state.selectedParentId = nodeData.parentPermissionId;
            this.state.currentPermissionTypeId = parseInt(permissionTypeId);
            this.state.excludePermissionId = parseInt(permissionId);

            // Fill form fields with tree node data initially
            const cleanText = node.originalText || node.text.replace(/<[^>]*>/g, '').split('(ID:')[0].trim();
            $('#permissionName').val(nodeData.name || cleanText);
            $('#permissionDescription').val(nodeData.description || '');
            $('#permissionScope').val(nodeData.scope || 'Global');
            $('#permissionAction').val(nodeData.action || 'Read');

            console.log('✅ Form populated with tree data:', {
                name: nodeData.name || cleanText,
                permissionTypeId: permissionTypeId,
                parentPermissionId: nodeData.parentPermissionId
            });

            // Update modal UI for edit mode
            $('#permission-modal-title').text('Edit Permission');
            $('#save-btn-text').text('Update Permission');
            $('#savePermissionBtn').removeClass('btn-primary').addClass('btn-warning');

            // Show modal (dropdowns will be populated by the modal show event)
            $('#permissionModal').modal('show');

        } catch (error) {
            console.error('❌ Error setting up edit permission modal:', error);
            this.ui.showError('Failed to load permission for editing');
        }
    }

    // 🆕 UNIFIED: Save or Update Permission Type
    async saveOrUpdatePermissionType() {
        try {
            const isEdit = $('#typeId').val() !== '';

            const formData = {
                Name: $('#typeName').val().trim(),
                Description: $('#typeDescription').val().trim(),
                Icon: $('#typeIcon').val().trim(),
                Color: $('#typeColor').val()
            };

            // Add ID for edit mode
            if (isEdit) {
                formData.Id = parseInt($('#typeId').val());
            }

            // Validation
            if (!formData.Name) {
                this.ui.showError('Name is required');
                return;
            }

            // Call appropriate API method
            const result = isEdit
                ? await this.api.updatePermissionType(formData)
                : await this.api.createPermissionType(formData);

            if (result.success) {
                $('#permissionTypeModal').modal('hide');
                this.ui.showSuccess(`Permission type ${isEdit ? 'updated' : 'created'} successfully`);
                await this.refreshData();
            } else {
                this.ui.showError(result.message || `Failed to ${isEdit ? 'update' : 'create'} permission type`);
            }
        } catch (error) {
            console.error('Error saving permission type:', error);
            this.ui.showError('Failed to save permission type');
        }
    }

    // 🆕 FIXED: Save or Update Permission
    async saveOrUpdatePermission() {
        try {
            const isEdit = $('#permissionId').val() !== '';

            // 🚨 FOR EDIT: Optionally fetch latest data from server before saving
            if (isEdit && this.state.fetchLatestDataOnSave) {
                const permissionId = $('#permissionId').val();
                console.log('🔄 Fetching latest server data before saving...');

                const permissionDetails = await this.api.getPermissionDetails(permissionId);
                if (permissionDetails && permissionDetails.success) {
                    console.log('✅ Got latest server data:', permissionDetails.data);
                    // You could update certain fields here if needed
                }
            }

            // Get PermissionTypeId from the visible dropdown, not hidden field
            const permissionTypeIdValue = $('#permissionTypeSelect').val();
            const permissionTypeId = permissionTypeIdValue ? parseInt(permissionTypeIdValue) : null;

            // Get Parent Permission ID from dropdown with detailed logging
            const parentPermissionIdValue = $('#parentPermissionSelect').val();
            console.log('🔍 Parent dropdown analysis:', {
                rawValue: parentPermissionIdValue,
                valueType: typeof parentPermissionIdValue,
                isEmpty: parentPermissionIdValue === '',
                isNull: parentPermissionIdValue === null,
                isUndefined: parentPermissionIdValue === undefined,
                selectedText: $('#parentPermissionSelect option:selected').text()
            });

            const parentPermissionId = parentPermissionIdValue && parentPermissionIdValue !== '' && parentPermissionIdValue !== 'null'
                ? parseInt(parentPermissionIdValue)
                : null;

            console.log('🔧 Form values (FIXED):', {
                permissionTypeIdValue: permissionTypeIdValue,
                permissionTypeId: permissionTypeId,
                parentPermissionIdValue: parentPermissionIdValue,
                parentPermissionId: parentPermissionId,
                name: $('#permissionName').val(),
                isEdit: isEdit,
                description: $('#permissionDescription').val(),
                scope: $('#permissionScope').val(),
                action: $('#permissionAction').val()
            });

            // 🚨 FIX: Enhanced validation for permission type ID
            if (!permissionTypeId || permissionTypeId <= 0 || isNaN(permissionTypeId)) {
                console.error('❌ Invalid permission type ID:', {
                    value: permissionTypeIdValue,
                    parsed: permissionTypeId,
                    isNaN: isNaN(permissionTypeId)
                });
                this.ui.showError('Please select a valid permission type. If this error persists, please refresh the page.');
                return;
            }

            const formData = {
                Name: $('#permissionName').val().trim(),
                Description: $('#permissionDescription').val().trim(),
                PermissionTypeId: permissionTypeId,
                ParentPermissionId: parentPermissionId,
                Scope: $('#permissionScope').val() || 'Global',
                Action: $('#permissionAction').val() || 'Read',
                Icon: 'fas fa-key',
                Color: 'primary',
                CanHaveChildren: true
            };

            // Add ID for edit mode
            if (isEdit) {
                formData.Id = parseInt($('#permissionId').val());
            }

            // Validation
            if (!formData.Name) {
                this.ui.showError('Name is required');
                return;
            }

            console.log('🚀 Sending permission data (FIXED):', formData);

            // Call appropriate API method
            const result = isEdit
                ? await this.api.updatePermission(formData)
                : await this.api.createPermission(formData);

            if (result.success) {
                $('#permissionModal').modal('hide');
                this.ui.showSuccess(`Permission ${isEdit ? 'updated' : 'created'} successfully`);

                // 🚨 FIX: Only refresh tree after successful save
                await this.refreshData();
            } else {
                this.ui.showError(result.message || `Failed to ${isEdit ? 'update' : 'create'} permission`);
            }
        } catch (error) {
            console.error('Error saving permission:', error);
            this.ui.showError('Failed to save permission');
        }
    }

    // 🆕 METHOD: Initialize Permission Type Dropdown
    async initializePermissionTypeDropdown() {
        try {
            console.log('🔄 Initializing permission type dropdown...');

            const permissionTypes = await this.api.getPermissionTypes();
            console.log('📋 Received permission types:', permissionTypes);

            const $dropdown = $('#permissionTypeSelect');
            console.log('📋 Found dropdown element:', $dropdown.length > 0 ? 'YES' : 'NO');

            $dropdown.empty();
            $dropdown.append('<option value="">Select Permission Type</option>');

            if (permissionTypes && permissionTypes.length > 0) {
                permissionTypes.forEach(type => {
                    const $option = $(`<option value="${type.id}">${type.name}</option>`);
                    $dropdown.append($option);
                    console.log('✅ Added permission type option:', type.name, 'ID:', type.id);
                });
            } else {
                console.warn('⚠️ No permission types received from API');
                $dropdown.append('<option value="" disabled>No permission types available</option>');
            }

            console.log('✅ Permission type dropdown initialized with', permissionTypes?.length || 0, 'types');
        } catch (error) {
            console.error('❌ Error initializing permission type dropdown:', error);
            const $dropdown = $('#permissionTypeSelect');
            $dropdown.empty();
            $dropdown.append('<option value="">Error loading types</option>');
        }
    }

    // 🆕 METHOD: Handle Permission Type Change
    handlePermissionTypeChange() {
        const self = this;

        $('#permissionTypeSelect').off('change').on('change', function () {
            const newPermissionTypeId = $(this).val();
            console.log('🔄 Permission type changed to:', newPermissionTypeId);

            if (newPermissionTypeId) {
                self.state.currentPermissionTypeId = parseInt(newPermissionTypeId);
                // Update hidden field to match the dropdown
                $('#permissionTypeId').val(newPermissionTypeId);

                // Show parent dropdown and populate it with permissions from the selected type
                $('#parentPermissionGroup').show();
                console.log('🔄 Updating parent dropdown for new type:', newPermissionTypeId);
                self.populateParentDropdownOnShow();
            } else {
                // Hide parent dropdown if no type selected
                $('#parentPermissionGroup').hide();
                self.state.currentPermissionTypeId = null;
                $('#permissionTypeId').val('');
                console.log('❌ No type selected, hiding parent dropdown');
            }
        });
    }
    updateHierarchyPreview() {
        const selectedText = $('#parentPermissionSelect option:selected').text();
        const permissionName = $('#permissionName').val() || '[New Permission]';

        if ($('#parentPermissionSelect').val()) {
            const hierarchy = selectedText.replace(/\(ID: \d+\)/, '').trim() + ' → ' + permissionName;
            $('#hierarchyPath').text(hierarchy);
            $('#hierarchyPreview').show();
        } else {
            $('#hierarchyPreview').hide();
        }
    }

    // 🆕 FIXED: Populate parent dropdown when modal shows
    async populateParentDropdownOnShow() {
        console.log('🔄 populateParentDropdownOnShow called');
        console.log('📋 Current state:', {
            currentPermissionTypeId: this.state.currentPermissionTypeId,
            selectedParentId: this.state.selectedParentId,
            excludePermissionId: this.state.excludePermissionId
        });

        // 🚨 FIX: Better validation for currentPermissionTypeId
        if (!this.state.currentPermissionTypeId || isNaN(this.state.currentPermissionTypeId)) {
            console.log('❌ No valid permission type ID available:', this.state.currentPermissionTypeId);

            // Try to get from form dropdown as fallback
            const formPermissionTypeId = $('#permissionTypeSelect').val();
            if (formPermissionTypeId && !isNaN(parseInt(formPermissionTypeId))) {
                this.state.currentPermissionTypeId = parseInt(formPermissionTypeId);
                console.log('✅ Recovered permission type ID from dropdown:', this.state.currentPermissionTypeId);
            } else {
                console.log('❌ Cannot populate parent dropdown - no permission type ID available');
                $('#parentPermissionGroup').hide();
                return;
            }
        }

        try {
            console.log('🔄 Populating parent dropdown for type:', this.state.currentPermissionTypeId);

            // 🚨 VALIDATION: Ensure we have a valid permission type ID
            if (!this.state.currentPermissionTypeId || this.state.currentPermissionTypeId <= 0) {
                console.error('❌ Invalid permission type ID for API call:', this.state.currentPermissionTypeId);
                const $dropdown = $('#parentPermissionSelect');
                $dropdown.empty();
                $dropdown.append('<option value="">No valid permission type selected</option>');
                $('#parentPermissionGroup').show();
                return;
            }

            // Clear the dropdown first
            const $dropdown = $('#parentPermissionSelect');
            $dropdown.empty();
            $dropdown.append('<option value="">Loading permissions...</option>');

            // Get available parent permissions for this specific type
            const parentPermissions = await this.api.getParentPermissions(
                this.state.currentPermissionTypeId,
                this.state.excludePermissionId
            );

            console.log('📋 Received parent permissions:', parentPermissions);

            // Clear and populate dropdown
            $dropdown.empty();
            $dropdown.append('<option value="">None (Root Permission)</option>');

            // Add available parent permissions with hierarchical display
            if (parentPermissions && parentPermissions.length > 0) {
                parentPermissions.forEach(permission => {
                    const level = permission.level || 0;
                    const indentation = '└─'.repeat(level);
                    const displayText = `${indentation} ${permission.name} (ID: ${permission.id})`;

                    const $option = $(`<option value="${permission.id}">${displayText}</option>`);

                    // Disable circular references
                    if (permission.id === this.state.excludePermissionId) {
                        $option.prop('disabled', true);
                        $option.text($option.text() + ' [Current Permission]');
                    }

                    $dropdown.append($option);
                });
            } else {
                // Add message if no permissions available
                $dropdown.append('<option value="" disabled>No permissions available for this type</option>');
            }

            // 🚨 FIX: Set selected value if exists and verify it's in the list
            if (this.state.selectedParentId) {
                const selectedValue = this.state.selectedParentId.toString();

                // Check if the option exists in the dropdown
                const $targetOption = $dropdown.find(`option[value="${selectedValue}"]`);
                if ($targetOption.length > 0) {
                    $dropdown.val(selectedValue);
                    console.log('✅ Set parent dropdown to:', this.state.selectedParentId);

                    // Trigger hierarchy preview update
                    this.updateHierarchyPreview();
                } else {
                    console.warn('⚠️ Parent permission not found in dropdown options:', this.state.selectedParentId);
                    console.log('📋 Available options:');
                    $dropdown.find('option').each(function () {
                        console.log(`   - Value: "${$(this).val()}", Text: "${$(this).text()}"`);
                    });
                }
            } else {
                console.log('ℹ️ No parent selected (root permission)');
            }

            // Show the parent permission field
            $('#parentPermissionGroup').show();

            console.log('✅ Parent dropdown populated successfully. Total options:', $dropdown.find('option').length);

        } catch (error) {
            console.error('❌ Error populating parent dropdown:', error);
            // Fallback
            const $dropdown = $('#parentPermissionSelect');
            $dropdown.empty().append('<option value="">None (Root Permission)</option>');
            $('#parentPermissionGroup').show();
        }
    }

    // 🔧 HELPER: Reset Permission Type Form
    resetPermissionTypeForm() {
        $('#permission-type-form')[0].reset();
        $('#typeId').val('');

        // Clear validation states
        $('#permission-type-form .is-invalid').removeClass('is-invalid');
        $('#permission-type-form .invalid-feedback').remove();
    }

    // 🔧 HELPER: Reset Permission Form
    resetPermissionForm() {
        // Clean reset - no select picker to destroy
        $('#permission-form')[0].reset();
        $('#permissionId').val('');
        $('#permissionTypeId').val('');
        $('#permissionTypeSelect').val('');
        $('#parentPermissionSelect').val('');

        // Hide hierarchy preview and parent group
        $('#hierarchyPreview').hide();
        $('#parentPermissionGroup').hide();

        // Remove any temporary buttons (like fetch latest data)
        $('#fetchLatestDataBtn').remove();

        // Clear validation states
        $('#permission-form .is-invalid').removeClass('is-invalid');
        $('#permission-form .invalid-feedback').remove();

        // Clear state
        this.state.selectedParentId = null;
        this.state.currentPermissionTypeId = null;
        this.state.excludePermissionId = null;
    }

    // Route edit calls based on node type
    editNode(nodeId) {
        if (nodeId.startsWith('type_')) {
            this.showEditPermissionTypeModal(nodeId);
        } else if (nodeId.startsWith('perm_')) {
            this.showEditPermissionModal(nodeId);
        }
    }

    // Delete with confirmation
    async deleteNode(nodeId) {
        try {
            const node = this.tree.getNode(nodeId);
            if (!node) return;

            const isType = nodeId.startsWith('type_');
            const name = node.originalText || node.text.replace(/<[^>]*>/g, '').split('(ID:')[0].trim();

            const confirmed = await this.ui.showConfirmDialog(
                `Delete ${isType ? 'Permission Type' : 'Permission'}?`,
                `Are you sure you want to delete "${name}"? This action cannot be undone.`
            );

            if (!confirmed) return;

            const id = isType ? nodeId.replace('type_', '') : nodeId.replace('perm_', '');
            const result = isType
                ? await this.api.deletePermissionType(id)
                : await this.api.deletePermission(id);

            if (result.success) {
                this.ui.showSuccess(`${isType ? 'Permission type' : 'Permission'} deleted successfully`);
                await this.refreshData();
            } else {
                this.ui.showError(result.message || 'Failed to delete');
            }
        } catch (error) {
            console.error('Error deleting:', error);
            this.ui.showError('Failed to delete item');
        }
    }

    getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    // 🔧 DEBUG: Manual test function
    testPermissionTypeDropdown() {
        console.log('🧪 Testing permission type dropdown...');
        const $dropdown = $('#permissionTypeSelect');
        console.log('📋 Dropdown found:', $dropdown.length > 0);
        console.log('📋 Dropdown HTML:', $dropdown.html());
        console.log('📋 Current value:', $dropdown.val());

        // Test change event
        $dropdown.val('1').trigger('change');
        console.log('🧪 Triggered change event with value 1');
    }
}

/**
* UI Management
*/
class PermissionManagerUI {
    constructor(manager) {
        this.manager = manager;
    }

    async init() {
        console.log('Initializing UI...');
    }

    showLoadingState() {
        $('#permission-loading-state').show();
        $('#permission-tree').hide();
        $('#permission-empty-state').hide();
        $('#permission-search-container').hide();
    }

    hideLoadingState() {
        $('#permission-loading-state').hide();
    }

    showTreeView() {
        $('#permission-tree').show();
        $('#permission-search-container').show();
        $('#permission-empty-state').hide();
    }

    showEmptyState() {
        $('#permission-empty-state').show();
        $('#permission-tree').hide();
        $('#permission-search-container').hide();
    }

    showSuccess(message) {
        if (typeof alert !== 'undefined' && alert.success) {
            alert.success(message, { popup: false });
        } else {
            console.log('SUCCESS:', message);
        }
    }

    showError(message) {
        if (typeof alert !== 'undefined' && alert.error) {
            alert.error(message, { popup: false });
        } else {
            console.error('ERROR:', message);
        }
    }

    async showConfirmDialog(title, text) {
        if (typeof Swal !== 'undefined') {
            const result = await Swal.fire({
                title: title,
                text: text,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6',
                confirmButtonText: 'Yes, delete it!'
            });
            return result.isConfirmed;
        }
        return confirm(`${title}\n${text}`);
    }
}

/**
* Tree Management with Child Count Icons
*/
class PermissionManagerTree {
    constructor(manager) {
        this.manager = manager;
        this.treeInstance = null;
    }

    async init() {
        this.treeContainer = document.getElementById('permission-tree');
    }

    async render(data) {
        if (!this.treeContainer) return;

        if ($.jstree.reference(this.treeContainer)) {
            $(this.treeContainer).jstree('destroy');
        }

        $(this.treeContainer).jstree({
            'core': {
                'data': data,
                'check_callback': true,
                'themes': {
                    'responsive': true,
                    'variant': 'large',
                    'icons': true
                }
            },
            'search': {
                'case_insensitive': true,
                'show_only_matches': true,
                'show_only_matches_children': true,
                'search_leaves_only': false,
                'fuzzy': false
            },
            'state': {
                'key': 'permission_tree_state',
                'opened': true,
                'selected': true,
                'filter': false,  // Don't use JSTree's filter state, we'll handle search manually
                'ttl': 86400000
            },
            'contextmenu': {
                'items': {
                    'addPermission': {
                        'label': 'Add Permission',
                        'action': (obj) => {
                            const nodeId = $(obj.reference[0]).closest('li').attr('id');
                            this.manager.showCreatePermissionModal(nodeId);
                        }
                    },
                    'edit': {
                        'label': 'Edit',
                        'action': (obj) => {
                            const nodeId = $(obj.reference[0]).closest('li').attr('id');
                            this.manager.editNode(nodeId);
                        }
                    },
                    'delete': {
                        'label': 'Delete',
                        'action': (obj) => {
                            const nodeId = $(obj.reference[0]).closest('li').attr('id');
                            this.manager.deleteNode(nodeId);
                        }
                    }
                }
            },
            'plugins': ['contextmenu', 'state', 'search']
        });

        this.treeInstance = $(this.treeContainer).jstree(true);

        // Setup event handlers for expand/collapse to update child count visibility
        $(this.treeContainer).on('after_open.jstree after_close.jstree', () => {
            setTimeout(() => this.applyChildCountIcons(), 100);
        });

        // 🔧 FIX: Restore search state when tree is ready
        $(this.treeContainer).on('ready.jstree', () => {
            console.log('🔄 Tree ready, restoring search state...');

            const savedSearch = localStorage.getItem('permission_search_term');
            if (savedSearch && savedSearch.trim() !== '') {
                console.log('🔍 Restoring search term:', savedSearch);
                $('#permission-search').val(savedSearch);

                // Apply search after a short delay to ensure tree is fully rendered
                setTimeout(() => {
                    this.treeInstance.search(savedSearch);
                    console.log('✅ Search state restored successfully');
                }, 300);
            }
        });

        // 🔧 FIX: Restore search state when tree state is restored
        $(this.treeContainer).on('state_ready.jstree', () => {
            console.log('🔄 JSTree state restored, checking search...');

            // Apply child count icons after state restoration
            setTimeout(() => this.applyChildCountIcons(), 200);

            // Re-apply search if it exists
            const savedSearch = localStorage.getItem('permission_search_term');
            if (savedSearch && savedSearch.trim() !== '') {
                console.log('🔍 Re-applying search after state restore:', savedSearch);
                $('#permission-search').val(savedSearch);

                setTimeout(() => {
                    this.treeInstance.search(savedSearch);
                    console.log('✅ Search re-applied after state restore');
                }, 400);
            }
        });
    }

    // METHOD: Apply child count icons to replace jstree expand/collapse icons
    applyChildCountIcons() {
        const self = this;

        // Remove existing child count badges to avoid duplicates
        $('.child-count-badge').remove();

        // Process each tree node
        $('#permission-tree li').each(function () {
            const $li = $(this);
            const nodeId = $li.attr('id');
            const childCount = parseInt($li.attr('data-child-count')) || 0;
            const hasChildren = $li.attr('data-has-children') === 'true';

            // Find the jstree-ocl element (expand/collapse icon)
            const $ocl = $li.find('> .jstree-anchor .jstree-ocl').first();

            if ($ocl.length > 0) {
                // Hide the original expand/collapse icon
                $ocl.hide();

                // Create child count badge
                if (hasChildren && childCount > 0) {
                    const badgeClass = nodeId && nodeId.startsWith('type_') ? 'child-count-type' : 'child-count-permission';
                    const $badge = $(`
                       <span class="child-count-badge ${badgeClass}" data-count="${childCount}">
                           ${childCount}
                       </span>
                   `);

                    // Insert badge after the ocl element
                    $ocl.after($badge);

                    // Make badge clickable to toggle expand/collapse
                    $badge.on('click', function (e) {
                        e.preventDefault();
                        e.stopPropagation();

                        if (self.treeInstance.is_open(nodeId)) {
                            self.treeInstance.close_node(nodeId);
                        } else {
                            self.treeInstance.open_node(nodeId);
                        }
                    });
                } else {
                    // For leaf nodes (no children), show a small dot
                    const $emptyBadge = $(`
                       <span class="child-count-badge child-count-empty">
                           •
                       </span>
                   `);
                    $ocl.after($emptyBadge);
                }
            }
        });
    }

    getNode(nodeId) {
        return this.treeInstance ? this.treeInstance.get_node(nodeId) : null;
    }
}

/**
* API Communication
*/
class PermissionManagerAPI {
    constructor(manager) {
        this.manager = manager;
    }

    async makeRequest(method, endpoint, data = null) {
        //debugger;
        const url = `${this.manager.config.baseUrl}/${endpoint}`;

        const options = {
            method,
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        };

        // Add anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) {
            options.headers['RequestVerificationToken'] = token;
        }

        if (data && (method === 'POST' || method === 'PUT')) {
            options.body = JSON.stringify(data);
            console.log(`Making ${method} request to ${url} with data:`, data);
        }

        try {
            const response = await fetch(url, options);
            console.log(`Response status: ${response.status}`);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Response error:', errorText);
                return { success: false, message: `HTTP ${response.status}: ${errorText}` };
            }

            const result = await response.json();
            console.log('Response data:', result);
            return result;
        } catch (error) {
            console.error('Fetch error:', error);
            return { success: false, message: error.message };
        }
    }

    async getTreeData() {
        return await this.makeRequest('GET', 'GetTreeData');
    }

    // Get permission details for editing
    async getPermissionDetails(permissionId) {
        return await this.makeRequest('GET', `GetPermissionDetails/${permissionId}`);
    }

    async getPermissionTypes() {
        return await this.makeRequest('GET', 'GetPermissionTypes');
    }

    async createPermissionType(data) {
        return await this.makeRequest('POST', 'CreatePermissionType', data);
    }

    async createPermission(data) {
        return await this.makeRequest('POST', 'CreatePermission', data);
    }

    async updatePermissionType(data) {
        return await this.makeRequest('POST', 'EditPermissionType', data);
    }

    async updatePermission(data) {
        return await this.makeRequest('POST', 'EditPermission', data);
    }

    async deletePermissionType(id) {
        return await this.makeRequest('POST', 'DeletePermissionType', { Id: parseInt(id) });
    }

    async deletePermission(id) {
        return await this.makeRequest('POST', 'DeletePermission', { Id: parseInt(id) });
    }

    async getParentPermissions(permissionTypeId, excludePermissionId = null) {
        const params = new URLSearchParams();
        params.append('permissionTypeId', permissionTypeId);
        if (excludePermissionId) {
            params.append('excludePermissionId', excludePermissionId);
        }

        const url = `${this.manager.config.baseUrl}/GetParentPermissions?${params.toString()}`;

        console.log('🔄 Making API call to:', url);
        console.log('📋 Parameters:', { permissionTypeId, excludePermissionId });

        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            console.log('📡 API Response status:', response.status);

            if (!response.ok) {
                console.error('❌ Failed to get parent permissions:', response.status);
                console.log('🔄 Falling back to tree method');
                return this.getParentPermissionsFromTree(permissionTypeId, excludePermissionId);
            }

            const result = await response.json();
            console.log('✅ API Response data:', result);

            const permissions = Array.isArray(result) ? result : (result.data || []);
            console.log('📋 Processed permissions:', permissions);

            return permissions;
        } catch (error) {
            console.error('❌ Error fetching parent permissions:', error);
            console.log('🔄 Falling back to tree method');
            return this.getParentPermissionsFromTree(permissionTypeId, excludePermissionId);
        }
    }

    // Fallback method: Get parent permissions from current tree data
    getParentPermissionsFromTree(permissionTypeId, excludePermissionId = null) {
        try {
            console.log('🔍 Getting parent permissions from tree for type:', permissionTypeId, 'excluding:', excludePermissionId);

            const permissions = [];
            const typeNodeId = `type_${permissionTypeId}`;

            // Find the permission type node in the tree
            const typeNode = this.manager.tree.getNode(typeNodeId);
            console.log('🌳 Found type node:', typeNode);

            if (!typeNode) {
                console.warn('❌ Type node not found for ID:', typeNodeId);
                return permissions;
            }

            // Get the tree instance to access all nodes
            const treeInstance = this.manager.tree.treeInstance;
            if (!treeInstance) {
                console.warn('❌ Tree instance not available');
                return permissions;
            }

            // Get all nodes in the tree
            const allNodes = treeInstance.get_json('#', { flat: true });
            console.log('📊 All tree nodes:', allNodes);

            // Filter permissions for this permission type
            allNodes.forEach(node => {
                if (node.id && node.id.startsWith('perm_')) {
                    const permissionId = parseInt(node.id.replace('perm_', ''));

                    // Skip excluded permission
                    if (excludePermissionId && permissionId === parseInt(excludePermissionId)) {
                        console.log('⏭️ Skipping excluded permission:', permissionId);
                        return;
                    }

                    // Check if this permission belongs to the correct permission type
                    const nodeData = node.data || {};
                    const permissionTypeFromData = nodeData.permissionTypeId;

                    // If we have permissionTypeId in data, use it; otherwise check parent hierarchy
                    let belongsToType = false;
                    if (permissionTypeFromData) {
                        belongsToType = permissionTypeFromData == permissionTypeId;
                    } else {
                        // Check if parent is the permission type we're looking for
                        let currentNode = node;
                        while (currentNode && currentNode.parent !== '#') {
                            if (currentNode.parent === typeNodeId) {
                                belongsToType = true;
                                break;
                            }
                            // Find parent node in the flat list
                            currentNode = allNodes.find(n => n.id === currentNode.parent);
                        }
                    }

                    if (belongsToType) {
                        const cleanText = node.originalText || node.text.replace(/<[^>]*>/g, '').split('(ID:')[0].trim();

                        const permission = {
                            id: permissionId,
                            name: nodeData.name || cleanText,
                            displayName: nodeData.displayName || nodeData.name || cleanText,
                            level: nodeData.level || 0,
                            hierarchyPath: nodeData.hierarchyPath || '',
                            parentPermissionId: nodeData.parentPermissionId
                        };

                        permissions.push(permission);
                        console.log('✅ Added permission:', permission);
                    }
                }
            });

            // Sort by hierarchy path or name
            permissions.sort((a, b) => {
                const pathA = a.hierarchyPath || a.name;
                const pathB = b.hierarchyPath || b.name;
                return pathA.localeCompare(pathB);
            });

            console.log('📋 Final permissions list:', permissions);
            return permissions;

        } catch (error) {
            console.error('❌ Error generating parent permissions from tree:', error);
            return [];
        }
    }
}

// Global functions for compatibility
function showCreatePermissionTypeModal() {
    window.permissionManager?.showCreatePermissionTypeModal();
}

function showCreatePermissionModal(nodeId) {
    console.log('Global function called with nodeId:', nodeId);

    if (!nodeId) {
        console.error('No nodeId provided to showCreatePermissionModal');
        return;
    }

    if (!window.permissionManager) {
        console.error('Permission manager not initialized');
        return;
    }

    window.permissionManager.showCreatePermissionModal(nodeId);
}

function refreshTree() {
    window.permissionManager?.refreshData();
}



// 🔧 DEBUG: Global test functions
function testPermissionTypeDropdown() {
    window.permissionManager?.testPermissionTypeDropdown();
}

function debugPermissionModal() {
    console.log('🧪 Debug Permission Modal');
    console.log('📋 Permission Type Select exists:', $('#permissionTypeSelect').length > 0);
    console.log('📋 Permission Type Select HTML:', $('#permissionTypeSelect').html());
    console.log('📋 Permission Type Select value:', $('#permissionTypeSelect').val());
    console.log('📋 Permission Type Hidden field value:', $('#permissionTypeId').val());
    console.log('📋 Parent Permission Group visible:', $('#parentPermissionGroup').is(':visible'));
    console.log('📋 Parent Permission Select value:', $('#parentPermissionSelect').val());
    console.log('📋 Parent Permission Select selected text:', $('#parentPermissionSelect option:selected').text());
    console.log('📋 Permission Manager State:', window.permissionManager?.state);
    console.log('📋 Available permission types in dropdown:');
    $('#permissionTypeSelect option').each(function () {
        console.log(`   - Value: ${$(this).val()}, Text: ${$(this).text()}`);
    });
    console.log('📋 Available parent permissions in dropdown:');
    $('#parentPermissionSelect option').each(function () {
        console.log(`   - Value: ${$(this).val()}, Text: ${$(this).text()}`);
    });
}

function testParentSelection(parentId) {
    console.log('🧪 Testing parent selection with ID:', parentId);
    $('#parentPermissionSelect').val(parentId.toString());
    console.log('📋 After setting value:', $('#parentPermissionSelect').val());
    $('#parentPermissionSelect').trigger('change');
}

function debugTreeStructure(nodeId) {
    console.log('🧪 Debugging Tree Structure for node:', nodeId);
    const manager = window.permissionManager;
    if (!manager || !manager.tree.treeInstance) {
        console.error('❌ Tree instance not available');
        return;
    }

    const node = manager.tree.getNode(nodeId);
    if (!node) {
        console.error('❌ Node not found:', nodeId);
        return;
    }

    console.log('📋 Node details:', {
        id: node.id,
        text: node.text,
        parent: node.parent,
        children: node.children,
        originalData: node.original?.data,
        state: node.state
    });

    // Check parent
    if (node.parent && node.parent !== '#') {
        const parentNode = manager.tree.getNode(node.parent);
        console.log('📋 Parent node:', {
            id: parentNode?.id,
            text: parentNode?.text,
            isPermission: parentNode?.id?.startsWith('perm_'),
            isType: parentNode?.id?.startsWith('type_')
        });
    }

    // Check children
    if (node.children && node.children.length > 0) {
        console.log('📋 Children:', node.children);
    }
}