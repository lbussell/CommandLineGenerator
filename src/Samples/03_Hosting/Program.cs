// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using CommandLineGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

/// <summary>
/// A builder that wraps <see cref="HostApplicationBuilder"/> and adds
/// System.CommandLine integration.
/// </summary>
internal interface ICommandLineApplicationBuilder : IHostApplicationBuilder
{
    ICommandLineApplicationBuilder AddRootCommand(RootCommand command);
    CommandLineHost Build(string[] args);
}

internal sealed class CommandLineApplicationBuilder(HostApplicationBuilder innerBuilder)
    : ICommandLineApplicationBuilder
{
    private readonly HostApplicationBuilder _innerBuilder = innerBuilder;
    private RootCommand? _rootCommand;

    // IHostApplicationBuilder forwarding
    public IDictionary<object, object> Properties =>
        ((IHostApplicationBuilder)_innerBuilder).Properties;
    public IConfigurationManager Configuration => _innerBuilder.Configuration;
    public IHostEnvironment Environment => _innerBuilder.Environment;
    public ILoggingBuilder Logging => _innerBuilder.Logging;
    public IMetricsBuilder Metrics => _innerBuilder.Metrics;
    public IServiceCollection Services => _innerBuilder.Services;

    public void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null
    )
        where TContainerBuilder : notnull
    {
        _innerBuilder.ConfigureContainer(factory, configure);
    }

    public ICommandLineApplicationBuilder AddRootCommand(RootCommand command)
    {
        _rootCommand = command;
        return this;
    }

    public CommandLineHost Build(string[] args)
    {
        if (_rootCommand is null)
            throw new InvalidOperationException(
                "A RootCommand must be added before building. Call AddRootCommand first."
            );

        ParseResult parseResult = _rootCommand.Parse(args);
        _innerBuilder.Services.AddSingleton(parseResult);

        IHost innerHost = _innerBuilder.Build();
        return new CommandLineHost(innerHost);
    }
}

/// <summary>
/// An <see cref="IHost"/> wrapper that starts the inner host, invokes the parsed
/// System.CommandLine <see cref="ParseResult"/>, then gracefully stops the host.
/// </summary>
internal sealed class CommandLineHost(IHost innerHost) : IHost, IAsyncDisposable
{
    private readonly IHost _innerHost = innerHost;

    public IServiceProvider Services => _innerHost.Services;

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _innerHost.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        _innerHost.StopAsync(cancellationToken);

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
    public static CommandLineApplicationBuilder WithCommandLine(
        this HostApplicationBuilder builder
    ) => new CommandLineApplicationBuilder(builder);
}

internal sealed record RootCommandOptions(bool Verbose = false);

internal sealed record ListCommandOptions(string Path);

[CommandLineBindable(typeof(RootCommandOptions))]
[CommandLineBindable(typeof(ListCommandOptions))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal static partial class CommandLineContext { }
