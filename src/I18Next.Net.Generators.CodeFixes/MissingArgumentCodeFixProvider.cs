using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace I18Next.Net.Generators.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingArgumentCodeFixProvider))]
[Shared]
public sealed class MissingArgumentCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "I18N011.Rename";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("I18N011");

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not AnonymousObjectCreationExpressionSyntax creation)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(DiagnosticProperties.Member, out var member)
                || !diagnostic.Properties.TryGetValue(DiagnosticProperties.Placeholder, out var placeholder))
                continue;

            context.RegisterCodeFix(CodeAction.Create($"Rename '{member}' to '{placeholder}'",
                c => RenameAsync(document, root, creation, member, placeholder, c), EquivalenceKey), diagnostic);
        }
    }

    private static async Task<Document> RenameAsync(Document document, SyntaxNode root, AnonymousObjectCreationExpressionSyntax creation, string member,
        string placeholder, CancellationToken cancellationToken)
    {
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var declarator = creation.Initializers.FirstOrDefault(i => model?.GetDeclaredSymbol(i, cancellationToken)?.Name == member);

        if (declarator == null)
            return document;

        var identifier = SyntaxFacts.GetKeywordKind(placeholder) == SyntaxKind.None ? placeholder : "@" + placeholder;
        var name = SyntaxFactory.IdentifierName(SyntaxFactory.ParseToken(identifier));
        AnonymousObjectMemberDeclaratorSyntax renamed;

        if (declarator.NameEquals != null)
        {
            renamed = declarator.WithNameEquals(declarator.NameEquals.WithName(name.WithTriviaFrom(declarator.NameEquals.Name)));
        }
        else
        {
            var nameEquals = SyntaxFactory.NameEquals(name.WithTrailingTrivia(SyntaxFactory.Space),
                SyntaxFactory.Token(SyntaxKind.EqualsToken).WithTrailingTrivia(SyntaxFactory.Space));

            renamed = SyntaxFactory.AnonymousObjectMemberDeclarator(nameEquals, declarator.Expression.WithoutLeadingTrivia())
                .WithLeadingTrivia(declarator.GetLeadingTrivia());
        }

        return document.WithSyntaxRoot(root.ReplaceNode(declarator, renamed));
    }
}
