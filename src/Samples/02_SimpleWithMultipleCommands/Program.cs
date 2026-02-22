// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using CommandLineGenerator;

RootCommand rootCommand = [];
rootCommand.AddOptions<RootCommandOptions>();
rootCommand.SetAction(parseResult =>
{
    // Parse the command line arguments into an instance of MyOptionsClass.
    RootCommandOptions options = CommandLineContext.Parse<RootCommandOptions>(parseResult);
    Console.WriteLine($"You passed in these options: {options}");
});

Command listCommand = new Command("list");
listCommand.AddOptions<ListCommandOptions>();
listCommand.SetAction(parseResult =>
{
    ListCommandOptions options = CommandLineContext.Parse<ListCommandOptions>(parseResult);
    Console.WriteLine($"You passed in these options: {options}");
});

rootCommand.Add(listCommand);
rootCommand.Parse(args).Invoke();

internal sealed record RootCommandOptions(bool Verbose = false);

internal sealed record ListCommandOptions(string Path);

[CommandLineBindable(typeof(RootCommandOptions))]
[CommandLineBindable(typeof(ListCommandOptions))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal static partial class CommandLineContext { }
