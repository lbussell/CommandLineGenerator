// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using Microsoft.Extensions.Hosting;

namespace CommandLineHosting;

/// <summary>
/// A builder that wraps <see cref="HostApplicationBuilder"/> and adds
/// System.CommandLine integration.
/// </summary>
public interface ICommandLineApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Sets the root command for the application.
    /// </summary>
    ICommandLineApplicationBuilder AddRootCommand(RootCommand command);

    /// <summary>
    /// Parses the command-line arguments and builds the <see cref="CommandLineHost"/>.
    /// </summary>
    CommandLineHost Build(string[] args);
}
