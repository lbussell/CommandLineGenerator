// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text;
using CommandLineGenerator.Emitters;
using CommandLineGenerator.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("CommandLineGenerator.Tests")]

namespace CommandLineGenerator;

/// <summary>
/// Incremental source generator that generates CLI binding infrastructure from
/// <c>[CommandLineBindable]</c>-decorated binding context classes.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CommandLineGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Always generate the base infrastructure (attributes and base class)
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource(
                "Attributes.g.cs",
                SourceText.From(AttributesEmitter.Emit(), Encoding.UTF8)
            );
        });

        // Discover binding context classes with [CommandLineBindable]
        IncrementalValuesProvider<BindingContextInfo?> bindingContexts = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                AttributeNames.CommandLineBindable,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => BindingContextParser.Parse(ctx, ct)
            )
            .Where(static c => c is not null);

        // Generate partial class for each binding context
        context.RegisterSourceOutput(
            bindingContexts,
            static (spc, ctx) =>
            {
                if (ctx is not null)
                {
                    string source = BindingContextEmitter.Emit(ctx);
                    spc.AddSource($"{ctx.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        );
    }
}
