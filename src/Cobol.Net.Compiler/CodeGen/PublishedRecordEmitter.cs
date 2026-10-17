// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using static CobolNet.CodeGen.Emit.EmitText;

namespace CobolNet.CodeGen;

/// <summary>
/// ⛔ THE PUBLISHED RECORD STRUCT (DESIGN-external-repository §4.3, the host-type table's group rows; kb/Work PB2099): the
/// .NET type a host entry presents for a GROUP formal or RETURNING item, rendered from the formal's DESCRIPTION. One
/// property per named subordinate in its host type (<see cref="HostSurface.TypeOf"/>), a nested record struct per
/// subordinate group, an array for OCCURS; a REDEFINES subordinate is not a member (the record is the first description;
/// its other views are the image's), and FILLER is no member either.
/// <para>⛔ <b>THE GEOMETRY IS THE STORAGE LAYOUT'S, NEVER RE-DERIVED.</b> Every member's place in the record image is
/// <see cref="BitLayout.ChildStarts"/>, the one §8.5.1.6.3 placement walk — in BITS, so a USAGE BIT member that shares a
/// byte is placed exactly as the program places it, and a REDEFINES view starts at its target — which is also the walk
/// the binder's class offsets (a group FORMAL is an area, read through windows) and the record-struct image codec (a
/// RETURNING group is a record struct) are laid out by; an occurrence's stride is <see cref="BitLayout.StrideBits"/>, the
/// one subscript stride. A group formal has no internal record struct whose member walk the published struct could
/// share, so it is rendered here, from the description, over that geometry (DESIGN-external-repository §4.3).</para>
/// <para>⛔ <b>ITS DOOR RULES ARE THE ENTRY'S.</b> Composing the image lands every member through <c>HostLanding</c> exactly
/// as an elementary formal is landed (loss refused with the member named, null refused, an array of the wrong length
/// refused); decoding runs the one validity rule (<c>HostLanding.TryDecode</c>, the class test NUMERIC) on every numeric
/// member, and a record any member of which fails it is not decoded at all. Positions no member covers — FILLER, the tail
/// of a REDEFINES view longer than the first description — keep the content <c>FromImage</c> (or the entry's write-back)
/// read, so an image round-trips; a record the host constructs holds spaces there (the content of a group MOVE SPACES).</para>
/// </summary>
/// <param name="profileOf">The entry class's static <c>NumProfile</c> field of a numeric member (the entry owns the
/// fields, so the elementary formals and the record members share one declaration list).</param>
internal sealed class PublishedRecordEmitter(Func<DataItem, string> profileOf)
{
    /// <summary>Whether a host entry publishes a record for <paramref name="group"/>. A fixed group always: an OCCURS
    /// DEPENDING ON table is held (its storage is the maximum allocation, ISO §8.5.1.8, the image the CALL lane's text
    /// crossing carries), and so is a pointer or object reference (its managed slot, <see cref="HoldsSlots"/>). A
    /// VARIABLE-LENGTH group (<see cref="HoldsComponents"/>) whenever the CALL lane carries it — its current extent
    /// composes (<see cref="DataItem.CurrentExtentImageCapable"/>, the gate of every §8.5.1.12 carrier) — as the CALL
    /// lane's variable-length carrier (<c>CobolVarGroup</c>, <see cref="EmitVar"/>). One shape is the GR13 restriction
    /// DESIGN-external-repository §4.3 states (ISO §14.2.3 13): a group holding BOTH a variable-length component and a
    /// pointer or object-reference member. ⛔ <b>THE RESTRICTION IS THE BOUNDARY'S, NOT THIS EMITTER'S</b> (kb/Work PB3273):
    /// no crossing form carries that group — the variable-length carrier has no slot for a reference, the cell area has no
    /// component, and the activated program's own formal of that description stages the Tier-C loud at its copy-in
    /// (<c>TierCIsland</c>; a COBOL CALL of it is COBOLNET1688) — so an entry would only reach a loud. The
    /// <see cref="HoldsSlots"/> conjunct is therefore implied today by <see cref="DataItem.CurrentExtentImageCapable"/>
    /// (a reference leaf is no member of its law), and it STAYS as the guard: when the boundary gains a form for the
    /// shape, this emitter must compose that form's storage (the cell area's slots beside the carrier's components) before
    /// the restriction lifts, or a record would silently drop its references. <c>HostEntryShapeDriftTests</c> pins both
    /// halves.</summary>
    internal static bool Renders(DataItem group) =>
        group.IsGroup && DataItem.DescendantsOf(group).Where(Occupies).All(d => !d.IsAnyLength)
        && (!HoldsComponents(group) || (group.CurrentExtentImageCapable && !HoldsSlots(group)));

    /// <summary>Whether <paramref name="item"/> is or holds a §8.5.1.12 VARIABLE-LENGTH COMPONENT — a dynamic-length
    /// elementary item or a dynamic-capacity table (<see cref="CellComponents"/>, the data model's one enumeration of
    /// them) — so a group of it crosses as the CALL lane's variable-length carrier, never as its image alone.</summary>
    internal static bool HoldsComponents(DataItem item) => CellComponents.PerOccurrence(item) > 0;

    /// <summary>The components of one occurrence of the record of <paramref name="group"/>, in ordinal order: the
    /// carrier's <c>Dynamic</c> list of a SCOPE (the published record, or one element of a dynamic-capacity table,
    /// whose element is a scope of its own), placed by the description's layout (<see cref="CellComponents.InLayout"/>:
    /// a RETURNING item has no storage class whose offsets <see cref="CellComponents.Of"/> could read).</summary>
    private static List<(DataItem Item, int At)> ComponentsOf(DataItem group) =>
        CellComponents.InLayout(group).Select(p => (p.Item, p.At)).ToList();

    /// <summary>The ordinal of <paramref name="member"/>'s first component within its group <paramref name="group"/>'s
    /// components: those of the storage children before it, each times its occurrences — the order
    /// <see cref="CellComponents.Of"/> numbers them in.</summary>
    private static int OrdinalWithin(DataItem group, DataItem member) =>
        group.Children.TakeWhile(c => !ReferenceEquals(c, member)).Where(c => c.RedefinesTargetName is null)
            .Sum(c => CellComponents.PerOccurrence(c) * (c.Occurs ?? 1));

    /// <summary>The minimum capacity of a dynamic-capacity table, integer-4 of its FROM phrase ("If integer-4 is absent,
    /// a value of zero is assumed for it", ISO §13.18.38.4 16)).</summary>
    private static int MinimumCapacity(DataItem table) => table.OccursSpec?.InitialCap ?? 0;

    /// <summary>Whether <paramref name="item"/> is or holds a member whose value rides a managed slot of its area rather
    /// than its bytes — a pointer or object reference (<see cref="SlotWindow.CarriedBySlot"/>, the data model's one
    /// test) — so a group of it crosses as the CALL lane's cell area (<c>__Area</c>), never as its image alone.</summary>
    internal static bool HoldsSlots(DataItem item) =>
        SlotWindow.CarriedBySlot(item) || DataItem.DescendantsOf(item).Any(SlotWindow.CarriedBySlot);

    /// <summary>The DEPENDING ON item of an OCCURS DEPENDING ON member <paramref name="table"/> whose count the record
    /// <paramref name="root"/> itself holds — data-name-1 inside the record, which §13.18.38.3 20) places before the
    /// table — or null: a fixed table, or one whose data-name-1 is outside the record, whose property then holds the
    /// table's whole area, every occurrence the storage has (§8.5.1.8; the image the boundary carries).</summary>
    private static DataItem? DependingInRecord(DataItem root, DataItem table) =>
        table.OccursSpec is { DependingName: not null, Depending: { } dep } && RelativeBits(root, dep) >= 0 ? dep : null;

    /// <summary>The bit offset of <paramref name="item"/> from the start of its ancestor <paramref name="ancestor"/>
    /// (0 for the ancestor itself), summed along the parent chain over the one placement walk
    /// (<see cref="BitLayout.StartBitWithin"/>); -1 when <paramref name="item"/> is not under it. Asked only of a
    /// DEPENDING ON item and of a group holding an OCCURS DEPENDING ON table, neither of which lies in a table
    /// (§13.18.38.3 2) and 1) b)), so each is one place.</summary>
    private static int RelativeBits(DataItem ancestor, DataItem item)
    {
        int bits = 0;
        for (DataItem? d = item; d is not null; d = d.Parent)
        {
            if (ReferenceEquals(d, ancestor)) return bits;
            int at = d.Parent is null ? -1 : BitLayout.StartBitWithin(d.Parent, d);
            if (at < 0) return -1;
            bits += at;
        }
        return -1;
    }

    /// <summary>An entry that occupies storage of the record: a group or elementary item, never a condition-name or a
    /// RENAMES entry.</summary>
    private static bool Occupies(DataItem d) => d.Level is not (66 or 88) && (d.IsGroup || d.IsElementary);

    /// <summary>The record's members: its named, non-redefining subordinates, in source order.</summary>
    private static IEnumerable<DataItem> MembersOf(DataItem group) =>
        group.Children.Where(c => Occupies(c) && c.CobolName is not null && c.RedefinesTargetName is null);

    /// <summary>The width of the record's image in characters, one per byte of its storage.</summary>
    internal static int Width(DataItem group) => group.ByteWidth;

    /// <summary>One member as the struct presents it. <paramref name="Depending"/> is the DEPENDING ON item of an OCCURS
    /// DEPENDING ON table whose count the record holds (<see cref="DependingInRecord"/>): the array then has that many
    /// entries; every other fixed table's array has its <see cref="DataItem.Occurs"/> (the maximum, for a DEPENDING ON
    /// item outside the record), and a dynamic-capacity table's array has its current capacity.
    /// <paramref name="Ordinal"/> is its first component's ordinal within its record's components
    /// (<see cref="OrdinalWithin"/>).</summary>
    private sealed record Member(DataItem Item, string Name, HostType Type, string? Record, DataItem? Depending, int Ordinal)
    {
        public int Occurs => Item.Occurs ?? 0;
        public int Minimum => Depending is null ? Occurs : Item.OccursSpec!.Min;
        public string Element => Record ?? Type.CsType!;
        public bool IsTable => Occurs > 0 || Item.IsDynamicTable;
        public string PropertyType => IsTable ? $"{Element}[]" : Element;
        /// <summary>A dynamic-length item or a dynamic-capacity table: a component of the carrier, with no image.</summary>
        public bool IsComponent => CellComponents.IsComponent(Item);
        /// <summary>Is or holds a component (<see cref="HoldsComponents"/>).</summary>
        public bool Holds => HoldsComponents(Item);
    }

    /// <summary>Emit the published record struct of <paramref name="group"/>, named <paramref name="name"/>, as a nested
    /// type at the writer's position.</summary>
    internal void Emit(DataItem group, string name, CodeWriter w) => Emit(group, group, name, w);

    /// <summary>Emit the record struct of <paramref name="group"/>, a subordinate (or the whole) of the published record
    /// <paramref name="root"/>, whose DEPENDING ON items it places.</summary>
    private void Emit(DataItem root, DataItem group, string name, CodeWriter w)
    {
        var scope = CsNameScope.RecordStruct();
        scope.Reserve([name]);   // a member never spells its enclosing type (CS0542)
        var members = new List<Member>();
        foreach (var m in MembersOf(group))
        {
            string word = DataItem.Sanitize(m.CobolName!);
            string prop = CsNames.Allocate(word, scope);
            HostType type = HostSurface.TypeOf(m);
            members.Add(new Member(m, prop, type, type.Kind is HostTypeKind.Group ? CsNames.Allocate($"{word}_Record", scope) : null,
                DependingInRecord(root, m), OrdinalWithin(group, m)));
        }
        // A record whose storage holds variable-length components (ISO §8.5.1.12): they have no image, so the record
        // presents them as members and composes the CALL lane's carrier of them (EmitVar).
        var components = ComponentsOf(group);
        bool variable = components.Count > 0;
        // A DEPENDING ON item this struct presents, with the fewest occurrences of the table it counts: the constructor
        // gives it that count, so `new R()` is a whole record whose table has its minimum number of entries.
        var counts = new Dictionary<DataItem, int>(ReferenceEqualityComparer.Instance);
        foreach (var t in DataItem.DescendantsOf(root))
            if (DependingInRecord(root, t) is { } dep)
                counts[dep] = Math.Max(counts.GetValueOrDefault(dep), t.OccursSpec!.Min);
        int width = Width(group);
        w.Line($"/// <summary>{group.Level:00} {group.CobolName ?? "FILLER"} — the published record of the description (ISO "
            + "§14.2.3 13), DESIGN-external-repository §4.3): one property per named subordinate, landed exactly into the "
            + "record image and decoded from it by the class test NUMERIC.</summary>");
        using (w.Block($"public record struct {name}"))
        {
            w.Line("private string? __image;   // the image FromImage read: the positions no member covers (FILLER, a REDEFINES view's tail)");
            bool scopeRoot = variable && (ReferenceEquals(group, root) || group.IsDynamicTable);
            if (scopeRoot)
                w.Line("private CobolVarGroup? __var;   // the carrier the write-back read: the components no member presents (FILLER)");
            w.Line();
            w.Line($"/// <summary>A record whose text members are empty (spaces in the image), whose numbers are zero and "
                + "whose tables have their declared occurrences.</summary>");
            using (w.Block($"public {name}()"))
                foreach (var m in members) EmitInitial(m, counts, w);
            foreach (var m in members)
            {
                string occurs = m.Item.IsDynamicTable ? " OCCURS DYNAMIC" : m.Occurs > 0 ? $" OCCURS {m.Occurs}" : "";
                w.Line($"/// <summary>{m.Item.Level:00} {m.Item.CobolName}{occurs}.</summary>");
                w.Line($"public {m.PropertyType} {m.Name}" + " { get; set; }");   // a COBOL member, named by the allocator
            }
            w.Line();
            // A variable-length record has no storage image to publish: its dynamic-length items "may be located elsewhere"
            // (ISO §8.5.1.10.3) and its dynamic-capacity tables' items "are not required to be contiguous" (§8.5.1.9.1).
            if (!variable)
            {
                w.Line("/// <summary>The record's image: every member landed exactly, or refused with "
                    + "<see cref=\"global::System.ArgumentException\"/> naming it.</summary>");
                w.Line($"public readonly string AsImage() => __Land({CsLiteral(name)});");
                w.Line("/// <summary>The record an image holds, every numeric member decoded by the class test NUMERIC, or "
                    + "<see cref=\"global::System.FormatException\"/> naming the first that fails it.</summary>");
                using (w.Block($"public static {name} FromImage(string image)"))
                {
                    w.Line("global::System.FormatException? __bad = null;");
                    w.Line($"__TryGet(HostLanding.RequireImage(image, {width}, {CsLiteral(name)}), null, 0, \"\", ref __bad, out var __r);");
                    w.Line("return __bad is null ? __r : throw __bad;");
                }
            }
            using (w.Block("internal readonly string __Land(string __parameter, string __path = \"\")"))
            {
                w.Line($"var __b = (__image ?? {BlankImage(group, width)}).ToCharArray();");
                w.Line("__Put(__b, 0, __parameter, __path);");
                w.Line("return new string(__b);");
            }
            if (variable) EmitVar(group, name, members, components, scopeRoot, width, w);
            using (w.Block("internal readonly void __Put(char[] __b, int __at, string __parameter, string __path)"))
                foreach (var m in members) EmitPut(root, group, m, w);
            if (HoldsSlots(group))
            {
                // A record whose pointer or object-reference members ride managed slots (§4.3's last table row): its
                // storage is the CALL lane's cell area (kb/Work PB2087) — the image, and each reference in its slot.
                using (w.Block("internal readonly StorageCell __Area(string __parameter)"))
                {
                    w.Line("var __c = new StorageCell { Ref = __Land(__parameter) };");
                    w.Line("__PutSlots(__c, 0);");
                    w.Line("return __c;");
                }
                using (w.Block("internal readonly void __PutSlots(StorageCell __c, int __at)"))
                    foreach (var m in members.Where(m => HoldsSlots(m.Item))) EmitPutSlots(group, m, w);
            }
            using (w.Block($"internal static bool __TryGet(string __s, StorageCell? __c, int __at, string __name, ref global::System.FormatException? __bad, out {name} __r)"))
            {
                w.Line($"__r = new {name}();");
                if (!BitLayout.IsBitItem(group))
                    w.Line($"__r.__image = __s.Substring(__at / 8, {width});");
                w.Line("bool __ok = true;");
                foreach (var m in members) EmitGet(root, group, m, w);
                w.Line("return __ok;");
            }
            foreach (var m in members.Where(m => m.Record is not null))
            {
                w.Line();
                Emit(root, m.Item, m.Record!, w);
            }
        }
    }

    /// <summary>The image of a record the host constructed, before its members are landed: spaces, the content a group
    /// MOVE SPACES leaves — as national spaces in a national group, and zero bits in a bit group.</summary>
    private static string BlankImage(DataItem group, int width) => group.GroupUsage switch
    {
        GroupUsage.Bit => $"new string((char)0, {width})",
        GroupUsage.National => RuntimeApi.NatBytes($"new string(' ', {width / RuntimeApi.BytesPerNational})"),
        _ => $"new string(' ', {width})",
    };

    /// <summary>One member's initial value in the parameterless constructor; <paramref name="counts"/> gives each DEPENDING
    /// ON item the record holds the count its table starts with (its fewest occurrences), and that table as many
    /// entries.</summary>
    private static void EmitInitial(Member m, IReadOnlyDictionary<DataItem, int> counts, CodeWriter w)
    {
        bool text = m.Type.Kind is HostTypeKind.Text;
        if (m.IsTable)
        {
            // A dynamic-capacity table starts at its minimum capacity (ISO §14.6.2.3.2 6)).
            int n = m.Item.IsDynamicTable ? MinimumCapacity(m.Item) : m.Depending is { } dep ? counts[dep] : m.Occurs;
            w.Line($"{m.Name} = new {m.Element}[{n}];");
            if (text) w.Line($"global::System.Array.Fill({m.Name}, \"\");");
            if (m.Record is not null) w.Line($"for (int __i = 0; __i < {n}; __i++) {m.Name}[__i] = new();");
        }
        else if (counts.TryGetValue(m.Item, out int count) && count > 0 && m.Type.Kind is HostTypeKind.Integer)
            w.Line($"{m.Name} = ({m.Type.CsType}){count};");
        else if (text) w.Line($"{m.Name} = \"\";");
        else if (m.Record is not null) w.Line($"{m.Name} = new();");
    }

    /// <summary>The member's bit position in the image, relative to the record's own (<paramref name="group"/>'s) start
    /// at <c>__at</c>: the one placement walk (<see cref="BitLayout.ChildStarts"/>), and for an occurrence the one
    /// subscript stride.</summary>
    private static string Position(DataItem group, Member m, bool occurrence)
    {
        int rel = BitLayout.StartBitWithin(group, m.Item);
        if (rel < 0)   // only a REDEFINES chain can be unplaced, and a redefiner is never a member (MembersOf)
            throw new InvalidOperationException(
                $"PublishedRecordEmitter: the member '{m.Item.CobolName}' has no place in '{group.CobolName}'");
        return $"__at + {rel}{(occurrence ? $" + __i * {BitLayout.StrideBits(m.Item)}" : "")}";
    }

    /// <summary>The character image of a DEPENDING ON item the record holds, read from <paramref name="source"/> — the
    /// landing's buffer <c>__b</c> (<paramref name="buffer"/>) or the decode's image <c>__s</c> — at its place relative to
    /// the start of <paramref name="group"/>'s struct (<c>__at</c>).</summary>
    private static string DependingImage(DataItem root, DataItem group, DataItem dep, string source, bool buffer)
    {
        string bytes = $"(__at + {RelativeBits(root, dep) - RelativeBits(root, group)}) / 8";
        return Characters(dep, buffer ? $"new string({source}, {bytes}, {dep.ByteWidth})" : $"{source}.Substring({bytes}, {dep.ByteWidth})");
    }

    /// <summary>An elementary item's characters as its host decode reads them from <paramref name="storage"/>, the item's
    /// own storage characters: a national item's character positions (its UTF-16BE pairs), every other item's storage.</summary>
    private static string Characters(DataItem leaf, string storage) =>
        NationalWindow.PositionsOf(leaf) is { } positions ? RuntimeApi.NatReadWindow(storage, "0", $"{positions}") : storage;

    /// <summary>⛔ THE CARRIER OF A VARIABLE-LENGTH RECORD (ISO §8.5.1.12; DESIGN-external-repository §4.3): a group with
    /// dynamic-length items or dynamic-capacity tables crosses as the CALL lane's carrier of its description
    /// (<c>CobolVarGroup</c>, kb/Work PB204), never as an image, because those components have none: "all dynamic-length
    /// elementary items are considered to be of zero length" in its byte accounting (§8.5.1.12.3). The record composes
    /// the carrier in the description's own shape, so the activated program's formal takes it as a COBOL argument of the
    /// same description (<c>CobolArgAdapt.VarGroup</c>): its FIXED RUN is the landed image with each table's reservation
    /// cut out (<c>CobolVarGroup.RunOf</c>, the one fixed-run rule a cell-backed group's carrier is built by too), and
    /// its components, in the order <see cref="CellComponents"/> numbers them, are each dynamic-length item's landed
    /// content (<c>HostLanding.LandVariable</c>, up to its maximum size) and each dynamic-capacity table's occurrences —
    /// their images concatenated, or, for a table whose elements are themselves variable-length groups, the
    /// occurrences' own carriers (<c>CobolVarGroup.Elements</c>, kb/Work PB2496). A table's array is its current
    /// capacity (<c>HostLanding.RequireCapacity</c>). A SCOPE root — the published record, or one element of a
    /// dynamic-capacity table, whose element is a scope of its own — has <c>__Var</c> and the carrier's
    /// <c>__TryGet</c>; every record that holds components has <c>__PutDyn</c> and <c>__TryGetDyn</c> over its
    /// sub-range of its scope's components.</summary>
    private void EmitVar(DataItem group, string name, List<Member> members, List<(DataItem Item, int At)> components,
        bool scopeRoot, int width, CodeWriter w)
    {
        using (w.Block("internal readonly void __PutDyn(string[] __d, CobolVarGroup[]?[]? __e, int __dyn, string __parameter, string __path)"))
            foreach (var m in members.Where(m => m.Holds)) EmitPutDyn(m, w);
        using (w.Block($"internal static bool __TryGetDyn(CobolVarGroup __v, int __dyn, string __name, ref global::System.FormatException? __bad, ref {name} __r)"))
        {
            w.Line("bool __ok = true;");
            foreach (var m in members.Where(m => m.Holds)) EmitGetDyn(m, w);
            w.Line("return __ok;");
        }
        if (!scopeRoot) return;
        int n = components.Count;
        var at = components.Select(c => c.At).ToList();
        var tables = components.Select(c => c.Item.IsDynamicTable ? c.Item.ByteWidth : 0).ToList();
        bool nested = components.Any(c => c.Item.IsDynamicTable && ComponentsOf(c.Item).Count > 0);
        w.Line("/// <summary>The record as the CALL lane's variable-length carrier: every member landed exactly, or refused "
            + "with <see cref=\"global::System.ArgumentException\"/> naming it.</summary>");
        using (w.Block("internal readonly CobolVarGroup __Var(string __parameter, string __path = \"\")"))
        {
            // The components no member presents (FILLER) keep what the write-back read, as the image's positions do.
            w.Line("string[] __d;");
            w.Line($"if (__var?.Dynamic is {{ Length: {n} }} __wd) __d = (string[])__wd.Clone();");
            w.Line($"else {{ __d = new string[{n}]; global::System.Array.Fill(__d, \"\"); }}");
            w.Line($"var __e = __var?.Elements is {{ Length: {n} }} __we ? (CobolVarGroup[]?[])__we.Clone() : {(nested ? $"new CobolVarGroup[]?[{n}]" : "null")};");
            w.Line($"string __run = {RuntimeApi.VarGroupRunOf("__Land(__parameter, __path)", at, tables)};");
            w.Line("__PutDyn(__d, __e, 0, __parameter, __path);");
            w.Line("return new CobolVarGroup(__run, __d, __e);");
        }
        w.Line("/// <summary>The record a carrier holds, every numeric member decoded by the class test NUMERIC; false, with the "
            + "first refusal in __bad, when one fails it.</summary>");
        using (w.Block($"internal static bool __TryGet(CobolVarGroup __v, string __name, ref global::System.FormatException? __bad, out {name} __r)"))
        {
            w.Line($"bool __ok = __TryGet({RuntimeApi.VarGroupWindowOf(RuntimeApi.VarGroupFixed("__v"), BlankImage(group, width), at, tables)}, "
                + "null, 0, __name, ref __bad, out __r);");
            w.Line("__r.__var = __v;");
            w.Line("return __TryGetDyn(__v, 0, __name, ref __bad, ref __r) && __ok;");
        }
    }

    /// <summary>One member's components into the carrier's lists (<c>__d</c>, and <c>__e</c> for a table of
    /// variable-length elements) at its ordinals from <c>__dyn</c>.</summary>
    private void EmitPutDyn(Member m, CodeWriter w)
    {
        string path = $"__path + {CsLiteral(m.Name)}";
        string at = $"__dyn + {m.Ordinal}";
        string each = $"{path} + \"[\" + __i + \"]\"";
        if (m.Item is { IsElementary: true, IsDynamicLength: true })
        {
            w.Line($"__d[{at}] = HostLanding.LandVariable({m.Name}, {m.Item.DynMaxSize}, "
                + $"{(m.Item.Pic!.Category is PicCategory.Boolean ? "true" : "false")}, __parameter, {path});");
            return;
        }
        if (m.Item.IsDynamicTable)
        {
            using (w.Block(""))
            {
                w.Line($"var __a = HostLanding.RequireCapacity({m.Name}, {MinimumCapacity(m.Item)}, __parameter, {path});");
                if (m.Record is not null && ComponentsOf(m.Item).Count > 0)
                {
                    w.Line("var __o = new CobolVarGroup[__a.Length];");
                    w.Line($"for (int __i = 0; __i < __a.Length; __i++) __o[__i] = __a[__i].__Var(__parameter, {each} + \".\");");
                    w.Line($"__e![{at}] = __o;");
                    w.Line($"__d[{at}] = \"\";");
                    return;
                }
                int elementWidth = m.Item.ByteWidth;
                w.Line($"var __s = new global::System.Text.StringBuilder(__a.Length * {elementWidth});");
                w.Line(m.Record is not null
                    ? $"for (int __i = 0; __i < __a.Length; __i++) __s.Append(__a[__i].__Land(__parameter, {each} + \".\"));"
                    : $"for (int __i = 0; __i < __a.Length; __i++) {{ var __b = new char[{elementWidth}]; "
                        + $"{PutLeaf(m.Item, m.Type, "__a[__i]", "0", each)} __s.Append(__b); }}");
                w.Line($"__d[{at}] = __s.ToString();");
            }
            return;
        }
        // A group, or a fixed table of groups, that holds components: each record's own, at its ordinals (one
        // occurrence's components times the occurrence number past the first).
        w.Line(m.Occurs == 0
            ? $"{m.Name}.__PutDyn(__d, __e, {at}, __parameter, {path} + \".\");"
            : $"for (int __i = 0; __i < {m.Name}.Length; __i++) {m.Name}[__i].__PutDyn(__d, __e, {at} + __i * "
                + $"{CellComponents.PerOccurrence(m.Item)}, __parameter, {each} + \".\");");
    }

    /// <summary>One member's components decoded from the carrier <c>__v</c> at its ordinals from <c>__dyn</c>: a
    /// dynamic-length item its content, a dynamic-capacity table one entry per occurrence it carries (§14.6.9.2 recreates
    /// a receiving table at the sender's capacity), each decoded as a member of the element's description.</summary>
    private void EmitGetDyn(Member m, CodeWriter w)
    {
        string name = $"__name + {CsLiteral(m.Name)}";
        string at = $"__dyn + {m.Ordinal}";
        string each = $"{name} + \"[\" + __i + \"]\"";
        if (m.Item is { IsElementary: true, IsDynamicLength: true })
        {
            w.Line($"__r.{m.Name} = {RuntimeApi.VarGroupDyn("__v", at)};");
            return;
        }
        if (m.Item.IsDynamicTable)
        {
            using (w.Block(""))
            {
                bool nested = m.Record is not null && ComponentsOf(m.Item).Count > 0;
                w.Line(nested
                    ? $"var __o = {RuntimeApi.VarGroupElementCarriersAt("__v", at)};"
                    : $"var __o = {RuntimeApi.VarGroupOccurrences(RuntimeApi.VarGroupDyn("__v", at), m.Item.ByteWidth)};");
                w.Line($"__r.{m.Name} = new {m.Element}[__o.Length];");
                string body = m.Record is not null
                    ? $"{{ if ({m.Record}.__TryGet(__o[__i]{(nested ? "" : ", null, 0")}, {each} + \".\", ref __bad, out var __g)) "
                        + $"__r.{m.Name}[__i] = __g; else __ok = false; }}"
                    : $"{{ var __s = __o[__i]; {GetLeaf(m.Item, m.Type, $"__r.{m.Name}[__i]", "0", each)} }}";
                w.Line($"for (int __i = 0; __i < __o.Length; __i++) {body}");
            }
            return;
        }
        w.Line(m.Occurs == 0
            ? $"{{ var __g = __r.{m.Name}; if (!{m.Record}.__TryGetDyn(__v, {at}, {name} + \".\", ref __bad, ref __g)) __ok = false; "
                + $"__r.{m.Name} = __g; }}"
            : $"if (__r.{m.Name} is {{ }} __t{m.Ordinal}) for (int __i = 0; __i < __t{m.Ordinal}.Length; __i++) "
                + $"if (!{m.Record}.__TryGetDyn(__v, {at} + __i * {CellComponents.PerOccurrence(m.Item)}, {each} + \".\", "
                + $"ref __bad, ref __t{m.Ordinal}[__i])) __ok = false;");
    }

    /// <summary>A member's references into the cell area <c>__c</c> (<c>__PutSlots</c>): a pointer or object-reference
    /// member into its managed slot (<c>CobolPtr.SlotWrite</c>, the store every description of the area reads through), a
    /// data pointer's .NET null as the NULL address, as for an elementary formal; a nested record's through its own.
    /// The image walk (<c>__Put</c>) has already required every table to have its occurrences.</summary>
    private static void EmitPutSlots(DataItem group, Member m, CodeWriter w)
    {
        string Put(string value, string at) => m.Record is not null
            ? $"{value}.__PutSlots(__c, {at});"
            : RuntimeApi.PtrSlotWrite("__c", $"({at}) / 8", m.Item.Pic!.ClrType,
                m.Item.Pic.Category is PicCategory.Pointer ? $"{value} ?? ManagedPointer.Null" : value) + ";";
        w.Line(m.Occurs == 0
            ? Put(m.Name, Position(group, m, false))
            : $"for (int __i = 0; __i < {m.Name}.Length; __i++) {Put($"{m.Name}[__i]", Position(group, m, true))}");
    }

    private void EmitPut(DataItem root, DataItem group, Member m, CodeWriter w)
    {
        if (m.IsComponent) return;   // a component has no image: it lands in the carrier (EmitVar)
        // A reference has no image: it lands in its slot (__PutSlots), and a table of them is held to its occurrences here.
        bool slot = SlotWindow.CarriedBySlot(m.Item);
        if (slot && m.Occurs == 0) return;
        string path = $"__path + {CsLiteral(m.Name)}";
        if (m.Occurs == 0)
        {
            w.Line(m.Record is not null
                ? $"{m.Name}.__Put(__b, {Position(group, m, false)}, __parameter, {path} + \".\");"
                : PutLeaf(m.Item, m.Type, m.Name, Position(group, m, false), path));
            return;
        }
        using (w.Block(""))
        {
            // A table's array has its occurrences: the DEPENDING ON item's landed value for a count the record holds
            // (ISO §13.18.38.4 7)), the declared count otherwise.
            string count = m.Depending is { } dep
                ? $"HostLanding.LandedOccurrences({DependingImage(root, group, dep, "__b", buffer: true)}, {profileOf(dep)}, "
                    + $"{m.Minimum}, {m.Occurs}, __parameter, {path}, {CsLiteral(dep.CobolName!)})"
                : $"{m.Occurs}";
            string require = $"HostLanding.RequireOccurrences({m.Name}, {count}, {m.Minimum}, {m.Occurs}, __parameter, {path});";
            if (slot)
            {
                w.Line(require);
                return;
            }
            w.Line($"var __a = {require}");
            string at = Position(group, m, true);
            string each = $"{path} + \"[\" + __i + \"]\"";
            w.Line($"for (int __i = 0; __i < __a.Length; __i++) "
                + (m.Record is not null
                    ? $"__a[__i].__Put(__b, {at}, __parameter, {each} + \".\");"
                    : PutLeaf(m.Item, m.Type, "__a[__i]", at, each)));
        }
    }

    /// <summary>One elementary member's landing into the image buffer <c>__b</c> at bit position <paramref name="at"/>:
    /// the landing an elementary formal of its description gets (<c>HostLanding</c>), written in the member's storage
    /// form — a numeric item's record image under its profile, a national item's UTF-16BE bytes, a bit item's boolean
    /// positions spliced into a shared byte.</summary>
    private string PutLeaf(DataItem leaf, HostType type, string value, string at, string member)
    {
        PicInfo pic = leaf.Pic!;
        string bytes = $"({at}) / 8";
        string Copy(string chars) =>
            $"{(NationalWindow.PositionsOf(leaf) is not null ? RuntimeApi.NatBytes(chars) : chars)}.CopyTo(__b.AsSpan({bytes}));";
        switch (type.Kind)
        {
            case HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec:
            {
                string profile = profileOf(leaf);
                return Copy(RuntimeApi.NumFormatImage(
                    HostEntryEmitter.Unsigned16(pic, $"HostLanding.Land({value}, {profile}, __parameter, {member})"), profile));
            }
            case HostTypeKind.Index:
                return Copy(RuntimeApi.NumFormatImage($"(Int128){value}", profileOf(leaf)));
            case HostTypeKind.Float:
                return Copy(RuntimeApi.NumFormatImageFloat(value, profileOf(leaf), pic.IsSingle));
            case HostTypeKind.Text when BitLayout.IsBitLeaf(leaf):
                return RuntimeApi.BitsWriteWindowInto("__b", at,
                    $"HostLanding.LandText({value}, {pic.Length}, false, true, __parameter, {member})") + ";";
            case HostTypeKind.Text:
                return Copy($"HostLanding.LandText({value}, {NationalWindow.PositionsOf(leaf) ?? leaf.ByteWidth}, "
                    + $"{(leaf.Justified ? "true" : "false")}, {(pic.Category is PicCategory.Boolean ? "true" : "false")}, __parameter, {member})");
            default:
                throw new InvalidOperationException(
                    $"PublishedRecordEmitter: the member '{leaf.CobolName}' ({type.Kind}) has no image (Renders admits none)");
        }
    }

    private void EmitGet(DataItem root, DataItem group, Member m, CodeWriter w)
    {
        if (m.IsComponent) return;   // a component has no image: it is decoded from the carrier (EmitVar)
        string name = $"__name + {CsLiteral(m.Name)}";
        if (m.Occurs == 0)
        {
            w.Line(GetMember(m, $"__r.{m.Name}", Position(group, m, false), name));
            return;
        }
        string each = GetMember(m, $"__r.{m.Name}[__i]", Position(group, m, true), $"{name} + \"[\" + __i + \"]\"");
        if (m.Depending is not { } dep)
        {
            w.Line($"for (int __i = 0; __i < {m.Occurs}; __i++) {each}");
            return;
        }
        // The table's current occurrences are the DEPENDING ON item's content, decoded by the validity rule and within
        // the table's bounds (ISO §13.18.38.4 7)); the occurrences past them are undefined there and are not decoded.
        w.Line($"{{ if (HostLanding.TryOccurrences({DependingImage(root, group, dep, "__s", buffer: false)}, {profileOf(dep)}, "
            + $"{m.Minimum}, {m.Occurs}, {name}, {CsLiteral(dep.CobolName!)}, ref __bad, out int __n)) "
            + $"{{ __r.{m.Name} = new {m.Element}[__n]; for (int __i = 0; __i < __n; __i++) {each} }} else __ok = false; }}");
    }

    private string GetMember(Member m, string target, string at, string name) =>
        m.Record is not null
            ? $"{{ if ({m.Record}.__TryGet(__s, __c, {at}, {name} + \".\", ref __bad, out var __g)) {target} = __g; else __ok = false; }}"
            : GetLeaf(m.Item, m.Type, target, at, name);

    /// <summary>One elementary member decoded from the image <c>__s</c> at bit position <paramref name="at"/>: a numeric
    /// item by the one validity rule (a failure clears <c>__ok</c>), every other item as its storage holds it.</summary>
    private string GetLeaf(DataItem leaf, HostType type, string target, string at, string name)
    {
        PicInfo pic = leaf.Pic!;
        // A pointer or object reference is its managed slot's value (CobolPtr.SlotRead, the read every description of
        // the area makes); an image alone (FromImage) holds no reference, so the member keeps its null state.
        if (SlotWindow.CarriedBySlot(leaf))
            return $"{target} = __c is null ? {pic.DefaultInitializer} : "
                + $"{RuntimeApi.PtrSlotRead("__c", $"({at}) / 8", pic.ClrType, pic.DefaultInitializer)};";
        string bytes = $"({at}) / 8";
        string chars = Characters(leaf, $"__s.Substring({bytes}, {leaf.ByteWidth})");
        return type.Kind switch
        {
            HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec =>
                HostEntryEmitter.NumericDecode(type, pic, chars, profileOf(leaf), name, target, "__bad", " else __ok = false;"),
            // An INDEX data item is of class index, of which no class condition may be asked ("Identifier-1 shall not
            // reference a data item of class index", ISO §8.8.4.4.3 1)), so the validity rule does not apply: its eight
            // bytes are the value the program stored, and the codec decodes them as the record struct's GroupImageCodec does.
            HostTypeKind.Index => $"{target} = (long){RuntimeApi.NumParseImage(chars, profileOf(leaf), sending: false)};",
            // The image codec's own decode (the record struct's GroupImageCodec decodes a float leaf the same way): the
            // record's bytes are the host's, not a windowed leaf a statement reads, so NumericRenderer.WindowedNum is not
            // the reader here. A floating-point member's host type is its own carrier (§4.3's table), so the decode is
            // the codec's, exactly as an elementary floating-point formal's write-back takes its cell's value.
            HostTypeKind.Float =>
                $"{target} = ({pic.ClrType}){RuntimeApi.NumParseImageFloat(chars, profileOf(leaf), pic.IsSingle)};",
            HostTypeKind.Text when BitLayout.IsBitLeaf(leaf) => $"{target} = {RuntimeApi.BitsReadWindow("__s", at, $"{pic.Length}")};",
            HostTypeKind.Text => $"{target} = {chars};",
            _ => throw new InvalidOperationException(
                $"PublishedRecordEmitter: the member '{leaf.CobolName}' ({type.Kind}) has no image (Renders admits none)"),
        };
    }
}
