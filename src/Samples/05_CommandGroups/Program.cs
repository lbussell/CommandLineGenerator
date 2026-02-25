// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using CommandLineGenerator;
using CommandLineHosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
CommandLineApplicationBuilder commandLineBuilder = builder.WithCommandLine();

commandLineBuilder.Services.AddSingleton<GitService>();
commandLineBuilder.Add<ListCommand>();
CommandLineHost commandLineHost = commandLineBuilder.Build(args);
await commandLineHost.RunAsync();

internal sealed record ListOptions(bool Verbose = false);

internal sealed record ListReposOptions([CommandLineArgument] string Organization);

internal sealed record ListBranchesOptions([CommandLineArgument] string Repo);

[CommandLineHandler("list", BindingContext = typeof(CommandLineContext))]
internal sealed class ListCommand(GitService gitService)
{
    [Command]
    public void List(ListOptions options)
    {
        gitService.ListInfo(options.Verbose);
    }

    [Command("repos")]
    public void Repos(ListReposOptions options)
    {
        gitService.ListRepos(options.Organization);
    }

    [Command("branches")]
    public void Branches(ListBranchesOptions options)
    {
        gitService.ListBranches(options.Repo);
    }
}

internal sealed class GitService
{
    public void ListInfo(bool verbose) =>
        Console.WriteLine(verbose ? "Detailed listing info..." : "Basic listing info");

    public void ListRepos(string org) => Console.WriteLine($"Repos for {org}: repo1, repo2, repo3");

    public void ListBranches(string repo) =>
        Console.WriteLine($"Branches for {repo}: main, dev, feature-1");
}

[CommandLineBindable(typeof(ListOptions))]
[CommandLineBindable(typeof(ListReposOptions))]
[CommandLineBindable(typeof(ListBranchesOptions))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal static partial class CommandLineContext { }
