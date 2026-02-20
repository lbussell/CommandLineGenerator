// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

namespace CommandLineGenerator;

/// <summary>
/// Shared attribute name constants used throughout the generator.
/// </summary>
internal static class AttributeNames
{
    public const string CommandClass = "CommandAttribute";
    public const string MapCommandLineOptionsClass = "MapCommandLineOptionsAttribute";
    public const string ArgumentClass = "ArgumentAttribute";
    public const string OptionClass = "OptionAttribute";

    public const string Command = Namespaces.Generated + "." + CommandClass;
    public const string MapCommandLineOptions =
        Namespaces.Generated + "." + MapCommandLineOptionsClass;
    public const string Argument = Namespaces.Generated + "." + ArgumentClass;
    public const string Option = Namespaces.Generated + "." + OptionClass;
}
