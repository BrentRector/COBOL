// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

/// <summary>
/// The ONE home for every OO C# / file-key naming convention (P9 R8 — kills the four scattered
/// <c>__GET_</c>/<c>__SET_</c> accessor-name builders and the loose <c>__FACTORY</c>/band strings).
/// A LEADING <c>__</c> cannot begin a COBOL-derived name (§8.3.2.1: "The hyphen or underscore shall not appear as the
/// first or last character in such words", so <see cref="DataItem.Sanitize"/> never yields a leading <c>__</c>), so a
/// name that BEGINS with one of these tags never collides with a user name — the §8.4.5/§11.7.4 GR1a
/// implementor-defined externalized-name license. ⛔ A <c>__</c> INSIDE or AT THE END of a name is no such guarantee:
/// the word mapping writes each hyphen as an underscore, so <c>X--FACTORY</c> is <c>X__FACTORY</c> (kb/Work PB2921).
/// Every name synthesized from a user word is therefore a PREFIX family, never a suffix.
/// NOTE — the <c>::EXT::</c> band is a WIRE CONTRACT with the runtime: <c>CobolFile.Register*</c> and
/// <c>ExternalStore</c> recognize the prefix on the emitted key (ISO §13.18.22.4 GR4a run-unit sharing), so the
/// runtime carries its own copy of that spelling; change either side only with the other.
/// </summary>
public static class NamingConvention
{
    /// <summary>The factory singleton class of the class whose C# name is <paramref name="classCsName"/> (brief D11 —
    /// <c>__FACTORY_FOO</c>, a REAL sibling class; the runtime's standard class BASE is <c>__FACTORY_BASE</c>). A
    /// prefix family, so no class-name can spell it: the suffix form <c>FOO__FACTORY</c> it replaced was also the
    /// spelling of the legal class-name <c>FOO--FACTORY</c> (kb/Work PB2921).</summary>
    public static string FactoryClassName(string classCsName) => FactoryClassPrefix + classCsName;

    /// <summary>The <see cref="FactoryClassName"/> family's tag.</summary>
    public const string FactoryClassPrefix = "__FACTORY_";

    /// <summary>The C# name of a METHOD-ID spelled like its own CLASS-ID (a C# member may not be named like its
    /// enclosing type, CS0542; the method-name and the class-name sit in different source elements, so the COBOL is
    /// legal — <c>OoClassTable.AddMethods</c>). A prefix family: the <c>_M</c> suffix it replaced was also the
    /// spelling of the method-name <c>FOO-M</c> beside it (CS0111; kb/Work PB2921).</summary>
    public static string ClassNamedMethodName(string methodCsName) => ClassNamedMethodPrefix + methodCsName;

    /// <summary>The <see cref="ClassNamedMethodName"/> family's tag.</summary>
    public const string ClassNamedMethodPrefix = "__method_";

    /// <summary>The factory singleton field (§9.3.14.2 — "created before it is first referenced").</summary>
    public const string FactoryInstanceField = "__Instance";

    /// <summary>The predefined New creation method (§16.2.1.2 GR1 — ACTIVE-CLASS covariant creation).</summary>
    public const string FactoryNewMethod = "__New";

    /// <summary>The run-unit EXTERNAL file-connector key band (§13.18.22.4 GR4a — ONE connector per run unit;
    /// the runtime recognizes this prefix: the wire contract noted above).</summary>
    public const string ExternalFileBand = "::EXT::";

    /// <summary>The per-object INSTANCE file-connector key band (M2-OO-1i, §9.1.4 — one connector per object).</summary>
    public const string InstanceFileBand = "::INST::";

    /// <summary>The FACTORY file-connector key band (one connector per class singleton).</summary>
    public const string FactoryFileBand = "::FACT::";

    /// <summary>The pinned accessor-roster name of a property GET method (§11.7.4 GR1a —
    /// <c>__GET_&lt;P&gt;</c>; override/0829/implements machinery applies to accessors unchanged).</summary>
    public static string GetAccessorName(string propertyName) =>
        "__GET_" + DataItem.WordIdentifier(propertyName);

    /// <summary>The pinned accessor-roster name of a property SET method (<c>__SET_&lt;P&gt;</c>).</summary>
    public static string SetAccessorName(string propertyName) =>
        "__SET_" + DataItem.WordIdentifier(propertyName);

    // ── COBOL-word-derived synthesized C# names (kb/Work PB973) ────────────────────────────────────────────
    // A synthesized member whose C# name EMBEDS a user-defined word lives in the same identifier space as the
    // emitter's own fixed `__X` members (`__N` the paragraph count, `__pc`, `__V`, `__NEW`, `__PROP`, …). A family
    // is collision-free only when it carries a NON-EMPTY TAG that no fixed emitter name starts with: the bare
    // `"__" + WORD` shape (the old method-formal parameter name) let a LINKAGE formal named N become `__N` and
    // shadow the paragraph-count constant (CS1628 inside the dispatch local function). The families are listed
    // ONCE here; `SynthesizedNameFamilyDriftTests` (Unit) proves every emitter-fixed `__X` literal in the compiler
    // sources falls outside every family, and that no other site derives a `__` name from a user word.

    /// <summary>Every COBOL-word-derived family prefix (the drift test's roster). Adding a family = one entry.</summary>
    public static readonly IReadOnlyList<string> CobolWordDerivedPrefixes =
    [
        FormalParameterPrefix, AddressCarrierPrefix, InstanceFileKeyPrefix, CapacityRegisterPrefix,
        DebugRegisterPrefix, SumCounterPrefix, "__GET_", "__SET_", FactoryClassPrefix, ClassNamedMethodPrefix,
    ];

    /// <summary>A method formal's C# <c>ref</c> parameter (the carrier; the body reads the capturable LOCAL
    /// named <see cref="DataItem.CsName"/>, and a C# local may not shadow a parameter — CS0136).</summary>
    public const string FormalParameterPrefix = "__formal_";
    /// <summary>A BASED record's address carrier field (ALLOCATE / SET ADDRESS OF).</summary>
    public const string AddressCarrierPrefix = "__addr_";
    /// <summary>A method-scope file's per-object connector-key field.</summary>
    public const string InstanceFileKeyPrefix = "__fkey_";
    /// <summary>An OCCURS DYNAMIC CAPACITY register.</summary>
    public const string CapacityRegisterPrefix = "__cap_";
    /// <summary>A DEBUG-ITEM register.</summary>
    public const string DebugRegisterPrefix = "__dbg_";
    /// <summary>A report SUM counter register.</summary>
    public const string SumCounterPrefix = "__sum_";

    /// <summary>A method formal's C# parameter name (uppercased word; the method's binder uniquifies).</summary>
    public static string FormalParameterName(string cobolWord) =>
        FormalParameterPrefix + DataItem.WordIdentifier(cobolWord);

    /// <summary>A BASED record's address-carrier field name, spelled from the root's C# name — the unit-unique STEM
    /// (<c>DataBinder.InheritedMemberNamesOf</c>), never its COBOL word: a contained program's own BASED <c>B</c>
    /// and a container's GLOBAL BASED <c>B</c> (bridged into the same class) are one word but two stems (<c>B</c>,
    /// <c>B_2</c>), and a word-keyed name made them one member (CS0102; wave 69 Z). A keyword stem's verbatim
    /// <c>@</c> is dropped — the prefix already keeps the name off every keyword.</summary>
    public static string AddressCarrierName(string rootCsName) =>
        AddressCarrierPrefix + rootCsName.TrimStart('@');

    /// <summary>A method-scope file's instance connector-key field name.</summary>
    public static string InstanceFileKeyName(string fileName) => InstanceFileKeyPrefix + DataItem.Sanitize(fileName);

    /// <summary>An OCCURS DYNAMIC table's CAPACITY register name (keyed on the table's own C# name).</summary>
    public static string CapacityRegisterName(string tableCsName) => CapacityRegisterPrefix + tableCsName;

    /// <summary>A DEBUG-ITEM register name (<c>DEBUG-ITEM</c>, <c>DEBUG-LINE</c>, …).</summary>
    public static string DebugRegisterName(string registerWord) => DebugRegisterPrefix + DataItem.Sanitize(registerWord);

    /// <summary>A report SUM counter register (report-name + the entry's ordinal in the RD).</summary>
    public static string SumCounterName(string reportName, int ordinal) =>
        $"{SumCounterPrefix}{DataItem.Sanitize(reportName)}_{ordinal}";
}
