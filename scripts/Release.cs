// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

/*
 * Usage:
 * Do pre-release: dotnet scripts/Release.cs
 * Do stable release: dotnet scripts/Release.cs --stable
 */

#:package CliWrap@3.10.0
#:package Spectre.Console@0.54.1-alpha.0.31

using System.Text;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;

var firstArg = args.FirstOrDefault() ?? "";
var isStableRelease = firstArg.Equals("--stable");

var gh = new CliWrapper("gh");
var dotnet = new CliWrapper("dotnet");

Prompt.Info("Checking GitHub CLI authentication...");
await gh.RunAsync("auth status");

var stableVersionFlag = isStableRelease ? "true" : "false";
var versionResult = await dotnet.RunAsync($"msbuild src/CommandLineGenerator/CommandLineGenerator.csproj -getProperty:PackageVersion -p:StableVersion={stableVersionFlag}");
var packageVersion = versionResult.StandardOutput.Trim();

var releasePrompt = isStableRelease
    ? $"This will publish [green]STABLE[/] package [blue]v{Markup.Escape(packageVersion)}[/]. Are you sure?"
    : $"This will publish [yellow]PRE-RELEASE[/] package [blue]v{Markup.Escape(packageVersion)}[/]. Are you sure?";
var doRelease = Prompt.Confirm(releasePrompt);
if (!doRelease)
{
    Prompt.Warning("Release canceled.");
    Environment.Exit(1);
}

await gh.RunWithConfirmationAsync($"workflow run publish-nuget.yml -f stable-version={stableVersionFlag}");
Prompt.Success($"Workflow triggered for [blue]v{Markup.Escape(packageVersion)}[/].");

#region Helpers
internal class CliWrapper
{
    private readonly string _commandName;
    private readonly Command _command;
    private readonly PipeTarget _stdOutPipe;
    private readonly PipeTarget _stdErrPipe;

    public CliWrapper(string command)
    {
        _commandName = command;
        _stdOutPipe = PipeTarget.ToDelegate(line => AnsiConsole.MarkupLineInterpolated($"[dim][[stdout]] {line}[/]"));
        _stdErrPipe = PipeTarget.ToDelegate(line => AnsiConsole.MarkupLineInterpolated($"[yellow][[stderr]] {line}[/]"));
        _command = Cli.Wrap(command)
            .WithStandardOutputPipe(_stdOutPipe)
            .WithStandardErrorPipe(_stdErrPipe)
            .WithValidation(CommandResultValidation.ZeroExitCode);
    }

    public async Task<BufferedCommandResult> RunWithConfirmationAsync(
        string arguments,
        string? standardInput = null,
        CancellationToken cancellationToken = default
    )
    {
        var commandString = Markup.Escape($"{_commandName} {arguments}");
        return !Prompt.Confirm($"Run `[blue]{commandString}[/]`?")
            ? throw new OperationCanceledException("User aborted the operation.")
            : await RunAsync(arguments, standardInput, cancellationToken);
    }

    public async Task<BufferedCommandResult> RunAsync(
        string arguments,
        string? standardInput = null,
        CancellationToken cancellationToken = default
    )
    {
        var commandString = Markup.Escape($"{_commandName} {arguments}");
        var cmd = _command.WithArguments(arguments);
        AnsiConsole.MarkupLineInterpolated($"[blue][[exec]] {commandString}[/]");
        if (standardInput is not null) cmd = cmd.WithStandardInputPipe(PipeSource.FromString(standardInput));
        return await cmd.ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8, cancellationToken);
    }

}

internal static class Prompt
{
    public static void Info(string message) => AnsiConsole.MarkupLine($"[green][[info]][/] {message}");
    public static void Success(string message) => AnsiConsole.MarkupLine($"[green][[success]][/] {message}");
    public static void Error(string message) => AnsiConsole.MarkupLine($"[red][[error]][/] {message}");
    public static void Warning(string message) => AnsiConsole.MarkupLine($"[yellow][[warning]][/] {message}");
    public static bool Confirm(string message) => AnsiConsole.Confirm($"[purple][[confirm]][/] {message}");
    public static string Ask(string message) => AnsiConsole.Prompt(new TextPrompt<string>(message).PromptStyle("blue"));
}
#endregion
