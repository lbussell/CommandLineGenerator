// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommandLineHosting;

/// <summary>
/// An <see cref="IHost"/> wrapper that starts the inner host, invokes the parsed
/// System.CommandLine <see cref="ParseResult"/>, then gracefully stops the host.
/// </summary>
public sealed class CommandLineHost(IHost innerHost) : IHost, IAsyncDisposable
{
    private readonly IHost _innerHost = innerHost;

    /// <inheritdoc/>
    public IServiceProvider Services => _innerHost.Services;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _innerHost.StartAsync(cancellationToken);

    /// <inheritdoc/>
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
            CommandLineHostConfiguration config = new CommandLineHostConfiguration
            {
                ServiceProvider = Services,
            };
            await parseResult.InvokeAsync(
                configuration: config,
                cancellationToken: cancellationToken
            );
        }
        finally
        {
            await StopAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _innerHost.Dispose();

    /// <inheritdoc/>
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
