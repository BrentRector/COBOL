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
    /// <summary>Whether this slice publishes a record for <paramref name="group"/>: a fixed group with no member the
    /// record image does not hold — a pointer or object reference (a managed slot), a
    /// dynamic-length item or dynamic-capacity table (the cell's variable part), an OCCURS DEPENDING ON table.
    /// <para>⚠ WIP (kb/Work PB2099, wave 1080 checkpoint): those shapes cross as the CALL lane's cell-area and
    /// variable-length carriers (§4.3's last table row), which the published record renders in the slice's next step;
    /// until then a unit with such a formal publishes no host entry.</para></summary>
    internal static bool Renders(DataItem group) =>
        group.IsGroup && DataItem.DescendantsOf(group).Where(Occupies).All(d =>
            !SlotWindow.CarriedBySlot(d) && !DynSlotWindow.CarriedBySlot(d)
            && !VarGroupWindow.Applies(d) && !d.IsDynamicTable && !d.TableBoundsPending
            && d.OccursSpec?.DependingName is null && !d.IsDynamicLength && !d.IsAnyLength);

    /// <summary>An entry that occupies storage of the record: a group or elementary item, never a condition-name or a
    /// RENAMES entry.</summary>
    private static bool Occupies(DataItem d) => d.Level is not (66 or 88) && (d.IsGroup || d.IsElementary);

    /// <summary>The record's members: its named, non-redefining subordinates, in source order.</summary>
    private static IEnumerable<DataItem> MembersOf(DataItem group) =>
        group.Children.Where(c => Occupies(c) && c.CobolName is not null && c.RedefinesTargetName is null);

    /// <summary>The width of the record's image in characters, one per byte of its storage.</summary>
    internal static int Width(DataItem group) => group.ByteWidth;

    /// <summary>One member as the struct presents it.</summary>
    private sealed record Member(DataItem Item, string Name, HostType Type, string? Record)
    {
        public int Occurs => Item.Occurs ?? 0;
        public string Element => Record ?? Type.CsType!;
        public string PropertyType => Occurs > 0 ? $"{Element}[]" : Element;
    }

    /// <summary>Emit the published record struct of <paramref name="group"/>, named <paramref name="name"/>, as a nested
    /// type at the writer's position.</summary>
    internal void Emit(DataItem group, string name, CodeWriter w)
    {
        var scope = CsNameScope.RecordStruct();
        scope.Reserve([name]);   // a member never spells its enclosing type (CS0542)
        var members = new List<Member>();
        foreach (var m in MembersOf(group))
        {
            string word = DataItem.Sanitize(m.CobolName!);
            string prop = CsNames.Allocate(word, scope);
            HostType type = HostSurface.TypeOf(m);
            members.Add(new Member(m, prop, type, type.Kind is HostTypeKind.Group ? CsNames.Allocate($"{word}_Record", scope) : null));
        }
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
                foreach (var m in members) EmitInitial(m, w);
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
                w.Line($"__TryGet(HostLanding.RequireImage(image, {width}, {CsLiteral(name)}), 0, \"\", ref __bad, out var __r);");
                w.Line("return __bad is null ? __r : throw __bad;");
            }
            using (w.Block("internal readonly string __Land(string __parameter)"))
            {
                w.Line($"var __b = (__image ?? {BlankImage(group, width)}).ToCharArray();");
                w.Line("__Put(__b, 0, __parameter, \"\");");
                w.Line("return new string(__b);");
            }
            using (w.Block("internal readonly void __Put(char[] __b, int __at, string __parameter, string __path)"))
                foreach (var m in members) EmitPut(group, m, w);
            using (w.Block($"internal static bool __TryGet(string __s, int __at, string __name, ref global::System.FormatException? __bad, out {name} __r)"))
            {
                w.Line($"__r = new {name}();");
                if (!BitLayout.IsBitItem(group))
                    w.Line($"__r.__image = __s.Substring(__at / 8, {width});");
                w.Line("bool __ok = true;");
                foreach (var m in members) EmitGet(group, m, w);
                w.Line("return __ok;");
            }
            foreach (var m in members.Where(m => m.Record is not null))
            {
                w.Line();
                Emit(m.Item, m.Record!, w);
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

    private static void EmitInitial(Member m, CodeWriter w)
    {
        bool text = m.Type.Kind is HostTypeKind.Text;
        if (m.Occurs > 0)
        {
            w.Line($"{m.Name} = new {m.Element}[{m.Occurs}];");
            if (text) w.Line($"global::System.Array.Fill({m.Name}, \"\");");
            if (m.Record is not null) w.Line($"for (int __i = 0; __i < {m.Occurs}; __i++) {m.Name}[__i] = new();");
        }
        else if (text) w.Line($"{m.Name} = \"\";");
        else if (m.Record is not null) w.Line($"{m.Name} = new();");
    }

    /// <summary>The member's bit position in the image, relative to the record's own (<paramref name="group"/>'s) start
    /// at <c>__at</c>: the one placement walk (<see cref="BitLayout.ChildStarts"/>), and for an occurrence the one
    /// subscript stride.</summary>
    private static string Position(DataItem group, Member m, bool occurrence)
    {
        int rel = BitLayout.StartBitWithin(group, m.Item);
        return $"__at + {rel}{(occurrence ? $" + __i * {BitLayout.StrideBits(m.Item)}" : "")}";
    }

    private void EmitPut(DataItem group, Member m, CodeWriter w)
    {
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
            w.Line($"var __a = HostLanding.RequireOccurrences({m.Name}, {m.Occurs}, {m.Occurs}, {m.Occurs}, __parameter, {path});");
            string at = Position(group, m, true);
            string each = $"{path} + \"[\" + __i + \"]\"";
            w.Line($"for (int __i = 0; __i < {m.Occurs}; __i++) "
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

    private void EmitGet(DataItem group, Member m, CodeWriter w)
    {
        string name = $"__name + {CsLiteral(m.Name)}";
        if (m.Occurs == 0)
        {
            w.Line(GetMember(m, $"__r.{m.Name}", Position(group, m, false), name));
            return;
        }
        w.Line($"for (int __i = 0; __i < {m.Occurs}; __i++) "
            + GetMember(m, $"__r.{m.Name}[__i]", Position(group, m, true), $"{name} + \"[\" + __i + \"]\""));
    }

    private string GetMember(Member m, string target, string at, string name) =>
        m.Record is not null
            ? $"{{ if ({m.Record}.__TryGet(__s, {at}, {name} + \".\", ref __bad, out var __g)) {target} = __g; else __ok = false; }}"
            : GetLeaf(m.Item, m.Type, target, at, name);

    /// <summary>One elementary member decoded from the image <c>__s</c> at bit position <paramref name="at"/>: a numeric
    /// item by the one validity rule (a failure clears <c>__ok</c>), every other item as its storage holds it.</summary>
    private string GetLeaf(DataItem leaf, HostType type, string target, string at, string name)
    {
        PicInfo pic = leaf.Pic!;
        string bytes = $"({at}) / 8";
        string chars = NationalWindow.PositionsOf(leaf) is { } positions
            ? RuntimeApi.NatReadWindow("__s", bytes, $"{positions}")
            : $"__s.Substring({bytes}, {leaf.ByteWidth})";
        return type.Kind switch
        {
            HostTypeKind.Integer or HostTypeKind.Decimal or HostTypeKind.Dec =>
                HostEntryEmitter.NumericDecode(type, pic, chars, profileOf(leaf), name, target, "__bad", " else __ok = false;"),
            HostTypeKind.Index =>
                $"{{ if (HostLanding.TryDecode({chars}, {profileOf(leaf)}, {name}, ref __bad, out Int128 __hu)) {target} = (long)__hu; else __ok = false; }}",
            HostTypeKind.Float =>
                $"{target} = ({pic.ClrType}){RuntimeApi.NumParseImageFloat(chars, profileOf(leaf), pic.IsSingle)};",
            HostTypeKind.Text when BitLayout.IsBitLeaf(leaf) => $"{target} = {RuntimeApi.BitsReadWindow("__s", at, $"{pic.Length}")};",
            HostTypeKind.Text => $"{target} = {chars};",
            _ => throw new InvalidOperationException(
                $"PublishedRecordEmitter: the member '{leaf.CobolName}' ({type.Kind}) has no image (Renders admits none)"),
        };
    }
}
