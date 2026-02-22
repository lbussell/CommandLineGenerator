// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using CommandLineGenerator;

RootCommand myCommand = new RootCommand(
    description: "Basic command line app without hosting or dependency injection."
);

// Add options and arguments automatically based on MyOptionsClass.
myCommand.AddOptions<MyOptionsClass>();

myCommand.SetAction(
    (parseResult, ct) =>
    {
        // Parse the command line arguments into an instance of MyOptionsClass.
        MyOptionsClass options = CommandLineContext.Parse<MyOptionsClass>(parseResult);
        Console.WriteLine(options);
        return Task.CompletedTask;
    }
);
myCommand.Parse(args).Invoke();

internal sealed class MyOptionsClass
{
    // Public properties are automatically treated as options.
    public required string RequiredStringOption { get; set; }

    // You can also explicitly mark properties as options.
    [CommandLineOption]
    public required string ExplicitStringOption { get; set; }

    // Arguments must be explicitly marked with [CommandLineArgument].
    // Arguments are positional.
    [CommandLineArgument]
    public required string RequiredStringArgument { get; set; }

    // Properties with default values are automatically treated as optional options.
    public string OptionalDefaultStringOption { get; set; } = "Default value";

    // Nullable properties are also treated as optional.
    public string? OptionalNullableStringOption { get; set; } = null;

    // Properties marked with [CommandLineIgnore] will be completely ignored.
    [CommandLineIgnore]
    public string IgnoredProperty { get; set; } = "This property will be ignored.";
}

internal sealed record MyOptionsRecord(
    string RequiredStringOption,
    [CommandLineOption] string ExplicitStringOption,
    [CommandLineArgument] string RequiredStringArgument,
    string OptionalDefaultStringOption = "Default value",
    string? OptionalNullableStringOption = null,
    [CommandLineIgnore] string IgnoredProperty = "This property will be ignored."
);

// Generate binding code for MyOptionsClass and MyOptionsRecord
[CommandLineBindable(typeof(MyOptionsClass))]
[CommandLineBindable(typeof(MyOptionsRecord))]
// Use kebab-case for command line options (e.g. "--required-string-option")
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal static partial class CommandLineContext { }
