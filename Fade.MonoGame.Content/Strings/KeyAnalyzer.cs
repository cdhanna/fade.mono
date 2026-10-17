using System.Reflection;
using System.Runtime.Loader;
using FadeBasic;
using FadeBasic.Ast;
using FadeBasic.Sdk;
using FadeBasic.SourceGenerators;
using FadeBasic.Virtual;

namespace Fade.MonoGame.Content.Strings;

public enum KeyExpressionKind
{
    Literal,
    Variable,
    ArrayOrFunction,
    Command,
    Concatenation,
    Other,
}

/// <summary>One place the program passes a string table key: a parameter marked [FadeLocKey].</summary>
public sealed class KeyUse
{
    public string file = "";
    public int line;    // 1-based
    public int column;  // 1-based; 0 when it is not known (a key that came from a #constant)
    public string command = "";
    public KeyExpressionKind kind;

    /// <summary>The key, when <see cref="kind"/> is <see cref="KeyExpressionKind.Literal"/>.</summary>
    public string key = "";

    /// <summary>The parameter wants a constant only (<c>lockey$</c>).</summary>
    public bool constantOnly;

    /// <summary>The command resolves its text straight away, so every arg it needs must be passed here.</summary>
    public bool argsComplete;

    /// <summary>The command takes name/value args after the key.</summary>
    public bool hasArgs;

    /// <summary>How many values were passed as args.</summary>
    public int argValueCount;

    /// <summary>The arg names, or null when any of them is not a constant string.</summary>
    public List<string>? argNames;

    public string Where => column > 0 ? $"{file}({line},{column})" : $"{file}({line})";
}

/// <summary>A problem with the program itself, before any key is looked at.</summary>
public readonly record struct SourceProblem(string code, string file, int line, string message);

/// <summary>
/// Parses a Fade program with its command libraries, exactly as the build's FadeProjectTask does,
/// and finds every call that passes a string table key. A key position is any command parameter
/// marked with an attribute named FadeLocKeyAttribute; it is matched by name, so the attribute's
/// type identity across load contexts does not matter.
///
/// The command metadata Fade keeps (CommandInfo) has no parameter names or attributes, so they
/// come from reflection: a command's executor is a generated method named Call_{Method}_{sig} on
/// the command class, which leads back to the C# method.
/// </summary>
public static class KeyAnalyzer
{
    public sealed class Result
    {
        public readonly List<KeyUse> uses = new();
        public readonly List<SourceProblem> problems = new();
    }

    /// <param name="sources">The .fbasic files, in build order.</param>
    /// <param name="commands">Each command library: its dll and the command classes in it.</param>
    /// <param name="probeDirs">Where the libraries' dependencies can be found.</param>
    public static Result Analyze(List<string> sources, List<(string dll, string className)> commands, List<string> probeDirs)
    {
        var result = new Result();

        // A file that is not UTF-8 is read with U+FFFD in place of every accent, silently.
        var utf8 = new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true);
        foreach (var file in sources)
        {
            try { utf8.GetString(File.ReadAllBytes(file)); }
            catch (System.Text.DecoderFallbackException)
            {
                result.problems.Add(new SourceProblem("FLOC014", file, 1,
                    "this file is not UTF-8, so any accented letters in it are lost when it is compiled; save it as UTF-8"));
            }
        }

        var loadContext = new AssemblyLoadContext("fade-strings-commands", isCollectible: true);
        try
        {
            var collection = LoadCommands(loadContext, commands, probeDirs);
            var map = SourceMap.CreateSourceMap(sources);
            var lex = new Lexer().TokenizeWithErrors(map.fullSource, collection);
            if (lex.tokenErrors.Count > 0)
            {
                // The Fade compile reports these itself; there is nothing to check until they are fixed.
                return result;
            }
            map.ProvideTokens(lex);
            var program = new Parser(lex.stream, collection).ParseProgram();
            if (program.GetAllErrors().Count > 0) return result;

            // Keys used by tests are not uses by the game: a key only a test needs is unused.
            var inTests = new HashSet<IAstNode>();
            foreach (var test in program.tests)
                foreach (var node in test.Where(n => n is CommandExpression || n is CommandStatement))
                    inTests.Add(node);

            var methods = new Dictionary<MethodInfo, ParameterInfo[]?>();
            foreach (var node in program.Where(n => n is CommandExpression || n is CommandStatement))
            {
                if (inTests.Contains(node)) continue;
                CommandInfo cmd;
                List<IExpressionNode> args;
                List<int> argMap;
                if (node is CommandExpression ce) (cmd, args, argMap) = (ce.command, ce.args, ce.argMap);
                else if (node is CommandStatement cs) (cmd, args, argMap) = (cs.command, cs.args, cs.argMap);
                else continue;
                if (cmd.executor == null) continue;

                if (!methods.TryGetValue(cmd.executor.Method, out var ps))
                    methods[cmd.executor.Method] = ps = FindMethod(cmd)?.GetParameters();
                if (ps == null) continue;

                var keyParam = Array.FindIndex(ps, p => HasAttribute(p, "FadeLocKeyAttribute"));
                if (keyParam < 0) continue;
                var argsParam = Array.FindIndex(ps, p => HasAttribute(p, "FadeLocArgsAttribute"));

                for (var i = 0; i < args.Count; i++)
                {
                    if (argMap[i] != keyParam) continue;
                    var use = Describe(map, cmd, args[i], ps[keyParam]);
                    if (argsParam >= 0)
                    {
                        use.hasArgs = true;
                        use.argsComplete = GetBool(ps[argsParam], "FadeLocArgsAttribute", "complete");
                        var values = new List<IExpressionNode>();
                        for (var j = 0; j < args.Count; j++)
                            if (argMap[j] == argsParam) values.Add(args[j]);
                        use.argValueCount = values.Count;
                        use.argNames = new List<string>();
                        for (var j = 0; j < values.Count; j += 2)
                        {
                            if (values[j] is LiteralStringExpression name) use.argNames.Add(name.value);
                            else { use.argNames = null; break; }
                        }
                    }
                    result.uses.Add(use);
                }
            }
        }
        finally
        {
            loadContext.Unload();
        }

        return result;
    }

    private static KeyUse Describe(SourceMap map, CommandInfo cmd, IExpressionNode arg, ParameterInfo param)
    {
        var location = map.GetOriginalLocation(arg.StartToken);
        var fromConstant = arg.StartToken.flags.HasFlag(TokenFlags.IsConstant);
        var use = new KeyUse
        {
            file = location.fileName,
            line = location.startLine + 1,
            // A token a #constant produced reports a column shifted by the difference in length.
            column = fromConstant ? 0 : location.startChar + 1,
            command = cmd.name,
            constantOnly = GetBool(param, "FadeLocKeyAttribute", "constantOnly"),
        };

        switch (arg)
        {
            case LiteralStringExpression s:
                use.kind = KeyExpressionKind.Literal;
                use.key = s.value;
                break;
            case BinaryOperandExpression:
                use.kind = KeyExpressionKind.Concatenation;
                break;
            case VariableRefNode:
                use.kind = KeyExpressionKind.Variable;
                break;
            case ArrayIndexReference:
                use.kind = KeyExpressionKind.ArrayOrFunction;
                break;
            case CommandExpression:
                use.kind = KeyExpressionKind.Command;
                break;
            default:
                use.kind = KeyExpressionKind.Other;
                break;
        }
        return use;
    }

    private static bool HasAttribute(ParameterInfo p, string name)
        => p.CustomAttributes.Any(a => a.AttributeType.Name == name);

    private static bool GetBool(ParameterInfo p, string attribute, string member)
    {
        var data = p.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == attribute);
        if (data == null) return false;
        foreach (var named in data.NamedArguments)
            if (named.MemberName == member && named.TypedValue.Value is bool b) return b;
        return false;
    }

    /// <summary>The C# method behind a command, from its generated executor.</summary>
    private static MethodInfo? FindMethod(CommandInfo cmd)
    {
        var generated = cmd.executor.Method;
        var prefix = "Call_";
        var suffix = "_" + cmd.sig;
        var name = generated.Name;
        if (!name.StartsWith(prefix) || !name.EndsWith(suffix)) return null;
        var csName = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
        return generated.DeclaringType?
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == csName && m.GetParameters().Length == cmd.args.Length)
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == nameof(FadeBasicCommandAttribute)
                                                    && a.ConstructorArguments.Count > 0
                                                    && (a.ConstructorArguments[0].Value as string) == cmd.name))
            .FirstOrDefault(m => m.GetParameters().Select((p, i) => SameType(p, cmd.args[i].typeCode)).All(x => x));
    }

    private static bool SameType(ParameterInfo p, byte typeCode)
    {
        var t = p.ParameterType;
        if (t.IsByRef) t = t.GetElementType()!;
        if (t.IsArray) t = t.GetElementType()!;
        return typeCode switch
        {
            TypeCodes.INT => t == typeof(int),
            TypeCodes.STRING => t == typeof(string),
            TypeCodes.REAL => t == typeof(float),
            TypeCodes.ANY => t == typeof(object),
            _ => true
        };
    }

    /// <summary>
    /// The same as FadeBasic.Build's ProjectBuilder.LoadCommands: load each library into a
    /// collectible context, make an instance of each command class, and read its metadata.
    /// FadeBasic.Lang.Core itself comes from this process, so the types line up.
    /// </summary>
    private static CommandCollection LoadCommands(AssemblyLoadContext context, List<(string dll, string className)> commands, List<string> probeDirs)
    {
        var dirs = new List<string>(probeDirs);
        foreach (var (dll, _) in commands)
        {
            var dir = Path.GetDirectoryName(dll);
            if (!string.IsNullOrEmpty(dir) && !dirs.Contains(dir)) dirs.Add(dir);
        }

        var host = typeof(IMethodSource).Assembly;
        context.Resolving += (ctx, name) =>
        {
            if (name.Name == host.GetName().Name) return host;
            foreach (var dir in dirs)
            {
                var candidate = Path.Combine(dir, name.Name + ".dll");
                if (File.Exists(candidate)) return LoadIntoMemory(ctx, candidate);
            }
            return null;
        };

        var sources = new List<IMethodSource>();
        var loaded = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        using var _ = context.EnterContextualReflection();
        foreach (var (dll, className) in commands)
        {
            if (!loaded.TryGetValue(dll, out var assembly)) loaded[dll] = assembly = LoadIntoMemory(context, dll);
            var type = assembly.GetType(className) ?? throw new InvalidOperationException($"no command class {className} in {dll}");
            sources.Add((IMethodSource)Activator.CreateInstance(type)!);
        }
        return new CommandCollection(sources.ToArray());
    }

    // From memory, not from the path, so the dll is not locked while the build goes on.
    private static Assembly LoadIntoMemory(AssemblyLoadContext context, string path)
    {
        using var stream = new MemoryStream(File.ReadAllBytes(path));
        return context.LoadFromStream(stream);
    }
}
