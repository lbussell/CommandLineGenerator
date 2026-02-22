// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;

namespace CommandLineHosting;

/// <summary>
/// An <see cref="InvocationConfiguration"/> that carries an <see cref="IServiceProvider"/>
/// for dependency injection during command execution.
/// </summary>
public sealed class CommandLineHostConfiguration : InvocationConfiguration
{
    /// <summary>
    /// Gets the service provider for resolving dependencies during command execution.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }
}
