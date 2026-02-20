// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.Text;
using Microsoft.CodeAnalysis;

namespace CommandLineGenerator;

/// <summary>
/// Utility functions for code generation.
/// </summary>
internal static class Utilities
{
    /// <summary>
    /// Converts a PascalCase name to kebab-case.
    /// </summary>
    public static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Insert hyphen before each uppercase letter (except the first), then lowercase
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c))
            {
                result.Append('-');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }

    /// <summary>
    /// Escapes a string for use in generated C# string literals.
    /// </summary>
    public static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Gets a fully qualified type name with global:: prefix.
    /// </summary>
    public static string GetFullyQualifiedName(string? ns, string typeName)
    {
        bool hasNamespace = !string.IsNullOrEmpty(ns) && ns != Namespaces.Global;
        return hasNamespace ? $"global::{ns}.{typeName}" : $"global::{typeName}";
    }

    /// <summary>
    /// Gets the fully qualified mapper class name for an options type.
    /// </summary>
    public static string GetMapperName(OptionsTypeInfo optType)
    {
        return GetFullyQualifiedName(optType.Namespace, $"{optType.TypeName}Mapper");
    }

    /// <summary>
    /// Formats a default value for use in generated code.
    /// </summary>
    public static string FormatDefaultValue(object? value, ITypeSymbol type)
    {
        if (value is null)
            return "null";

        if (type.SpecialType == SpecialType.System_String)
            return $"\"{value}\"";

        if (type.SpecialType == SpecialType.System_Boolean)
            return value.ToString()!.ToLowerInvariant();

        return value.ToString()!;
    }
}
