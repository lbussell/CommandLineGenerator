// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using CommandLineGenerator;
using Microsoft.Extensions.DependencyInjection;
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
builder.AddCommandLine(rootCommand, args);

await using CommandLineHost commandLineHost = new CommandLineHost(builder.Build());
await commandLineHost.RunAsync();

/// <summary>
/// An <see cref="IHost"/> wrapper that starts the inner host, invokes the parsed
/// System.CommandLine <see cref="ParseResult"/>, then gracefully stops the host.
/// </summary>
internal sealed class CommandLineHost : IHost, IAsyncDisposable
{
    private readonly IHost _innerHost;

    public CommandLineHost(IHost innerHost)
    {
        _innerHost = innerHost;
    }

    public IServiceProvider Services => _innerHost.Services;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _innerHost.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _innerHost.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Starts the host, invokes the parsed command, then stops the host.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await StartAsync(cancellationToken);
        try
        {
            ParseResult parseResult = Services.GetRequiredService<ParseResult>();
            await parseResult.InvokeAsync(cancellationToken: cancellationToken);
        }
        finally
        {
            await StopAsync(cancellationToken);
        }
    }

    public void Dispose() => _innerHost.Dispose();

    public async ValueTask DisposeAsync()
    {
        if (_innerHost is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _innerHost.Dispose();
        }
    }
}

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
