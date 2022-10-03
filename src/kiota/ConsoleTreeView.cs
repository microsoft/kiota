using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Text;

namespace Kiota;

internal class ConsoleTreeView<NodeType> : ContentView
{
    private readonly NodeType _root;
    private readonly uint _maxDepth;
    private readonly Func<NodeType, string> _nodeNameGetter;
    private readonly Func<NodeType, IEnumerable<NodeType>> _childrenGetter;

    public ConsoleTreeView(NodeType rootNode, Func<NodeType, string> nodeNameGetter, Func<NodeType, IEnumerable<NodeType>> childrenGetter, uint maxDepth = default)
    {
        ArgumentNullException.ThrowIfNull(nodeNameGetter);
        ArgumentNullException.ThrowIfNull(childrenGetter);
        _root = rootNode;
        _maxDepth = maxDepth;
        _nodeNameGetter = nodeNameGetter;
        _childrenGetter = childrenGetter;
    }
    private const string Cross = " ├─";
    private const string Corner = " └─";
    private const string Vertical = " │ ";
    private const string Space = "   ";
    public override void Render(ConsoleRenderer renderer, Region region = null)
    {
        var builder = new StringBuilder();
        if (_root != null) {
            RenderNode(_root, builder);
        }
        var content = new ContentSpan(builder.ToString());
        renderer.RenderToRegion(content, region);
    }
    public string RenderAsString() {
        var builder = new StringBuilder();
        if (_root != null) {
            RenderNode(_root, builder);
        }
        return builder.ToString();
    }
    private void RenderNode(NodeType node, StringBuilder builder, string indent = "", int nodeDepth = 0)
    {
        builder.AppendLine(_nodeNameGetter(node));

        var children = _childrenGetter(node);
        var numberOfChildren = children.Count();
        for (var i = 0; i < numberOfChildren; i++)
        {
            var child = children.ElementAt(i);
            var isLast = i == (numberOfChildren - 1);
            RenderChildNode(child, builder, indent, isLast, nodeDepth);
        }
    }

    private void RenderChildNode(NodeType node, StringBuilder builder, string indent, bool isLast, int nodeDepth = 0)
    {
        if (nodeDepth >= _maxDepth && _maxDepth != 0)
            return;
        builder.Append(indent);

        if (isLast)
        {
            builder.Append(Corner);
            indent += Space;
        }
        else
        {
            builder.Append(Cross);
            indent += Vertical;
        }

        RenderNode(node, builder, indent, ++nodeDepth);
    }
}
