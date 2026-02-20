// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CommandLineGenerator.Parsing;

/// <summary>
/// Parses methods decorated with [Command] into CommandMethodInfo models.
/// </summary>
internal static class CommandMethodParser
{
    /// <summary>
    /// Extracts metadata from a method decorated with [Command].
    /// </summary>
    public static CommandMethodInfo? Parse(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.TargetSymbol is not IMethodSymbol methodSymbol)
            return null;

        AttributeData? commandAttr = ctx.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == AttributeNames.Command
        );

        if (commandAttr is null)
            return null;

        string commandName =
            commandAttr.ConstructorArguments.Length > 0
                ? commandAttr.ConstructorArguments[0].Value as string ?? ""
                : "";

        string? description = null;
        foreach (KeyValuePair<string, TypedConstant> namedArg in commandAttr.NamedArguments)
        {
            if (namedArg.Key == "Description")
                description = namedArg.Value.Value as string;
        }

        INamedTypeSymbol containingType = methodSymbol.ContainingType;
        string containingNs = containingType.ContainingNamespace.ToDisplayString();
        string containingFullName = Utilities.GetFullyQualifiedName(containingNs, containingType.Name);

        // Extract parameter types (should be options types)
        ImmutableArray<string> optionsTypeNames = [.. methodSymbol
            .Parameters.Select(p =>
            {
                string ns = p.Type.ContainingNamespace?.ToDisplayString() ?? "";
                return Utilities.GetFullyQualifiedName(ns, p.Type.Name);
            })];

        // Check if method returns Task (async) or void (sync)
        ITypeSymbol returnType = methodSymbol.ReturnType;
        bool isAsync =
            returnType.Name == "Task"
            && returnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";

        return new CommandMethodInfo(
            containingNs,
            containingType.Name,
            containingFullName,
            methodSymbol.Name,
            commandName,
            description,
            optionsTypeNames,
            isAsync
        );
    }
}
