// Clean formula-generator.js - Simple and direct
class FormulaGenerator {
    constructor() {
        this.errors = [];
        this.generatedFormulas = {};
    }

    generateFormulasFromCanvas(canvasData, formulaData) {
        this.errors = [];
        this.generatedFormulas = {};

        try {
            const graph = this.parseCanvas(canvasData);
            const outputs = graph.nodes.filter(n => n.type === 'output');

            for (const output of outputs) {
                const expression = this.buildExpression(output.id, graph, formulaData, new Set());
                if (expression) {
                    this.generatedFormulas[output.name] = expression;
                }
            }

            return {
                success: this.errors.length === 0,
                formulas: this.generatedFormulas,
                errors: this.errors
            };
        } catch (error) {
            this.errors.push(error.message);
            return { success: false, formulas: {}, errors: this.errors };
        }
    }

    parseCanvas(canvasData) {
        const nodes = [];
        const connections = [];

        // Parse nodes
        Object.entries(canvasData.nodes).forEach(([id, data]) => {
            const node = this.parseNode(id, data);
            if (node) nodes.push(node);
        });

        // Parse connections - from inputs perspective (cleaner)
        Object.entries(canvasData.nodes).forEach(([nodeId, nodeData]) => {
            if (nodeData.inputs) {
                Object.entries(nodeData.inputs).forEach(([inputPort, inputData]) => {
                    if (inputData.connections) {
                        inputData.connections.forEach(conn => {
                            connections.push({
                                from: conn.node,
                                to: nodeId,
                                toPort: inputPort
                            });
                        });
                    }
                });
            }
        });

        return { nodes, connections };
    }

    parseNode(id, data) {
        if (!data.html) return null;
        const $html = $(data.html);

        // Variable
        const varId = $html.attr('data-variable-id');
        if (varId) return { id, type: 'variable', varId, name: $html.text().trim() };

        // Constant  
        const constId = $html.attr('data-constant-id');
        if (constId) {
            const value = $html.attr('data-constant-value');
            return { id, type: 'constant', constId, value, name: $html.text().trim() };
        }

        // Operation
        const opId = $html.attr('data-operation-id');
        if (opId) return { id, type: 'operation', op: opId };

        // Output
        const outId = $html.attr('data-output-id');
        if (outId) return { id, type: 'output', outId, name: $html.text().trim() };

        return null;
    }

    
    // ✅ ALSO UPDATE: generateFormulasFromCanvas to handle errors properly
    generateFormulasFromCanvas(canvasData, formulaData) {
        this.errors = [];
        this.generatedFormulas = {};

        try {
            const graph = this.parseCanvas(canvasData);
            const outputs = graph.nodes.filter(n => n.type === 'output');

            // ✅ Check if there are any outputs on canvas
            if (outputs.length === 0) {
                // No error if no outputs on canvas - this is normal
                return {
                    success: true,
                    formulas: {},
                    errors: []
                };
            }

            for (const output of outputs) {
                const expression = this.buildExpression(output.id, graph, formulaData, new Set());

                // ✅ Only add to formulas if expression is valid (not 'INVALID')
                if (expression && expression !== 'INVALID') {
                    this.generatedFormulas[output.name] = expression;
                }
                // If expression is 'INVALID', the error was already added in buildExpression
            }

            return {
                success: this.errors.length === 0,
                formulas: this.generatedFormulas,
                errors: this.errors
            };
        } catch (error) {
            this.errors.push(error.message);
            return { success: false, formulas: {}, errors: this.errors };
        }
    }

    // ✅ ENHANCED: buildExpression with full connectivity validation
    buildExpression(nodeId, graph, formulaData, visited) {
        if (visited.has(nodeId)) throw new Error(`Circular dependency at ${nodeId}`);
        visited.add(nodeId);

        const node = graph.nodes.find(n => n.id === nodeId);
        if (!node) throw new Error(`Node ${nodeId} not found`);

        let result;
        try {
            switch (node.type) {
                case 'variable':
                    const variable = formulaData.mappedFields.find(v => v.id == node.varId);
                    result = variable ? `[Input:${variable.name}#${variable.id}]` : '0';
                    break;

                case 'constant':
                    result = node.value || '0';
                    break;

                case 'operation':
                    // ✅ ENHANCED: Validate operation connectivity
                    result = this.buildOperationWithValidation(node, graph, formulaData, visited);
                    break;

                case 'output':
                    // ✅ Check for unconnected outputs
                    const inputConn = graph.connections.find(c => c.to === nodeId);
                    if (!inputConn) {
                        this.errors.push(`Output "${node.name}" is not connected to any input`);
                        result = 'INVALID';
                    } else {
                        result = this.buildExpression(inputConn.from, graph, formulaData, visited);
                    }
                    break;

                default:
                    result = '0';
                    break;
            }
        } finally {
            visited.delete(nodeId);
        }

        return result;
    }

    // ✅ NEW: Enhanced operation building with connectivity validation
    buildOperationWithValidation(node, graph, formulaData, visited) {
        // Get all connections to this operation
        const connections = graph.connections.filter(c => c.to === node.id);

        // Sort by port number (input_1, input_2, input_3...)
        connections.sort((a, b) => {
            const portA = parseInt(a.toPort.replace('input_', '')) || 1;
            const portB = parseInt(b.toPort.replace('input_', '')) || 1;
            return portA - portB;
        });

        // ✅ Get operation configuration to know expected inputs
        const operation = this.getOperationConfig(node.op);
        const expectedInputs = operation ? operation.inputs : 2; // Default to 2 if unknown

        // ✅ Check if all required inputs are connected
        const missingInputs = [];
        for (let i = 1; i <= expectedInputs; i++) {
            const hasConnection = connections.some(conn =>
                parseInt(conn.toPort.replace('input_', '')) === i
            );
            if (!hasConnection) {
                missingInputs.push(`input ${i}`);
            }
        }

        // ✅ If inputs are missing, create error and return INVALID
        if (missingInputs.length > 0) {
            const operationName = operation ? operation.label : node.op.toUpperCase();
            this.errors.push(`${operationName} operation is missing connections: ${missingInputs.join(', ')}`);
            return 'INVALID';
        }

        // ✅ Build expressions for each connected input
        const inputs = [];
        for (const conn of connections) {
            const expr = this.buildExpression(conn.from, graph, formulaData, new Set(visited));

            // ✅ If any input expression is invalid, this operation is invalid
            if (expr === 'INVALID') {
                return 'INVALID'; // Propagate the invalid state up the chain
            }

            inputs.push(expr);
        }

        console.log(`${node.op}: [${inputs.join(', ')}] - All inputs connected`);

        // ✅ Format operation with validated inputs
        return this.formatOperation(node.op, inputs);
    }

    // ✅ SIMPLIFIED: Get operation configuration (no fallback)
    getOperationConfig(operationId) {
        // Check direct buttons from OPERATIONS_CONFIG
        const directOp = OPERATIONS_CONFIG?.find(op => op.id === operationId && op.type === 'button');
        if (directOp) return directOp;

        // Check dropdown children
        if (typeof OPERATIONS_CONFIG !== 'undefined') {
            for (const group of OPERATIONS_CONFIG) {
                if (group.type === 'dropdown' && group.children) {
                    const child = group.children.find(child => child.id === operationId);
                    if (child) return child;
                }
            }
        }

        // No fallback - return null if not found
        return null;
    }

    // ✅ COMPLETE: Format operation expression for all 20 operations
    formatOperation(operationId, inputs) {
        switch (operationId) {
            // Basic Math
            case 'add': return `(${inputs.join(' + ')})`;
            case 'subtract': return `(${inputs.join(' - ')})`;
            case 'multiply': return `(${inputs.join(' * ')})`;
            case 'divide': return `(${inputs.join(' / ')})`;
            case 'power': return `POWER(${inputs[0] || '0'}, ${inputs[1] || '1'})`;
            case 'mod': return `MOD(${inputs[0] || '0'}, ${inputs[1] || '1'})`;
            case 'sqrt': return `SQRT(${inputs[0] || '0'})`;

            // Comparison
            case 'greater': return `(${inputs[0]} > ${inputs[1] || '0'})`;
            case 'less': return `(${inputs[0]} < ${inputs[1] || '0'})`;
            case 'greaterEqual': return `(${inputs[0]} >= ${inputs[1] || '0'})`;
            case 'lessEqual': return `(${inputs[0]} <= ${inputs[1] || '0'})`;
            case 'equals': return `(${inputs[0]} = ${inputs[1] || '0'})`;
            case 'notEqual': return `(${inputs[0]} <> ${inputs[1] || '0'})`;

            // Logical
            case 'and': return `(${inputs[0]} AND ${inputs[1]})`;
            case 'or': return `(${inputs[0]} OR ${inputs[1]})`;
            case 'not': return `NOT(${inputs[0] || 'FALSE'})`;

            // Functions
            case 'abs': return `ABS(${inputs[0] || '0'})`;
            case 'round': return `ROUND(${inputs[0] || '0'})`;
            case 'if': return `IF(${inputs[0] || 'TRUE'}, ${inputs[1] || '1'}, ${inputs[2] || '0'})`;
            case 'min': return `MIN(${inputs.join(', ')})`;
            case 'max': return `MAX(${inputs.join(', ')})`;

            default: return `UNKNOWN_OP(${inputs.join(', ')})`;
        }
    }
}

// Simple functions
function regenerateFormulas() {
    if (!window.editor) return;

    const generator = new FormulaGenerator();
    const result = generator.generateFormulasFromCanvas(
        { nodes: window.editor.drawflow.drawflow.Home.data },
        formulaData
    );

    if (typeof window.updateFormulaFields === 'function') {
        window.updateFormulaFields(result);
    }

    console.log('Generated:', result);
}

function setupFormulaGeneration() {
    $('#regenerate-formula-btn').off('click').on('click', function () {
        ensureFormulaData();  // ✅ Add this line
        regenerateFormulas();
    });
}

window.FormulaGenerator = FormulaGenerator;
window.regenerateFormulas = regenerateFormulas;
window.setupFormulaGeneration = setupFormulaGeneration;