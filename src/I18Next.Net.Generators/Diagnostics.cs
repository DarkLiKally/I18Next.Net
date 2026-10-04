using Microsoft.CodeAnalysis;

namespace I18Next.Net.Generators;

internal static class Diagnostics
{
    private const string Category = "I18Next";

    public static readonly DiagnosticDescriptor InvalidFile = new(
        "I18N001", "Invalid translation file", "The translation file '{0}' is invalid: {1}", Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MissingKey = new(
        "I18N002", "Missing translation", "The key '{0}' of the namespace '{1}' is missing in the language '{2}'", Category, DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor UnknownPlaceholder = new(
        "I18N003", "Unknown placeholder", "The key '{0}' of the namespace '{1}' uses the placeholders {2} in the language '{3}' which the source language does not use",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor MissingNamespace = new(
        "I18N004", "Missing namespace", "The namespace '{0}' is missing in the language '{1}'", Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor NoResources = new(
        "I18N005", "No translation files", "No translation files of the source language '{0}' were found in '{1}', add them as AdditionalFiles",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor UnknownKey = new(
        "I18N010", "Unknown translation key", "The translation key '{0}' does not exist in the namespace '{1}'", Category, DiagnosticSeverity.Warning, true);
}
