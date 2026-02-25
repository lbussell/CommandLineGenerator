// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CommandLineGenerator.Parsing;

/// <summary>
/// Parses types referenced by <c>[CommandLineBindable(typeof(T))]</c> into
/// <see cref="BindableTypeInfo"/> models by extracting their public members.
/// </summary>
internal static class BindableTypeParser
{
    /// <summary>
    /// Extracts member metadata from a type for command-line binding.
    /// Supports both property-based classes and positional record parameters.
    /// </summary>
    public static BindableTypeInfo? Parse(
        INamedTypeSymbol typeSymbol,
        NamingConvention namingConvention,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        List<MemberInfo> members = [];
        bool hasPrimaryCtor = false;

        // Check for primary constructor (records and classes with primary constructors)
        IMethodSymbol? primaryCtor = typeSymbol.InstanceConstructors.FirstOrDefault(c =>
            c.Parameters.Length > 0
            && c.DeclaringSyntaxReferences.Any(r =>
            {
                ct.ThrowIfCancellationRequested();
                SyntaxNode syntax = r.GetSyntax(ct);
                return syntax
                    is Microsoft.CodeAnalysis.CSharp.Syntax.RecordDeclarationSyntax
                        or Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax;
            })
        );

        if (primaryCtor is not null)
        {
            hasPrimaryCtor = true;
            foreach (IParameterSymbol param in primaryCtor.Parameters)
            {
                ct.ThrowIfCancellationRequested();
                MemberInfo? member = ExtractFromParameter(param, namingConvention);
                if (member is not null)
                {
                    members.Add(member);
                }
            }
        }
        else
        {
            // Fall back to public properties with public setters
            foreach (IPropertySymbol prop in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                ct.ThrowIfCancellationRequested();

                if (prop.DeclaredAccessibility != Accessibility.Public)
                    continue;
                if (
                    prop.SetMethod is null
                    || prop.SetMethod.DeclaredAccessibility != Accessibility.Public
                )
                    continue;

                MemberInfo? member = ExtractFromProperty(prop, namingConvention, ct);
                if (member is not null)
                {
                    members.Add(member);
                }
            }
        }

        string ns = typeSymbol.ContainingNamespace.ToDisplayString();
        string fullTypeName = Utilities.GetFullyQualifiedName(ns, typeSymbol.Name);

        return new BindableTypeInfo(
            ns,
            typeSymbol.Name,
            fullTypeName,
            [.. members],
            hasPrimaryCtor
        );
    }

    private static bool HasAttribute(IEnumerable<AttributeData> attributes, string fullName)
    {
        return attributes.Any(a => a.AttributeClass?.ToDisplayString() == fullName);
    }

    private static MemberInfo? ExtractFromParameter(
        IParameterSymbol param,
        NamingConvention namingConvention
    )
    {
        ImmutableArray<AttributeData> attributes = param.GetAttributes();

        if (HasAttribute(attributes, AttributeNames.CommandLineIgnore))
            return null;

        bool isArgument = HasAttribute(attributes, AttributeNames.CommandLineArgument);

        string cliName =
            namingConvention == NamingConvention.KebabCase
                ? Utilities.ToKebabCase(param.Name)
                : param.Name;

        bool isBoolean = param.Type.SpecialType == SpecialType.System_Boolean;
        bool isNullable = param.NullableAnnotation == NullableAnnotation.Annotated;
        bool isValueType = param.Type.IsValueType;

        (string? customParserType, string? customParserMethod) = ExtractCustomParser(attributes);

        return new MemberInfo(
            param.Name,
            param.Type.ToDisplayString(),
            cliName,
            isArgument,
            isBoolean,
            isNullable,
            param.HasExplicitDefaultValue,
            param.HasExplicitDefaultValue
                ? Utilities.FormatDefaultValue(param.ExplicitDefaultValue, param.Type)
                : null,
            isValueType,
            customParserType,
            customParserMethod
        );
    }

    private static MemberInfo? ExtractFromProperty(
        IPropertySymbol prop,
        NamingConvention namingConvention,
        CancellationToken ct
    )
    {
        ImmutableArray<AttributeData> attributes = prop.GetAttributes();

        if (HasAttribute(attributes, AttributeNames.CommandLineIgnore))
            return null;

        bool isArgument = HasAttribute(attributes, AttributeNames.CommandLineArgument);

        string cliName =
            namingConvention == NamingConvention.KebabCase
                ? Utilities.ToKebabCase(prop.Name)
                : prop.Name;

        bool isBoolean = prop.Type.SpecialType == SpecialType.System_Boolean;
        bool isNullable = prop.NullableAnnotation == NullableAnnotation.Annotated;
        bool isValueType = prop.Type.IsValueType;

        // Detect property initializers via syntax (e.g. = "Default value")
        (bool hasDefault, string? defaultValue) = GetPropertyDefault(prop, ct);

        (string? customParserType, string? customParserMethod) = ExtractCustomParser(attributes);

        return new MemberInfo(
            prop.Name,
            prop.Type.ToDisplayString(),
            cliName,
            isArgument,
            isBoolean,
            isNullable,
            hasDefault,
            defaultValue,
            isValueType,
            customParserType,
            customParserMethod
        );
    }

    private static (bool HasDefault, string? DefaultValue) GetPropertyDefault(
        IPropertySymbol prop,
        CancellationToken ct
    )
    {
        foreach (SyntaxReference syntaxRef in prop.DeclaringSyntaxReferences)
        {
            ct.ThrowIfCancellationRequested();
            SyntaxNode syntax = syntaxRef.GetSyntax(ct);
            if (
                syntax is Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax propDecl
                && propDecl.Initializer is not null
            )
            {
                string initText = propDecl.Initializer.Value.ToString();
                return (true, initText);
            }
        }
        return (false, null);
    }

    private static (string? TypeName, string? MethodName) ExtractCustomParser(
        ImmutableArray<AttributeData> attributes
    )
    {
        AttributeData? attr = attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == AttributeNames.CommandLineCustomParser
        );

        if (attr is null || attr.ConstructorArguments.Length < 2)
            return (null, null);

        if (
            attr.ConstructorArguments[0].Value is INamedTypeSymbol parserType
            && attr.ConstructorArguments[1].Value is string methodName
        )
        {
            string ns = parserType.ContainingNamespace.ToDisplayString();
            string fullTypeName = Utilities.GetFullyQualifiedName(ns, parserType.Name);
            return (fullTypeName, methodName);
        }

        return (null, null);
    }
}
