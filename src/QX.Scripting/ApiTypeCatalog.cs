using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace Qx.Scripting;

/// <summary>Represents an assembly whose exported types are listed in an <see cref="ApiTypeCatalog"/>.</summary>
/// <param name="Name">The simple name of the assembly.</param>
/// <param name="Version">The assembly version, or <see langword="null"/> when it has none.</param>
/// <param name="TypeCount">The number of public types the assembly exports.</param>
public sealed record ApiAssembly(string Name, string? Version, int TypeCount);

/// <summary>Represents a single <c>&lt;param&gt;</c> entry read from the generated XML documentation.</summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Text">The parameter description as flattened text.</param>
public sealed record ApiDocParameter(string Name, string Text);

/// <summary>
/// Represents a single <c>&lt;exception&gt;</c> entry read from the generated XML documentation.
/// </summary>
/// <param name="Type">
/// The documented exception type, with the documentation ID prefix such as <c>T:</c> removed.
/// </param>
/// <param name="Text">The condition under which the exception is thrown as flattened text, or an empty string.</param>
public sealed record ApiDocException(string Type, string Text);

/// <summary>
/// Represents the documentation attached to a catalog type or member, read from the XML
/// documentation file the compiler emits next to the assembly.
/// </summary>
/// <remarks>
/// Every part is optional; the whole record is absent when the member carries no documentation or
/// the assembly has no XML file. All text arrives with whitespace collapsed to single spaces and
/// with cross references replaced by the name of their target. Parts that are
/// <see langword="null"/> are left out when the record is serialized to JSON.
/// </remarks>
/// <param name="Summary">The summary text, or <see langword="null"/> when there is none.</param>
/// <param name="Returns">The returns text, or <see langword="null"/> when there is none.</param>
/// <param name="Remarks">The remarks text, or <see langword="null"/> when there are none.</param>
/// <param name="Parameters">The documented parameters, or <see langword="null"/> when none are documented.</param>
/// <param name="Exceptions">The documented exceptions, or <see langword="null"/> when none are documented.</param>
public sealed record ApiDoc(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Summary = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Returns = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Remarks = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<ApiDocParameter>? Parameters = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<ApiDocException>? Exceptions = null);

/// <summary>Represents a short reference to a type in an <see cref="ApiTypeCatalog"/>.</summary>
/// <param name="Name">
/// The display name in C# syntax, with generic parameters and, for a nested type, the declaring
/// type, such as <c>List&lt;T&gt;</c> or <c>Outer.Inner</c>.
/// </param>
/// <param name="FullName">
/// The namespace qualified name with nested types joined by dots; generic types keep their arity
/// suffix, such as <c>`1</c>.
/// </param>
/// <param name="Kind">
/// The type kind: <c>class</c>, <c>static class</c>, <c>struct</c>, <c>interface</c>,
/// <c>enum</c> or <c>delegate</c>.
/// </param>
/// <param name="Assembly">The simple name of the assembly that exports the type.</param>
/// <param name="Namespace">The namespace, or <see langword="null"/> for a type in the global namespace.</param>
/// <param name="Documentation">The XML documentation of the type, or <see langword="null"/> when none is available.</param>
public sealed record ApiTypeReference(
    string Name,
    string FullName,
    string Kind,
    string Assembly,
    string? Namespace,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ApiDoc? Documentation = null);

/// <summary>Represents a public member of a type in an <see cref="ApiTypeCatalog"/>.</summary>
/// <param name="Kind">
/// The member kind: <c>constructor</c>, <c>property</c>, <c>method</c>, <c>event</c>,
/// <c>field</c>, or <c>value</c> for an enum member.
/// </param>
/// <param name="Name">The member name; for a constructor, the display name of the type.</param>
/// <param name="Signature">The member declaration in C# syntax, including default parameter values.</param>
/// <param name="IsStatic">
/// <see langword="true"/> if the member is static, which enum values always are; otherwise,
/// <see langword="false"/>.
/// </param>
/// <param name="DeclaredBy">
/// The full name of the type that declares the member, or an empty string when it is unknown.
/// </param>
/// <param name="Documentation">The XML documentation of the member, or <see langword="null"/> when none is available.</param>
public sealed record ApiMember(
    string Kind,
    string Name,
    string Signature,
    bool IsStatic,
    string DeclaredBy,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ApiDoc? Documentation = null);

/// <summary>Represents the full description of a type in an <see cref="ApiTypeCatalog"/>, including its public members.</summary>
/// <param name="Name">The display name in C# syntax.</param>
/// <param name="FullName">The namespace qualified name with nested types joined by dots.</param>
/// <param name="Kind">
/// The type kind: <c>class</c>, <c>static class</c>, <c>struct</c>, <c>interface</c>,
/// <c>enum</c> or <c>delegate</c>.
/// </param>
/// <param name="Assembly">The simple name of the assembly that exports the type.</param>
/// <param name="Namespace">The namespace, or <see langword="null"/> for a type in the global namespace.</param>
/// <param name="Signature">
/// The type declaration in C# syntax, with its base type, interfaces and generic constraints.
/// </param>
/// <param name="BaseType">
/// The display name of the base type, or <see langword="null"/> when it is <see cref="object"/>,
/// <see cref="ValueType"/>, <see cref="Enum"/> or <see cref="MulticastDelegate"/>.
/// </param>
/// <param name="Interfaces">The display names of every interface the type implements, sorted.</param>
/// <param name="Members">
/// The public constructors, properties, methods, events and fields, or the values of an enum,
/// ordered by kind and then by name. Interfaces also list the members of the interfaces they
/// inherit.
/// </param>
/// <param name="Documentation">The XML documentation of the type, or <see langword="null"/> when none is available.</param>
public sealed record ApiTypeDetails(
    string Name,
    string FullName,
    string Kind,
    string Assembly,
    string? Namespace,
    string Signature,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<ApiMember> Members,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ApiDoc? Documentation = null);

/// <summary>Represents the result of looking up a type by name in an <see cref="ApiTypeCatalog"/>.</summary>
/// <param name="Query">The name as it was passed to the lookup.</param>
/// <param name="Type">
/// The details of the matching type, or <see langword="null"/> when the name is ambiguous or
/// unknown.
/// </param>
/// <param name="Ambiguous">
/// <see langword="true"/> if the name matched more than one type; otherwise, <see langword="false"/>.
/// </param>
/// <param name="NotFound">
/// <see langword="true"/> if no type matched the name; otherwise, <see langword="false"/>.
/// </param>
/// <param name="Candidates">
/// The candidate types: every matching type sorted by full name when the name is ambiguous, up to
/// 15 types whose name contains the query when nothing matched, and none on a unique match.
/// </param>
public sealed record ApiTypeLookup(
    string Query,
    ApiTypeDetails? Type,
    bool Ambiguous,
    bool NotFound,
    IReadOnlyList<ApiTypeReference> Candidates);

/// <summary>
/// Represents a member found by <see cref="ApiTypeCatalog.SearchMembers(string, string?, int)"/>,
/// together with the type it is listed on.
/// </summary>
/// <param name="Type">The full name of the type the member is listed on.</param>
/// <param name="DeclaredBy">
/// The full name of the type that declares the member, which differs from
/// <paramref name="Type"/> for inherited interface members.
/// </param>
/// <param name="Kind">
/// The member kind: <c>constructor</c>, <c>property</c>, <c>method</c>, <c>event</c>,
/// <c>field</c>, or <c>value</c> for an enum member.
/// </param>
/// <param name="Name">The member name; for a constructor, the display name of the type.</param>
/// <param name="Signature">The member declaration in C# syntax.</param>
/// <param name="IsStatic">
/// <see langword="true"/> if the member is static; otherwise, <see langword="false"/>.
/// </param>
/// <param name="Documentation">The XML documentation of the member, or <see langword="null"/> when none is available.</param>
public sealed record ApiMemberReference(
    string Type,
    string DeclaredBy,
    string Kind,
    string Name,
    string Signature,
    bool IsStatic,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ApiDoc? Documentation = null);

/// <summary>
/// Provides search and lookup over the public types of a set of assemblies, with their members
/// and XML documentation.
/// </summary>
/// <remarks>
/// Types that cannot be loaded are skipped. The member index behind
/// <see cref="SearchMembers(string, string?, int)"/> is built on its first call and reused
/// afterwards.
/// </remarks>
public sealed class ApiTypeCatalog
{
    private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
    private const int DefaultTypeLimit = 50;
    private const int DefaultMemberLimit = 60;
    private const int MaximumLimit = 500;

    private readonly IReadOnlyList<(Type Type, string Assembly)> _types;
    private readonly Lazy<IReadOnlyList<ApiMemberReference>> _memberIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiTypeCatalog"/> class over the assemblies
    /// listed in <see cref="ScriptEngine.ReferenceAssemblies"/>.
    /// </summary>
    public ApiTypeCatalog()
        : this(ScriptEngine.ReferenceAssemblies)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiTypeCatalog"/> class over the specified
    /// assemblies.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies whose exported types are listed. <see langword="null"/> entries and
    /// duplicates are ignored.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblies"/> is <see langword="null"/>.</exception>
    public ApiTypeCatalog(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var types = new List<(Type Type, string Assembly)>();
        var catalogAssemblies = new List<ApiAssembly>();

        foreach (Assembly assembly in assemblies
            .Where(assembly => assembly is not null)
            .DistinctBy(assembly => assembly.FullName)
            .OrderBy(assembly => assembly.GetName().Name, StringComparer.OrdinalIgnoreCase))
        {
            Type[] exported = ExportedTypes(assembly);
            string name = assembly.GetName().Name ?? assembly.FullName ?? "?";
            foreach (Type type in exported)
            {
                if (!type.IsSpecialName)
                    types.Add((type, name));
            }
            catalogAssemblies.Add(new ApiAssembly(name, assembly.GetName().Version?.ToString(), exported.Length));
        }

        _types = types
            .OrderBy(entry => TypeName(entry.Type), StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => FullName(entry.Type), StringComparer.Ordinal)
            .ToArray();
        Assemblies = catalogAssemblies;
        _memberIndex = new Lazy<IReadOnlyList<ApiMemberReference>>(
            BuildMemberIndex,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Gets the assemblies in the catalog, ordered by name.</summary>
    public IReadOnlyList<ApiAssembly> Assemblies { get; }

    /// <summary>
    /// Reads the compiler generated XML documentation for a type, method, constructor, property,
    /// event or field.
    /// </summary>
    /// <remarks>
    /// The documentation file is looked up next to the declaring assembly, then in the application
    /// base directory, and parsed once per assembly. A missing, unreadable or malformed file yields
    /// <see langword="null"/> rather than an exception.
    /// </remarks>
    /// <param name="member">The member to read the documentation for.</param>
    /// <returns>The documentation, or <see langword="null"/> when the member is undocumented.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="member"/> is <see langword="null"/>.</exception>
    public static ApiDoc? DocumentationFor(MemberInfo member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return Describe(member);
    }

    /// <summary>
    /// Searches the catalog for types whose display name or full name contains the query.
    /// </summary>
    /// <remarks>Matching is case-insensitive, and blank filters count as <see langword="null"/>.</remarks>
    /// <param name="query">The text to look for in the type names, or <see langword="null"/> to list every type.</param>
    /// <param name="assembly">
    /// The text the assembly name must contain, or <see langword="null"/> for every assembly.
    /// </param>
    /// <param name="limit">
    /// The maximum number of results. Zero or less uses the default of 50, and the value is capped
    /// at 500.
    /// </param>
    /// <returns>
    /// The matching types: exact name matches first, then name prefix matches, then the rest, each
    /// sorted by name.
    /// </returns>
    public IReadOnlyList<ApiTypeReference> SearchTypes(
        string? query = null,
        string? assembly = null,
        int limit = DefaultTypeLimit)
    {
        string? normalizedQuery = NormalizeOptional(query);
        string? normalizedAssembly = NormalizeOptional(assembly);
        IEnumerable<(Type Type, string Assembly)> source = _types;

        if (normalizedAssembly is not null)
        {
            source = source.Where(entry =>
                entry.Assembly.Contains(normalizedAssembly, StringComparison.OrdinalIgnoreCase));
        }

        if (normalizedQuery is not null)
        {
            source = source.Where(entry =>
                TypeName(entry.Type).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                FullName(entry.Type).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase));
        }

        return source
            .OrderBy(entry => MatchRank(entry.Type, normalizedQuery))
            .ThenBy(entry => TypeName(entry.Type), StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => FullName(entry.Type), StringComparer.Ordinal)
            .Take(NormalizeLimit(limit, DefaultTypeLimit))
            .Select(Reference)
            .ToArray();
    }

    /// <summary>
    /// Looks up a type by its full name, falling back to its short or display name.
    /// </summary>
    /// <remarks>
    /// The name is trimmed, <c>+</c> becomes <c>.</c> and <c>global::</c> is removed. Matching is
    /// case-insensitive, and full name matches take precedence over short name matches.
    /// </remarks>
    /// <param name="name">The full name, short name or display name of the type.</param>
    /// <returns>
    /// The lookup result: the type details on a unique match, the candidates when the name is
    /// ambiguous, or up to 15 suggestions when nothing matched.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty or whitespace.</exception>
    public ApiTypeLookup GetType(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string query = NormalizeTypeQuery(name);

        List<(Type Type, string Assembly)> matches = _types
            .Where(entry => string.Equals(FullName(entry.Type), query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            matches = _types
                .Where(entry =>
                    string.Equals(entry.Type.Name, query, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(TypeName(entry.Type), query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (matches.Count == 1)
            return new ApiTypeLookup(name, Details(matches[0]), false, false, []);

        if (matches.Count > 1)
        {
            return new ApiTypeLookup(
                name,
                null,
                true,
                false,
                matches.Select(Reference).OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray());
        }

        ApiTypeReference[] suggestions = _types
            .Where(entry =>
                TypeName(entry.Type).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                FullName(entry.Type).Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => MatchRank(entry.Type, query))
            .ThenBy(entry => TypeName(entry.Type), StringComparer.OrdinalIgnoreCase)
            .Take(15)
            .Select(Reference)
            .ToArray();
        return new ApiTypeLookup(name, null, false, true, suggestions);
    }

    /// <summary>
    /// Searches the public members of every catalog type by member name, signature or type name.
    /// </summary>
    /// <remarks>The member index is built on the first call and reused afterwards.</remarks>
    /// <param name="query">
    /// The text to look for, case-insensitively, in the member name, the signature and the full
    /// name of the type the member is listed on.
    /// </param>
    /// <param name="kind">
    /// The member kind to restrict to, such as <c>method</c> or <c>property</c>, or
    /// <see langword="null"/> for every kind.
    /// </param>
    /// <param name="limit">
    /// The maximum number of results. Zero or less uses the default of 60, and the value is capped
    /// at 500.
    /// </param>
    /// <returns>
    /// The matching members: exact name matches first, then name prefix and name substring
    /// matches, then signature and type name matches.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="query"/> is <see langword="null"/>, empty or whitespace.</exception>
    public IReadOnlyList<ApiMemberReference> SearchMembers(
        string query,
        string? kind = null,
        int limit = DefaultMemberLimit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        string? normalizedKind = NormalizeOptional(kind);
        IEnumerable<ApiMemberReference> source = _memberIndex.Value;

        if (normalizedKind is not null)
        {
            source = source.Where(member =>
                string.Equals(member.Kind, normalizedKind, StringComparison.OrdinalIgnoreCase));
        }

        return source
            .Where(member =>
                member.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                member.Signature.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                member.Type.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(member => MemberMatchRank(member, query))
            .ThenBy(member => member.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(member => member.Kind, StringComparer.Ordinal)
            .ThenBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
            .Take(NormalizeLimit(limit, DefaultMemberLimit))
            .ToArray();
    }

    private IReadOnlyList<ApiMemberReference> BuildMemberIndex()
    {
        var members = new List<ApiMemberReference>();
        foreach ((Type type, string assembly) in _types)
        {
            ApiTypeDetails details = Details((type, assembly));
            members.AddRange(details.Members.Select(member => new ApiMemberReference(
                details.FullName,
                member.DeclaredBy,
                member.Kind,
                member.Name,
                member.Signature,
                member.IsStatic,
                member.Documentation)));
        }
        return members;
    }

    private static ApiTypeReference Reference((Type Type, string Assembly) entry) =>
        new(
            TypeName(entry.Type),
            FullName(entry.Type),
            ReflectionFormat.TypeKind(entry.Type),
            entry.Assembly,
            entry.Type.Namespace,
            Describe(entry.Type));

    private static ApiTypeDetails Details((Type Type, string Assembly) entry)
    {
        Type type = entry.Type;
        Type? baseType = MeaningfulBaseType(type.BaseType) ? type.BaseType : null;
        return new ApiTypeDetails(
            TypeName(type),
            FullName(type),
            ReflectionFormat.TypeKind(type),
            entry.Assembly,
            type.Namespace,
            ReflectionFormat.TypeDeclaration(type),
            baseType is null ? null : ReflectionFormat.FriendlyName(baseType),
            type.GetInterfaces()
                .Select(value => ReflectionFormat.FriendlyName(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray(),
            Members(type),
            Describe(type));
    }

    private static ApiDoc? Describe(MemberInfo? member) => Describe(member, 0);

    /// <summary>
    /// Finds a member's documentation and follows <c>inheritdoc</c> to the overridden member or
    /// the implemented interface member, which is what the compiler leaves unresolved.
    /// </summary>
    private static ApiDoc? Describe(MemberInfo? member, int depth)
    {
        const int max_depth = 8;
        if (member is null || depth > max_depth)
            return null;
        Assembly? assembly = (member as Type ?? member.DeclaringType)?.Assembly;
        if (assembly is null)
            return null;
        XmlDocSet docs = XmlDocSet.ForAssembly(assembly);
        if (docs.IsEmpty)
            return null;
        string id = ReflectionFormat.DocumentationId(member);
        ApiDoc? own = docs.Find(id);
        if (own?.Summary is not null || !docs.Inherits(id, out string? source))
            return own;
        if (source is not null)
            return docs.Find(source) ?? own;
        return Inherited(member)
            .Select(inherited => Describe(inherited, depth + 1))
            .FirstOrDefault(doc => doc?.Summary is not null) ?? own;
    }

    private static IEnumerable<MemberInfo> Inherited(MemberInfo member) => member switch
    {
        Type type => [.. new[] { type.BaseType }.OfType<Type>(), .. type.GetInterfaces()],
        MethodInfo method => Overridden(method),
        PropertyInfo property => (property.GetMethod ?? property.SetMethod) is { } accessor
            ? Overridden(accessor).Select(Owner).OfType<PropertyInfo>()
            : [],
        EventInfo @event => @event.AddMethod is { } adder
            ? Overridden(adder).Select(Owner).OfType<EventInfo>()
            : [],
        _ => []
    };

    private static IEnumerable<MethodInfo> Overridden(MethodInfo method)
    {
        MethodInfo definition = method.GetBaseDefinition();
        if (definition != method)
            yield return definition;
        if (method.DeclaringType is not { IsInterface: false } type)
            yield break;
        foreach (Type contract in type.GetInterfaces())
        {
            InterfaceMapping map = type.GetInterfaceMap(contract);
            int index = Array.IndexOf(map.TargetMethods, method);
            if (index >= 0)
                yield return map.InterfaceMethods[index];
        }
    }

    private static MemberInfo? Owner(MethodInfo accessor)
    {
        const BindingFlags every = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type? type = accessor.DeclaringType;
        return (MemberInfo?)type?.GetProperties(every).FirstOrDefault(property => property.GetMethod == accessor || property.SetMethod == accessor)
            ?? type?.GetEvents(every).FirstOrDefault(@event => @event.AddMethod == accessor);
    }

    private static IReadOnlyList<ApiMember> Members(Type type)
    {
        if (type.IsEnum)
        {
            return Enum.GetNames(type)
                .Select(name => new ApiMember(
                    "value",
                    name,
                    ReflectionFormat.EnumValue(type, name),
                    true,
                    FullName(type),
                    Describe(type.GetField(name, BindingFlags.Public | BindingFlags.Static))))
                .ToArray();
        }

        var members = new List<ApiMember>();
        var signatures = new HashSet<string>(StringComparer.Ordinal);

        foreach (ConstructorInfo constructor in type.GetConstructors(MemberFlags))
        {
            Add(
                members,
                signatures,
                "constructor",
                TypeName(type),
                ReflectionFormat.Constructor(constructor),
                constructor.IsStatic,
                constructor.DeclaringType,
                constructor);
        }

        foreach (Type scope in TypeScopes(type))
        {
            foreach (PropertyInfo property in scope.GetProperties(MemberFlags))
            {
                MethodInfo? accessor = property.GetMethod ?? property.SetMethod;
                Add(
                    members,
                    signatures,
                    "property",
                    property.Name,
                    ReflectionFormat.Property(property),
                    accessor?.IsStatic == true,
                    property.DeclaringType,
                    property);
            }

            foreach (MethodInfo method in scope.GetMethods(MemberFlags))
            {
                if (method.IsSpecialName ||
                    method.DeclaringType == typeof(object) ||
                    method.Name.StartsWith('<'))
                    continue;
                Add(
                    members,
                    signatures,
                    "method",
                    method.Name,
                    ReflectionFormat.Method(method),
                    method.IsStatic,
                    method.DeclaringType,
                    method);
            }

            foreach (EventInfo @event in scope.GetEvents(MemberFlags))
            {
                MethodInfo? accessor = @event.AddMethod ?? @event.RemoveMethod;
                Add(
                    members,
                    signatures,
                    "event",
                    @event.Name,
                    ReflectionFormat.Event(@event),
                    accessor?.IsStatic == true,
                    @event.DeclaringType,
                    @event);
            }

            foreach (FieldInfo field in scope.GetFields(MemberFlags))
            {
                Add(
                    members,
                    signatures,
                    "field",
                    field.Name,
                    ReflectionFormat.Field(field),
                    field.IsStatic,
                    field.DeclaringType,
                    field);
            }
        }

        return members
            .OrderBy(member => MemberRank(member.Kind))
            .ThenBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(member => member.Signature, StringComparer.Ordinal)
            .ToArray();
    }

    private static void Add(
        ICollection<ApiMember> members,
        ISet<string> signatures,
        string kind,
        string name,
        string signature,
        bool isStatic,
        Type? declaredBy,
        MemberInfo source)
    {
        if (signatures.Add($"{kind}:{signature}"))
        {
            members.Add(new ApiMember(
                kind,
                name,
                signature,
                isStatic,
                declaredBy is null ? "" : FullName(declaredBy),
                Describe(source)));
        }
    }

    private static IEnumerable<Type> TypeScopes(Type type)
    {
        yield return type;
        if (!type.IsInterface)
            yield break;
        foreach (Type inherited in type.GetInterfaces())
            yield return inherited;
    }

    private static Type[] ExportedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>().Where(type => type.IsVisible).ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static string TypeName(Type type) => ReflectionFormat.FriendlyName(type);

    private static string FullName(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

    private static string NormalizeTypeQuery(string query) =>
        query.Trim().Replace('+', '.').Replace("global::", "", StringComparison.Ordinal);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int NormalizeLimit(int limit, int fallback) =>
        Math.Clamp(limit <= 0 ? fallback : limit, 1, MaximumLimit);

    private static int MatchRank(Type type, string? query)
    {
        if (query is null)
            return 0;
        string name = TypeName(type);
        if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 1;
        return 2;
    }

    private static int MemberRank(string kind) =>
        kind switch
        {
            "constructor" => 0,
            "property" => 1,
            "method" => 2,
            "event" => 3,
            "field" => 4,
            _ => 5
        };

    private static int MemberMatchRank(ApiMemberReference member, string query)
    {
        if (string.Equals(member.Name, query, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (member.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 1;
        if (member.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 2;
        if (member.Signature.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 3;
        if (member.Type.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 4;
        if (member.Type.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 5;
        return 6;
    }

    private static bool MeaningfulBaseType(Type? type) =>
        type is not null &&
        type != typeof(object) &&
        type != typeof(ValueType) &&
        type != typeof(Enum) &&
        type != typeof(MulticastDelegate);
}

/// <summary>
/// Contains the parsed contents of one compiler generated XML documentation file, keyed by
/// ECMA-334 documentation comment identifier.
/// </summary>
/// <remarks>
/// Instances are immutable and cached per assembly; every failure path (no file, unreadable file,
/// malformed XML) collapses to <see cref="Empty"/> so that documentation is never able to break
/// catalog construction.
/// </remarks>
internal sealed class XmlDocSet
{
    public static readonly XmlDocSet Empty = new(
        new Dictionary<string, ApiDoc>(0, StringComparer.Ordinal),
        new Dictionary<string, string?>(0, StringComparer.Ordinal));

    private static readonly ConcurrentDictionary<Assembly, Lazy<XmlDocSet>> Cache = new();

    private readonly IReadOnlyDictionary<string, ApiDoc> _entries;
    private readonly IReadOnlyDictionary<string, string?> _inheriting;

    private XmlDocSet(IReadOnlyDictionary<string, ApiDoc> entries, IReadOnlyDictionary<string, string?> inheriting)
    {
        _entries = entries;
        _inheriting = inheriting;
    }

    public bool IsEmpty => _entries.Count == 0 && _inheriting.Count == 0;

    /// <summary>
    /// Gets whether the entry for <paramref name="id"/> takes its text from <c>inheritdoc</c>, and
    /// the documentation id it names, or <see langword="null"/> when it inherits implicitly.
    /// </summary>
    public bool Inherits(string id, out string? source) => _inheriting.TryGetValue(id, out source);

    public int Count => _entries.Count;

    public ApiDoc? Find(string id) =>
        id.Length != 0 && _entries.TryGetValue(id, out ApiDoc? doc) ? doc : null;

    /// <summary>
    /// Returns the documentation for <paramref name="assembly"/>, parsing the XML file on first
    /// use and reusing the parsed result for the lifetime of the process.
    /// </summary>
    public static XmlDocSet ForAssembly(Assembly assembly) =>
        Cache.GetOrAdd(
            assembly,
            static key => new Lazy<XmlDocSet>(
                () => Load(ResolvePath(key)),
                LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;

    /// <summary>
    /// Parses an XML documentation file, or returns <see cref="Empty"/> when the path is null, the
    /// file does not exist, cannot be read, or does not parse.
    /// </summary>
    public static XmlDocSet Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !FileExists(path))
            return Empty;

        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };
            using XmlReader reader = XmlReader.Create(path, settings);
            XDocument document = XDocument.Load(reader);
            return Parse(document);
        }
        catch
        {
            return Empty;
        }
    }

    /// <summary>
    /// Locates the XML documentation file for an assembly.
    /// </summary>
    /// <remarks>
    /// Prefers the file next to the loaded module and falls back to the application base
    /// directory, which is where a self-extracting single-file host places bundled content.
    /// Returns <see langword="null"/> when the assembly has no physical file and no matching file
    /// exists next to the host.
    /// </remarks>
    [UnconditionalSuppressMessage(
        "SingleFile",
        "IL3002",
        Justification = "The module path is probed only to find a sibling file; absence is handled.")]
    public static string? ResolvePath(Assembly assembly)
    {
        var candidates = new List<string>(2);
        try
        {
            string module = assembly.ManifestModule.FullyQualifiedName;
            if (module.Length > 0 && !module.StartsWith('<'))
                candidates.Add(module);
        }
        catch
        {
        }

        if (assembly.GetName().Name is { Length: > 0 } simple)
            candidates.Add(Path.Combine(AppContext.BaseDirectory, simple + ".dll"));

        foreach (string candidate in candidates)
        {
            try
            {
                string path = Path.ChangeExtension(candidate, ".xml");
                if (FileExists(path))
                    return path;
            }
            catch (ArgumentException)
            {
            }
        }

        return null;
    }

    private static bool FileExists(string path)
    {
        try
        {
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    private static XmlDocSet Parse(XDocument document)
    {
        IEnumerable<XElement>? entries = document.Root?.Element("members")?.Elements("member");
        if (entries is null)
            return Empty;

        var parsed = new Dictionary<string, ApiDoc>(StringComparer.Ordinal);
        var inheriting = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (XElement entry in entries)
        {
            string? id = entry.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(id))
                continue;
            if (entry.Element("inheritdoc") is { } inheritdoc)
                inheriting[id.Trim()] = inheritdoc.Attribute("cref")?.Value.Trim();
            if (Describe(entry) is { } doc)
                parsed.TryAdd(id.Trim(), doc);
        }

        return parsed.Count == 0 && inheriting.Count == 0 ? Empty : new XmlDocSet(parsed, inheriting);
    }

    private static ApiDoc? Describe(XElement entry)
    {
        string? summary = Text(entry.Element("summary"));
        string? returns = Text(entry.Element("returns"));
        string? remarks = Text(entry.Element("remarks"));

        ApiDocParameter[] parameters = entry.Elements("param")
            .Select(element => new ApiDocParameter(element.Attribute("name")?.Value?.Trim() ?? "", Text(element) ?? ""))
            .Where(parameter => parameter.Name.Length > 0 && parameter.Text.Length > 0)
            .ToArray();

        ApiDocException[] exceptions = entry.Elements("exception")
            .Select(element => new ApiDocException(Cref(element.Attribute("cref")?.Value) ?? "", Text(element) ?? ""))
            .Where(exception => exception.Type.Length > 0)
            .ToArray();

        if (summary is null &&
            returns is null &&
            remarks is null &&
            parameters.Length == 0 &&
            exceptions.Length == 0)
        {
            return null;
        }

        return new ApiDoc(
            summary,
            returns,
            remarks,
            parameters.Length == 0 ? null : parameters,
            exceptions.Length == 0 ? null : exceptions);
    }

    /// <summary>
    /// Flattens the inline markup of a documentation element into a single line: cross references
    /// become their target name, <c>paramref</c> and <c>typeparamref</c> become the referenced
    /// name, block elements become spaces, and every whitespace run collapses to one space.
    /// </summary>
    /// <returns>The flattened text, or <see langword="null"/> when the element is absent or blank.</returns>
    public static string? Text(XElement? element)
    {
        if (element is null)
            return null;
        var builder = new StringBuilder();
        Flatten(element, builder);
        return Collapse(builder);
    }

    private static void Flatten(XElement element, StringBuilder builder)
    {
        foreach (XNode node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;
                case XElement child:
                    Inline(child, builder);
                    break;
            }
        }
    }

    private static void Inline(XElement element, StringBuilder builder)
    {
        switch (element.Name.LocalName)
        {
            case "see":
            case "seealso":
                if (element.IsEmpty || element.Nodes().All(node => node is XText text && text.Value.Trim().Length == 0))
                {
                    builder.Append(' ');
                    builder.Append(
                        Cref(element.Attribute("cref")?.Value) ??
                        element.Attribute("langword")?.Value ??
                        element.Attribute("href")?.Value ??
                        "");
                    builder.Append(' ');
                }
                else
                {
                    Flatten(element, builder);
                }
                break;
            case "paramref":
            case "typeparamref":
                builder.Append(' ').Append(element.Attribute("name")?.Value ?? "").Append(' ');
                break;
            case "para":
            case "item":
            case "listheader":
            case "br":
                builder.Append(' ');
                Flatten(element, builder);
                builder.Append(' ');
                break;
            default:
                Flatten(element, builder);
                break;
        }
    }

    private static string? Cref(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        string trimmed = value.Trim();
        return trimmed.Length > 2 && trimmed[1] == ':' && char.IsLetter(trimmed[0])
            ? trimmed[2..]
            : trimmed;
    }

    private static string? Collapse(StringBuilder source)
    {
        var builder = new StringBuilder(source.Length);
        bool pending = false;
        for (int index = 0; index < source.Length; index++)
        {
            char character = source[index];
            if (char.IsWhiteSpace(character))
            {
                pending = builder.Length > 0;
                continue;
            }
            if (pending)
            {
                if (!Tight(character) && !Opening(builder[^1]))
                    builder.Append(' ');
                pending = false;
            }
            builder.Append(character);
        }
        return builder.Length == 0 ? null : builder.ToString();
    }

    private static bool Tight(char character) =>
        character is '.' or ',' or ';' or ':' or ')' or ']' or '}' or '!' or '?';

    private static bool Opening(char character) =>
        character is '(' or '[' or '{';
}
