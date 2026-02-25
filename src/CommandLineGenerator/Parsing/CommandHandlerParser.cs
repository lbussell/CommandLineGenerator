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
    /// <c>[CommandLineHandler]</c>.
    /// </summary>
    public static CommandHandlerInfo? Parse(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol handlerType)
            return null;

        // Extract the BindingContext type from [CommandLineHandler(BindingContext = typeof(...))]
        INamedTypeSymbol? contextType = null;
        foreach (AttributeData attr in ctx.Attributes)
        {
            ct.ThrowIfCancellationRequested();
            if (attr.AttributeClass?.ToDisplayString() != AttributeNames.CommandLineHandler)
                continue;

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

        // Find a method decorated with [Command]
        foreach (IMethodSymbol method in handlerType.GetMembers().OfType<IMethodSymbol>())
        {
            ct.ThrowIfCancellationRequested();

            string? commandName = GetCommandName(method);
            if (commandName is null)
                continue;

            // Must have at least one parameter (the options type)
            if (method.Parameters.Length == 0)
                continue;

            IParameterSymbol optionsParam = method.Parameters[0];
            INamedTypeSymbol? optionsType = optionsParam.Type as INamedTypeSymbol;
            if (optionsType is null)
                continue;

            // Check for optional CancellationToken parameter
            bool acceptsCancellationToken =
                method.Parameters.Length >= 2
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

            string handlerNs = handlerType.ContainingNamespace.ToDisplayString();
            string handlerFullName = Utilities.GetFullyQualifiedName(handlerNs, handlerType.Name);

            string optionsNs = optionsType.ContainingNamespace.ToDisplayString();
            string optionsFullName = Utilities.GetFullyQualifiedName(optionsNs, optionsType.Name);

            string contextNs = contextType.ContainingNamespace.ToDisplayString();

            return new CommandHandlerInfo(
                handlerType.Name,
                handlerFullName,
                method.Name,
                commandName,
                optionsType.Name,
                optionsFullName,
                isAsync,
                returnsExitCode,
                acceptsCancellationToken,
                contextType.Name,
                contextNs
            );
        }

        return null;
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
