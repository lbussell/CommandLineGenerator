// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

// Simple CLI without Microsoft.Extensions.Hosting
// Uses generated mapper directly with System.CommandLine

using System.CommandLine;
using CommandLineGenerator;

RootCommand myCommand = new RootCommand(
    description: "Command line app without hosting or dependency injection."
);
myCommand.AddOptions<CommandLineOptions>();
myCommand.SetAction(
    (parseResult, ct) =>
    {
        CommandLineOptions options = CommandLineOptionsMapper.Parse(parseResult);
        Console.WriteLine($"{options.Greeting}, {options.Name}!");
        return Task.CompletedTask;
    }
);

return await myCommand.Parse(args).InvokeAsync();

[MapCommandLineOptions]
internal sealed record CommandLineOptions([Argument] string Name, string Greeting = "Hello");
