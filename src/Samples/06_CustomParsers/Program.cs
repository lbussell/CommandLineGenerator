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

// --- Options record using custom parsers ---

internal sealed record FetchOptions(
    // Single-arg pattern: UriParser has a static Parse(string) method
    [CommandLineArgument(CustomParser = typeof(UriParser))] Uri Endpoint,
    // Two-arg pattern: shared utility class, specify which method
    [CommandLineOption(
        CustomParser = typeof(Parsers),
        CustomParserMethod = nameof(Parsers.ParseTags)
    )] string[] Tags,
    // Single-arg pattern: VersionParser has a static Parse(string) method
    [CommandLineOption(CustomParser = typeof(VersionParser))] Version? ApiVersion
);

// --- Dedicated parser classes (single-arg pattern) ---

internal static class UriParser
{
    public static Uri Parse(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            ? uri
            : throw new FormatException($"'{value}' is not a valid absolute URI.");
}

internal static class VersionParser
{
    public static Version Parse(string value) =>
        Version.TryParse(value, out Version? version)
            ? version
            : throw new FormatException(
                $"'{value}' is not a valid version (expected: Major.Minor)."
            );
}

// --- Shared utility class (two-arg pattern) ---

internal static class Parsers
{
    public static string[] ParseTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

// --- Command handler ---

[CommandLineHandler("fetch", BindingContext = typeof(CommandLineContext))]
internal sealed class FetchCommand
{
    [Command]
    public void Execute(FetchOptions options)
    {
        Console.WriteLine($"Endpoint: {options.Endpoint}");
        Console.WriteLine($"Tags: [{string.Join(", ", options.Tags)}]");
        Console.WriteLine($"API Version: {options.ApiVersion?.ToString() ?? "(not specified)"}");
    }

    [Command("upload")]
    public void Upload(UploadOptions options)
    {
        Console.WriteLine($"Upload file: {options.File}");
        Console.WriteLine($"Target: {options.Target}");
    }
}

// --- Class-based options with custom parser on properties ---

internal sealed class UploadOptions
{
    [CommandLineArgument]
    public required string File { get; set; }

    [CommandLineOption(CustomParser = typeof(UriParser))]
    public required Uri Target { get; set; }
}

[CommandLineBindable(typeof(FetchOptions))]
[CommandLineBindable(typeof(UploadOptions))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal static partial class CommandLineContext { }
