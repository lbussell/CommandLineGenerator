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
/// Incremental source generator that generates CLI infrastructure from attributed classes.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CommandLineGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Always generate the base infrastructure (attributes)
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource(
                "Attributes.g.cs",
                SourceText.From(AttributesEmitter.Emit(), Encoding.UTF8)
            );
        });

        // Discover options types with [MapCommandLineOptions]
        IncrementalValueProvider<ImmutableArray<OptionsTypeInfo?>> optionsTypes = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                AttributeNames.MapCommandLineOptions,
                predicate: static (node, _) =>
                    node is RecordDeclarationSyntax or ClassDeclarationSyntax,
                transform: static (ctx, ct) => OptionsTypeParser.Parse(ctx, ct)
            )
            .Where(static o => o is not null)
            .Collect()!;

        // Discover command methods with [Command]
        IncrementalValueProvider<ImmutableArray<CommandMethodInfo?>> commandMethods = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                AttributeNames.Command,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => CommandMethodParser.Parse(ctx, ct)
            )
            .Where(static c => c is not null)
            .Collect()!;

        // Combine options types with command methods to generate ConsoleApp
        IncrementalValueProvider<(ImmutableArray<CommandMethodInfo?> Left, ImmutableArray<OptionsTypeInfo?> Right)> combined = commandMethods.Combine(optionsTypes);

        context.RegisterSourceOutput(
            combined,
            static (spc, data) =>
            {
                ImmutableArray<CommandMethodInfo?> commands = data.Left;
                ImmutableArray<OptionsTypeInfo?> options = data.Right;
                Dictionary<string, OptionsTypeInfo> optionsDict = options
                    .Where(o => o is not null)
                    .ToDictionary(o => o!.FullTypeName, o => o!);

                string? consoleAppSource = ConsoleAppEmitter.Emit(commands!, optionsDict);
                if (consoleAppSource is not null)
                {
                    spc.AddSource(
                        "ConsoleApp.g.cs",
                        SourceText.From(consoleAppSource, Encoding.UTF8)
                    );
                }
            }
        );

        // Generate mapper classes for each options type
        context.RegisterSourceOutput(
            optionsTypes,
            static (spc, optionsTypes) =>
            {
                foreach (OptionsTypeInfo? opt in optionsTypes)
                {
                    if (opt is not null)
                    {
                        string mapperSource = OptionsMapperEmitter.Emit(opt);
                        spc.AddSource(
                            $"{opt.TypeName}Mapper.g.cs",
                            SourceText.From(mapperSource, Encoding.UTF8)
                        );
                    }
                }

                // Generate CommandExtensions with AddOptions<T>() for all options types
                string? extensionsSource = CommandExtensionsEmitter.Emit(optionsTypes);
                if (extensionsSource is not null)
                {
                    spc.AddSource(
                        "CommandExtensions.g.cs",
                        SourceText.From(extensionsSource, Encoding.UTF8)
                    );
                }
            }
        );
    }
}
