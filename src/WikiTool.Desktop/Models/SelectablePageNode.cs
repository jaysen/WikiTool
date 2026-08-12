using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WikiTool.Desktop.Models;

/// <summary>
/// Wraps a <see cref="FolderTreeNode"/> with a tri-state check box so pages can be picked in bulk.
/// Checking a folder checks everything under it; a folder shows indeterminate when only some of its
/// descendants are checked.
/// </summary>
public partial class SelectablePageNode : ObservableObject
{
    private bool _isUpdatingSelection;

    public FolderTreeNode FolderTreeNode { get; }
    public string RelativePath { get; }
    public SelectablePageNode? Parent { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool? _checkState; // true = checked, false = unchecked, null = indeterminate

    public ObservableCollection<SelectablePageNode> Children { get; } = [];

    public string Name => FolderTreeNode.Name;
    public bool IsFolder => FolderTreeNode.IsFolder;

    public SelectablePageNode(FolderTreeNode node, string relativePath, SelectablePageNode? parent = null)
    {
        FolderTreeNode = node;
        RelativePath = relativePath;
        Parent = parent;
        _checkState = false;
    }

    partial void OnCheckStateChanged(bool? value)
    {
        if (_isUpdatingSelection) return;

        _isUpdatingSelection = true;
        try
        {
            // When check state changes from user interaction (not null/indeterminate click)
            if (value.HasValue)
            {
                IsSelected = value.Value;

                // Propagate to all children
                SetChildrenCheckState(value.Value);
            }

            // Update parent state
            Parent?.UpdateCheckStateFromChildren();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void SetChildrenCheckState(bool isChecked)
    {
        foreach (var child in Children)
        {
            child._isUpdatingSelection = true;
            child.CheckState = isChecked;
            child.IsSelected = isChecked;
            child.SetChildrenCheckState(isChecked);
            child._isUpdatingSelection = false;
        }
    }

    public void UpdateCheckStateFromChildren()
    {
        if (_isUpdatingSelection) return;
        if (Children.Count == 0) return;

        _isUpdatingSelection = true;
        try
        {
            var allChildren = GetAllDescendants(this).ToList();
            var checkedCount = allChildren.Count(c => c.CheckState == true);
            var totalCount = allChildren.Count;

            if (checkedCount == 0)
            {
                CheckState = false;
                IsSelected = false;
            }
            else if (checkedCount == totalCount)
            {
                CheckState = true;
                IsSelected = true;
            }
            else
            {
                CheckState = null; // Indeterminate
                IsSelected = false;
            }

            // Continue updating up the tree
            Parent?.UpdateCheckStateFromChildren();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private static IEnumerable<SelectablePageNode> GetAllDescendants(SelectablePageNode node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var descendant in GetAllDescendants(child))
            {
                yield return descendant;
            }
        }
    }
}
