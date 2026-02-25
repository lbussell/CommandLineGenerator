// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using System.CommandLine;
using CommandLineGenerator;
using CommandLineHosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
CommandLineApplicationBuilder commandLineBuilder = builder.WithCommandLine();

commandLineBuilder.Services.AddSingleton<MyService>();
commandLineBuilder.Add<ExampleCommand>();
CommandLineHost commandLineHost = commandLineBuilder.Build(args);
await commandLineHost.RunAsync();

internal sealed record ExampleCommandOptions([CommandLineArgument] string Name);

[CommandLineHandler(BindingContext = typeof(CommandLineContext))]
internal sealed class ExampleCommand(MyService myService)
{
    [Command]
    public void Execute(ExampleCommandOptions options)
    {
        myService.SayHello(options.Name);
    }
}

internal sealed class MyService
{
    public void SayHello(string name) => Console.WriteLine($"Hello, {name}!");
}

[CommandLineBindable(typeof(ExampleCommandOptions))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal static partial class CommandLineContext { }
