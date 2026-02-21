// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace CommandLineGenerator;

/// <summary>
/// The naming convention to use for generated CLI names.
/// Values must match the generated <c>CommandLineNamingConvention</c> enum.
/// </summary>
internal enum NamingConvention
{
    /// <summary>
    /// Convert PascalCase property names to kebab-case (e.g. "MyOption" → "my-option").
    /// </summary>
    KebabCase = 0,
}

/// <summary>
/// Represents metadata about a binding context class decorated with
/// <c>[CommandLineBindable]</c> attributes.
/// </summary>
internal sealed record BindingContextInfo(
    string Namespace,
    string ClassName,
    NamingConvention NamingConvention,
    ImmutableArray<BindableTypeInfo> BindableTypes
);

/// <summary>
/// Represents metadata about a type that can be bound from command-line arguments.
/// </summary>
internal sealed record BindableTypeInfo(
    string Namespace,
    string TypeName,
    string FullTypeName,
    ImmutableArray<MemberInfo> Members,
    bool HasPrimaryConstructor
);

/// <summary>
/// Represents metadata about a single member (property or constructor parameter)
/// that maps to a command-line option or argument.
/// </summary>
internal sealed record MemberInfo(
    string PropertyName,
    string TypeName,
    string CliName,
    bool IsArgument,
    bool IsBoolean,
    bool IsNullable,
    bool HasDefaultValue,
    string? DefaultValue,
    bool IsValueType
);
