/**
 * RolePermissionsManager - Dynamic Permission Assignment with JSTree
 * Uses the exact handleJstreeCheckable configuration from ui-tree.demo.js
 * Enhanced with comprehensive debugging for selection issues
 */
class RolePermissionsManager {
    constructor() {
        this.roleData = null;
        this.tree = null;
        this.selectedPermissions = [];
        this.totalPermissions = 0;
        this.isInitialized = false;

        console.log('🎯 RolePermissionsManager created');
    }

    /**
     * Initialize the role permissions manager
     * @param {Object} roleData - Role data with endpoints and initial selections
     */
    async init(roleData) {
        try {
            this.roleData = roleData;
            this.selectedPermissions = [...roleData.initialSelectedPermissions];

            console.log('🚀 Initializing RolePermissionsManager for role:', roleData.roleId);
            console.log('📋 Initial selected permissions from server:', this.selectedPermissions);

            // Show loading state
            this.showLoadingState();

            // Load tree data from server
            const treeData = await this.loadTreeDataFromServer();

            // Initialize JSTree with the exact configuration from ui-tree.demo.js
            this.initializeTree(treeData);

            // Bind events
            this.bindEvents();

            this.isInitialized = true;
            console.log('✅ RolePermissionsManager initialized successfully');

        } catch (error) {
            console.error('❌ Failed to initialize RolePermissionsManager:', error);
            this.showErrorState('Failed to load permissions');
        }
    }

    /**
     * Load tree data from server
     */
    async loadTreeDataFromServer() {
        try {
            const url = `${this.roleData.endpoints.loadUrl}?roleId=${this.roleData.roleId}`;
            console.log('📡 Loading tree data from:', url);

            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Cache-Control': 'no-cache'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            console.log('📋 Server response:', result);

            if (result.success) {
                return this.convertServerDataToJSTreeFormat(result.data);
            } else {
                throw new Error(result.message || 'Server returned error');
            }
        } catch (error) {
            console.error('❌ Error loading tree data:', error);
            throw error;
        }
    }

    /**
     * Convert server tree data to JSTree format (like handleJstreeCheckable)
     */
    convertServerDataToJSTreeFormat(serverData) {
        console.log('🔄 Converting server data to JSTree format');
        console.log('📋 Raw server data:', serverData);

        if (!serverData || !Array.isArray(serverData)) {
            console.warn('⚠️ Invalid server data format');
            return [];
        }

        const treeData = [];

        serverData.forEach(typeNode => {
            console.log('🔍 Processing type node:', typeNode);

            if (typeNode.type === 'permission_type') {
                const permissionTypeData = {
                    text: typeNode.text.replace(/<[^>]*>/g, ''), // Strip HTML
                    icon: this.extractIconFromHtml(typeNode.text),
                    children: []
                };

                // Add children permissions
                if (typeNode.children && typeNode.children.length > 0) {
                    typeNode.children.forEach(permNode => {
                        console.log('🔍 Processing permission node:', permNode);

                        if (permNode.type === 'permission' || permNode.type === 'permission_child') {
                            const permissionId = this.extractPermissionIdFromNodeId(permNode.id);
                            const isSelected = this.selectedPermissions.includes(permissionId);

                            console.log('🎯 Permission details:', {
                                nodeId: permNode.id,
                                permissionId: permissionId,
                                text: permNode.text,
                                isSelected: isSelected
                            });

                            const permissionData = {
                                id: permNode.id, // Keep original ID for reference
                                text: permNode.text.replace(/<[^>]*>/g, ''), // Strip HTML
                                icon: "fa fa-key text-primary fa-lg",
                                state: isSelected ? { selected: true, checked: true } : {},
                                data: {
                                    permissionId: permissionId,
                                    originalId: permNode.id
                                }
                            };

                            // Handle hierarchical permissions
                            if (permNode.children && permNode.children.length > 0) {
                                permissionData.children = [];
                                permNode.children.forEach(childNode => {
                                    const childPermissionId = this.extractPermissionIdFromNodeId(childNode.id);
                                    const isChildSelected = this.selectedPermissions.includes(childPermissionId);

                                    console.log('🎯 Child permission details:', {
                                        nodeId: childNode.id,
                                        permissionId: childPermissionId,
                                        text: childNode.text,
                                        isSelected: isChildSelected
                                    });

                                    permissionData.children.push({
                                        id: childNode.id, // Keep original ID
                                        text: childNode.text.replace(/<[^>]*>/g, ''),
                                        icon: "fa fa-file text-success fa-lg",
                                        state: isChildSelected ? { selected: true, checked: true } : {},
                                        data: {
                                            permissionId: childPermissionId,
                                            originalId: childNode.id
                                        }
                                    });
                                });
                            }

                            permissionTypeData.children.push(permissionData);
                        }
                    });
                }

                treeData.push(permissionTypeData);
            }
        });

        // Count total permissions for statistics
        this.totalPermissions = this.countTotalPermissions(treeData);
        console.log('📊 Total permissions:', this.totalPermissions);
        console.log('📋 Final tree data:', treeData);

        return treeData;
    }

    /**
     * Initialize JSTree with EXACT handleJstreeCheckable configuration
     */
    initializeTree(treeData) {
        console.log('🌳 Initializing JSTree with handleJstreeCheckable configuration');

        if (!treeData || treeData.length === 0) {
            this.showEmptyState('No permissions available');
            return;
        }

        // Use EXACT configuration from handleJstreeCheckable
        $("#permission-tree").jstree({
            "plugins": ["wholerow", "checkbox", "types"],
            "core": {
                "themes": {
                    "responsive": false
                },
                "data": treeData
            },
            "types": {
                "default": {
                    "icon": "fa fa-folder text-primary fa-lg"
                },
                "file": {
                    "icon": "fa fa-file text-success fa-lg"
                }
            }
        });

        // Store tree reference
        this.tree = $("#permission-tree");

        // Bind JSTree events
        this.tree.on('check_node.jstree uncheck_node.jstree', (e, data) => {
            console.log('📋 Tree event:', e.type, 'Node:', data.node.text, 'ID:', data.node.id);

            // Small delay to let JSTree finish processing
            setTimeout(() => {
                this.updateSelectedPermissions();
                this.updateStatistics();
                this.updateHiddenFields();
            }, 10);
        });

        this.tree.on('ready.jstree', () => {
            console.log('✅ JSTree ready');
            console.log('🎯 Initial selected permissions from server:', this.selectedPermissions);

            // Force update after tree is ready
            setTimeout(() => {
                this.updateSelectedPermissions();
                this.updateStatistics();
                console.log('📊 After ready - selected permissions:', this.selectedPermissions);
            }, 100);

            this.hideLoadingState();
        });
    }

    /**
     * Update selected permissions array from tree with enhanced debugging
     */
    updateSelectedPermissions() {
        if (!this.tree) {
            console.log('❌ Tree not initialized');
            return;
        }

        console.log('🔄 === STARTING updateSelectedPermissions ===');

        // Get ALL checked nodes (including parent nodes)
        const allCheckedNodeIds = this.tree.jstree('get_checked');
        console.log('📋 ALL checked node IDs:', allCheckedNodeIds);

        // Get checked nodes with full node objects
        const checkedNodes = this.tree.jstree('get_checked', true);
        console.log('📋 Checked nodes (full objects):', checkedNodes.length);

        // Clear current selection
        this.selectedPermissions = [];

        // Debug: Log all checked nodes first
        checkedNodes.forEach((node, index) => {
            console.log(`📋 Node ${index + 1}:`, {
                id: node.id,
                text: node.text,
                type: node.type,
                data: node.data,
                original: node.original,
                parent: node.parent,
                state: node.state
            });
        });

        // Method 1: Process using node IDs directly (simplest approach)
        console.log('🎯 Method 1: Processing node IDs directly...');
        allCheckedNodeIds.forEach(nodeId => {
            if (nodeId && nodeId.startsWith('perm_')) {
                const permissionId = parseInt(nodeId.replace('perm_', ''));
                if (!isNaN(permissionId) && permissionId > 0) {
                    if (!this.selectedPermissions.includes(permissionId)) {
                        this.selectedPermissions.push(permissionId);
                        console.log('✅ Method 1 - Added permission ID:', permissionId, 'from nodeId:', nodeId);
                    }
                }
            } else {
                console.log('⚠️ Method 1 - Skipping non-permission node:', nodeId);
            }
        });

        // Method 2: Process using full node objects as backup
        if (this.selectedPermissions.length === 0) {
            console.log('🎯 Method 2: Processing full node objects...');

            const permissionNodes = checkedNodes.filter(node => {
                const isPermissionNode = node.id && node.id.startsWith('perm_');
                console.log(`🔍 Node ${node.text}: isPermissionNode = ${isPermissionNode} (ID: ${node.id})`);
                return isPermissionNode;
            });

            console.log('🎯 Permission nodes to process:', permissionNodes.length);

            permissionNodes.forEach((node, index) => {
                console.log(`🔧 Processing permission node ${index + 1}: ${node.text}`);

                let permissionId = null;

                // Try to extract permission ID from multiple sources
                if (node.data && typeof node.data.permissionId !== 'undefined') {
                    permissionId = parseInt(node.data.permissionId);
                    console.log(`✅ Method 2a - Found permissionId in node.data: ${permissionId}`);
                }
                else if (node.original && node.original.data && typeof node.original.data.permissionId !== 'undefined') {
                    permissionId = parseInt(node.original.data.permissionId);
                    console.log(`✅ Method 2b - Found permissionId in node.original.data: ${permissionId}`);
                }
                else if (node.id && node.id.startsWith('perm_')) {
                    const extracted = node.id.replace('perm_', '');
                    permissionId = parseInt(extracted);
                    console.log(`✅ Method 2c - Extracted from node.id: ${node.id} -> ${permissionId}`);
                }

                // Validate and add to selection
                if (permissionId && !isNaN(permissionId) && permissionId > 0) {
                    if (!this.selectedPermissions.includes(permissionId)) {
                        this.selectedPermissions.push(permissionId);
                        console.log(`🎯 Method 2 - Added permission ID: ${permissionId} for "${node.text}"`);
                    } else {
                        console.log(`⚠️ Method 2 - Permission ID ${permissionId} already in selection`);
                    }
                } else {
                    console.log(`❌ Method 2 - Could not extract valid permission ID from node: ${node.text}`, {
                        nodeId: node.id,
                        extractedValue: node.id ? node.id.replace('perm_', '') : 'N/A',
                        parsedValue: permissionId
                    });
                }
            });
        }

        console.log('📊 === FINAL RESULTS ===');
        console.log('📊 Selected permissions:', this.selectedPermissions);
        console.log('📊 Selection count:', this.selectedPermissions.length);
        console.log('📊 === END updateSelectedPermissions ===');

        // Also update hidden field for form submission
        this.updateHiddenFields();
    }

    /**
     * Update statistics display
     */
    updateStatistics() {
        const assignedCount = this.selectedPermissions.length;
        const percentage = this.totalPermissions > 0 ? Math.round((assignedCount / this.totalPermissions) * 100) : 0;

        // Update display elements
        $('#assigned-count').text(assignedCount);
        $('#total-count').text(this.totalPermissions);
        $('#assignment-percentage').text(percentage + '%');

        // Update progress bar
        const progressBar = $('#assignment-progress');
        progressBar.css('width', percentage + '%');
        progressBar.removeClass('bg-success bg-warning bg-info bg-danger');

        if (percentage >= 75) {
            progressBar.addClass('bg-success');
        } else if (percentage >= 50) {
            progressBar.addClass('bg-warning');
        } else if (percentage >= 25) {
            progressBar.addClass('bg-info');
        } else {
            progressBar.addClass('bg-danger');
        }

        console.log('📊 Statistics updated:', { assigned: assignedCount, total: this.totalPermissions, percentage });
    }

    /**
     * Update hidden form fields for submission with enhanced debugging
     */
    updateHiddenFields() {
        console.log('📝 === UPDATING HIDDEN FIELDS ===');

        const form = $('#permissionsForm');
        console.log('📝 Form element:', form.length > 0 ? 'Found' : 'NOT FOUND');

        // Remove existing hidden fields
        const existingFields = form.find('input[name="GrantedPermissionIds"]');
        console.log('📝 Existing hidden fields to remove:', existingFields.length);
        existingFields.remove();

        // Add new hidden fields for each selected permission
        this.selectedPermissions.forEach((permissionId, index) => {
            const hiddenField = $(`<input type="hidden" name="GrantedPermissionIds" value="${permissionId}" />`);
            form.append(hiddenField);
            console.log(`📝 Added hidden field[${index}]: GrantedPermissionIds = ${permissionId}`);
        });

        // Verify hidden fields were added
        const newFields = form.find('input[name="GrantedPermissionIds"]');
        console.log('📝 New hidden fields added:', newFields.length);
        console.log('📝 Hidden field values:', newFields.map(function () { return this.value; }).get());

        console.log('📝 === HIDDEN FIELDS UPDATE COMPLETE ===');
    }

    /**
     * Bind UI events
     */
    bindEvents() {
        console.log('🔗 Binding UI events');

        // Search functionality
        let searchTimeout;
        $('#permission-search').on('input', (e) => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                if (this.tree) {
                    this.tree.jstree('search', e.target.value);
                }
            }, 300);
        });

        // Tree control buttons
        $('#expand-all').click(() => {
            if (this.tree) {
                this.tree.jstree('open_all');
                console.log('🌳 Expanded all nodes');
            }
        });

        $('#collapse-all').click(() => {
            if (this.tree) {
                this.tree.jstree('close_all');
                console.log('🌳 Collapsed all nodes');
            }
        });

        $('#select-all').click(() => {
            if (this.tree) {
                this.tree.jstree('check_all');
                console.log('✅ Selected all nodes');
                // Force update after select all
                setTimeout(() => {
                    this.updateSelectedPermissions();
                    this.updateStatistics();
                }, 100);
            }
        });

        $('#deselect-all').click(() => {
            if (this.tree) {
                this.tree.jstree('uncheck_all');
                console.log('❌ Deselected all nodes');
                // Force update after deselect all
                setTimeout(() => {
                    this.updateSelectedPermissions();
                    this.updateStatistics();
                }, 100);
            }
        });

        // AJAX save button
        $('#save-permissions-ajax').click(() => this.savePermissionsAjax());
    }

    /**
     * Enhanced save method with comprehensive debugging
     */
    async savePermissionsAjax() {
        const saveButton = $('#save-permissions-ajax');
        const originalHtml = saveButton.html();

        try {
            console.log('💾 === SAVING PERMISSIONS START ===');
            console.log('📋 Role ID:', this.roleData.roleId, 'Type:', typeof this.roleData.roleId);

            // Force update selections before saving
            this.updateSelectedPermissions();

            console.log('📋 Selected permissions before save:', this.selectedPermissions);
            console.log('📋 Selected permissions count:', this.selectedPermissions.length);

            // Validate role ID
            const roleId = parseInt(this.roleData.roleId);
            if (isNaN(roleId) || roleId <= 0) {
                throw new Error(`Invalid role ID: ${this.roleData.roleId}`);
            }

            // Set loading state
            saveButton.prop('disabled', true)
                .html('<i class="fas fa-spinner fa-spin me-1"></i>Saving...');

            // Prepare request body using form data format
            const formParams = new URLSearchParams();
            formParams.append('roleId', roleId);

            // Add each permission ID as a separate parameter
            this.selectedPermissions.forEach(permissionId => {
                formParams.append('grantedPermissionIds', permissionId);
            });

            // Add anti-forgery token if available
            const token = $('input[name="__RequestVerificationToken"]').val();
            if (token) {
                formParams.append('__RequestVerificationToken', token);
                console.log('🔐 Added anti-forgery token');
            }

            // Debug form data
            console.log('📤 Form data body:', formParams.toString());

            // Make AJAX request
            const response = await fetch(this.roleData.endpoints.saveUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'Accept': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: formParams.toString()
            });

            console.log('📡 Response status:', response.status);
            console.log('📡 Response ok:', response.ok);

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            console.log('📋 Server response:', result);

            if (result.success) {
                console.log('✅ Permissions saved successfully');

                // Show success message
                this.showSuccessMessage(result.message || 'Permissions saved successfully!');

                // Redirect after delay if redirect URL is provided
                if (this.roleData.endpoints.redirectUrl) {
                    setTimeout(() => {
                        window.location.href = this.roleData.endpoints.redirectUrl;
                    }, 2000);
                }
            } else {
                throw new Error(result.message || 'Failed to save permissions');
            }
        } catch (error) {
            console.error('❌ Error saving permissions:', error);
            console.error('❌ Error details:', {
                message: error.message,
                stack: error.stack,
                roleData: this.roleData,
                selectedPermissions: this.selectedPermissions
            });
            this.showErrorMessage('An error occurred while saving permissions: ' + error.message);
        } finally {
            // Reset button state
            saveButton.prop('disabled', false).html(originalHtml);
            console.log('💾 === SAVING PERMISSIONS END ===');
        }
    }

    /**
     * Utility methods
     */

    extractIconFromHtml(html) {
        if (!html) return 'fa fa-folder text-primary fa-lg';
        const match = html.match(/class="([^"]*(?:fa-[\w-]+)[^"]*)"/);
        return match ? match[1] : 'fa fa-folder text-primary fa-lg';
    }

    extractPermissionIdFromNodeId(nodeId) {
        if (!nodeId || !nodeId.startsWith('perm_')) return 0;
        const id = parseInt(nodeId.replace('perm_', ''));
        return isNaN(id) ? 0 : id;
    }

    countTotalPermissions(treeData) {
        let count = 0;
        const countRecursive = (nodes) => {
            nodes.forEach(node => {
                if (node.data && node.data.permissionId) {
                    count++;
                }
                if (node.children && node.children.length > 0) {
                    countRecursive(node.children);
                }
            });
        };
        countRecursive(treeData);
        return count;
    }

    /**
     * UI State methods
     */

    showLoadingState() {
        $('#permission-tree').html(`
            <div class="tree-loading">
                <i class="fas fa-spinner fa-spin fa-2x me-3"></i>
                Loading permissions...
            </div>
        `);
    }

    hideLoadingState() {
        // Tree content will replace loading state
    }

    showEmptyState(message) {
        $('#permission-tree').html(`
            <div class="tree-empty">
                <i class="fas fa-exclamation-triangle"></i>
                ${message}
            </div>
        `);
    }

    showErrorState(message) {
        $('#permission-tree').html(`
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-circle me-2"></i>
                ${message}
            </div>
        `);
    }

    showSuccessMessage(message) {
        if (typeof alert !== 'undefined' && alert.success) {
            alert.success(message, { popup: false });
        } else {
            console.log('✅ Success:', message);
        }
    }

    showErrorMessage(message) {
        if (typeof alert !== 'undefined' && alert.error) {
            alert.error(message, { popup: false });
        } else {
            console.error('❌ Error:', message);
        }
    }

    /**
     * Public API methods
     */

    getSelectedPermissions() {
        return [...this.selectedPermissions];
    }

    getTotalPermissions() {
        return this.totalPermissions;
    }

    isReady() {
        return this.isInitialized && this.tree !== null;
    }

    destroy() {
        if (this.tree) {
            this.tree.jstree('destroy');
            this.tree = null;
        }
        this.isInitialized = false;
        this.selectedPermissions = [];
        console.log('🗑️ RolePermissionsManager destroyed');
    }

    /**
     * Manual debug methods for testing
     */
    debugTreeState() {
        console.log('🐛 === DEBUG TREE STATE ===');
        if (this.tree) {
            const allNodes = this.tree.jstree('get_json', '#', { flat: true });
            console.log('🐛 All tree nodes:', allNodes);

            const checkedIds = this.tree.jstree('get_checked');
            console.log('🐛 Checked node IDs:', checkedIds);

            const checkedNodes = this.tree.jstree('get_checked', true);
            console.log('🐛 Checked node objects:', checkedNodes);
        } else {
            console.log('🐛 Tree not initialized');
        }
        console.log('🐛 Current selected permissions:', this.selectedPermissions);
        console.log('🐛 === END DEBUG ===');
    }

    forceRefreshSelection() {
        console.log('🔄 Manual selection refresh triggered');
        this.updateSelectedPermissions();
        this.updateStatistics();
        this.updateHiddenFields();
    }
}