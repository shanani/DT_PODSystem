using System;
using System.Collections.Generic;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.ViewModels
{
    public class FormulaCanvasViewModel
    {
        // ✅ CHANGED: QueryId instead of TemplateId
        public int QueryId { get; set; }
        public string QueryName { get; set; } = string.Empty;
        public FormulaCanvasDto Canvas { get; set; } = new FormulaCanvasDto();
        public CanvasConfigurationViewModel Configuration { get; set; } = new CanvasConfigurationViewModel();
        public ElementPaletteViewModel ElementPalette { get; set; } = new ElementPaletteViewModel();
        public CanvasViewStateViewModel ViewState { get; set; } = new CanvasViewStateViewModel();
        public CanvasPerformanceViewModel Performance { get; set; } = new CanvasPerformanceViewModel();

        // API Endpoints - Updated for Query
        public string SaveCanvasUrl { get; set; } = string.Empty;
        public string ValidateFormulaUrl { get; set; } = string.Empty;
        public string TestFormulaUrl { get; set; } = string.Empty;
        public string GetConstantsUrl { get; set; } = string.Empty; // ✅ CHANGED: From GetVariablesUrl
        public string ExportCanvasUrl { get; set; } = string.Empty;
    }

    // ✅ NO CHANGES - All other ViewModels remain the same
    public class CanvasConfigurationViewModel
    {
        public int Width { get; set; } = 1200;
        public int Height { get; set; } = 800;
        public string BackgroundColor { get; set; } = "#f8f9fa";
        public int GridSize { get; set; } = 20;
        public bool SnapToGrid { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public bool ShowRulers { get; set; } = false;
        public decimal ZoomMin { get; set; } = 0.25m;
        public decimal ZoomMax { get; set; } = 3.0m;
        public decimal ZoomStep { get; set; } = 0.25m;
        public bool AutoSave { get; set; } = false;
        public bool ValidationOnChange { get; set; } = true;
        public int UndoLevels { get; set; } = 50;
        public bool EnableKeyboardShortcuts { get; set; } = true;
    }

    public class ElementPaletteViewModel
    {
        public List<string> Categories { get; set; } = new List<string>();
        public List<ElementTemplateViewModel> Fields { get; set; } = new List<ElementTemplateViewModel>();
        public List<ElementTemplateViewModel> Variables { get; set; } = new List<ElementTemplateViewModel>();
        public List<ElementTemplateViewModel> Operations { get; set; } = new List<ElementTemplateViewModel>();
        public List<ElementTemplateViewModel> Functions { get; set; } = new List<ElementTemplateViewModel>();
        public bool IsCollapsed { get; set; } = false;
        public bool SearchEnabled { get; set; } = true;
        public string? SearchTerm { get; set; }
    }

    public class ElementTemplateViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public CanvasElementType Type { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = "#A54EE1";
        public string BorderColor { get; set; } = "#4F008C";
        public string TextColor { get; set; } = "#FFFFFF";
        public DataTypeEnum DataType { get; set; } = DataTypeEnum.Number;
        public Dictionary<string, object> DefaultProperties { get; set; } = new Dictionary<string, object>();
        public bool IsAvailable { get; set; } = true;
        public int DefaultWidth { get; set; } = 120;
        public int DefaultHeight { get; set; } = 60;
    }

    public class CanvasViewStateViewModel
    {
        public decimal ZoomLevel { get; set; } = 1.0m;
        public int PanX { get; set; } = 0;
        public int PanY { get; set; } = 0;
        public List<string> SelectedElements { get; set; } = new List<string>();
        public List<string> VisibleLayers { get; set; } = new List<string>();
        public string ViewMode { get; set; } = "design"; // design, preview, debug
        public bool ShowTooltips { get; set; } = true;
        public bool ShowMinimap { get; set; } = false;
    }

    public class CanvasPerformanceViewModel
    {
        public int ElementCount { get; set; }
        public int ConnectionCount { get; set; }
        public TimeSpan LastSaveTime { get; set; }
        public TimeSpan LastValidationTime { get; set; }
        public long CanvasStateSize { get; set; }
        public string? PerformanceStatus { get; set; }
        public List<string> PerformanceWarnings { get; set; } = new List<string>();
    }

    public class CanvasToolbarViewModel
    {
        public List<ToolbarButtonViewModel> Buttons { get; set; } = new List<ToolbarButtonViewModel>();
        public List<ToolbarDropdownViewModel> Dropdowns { get; set; } = new List<ToolbarDropdownViewModel>();
        public bool IsVisible { get; set; } = true;
        public string Position { get; set; } = "top"; // top, bottom, left, right
    }

    public class ToolbarButtonViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Tooltip { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public string CssClass { get; set; } = "btn btn-outline-secondary btn-sm";
        public string? KeyboardShortcut { get; set; }
    }

    public class ToolbarDropdownViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public List<ToolbarDropdownItemViewModel> Items { get; set; } = new List<ToolbarDropdownItemViewModel>();
        public bool IsEnabled { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }

    public class ToolbarDropdownItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool IsSeparator { get; set; } = false;
    }

    public class CanvasPropertiesViewModel
    {
        public string? SelectedElementId { get; set; }
        public ElementPropertiesViewModel? SelectedElement { get; set; }
        public CanvasGlobalPropertiesViewModel GlobalProperties { get; set; } = new CanvasGlobalPropertiesViewModel();
        public bool IsVisible { get; set; } = true;
        public string Position { get; set; } = "right"; // left, right
    }

    public class ElementPropertiesViewModel
    {
        public string ElementId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public CanvasElementType Type { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<PropertyFieldViewModel> EditableProperties { get; set; } = new List<PropertyFieldViewModel>();
    }

    public class PropertyFieldViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text, number, select, checkbox, color
        public object? Value { get; set; }
        public object? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public List<PropertyOptionViewModel> Options { get; set; } = new List<PropertyOptionViewModel>();
        public string? ValidationPattern { get; set; }
        public string? Tooltip { get; set; }
    }

    public class PropertyOptionViewModel
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class CanvasGlobalPropertiesViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string BackgroundColor { get; set; } = "#f8f9fa";
        public bool ShowGrid { get; set; }
        public int GridSize { get; set; }
        public bool SnapToGrid { get; set; }
    }
}