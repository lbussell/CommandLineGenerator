// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using CommandLineGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Set up commands
RootCommand rootCommand = new RootCommand();
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
builder.AddCommandLine(rootCommand, args);

IHost host = builder.Build();
await host.StartAsync();
ParseResult parseResult = host.Services.GetRequiredService<ParseResult>();
await parseResult.InvokeAsync();
await host.StopAsync();

internal static class CommandLineHostingExtensions
{
    public static HostApplicationBuilder AddCommandLine(
        this HostApplicationBuilder builder,
        RootCommand rootCommand,
        string[] args
    )
    {
        ParseResult parseResult = rootCommand.Parse(args);
        builder.Services.AddSingleton(parseResult);
        return builder;
    }
}

internal sealed record RootCommandOptions(bool Verbose = false);

internal sealed record ListCommandOptions(string Path);

[CommandLineBindable(typeof(RootCommandOptions))]
[CommandLineBindable(typeof(ListCommandOptions))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal sealed partial class CommandLineContext : CommandLineBindingContext { }
