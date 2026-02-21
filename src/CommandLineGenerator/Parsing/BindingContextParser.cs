// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.CodeAnalysis;

namespace CommandLineGenerator.Parsing;

/// <summary>
/// Parses classes that derive from <c>CommandLineBindingContext</c> and are decorated
/// with <c>[CommandLineBindable]</c> and optionally <c>[CommandLineNamingConvention]</c>.
/// </summary>
internal static class BindingContextParser
{
    /// <summary>
    /// Extracts metadata from a binding context class.
    /// </summary>
    public static BindingContextInfo? Parse(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
            return null;

        // Must derive from CommandLineBindingContext
        if (!DerivesFrom(classSymbol, AttributeNames.CommandLineBindingContext))
            return null;

        // Extract naming convention
        NamingConvention namingConvention = NamingConvention.KebabCase;
        foreach (AttributeData attr in classSymbol.GetAttributes())
        {
            ct.ThrowIfCancellationRequested();
            if (
                attr.AttributeClass?.ToDisplayString() == AttributeNames.CommandLineNamingConvention
            )
            {
                if (
                    attr.ConstructorArguments.Length > 0
                    && attr.ConstructorArguments[0].Value is int val
                )
                {
                    namingConvention = (NamingConvention)val;
                }
            }
        }

        // Extract all [CommandLineBindable(typeof(T))] attributes
        List<BindableTypeInfo> bindableTypes = [];
        foreach (AttributeData attr in ctx.Attributes)
        {
            ct.ThrowIfCancellationRequested();
            if (attr.AttributeClass?.ToDisplayString() != AttributeNames.CommandLineBindable)
                continue;

            if (
                attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is INamedTypeSymbol targetType
            )
            {
                BindableTypeInfo? typeInfo = BindableTypeParser.Parse(
                    targetType,
                    namingConvention,
                    ct
                );
                if (typeInfo is not null)
                {
                    bindableTypes.Add(typeInfo);
                }
            }
        }

        if (bindableTypes.Count == 0)
            return null;

        string ns = classSymbol.ContainingNamespace.ToDisplayString();

        return new BindingContextInfo(ns, classSymbol.Name, namingConvention, [.. bindableTypes]);
    }

    private static bool DerivesFrom(INamedTypeSymbol symbol, string baseTypeFullName)
    {
        INamedTypeSymbol? current = symbol.BaseType;
        while (current is not null)
        {
            if (current.ToDisplayString() == baseTypeFullName)
                return true;
            current = current.BaseType;
        }
        return false;
    }
}
