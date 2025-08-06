// create-query.js - Query Builder (Fixed Database Pattern)
// ===========================================================

// Query data structure (adapted from formulaData)
let queryData = {
    queryId: null,
    globalConstants: [],
    localConstants: [],
    mappedFields: [], // ✅ CHANGED: mappedVariables → mappedFields
    outputs: [],
    canvasState: null
};

let editor = null;
let constantCounter = 1;
let outputCounter = 1;

// ✅ UPDATED: Mapped Fields Search and Lazy Loading Functions
let mappedFieldsSearchResults = [];
let currentSearchTerm = '';
let searchTimeout = null;
let currentPage = 0;
const PAGE_SIZE = 20;
const svgCache = new Map();

// ✅ UPDATED OPERATIONS_CONFIG WITH COMPLETE TOOLTIP DATA
const OPERATIONS_CONFIG = [
    // Direct buttons - Basic Math Operations
    {
        id: 'add',
        symbol: '+',
        label: 'Add',
        inputs: 2,
        outputs: 1,
        color: '#00C48C',
        icon: 'fa-plus',
        type: 'button',
        style: 'success',
        description: 'Add two numbers together',
        tooltip: 'Adds input1 + input2',
        inputTooltips: [
            { label: 'First Number', description: 'First value to add' },
            { label: 'Second Number', description: 'Second value to add' }
        ],
        example: '5 + 3 = 8'
    },
    {
        id: 'subtract',
        symbol: '−',
        label: 'Subtract',
        inputs: 2,
        outputs: 1,
        color: '#FF375E',
        icon: 'fa-minus',
        type: 'button',
        style: 'danger',
        description: 'Subtract second number from first',
        tooltip: 'Calculates input1 - input2',
        inputTooltips: [
            { label: 'Minuend', description: 'Number to subtract from' },
            { label: 'Subtrahend', description: 'Number to subtract' }
        ],
        example: '10 - 3 = 7'
    },
    {
        id: 'multiply',
        symbol: '×',
        label: 'Multiply',
        inputs: 2,
        outputs: 1,
        color: '#EF7945',
        icon: 'fa-times',
        type: 'button',
        style: 'warning',
        description: 'Multiply two numbers',
        tooltip: 'Multiplies input1 × input2',
        inputTooltips: [
            { label: 'Multiplicand', description: 'First number to multiply' },
            { label: 'Multiplier', description: 'Second number to multiply' }
        ],
        example: '4 × 6 = 24'
    },
    {
        id: 'divide',
        symbol: '÷',
        label: 'Divide',
        inputs: 2,
        outputs: 1,
        color: '#1BCED8',
        icon: 'fa-divide',
        type: 'button',
        style: 'info',
        description: 'Divide first number by second',
        tooltip: 'Divides input1 ÷ input2',
        inputTooltips: [
            { label: 'Dividend', description: 'Number to be divided' },
            { label: 'Divisor', description: 'Number to divide by (cannot be 0)' }
        ],
        example: '12 ÷ 3 = 4'
    },

    // Comparisons dropdown
    {
        id: 'comparisons',
        label: 'Comparisons',
        icon: 'fa-not-equal',
        type: 'dropdown',
        style: 'secondary',
        children: [
            {
                id: 'greater',
                symbol: '>',
                label: 'Greater Than',
                inputs: 2,
                outputs: 1,
                color: '#FF375E',
                description: 'Check if first value is greater than second',
                tooltip: 'Returns true if input1 > input2',
                inputTooltips: [
                    { label: 'Value A', description: 'Value to check if greater' },
                    { label: 'Value B', description: 'Value to compare against' }
                ],
                example: '7 > 3 → true'
            },
            {
                id: 'less',
                symbol: '<',
                label: 'Less Than',
                inputs: 2,
                outputs: 1,
                color: '#EF7945',
                description: 'Check if first value is less than second',
                tooltip: 'Returns true if input1 < input2',
                inputTooltips: [
                    { label: 'Value A', description: 'Value to check if smaller' },
                    { label: 'Value B', description: 'Value to compare against' }
                ],
                example: '2 < 5 → true'
            },
            {
                id: 'greaterEqual',
                symbol: '≥',
                label: 'Greater Equal',
                inputs: 2,
                outputs: 1,
                color: '#FF375E',
                description: 'Check if first value is greater than or equal to second',
                tooltip: 'Returns true if input1 ≥ input2',
                inputTooltips: [
                    { label: 'Value A', description: 'Value to check if greater or equal' },
                    { label: 'Value B', description: 'Value to compare against' }
                ],
                example: '5 ≥ 5 → true'
            },
            {
                id: 'lessEqual',
                symbol: '≤',
                label: 'Less Equal',
                inputs: 2,
                outputs: 1,
                color: '#EF7945',
                description: 'Check if first value is less than or equal to second',
                tooltip: 'Returns true if input1 ≤ input2',
                inputTooltips: [
                    { label: 'Value A', description: 'Value to check if smaller or equal' },
                    { label: 'Value B', description: 'Value to compare against' }
                ],
                example: '3 ≤ 5 → true'
            },
            {
                id: 'equals',
                symbol: '=',
                label: 'Equals',
                inputs: 2,
                outputs: 1,
                color: '#00C48C',
                description: 'Check if two values are equal',
                tooltip: 'Returns true if input1 = input2, false otherwise',
                inputTooltips: [
                    { label: 'Value A', description: 'First value to compare' },
                    { label: 'Value B', description: 'Second value to compare' }
                ],
                example: '5 = 5 → true'
            },
            {
                id: 'notEqual',
                symbol: '≠',
                label: 'Not Equal',
                inputs: 2,
                outputs: 1,
                color: '#FF375E',
                description: 'Check if two values are not equal',
                tooltip: 'Returns true if input1 ≠ input2, false otherwise',
                inputTooltips: [
                    { label: 'Value A', description: 'First value to compare' },
                    { label: 'Value B', description: 'Second value to compare' }
                ],
                example: '5 ≠ 3 → true'
            }
        ]
    },

    // Functions dropdown
    {
        id: 'functions',
        label: 'Functions',
        icon: 'fa-calculator',
        type: 'dropdown',
        style: 'secondary',
        children: [
            {
                id: 'abs',
                symbol: '|x|',
                label: 'Absolute Value',
                inputs: 1,
                outputs: 1,
                color: '#00C48C',
                description: 'Returns absolute (positive) value of input',
                tooltip: 'Returns |input| - always positive',
                inputTooltips: [
                    { label: 'Number', description: 'Number to get absolute value of' }
                ],
                example: '|-5| = 5'
            },
            {
                id: 'sqrt',
                symbol: '√',
                label: 'Square Root',
                inputs: 1,
                outputs: 1,
                color: '#4F008C',
                description: 'Calculate square root',
                tooltip: 'Calculates √input',
                inputTooltips: [
                    { label: 'Number', description: 'Number to find square root of (must be ≥ 0)' }
                ],
                example: '√16 = 4'
            },
            {
                id: 'power',
                symbol: '^',
                label: 'Power',
                inputs: 2,
                outputs: 1,
                color: '#A54EE1',
                description: 'Raise first number to power of second',
                tooltip: 'Calculates input1 ^ input2',
                inputTooltips: [
                    { label: 'Base', description: 'Base number' },
                    { label: 'Exponent', description: 'Power to raise base to' }
                ],
                example: '2 ^ 3 = 8'
            },
            {
                id: 'round',
                symbol: 'RND',
                label: 'Round',
                inputs: 1,
                outputs: 1,
                color: '#EF7945',
                description: 'Round number to nearest integer',
                tooltip: 'Rounds to nearest whole number',
                inputTooltips: [
                    { label: 'Number', description: 'Decimal number to round' }
                ],
                example: 'ROUND(3.7) = 4'
            },
            {
                id: 'min',
                symbol: 'MIN',
                label: 'Minimum',
                inputs: 2,
                outputs: 1,
                color: '#1BCED8',
                description: 'Returns the smaller of two values',
                tooltip: 'Returns MIN(input1, input2)',
                inputTooltips: [
                    { label: 'Value A', description: 'First value to compare' },
                    { label: 'Value B', description: 'Second value to compare' }
                ],
                example: 'MIN(5, 3) = 3'
            },
            {
                id: 'max',
                symbol: 'MAX',
                label: 'Maximum',
                inputs: 2,
                outputs: 1,
                color: '#A54EE1',
                description: 'Returns the larger of two values',
                tooltip: 'Returns MAX(input1, input2)',
                inputTooltips: [
                    { label: 'Value A', description: 'First value to compare' },
                    { label: 'Value B', description: 'Second value to compare' }
                ],
                example: 'MAX(5, 3) = 5'
            }
        ]
    },

    // Special Operations dropdown
    {
        id: 'special',
        label: 'Logic & Special',
        icon: 'fa-code-branch',
        type: 'dropdown',
        style: 'primary',
        children: [
            {
                id: 'if',
                symbol: 'IF',
                label: 'If Condition',
                inputs: 3,
                outputs: 1,
                color: '#A54EE1',
                description: 'Conditional logic: if condition then value_if_true else value_if_false',
                tooltip: 'IF(condition, value_if_true, value_if_false)',
                inputTooltips: [
                    { label: 'Condition', description: 'Boolean condition to evaluate (true/false)' },
                    { label: 'If True', description: 'Value returned when condition is TRUE' },
                    { label: 'If False', description: 'Value returned when condition is FALSE' }
                ],
                example: 'IF(salary > 1000, salary, 0)'
            },
            { separator: true },
            {
                id: 'and',
                symbol: 'AND',
                label: 'AND Logic',
                inputs: 2,
                outputs: 1,
                color: '#1BCED8',
                description: 'Returns true only if both inputs are true',
                tooltip: 'Logical AND: true if both conditions are true',
                inputTooltips: [
                    { label: 'Condition A', description: 'First boolean condition' },
                    { label: 'Condition B', description: 'Second boolean condition' }
                ],
                example: '(5 > 3) AND (2 < 4) → true'
            },
            {
                id: 'or',
                symbol: 'OR',
                label: 'OR Logic',
                inputs: 2,
                outputs: 1,
                color: '#FFE923',
                description: 'Returns true if at least one input is true',
                tooltip: 'Logical OR: true if either condition is true',
                inputTooltips: [
                    { label: 'Condition A', description: 'First boolean condition' },
                    { label: 'Condition B', description: 'Second boolean condition' }
                ],
                example: '(5 > 10) OR (2 < 4) → true'
            },
            {
                id: 'not',
                symbol: 'NOT',
                label: 'NOT Logic',
                inputs: 1,
                outputs: 1,
                color: '#ADB5BD',
                description: 'Inverts boolean value: true becomes false, false becomes true',
                tooltip: 'Logical NOT: inverts the boolean value',
                inputTooltips: [
                    { label: 'Condition', description: 'Boolean condition to invert' }
                ],
                example: 'NOT(5 > 10) → true'
            }
        ]
    }
];




// ✅ SINGLE INITIALIZATION FUNCTION - NO DUPLICATES
function initializeQuery() {
    queryData.queryId = window.queryId || $('#query-id').val();

    // Load data from server FIRST
    loadServerData();

    // Replace with this call:
    ensureFormulaData();

    // Initialize canvas and components - WITH PROPER SEQUENCE
    setTimeout(() => {
        // 1. Initialize canvas ONCE
        if (!window.editor) {
            initializeDrawflowCanvas();
        }

        // 2. Setup event handlers
        setupEventHandlers();

        // 3. Setup operations toolbar
        setupOperationsToolbar();

        // 4. Setup drag and drop
        setupDragAndDrop();

        // 5. Setup canvas controls
        setupCanvasControls();

        // 6. Populate UI components
        populateAllComponents();

        // 7. Restore canvas state if exists - with proper timing
        setTimeout(() => {
            if (queryData.canvasState) {
                restoreCanvasState();
            }

            // 8. Setup formula generation
            trySetupFormulaGeneration();
            updateCanvasEmptyState();
            updateComponentCounts();
        }, 300);
    }, 200);



}


// ✅ FIXED: Property name mismatch - change OutputFields to Outputs
function getQueryFormData() {
    let canvasData = null;
    let generatedFormulas = {};

    // ✅ STEP 1: Generate fresh formulas from current canvas state
    if (window.editor && window.editor.drawflow) {
        // Ensure formulaData is current
        ensureFormulaData();

        const generator = new FormulaGenerator();
        const result = generator.generateFormulasFromCanvas(
            { nodes: window.editor.drawflow.drawflow.Home.data },
            window.formulaData
        );

        if (result.success || result.formulas) {
            // Update UI fields first, then collect from them
            updateFormulaFields(result);

            // Now collect from updated UI fields
            const $formulaFields = $('.formula-field[data-output-id]');

            $formulaFields.each(function () {
                const $field = $(this);
                const outputId = parseInt($field.data('output-id'));
                const formulaValue = $field.val();

                if (outputId && formulaValue) {
                    // Extract the formula part after " = "
                    const formulaPart = formulaValue.includes(' = ')
                        ? formulaValue.split(' = ')[1]
                        : '';

                    // Only save if formula is valid and not empty
                    if (formulaPart.trim() &&
                        formulaPart.trim() !== '' &&
                        formulaPart.trim() !== '[object Object]' &&
                        formulaPart.trim() !== 'INVALID') {
                        generatedFormulas[outputId] = formulaPart.trim();
                        console.log(`✅ Collected formula for output ${outputId}: "${formulaPart.trim()}"`);
                    }
                }
            });
        }
    }

    // ✅ STEP 2: Collect canvas data
    if (window.editor && window.editor.drawflow) {
        try {
            const exportedData = window.editor.export();
            const container = document.getElementById('formula-canvas');
            const containerRect = container.getBoundingClientRect();
            const centerX = window.editor.canvas_x + (containerRect.width / 2) / window.editor.zoom;
            const centerY = window.editor.canvas_y + (containerRect.height / 2) / window.editor.zoom;

            canvasData = {
                nodes: exportedData.drawflow.Home.data,
                zoom: window.editor.zoom || 1,
                position: {
                    x: window.editor.canvas_x || 0,
                    y: window.editor.canvas_y || 0
                },
                viewport: {
                    centerX: centerX,
                    centerY: centerY
                }
            };
        } catch (error) {
            console.error('❌ Error collecting canvas data:', error);
            canvasData = null;
        }
    }

    // ✅ STEP 3: Map output fields with current expressions
    const queryOutputs = queryData.outputs.map((output, index) => {
        const formulaExpression = generatedFormulas[output.id] || '';

        console.log(`✅ Final mapping output "${output.name}" (ID: ${output.id}) = "${formulaExpression}"`);

        return {
            Id: (output.id && output.id > 0) ? output.id : null,
            Name: output.name,
            DisplayName: output.displayName || output.name,
            Description: output.description || '',
            FormulaExpression: formulaExpression, // ✅ Fresh generated expression
            ExecutionOrder: index + 1,
            DisplayOrder: index + 1,
            IsActive: true,
            IncludeInOutput: true,
            IsRequired: false,
            IsVisible: true
        };
    });

    // ✅ STEP 4: Combine constants
    const allConstants = [
        ...queryData.globalConstants.map(c => ({
            Id: (c.id && c.id > 0) ? c.id : null,
            Name: c.name || '',
            DisplayName: c.displayName || c.name || '',
            DefaultValue: (c.value !== undefined && c.value !== null) ? c.value.toString() : (c.defaultValue || '0'),
            IsGlobal: true,
            IsConstant: true,
            IsRequired: false,
            Description: c.description || ""
        })),
        ...queryData.localConstants.map(c => ({
            Id: (c.id && c.id > 0) ? c.id : null,
            Name: c.name || '',
            DisplayName: c.displayName || c.name || '',
            DefaultValue: (c.value !== undefined && c.value !== null) ? c.value.toString() : (c.defaultValue || '0'),
            IsGlobal: false,
            IsConstant: true,
            IsRequired: false,
            Description: c.description || ""
        }))
    ];

    // ✅ FIXED: Use "Outputs" instead of "OutputFields" to match C# DTO
    const finalResult = {
        Constants: allConstants,
        Outputs: queryOutputs, // ✅ FIXED: Changed from "OutputFields" to "Outputs"
        CanvasState: canvasData ? JSON.stringify(canvasData) : '{}'
    };

    console.log('✅ Complete form data with correct property names:', finalResult);
    return finalResult;
}

// ✅ ENHANCED: saveQueryData() with complete form data including Name, Description, Status
async function saveQueryData() {
    try {
        // Debug editor state first
        debugEditorState();

        // ✅ Force formula generation before collecting data
        if (window.editor && window.editor.drawflow) {
            ensureFormulaData();
            autoRegenerateFormulas();

            // Small delay to ensure UI is updated
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        const queryFormData = getQueryFormData();

        // ✅ FIXED: Wait for DOM to be ready and read values directly
        const formName = document.getElementById('query-name')?.value?.trim() || null;
        const formDescription = document.getElementById('query-description')?.value?.trim() || null;
        const formStatus = document.getElementById('query-status')?.value || null;

        // ✅ DEBUG: Log the actual values being sent
        console.log('🔍 Form values being sent:');
        console.log('- formName:', formName);
        console.log('- formDescription:', formDescription);
        console.log('- formStatus:', formStatus);

        // ✅ FIXED: Convert string status to enum number
        let statusValue = null;
        if (formStatus) {
            switch (formStatus) {
                case 'Draft': statusValue = 0; break;
                case 'Testing': statusValue = 1; break;
                case 'Active': statusValue = 2; break;
                case 'Archived': statusValue = 3; break;
                case 'Suspended': statusValue = 4; break;
                default: statusValue = 0; // Default to Draft
            }
        }

        console.log('🔍 Status conversion:', formStatus, '→', statusValue);

        // ✅ FIXED: Build Data object step by step to avoid overwrites
        const dataObject = {
            // ✅ Basic information FIRST
            Name: formName,
            Description: formDescription,
            Status: statusValue, // ✅ Send as number, not string
            // ✅ Canvas data without potential Name/Description/Status conflicts
            Constants: queryFormData.Constants || [],
            Outputs: queryFormData.Outputs || [],
            CanvasState: queryFormData.CanvasState || '{}'
        };

        console.log('🔍 dataObject after creation:', dataObject);
        console.log('🔍 dataObject.Name:', dataObject.Name);
        console.log('🔍 dataObject.Status:', dataObject.Status);

        const requestPayload = {
            QueryId: queryData.queryId,
            Data: dataObject
        };

        console.log('🔍 requestPayload.Data after assignment:', requestPayload.Data);
        console.log('🔍 requestPayload.Data.Name:', requestPayload.Data.Name);

        // ✅ Enhanced logging for debugging
        console.log('🚀 Sending complete request payload:');
        console.log('- QueryId:', requestPayload.QueryId);
        console.log('- Data.Name:', requestPayload.Data.Name);
        console.log('- Data.Description:', requestPayload.Data.Description);
        console.log('- Data.Status:', requestPayload.Data.Status);
        console.log('- Data keys:', Object.keys(requestPayload.Data));
        console.log('- Full payload:', requestPayload);

        const response = await fetch('/Query/SaveQuery', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestPayload)
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error('❌ Server response error:', response.status, errorText);
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const result = await response.json();

        if (result.success) {
            alert.success('Query saved successfully!', { popup: false });
            console.log('✅ Query saved successfully');

            // ✅ Update local queryData with saved values to prevent unnecessary saves
            if (requestPayload.Name) {
                queryData.queryName = requestPayload.Name;
            }
            if (requestPayload.Description) {
                queryData.description = requestPayload.Description;
            }
            if (requestPayload.Status) {
                queryData.status = requestPayload.Status;
            }

            return true;
        } else {
            console.error('❌ Server returned error:', result.message);
            alert.error(result.message || 'Failed to save query');
            return false;
        }

    } catch (error) {
        console.error('💥 Error in saveQueryData:', error);
        alert.error('Error saving query: ' + error.message);
        return false;
    }
}

// ✅ CONSTANTS MANAGEMENT FUNCTIONS
async function saveConstant() {
    const name = $('#constant-name').val().trim();
    const value = $('#constant-value').val().trim();
    const category = $('#constant-category').val();
    const description = $('#constant-description').val().trim();

    if (!name || !value || isNaN(parseFloat(value))) {
        alert.error('Name and valid numeric value are required.');
        return;
    }

    const isEditing = $('#save-constant-modal').data('editing');
    const constantId = isEditing ? $('#save-constant-modal').data('constant-id') : 0;

    const constantData = {
        Id: constantId,
        Name: name,
        DisplayName: name,
        DefaultValue: value,
        IsGlobal: category === 'global',
        IsConstant: true,
        IsRequired: false,
        Description: description
    };

    try {
        const response = await fetch('/Query/SaveConstant', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                QueryId: queryData.queryId,
                Constant: constantData
            })
        });

        const result = await response.json();
        if (!result.success) {
            alert.error(result.message || 'Failed to save constant');
            return;
        }

        // ✅ Create consistent local data structure (camelCase)
        const localConstantData = {
            id: result.id,
            name: name,
            displayName: name,
            defaultValue: value,
            value: parseFloat(value),
            isGlobal: category === 'global',
            isConstant: true,
            isRequired: false,
            description: description
        };

        if (isEditing) {
            updateConstantInLocalData(constantId, localConstantData);
            updateConstantNodeInCanvas(constantId, {
                name: name,
                value: parseFloat(value),
                description: description
            });
        } else {
            addConstantToLocalData(localConstantData, category);
        }

        populateConstants();
        updateComponentCounts();
        $('#constant-modal').modal('hide');
        $('#constant-form')[0].reset();
        $('#save-constant-modal').removeData('editing').removeData('constant-id');
        $('#v-pills-constants-tab').tab('show');
        alert.success(isEditing ? 'Constant updated!' : 'Constant saved!', { popup: false });

    } catch (error) {
        console.error('Error saving constant:', error);
        alert.error('Error saving constant: ' + error.message);
    }
}

async function editConstant(constantId) {
    try {
        // Show modal with loading state first
        $('#constant-form')[0].reset();
        $('#constant-modal-title').text('Loading...');
        $('#save-constant-modal').text('Loading...');

        // ✅ HIDE category dropdown in edit mode
        $('#constant-category').closest('.mb-3').hide();

        $('#constant-modal').modal('show');

        // Fetch constant data from server
        const response = await fetch(`/Query/GetConstant/${constantId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        if (!result.success) {
            console.error('❌ Server returned error:', result.message);
            alert.error(result.message || 'Constant not found');
            $('#constant-modal').modal('hide');
            return;
        }

        const constant = result.constant;

        // Populate form fields with server data
        $('#constant-name').val(constant.name || constant.Name || '');
        $('#constant-value').val(constant.defaultValue || constant.DefaultValue || '');
        $('#constant-category').val((constant.isGlobal || constant.IsGlobal) ? 'global' : 'local');
        $('#constant-description').val(constant.description || constant.Description || '');

        // Update modal UI for editing mode
        $('#constant-modal-title').text('Edit Constant');
        $('#save-constant-modal').text('Update Constant')
            .data('editing', true)
            .data('constant-id', constantId);

        // Focus on name field after modal is fully shown
        setTimeout(() => $('#constant-name').focus(), 300);

    } catch (error) {
        console.error('💥 Error loading constant for edit:', error);
        alert.error('Error loading constant: ' + error.message);
        $('#constant-modal').modal('hide');
    }
}

function showAddConstantModal() {
    $('#constant-modal-title').text('Add Constant');
    $('#constant-form')[0].reset();

    // ✅ SHOW category dropdown in add mode
    $('#constant-category').closest('.mb-3').show();

    // ✅ Set input type to number for value field
    $('#constant-value').attr('type', 'number').attr('step', 'any').attr('placeholder', 'e.g., 0.15');

    // Clear any edit mode data
    $('#save-constant-modal').text('Create Constant').removeData('editing').removeData('constant-id');

    $('#constant-modal').modal('show');
    setTimeout(() => $('#constant-name').focus(), 300);
}

async function removeConstant(constantId) {
    if (!confirm('Remove this constant?')) return;

    // ✅ Save current canvas state first to ensure server has latest data
    try {
        await saveQueryData();
    } catch (error) {
        console.warn('⚠️ Failed to save canvas state before deletion:', error);
    }

    try {
        const response = await fetch('/Query/DeleteConstant', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                QueryId: queryData.queryId,
                ConstantId: constantId
            })
        });

        const result = await response.json();

        if (!result.success) {
            // ❌ Server rejected deletion - show validation error
            if (result.usageDetails && result.usageDetails.length > 0) {
                let errorMsg = result.message + '\n\nUsage Details:\n';
                result.usageDetails.forEach(detail => {
                    errorMsg += '• ' + detail + '\n';
                });

                if (result.requiredActions && result.requiredActions.length > 0) {
                    errorMsg += '\nRequired Actions:\n';
                    result.requiredActions.forEach(action => {
                        errorMsg += '• ' + action + '\n';
                    });
                }

                alert.error(errorMsg);
            } else {
                alert.error(result.message || 'Failed to delete constant');
            }

            // ✅ IMPORTANT: Don't touch the canvas - constant is still in use
            return;
        }

        // ✅ SUCCESS: Server confirmed deletion (constant was NOT in use)
        // Remove from local data arrays
        const numericConstantId = typeof constantId === 'string' ? parseInt(constantId) : constantId;
        queryData.globalConstants = queryData.globalConstants.filter(c => c.id !== numericConstantId);
        queryData.localConstants = queryData.localConstants.filter(c => c.id !== numericConstantId);

        // Update UI lists only
        populateConstants();
        updateComponentCounts();

        alert.success(result.message || 'Constant deleted successfully!', { popup: false });

        setTimeout(() => {
            triggerOutputsChanged();
        }, 100);

    } catch (error) {
        console.error('💥 Error deleting constant:', error);
        alert.error('Error deleting constant: ' + error.message);
    }
}

// ✅ OUTPUT MANAGEMENT FUNCTIONS
async function saveOutput() {
    const form = $('#output-form')[0];
    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return;
    }

    const name = $('#output-name').val().trim();
    const displayName = $('#output-display-name').val().trim() || name;
    const description = $('#output-description').val().trim();

    if (!name) {
        alert.error('Output name is required');
        return;
    }

    const isEditing = $('#save-output').data('editing');
    const outputId = isEditing ? $('#save-output').data('output-id') : 0;

    const outputData = {
        Id: outputId,
        Name: name,
        DisplayName: displayName,
        Description: description,
        FormulaExpression: '',
        ExecutionOrder: queryData.outputs.length + 1,
        DisplayOrder: queryData.outputs.length + 1,
        IsActive: true,
        IncludeInOutput: true,
        IsRequired: false,
        IsVisible: true
    };

    try {
        const response = await fetch('/Query/SaveOutput', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                QueryId: queryData.queryId,
                Output: outputData
            })
        });

        const result = await response.json();
        if (!result.success) {
            alert.error(result.message || 'Failed to save output');
            return;
        }

        outputData.id = result.id;
        outputData.format = 'Number';
        outputData.name = name;
        outputData.displayName = displayName;

        if (isEditing) {
            updateOutputInLocalData(outputId, outputData);
            updateOutputNodeInCanvas(outputId, {
                name: name,
                displayName: displayName,
                description: description
            });
        } else {
            queryData.outputs.push(outputData);
        }

        populateOutputs();
        updateComponentCounts();

        $('#output-modal').modal('hide');
        $('#output-form')[0].reset();
        $('#save-output').removeData('editing').removeData('output-id');
        $('#v-pills-outputs-tab').tab('show');

        alert.success(isEditing ? 'Output updated!' : 'Output saved!', { popup: false });

        setTimeout(() => {
            triggerOutputsChanged();
        }, 100);

    } catch (error) {
        console.error('Error saving output:', error);
        alert.error('Error saving output: ' + error.message);
    }
}

async function editOutput(outputId) {
    try {
        // Show modal with loading state first
        $('#output-form')[0].reset();
        $('#output-modal-title').text('Loading...');
        $('#save-output').text('Loading...');
        $('#output-modal').modal('show');

        // Fetch output data from server
        const response = await fetch(`/Query/GetOutput/${outputId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        if (!result.success) {
            console.error('❌ Server returned error:', result.message);
            alert.error(result.message || 'Output not found');
            $('#output-modal').modal('hide');
            return;
        }

        const output = result.output;

        // Populate form fields with server data
        $('#output-name').val(output.name || output.Name || '');
        $('#output-display-name').val(output.displayName || output.DisplayName || '');
        $('#output-description').val(output.description || output.Description || '');

        // Update modal UI for editing mode
        $('#output-modal-title').text('Edit Output Field');
        $('#save-output').text('Update Output')
            .data('editing', true)
            .data('output-id', outputId);

        // Focus on name field after modal is fully shown
        setTimeout(() => $('#output-name').focus(), 300);

    } catch (error) {
        console.error('💥 Error loading output for edit:', error);
        alert.error('Error loading output: ' + error.message);
        $('#output-modal').modal('hide');
    }
}

function showAddOutputModal() {
    $('#output-form')[0].reset();
    $('#output-modal-title').text('Add Output Field');
    $('#save-output').text('Create Output').removeData('editing').removeData('output-id');
    $('#output-modal').modal('show');
    setTimeout(() => $('#output-name').focus(), 300);
}

async function removeOutput(outputId) {
    if (!confirm('Remove this output field?')) return;

    // Save current canvas state first
    try {
        await saveQueryData();
    } catch (error) {
        console.warn('⚠️ Failed to save canvas state before deletion:', error);
    }

    try {
        const response = await fetch('/Query/DeleteOutput', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                QueryId: queryData.queryId,
                OutputId: outputId
            })
        });

        const result = await response.json();

        if (!result.success) {
            // ❌ Server rejected deletion - show validation error
            if (result.usageDetails && result.usageDetails.length > 0) {
                let errorMsg = result.message + '\n\nUsage Details:\n';
                result.usageDetails.forEach(detail => {
                    errorMsg += '• ' + detail + '\n';
                });

                if (result.requiredActions && result.requiredActions.length > 0) {
                    errorMsg += '\nRequired Actions:\n';
                    result.requiredActions.forEach(action => {
                        errorMsg += '• ' + action + '\n';
                    });
                }

                alert.error(errorMsg);
            } else {
                alert.error(result.message || 'Failed to delete output');
            }

            // ✅ IMPORTANT: Don't touch the canvas - output is still in use
            return;
        }

        // ✅ SUCCESS: Server confirmed deletion (output was NOT in use)
        // Remove from local data array
        const numericOutputId = typeof outputId === 'string' ? parseInt(outputId) : outputId;
        queryData.outputs = queryData.outputs.filter(o => o.id !== numericOutputId);

        // Update UI lists only
        populateOutputs();
        updateComponentCounts();

        alert.success(result.message || 'Output deleted successfully!', { popup: false });

    } catch (error) {
        console.error('💥 Error deleting output:', error);
        alert.error('Error deleting output: ' + error.message);
    }
}

// ✅ HELPER FUNCTIONS FOR DATA MANAGEMENT
function addConstantToLocalData(constantData, category) {
    if (category === 'global') {
        queryData.globalConstants.push(constantData);
    } else {
        queryData.localConstants.push(constantData);
    }
}

function updateConstantInLocalData(constantId, newData) {
    // Find in global constants
    const globalIndex = queryData.globalConstants.findIndex(c => c.id === constantId);
    if (globalIndex !== -1) {
        // If category changed from global to local, move it
        if (!newData.isGlobal) {
            queryData.globalConstants.splice(globalIndex, 1);
            queryData.localConstants.push({ ...newData, category: 'local' });
        } else {
            queryData.globalConstants[globalIndex] = { ...newData, category: 'global' };
        }
        return;
    }

    // Find in local constants
    const localIndex = queryData.localConstants.findIndex(c => c.id === constantId);
    if (localIndex !== -1) {
        // If category changed from local to global, move it
        if (newData.isGlobal) {
            queryData.localConstants.splice(localIndex, 1);
            queryData.globalConstants.push({ ...newData, category: 'global' });
        } else {
            queryData.localConstants[localIndex] = { ...newData, category: 'local' };
        }
        return;
    }

    console.warn('⚠️ Constant not found in either array:', constantId);
}

function updateOutputInLocalData(outputId, newData) {
    const outputIndex = queryData.outputs.findIndex(o => o.id === outputId);
    if (outputIndex !== -1) {
        queryData.outputs[outputIndex] = newData;
    }
}

function updateConstantNodeInCanvas(constantId, newData) {
    if (!window.editor) {
        return;
    }

    const nodes = window.editor.drawflow.drawflow.Home.data;
    let updatedCount = 0;

    Object.entries(nodes).forEach(([nodeId, nodeData]) => {
        if (nodeData.html && nodeData.html.includes(`data-constant-id="${constantId}"`)) {
            const newHtml = `
                <div data-constant-id="${constantId}" title="${newData.description || newData.name}" data-constant-value="${newData.value}">
                    <label>${newData.name}</label>
                    <p>${newData.value}</p>
                </div>
            `;

            // Update the node data
            nodeData.html = newHtml;

            // Update the DOM element if it exists
            const nodeElement = document.querySelector(`#node-${nodeId} .drawflow_content_node`);
            if (nodeElement) {
                nodeElement.innerHTML = newHtml;
                updatedCount++;
            }
        }
    });
}

function updateOutputNodeInCanvas(outputId, newData) {
    if (!window.editor) return;

    const nodes = window.editor.drawflow.drawflow.Home.data;

    Object.entries(nodes).forEach(([nodeId, nodeData]) => {
        if (nodeData.html && nodeData.html.includes(`data-output-id="${outputId}"`)) {
            const newHtml = `<div data-output-id="${outputId}" title="${newData.description || newData.displayName}">${newData.displayName}</div>`;

            nodeData.html = newHtml;

            const nodeElement = document.querySelector(`#node-${nodeId} .drawflow_content_node`);
            if (nodeElement) {
                nodeElement.innerHTML = newHtml;
            }
        }
    });
}


// ✅ UI POPULATION FUNCTIONS
function populateConstants() {
    const globalContainer = $('#global-constants-list');
    const localContainer = $('#local-constants-list');

    // Clear containers
    globalContainer.empty();
    localContainer.empty();

    // ✅ GLOBAL CONSTANTS
    if (queryData.globalConstants && queryData.globalConstants.length > 0) {
        queryData.globalConstants.forEach((constant, index) => {
            const item = createConstantItem(constant);
            globalContainer.append(item);
        });
    } else {
        globalContainer.html('<div class="text-muted text-center p-3">No global constants</div>');
    }

    // ✅ LOCAL CONSTANTS  
    if (queryData.localConstants && queryData.localConstants.length > 0) {
        queryData.localConstants.forEach((constant, index) => {
            const item = createConstantItem(constant);
            localContainer.append(item);
        });
    } else {
        localContainer.html('<div class="text-muted text-center p-3">No local constants</div>');
    }
}

function populateOutputs() {
    const container = $('#output-fields-list');
    if (!container.length) {
        console.error('Output fields container not found');
        return;
    }

    container.empty();

    if (!queryData.outputs || !queryData.outputs.length) {
        container.html('<div class="text-muted text-center p-3">No output fields defined</div>');
        $('#no-outputs-message').show();
        return;
    }

    $('#no-outputs-message').hide();

    queryData.outputs.forEach(output => {
        const outputId = output.id;
        const name = output.name || output.displayName || 'Unnamed';
        const displayName = output.displayName || output.name || 'Unnamed';
        const description = output.description || '';
        const format = output.format || 'Number';

        const item = $(`
            <div class="output-item variable-item" 
                 data-id="${outputId}"
                 data-output-id="${outputId}"
                 draggable="true"
                 style="cursor: grab; padding: 12px; border: 1px solid #dee2e6; border-radius: 6px; margin-bottom: 8px; background: white;">
                <div class="d-flex align-items-center justify-content-between">
                    <div class="d-flex align-items-center">
                        <div style="width: 32px; height: 32px; background: #A54EE1; color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin-right: 8px;">
                            <i class="fa fa-chart-line fa-sm"></i>
                        </div>
                        <div>
                            <div class="fw-bold" style="font-size: 14px;">${displayName}</div>
                            <small class="text-muted">${name} (${format})</small>
                            ${description ? `<div class="small text-secondary mt-1">${description}</div>` : ''}
                        </div>
                    </div>
                    <div class="d-flex align-items-center">
                        <button class="btn btn-sm btn-outline-secondary me-1 edit-output-btn" 
                                data-output-id="${outputId}" title="Edit Output" type="button">
                            <i class="fa fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger delete-output-btn" 
                                data-output-id="${outputId}" title="Delete Output" type="button">
                            <i class="fa fa-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        `);

        // ✅ Add drag event handlers for outputs using database IDs
        item.on('dragstart', function (e) {
            const dragData = {
                type: 'output',
                id: outputId,
                name: name,
                displayName: displayName,
                description: description
            };

            e.originalEvent.dataTransfer.setData('text/plain', JSON.stringify(dragData));
        });

        // Edit button handler
        item.find('.edit-output-btn').on('click', function (e) {
            e.preventDefault();
            const outputId = $(this).data('output-id');
            editOutput(outputId);
        });

        // Delete button handler
        item.find('.delete-output-btn').on('click', function (e) {
            e.preventDefault();
            const outputId = $(this).data('output-id');
            removeOutput(outputId);
        });

        container.append(item);
    });

    // ✅ Generate formula fields after populating outputs
    setTimeout(() => {
        generateFormulaFields();
    }, 50);
}

function createConstantItem(constant) {
    // Value parsing logic
    let displayValue = 0;
    if (constant.value !== undefined && constant.value !== null && !isNaN(constant.value)) {
        displayValue = constant.value;
    } else if (constant.defaultValue !== undefined && constant.defaultValue !== null && constant.defaultValue !== '') {
        const parsed = parseFloat(constant.defaultValue);
        if (!isNaN(parsed)) {
            displayValue = parsed;
        }
    }

    const formattedValue = Number(displayValue).toLocaleString('en-US', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 6,
        useGrouping: true
    });

    const item = $(`
        <div class="constant-item variable-item" 
             data-id="${constant.id}"
             draggable="true"
             style="cursor: grab;">
            <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center flex-grow-1">
                    <div style="width: 32px; height: 32px; background: #28a745; color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin-right: 8px;">
                        <i class="fa fa-hashtag fa-sm"></i>
                    </div>
                    <div>
                        <div class="fw-bold" style="font-size: 14px;">${constant.displayName}</div>
                        <small class="text-muted">
                            <span class="badge bg-success">Number</span>
                            <span class="value-display ms-1 fw-bold" style="color: #28a745;">${formattedValue}</span>
                        </small>
                        ${constant.description ? `<div class="small text-secondary mt-1">${constant.description}</div>` : ''}
                    </div>
                </div>
                
                <div class="constant-controls d-flex align-items-center">
                    <button class="btn btn-sm btn-outline-secondary me-1 edit-constant-btn" 
                            data-constant-id="${constant.id}" title="Edit Constant" type="button">
                        <i class="fa fa-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger delete-constant-btn" 
                            data-constant-id="${constant.id}" title="Delete Constant" type="button">
                        <i class="fa fa-trash"></i>
                    </button>
                </div>
            </div>
        </div>
    `);

    // ✅ Drag handling
    item.on('dragstart', function (e) {
        const dragData = {
            type: 'constant',
            id: constant.id,
            name: constant.displayName,
            value: displayValue,
            dataType: 'Number',
            description: constant.description || ''
        };

        e.originalEvent.dataTransfer.setData('text/plain', JSON.stringify(dragData));
        e.originalEvent.dataTransfer.effectAllowed = 'copy';
        item.addClass('dragging');
    });

    item.on('dragend', function (e) {
        item.removeClass('dragging');
    });

    // ✅ Button events
    item.find('.edit-constant-btn').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        const constantId = $(this).data('constant-id');
        editConstant(constantId);
    });

    item.find('.delete-constant-btn').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        const constantId = $(this).data('constant-id');
        removeConstant(constantId);
    });

    return item;
}

function populateMappedFields() {
    // Setup search functionality if not already done
    if (!$('#template-search').data('search-initialized')) {
        setupMappedFieldsSearch();
        $('#template-search').data('search-initialized', true);
    }
}

function populateAllComponents() {
    populateMappedFields();
    populateConstants();
    populateOutputs();
    updateComponentCounts();
}

 

 
 

// ✅ CANVAS FUNCTIONS
function updateCanvasEmptyState() {
    if (!window.editor) return;

    const nodes = window.editor.drawflow.drawflow.Home.data;
    const nodeCount = Object.keys(nodes).length;
    const hasOutputs = queryData.outputs && queryData.outputs.length > 0;

    $('.canvas-empty-state').remove();

    if (nodeCount === 0) {
        let emptyStateHtml = '';

        if (!hasOutputs) {
            emptyStateHtml = `
                <div class="canvas-empty-state" style="position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); text-align: center; z-index: 100; pointer-events: none;">
                    <i class="fa fa-plus-circle fa-3x mb-3 text-muted"></i>
                    <h5 class="text-muted">Create Your First Output Field</h5>
                    <p class="text-muted">Output fields will automatically appear on the canvas as endpoints</p>
                    <button class="btn btn-primary" onclick="$('#add-output-btn').click()" style="pointer-events: all;">
                        <i class="fa fa-plus me-2"></i>Add Output Field
                    </button>
                </div>
            `;
        } else {
            emptyStateHtml = `
                <div class="canvas-empty-state" style="position: absolute; top: 30%; left: 50%; transform: translate(-50%, -50%); text-align: center; z-index: 100; pointer-events: none;">
                    <i class="fa fa-mouse-pointer fa-2x mb-2 text-muted"></i>
                    <h6 class="text-muted">Drag & Drop to Build Formulas</h6>
                    <p class="text-muted small">Connect variables and operations to the output endpoints</p>
                </div>
            `;
        }

        $('#formula-canvas').append(emptyStateHtml);
    }
}

function clearCanvas() {
    if (confirm('Clear all nodes from canvas?')) {
        if (window.editor) {
            window.editor.clear();
            updateCanvasEmptyState();

            setTimeout(() => {
                if (typeof updateFormulaStatus === 'function') {
                    updateFormulaStatus('modified');
                }
            }, 100);
        }
    }
}

function fitCanvasToView() {
    if (window.editor) {
        window.editor.zoom_reset();
    }
}

function updateComponentCounts() {
    $('#constants-count').text(queryData.globalConstants.length + queryData.localConstants.length);
    $('#outputs-count').text(queryData.outputs.length);
}

// ✅ VALIDATION FUNCTIONS
function validateQueryCustom() {
    // For progress saving - only basic validation
    return true;
}

function debugEditorState() {
    if (window.editor && window.editor.drawflow && window.editor.drawflow.drawflow && window.editor.drawflow.drawflow.Home) {
        const nodes = window.editor.drawflow.drawflow.Home.data;
    }
}



// ✅ CANVAS DROP AND NODE CREATION FUNCTIONS
function setupCanvasDropZone() {
    const canvas = document.getElementById('formula-canvas');
    if (!canvas) {
        console.error('Canvas not found for drop zone setup');
        return;
    }

    canvas.addEventListener('dragover', function (e) {
        e.preventDefault();
        e.stopPropagation();
        e.dataTransfer.dropEffect = 'copy';
        canvas.style.borderColor = '#A54EE1';
        canvas.style.backgroundColor = 'rgba(165, 78, 225, 0.05)';
    });

    canvas.addEventListener('dragleave', function (e) {
        e.preventDefault();
        e.stopPropagation();
        canvas.style.borderColor = '#dee2e6';
        canvas.style.backgroundColor = '#f8f9fa';
    });

    canvas.addEventListener('drop', function (e) {
        e.preventDefault();
        e.stopPropagation();

        canvas.style.borderColor = '#dee2e6';
        canvas.style.backgroundColor = '#f8f9fa';

        try {
            const dragDataStr = e.dataTransfer.getData('text/plain');

            if (!dragDataStr) {
                console.error('No drag data received');
                return;
            }

            const dragData = JSON.parse(dragDataStr);

            const rect = canvas.getBoundingClientRect();
            const pos_x = e.clientX - rect.left;
            const pos_y = e.clientY - rect.top;

            createNodeFromDrop(dragData, pos_x, pos_y);
        } catch (error) {
            console.error('Drop handling error:', error);
            alert.error('Error processing drop: ' + error.message);
        }
    });
}

function createNodeFromDrop(dragData, pos_x, pos_y) {
    if (!window.editor) {
        console.error('window.editor not initialized');
        alert.error('Canvas not ready');
        return;
    }

    if (!dragData || !dragData.type) {
        console.error('Invalid drag data:', dragData);
        alert.error('Invalid drag data');
        return;
    }

    let nodeHtml = '';
    let inputs = 0;
    let outputs = 1;
    let nodeClass = '';
    let nodeId = '';

    // ✅ Generate node ID based on type and check for duplicates
    const existingNodes = window.editor.drawflow.drawflow.Home.data;

    switch (dragData.type) {
        case 'variable':
            nodeId = `node_${dragData.id}`;
            if (Object.keys(existingNodes).includes(nodeId)) {
                return;
            }
            nodeHtml = `<div data-variable-id="${dragData.id}" title="${dragData.description || dragData.name}">${dragData.name}</div>`;
            nodeClass = 'variable-node';
            inputs = 0;
            outputs = 1;
            break;

        case 'constant':
            nodeId = `node_${dragData.id}`;
            if (Object.keys(existingNodes).includes(nodeId)) {
                return;
            }
            nodeHtml = `<div data-constant-id="${dragData.id}" title="${dragData.description || dragData.name}" data-constant-value="${dragData.value}">
                            <label>${dragData.name}</label>
                            <p>${dragData.value}</p>
                        </div>`;
            nodeClass = 'constant-node';
            inputs = 0;
            outputs = 1;
            break;

        case 'operation':
            // Operations can have multiple instances, so use random ID
            nodeId = `node_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
            nodeHtml = `<div data-operation-id="${dragData.operation}" title="${dragData.label}">${dragData.symbol}</div>`;
            nodeClass = 'operation-node operation-' + dragData.operation;
            inputs = dragData.inputs || 2;
            outputs = 1;
            break;

        case 'output':
            // Check if output node already exists by HTML content
            const outputExists = Object.values(existingNodes).some(node =>
                node.html && node.html.includes(`data-output-id="${dragData.id}"`)
            );
            if (outputExists) {
                return;
            }
            nodeId = `node_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`; // Use random ID for drawflow
            nodeHtml = `<div data-output-id="${dragData.id}" title="${dragData.description || dragData.name}">${dragData.displayName}</div>`;
            nodeClass = 'output-node';
            inputs = 1;
            outputs = 0;
            break;

        default:
            console.error('Unknown drag type:', dragData.type);
            alert.error('Unknown element type');
            return;
    }

    if (nodeHtml) {
        try {
            window.editor.addNode(nodeId, inputs, outputs, pos_x, pos_y, nodeClass, {}, nodeHtml);
            updateCanvasEmptyState();

            if (typeof updateFormulaStatus === 'function') {
                updateFormulaStatus('modified');
            }

        } catch (error) {
            console.error('Error adding node:', error);
            alert.error('Error adding node: ' + error.message);
        }
    }
}

function addOperationToCanvas(operationId) {
    if (!window.editor) {
        console.error('window.editor not initialized!');
        return;
    }

    const operation = getOperationById(operationId);
    if (!operation) {
        console.error('Unknown operation:', operationId);
        return;
    }

    const dragData = {
        type: 'operation',
        operation: operationId,
        symbol: operation.symbol,
        label: operation.label,
        inputs: operation.inputs,
        color: operation.color
    };

    const canvasElement = document.getElementById('formula-canvas');
    const canvasRect = canvasElement.getBoundingClientRect();
    const pos_x = (canvasRect.width / 2) + Math.random() * 100 - 50;
    const pos_y = (canvasRect.height / 2) + Math.random() * 100 - 50;

    createNodeFromDrop(dragData, pos_x, pos_y);
}

// ✅ CANVAS STATE MANAGEMENT
function restoreCanvasState() {
    if (!window.editor || !queryData.canvasState) {
        return;
    }

    try {
        const savedCanvasData = typeof queryData.canvasState === 'string'
            ? JSON.parse(queryData.canvasState)
            : queryData.canvasState;

        if (!savedCanvasData || !savedCanvasData.nodes) {
            return;
        }

        // ✅ Use import() to restore nodes AND connections automatically
        const importData = {
            drawflow: {
                Home: {
                    data: savedCanvasData.nodes
                }
            }
        };

        window.editor.clear();
        window.editor.import(importData);

        setTimeout(() => {
            if (savedCanvasData.zoom) {
                window.editor.zoom = savedCanvasData.zoom;
            }
            if (savedCanvasData.position) {
                window.editor.canvas_x = savedCanvasData.position.x || 0;
                window.editor.canvas_y = savedCanvasData.position.y || 0;
            }

            const transform = `translate(${window.editor.canvas_x}px, ${window.editor.canvas_y}px) scale(${window.editor.zoom})`;
            window.editor.precanvas.style.transform = transform;

            // ✅ ADD: Update formulaData after canvas restore
            ensureFormulaData();

            updateCanvasEmptyState();
        }, 200);


        // ✅ Restore positioning after import
        setTimeout(() => {
            if (savedCanvasData.zoom) {
                window.editor.zoom = savedCanvasData.zoom;
            }
            if (savedCanvasData.position) {
                window.editor.canvas_x = savedCanvasData.position.x || 0;
                window.editor.canvas_y = savedCanvasData.position.y || 0;
            }

            const transform = `translate(${window.editor.canvas_x}px, ${window.editor.canvas_y}px) scale(${window.editor.zoom})`;
            window.editor.precanvas.style.transform = transform;

            updateCanvasEmptyState();
        }, 200);

    } catch (error) {
        console.error('🔄 ❌ Error restoring canvas:', error);
        window.editor.clear();
    }
}

// ✅ CONNECTION VALIDATION
function setupSingleConnectionOutputs() {
    if (!window.editor) {
        console.error('Editor not initialized for single connection setup');
        return;
    }

    // ✅ Main validation: Check before connection is actually created
    window.editor.on('connectionCreated', (info) => {
        const { input_id, input_class } = info;

        // ✅ Check if target node is an output node (has data-output-id)
        const targetNode = document.querySelector(`#node-${input_id}`);
        const targetNodeContent = targetNode?.querySelector('.drawflow_content_node');
        const isOutputNode = targetNodeContent?.querySelector('[data-output-id]');

        if (isOutputNode) {
            // ✅ Get current connections to this input
            const nodeData = window.editor.drawflow.drawflow.Home.data[input_id];
            const currentConnections = nodeData?.inputs?.[input_class]?.connections || [];

            // ✅ If more than 1 connection, remove the newest one (just created)
            if (currentConnections.length > 1) {
                // Get the connection that was just created (last one)
                const newestConnection = currentConnections[currentConnections.length - 1];

                // Remove the connection
                window.editor.removeSingleConnection(
                    newestConnection.node,
                    input_id,
                    newestConnection.input,
                    input_class
                );

                // ✅ Show user feedback
                alert.warning('Output fields can only accept one connection!', { popup: false });
            }
        }
    });
}

// ✅ SINGLE FIX: Update loadServerData() to use correct property names
function loadServerData() {
    const serverData = window.serverQueryData;
    if (!serverData) {
        return;
    }

    // ✅ FIX: Use correct property names from server (lowercase from JSON serialization)
    if (serverData.globalConstants) {
        queryData.globalConstants = serverData.globalConstants.map(gc => {
            const rawValue = gc.defaultValue || gc.value || '0';
            let parsedValue = 0;

            if (rawValue !== undefined && rawValue !== null && rawValue !== '') {
                const numVal = parseFloat(rawValue);
                if (!isNaN(numVal)) {
                    parsedValue = numVal;
                }
            }

            return {
                id: gc.id,
                name: gc.name,
                displayName: gc.displayName || gc.name,
                value: parsedValue,
                defaultValue: rawValue,
                description: gc.description || '',
                isGlobal: true,
                isConstant: true,
                category: 'global'
            };
        });
    }

    // ✅ FIX: Use correct property names from server (lowercase)
    if (serverData.localConstants) {
        queryData.localConstants = serverData.localConstants.map(lc => {
            const rawValue = lc.defaultValue || lc.value || '0';
            let parsedValue = 0;

            if (rawValue !== undefined && rawValue !== null && rawValue !== '') {
                const numVal = parseFloat(rawValue);
                if (!isNaN(numVal)) {
                    parsedValue = numVal;
                }
            }

            return {
                id: lc.id,
                name: lc.name,
                displayName: lc.displayName || lc.name,
                value: parsedValue,
                defaultValue: rawValue,
                description: lc.description || '',
                isGlobal: false,
                isConstant: true,
                category: 'local'
            };
        });
    }

    // ✅ FIX: Use correct property names from server (lowercase)
    if (serverData.outputFields) {
        queryData.outputs = serverData.outputFields.map(o => ({
            id: o.id,
            name: o.name,
            displayName: o.displayName || o.name,
            format: o.dataType || 'Number',
            description: o.description || '',
            formula: o.formulaExpression || '',
            displayOrder: o.displayOrder || 0
        }));
    }

    if (serverData.canvasState) {
        queryData.canvasState = serverData.canvasState;
    }

    console.log('📊 Data loaded:', queryData);
}

// ✅ FIXED: generateFormulaFields() with data-output-id
function generateFormulaFields() {
    const container = $('#formula-fields-container');
    const noMessage = $('#no-formulas-message');

    // Clear existing fields
    container.find('.formula-field-item').remove();

    if (!queryData.outputs || queryData.outputs.length === 0) {
        noMessage.show();
        return;
    }

    noMessage.hide();

    // Generate field for each output
    queryData.outputs.forEach((output, index) => {
        const outputName = output.name || output.displayName || 'Unnamed';
        const displayName = output.displayName || output.name || 'Unnamed';
        const initialValue = `[${outputName}] = `;

        const fieldHtml = `
            <div class="formula-field-item mb-2" data-output-id="${output.id}">
                <label for="formula-${output.id}" class="form-label text-muted small">
                    ${displayName} <span class="badge bg-secondary">Output</span>
                </label>
                <input type="text" 
                       id="formula-${output.id}" 
                       class="form-control formula-field" 
                       data-output-id="${output.id}"
                       readonly
                       value="${initialValue}"
                       style="font-family: 'Consolas', 'Monaco', 'Courier New', monospace; font-size: 14px; background-color: #f8f9fa;"
                       placeholder="Formula will be generated from canvas connections...">
            </div>
        `;

        container.append(fieldHtml);
    });
}

  

// ✅ EVENT HANDLERS SETUP - NO DUPLICATES
function setupEventHandlers() {
    // Add constant button
    $('#add-constant-btn').off('click').on('click', showAddConstantModal);

    // Add output button  
    $('#add-output-btn').off('click').on('click', showAddOutputModal);

    // Modal save buttons - using correct IDs from HTML
    $('#save-constant-modal').off('click').on('click', saveConstant);
    $('#save-output').off('click').on('click', saveOutput);

    // Canvas controls
    $('#clear-canvas-btn').off('click').on('click', clearCanvas);
    $('#fit-canvas-btn').off('click').on('click', fitCanvasToView);

    $('#sync-mapped-fields-btn').off('click').on('click', function (e) {
        e.preventDefault();
        syncMappedFields();
    });
     

}

function setupDragAndDrop() {
    // Drag events are attached directly to each item when created
}

function setupCanvasControls() {
    $('#canvas-zoom-in').on('click', function () {
        if (window.editor) window.editor.zoom_in();
    });

    $('#canvas-zoom-out').on('click', function () {
        if (window.editor) window.editor.zoom_out();
    });

    $('#canvas-fit').on('click', function () {
        if (window.editor) window.editor.zoom_reset();
    });

    $('#clear-canvas-btn').on('click', function () {
        clearCanvas();
    });
}

  
 

 
 
function getOperationById(operationId) {
    // Check direct buttons
    const directOp = OPERATIONS_CONFIG.find(op => op.id === operationId && op.type === 'button');
    if (directOp) return directOp;

    // Check dropdown children
    for (const group of OPERATIONS_CONFIG) {
        if (group.type === 'dropdown' && group.children) {
            const child = group.children.find(child => child.id === operationId);
            if (child) {
                return {
                    ...child,
                    color: group.color || '#6c757d'
                };
            }
        }
    }
    return null;
}

function createOperationsToolbar() {
    const canvas = document.getElementById('formula-canvas');
    if (!canvas) return;

    // Create toolbar container
    const toolbar = document.createElement('div');
    toolbar.className = 'operations-toolbar';
    toolbar.style.cssText = 'position: absolute; top: 10px; left: 50%; transform: translateX(-50%); z-index: 100; background: white; border-radius: 8px; padding: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);';

    const btnGroup = document.createElement('div');
    btnGroup.className = 'btn-group';
    btnGroup.setAttribute('role', 'group');

    // Loop through config and create elements
    OPERATIONS_CONFIG.forEach(item => {
        if (item.type === 'button') {
            // Create direct button
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = `btn btn-sm btn-outline-${item.style} operation-btn`;
            btn.setAttribute('data-operation', item.id);
            btn.setAttribute('title', `${item.label} (${item.symbol})`);
            btn.innerHTML = `<i class="fa ${item.icon}"></i>`;
            btnGroup.appendChild(btn);

        } else if (item.type === 'dropdown') {
            // Create dropdown group
            const dropdownGroup = document.createElement('div');
            dropdownGroup.className = 'btn-group';
            dropdownGroup.setAttribute('role', 'group');

            // Dropdown button
            const dropdownBtn = document.createElement('button');
            dropdownBtn.type = 'button';
            dropdownBtn.className = `btn btn-sm btn-outline-${item.style} dropdown-toggle`;
            dropdownBtn.setAttribute('data-bs-toggle', 'dropdown');
            dropdownBtn.setAttribute('title', item.label);
            dropdownBtn.innerHTML = `<i class="fa ${item.icon}"></i>`;

            // Dropdown menu
            const dropdownMenu = document.createElement('ul');
            dropdownMenu.className = 'dropdown-menu';

            // Loop through children
            item.children.forEach((child, index) => {
                if (child.separator) {
                    // Add separator
                    const separator = document.createElement('li');
                    separator.innerHTML = '<hr class="dropdown-divider">';
                    dropdownMenu.appendChild(separator);
                } else {
                    // Add dropdown item
                    const li = document.createElement('li');
                    const a = document.createElement('a');
                    a.className = 'dropdown-item operation-btn';
                    a.href = '#';
                    a.setAttribute('data-operation', child.id);
                    a.innerHTML = `<span class="me-2">${child.symbol}</span>${child.label}`;
                    li.appendChild(a);
                    dropdownMenu.appendChild(li);
                }
            });

            dropdownGroup.appendChild(dropdownBtn);
            dropdownGroup.appendChild(dropdownMenu);
            btnGroup.appendChild(dropdownGroup);
        }
    });

    toolbar.appendChild(btnGroup);
    canvas.style.position = 'relative';
    canvas.appendChild(toolbar);
}

 

async function preloadOperationSVGs() {
    const operations = [
        'add', 'subtract', 'multiply', 'divide', 'power', 'sqrt',
        'equals', 'greater', 'less', 'greaterEqual', 'lessEqual', 'notEqual',
        'if', 'and', 'or', 'not', 'abs', 'round', 'min', 'max'
    ];

    const loadPromises = operations.map(async (operationId) => {
        try {
            const svgPath = `/site/images/ops/${operationId}.svg`;
            const response = await fetch(svgPath);

            if (response.ok) {
                const svgContent = await response.text();
                svgCache.set(operationId, svgContent);
            } else {
                svgCache.set(operationId, '<p class="text-muted small">Diagram not available</p>');
            }
        } catch (error) {
            svgCache.set(operationId, '<p class="text-muted small">Error loading diagram</p>');
        }
    });

    await Promise.all(loadPromises);
}

// ✅ 2. SVG LOADER FROM CACHE
function loadOperationSVGFromCache(operationId) {
    return svgCache.get(operationId) || '<p class="text-muted small">Diagram not available</p>';
}

// ✅ REPLACE your initializeOperationTooltips() function with this:

// ✅ REPLACE your initializeOperationTooltips() function with this:

function initializeOperationTooltips() {
    $('.simple-tooltip').remove();
    $(document).off('mouseenter mouseleave', '.operation-btn');

    let showTimeout, hideTimeout;

    $(document).on('mouseenter', '.operation-btn', function (e) {
        const $btn = $(this);
        const operationId = $btn.data('operation');

        if (!operationId) return;

        clearTimeout(showTimeout);
        clearTimeout(hideTimeout);
        $('.simple-tooltip').remove();

        const operation = getOperationById(operationId);
        if (!operation) return;

        showTimeout = setTimeout(() => {
            try {
                const svgContent = loadOperationSVGFromCache(operationId);

                const tooltipHtml = `
                    <div class="simple-tooltip force-above">
                        <div class="tooltip-body">
                            <div class="op-title">
                                <span class="op-symbol" style="color: ${operation.color}">${operation.symbol}</span>
                                <strong>${operation.label}</strong>
                            </div>
                            <div class="description">${operation.description || 'No description'}</div>
                            ${operation.inputTooltips ? operation.inputTooltips.map(input =>
                    `<div class="input-row">
                                    <span class="input-label">${input.label}:</span> 
                                    <span class="input-desc">${input.description}</span>
                                </div>`
                ).join('') : ''}
                            <div class="example">
                                <strong>Ex:</strong> <code>${operation.example || 'No example'}</code>
                            </div>
                            <div class="diagram">
                                ${svgContent}
                            </div>
                        </div>
                    </div>
                `;

                const $tooltip = $(tooltipHtml);
                $('body').append($tooltip);

                // ✅ ALWAYS POSITION ABOVE - SCALE DOWN IF NEEDED
                const btnRect = this.getBoundingClientRect();
                const tooltipEl = $tooltip[0];
                let tooltipRect = tooltipEl.getBoundingClientRect();

                // Calculate desired position above button
                let left = btnRect.left + (btnRect.width / 2) - (tooltipRect.width / 2);
                let top = btnRect.top - tooltipRect.height - 8;

                const PADDING = 10;
                const viewportWidth = window.innerWidth;
                let scale = 1;

                // ✅ FORCE ABOVE: If tooltip doesn't fit above, scale it down
                if (top < PADDING) {
                    const availableHeight = btnRect.top - PADDING - 8;
                    scale = Math.max(0.6, availableHeight / tooltipRect.height); // Min 60% scale

                    // Apply scale
                    $tooltip.css('transform', `scale(${scale})`);
                    $tooltip.css('transform-origin', 'center bottom');

                    // Recalculate position with new scale
                    tooltipRect = tooltipEl.getBoundingClientRect();
                    top = btnRect.top - (tooltipRect.height * scale) - 8;
                    left = btnRect.left + (btnRect.width / 2) - (tooltipRect.width * scale / 2);
                }

                // Horizontal positioning with scale consideration
                if (left < PADDING) {
                    left = PADDING;
                } else if (left + (tooltipRect.width * scale) > viewportWidth - PADDING) {
                    left = viewportWidth - (tooltipRect.width * scale) - PADDING;
                }

                // Ensure minimum top position
                if (top < PADDING) {
                    top = PADDING;
                }

                $tooltip.css({
                    left: left + 'px',
                    top: top + 'px',
                    opacity: 0
                });

                $tooltip.animate({ opacity: 1 }, 200);

            } catch (error) {
                console.error('❌ Error creating tooltip:', error);
            }
        }, 400);
    });

    $(document).on('mouseleave', '.operation-btn', function () {
        clearTimeout(showTimeout);
        const $tooltip = $('.simple-tooltip');
        if ($tooltip.length) {
            hideTimeout = setTimeout(() => {
                $tooltip.animate({ opacity: 0 }, 150, function () {
                    $(this).remove();
                });
            }, 100);
        }
    });

    $(window).on('scroll resize', function () {
        $('.simple-tooltip').remove();
    });
}

// ✅ 4. UPDATE YOUR EXISTING setupOperationsToolbar() FUNCTION
// Replace your current setupOperationsToolbar() with this:
async function setupOperationsToolbar() {
    const canvas = document.getElementById('formula-canvas');
    if (!canvas) {
        console.error('Canvas not found');
        return;
    }

    // ✅ ADD: Preload SVGs first
    await preloadOperationSVGs();

    // Create operations toolbar dynamically (your existing code)
    createOperationsToolbar();

    // ✅ ADD: Initialize tooltips after SVGs are loaded
    setTimeout(() => {
        initializeOperationTooltips();
    }, 100);

    // Navigation controls (your existing code - keep as is)
    const navHtml = `
        <div style="position: absolute; top: 50px; right: 10px; z-index: 100;">
            <div class="btn-group-vertical" role="group">
                <button class="btn btn-sm btn-outline-secondary" id="canvas-zoom-in" title="Zoom In">
                    <i class="fa fa-search-plus"></i>
                </button>
                <button class="btn btn-sm btn-outline-secondary" id="canvas-zoom-out" title="Zoom Out">
                    <i class="fa fa-search-minus"></i>
                </button>
                <button class="btn btn-sm btn-outline-secondary" id="canvas-fit" title="Fit">
                    <i class="fa fa-expand-arrows-alt"></i>
                </button>
            </div>
        </div>
    `;
    canvas.insertAdjacentHTML('beforeend', navHtml);

    // Handle operation clicks (your existing code - keep as is)
    $(document).off('click', '.operation-btn').on('click', '.operation-btn', function (e) {
        e.preventDefault();
        const operationId = $(this).data('operation');
        if (operationId) {
            addOperationToCanvas(operationId);
        }
    });

    // Event handlers (your existing code - keep as is)
    $('#canvas-zoom-in').on('click', function () {
        if (window.editor) window.editor.zoom_in();
    });

    $('#canvas-zoom-out').on('click', function () {
        if (window.editor) window.editor.zoom_out();
    });

    $('#canvas-fit').on('click', function () {
        if (window.editor) window.editor.zoom_reset();
    });
}
 

function extractDataFromCanvas() {
    if (!window.editor || !window.editor.drawflow || !window.editor.drawflow.drawflow.Home.data) {
        return { variables: [], constants: [] };
    }

    const variables = [];
    const constants = [];
    const nodes = window.editor.drawflow.drawflow.Home.data;

    Object.values(nodes).forEach(node => {
        if (!node.html) return;

        // Extract variables (mapped fields)
        const varIdMatch = node.html.match(/data-variable-id="(\d+)"/);
        if (varIdMatch) {
            const titleMatch = node.html.match(/title="([^"]*)"/);
            const textMatch = node.html.match(/>([^<]+)</);

            variables.push({
                id: parseInt(varIdMatch[1]),
                name: node.html.match(/data-field-name="([^"]*)"/) ? node.html.match(/data-field-name="([^"]*)"/)[1] : (textMatch ? textMatch[1].trim() : `Variable_${varIdMatch[1]}`),
                displayName: textMatch ? textMatch[1].trim() : `Variable_${varIdMatch[1]}`, // ✅ Use textMatch
                dataType: "Number"
            });
        }

        // Extract constants
        const constIdMatch = node.html.match(/data-constant-id="(\d+)"/);
        if (constIdMatch) {
            const valueMatch = node.html.match(/data-constant-value="([^"]*)"/);
            const titleMatch = node.html.match(/title="([^"]*)"/);

            constants.push({
                id: parseInt(constIdMatch[1]),
                name: titleMatch ? titleMatch[1] : `Constant_${constIdMatch[1]}`,
                value: valueMatch ? parseFloat(valueMatch[1]) : 0
            });
        }
    });

    return { variables, constants };
}

// 2. Function to ensure formulaData is properly structured
function ensureFormulaData() {
    if (!window.formulaData) {
        window.formulaData = {};
    }

    // Get data from canvas
    const canvasData = extractDataFromCanvas();

    // Update formulaData with canvas data + queryData
    window.formulaData = {
        operators: [],
        functions: [],
        variables: canvasData.variables,  // ✅ From canvas
        constants: canvasData.constants.concat(  // ✅ Canvas + queryData constants
            (queryData?.globalConstants || []).concat(queryData?.localConstants || [])
        ),
        mappedFields: canvasData.variables  // ✅ Add this for compatibility
    };

    console.log('✅ FormulaData updated:', window.formulaData);
}
 

// ✅ COMPLETE initializeDrawflowCanvas() with live validation
function initializeDrawflowCanvas() {
    const container = document.getElementById('formula-canvas');
    if (!container) {
        console.error('Formula canvas container not found');
        return;
    }

    // ✅ PREVENT DUPLICATE INITIALIZATION
    if (window.editor && window.editor.container === container) {
        console.log('Canvas already initialized');
        return;
    }

    try {
        // Clear any existing editor
        if (window.editor) {
            window.editor.clear();
            delete window.editor;
        }

        // ✅ CREATE NEW EDITOR INSTANCE
        window.editor = new Drawflow(container);
        window.editor.reroute = true;
        window.editor.reroute_fix_curvature = true;
        window.editor.force_first_input = false;
        window.editor.editor_mode = 'edit';
        window.editor.start();

        // ✅ Setup validation AFTER editor is ready
        setupSingleConnectionOutputs();
        setupCanvasDropZone();
        setupLiveFormulaValidation(); // ✅ ADD: Live validation setup

        console.log('✅ Canvas initialized successfully');
    } catch (error) {
        console.error('Failed to initialize Drawflow:', error);
    }
}

// ✅ NEW: Setup live formula validation (from wizard-step4.js)
function setupLiveFormulaValidation() {
    if (!window.editor) {
        console.error('Editor not initialized for live validation setup');
        return;
    }

    // ✅ Auto-regenerate when connections are created
    window.editor.on('connectionCreated', (info) => {
        setTimeout(() => {
            autoRegenerateFormulas();
        }, 300);
    });

    // ✅ Auto-regenerate when connections are removed
    window.editor.on('connectionRemoved', (info) => {
        setTimeout(() => {
            autoRegenerateFormulas();
        }, 300);
    });

    // ✅ Auto-regenerate when nodes are added
    const originalAddNode = window.editor.addNode;
    window.editor.addNode = function (...args) {
        const result = originalAddNode.apply(this, args);
        setTimeout(() => {
            autoRegenerateFormulas();
        }, 300);
        return result;
    };

    console.log('✅ Live formula validation setup complete');
}

// ✅ NEW: Auto-regenerate function with validation (from wizard-step4.js)
function autoRegenerateFormulas() {
    if (!window.editor) return;

    // Update formulaData first
    ensureFormulaData();

    // Generate formulas
    const generator = new FormulaGenerator();
    const canvasData = { nodes: window.editor.drawflow.drawflow.Home.data };
    const result = generator.generateFormulasFromCanvas(canvasData, window.formulaData);

    // Update formula fields and show validation
    updateFormulaFields(result);

    // Update status
    if (typeof updateFormulaStatus === 'function') {
        updateFormulaStatus(result.success ? 'valid' : 'invalid');
    }

    console.log('🔄 Auto-regenerated formulas:', result.success ? 'Success' : 'Failed', result);
}

// ✅ FIXED: Per-output validation - each output independently valid/invalid
function updateFormulaFields(formulaResult) {
    // Clear previous errors and styling
    $('#formula-errors').hide();
    $('#formula-errors-list').empty();
    $('.formula-field').removeClass('is-invalid is-valid');

    // Show errors if any exist
    if (formulaResult && formulaResult.errors && formulaResult.errors.length > 0) {
        displayFormulaErrors(formulaResult.errors);
    }

    // ✅ Process each output independently
    $('.formula-field').each(function () {
        const $field = $(this);
        const outputId = $field.closest('.formula-field-item').data('output-id');
        const output = queryData.outputs.find(o => o.id == outputId);

        if (!output) return;

        const outputName = output.name || output.displayName || 'Unnamed';

        // ✅ Check if this specific output has a valid formula
        const hasValidFormula = formulaResult &&
            formulaResult.formulas &&
            formulaResult.formulas[outputName];

        if (hasValidFormula) {
            // ✅ This output is VALID - show green with formula
            const formula = formulaResult.formulas[outputName];
            const fullFormula = `[${outputName}] = ${formula}`;
            $field.val(fullFormula).removeClass('is-invalid').addClass('is-valid');
        } else {
            // ✅ This output is INVALID - show red with empty formula
            $field.val(`[${outputName}] = `).removeClass('is-valid').addClass('is-invalid');
        }
    });
}

// ✅ NEW: Display formula errors function (from wizard-step4.js)
function displayFormulaErrors(errors) {
    if (!errors || errors.length === 0) return;

    const $errorsList = $('#formula-errors-list');
    $errorsList.empty();

    errors.forEach(error => {
        $errorsList.append(`<li>${error}</li>`);
    });

    $('#formula-errors').show();
}

// ✅ ENHANCED: trySetupFormulaGeneration with auto-regeneration (from wizard-step4.js)
function trySetupFormulaGeneration() {
    function trySetup() {
        if (typeof window.setupFormulaGeneration === 'function') {
            window.setupFormulaGeneration();

            // ✅ Ensure formulaData is current before generating
            ensureFormulaData();

            // Auto-generate formulas after setup
            setTimeout(() => {
                if (typeof window.autoRegenerateFormulas === 'function') {
                    window.autoRegenerateFormulas();
                } else if (typeof window.regenerateFormulas === 'function') {
                    window.regenerateFormulas();
                } else {
                    // Use our local auto-regenerate function
                    autoRegenerateFormulas();
                }
            }, 500);

        } else if (typeof setupFormulaGeneration === 'function') {
            setupFormulaGeneration();

            // ✅ Ensure formulaData is current before generating
            ensureFormulaData();

            setTimeout(() => {
                if (typeof autoRegenerateFormulas === 'function') {
                    autoRegenerateFormulas();
                } else if (typeof regenerateFormulas === 'function') {
                    regenerateFormulas();
                } else {
                    // Use our local auto-regenerate function
                    autoRegenerateFormulas();
                }
            }, 500);

        } else {
            console.warn('⚠️ setupFormulaGeneration not available yet, retrying in 200ms...');
            setTimeout(trySetup, 200);
        }
    }

    trySetup();
}

// ✅ ENHANCED: triggerOutputsChanged with auto-regeneration
function triggerOutputsChanged() {
    generateFormulaFields();

    // Also trigger auto-regenerate
    setTimeout(() => {
        autoRegenerateFormulas();
    }, 100);
}

// ✅ ENHANCED: createNodeFromDrop with auto-regeneration
function createNodeFromDrop(dragData, pos_x, pos_y) {
    if (!window.editor) {
        console.error('window.editor not initialized');
        alert.error('Canvas not ready');
        return;
    }

    if (!dragData || !dragData.type) {
        console.error('Invalid drag data:', dragData);
        alert.error('Invalid drag data');
        return;
    }

    let nodeHtml = '';
    let inputs = 0;
    let outputs = 1;
    let nodeClass = '';
    let nodeId = '';

    // ✅ Generate node ID based on type and check for duplicates
    const existingNodes = window.editor.drawflow.drawflow.Home.data;

    switch (dragData.type) {
        case 'variable':
            nodeId = `node_${dragData.id}`;
            if (Object.keys(existingNodes).includes(nodeId)) {
                return;
            }

            // Get template name for the field
            const templateName = dragData.templateName || getCurrentTemplateName() || 'Unknown Template';

            nodeHtml = `
    <div data-variable-id="${dragData.id}" data-field-name="${dragData.name}" data-template-id="${dragData.templateId || ''}" title="${dragData.description || dragData.name}" style="text-align: center; padding: 5px;">
        <div style="font-weight: bold; font-size: 0.95rem; margin-bottom: 2px;">
            ${dragData.name}
        </div>
        <div style="font-size: 0.75rem; opacity: 0.8; font-style: italic;">
            ${templateName}
        </div>
    </div>
`;

            nodeClass = 'variable-node';
            inputs = 0;
            outputs = 1;
            break;

        case 'constant':
            nodeId = `node_${dragData.id}`;
            if (Object.keys(existingNodes).includes(nodeId)) {
                return;
            }
            nodeHtml = `<div data-constant-id="${dragData.id}" title="${dragData.description || dragData.name}" data-constant-value="${dragData.value}">
                            <label>${dragData.name}</label>
                            <p>${dragData.value}</p>
                        </div>`;
            nodeClass = 'constant-node';
            inputs = 0;
            outputs = 1;
            break;

        case 'operation':
            // Operations can have multiple instances, so use random ID
            nodeId = `node_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
            nodeHtml = `<div data-operation-id="${dragData.operation}" title="${dragData.label}">${dragData.symbol}</div>`;
            nodeClass = 'operation-node operation-' + dragData.operation;
            inputs = dragData.inputs || 2;
            outputs = 1;
            break;

        case 'output':
            // Check if output node already exists by HTML content
            const outputExists = Object.values(existingNodes).some(node =>
                node.html && node.html.includes(`data-output-id="${dragData.id}"`)
            );
            if (outputExists) {
                return;
            }
            nodeId = `node_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`; // Use random ID for drawflow
            nodeHtml = `<div data-output-id="${dragData.id}" title="${dragData.description || dragData.name}">${dragData.displayName}</div>`;
            nodeClass = 'output-node';
            inputs = 1;
            outputs = 0;
            break;

        default:
            console.error('Unknown drag type:', dragData.type);
            alert.error('Unknown element type');
            return;
    }

    if (nodeHtml) {
        try {
            window.editor.addNode(nodeId, inputs, outputs, pos_x, pos_y, nodeClass, {}, nodeHtml);
            updateCanvasEmptyState();

            // ✅ Trigger auto-regeneration after adding node
            setTimeout(() => {
                autoRegenerateFormulas();
            }, 300);

            if (typeof updateFormulaStatus === 'function') {
                updateFormulaStatus('modified');
            }

        } catch (error) {
            console.error('Error adding node:', error);
            alert.error('Error adding node: ' + error.message);
        }
    }
}

 
// ✅ UPDATED: createMappedFieldItem function with template name in drag data
function createMappedFieldItem(field) {
    const item = $(`
        <div class="mapped-field-result variable-item mb-2 p-2 border rounded bg-light" 
             draggable="true" 
             data-field-id="${field.fieldId}" 
             data-template-id="${field.templateId}"
             style="cursor: grab;">
            <div class="d-flex align-items-center">
                <div style="width: 32px; height: 32px; background: #007bff; color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin-right: 8px;">
                    <i class="fa fa-tag fa-sm"></i>
                </div>
                <div class="flex-grow-1">
                    <div class="fw-bold text-info" style="font-size: 14px;">${field.fieldName}</div>
                    <div class="small text-muted mt-1">
                        <i class="fa fa-building me-1"></i>${field.templateName}
                        ${field.vendorName ? ` • <i class="fa fa-user me-1"></i>${field.vendorName}` : ''}
                        ${field.categoryName ? ` • <i class="fa fa-folder me-1"></i>${field.categoryName}` : ''}
                    </div>
                </div>
            </div>
        </div>
    `);

    // Add drag functionality with template name included
    item.on('dragstart', function (e) {
        const dragData = {
            type: 'variable',
            id: field.fieldId,
            name: field.displayName || field.fieldName,
            displayName: field.displayName || field.fieldName,
            dataType: field.dataType,
            description: field.description || '',
            templateName: field.templateName || 'Unknown Template'  // ✅ ADD: Include template name
        };

        e.originalEvent.dataTransfer.setData('text/plain', JSON.stringify(dragData));
        e.originalEvent.dataTransfer.effectAllowed = 'copy';
        $(this).addClass('dragging');
    });

    item.on('dragend', function (e) {
        $(this).removeClass('dragging');
    });

    return item;
}

//Update Functions

// ============================================================================
// PART 1: UPDATED EXISTING FUNCTIONS (Multi-select Template Filter)
// ============================================================================

// ✅ UPDATED: setupMappedFieldsSearch with multi-select template filter
function setupMappedFieldsSearch() {
    const searchInput = $('#template-search');
    const clearBtn = $('#clear-search');
    const templateFilter = $('#template-filter');
    const loadingIndicator = $('#template-search-loading');

    // ✅ NEW: Load templates for filter dropdown
    loadTemplatesForFilter();

    // Search with debouncing
    searchInput.on('input', function () {
        const searchTerm = $(this).val().trim();

        // Clear previous timeout
        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        // Debounce search (300ms delay)
        searchTimeout = setTimeout(() => {
            performMappedFieldsSearch(searchTerm);
        }, 300);
    });

    // ✅ UPDATED: Template filter change handler for multi-select
    templateFilter.on('change', function () {
        const searchTerm = searchInput.val().trim();

        // Clear previous timeout
        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        // Immediate search when filter changes
        searchTimeout = setTimeout(() => {
            performMappedFieldsSearch(searchTerm);
        }, 100);
    });

    // Clear search and filter
    clearBtn.on('click', function () {
        searchInput.val('');
        templateFilter.val(null).trigger('change'); // ✅ UPDATED: Clear multi-select
        clearMappedFieldsSearch();
    });

    // Show more results
    $('#show-more-btn').on('click', function () {
        showMoreMappedFieldsResults();
    });
}

// ✅ UPDATED: performMappedFieldsSearch with multi-select template filter
async function performMappedFieldsSearch(searchTerm) {
    const loadingIndicator = $('#template-search-loading');
    const resultsContainer = $('#template-variables-list');
    const noSearchPerformed = $('#no-search-performed');
    const noResults = $('#no-search-results');
    const templateFilter = $('#template-filter');

    currentSearchTerm = searchTerm;
    currentPage = 0;

    // ✅ UPDATED: Get selected template IDs (multi-select)
    const selectedTemplateIds = templateFilter.val(); // Returns array for multi-select
    const templateIdsArray = selectedTemplateIds && selectedTemplateIds.length > 0 ?
        selectedTemplateIds.map(id => parseInt(id)) : null;

    // Handle empty search and no filter
    if ((!searchTerm || searchTerm.length < 2) && !templateIdsArray) {
        clearMappedFieldsSearch();
        return;
    }

    try {
        // Show loading
        loadingIndicator.show();
        noSearchPerformed.hide();
        noResults.hide();
        resultsContainer.find('.mapped-field-result').remove();

        // ✅ UPDATED: Include template IDs filter in request
        const requestBody = {
            searchTerm: searchTerm,
            page: 0,
            pageSize: PAGE_SIZE
        };

        // Add template IDs filter if selected
        if (templateIdsArray && templateIdsArray.length > 0) {
            requestBody.templateIds = templateIdsArray;
        }

        // Make AJAX request to search endpoint
        const response = await fetch('/Template/SearchMappedFields', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success) {
            mappedFieldsSearchResults = result.results || [];
            displayMappedFieldsSearchResults(mappedFieldsSearchResults, result.totalCount || 0);
        } else {
            console.error('Mapped fields search failed:', result.message);
            showNoMappedFieldsResults();
        }

    } catch (error) {
        console.error('Error searching mapped fields:', error);
        showNoMappedFieldsResults();
    } finally {
        loadingIndicator.hide();
    }
}

// ✅ UPDATED: showMoreMappedFieldsResults with multi-select template filter support
async function showMoreMappedFieldsResults() {
    const templateFilter = $('#template-filter');
    const selectedTemplateIds = templateFilter.val();

    if (!currentSearchTerm && (!selectedTemplateIds || selectedTemplateIds.length === 0)) return;

    currentPage++;
    const loadingIndicator = $('#template-search-loading');

    try {
        loadingIndicator.show();

        // ✅ UPDATED: Get selected template IDs (multi-select)
        const templateIdsArray = selectedTemplateIds && selectedTemplateIds.length > 0 ?
            selectedTemplateIds.map(id => parseInt(id)) : null;

        // ✅ UPDATED: Include template IDs filter in request
        const requestBody = {
            searchTerm: currentSearchTerm,
            page: currentPage,
            pageSize: PAGE_SIZE
        };

        // Add template IDs filter if selected
        if (templateIdsArray && templateIdsArray.length > 0) {
            requestBody.templateIds = templateIdsArray;
        }

        const response = await fetch('/Template/SearchMappedFields', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestBody)
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success && result.results && result.results.length > 0) {
            const container = $('#template-variables-list');

            // Append new results
            result.results.forEach(field => {
                const fieldItem = createMappedFieldItem(field);
                container.append(fieldItem);
            });

            // Update counts
            const currentShown = parseInt($('#shown-count').text()) + result.results.length;
            $('#shown-count').text(currentShown);

            // Hide "Show More" if all results are shown
            if (currentShown >= result.totalCount) {
                $('#show-more-container').hide();
            }
        }

    } catch (error) {
        console.error('Error loading more mapped fields:', error);
    } finally {
        loadingIndicator.hide();
    }
}

// ✅ UPDATED: clearMappedFieldsSearch to reset multi-select filter
function clearMappedFieldsSearch() {
    const container = $('#template-variables-list');
    const noSearchPerformed = $('#no-search-performed');
    const noResults = $('#no-search-results');
    const showMoreContainer = $('#show-more-container');
    const templateFilter = $('#template-filter');

    // Clear results
    container.find('.mapped-field-result').remove();

    // Reset state
    currentSearchTerm = '';
    currentPage = 0;
    mappedFieldsSearchResults = [];

    // ✅ UPDATED: Reset Select2 multi-select template filter
    templateFilter.val(null).trigger('change');

    // Show initial message
    noSearchPerformed.show();
    noResults.hide();
    showMoreContainer.hide();

    // Reset count
    $('#mapped-count').text('0');
}

// ✅ UPDATED: showNoMappedFieldsResults with multi-select messaging
function showNoMappedFieldsResults() {
    const noResults = $('#no-search-results');
    const noSearchPerformed = $('#no-search-performed');
    const showMoreContainer = $('#show-more-container');
    const templateFilter = $('#template-filter');

    // ✅ UPDATED: Update message based on multi-select filter state
    const selectedTemplateIds = templateFilter.val();
    const hasTemplateFilter = selectedTemplateIds && selectedTemplateIds.length > 0;
    const hasSearchTerm = currentSearchTerm && currentSearchTerm.length >= 2;

    let message = 'No mapped fields found';
    if (hasTemplateFilter && hasSearchTerm) {
        const templateCount = selectedTemplateIds.length;
        message = `No fields found in selected ${templateCount} template${templateCount > 1 ? 's' : ''} matching your search`;
    } else if (hasTemplateFilter) {
        const templateCount = selectedTemplateIds.length;
        message = `No fields found in selected ${templateCount} template${templateCount > 1 ? 's' : ''}`;
    } else if (hasSearchTerm) {
        message = 'No fields found matching your search';
    }

    // Update the no results message
    noResults.find('p').text(message);

    noSearchPerformed.hide();
    noResults.show();
    showMoreContainer.hide();
    $('#mapped-count').text('0');
}

// ✅ UPDATED: displayMappedFieldsSearchResults with multi-select filter info
function displayMappedFieldsSearchResults(results, totalCount) {
    const container = $('#template-variables-list');
    const noResults = $('#no-search-results');
    const showMoreContainer = $('#show-more-container');
    const shownCount = $('#shown-count');
    const totalCountSpan = $('#total-count');
    const mappedCount = $('#mapped-count');

    // Clear previous results
    container.find('.mapped-field-result').remove();

    if (!results || results.length === 0) {
        showNoMappedFieldsResults();
        return;
    }

    // Hide no search performed message
    $('#no-search-performed').hide();

    // Display results
    results.forEach(field => {
        const fieldItem = createMappedFieldItem(field);
        container.append(fieldItem);
    });

    // Update counts
    shownCount.text(results.length);
    totalCountSpan.text(totalCount);
    mappedCount.text(totalCount);

    // Show/hide "Show More" button
    if (results.length < totalCount) {
        showMoreContainer.show();
    } else {
        showMoreContainer.hide();
    }

    // ✅ UPDATED: Log search info for debugging (multi-select)
    const templateFilter = $('#template-filter');
    const selectedTemplateIds = templateFilter.val();
    let filterInfo = 'across all templates';
    if (selectedTemplateIds && selectedTemplateIds.length > 0) {
        filterInfo = `in ${selectedTemplateIds.length} selected template${selectedTemplateIds.length > 1 ? 's' : ''}`;
    }
    console.log(`✅ Search results: ${totalCount} fields found ${filterInfo}`);
}

//New Functions

// ============================================================================
// PART 2: NEW FUNCTIONS ONLY (Template Filter Support)
// ============================================================================

// ✅ NEW: Load templates for filter dropdown
async function loadTemplatesForFilter() {
    try {
        const response = await fetch('/Template/GetTemplatesForFilter', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success && result.data) {
            populateTemplateFilter(result.data);
        } else {
            console.warn('Failed to load templates for filter:', result.message);
        }

    } catch (error) {
        console.error('Error loading templates for filter:', error);
        // Continue without filter options - not critical
    }
}

// ✅ NEW: Populate template filter dropdown (Select2 multi-select with vertical layout)
function populateTemplateFilter(templates) {
    const templateFilter = $('#template-filter');

    // Clear existing options
    templateFilter.empty();

    // Add template options with better formatting
    templates.forEach(template => {
        // Main template name only (details will show in dropdown)
        const optionText = template.name;
        const optionData = {
            name: template.name,
            category: template.categoryName || '',
            vendor: template.vendorName || '',
            fieldCount: template.fieldCount || 0
        };

        const option = $(`<option value="${template.id}">${optionText}</option>`);
        option.data('template-info', optionData);
        templateFilter.append(option);
    });

    // ✅ Initialize Select2 with custom formatting and NO problematic CSS classes
    templateFilter.select2({
        placeholder: 'Select Templates...',
        allowClear: true,
        closeOnSelect: true,
        width: '100%',
        templateResult: formatTemplateOption,
        templateSelection: formatTemplateSelection
    });

    

    console.log(`✅ Loaded ${templates.length} templates for Select2 multi-select filter`);
}

// ✅ NEW: Format template option in dropdown (two-line display)
function formatTemplateOption(option) {
    if (!option.id) return option.text;

    const $option = $(option.element);
    const templateInfo = $option.data('template-info');

    if (!templateInfo) return option.text;

    const $result = $(`
        <div class="template-option">
            <div class="template-name">${templateInfo.name}</div>
            <div class="template-details">
                ${templateInfo.category ? `Category: ${templateInfo.category}` : ''}
                ${templateInfo.vendor ? ` • Vendor: ${templateInfo.vendor}` : ''}
                ${templateInfo.fieldCount > 0 ? ` • ${templateInfo.fieldCount} fields` : ''}
            </div>
        </div>
    `);

    return $result;
}

// ✅ NEW: Format selected template (compact display)
function formatTemplateSelection(option) {
    if (!option.id) return option.text;

    const $option = $(option.element);
    const templateInfo = $option.data('template-info');

    // Return just the template name for selected items (compact)
    return templateInfo ? templateInfo.name : option.text;
}
 
// ✅ UPDATED: Sync Mapped Fields Function - Uses existing Template controller method
async function syncMappedFields() {
    if (!window.editor || !window.editor.drawflow) {
        alert.error('Canvas not initialized');
        return;
    }

    const syncBtn = $('#sync-mapped-fields-btn');
    const originalText = syncBtn.html();

    try {
        // Show loading state
        syncBtn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-1"></i>Syncing...');

        const nodes = window.editor.drawflow.drawflow.Home.data;
        const variableNodes = [];

        // ✅ Step 1: Find all variable nodes (mapped fields) in canvas
        Object.entries(nodes).forEach(([nodeId, nodeData]) => {
            if (nodeData.html && nodeData.html.includes('data-variable-id=')) {
                const varIdMatch = nodeData.html.match(/data-variable-id="(\d+)"/);
                if (varIdMatch) {
                    const fieldId = parseInt(varIdMatch[1]);
                    variableNodes.push({
                        nodeId: nodeId,
                        fieldId: fieldId,
                        nodeData: nodeData
                    });
                }
            }
        });

        if (variableNodes.length === 0) {
            alert.info('No mapped fields found in canvas to sync', { popup: false });
            return;
        }

        console.log(`🔄 Found ${variableNodes.length} mapped field nodes to sync`);

        // ✅ Step 2: Use existing SearchMappedFields to get latest field information
        const fieldIds = variableNodes.map(node => node.fieldId);

        // Create a search that will return only our specific fields
        // We'll search with empty term but filter by field IDs using multiple small searches
        const latestFields = [];

        // Get field information by searching for each field individually
        for (const fieldId of fieldIds) {
            try {
                const response = await fetch('/Template/SearchMappedFields', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        searchTerm: '', // Empty search term
                        page: 0,
                        pageSize: 1000, // Large page size to get all results
                        templateIds: null // No template filter
                    })
                });

                if (response.ok) {
                    const result = await response.json();
                    if (result.success && result.results) {
                        // Find our specific field in the results
                        const fieldInfo = result.results.find(f => f.fieldId === fieldId);
                        if (fieldInfo) {
                            latestFields.push(fieldInfo);
                        }
                    }
                }
            } catch (error) {
                console.warn(`⚠️ Could not fetch info for field ${fieldId}:`, error);
            }
        }

        let updatedCount = 0;
        let orphanCount = 0;

        // ✅ Step 3: Update each node with latest information or mark as orphan
        variableNodes.forEach(({ nodeId, fieldId, nodeData }) => {
            const latestField = latestFields.find(f => f.fieldId === fieldId);
            const nodeElement = document.querySelector(`#node-${nodeId}`);

            if (!latestField) {
                // ✅ Field was deleted or not found - mark as orphan
                if (nodeElement) {
                    nodeElement.classList.add('orphan-node');

                    // Update node content to show orphan status
                    const contentNode = nodeElement.querySelector('.drawflow_content_node');
                    if (contentNode) {
                        contentNode.style.opacity = '0.5';
                        contentNode.style.backgroundColor = '#ff6b6b';
                        contentNode.style.color = 'white';
                        contentNode.title = 'Field deleted or not accessible - Orphan Node';

                        // Add orphan indicator
                        if (!contentNode.querySelector('.orphan-indicator')) {
                            const orphanIndicator = document.createElement('div');
                            orphanIndicator.className = 'orphan-indicator';
                            orphanIndicator.style.cssText = 'position: absolute; top: -5px; right: -5px; background: #dc3545; color: white; border-radius: 50%; width: 16px; height: 16px; font-size: 10px; display: flex; align-items: center; justify-content: center; font-weight: bold;';
                            orphanIndicator.innerHTML = '!';
                            contentNode.style.position = 'relative';
                            contentNode.appendChild(orphanIndicator);
                        }
                    }
                }
                orphanCount++;
                console.log(`⚠️ Field ${fieldId} not found - marked as orphan`);
            } else {
                // ✅ Field exists - check if update needed
                const currentName = extractFieldNameFromNode(nodeData.html);
                const currentTemplateName = extractTemplateNameFromNode(nodeData.html);

                const needsUpdate = currentName !== latestField.fieldName ||
                    currentTemplateName !== latestField.templateName;

                if (needsUpdate) {
                    // ✅ Update node content with latest information
                    const newHtml = `
                        <div data-variable-id="${fieldId}" title="${latestField.description || latestField.fieldName}" style="text-align: center; padding: 5px;">
                            <div style="font-weight: bold; font-size: 0.95rem; margin-bottom: 2px;">
                                ${latestField.fieldName}
                            </div>
                            <div style="font-size: 0.75rem; opacity: 0.8; font-style: italic;">
                                ${latestField.templateName}
                            </div>
                        </div>
                    `;

                    // Update node data
                    nodeData.html = newHtml;

                    // Update DOM element
                    if (nodeElement) {
                        const contentNode = nodeElement.querySelector('.drawflow_content_node');
                        if (contentNode) {
                            contentNode.innerHTML = newHtml;

                            // Remove orphan styling if it exists
                            nodeElement.classList.remove('orphan-node');
                            contentNode.style.opacity = '';
                            contentNode.style.backgroundColor = '';
                            contentNode.style.color = '';

                            // Remove orphan indicator if it exists
                            const orphanIndicator = contentNode.querySelector('.orphan-indicator');
                            if (orphanIndicator) {
                                orphanIndicator.remove();
                            }
                        }
                    }

                    updatedCount++;
                    console.log(`✅ Updated field ${fieldId}: "${currentName}" -> "${latestField.fieldName}" (Template: "${latestField.templateName}")`);
                }
            }
        });

        // ✅ Step 4: Show results to user
        let message = '';
        if (updatedCount > 0 && orphanCount > 0) {
            message = `Sync complete: ${updatedCount} field(s) updated, ${orphanCount} orphan(s) found`;
        } else if (updatedCount > 0) {
            message = `Sync complete: ${updatedCount} field(s) updated`;
        } else if (orphanCount > 0) {
            message = `Sync complete: ${orphanCount} orphan field(s) found`;
        } else {
            message = 'All mapped fields are up to date';
        }

        if (updatedCount > 0 || orphanCount > 0) {
            alert.success(message, { popup: false });

            // ✅ Trigger auto-regeneration after sync
            setTimeout(() => {
                if (typeof autoRegenerateFormulas === 'function') {
                    autoRegenerateFormulas();
                }
            }, 300);
        } else {
            alert.info(message, { popup: false });
        }

    } catch (error) {
        console.error('❌ Error syncing mapped fields:', error);
        alert.error('Error syncing mapped fields: ' + error.message);
    } finally {
        // Reset button state
        syncBtn.prop('disabled', false).html(originalText);
    }
}

// ✅ Helper function to extract field name from node HTML
function extractFieldNameFromNode(html) {
    const match = html.match(/<div style="font-weight: bold[^>]*>([^<]+)<\/div>/);
    return match ? match[1].trim() : '';
}

// ✅ Helper function to extract template name from node HTML
function extractTemplateNameFromNode(html) {
    const match = html.match(/<div style="font-size: 0\.75rem[^>]*>([^<]+)<\/div>/);
    return match ? match[1].trim() : '';
}

// ✅ Export function to window
window.syncMappedFields = syncMappedFields;


// ✅ Export the new functions
window.setupLiveFormulaValidation = setupLiveFormulaValidation;
window.autoRegenerateFormulas = autoRegenerateFormulas;
window.displayFormulaErrors = displayFormulaErrors;
 

// ✅ EXPORT ALL FUNCTIONS TO WINDOW
window.initializeQuery = initializeQuery;
window.getQueryFormData = getQueryFormData;
window.validateQueryCustom = validateQueryCustom;
window.saveQueryData = saveQueryData;
window.saveConstant = saveConstant;
window.saveOutput = saveOutput;
window.removeConstant = removeConstant;
window.removeOutput = removeOutput;
window.editOutput = editOutput;
window.editConstant = editConstant;
window.triggerOutputsChanged = triggerOutputsChanged;
window.updateFormulaFields = updateFormulaFields;

// ✅ FALLBACK FUNCTIONS
window.updateFormulaStatus = window.updateFormulaStatus || function (status) { };
window.regenerateFormulas = window.regenerateFormulas || function () { };

