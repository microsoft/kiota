using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Text;

namespace Kiota;

internal class ConsoleTreeView<NodeType> : ContentView<NodeType>
{
    private readonly uint _maxDepth;
    private readonly Func<NodeType, string> _nodeNameGetter;
    private readonly Func<NodeType, IEnumerable<NodeType>> _childrenGetter;

    public ConsoleTreeView(NodeType rootNode, Func<NodeType, string> nodeNameGetter, Func<NodeType, IEnumerable<NodeType>> childrenGetter, uint maxDepth = default):base(rootNode)
    {
        ArgumentNullException.ThrowIfNull(nodeNameGetter);
        ArgumentNullException.ThrowIfNull(childrenGetter);
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
        Span = new ContentSpan(GetTreeAsString());
        base.Render(renderer, region);
    }
    private string GetTreeAsString(){
        var builder = new StringBuilder();
        if (Value != null) {
            RenderNode(Value, builder);
        }
        return builder.ToString();
    }
    public override Size Measure(ConsoleRenderer renderer, Size maxSize)
    {
        var stringValue = GetTreeAsString();
        var width = stringValue.Split(Environment.NewLine).Max(static x => x.Length);
        var height = stringValue.Count(static x => x == Environment.NewLine[0]) + 1;
        return new Size(width, height);
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
