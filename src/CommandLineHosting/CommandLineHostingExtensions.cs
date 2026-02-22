// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Hosting;

namespace CommandLineHosting;

/// <summary>
/// Extension methods for integrating System.CommandLine with the hosting infrastructure.
/// </summary>
public static class CommandLineHostingExtensions
{
    /// <summary>
    /// Wraps the <see cref="HostApplicationBuilder"/> with a
    /// <see cref="CommandLineApplicationBuilder"/> that adds System.CommandLine integration.
    /// </summary>
    public static CommandLineApplicationBuilder WithCommandLine(
        this HostApplicationBuilder builder
    ) => new CommandLineApplicationBuilder(builder);
}
