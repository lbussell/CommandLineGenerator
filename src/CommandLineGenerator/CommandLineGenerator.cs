// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
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

        // Discover command classes with [CommandLineHandler]
        IncrementalValuesProvider<CommandHandlerInfo> commandHandlers = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                AttributeNames.CommandLineHandler,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => CommandHandlerParser.Parse(ctx, ct)
            )
            .Where(static c => c is not null)!;

        // Collect all handlers, group by binding context, and emit one file per context
        IncrementalValueProvider<ImmutableArray<CommandHandlerInfo>> allHandlers =
            commandHandlers.Collect();

        context.RegisterSourceOutput(
            allHandlers,
            static (spc, handlers) =>
            {
                if (handlers.IsEmpty)
                    return;

                // Group handlers by their binding context
                Dictionary<string, List<CommandHandlerInfo>> groups = [];
                foreach (CommandHandlerInfo handler in handlers)
                {
                    string key = $"{handler.ContextNamespace}.{handler.ContextClassName}";
                    if (!groups.TryGetValue(key, out List<CommandHandlerInfo>? list))
                    {
                        list = [];
                        groups[key] = list;
                    }
                    list.Add(handler);
                }

                foreach (KeyValuePair<string, List<CommandHandlerInfo>> group in groups)
                {
                    CommandHandlerInfo first = group.Value[0];
                    HandlerContextInfo contextInfo = new HandlerContextInfo(
                        first.ContextNamespace,
                        first.ContextClassName,
                        [.. group.Value]
                    );

                    string source = CommandHandlerEmitter.Emit(contextInfo);
                    spc.AddSource(
                        $"{contextInfo.ClassName}Handlers.g.cs",
                        SourceText.From(source, Encoding.UTF8)
                    );
                }
            }
        );
    }
}
