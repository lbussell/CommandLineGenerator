# New System.CommandLine Source Generator Design

### Binding-First Approach

The generator focuses on binding command-line arguments to plain C# classes and records.

```cs
internal sealed class MyOptionsClass
{
    // Public properties are automatically treated as options.
    public required string RequiredStringOption { get; set; }

    // You can also explicitly mark properties as options.
    [CommandLineOption]
    public required string ExplicitStringOption { get; set; }

    // Arguments must be explicitly marked with [CommandLineArgument].
    [CommandLineArgument]
    public required string RequiredStringArgument { get; set; }

    // Properties with default values are automatically treated as optional.
    public string OptionalDefaultStringOption { get; set; } = "Default value";

    // Nullable properties are also treated as optional.
    public string? OptionalNullableStringOption { get; set; } = null;

    // Properties marked with [CommandLineIgnore] will be completely ignored.
    [CommandLineIgnore]
    public string IgnoredProperty { get; set; } = "This property will be ignored.";
}

// Records work identically
internal sealed record MyOptionsRecord(
    string RequiredStringOption,
    [CommandLineOption] string ExplicitStringOption,
    [CommandLineArgument] string RequiredStringArgument,
    string OptionalDefaultStringOption = "Default value",
    string? OptionalNullableStringOption = null,
    [CommandLineIgnore] string IgnoredProperty = "This property will be ignored."
);

// Generate binding code by declaring a partial class that derives from CommandLineBindingContext
[CommandLineBindable(typeof(MyOptionsClass))]
[CommandLineBindable(typeof(MyOptionsRecord))]
[CommandLineNamingConvention(CommandLineNamingConvention.KebabCase)]
internal sealed partial class CommandLineContext : CommandLineBindingContext
{
}
```

### Generated API

The generator produces:
- `CommandLineContext.Parse<T>(ParseResult)` — parses command-line args into a typed instance
- `command.AddOptions<T>()` — extension method that adds all options/arguments to a `Command`
