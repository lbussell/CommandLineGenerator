// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace CommandLineGenerator;

/// <summary>
/// Shared attribute name constants used throughout the generator.
/// </summary>
internal static class AttributeNames
{
    private const string Ns = Namespaces.Generated;

    public const string CommandLineBindableClass = "CommandLineBindableAttribute";
    public const string CommandLineBindable = Ns + "." + CommandLineBindableClass;

    public const string CommandLineNamingConventionClass = "CommandLineNamingConventionAttribute";
    public const string CommandLineNamingConvention = Ns + "." + CommandLineNamingConventionClass;

    public const string CommandLineOptionClass = "CommandLineOptionAttribute";
    public const string CommandLineOption = Ns + "." + CommandLineOptionClass;

    public const string CommandLineArgumentClass = "CommandLineArgumentAttribute";
    public const string CommandLineArgument = Ns + "." + CommandLineArgumentClass;

    public const string CommandLineIgnoreClass = "CommandLineIgnoreAttribute";
    public const string CommandLineIgnore = Ns + "." + CommandLineIgnoreClass;

    public const string CommandLineBindingContextClass = "CommandLineBindingContext";
    public const string CommandLineBindingContext = Ns + "." + CommandLineBindingContextClass;
}
