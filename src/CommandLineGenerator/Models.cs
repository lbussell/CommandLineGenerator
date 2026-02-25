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

/// <summary>
/// Represents metadata about a single <c>[Command]</c>-decorated method
/// within a command handler class.
/// </summary>
internal sealed record CommandMethodInfo(
    string MethodName,
    string CommandName,
    string? OptionsTypeName,
    string? OptionsFullTypeName,
    bool IsAsync,
    bool ReturnsExitCode,
    bool AcceptsCancellationToken
);

/// <summary>
/// Represents metadata about a command handler class decorated with
/// <c>[CommandLineHandler]</c>. A handler class may contain multiple
/// <c>[Command]</c>-decorated methods forming a command group.
/// </summary>
internal sealed record CommandHandlerInfo(
    string TypeName,
    string FullTypeName,
    string GroupName,
    ImmutableArray<CommandMethodInfo> Commands,
    string ContextClassName,
    string ContextNamespace
);

/// <summary>
/// Represents a group of command handlers that share the same binding context,
/// used to generate a single extensions class with <c>Add&lt;T&gt;</c>.
/// </summary>
internal sealed record HandlerContextInfo(
    string Namespace,
    string ClassName,
    ImmutableArray<CommandHandlerInfo> Handlers
);
