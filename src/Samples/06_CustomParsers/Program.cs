// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using CommandLineGenerator;
using CommandLineHosting;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
CommandLineApplicationBuilder commandLineBuilder = builder.WithCommandLine();

commandLineBuilder.Add<FetchCommand>();
CommandLineHost commandLineHost = commandLineBuilder.Build(args);
await commandLineHost.RunAsync();

// Custom parser: string -> Uri with validation
// Custom parser: string -> string[] by splitting on commas
// Custom parser: string -> Version with validation
internal sealed record FetchOptions(
    [CommandLineCustomParser(typeof(Parsers), nameof(Parsers.ParseUri))]
    [CommandLineArgument]
        Uri Endpoint,
    [CommandLineCustomParser(typeof(Parsers), nameof(Parsers.ParseTags))] string[] Tags,
    [CommandLineCustomParser(typeof(Parsers), nameof(Parsers.ParseVersion))] Version? ApiVersion
);

[CommandLineHandler(BindingContext = typeof(CommandLineContext))]
internal sealed class FetchCommand
{
    [Command]
    public void Execute(FetchOptions options)
    {
        Console.WriteLine($"Endpoint: {options.Endpoint}");
        Console.WriteLine($"Tags: [{string.Join(", ", options.Tags)}]");
        Console.WriteLine($"API Version: {options.ApiVersion?.ToString() ?? "(not specified)"}");
    }
}

internal static class Parsers
{
    public static Uri ParseUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            ? uri
            : throw new FormatException($"'{value}' is not a valid absolute URI.");

    public static string[] ParseTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static Version ParseVersion(string value) =>
        Version.TryParse(value, out Version? version)
            ? version
            : throw new FormatException(
                $"'{value}' is not a valid version (expected: Major.Minor)."
            );
}

[CommandLineBindable(typeof(FetchOptions))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal static partial class CommandLineContext { }
