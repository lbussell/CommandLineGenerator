// SPDX-FileCopyrightText: Copyright (c) 2026 Logan Bussell
// SPDX-License-Identifier: MIT

using Microsoft.CodeAnalysis;

namespace CommandLineGenerator.Parsing;

/// <summary>
/// Parses classes decorated with <c>[CommandLineHandler]</c> to extract
/// command handler metadata for DI-based wiring code generation.
/// </summary>
internal static class CommandHandlerParser
{
    /// <summary>
    /// Extracts handler metadata from a command class decorated with
    /// <c>[CommandLineHandler]</c>. Collects all <c>[Command]</c>-decorated methods.
    /// </summary>
    public static CommandHandlerInfo? Parse(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol handlerType)
            return null;

        // Extract BindingContext and GroupName from [CommandLineHandler]
        INamedTypeSymbol? contextType = null;
        string groupName = "";

        foreach (AttributeData attr in ctx.Attributes)
        {
            ct.ThrowIfCancellationRequested();
            if (attr.AttributeClass?.ToDisplayString() != AttributeNames.CommandLineHandler)
                continue;

            // GroupName from constructor argument
            if (
                attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is string name
            )
            {
                groupName = name;
            }

            // BindingContext from named argument
            foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
            {
                if (
                    namedArg.Key == "BindingContext"
                    && namedArg.Value.Value is INamedTypeSymbol bindingContextType
                )
                {
                    contextType = bindingContextType;
                }
            }
        }

        if (contextType is null)
            return null;

        // Collect ALL methods decorated with [Command]
        List<CommandMethodInfo> commands = [];
        foreach (IMethodSymbol method in handlerType.GetMembers().OfType<IMethodSymbol>())
        {
            ct.ThrowIfCancellationRequested();

            string? commandName = GetCommandName(method);
            if (commandName is null)
                continue;

            CommandMethodInfo methodInfo = ParseMethod(method, commandName);
            commands.Add(methodInfo);
        }

        if (commands.Count == 0)
            return null;

        string handlerNs = handlerType.ContainingNamespace.ToDisplayString();
        string handlerFullName = Utilities.GetFullyQualifiedName(handlerNs, handlerType.Name);
        string contextNs = contextType.ContainingNamespace.ToDisplayString();

        return new CommandHandlerInfo(
            handlerType.Name,
            handlerFullName,
            groupName,
            [.. commands],
            contextType.Name,
            contextNs
        );
    }

    private static CommandMethodInfo ParseMethod(IMethodSymbol method, string commandName)
    {
        // Options type from first parameter (if any)
        string? optionsTypeName = null;
        string? optionsFullName = null;

        if (method.Parameters.Length > 0)
        {
            IParameterSymbol optionsParam = method.Parameters[0];
            if (
                optionsParam.Type is INamedTypeSymbol optionsType
                && optionsParam.Type.ToDisplayString() != "System.Threading.CancellationToken"
            )
            {
                optionsTypeName = optionsType.Name;
                string optionsNs = optionsType.ContainingNamespace.ToDisplayString();
                optionsFullName = Utilities.GetFullyQualifiedName(optionsNs, optionsType.Name);
            }
        }

        // Check for CancellationToken parameter
        bool acceptsCancellationToken =
            method.Parameters.Length >= 1
            && method.Parameters[method.Parameters.Length - 1].Type.ToDisplayString()
                == "System.Threading.CancellationToken";

        // Determine async/exit code from return type
        bool isAsync = false;
        bool returnsExitCode = false;
        string returnTypeStr = method.ReturnType.ToDisplayString();

        if (returnTypeStr == "System.Threading.Tasks.Task<int>")
        {
            isAsync = true;
            returnsExitCode = true;
        }
        else if (returnTypeStr == "System.Threading.Tasks.Task")
        {
            isAsync = true;
        }
        else if (method.ReturnType.SpecialType == SpecialType.System_Int32)
        {
            returnsExitCode = true;
        }

        return new CommandMethodInfo(
            method.Name,
            commandName,
            optionsTypeName,
            optionsFullName,
            isAsync,
            returnsExitCode,
            acceptsCancellationToken
        );
    }

    /// <summary>
    /// Returns the command name from a <c>[Command]</c> attribute on the method,
    /// or <c>null</c> if the method is not decorated with it.
    /// </summary>
    private static string? GetCommandName(IMethodSymbol method)
    {
        foreach (AttributeData attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == AttributeNames.Command)
            {
                if (
                    attr.ConstructorArguments.Length > 0
                    && attr.ConstructorArguments[0].Value is string name
                )
                {
                    return name;
                }
                return "";
            }
        }
        return null;
    }
}
