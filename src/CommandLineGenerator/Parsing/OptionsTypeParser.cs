// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommandLineGenerator.Parsing;

/// <summary>
/// Parses types decorated with [MapCommandLineOptions] into OptionsTypeInfo models.
/// </summary>
internal static class OptionsTypeParser
{
    /// <summary>
    /// Extracts metadata from a type decorated with [MapCommandLineOptions].
    /// </summary>
    public static OptionsTypeInfo? Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        AttributeData? mapAttr = ctx.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == AttributeNames.MapCommandLineOptions
        );

        if (mapAttr is null)
            return null;

        // Check for UseKebabCase property
        bool useKebabCase = mapAttr
            .NamedArguments.FirstOrDefault(a => a.Key == "UseKebabCase")
            .Value.Value
            is bool val
            ? val
            : true;

        // Get members from primary constructor parameters (for records) or properties
        List<OptionsMemberInfo> members = [];

        // Check for primary constructor (records and classes with primary constructors)
        IMethodSymbol? primaryCtor = typeSymbol.InstanceConstructors.FirstOrDefault(c =>
            c.Parameters.Length > 0
            && c.DeclaringSyntaxReferences.Any(r =>
                r.GetSyntax(ct) is RecordDeclarationSyntax or ClassDeclarationSyntax
            )
        );

        if (primaryCtor is not null)
        {
            foreach (IParameterSymbol param in primaryCtor.Parameters)
            {
                OptionsMemberInfo memberInfo = ExtractMemberInfo(param, useKebabCase);
                members.Add(memberInfo);
            }
        }
        else
        {
            // Fall back to public properties with public setters or init
            foreach (IPropertySymbol prop in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (prop.DeclaredAccessibility != Accessibility.Public)
                    continue;
                if (
                    prop.SetMethod is null
                    || prop.SetMethod.DeclaredAccessibility != Accessibility.Public
                )
                    continue;

                OptionsMemberInfo memberInfo = ExtractMemberInfoFromProperty(prop, useKebabCase);
                members.Add(memberInfo);
            }
        }

        string ns = typeSymbol.ContainingNamespace.ToDisplayString();
        string fullTypeName = Utilities.GetFullyQualifiedName(ns, typeSymbol.Name);

        return new OptionsTypeInfo(
            ns,
            typeSymbol.Name,
            fullTypeName,
            [.. members],
            useKebabCase
        );
    }

    private static (
        bool isArgument,
        string? explicitName,
        string? alias,
        string? description
    ) ExtractAttributeInfo(IEnumerable<AttributeData> attributes)
    {
        bool isArgument = false;
        string? explicitName = null;
        string? alias = null;
        string? description = null;

        foreach (AttributeData attr in attributes)
        {
            string? attrName = attr.AttributeClass?.ToDisplayString();

            if (attrName == AttributeNames.Argument)
            {
                isArgument = true;
                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Name")
                        explicitName = namedArg.Value.Value as string;
                    if (namedArg.Key == "Description")
                        description = namedArg.Value.Value as string;
                }
            }
            else if (attrName == AttributeNames.Option)
            {
                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Name")
                        explicitName = namedArg.Value.Value as string;
                    if (namedArg.Key == "Alias")
                        alias = namedArg.Value.Value as string;
                    if (namedArg.Key == "Description")
                        description = namedArg.Value.Value as string;
                }
            }
        }

        return (isArgument, explicitName, alias, description);
    }

    private static OptionsMemberInfo ExtractMemberInfo(IParameterSymbol param, bool useKebabCase)
    {
        (bool isArgument, string? explicitName, string? alias, string? description) = ExtractAttributeInfo(
            param.GetAttributes()
        );

        string cliName =
            explicitName ?? (useKebabCase ? Utilities.ToKebabCase(param.Name) : param.Name);
        bool isBoolean = param.Type.SpecialType == SpecialType.System_Boolean;
        bool isValueType = param.Type.IsValueType;

        return new OptionsMemberInfo(
            param.Name,
            param.Type.ToDisplayString(),
            cliName,
            isArgument,
            isBoolean,
            param.NullableAnnotation == NullableAnnotation.Annotated,
            param.HasExplicitDefaultValue,
            param.HasExplicitDefaultValue
                ? Utilities.FormatDefaultValue(param.ExplicitDefaultValue, param.Type)
                : null,
            alias,
            description,
            isValueType
        );
    }

    private static OptionsMemberInfo ExtractMemberInfoFromProperty(
        IPropertySymbol prop,
        bool useKebabCase
    )
    {
        (bool isArgument, string? explicitName, string? alias, string? description) = ExtractAttributeInfo(
            prop.GetAttributes()
        );

        string cliName = explicitName ?? (useKebabCase ? Utilities.ToKebabCase(prop.Name) : prop.Name);
        bool isBoolean = prop.Type.SpecialType == SpecialType.System_Boolean;
        bool isValueType = prop.Type.IsValueType;

        // Properties don't have default values in the same way; we'd need to analyze initializers
        return new OptionsMemberInfo(
            prop.Name,
            prop.Type.ToDisplayString(),
            cliName,
            isArgument,
            isBoolean,
            prop.NullableAnnotation == NullableAnnotation.Annotated,
            false,
            null,
            alias,
            description,
            isValueType
        );
    }
}
