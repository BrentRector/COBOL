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
    /// <summary>Whether this slice publishes a record for <paramref name="group"/>: a group with no variable-length
    /// component — a dynamic-length item or a dynamic-capacity table, which ride the cell's variable part. An OCCURS
    /// DEPENDING ON table is held (its storage is the maximum allocation, ISO §8.5.1.8, the image the CALL lane's text
    /// crossing carries), and so is a pointer or object reference (its managed slot, <see cref="HoldsSlots"/>).
    /// <para>⚠ WIP (kb/Work PB2099): the variable-length shapes cross as the CALL lane's variable-length carrier, which
    /// the published record renders in the slice's next step; until then a unit with such a formal publishes no host
    /// entry.</para></summary>
    internal static bool Renders(DataItem group) =>
        group.IsGroup && DataItem.DescendantsOf(group).Where(Occupies).All(d =>
            !DynSlotWindow.CarriedBySlot(d) && !VarGroupWindow.Applies(d) && !d.IsDynamicTable && !d.IsDynamicLength
            && !d.IsAnyLength);

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
    /// entries; every other table's array has its <see cref="DataItem.Occurs"/> (the maximum, for a DEPENDING ON item
    /// outside the record).</summary>
    private sealed record Member(DataItem Item, string Name, HostType Type, string? Record, DataItem? Depending)
    {
        public int Occurs => Item.Occurs ?? 0;
        public int Minimum => Depending is null ? Occurs : Item.OccursSpec!.Min;
        public string Element => Record ?? Type.CsType!;
        public string PropertyType => Occurs > 0 ? $"{Element}[]" : Element;
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
                DependingInRecord(root, m)));
        }
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
            w.Line();
            w.Line($"/// <summary>A record whose text members are empty (spaces in the image), whose numbers are zero and "
                + "whose tables have their declared occurrences.</summary>");
            using (w.Block($"public {name}()"))
                foreach (var m in members) EmitInitial(m, counts, w);
            foreach (var m in members)
            {
                w.Line($"/// <summary>{m.Item.Level:00} {m.Item.CobolName}{(m.Occurs > 0 ? $" OCCURS {m.Occurs}" : "")}.</summary>");
                w.Line($"public {m.PropertyType} {m.Name}" + " { get; set; }");   // a COBOL member, named by the allocator
            }
            w.Line();
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
            using (w.Block("internal readonly string __Land(string __parameter)"))
            {
                w.Line($"var __b = (__image ?? {BlankImage(group, width)}).ToCharArray();");
                w.Line("__Put(__b, 0, __parameter, \"\");");
                w.Line("return new string(__b);");
            }
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
        if (m.Occurs > 0)
        {
            int n = m.Depending is { } dep ? counts[dep] : m.Occurs;
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
