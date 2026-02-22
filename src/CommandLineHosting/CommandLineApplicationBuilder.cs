// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommandLineHosting;

/// <inheritdoc cref="ICommandLineApplicationBuilder"/>
public sealed class CommandLineApplicationBuilder(HostApplicationBuilder innerBuilder)
    : ICommandLineApplicationBuilder
{
    private readonly HostApplicationBuilder _innerBuilder = innerBuilder;
    private RootCommand? _rootCommand;

    /// <inheritdoc/>
    public IDictionary<object, object> Properties =>
        ((IHostApplicationBuilder)_innerBuilder).Properties;

    /// <inheritdoc/>
    public IConfigurationManager Configuration => _innerBuilder.Configuration;

    /// <inheritdoc/>
    public IHostEnvironment Environment => _innerBuilder.Environment;

    /// <inheritdoc/>
    public ILoggingBuilder Logging => _innerBuilder.Logging;

    /// <inheritdoc/>
    public IMetricsBuilder Metrics => _innerBuilder.Metrics;

    /// <inheritdoc/>
    public IServiceCollection Services => _innerBuilder.Services;

    /// <inheritdoc/>
    public void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null
    )
        where TContainerBuilder : notnull
    {
        _innerBuilder.ConfigureContainer(factory, configure);
    }

    /// <inheritdoc/>
    public ICommandLineApplicationBuilder AddRootCommand(RootCommand command)
    {
        _rootCommand = command;
        return this;
    }

    /// <inheritdoc/>
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
