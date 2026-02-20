// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

// CLI with Microsoft.Extensions.Hosting and Dependency Injection
// Demonstrates constructor injection in command classes

using CommandLineGenerator;
using Microsoft.Extensions.DependencyInjection;

ConsoleAppBuilder builder = ConsoleApp.CreateBuilder(args);

// Register services for dependency injection
builder.Host.Services.AddSingleton<GreetingService>();
builder.Host.Services.AddSingleton<FarewellService>();

builder.AddCommand<Commands>();

return await builder.RunAsync();

[MapCommandLineOptions]
internal record HelloOptions([Argument] string Name, string Greeting = "Hello");

[MapCommandLineOptions]
internal record GoodbyeOptions([Argument] string Name);

[MapCommandLineOptions]
internal record VerboseOptions(
    [Option(Alias = "-v", Description = "Enable verbose output")] bool Verbose = false
);

[MapCommandLineOptions]
internal record InfoOptions(
    [Option(Description = "Show version information")] bool Version = false
);

internal sealed class Commands(GreetingService greetingService, FarewellService farewellService)
{
    private readonly GreetingService _greetingService = greetingService;
    private readonly FarewellService _farewellService = farewellService;

    [Command("", Description = "Root command - shows welcome message")]
    public Task RootAsync()
    {
        Console.WriteLine("Welcome! Use 'hello', 'goodbye', or 'info' subcommands.");
        return Task.CompletedTask;
    }

    [Command("hello", Description = "Greet someone")]
    public Task HelloAsync(HelloOptions options, VerboseOptions verbose)
    {
        if (verbose.Verbose)
        {
            Console.WriteLine($"[DEBUG] Greeting: {options.Greeting}, Name: {options.Name}");
        }
        _greetingService.Greet(options.Name, options.Greeting);
        return Task.CompletedTask;
    }

    [Command("goodbye", Description = "Say goodbye to someone")]
    public Task GoodbyeAsync(GoodbyeOptions options, VerboseOptions verbose)
    {
        if (verbose.Verbose)
        {
            Console.WriteLine($"[DEBUG] Saying goodbye to: {options.Name}");
        }
        _farewellService.SayGoodbye(options.Name);
        return Task.CompletedTask;
    }

    [Command("info", Description = "Display app information (synchronous command)")]
    public void Info(InfoOptions options)
    {
        Console.WriteLine("CLI Application with Dependency Injection");
        if (options.Version)
        {
            Console.WriteLine("Version: 1.0.0");
        }
        Console.WriteLine($"Greeting Service: {_greetingService.GetType().Name}");
        Console.WriteLine($"Farewell Service: {_farewellService.GetType().Name}");
    }
}

internal sealed class GreetingService
{
    public void Greet(string name, string greeting = "Hello") =>
        Console.WriteLine($"{greeting}, {name}!");
}

internal sealed class FarewellService
{
    public void SayGoodbye(string name) =>
        Console.WriteLine($"Farewell, {name}! Until we meet again.");
}
