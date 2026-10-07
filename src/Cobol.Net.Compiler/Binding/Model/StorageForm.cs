// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;

namespace CobolNet.Binding.Model;

/// <summary>
/// The canonical storage representation of an elementary item or table element (rearchitecture PHASE 05;
/// DESIGN-data-model §2.1). A closed, pure value classification — no C# strings, no emit logic; the emitter
/// interprets it. Computed ONCE by <c>StorageFormPass</c> and (from Step 10) held init-only on <c>DataItem</c>,
/// replacing the late-mutated <c>StoreAsImage</c> flag and the recursive image-fact properties. A new representation
/// is a NEW case (an exhaustive switch makes a missing arm a compile error, not a runtime loud-stmt).
/// <para>The <c>Width</c> carried on the native cases is the item's CHARACTER-IMAGE width
/// (<c>ElementaryImageWidth</c> = digit count + a separate-sign position, ISO §13.18.52) — precomputed at
/// construction so <see cref="ImageWidth"/> reproduces <c>DataItem.ImageWidth</c> from the form alone (the D0
/// amendment, DEVLOG 749).</para>
/// </summary>
public abstract record StorageForm
{
    /// <summary>Does this leaf contribute character positions to an enclosing group's image codec
    /// (<c>AsImage()</c>/<c>FromImage()</c>, §14.4)? (An item-level query short-circuits on OCCURS DYNAMIC and
    /// recurses <c>Children.All</c> for a group — see <c>StorageFacts</c>.)</summary>
    public abstract bool IsCharacterImage { get; }

    /// <summary>This item's per-occurrence character-image width.</summary>
    public abstract int ImageWidth { get; }

    /// <summary>Native scaled integer — <c>long</c> (≤18 digits) or <c>Int128</c> (>18). Not character-imageable as
    /// itself, but IS image-CAPABLE (its zoned digit image is derived on demand). <paramref name="Width"/> = digits
    /// + separate-sign.</summary>
    public sealed record NativeInt(bool Wide, int Digits, int Width) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Width;
    }

    /// <summary>Native IEEE float (COMP-1/FLOAT-SHORT) or double (COMP-2). Never in a static record image.</summary>
    public sealed record NativeFloat(bool Single, int Width) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Width;
    }

    /// <summary>A C# <c>string</c> of exactly <paramref name="Width"/> characters — the ONE string-stored case:
    /// alphanumeric / numeric-edited / national / boolean, OR a numeric-DISPLAY (or Tier-B BINARY/PACKED) leaf
    /// promoted to its zoned image because it is used under a whole-group operand (§14.9.25.4 GR4). <paramref
    /// name="Category"/> is retained so the numeric pipeline decodes/encodes zoned images and so the promoted-numeric
    /// sub-case (<c>Category is Numeric</c>) equals the legacy <c>StoreAsImage</c>.</summary>
    public sealed record CharImage(int Width, PicCategory Category) : StorageForm
    {
        public override bool IsCharacterImage => true;
        public override int ImageWidth => Width;
    }

    /// <summary>A Tier-B REDEFINES view: a typed (offset, width) window over the class's ONE shared string backing.
    /// Assigned ONLY to a Tier-B member that is NOT image-promoted (a group backing / a non-numeric-display view); a
    /// Tier-B numeric-DISPLAY/BINARY/PACKED leaf is <see cref="CharImage"/>(Numeric) instead.</summary>
    public sealed record TierBWindow(RedefinesClass Class, int Offset, int Width) : StorageForm
    {
        public override bool IsCharacterImage => true;
        public override int ImageWidth => Width;
    }

    // TierCWindow DELETED (the Step D arm-1 dissolution): it was quarantined with a zero-assignment parity
    // obligation — the zero-fan-out trap the design §2.3 names — and its job dissolved into TierBWindow when
    // every numeric leaf kind's byte form was pinned (NumericByteForm; PB164 waves 1–2 + R40).

    /// <summary>An out-of-line OCCURS DYNAMIC table (<c>CobolDynTable&lt;T&gt;</c>, D9). <paramref name="Element"/> is
    /// the per-occurrence element's form — image-PROMOTED by the same rule as any leaf, so a numeric element a
    /// character channel writes is <see cref="CharImage"/> (kb/Work PB1939, PB2004). The table itself is never
    /// character-image (an item-level query short-circuits on IsDynamicTable).</summary>
    public sealed record DynamicTable(StorageForm Element) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Element.ImageWidth;
    }

    /// <summary>A DYNAMIC LENGTH elementary item (ISO §8.5.1.10 / §13.18.19; COBOL-2014) — a variable-length,
    /// minimum-length-zero native <c>string</c> whose current length varies at runtime. Never a fixed character
    /// image: a dynamic-length item makes its enclosing group a VARIABLE-LENGTH group (§8.5.1.12), so an item-level
    /// query short-circuits on it just like <see cref="DynamicTable"/>. <paramref name="Category"/> is Alphanumeric
    /// (PIC X) or National (PIC N), §13.18.19.4 GR1. <paramref name="Limit"/> is the maximum character count from
    /// the LIMIT phrase (§13.18.19.4 GR2), or -1 for the implementor-defined maximum (no explicit LIMIT).</summary>
    public sealed record DynamicString(PicCategory Category, int MaxSize) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => 0;
    }

    /// <summary>A .NET object reference. Carries the WHOLE §13.18.60.2 description (kind × FACTORY × ONLY ×
    /// name) rather than a class name, so the storage form and <c>PicInfo.ClrType</c> derive the emitted type
    /// from ONE place — <c>ObjectRefDescriptor.ClrTypeName</c> (kb/Work PB389; before it, the derivation was
    /// written twice and the FACTORY singleton type could not be spelled at all). Zero character positions.</summary>
    public sealed record ObjectRef(ObjectRefDescriptor Desc) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => 0;
    }

    /// <summary>A data pointer (<c>ManagedPointer</c>). Zero character positions.</summary>
    public sealed record PointerRef : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => 0;
    }

    /// <summary>A program pointer (<c>ProgramPointer</c> — the address of an OUTERMOST program, ISO §13.18.60
    /// GR24; P10 Step 7). Zero character positions (representation is implementor-defined — this
    /// implementation's is the managed identity carrier, never storage bytes).</summary>
    public sealed record ProgramPointerRef : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => 0;
    }

    /// <summary>A function pointer (<c>FunctionPointer</c> — the address of a function, ISO §13.18.60.4 GR26;
    /// kb/Work PB452/PB817). Zero character positions, exactly like its ProgramPointerRef twin (GR26 leaves the
    /// representation implementor-defined — this implementation's is the managed identity carrier).</summary>
    public sealed record FunctionPointerRef : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => 0;
    }

    /// <summary>A USAGE INDEX cell (a <c>long</c> occurrence number). Zero character positions.</summary>
    public sealed record IndexCell(int Width) : StorageForm
    {
        public override bool IsCharacterImage => false;
        public override int ImageWidth => Width;
    }
}
