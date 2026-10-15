// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Compiler.Oo;
using CobolNet.Runtime;
using static CobolNet.CodeGen.Emit.EmitText;

namespace CobolNet.CodeGen;

/// <summary>
/// ⛔ THE .NET HOST ENTRY — A GENERATED CALLER (DESIGN-external-repository §4.3, §4.4; kb/Work PB2099). For every
/// outermost program and every function unit — a definition, or a prototype the group does not also define — the module
/// declares one <c>public static class</c> whose <c>Call</c> is the .NET counterpart of a CALL statement naming the unit,
/// and whose <c>Cancel</c> (a program's only) is the counterpart of a CANCEL statement. ISO §14.2.3 13): "When either the
/// activating or the activated runtime element is other than a COBOL runtime element, the implementor shall specify the
/// restrictions and mechanisms for all supported language products"; this is that mechanism for .NET, and its one
/// restriction — the landing loses nothing — is <see cref="HostLanding"/>'s.
/// <para>⛔ <b>IT IS A CALLER, NOT A SECOND BOUNDARY.</b> The body is exactly the parts §4.3 lists, which the §17.3 drift
/// test asserts on the emitted corpus: <c>__CobolModule.EnsureRegistered()</c> (a definition's entry only); one
/// <see cref="HostLanding"/> landing per formal into a detached storage of the formal's own description, with the alias
/// grouping (<see cref="HostLanding.SharesStorage"/>); one <c>CobolArg</c> per formal over that storage, stating the
/// description a CALL site that checks EC-PROGRAM-ARG-MISMATCH states; exactly one <c>ProgramRegistry.CallProgram</c> with
/// <c>callerPath: null</c> (the outermost boundary, kb/Work R48) and <c>siteArgMismatchChecking: true</c>; a
/// <c>finally</c> of <see cref="HostLanding"/> decodes; the RETURNING decode. No catch clause, no instance, no mapping of
/// an EC condition: every activation step stays <c>ProgramTable.CallProgram</c>'s.</para>
/// <para>⛔ <b>THE DETACHED STORAGE IS IMAGE-STORED FOR A FIXED-POINT FORMAL.</b> ISO §14.2.3 9) allocates "a data item with
/// the same description and the same number of bytes as the formal parameter"; the entry holds a fixed-point one as its
/// record image (the CALL lane's TEXT crossing of an image-stored numeric item, with the item's <c>NumProfile</c>), so the
/// write-back sees the CONTENT the activated program left — the content a REDEFINES alias or a reference modification can
/// leave — and decodes it only when the class test NUMERIC is true of it (ISO §8.8.4.4.4 3) n);
/// <see cref="HostLanding.TryDecode"/>). A native cell could not hold such content to be refused.</para>
/// </summary>
internal sealed class HostEntryEmitter(SignEncoding signEncoding, OoClassTable? classes)
{
    /// <summary>The units that publish a host entry: every outermost unit, except a prototype whose definition the same
    /// group compiles (that definition's entry is the one class for the name — DESIGN-external-repository §7.2).</summary>
    internal static IEnumerable<BoundUnit> EntryUnits(IReadOnlyList<BoundUnit> units) =>
        units.Where(u => u.Parent is null && !(u.IsPrototype && units.Any(d => d is { Parent: null, IsPrototype: false }
            && d.IsFunction == u.IsFunction && string.Equals(d.ExternalizedName, u.ExternalizedName, StringComparison.Ordinal)))
            && ElementaryOnly(u));

    /// <summary>WIP (kb/Work PB2099, wave 1079 checkpoint): a unit with a GROUP formal or RETURNING item publishes its
    /// entry with the published record struct (DESIGN-external-repository §4.3's last table row), the slice's next step;
    /// until then only units whose every formal and RETURNING item is elementary publish one.</summary>
    private static bool ElementaryOnly(BoundUnit u) =>
        u.Data.LinkageFormals.All(f => !f.Item.IsGroup) && u.Data.LinkageReturning is not { IsGroup: true };

    /// <summary>Emit the host-entry class of every <see cref="EntryUnits"/> unit, each named by the one allocator in the
    /// module namespace's type scope (<paramref name="moduleTypes"/> — the names the module already declares there).</summary>
    internal void Emit(IReadOnlyList<BoundUnit> units, IEnumerable<string> moduleTypes, CodeWriter w)
    {
        var scope = CsNameScope.ModuleTypes(moduleTypes);
        foreach (var unit in EntryUnits(units))
        {
            w.Line();
            EmitEntry(unit, CsNames.Allocate(DataItem.Sanitize(unit.Name), scope), w);
        }
    }

    /// <summary>One formal (or the RETURNING item) as the entry handles it.</summary>
    private sealed record Item(DataItem Description, HostType Type, string Name, string Cell, string? Profile,
        bool ByValue, bool Optional);

    private void EmitEntry(BoundUnit unit, string className, CodeWriter w)
    {
        var members = CsNameScope.HostEntry(className);
        var parameters = members.Nested();
        int serial = 0;
        var profiles = new List<(string Field, string Init)>();
        Item Describe(DataItem description, string name, bool byValue, bool optional)
        {
            int n = serial++;
            HostType type = HostSurface.TypeOf(description);
            string? profile = null;
            if (description.Pic is { Category: PicCategory.Numeric, Usage: not Usage.Index } pic)
            {
                profile = $"__hp{n}";
                profiles.Add((profile, pic.ProfileInitializer(signEncoding)));
            }
            return new Item(description, type, name, $"__hc{n}", profile, byValue, optional);
        }

        var formals = unit.Data.LinkageFormals
            .Select(f => Describe(f.Item, CsNames.Allocate(DataItem.Sanitize(f.Item.CobolName ?? "FILLER"), parameters),
                f.ByValue, f.Optional))
            .ToList();
        Item? returning = unit.Data.LinkageReturning is { } r ? Describe(r, "__hr", false, false) : null;

        string kind = unit.IsFunction ? "FUNCTION-ID" : "PROGRAM-ID";
        w.Line($"/// <summary>{kind} {unit.Name} (ISO §14.2.3 13), DESIGN-external-repository §4.3): activates the "
            + $"{(unit.IsFunction ? "function" : "program")} through the run unit's activation boundary (§14.9.4.4 3)) as "
            + "a CALL statement naming it does, landing each argument exactly or refusing it.</summary>");
        using (w.Block($"public static class {className}"))
        {
            foreach (var (field, init) in profiles)
                w.Line($"private static readonly NumProfile {field} = {init};");
            string returnType = returning is null ? "void" : returning.Type.CsType!;
            string signature = string.Join(", ", formals.Select(ParameterText));
            using (w.Block($"public static {returnType} Call({signature})"))
                EmitCallBody(unit, formals, returning, w);
            if (!unit.IsFunction)
            {
                w.Line($"/// <summary>CANCEL {unit.Name} (ISO §14.9.5) in the current run unit.</summary>");
                w.Line($"public static void Cancel() => ProgramRegistry.Cancel({CsLiteral(unit.ExternalizedName)}, null);");
            }
        }
    }

    /// <summary>A formal's <c>Call</c> parameter: BY VALUE by value, BY REFERENCE by <c>ref</c> (the variable the
    /// activated program's stores reach, ISO §14.2.3 8)), and nullable when the formal is OPTIONAL (null is the omitted
    /// argument, §14.9.4.4 11)).</summary>
    private static string ParameterText(Item f) =>
        $"{(f.ByValue ? "" : "ref ")}{f.Type.CsType}{(f.Optional && !f.Type.CsType!.EndsWith('?') ? "?" : "")} {f.Name}";

    /// <summary>Whether the host type is a value type, so an OPTIONAL formal's parameter is a <see cref="Nullable{T}"/>.</summary>
    private static bool IsValueType(HostType t) =>
        t.Kind is HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec or HostTypeKind.Float or HostTypeKind.Index
        || t is { Kind: HostTypeKind.Pointer, CsType: "ProgramPointer" or "FunctionPointer" };

    private void EmitCallBody(BoundUnit unit, List<Item> formals, Item? returning, CodeWriter w)
    {
        if (!unit.IsPrototype)
            w.Line($"{ModuleNamespace.RegistrarClass}.EnsureRegistered();   // this assembly's programs, once per run unit (§11.2)");
        // 1. Landing, before anything is activated: each host value into a detached storage of its formal's description,
        //    exactly or refused (HostLanding); one variable given to two formals of one description is one storage.
        for (int i = 0; i < formals.Count; i++)
        {
            var f = formals[i];
            string landed = Storage(f);
            if (f.Optional) landed = $"{f.Name} is null ? null : {landed}";
            string aliases = "";
            if (!f.ByValue)
                for (int j = i - 1; j >= 0; j--)
                {
                    var e = formals[j];
                    // Only two ref parameters of one CLR type can refer to one variable (§4.3 "One storage for one
                    // variable passed twice").
                    if (e.ByValue || e.Type != f.Type || (e.Optional != f.Optional && IsValueType(e.Type))) continue;
                    bool same = OoConformance.DescriptionMismatch(e.Description, f.Description) is null
                        && OoConformance.DescriptionMismatch(f.Description, e.Description) is null;
                    aliases += $"HostLanding.SharesStorage(global::System.Runtime.CompilerServices.Unsafe.AreSame(ref {e.Name}, "
                        + $"ref {f.Name}), {(same ? "true" : "false")}, {CsLiteral(e.Name)}, {CsLiteral(f.Name)}) ? {e.Cell} : ";
                }
            w.Line($"var {f.Cell} = {aliases}{landed};");
        }
        if (returning is not null)
            w.Line($"var {returning.Cell} = {ReceiverSeed(returning)};");
        // 2. One CobolArg per formal over that storage, as a CALL site that checks EC-PROGRAM-ARG-MISMATCH renders it.
        string args = formals.Count == 0 ? "global::System.Array.Empty<CobolArg>()"
            : $"new CobolArg[] {{ {string.Join(", ", formals.Select(Arg))} }}";
        string ret = returning is null ? "null" : ArgOver(returning, CobolPassMode.Reference);
        string notFound = unit.IsFunction ? ", notFoundEc: \"EC-FUNCTION-NOT-FOUND\"" : "";
        bool decodes = formals.Any(f => !f.ByValue) || returning is not null;
        if (decodes) w.Line("global::System.FormatException? __hbad = null;");
        // 3. The one activation boundary — the outermost one (callerPath null), with the activating half of GR3 d) on.
        string call = $"ProgramRegistry.CallProgram({CsLiteral(unit.ExternalizedName)}, null, {args}, {ret}{notFound}, "
            + "siteArgMismatchChecking: true);";
        if (formals.Any(f => !f.ByValue))
        {
            w.Line($"try {{ {call} }}");
            // 4. Write-back, whether the boundary returned or threw: each BY REFERENCE variable decoded by its formal's
            //    description, by the one validity rule; content that fails it leaves the variable unchanged.
            using (w.Block("finally"))
                foreach (var f in formals.Where(f => !f.ByValue))
                    w.Line(f.Optional ? $"if ({f.Cell} is not null) {{ {Decode(f, f.Name)} }}" : Decode(f, f.Name));
        }
        else w.Line(call);
        if (returning is not null)
        {
            w.Line($"{returning.Type.CsType} __hres = default;");
            w.Line(Decode(returning, "__hres"));
        }
        if (decodes) w.Line("if (__hbad is not null) throw __hbad;");
        if (returning is not null) w.Line("return __hres;");
    }

    /// <summary>The detached storage of a formal's description, landed from its host value (§14.2.3 9)/10)).</summary>
    private string Storage(Item f)
    {
        string value = f.Optional && IsValueType(f.Type) ? $"{f.Name}.GetValueOrDefault()" : f.Name;
        DataItem d = f.Description;
        PicInfo pic = d.Pic ?? throw Unsupported(f);
        string member = "null";
        return f.Type.Kind switch
        {
            HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec =>
                $"ManagedPointer<string>.Cell(CobolNum.FormatImage({Unsigned16(pic, $"HostLanding.Land({value}, {f.Profile}, {CsLiteral(f.Name)}, {member})")}, {f.Profile}))",
            HostTypeKind.Float or HostTypeKind.Index or HostTypeKind.Pointer or HostTypeKind.ObjectReference =>
                $"ManagedPointer<{pic.ClrType}>.Cell({value})",
            HostTypeKind.Text when d.IsAnyLength =>
                $"ManagedPointer<string>.Cell(HostLanding.LandVariable({value}, CobolArg.Unstated, {Bool(pic.Category is PicCategory.Boolean)}, {CsLiteral(f.Name)}, {member}))",
            HostTypeKind.Text when d.IsDynamicLength =>
                $"ManagedPointer<string>.Cell(HostLanding.LandVariable({value}, {d.DynMaxSize}, {Bool(pic.Category is PicCategory.Boolean)}, {CsLiteral(f.Name)}, {member}))",
            HostTypeKind.Text =>
                $"ManagedPointer<string>.Cell(HostLanding.LandText({value}, {CallEmitter.BoundaryImageWidth(d)}, {Bool(d.Justified)}, {Bool(pic.Category is PicCategory.Boolean)}, {CsLiteral(f.Name)}, {member}))",
            _ => throw Unsupported(f),
        };
    }

    /// <summary>An UNSIGNED 16-byte binary item's landed value is its container's BITS (<c>HostLanding.Land</c>), which the
    /// image is formatted from as the unsigned value it is (<see cref="CobolNum.FormatImage(UInt128, in NumProfile)"/>).</summary>
    private static string Unsigned16(PicInfo pic, string landed) =>
        IsUnsigned16(pic) ? $"unchecked((UInt128){landed})" : landed;

    private static bool IsUnsigned16(PicInfo pic) =>
        pic is { Truncation: NumericTruncation.BinaryCapacity, Signed: false, StorageWidth: 16 };

    /// <summary>The RETURNING item's storage, allocated by the entry as an activating COBOL element allocates it (ISO
    /// §14.2.3 6) NOTE 1): the initial content of its description's storage form.</summary>
    private static string ReceiverSeed(Item r)
    {
        DataItem d = r.Description;
        PicInfo pic = d.Pic ?? throw Unsupported(r);
        return r.Type.Kind switch
        {
            HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec =>
                $"ManagedPointer<string>.Cell(CobolNum.FormatImage((Int128)0, {r.Profile}))",
            HostTypeKind.Float or HostTypeKind.Index or HostTypeKind.Pointer or HostTypeKind.ObjectReference =>
                $"ManagedPointer<{pic.ClrType}>.Cell(default!)",
            HostTypeKind.Text when d.IsAnyLength || d.IsDynamicLength => "ManagedPointer<string>.Cell(\"\")",
            HostTypeKind.Text => $"ManagedPointer<string>.Cell(new string('{(pic.Category is PicCategory.Boolean ? '0' : ' ')}', {CallEmitter.BoundaryImageWidth(d)}))",
            _ => throw Unsupported(r),
        };
    }

    /// <summary>A formal's <c>CobolArg</c>: the omitted-argument carrier for an OPTIONAL formal given null (§14.9.4.4
    /// 11)), else <see cref="ArgOver"/> in the formal's own mode.</summary>
    private string Arg(Item f)
    {
        string arg = ArgOver(f, f.ByValue ? CobolPassMode.Value : CobolPassMode.Reference);
        return f.Optional
            ? $"{f.Cell} is null ? new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, ManagedPointer.Null, null) : {arg}"
            : arg;
    }

    /// <summary>⛔ THE ARGUMENT OVER THE DETACHED STORAGE, stating what a CALL site that checks EC-PROGRAM-ARG-MISMATCH
    /// states for storage of this description (<c>CallEmitter</c>'s carried description, kb/Work PB873/PB165): a numeric
    /// item's <c>NumProfile</c>, the fixed storage length (§14.8.2.3.2 1)), and the §14.8.2 description, so §14.9.4.4 3) d)
    /// compares it with the activated program's formal when that program enables the check.</summary>
    private string ArgOver(Item f, CobolPassMode mode)
    {
        var facts = new List<string>(2);
        int length = StorageLength(f);
        if (length != RuntimeApi.UnstatedBoundaryLength) facts.Add($"Length: {length}");
        if (ActivationDescriptions.Of(f.Description, classes) is { } d)
            facts.Add($"Description: {RuntimeApi.ActivationDescriptionNew(d)}");
        return $"new CobolArg({RuntimeApi.PassModeText(mode)}, {f.Cell}, {f.Profile ?? "null"}"
            + string.Concat(facts.Select(x => ", " + x)) + ")";
    }

    /// <summary>The fixed storage length the argument states (<c>CallEmitter.ArgumentLength</c>'s rule for storage of this
    /// description): a fixed-length character item's positions, a fixed-point or floating-point numeric item's
    /// BYTE-LENGTH; none for an index, pointer or object-reference item, a DYNAMIC LENGTH or an ANY LENGTH item.</summary>
    private static int StorageLength(Item f) => f.Type.Kind switch
    {
        HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec or HostTypeKind.Float => f.Description.ImageWidth,
        HostTypeKind.Text when !f.Description.IsAnyLength && !f.Description.IsDynamicLength
            => CallEmitter.BoundaryImageWidth(f.Description),
        _ => RuntimeApi.UnstatedBoundaryLength,
    };

    /// <summary>⛔ THE ONE VALIDITY RULE (DESIGN-external-repository §4.3 "Write-back and RETURNING"): the storage's content
    /// decoded into <paramref name="target"/> by its description — a numeric content only when the class test NUMERIC is
    /// true of it (<see cref="HostLanding.TryDecode"/>), keeping the first refusal in <c>__hbad</c> and the target
    /// unchanged. Decoding never narrows: the host type holds every value the storage can (<see cref="HostSurface"/>).</summary>
    private static string Decode(Item f, string target)
    {
        PicInfo pic = f.Description.Pic ?? throw Unsupported(f);
        string name = CsLiteral(f.Name == "__hr" ? f.Description.CobolName ?? "RETURNING" : f.Name);
        string scale = $"{pic.Scale}";
        string? convert = f.Type.Kind switch
        {
            HostTypeKind.Integer => f.Type.CsType switch
            {
                "UInt128" => "unchecked((UInt128)__hu)",
                "Int128" => $"HostLanding.Integer(__hu, {scale})",
                _ => $"(long)HostLanding.Integer(__hu, {scale})",
            },
            HostTypeKind.Decimal => $"HostLanding.Decimal(__hu, {scale})",
            HostTypeKind.Dec when IsUnsigned16(pic) => null,
            HostTypeKind.Dec => $"HostLanding.Dec(__hu, {scale})",
            _ => null,
        };
        if (f.Type.Kind is HostTypeKind.Dec && IsUnsigned16(pic))
            return $"{{ if (HostLanding.TryDecode({f.Cell}.Value, {f.Profile}, {name}, ref __hbad, out Int128 __hu) "
                + $"&& HostLanding.TryDec(unchecked((UInt128)__hu), {scale}, {name}, ref __hbad, out CobolDec __hd)) {target} = __hd; }}";
        if (convert is not null)
            return $"{{ if (HostLanding.TryDecode({f.Cell}.Value, {f.Profile}, {name}, ref __hbad, out Int128 __hu)) {target} = {convert}; }}";
        return $"{target} = {f.Cell}.Value;";
    }

    private static string Bool(bool b) => b ? "true" : "false";

    private static InvalidOperationException Unsupported(Item f) =>
        new($"HostEntryEmitter: the host entry has no landing for '{f.Description.CobolName}' ({f.Type.Kind})");
}
