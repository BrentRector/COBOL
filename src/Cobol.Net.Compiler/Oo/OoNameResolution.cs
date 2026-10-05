// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE FUNNEL FOR RESOLVING A WRITTEN object-class-name OR interface-name (kb/Work PB365).
///
/// <para><b>The rule it owns is ISO §8.4.6.4, Scope of object-class-names and interface-names</b>: "The
/// object-class-name of an object class referenced within a source element shall be either the name of the
/// containing object class definition or declared in the REPOSITORY paragraph of that or a containing source
/// element", and the same sentence for interface-names. Every per-statement rule that says "shall be the name
/// of a class specified in the REPOSITORY paragraph" — §11.3.3 SR2 (CLASS-ID INHERITS), §11.4.3 SR1 (FACTORY
/// IMPLEMENTS), §11.6.3 SR2 (INTERFACE-ID INHERITS), §11.8.3 SR1 (OBJECT IMPLEMENTS), §14.2.2 SR8/SR9 (the
/// PROCEDURE DIVISION header's RAISING phrase), §14.9.49.3 SR16/SR17 (USE Format 4) — is an INSTANCE of it, so
/// they are one mechanism and not seven.</para>
///
/// <para><b>What it replaced.</b> Every one of those sites called <c>OoClassTable.Find</c> /
/// <c>FindInterface</c> directly, and <see cref="OoClassTable"/> is built ONCE PER COMPILATION GROUP from every
/// CLASS-ID / INTERFACE-ID definition in the file. So the compiler answered a question about the GROUP where
/// the standard asks about the REFERRING SOURCE ELEMENT: a program with no REPOSITORY paragraph at all could
/// write <c>USE AFTER EXCEPTION OBJECT CF4A</c> and compile clean, and COBOLNET0859 said so out loud — "does
/// not name a class of the compilation group". Widening a set is silent by construction: nothing downstream can
/// notice a reference that should not have resolved.</para>
///
/// <para><b>The scope is computed from the parse tree, not plumbed.</b> §12.3.3 SR1 forbids a configuration
/// section in a contained program, SR2 in a method definition, and SR3 the REPOSITORY paragraph in a factory or
/// instance definition — so the source elements that CAN carry a REPOSITORY are the outermost program, the
/// function definition, the class definition and the interface definition, and §12.3.4 GR1 spreads each one's
/// entries to "each directly or indirectly contained source unit". An ancestor walk from the reference site
/// therefore IS the rule, with no state to thread and nothing to keep in sync; a new reference site gets the
/// right scope by construction (CLAUDE.md rule 5 — the shape that makes the next case automatic).</para>
/// </summary>
public static class OoNameResolution
{
    /// <summary>Which alternatives the referencing construct admits. <c>Either</c> is the USE Format-4 /
    /// USAGE OBJECT REFERENCE shape (a brace group of object-class-name-1 | interface-name-1).</summary>
    public enum Want { Class, Interface, Either }

    /// <summary>A resolution outcome. <see cref="Class"/> and <see cref="Interface"/> are mutually exclusive:
    /// classes and interfaces share ONE name namespace (<see cref="OoClassTable"/> rejects a collision as
    /// COBOLNET0840), so at most one can be set.</summary>
    public readonly record struct Result(OoClassSymbol? Class, OoInterfaceSymbol? Interface)
    {
        public bool Ok => Class is not null || Interface is not null;
    }

    /// <summary>Resolve <paramref name="name"/> as an object-class-name / interface-name visible from
    /// <paramref name="site"/>, reporting through <paramref name="descriptor"/> when it is not. The message
    /// distinguishes the two failures, because their fixes differ: a name that is DEFINED in the group but not
    /// declared in scope needs a REPOSITORY entry (§8.4.6.4); a name that is defined nowhere needs a
    /// definition.</summary>
    /// <param name="where">The reference site, e.g. <c>"declarative section 'EO-SEC': USE AFTER EXCEPTION OBJECT"</c>.</param>
    /// <param name="code">The REFERENCING construct's own diagnostic code — each site keeps the code its
    /// goldens pin; what the funnel owns is the SET and the message, not the code.</param>
    /// <param name="ruleCitation">The referencing construct's own syntax rule, e.g. <c>"ISO §14.9.49.3 SR16/SR17"</c>.</param>
    public static Result Resolve(OoClassTable? table, EditionContext edition, RuleContext? site, string name,
        Want want, string where, string code, string ruleCitation)
    {
        var found = Lookup(table, site, name, want);
        if (found.Ok) return found;

        // A PARAMETERIZED definition is a skeleton, not a class or interface, so it is never in the table and a
        // reference to it can never resolve (kb/Work PB759). Say which rule, rather than "not defined": the
        // definition IS in the source. ISO §12.3.8.4 GR1 (class) and GR7 (interface) confine the name to the
        // REPOSITORY paragraph, where its only use is as an EXPANDS operand.
        if (table?.IsParameterized(name) == true)
        {
            edition.Error(code, $"{where} '{name}': '{name}' is a parameterized class or interface (its "
                + "CLASS-ID / INTERFACE-ID paragraph has a USING clause), which may be specified only in the "
                + "REPOSITORY paragraph — reference an expansion of it, declared with the EXPANDS phrase "
                + $"({ruleCitation}; ISO §12.3.8.4 GR1/GR7)");
            return default;
        }

        // Defined in the group but out of scope vs. not defined at all — name the actual condition (the old
        // 0859 named neither: it said "does not name a class of the compilation group", which was BOTH the
        // wrong set and, for an interface operand, the wrong rule).
        var cls = want is Want.Class or Want.Either ? table?.Find(name) : null;
        bool definedInGroup = cls is not null
            || (want is Want.Interface or Want.Either && table?.FindInterface(name) is not null);
        string kind = want switch
        {
            Want.Class => "a class",
            Want.Interface => "an interface",
            _ => "a class or interface",
        };
        string notDeclared = "it is not the name of the containing class or interface definition and is not "
            + "declared in the REPOSITORY paragraph of this source element or a containing one "
            + $"({ruleCitation}; ISO §8.4.6.4)";
        edition.Error(code,
            cls is { IsStandard: true }
                // The standard class is reached exactly like a written one — through a REPOSITORY class-specifier
                // (§12.3.8.3 SR6 b): name the missing entry, which is the whole fix.
                ? $"{where} '{name}': '{name}' is the standard class BASE (ISO §16.1), but {notDeclared} — "
                  + $"add `CLASS {name}` to the REPOSITORY paragraph"
            : definedInGroup
                ? $"{where} '{name}': {kind} of that name is defined in this compilation group, but {notDeclared}"
            : $"{where} '{name}': no {kind[(kind.IndexOf(' ') + 1)..]} of that name is defined in this compilation "
              + $"group ({ruleCitation})");
        return default;
    }

    /// <summary>The NON-DIAGNOSING half, for the sites that PARTITION on "is this word a class-name here?" —
    /// the PROCEDURE DIVISION / METHOD-ID RAISING word that may instead be an exception-name (§14.2.2 SR7 vs
    /// SR8/SR9), and INVOKE's receiver, which may instead be an identifier. The partition uses the SAME scoped
    /// set: a class the source element cannot reference is not a class-name in that position.</summary>
    public static Result Lookup(OoClassTable? table, RuleContext? site, string name, Want want)
    {
        if (table is null) return default;
        var scope = table.RepositoryScopeFor(site);
        // A specifier with `AS literal-n` names the definition whose EXTERNALIZED name is the literal (§12.3.8.4 GR2,
        // kb/Work PB974); without it the declared word names the definition of that name.
        if (want is Want.Class or Want.Either && scope.ClassTarget(name, out var clsExt)
            && (clsExt is null ? table.Find(name) : table.FindByExternalizedName(clsExt)) is { } cls)
            return new Result(cls, null);
        if (want is Want.Interface or Want.Either && scope.InterfaceTarget(name, out var ifcExt)
            && (ifcExt is null ? table.FindInterface(name) : table.FindInterfaceByExternalizedName(ifcExt)) is { } ifc)
            return new Result(null, ifc);
        return default;
    }
}

/// <summary>
/// The set of object-class-names and interface-names ONE source element may reference (ISO §8.4.6.4): its own
/// REPOSITORY declarations plus every containing source element's (§12.3.4 GR1), plus the name of the
/// containing class or interface definition itself — §12.3.8.2 GR5/GR8 say the self-naming specifier "is
/// ignored", so the definition's own name is in scope whether or not it is declared.
/// </summary>
public sealed class OoRepositoryScope
{
    /// <summary>The scope of a reference outside every source element that can declare one. It admits nothing:
    /// §8.4.6.4 offers no third source for a visible name.</summary>
    public static readonly OoRepositoryScope Empty = new([], []);

    // Declared name → the EXTERNALIZED name the specifier's `AS literal-n` gives, or null when it wrote none (the
    // containing definition's own name, too): null resolves by the declared word (kb/Work PB974).
    private readonly Dictionary<string, string?> _classes;
    private readonly Dictionary<string, string?> _interfaces;

    private OoRepositoryScope(Dictionary<string, string?> classes, Dictionary<string, string?> interfaces)
    {
        _classes = classes;
        _interfaces = interfaces;
    }

    public bool DeclaresClass(string name) => _classes.ContainsKey(name);
    public bool DeclaresInterface(string name) => _interfaces.ContainsKey(name);

    /// <summary>Whether <paramref name="name"/> is a class-name in this scope, and the externalized name its
    /// specifier's AS phrase gave it (null: resolve by the declared word).</summary>
    public bool ClassTarget(string name, out string? externalized) => _classes.TryGetValue(name, out externalized);

    /// <summary>The interface twin of <see cref="ClassTarget"/>.</summary>
    public bool InterfaceTarget(string name, out string? externalized) =>
        _interfaces.TryGetValue(name, out externalized);

    /// <summary>Build the scope visible at <paramref name="site"/> by walking its parse-tree ancestors: every
    /// REPOSITORY paragraph on the way out contributes (the reference's own source element first, then each
    /// containing one — §12.3.4 GR1), and a class / interface definition contributes its own name.</summary>
    public static OoRepositoryScope Build(RuleContext? site)
    {
        var classes = new Dictionary<string, string?>(CobolNames.Comparer);
        var interfaces = new Dictionary<string, string?>(CobolNames.Comparer);
        for (RuleContext? c = site; c is not null; c = c.Parent)
        {
            switch (c)
            {
                case Core.ClassDefinitionContext cd:
                    // className()[0] is the class's OWN name; the rest are the INHERITS operands
                    // (§11.3.2's `[ INHERITS FROM { object-class-name-2 } … ]`), which are REFERENCES.
                    // §12.3.8.3 SR5/SR8: a specifier naming the containing definition "is ignored" — its own name
                    // resolves to it, whatever an inner entry wrote.
                    if (cd.classIdParagraph()?.className().FirstOrDefault() is { } cn) classes[cn.GetText()] = null;
                    break;
                case Core.InterfaceDefinitionContext idf:
                    if (idf.interfaceName().FirstOrDefault() is { } inm) interfaces[inm.GetText()] = null;
                    break;
            }
            // The environment division of THIS context, when it directly carries one. Only a source-element
            // context does; a statement or clause context has no such child, so the test is a cheap miss.
            for (int i = 0; i < c.ChildCount; i++)
                if (c.GetChild(i) is Core.EnvironmentDivisionContext env)
                    Collect(env, classes, interfaces);
        }
        return classes.Count == 0 && interfaces.Count == 0 ? Empty : new OoRepositoryScope(classes, interfaces);
    }

    private static void Collect(Core.EnvironmentDivisionContext env, Dictionary<string, string?> classes,
        Dictionary<string, string?> interfaces)
    {
        // The source element's OWN specifiers are collected before a containing element's (the ancestor walk runs
        // outward), so TryAdd keeps the innermost declaration — §12.3.4 GR1 spreads a container's entries to
        // contained units, and a contained unit's own specifier for the same name is the one in force there.
        // The literal is read by the PURE ExternalizedName.Peek: DataBinder.BindSpecifierExternalizedName reports
        // on it, once, and a rejected literal resolves by the declared word — its own fallback.
        foreach (var re in SpecifierEntries(env))
        {
            string? ext = re.externalizedNamePhrase() is { } asPhrase ? ExternalizedName.Peek(asPhrase.literal()) : null;
            if (re.className() is { } cn) classes.TryAdd(cn.GetText(), ext);
            else if (re.interfaceName() is { } inm) interfaces.TryAdd(inm.GetText(), ext);
        }
    }

    /// <summary>The class-specifiers and interface-specifiers (§12.3.8.2) of the REPOSITORY paragraph directly
    /// in <paramref name="env"/> — with or without an EXPANDS phrase, whose object-class-name-1 / interface-name-2
    /// is DECLARED exactly as a plain specifier's is (kb/Work PB759). The one walk this scope and
    /// <see cref="OoExpansion"/> (a parameterized definition's own formals, §11.3.3 SR8 / §11.6.3 SR4) share.</summary>
    internal static IEnumerable<Core.RepositoryEntryContext> SpecifierEntries(Core.EnvironmentDivisionContext? env)
        => RepositoryEntries(env).Where(r => r.CLASS() is not null || r.INTERFACE() is not null);

    /// <summary>Every specifier of the REPOSITORY paragraph directly in <paramref name="env"/> — the scope a
    /// parameterized definition's parameter-names are declared in (<c>DataBinder.DeclareParameterNames</c>, kb/Work
    /// PB1744), whose FUNCTION … INTRINSIC specifiers are not class- or interface-specifiers.</summary>
    internal static IEnumerable<Core.RepositoryEntryContext> RepositoryEntries(Core.EnvironmentDivisionContext? env)
        => env?.configurationSection()?.configurationParagraph()
               .Select(p => p.repositoryParagraph()).Where(r => r is not null)
               .SelectMany(r => r!.repositoryEntry()) ?? [];
}
