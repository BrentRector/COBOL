// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
/// grouping (one storage for one variable given to two formals of one description, <see cref="HostLanding.RequireDistinct"/>
/// for two descriptions); one <c>CobolArg</c> per formal over that storage, stating the
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
            && Published(u));

    /// <summary>Whether every GROUP formal and RETURNING item of the unit has a published record
    /// (<see cref="PublishedRecordEmitter.Renders"/>). A unit with one that has none publishes no entry at all (the §14.2.3
    /// 13) restriction DOC-A.1-141 states): a group holding both a variable-length component and a reference, which no
    /// crossing form of the boundary carries.</summary>
    private static bool Published(BoundUnit u) =>
        u.Data.LinkageFormals.Select(f => (DataItem?)f.Item).Append(u.Data.LinkageReturning)
            .All(d => d is not { IsGroup: true } || PublishedRecordEmitter.Renders(d));

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

    /// <summary>One formal (or the RETURNING item) as the entry handles it; <paramref name="CsType"/> is its host type's
    /// C# spelling — for a group, its published record struct.</summary>
    private sealed record Item(DataItem Description, HostType Type, string CsType, string Name, string Cell, string? Profile,
        bool ByValue, bool Optional);

    private void EmitEntry(BoundUnit unit, string className, CodeWriter w)
    {
        // ONE scope for the records and the Call parameters, claimed in source order: the Call body names each record
        // type by its simple name, so a parameter spelled like one would shadow it (§4.3 "Names"; review w1093b R1-1).
        var members = CsNameScope.HostEntry(className);
        int serial = 0;
        var profiles = new List<(string Field, string Init)>();
        var memberProfiles = new Dictionary<DataItem, string>(ReferenceEqualityComparer.Instance);
        // A numeric member of a published record: its description's profile, one field per member however often the
        // record's landing and decode name it.
        string MemberProfile(DataItem leaf)
        {
            if (memberProfiles.TryGetValue(leaf, out var field)) return field;
            field = $"__hm{memberProfiles.Count}";
            memberProfiles.Add(leaf, field);
            profiles.Add((field, leaf.Pic!.ProfileInitializer(signEncoding)));
            return field;
        }
        var records = new List<(DataItem Group, string Name)>();
        Item Describe(DataItem description, string name, bool byValue, bool optional)
        {
            int n = serial++;
            HostType type = HostSurface.TypeOf(description);
            string? profile = null;
            if (CallEmitter.BoundaryProfile(description, signEncoding) is { } init)
            {
                profile = $"__hp{n}";
                profiles.Add((profile, init));
            }
            string csType = type.CsType ?? Record(description);
            return new Item(description, type, csType, name, $"__hc{n}", profile, byValue, optional);
        }
        // A group formal's published record struct, named `<word>_Record` in the class's member scope (§4.3 "Names").
        string Record(DataItem group)
        {
            string name = CsNames.Allocate($"{DataItem.Sanitize(group.CobolName ?? "FILLER")}_Record", members);
            records.Add((group, name));
            return name;
        }

        var formals = unit.Data.LinkageFormals
            .Select(f => Describe(f.Item, CsNames.Allocate(DataItem.Sanitize(f.Item.CobolName ?? "FILLER"), members),
                f.ByValue, f.Optional))
            .ToList();
        Item? returning = unit.Data.LinkageReturning is { } r ? Describe(r, "__hr", false, false) : null;
        // The records render first, so every member profile they name is declared with the formals' own.
        var recordText = new CodeWriter();
        var published = new PublishedRecordEmitter(MemberProfile);
        foreach (var (group, name) in records)
        {
            recordText.Line();
            published.Emit(group, name, recordText);
        }

        string kind = unit.IsFunction ? "FUNCTION-ID" : "PROGRAM-ID";
        w.Line($"/// <summary>{kind} {unit.Name} (ISO §14.2.3 13), DESIGN-external-repository §4.3): activates the "
            + $"{(unit.IsFunction ? "function" : "program")} through the run unit's activation boundary (§14.9.4.4 3)) as "
            + "a CALL statement naming it does, landing each argument exactly or refusing it.</summary>");
        using (w.Block($"public static class {className}"))
        {
            foreach (var (field, init) in profiles)
                w.Line($"private static readonly NumProfile {field} = {init};");
            if (records.Count > 0)
                foreach (string line in recordText.ToString().TrimEnd('\n').Split('\n'))
                    w.Line(line);
            string returnType = returning is null ? "void" : returning.CsType;
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
        $"{(f.ByValue ? "" : "ref ")}{f.CsType}{(f.Optional && !f.CsType.EndsWith('?') ? "?" : "")} {f.Name}";

    /// <summary>Whether the host type is a value type, so an OPTIONAL formal's parameter is a <see cref="Nullable{T}"/>.</summary>
    private static bool IsValueType(HostType t) =>
        t.Kind is HostTypeKind.Group or HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec or HostTypeKind.Float or HostTypeKind.Index
        || t is { Kind: HostTypeKind.Pointer, CsType: "ProgramPointer" or "FunctionPointer" };

    private void EmitCallBody(BoundUnit unit, List<Item> formals, Item? returning, CodeWriter w)
    {
        if (!unit.IsPrototype)
            w.Line($"{ModuleNamespace.RegistrarClass}.EnsureRegistered();   // this assembly's programs, once per run unit (DESIGN-external-repository §11.2)");
        // 1. Landing, before anything is activated (§4.3 "One storage for one variable passed twice"). Only two ref
        //    parameters of one CLR type can refer to one variable. One description: one storage, so the earlier
        //    formal's cell is the later one's when the variable is one (the landing's AreSame arm). Two descriptions:
        //    one storage cannot be both, so the variable is refused by its own statement, and every such statement
        //    precedes the FIRST landing, so the shape rule is decided before any value is: a value one formal would
        //    refuse never hides that the call is malformed. The two storages' cells are of different types whenever
        //    the descriptions land differently (an index and a PIC 99 item are both `ref long`; NIST IC207A), so the
        //    refusal is never an arm of the landing.
        var aliases = new string[formals.Count];
        for (int i = 0; i < formals.Count; i++)
        {
            var f = formals[i];
            aliases[i] = "";
            if (f.ByValue) continue;
            for (int j = i - 1; j >= 0; j--)
            {
                var e = formals[j];
                if (e.ByValue || e.CsType != f.CsType || (e.Optional != f.Optional && IsValueType(e.Type))) continue;
                string sameVariable = $"global::System.Runtime.CompilerServices.Unsafe.AreSame(ref {e.Name}, ref {f.Name})";
                // The arm shares only a storage that exists, so one null variable is landed as two would be (§4.3):
                // an OPTIONAL later formal is omitted before the arm is asked (below), and when only the earlier
                // formal is OPTIONAL it was omitted, so the later one lands the null by its own row (a text refuses
                // it, a data pointer's is NULL).
                if (OoConformance.SignatureDifference(e.Description, f.Description, "formal", "other formal") is null
                    && OoConformance.SignatureDifference(f.Description, e.Description, "formal", "other formal") is null)
                    aliases[i] += $"{sameVariable}{(e.Optional && !f.Optional ? $" && {e.Cell} is not null" : "")} ? {e.Cell} : ";
                else
                    w.Line($"HostLanding.RequireDistinct({sameVariable}, {CsLiteral(e.Name)}, {CsLiteral(f.Name)});");
            }
        }
        //    Then each host value into a detached storage of its formal's description, exactly or refused (HostLanding).
        for (int i = 0; i < formals.Count; i++)
        {
            var f = formals[i];
            string landed = aliases[i] + Storage(f);
            if (f.Optional) landed = $"{f.Name} is null ? null : {landed}";
            w.Line($"var {f.Cell} = {landed};");
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
            w.Line($"{returning.CsType} __hres = default;");
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
        // A group: its published record's image, every member landed exactly (PublishedRecordEmitter), crossing as the
        // CALL lane's TEXT crossing of a group of its description.
        if (f.Type.Kind is HostTypeKind.Group)
            return PublishedRecordEmitter.HoldsComponents(d)
                // A variable-length record: the CALL lane's variable-length carrier of its description (ISO §8.5.1.12).
                ? RuntimeApi.VarGroupCell($"{value}.__Var({CsLiteral(f.Name)})")
                : PublishedRecordEmitter.HoldsSlots(d)
                // A record holding references: the CALL lane's cell area (kb/Work PB2087), its image and its slots.
                ? $"{value}.__Area({CsLiteral(f.Name)})"
                : $"ManagedPointer<string>.Cell({GroupCrossing(d, $"{value}.__Land({CsLiteral(f.Name)})")})";
        PicInfo pic = d.Pic ?? throw Unsupported(f);
        string member = "null";
        return f.Type.Kind switch
        {
            HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec =>
                $"ManagedPointer<string>.Cell({RuntimeApi.NumFormatImage(Unsigned16(pic, $"HostLanding.Land({value}, {f.Profile}, {CsLiteral(f.Name)}, {member})"), f.Profile!)})",
            // A data pointer's NULL is the runtime's Null singleton (the value the predefined address NULL denotes),
            // never a .NET null reference, which the activated program's pointer reads would dereference.
            HostTypeKind.Pointer when pic.Category is PicCategory.Pointer =>
                $"ManagedPointer<{pic.ClrType}>.Cell({value} ?? ManagedPointer.Null)",
            HostTypeKind.Float or HostTypeKind.Index or HostTypeKind.Pointer or HostTypeKind.ObjectReference =>
                $"ManagedPointer<{pic.ClrType}>.Cell({value})",
            HostTypeKind.Text when d.IsAnyLength =>
                $"ManagedPointer<string>.Cell(HostLanding.LandVariable({value}, {RuntimeApi.UnstatedBoundaryLength},{Bool(pic.Category is PicCategory.Boolean)}, {CsLiteral(f.Name)}, {member}))",
            HostTypeKind.Text when d.IsDynamicLength =>
                $"ManagedPointer<string>.Cell(HostLanding.LandVariable({value}, {d.DynMaxSize}, {Bool(pic.Category is PicCategory.Boolean)}, {CsLiteral(f.Name)}, {member}))",
            HostTypeKind.Text =>
                $"ManagedPointer<string>.Cell(HostLanding.LandText({value}, {CallEmitter.BoundaryImageWidth(d)}, {Bool(d.Justified)}, {Bool(pic.Category is PicCategory.Boolean)}, {CsLiteral(f.Name)}, {member}))",
            _ => throw Unsupported(f),
        };
    }

    /// <summary>A group's record image (<paramref name="image"/>, one character per byte) as the CALL lane's TEXT crossing
    /// of a group of <paramref name="group"/>'s description carries it (<c>CallEmitter.CallStringRead</c>): the image
    /// itself, except that a bit or national group crosses as the elementary item it is treated as (ISO §14.8.2.1's NOTE,
    /// kb/Work PB1166) — its boolean or national character positions.</summary>
    private static string GroupCrossing(DataItem group, string image) =>
        !group.IsAsIfElementary ? image
        : group.GroupUsage is GroupUsage.National ? RuntimeApi.NatReadWindow(image, "0", $"{group.AsIfPic!.Length}")
        : RuntimeApi.BitsReadWindow(image, "0", $"{group.AsIfPic!.Length}");

    /// <summary>The inverse of <see cref="GroupCrossing"/>: the record image a group's crossing text carries.</summary>
    private static string GroupImage(DataItem group, string crossing) =>
        !group.IsAsIfElementary ? crossing
        : group.GroupUsage is GroupUsage.National ? RuntimeApi.NatBytes(crossing)
        : RuntimeApi.BitsPack(crossing, $"{group.AsIfPic!.Length}");

    /// <summary>An UNSIGNED 16-byte binary item's landed value is its container's BITS (<c>HostLanding.Land</c>), which the
    /// image is formatted from as the unsigned value it is (<see cref="CobolNum.FormatImage(UInt128, in NumProfile)"/>).</summary>
    internal static string Unsigned16(PicInfo pic, string landed) =>
        IsUnsigned16(pic) ? $"unchecked((UInt128){landed})" : landed;

    private static bool IsUnsigned16(PicInfo pic) =>
        pic is { Truncation: NumericTruncation.BinaryCapacity, Signed: false, StorageWidth: 16 };

    /// <summary>The RETURNING item's storage, allocated by the entry as an activating COBOL element allocates it (ISO
    /// §14.2.3 6) NOTE 1): the initial content of its description's storage form — for a group, the image of a record
    /// the host would construct (text members spaces, numbers zero).</summary>
    private static string ReceiverSeed(Item r)
    {
        DataItem d = r.Description;
        if (r.Type.Kind is HostTypeKind.Group)
            return PublishedRecordEmitter.HoldsComponents(d)
                ? RuntimeApi.VarGroupCell($"new {r.CsType}().__Var({CsLiteral(r.CsType)})")
                : PublishedRecordEmitter.HoldsSlots(d)
                ? $"new {r.CsType}().__Area({CsLiteral(r.CsType)})"
                : $"ManagedPointer<string>.Cell({GroupCrossing(d, $"new {r.CsType}().__Land({CsLiteral(r.CsType)})")})";
        PicInfo pic = d.Pic ?? throw Unsupported(r);
        return r.Type.Kind switch
        {
            HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec =>
                $"ManagedPointer<string>.Cell({RuntimeApi.NumFormatImage("(Int128)0", r.Profile!)})",
            HostTypeKind.Pointer when pic.Category is PicCategory.Pointer => $"ManagedPointer<{pic.ClrType}>.Cell(ManagedPointer.Null)",
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
    /// compares it with the activated program's formal when that program enables the check. The length and the description
    /// are stated by the CALL lane's own renderer, <see cref="CallEmitter.Described(string, int, string?)"/>
    /// (DESIGN-external-repository §17.3 "One renderer").</summary>
    private string ArgOver(Item f, CobolPassMode mode)
    {
        // A record holding references crosses as a COBOL argument of cell-backed storage does (CallEmitter.AreaOf): its
        // characters, and the cell area the activated program's formal is laid over (ISO §14.2.3 8)).
        string carrier = f.Cell, area = "";
        if (f.Type.Kind is HostTypeKind.Group && PublishedRecordEmitter.HoldsSlots(f.Description))
        {
            carrier = $"ManagedPointer<string>.OverField(() => {f.Cell}.Ref, __v => {{ {f.Cell}.Ref = __v; }})";
            area = $", Area: {RuntimeApi.ArgArea(f.Cell, "0")}";
        }
        string built = $"new CobolArg({RuntimeApi.PassModeText(mode)}, {carrier}, {f.Profile ?? "null"}{area})";
        string? description = ActivationDescriptions.Of(f.Description, classes) is { } d ? RuntimeApi.ActivationDescriptionNew(d) : null;
        return CallEmitter.Described(built, StorageLength(f), description);
    }

    /// <summary>The fixed storage length the argument states: the CALL lane's own rule
    /// (<see cref="CallEmitter.ArgumentLength(DataItem, CallCrossing)"/>) for the detached storage's crossing.</summary>
    private static int StorageLength(Item f) => CallEmitter.ArgumentLength(f.Description, Crossing(f));

    /// <summary>The crossing of the detached storage (see the class remarks): a fixed-point or character formal its
    /// (record) image, a variable-length group its variable-length carrier, a floating-point or index formal its native
    /// cell, a pointer or object reference its managed slot.</summary>
    private static CallCrossing Crossing(Item f) => f.Type.Kind switch
    {
        HostTypeKind.Group when PublishedRecordEmitter.HoldsComponents(f.Description) => CallCrossing.VarGroup,
        HostTypeKind.Float or HostTypeKind.Index => CallCrossing.Native,
        HostTypeKind.Pointer or HostTypeKind.ObjectReference => CallCrossing.Managed,
        _ => CallCrossing.Text,
    };

    /// <summary>⛔ THE ONE VALIDITY RULE (DESIGN-external-repository §4.3 "Write-back and RETURNING"): the storage's content
    /// decoded into <paramref name="target"/> by its description — a numeric content only when the class test NUMERIC is
    /// true of it (<see cref="HostLanding.TryDecode"/>), keeping the first refusal in <c>__hbad</c> and the target
    /// unchanged. Decoding never narrows: the host type holds every value the storage can (<see cref="HostSurface"/>).</summary>
    private static string Decode(Item f, string target)
    {
        string word = f.Name == "__hr" ? f.Description.CobolName ?? "RETURNING" : f.Name;
        if (f.Type.Kind is HostTypeKind.Group)
            return $"{{ if ({f.CsType}.__TryGet("
                + (PublishedRecordEmitter.HoldsComponents(f.Description) ? $"{f.Cell}.Value"
                    : PublishedRecordEmitter.HoldsSlots(f.Description)
                    ? $"{f.Cell}.Ref, {f.Cell}"
                    : $"{GroupImage(f.Description, $"{f.Cell}.Value")}, null")
                + (PublishedRecordEmitter.HoldsComponents(f.Description) ? "" : ", 0")
                + $", {CsLiteral(word + ".")}, ref __hbad, out var __hg)) {target} = __hg; }}";
        PicInfo pic = f.Description.Pic ?? throw Unsupported(f);
        return f.Type.Kind is HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec
            ? NumericDecode(f.Type, pic, $"{f.Cell}.Value", f.Profile!, CsLiteral(word), target, "__hbad", "")
            : $"{target} = {f.Cell}.Value;";
    }

    /// <summary>⛔ THE ONE NUMERIC DECODE of a fixed-point item's record image <paramref name="image"/> into
    /// <paramref name="target"/> of its host type — an elementary formal's storage and a published record's member alike:
    /// <see cref="HostLanding.TryDecode"/>'s validity rule (the first refusal kept in <paramref name="failure"/>, the
    /// target unchanged and <paramref name="onFail"/> run), then the host type's exact conversion.</summary>
    internal static string NumericDecode(HostType type, PicInfo pic, string image, string profile, string name, string target,
        string failure, string onFail)
    {
        string scale = $"{pic.Scale}";
        string tryDecode = $"HostLanding.TryDecode({image}, {profile}, {name}, ref {failure}, out Int128 __hu)";
        if (type.Kind is HostTypeKind.Dec && IsUnsigned16(pic))
            return $"{{ if ({tryDecode} && HostLanding.TryDec(unchecked((UInt128)__hu), {scale}, {name}, ref {failure}, "
                + $"out CobolDec __hd)) {target} = __hd;{onFail} }}";
        string convert = type.Kind switch
        {
            HostTypeKind.Integer => type.CsType switch
            {
                "UInt128" => "unchecked((UInt128)__hu)",
                "Int128" => $"HostLanding.Integer(__hu, {scale})",
                _ => $"(long)HostLanding.Integer(__hu, {scale})",
            },
            HostTypeKind.Decimal => $"HostLanding.Decimal(__hu, {scale})",
            HostTypeKind.Dec => $"HostLanding.Dec(__hu, {scale})",
            _ => throw new InvalidOperationException($"HostEntryEmitter.NumericDecode: {type.Kind} is not a fixed-point row"),
        };
        return $"{{ if ({tryDecode}) {target} = {convert};{onFail} }}";
    }

    private static string Bool(bool b) => b ? "true" : "false";

    private static InvalidOperationException Unsupported(Item f) =>
        new($"HostEntryEmitter: the host entry has no landing for '{f.Description.CobolName}' ({f.Type.Kind})");
}
