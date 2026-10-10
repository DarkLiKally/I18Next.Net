using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Options;

namespace I18Next.Net.Blazor;

/// <summary>
///     Renders a translation, like the <c>Trans</c> component of react-i18next. Markup in the translation is rendered as
///     text unless it is an allowed HTML tag and <see cref="AllowHtml" /> is enabled or it names one of the
///     <see cref="Components" />.
/// </summary>
public class Trans : I18NextComponentBase
{
    private static readonly Regex TagRegex = new(@"<(/?)([A-Za-z0-9][\w.-]*)\s*(/?)>", RegexOptions.CultureInvariant);

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase) { "br", "hr", "wbr" };

    [Inject]
    private IOptions<I18NextBlazorOptions> Options { get; set; }

    /// <summary>
    ///     The key to be translated, optionally prefixed with a namespace like <c>common:save</c>.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Key { get; set; }

    /// <summary>
    ///     The namespace of the key. Uses the default namespace if not provided.
    /// </summary>
    [Parameter]
    public string Ns { get; set; }

    /// <summary>
    ///     Additional arguments used to translate the key, e.g. <c>new { count = 3 }</c>.
    /// </summary>
    [Parameter]
    public object Args { get; set; }

    /// <summary>
    ///     Renders the allowed HTML tags of <see cref="I18NextBlazorOptions.AllowedHtmlTags" /> contained in the translation.
    ///     Tags with attributes and all other markup are rendered as text.
    /// </summary>
    [Parameter]
    public bool AllowHtml { get; set; }

    /// <summary>
    ///     Renders tags of the translation with the given name, e.g. <c>&lt;link&gt;docs&lt;/link&gt;</c>, with a component.
    ///     The content of the tag is passed to the component.
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<string, RenderFragment<RenderFragment>> Components { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var translation = Ns == null ? T(Key, Args) : I18n.GetFixedT(Ns).T(Key, Args);

        if (!AllowHtml && (Components == null || Components.Count == 0))
        {
            builder.AddContent(0, translation);

            return;
        }

        RenderNodes(builder, Parse(translation).Children);
    }

    private Element Parse(string translation)
    {
        var root = new Element(null);
        var openElements = new List<Element> { root };
        var position = 0;

        foreach (Match match in TagRegex.Matches(translation))
        {
            var name = match.Groups[2].Value;
            var closing = match.Groups[1].Length > 0;
            var selfClosing = match.Groups[3].Length > 0;

            if ((closing && selfClosing) || !IsKnownTag(name))
                continue;

            var openIndex = closing ? openElements.FindLastIndex(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)) : -1;

            if (closing && openIndex < 1)
                continue;

            var parent = openElements[openElements.Count - 1];
            AddText(parent, translation.Substring(position, match.Index - position));
            position = match.Index + match.Length;

            if (closing)
            {
                openElements.RemoveRange(openIndex, openElements.Count - openIndex);

                continue;
            }

            var element = new Element(name);
            parent.Children.Add(element);

            if (!selfClosing && (IsComponent(name) || !VoidElements.Contains(name)))
                openElements.Add(element);
        }

        AddText(openElements[openElements.Count - 1], translation.Substring(position));

        return root;
    }

    private void RenderNodes(RenderTreeBuilder builder, List<object> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is string text)
            {
                builder.AddContent(0, text);

                continue;
            }

            var element = (Element)node;

            if (Components != null && Components.TryGetValue(element.Name, out var component))
            {
                builder.AddContent(1, component(b => RenderNodes(b, element.Children)));

                continue;
            }

            builder.OpenElement(2, element.Name.ToLowerInvariant());
            RenderNodes(builder, element.Children);
            builder.CloseElement();
        }
    }

    private bool IsKnownTag(string name)
    {
        return IsComponent(name) || (AllowHtml && Options.Value.AllowedHtmlTags.Contains(name));
    }

    private bool IsComponent(string name)
    {
        return Components != null && Components.ContainsKey(name);
    }

    private static void AddText(Element parent, string text)
    {
        if (text.Length > 0)
            parent.Children.Add(WebUtility.HtmlDecode(text));
    }

    private sealed class Element(string name)
    {
        public List<object> Children { get; } = [];

        public string Name { get; } = name;
    }
}
