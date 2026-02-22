// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using CommandLineGenerator;
using CommandLineHosting;
using Microsoft.Extensions.Hosting;

// Set up commands
RootCommand rootCommand = [];
rootCommand.AddOptions<RootCommandOptions>();
rootCommand.SetAction(parseResult =>
{
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

// Set up hosting
HostApplicationBuilder builder = Host.CreateApplicationBuilder();
CommandLineApplicationBuilder commandLineBuilder = builder.WithCommandLine();
commandLineBuilder.AddRootCommand(rootCommand);
CommandLineHost commandLineHost = commandLineBuilder.Build(args);
await commandLineHost.RunAsync();

internal sealed record RootCommandOptions(bool Verbose = false);

internal sealed record ListCommandOptions(string Path);

[CommandLineBindable(typeof(RootCommandOptions))]
[CommandLineBindable(typeof(ListCommandOptions))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal static partial class CommandLineContext { }
