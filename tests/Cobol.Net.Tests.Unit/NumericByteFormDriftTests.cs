// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE BYTE-FORM TABLE (V59 step 2). Every USAGE has exactly ONE byte representation, stated here as a
/// table so that adding a usage to the compiler cannot silently inherit a representation it was never given.
/// <para>
/// This is the discriminator the record image had been missing. The compiler already pinned each usage's byte
/// WIDTH (<see cref="PicInfo.StorageWidth"/>, reported by <c>FUNCTION BYTE-LENGTH</c>) but nothing carried the
/// FORM those bytes take, and <see cref="NumericTruncation"/> cannot stand in for it — USAGE DISPLAY and USAGE
/// BINARY are both <see cref="NumericTruncation.DigitCount"/> and occupy entirely different bytes. That is how a
/// <c>PIC 9(4) COMP</c> came to reach a file as the four ASCII bytes <c>31 32 33 34</c>.
/// </para>
/// <para>
/// The representations themselves are implementor-defined and therefore OURS to pin and document (ISO/IEC
/// 1989:2023 §13.18.60.4 GR4 "a radix of 2 is used", GR11 "a radix of 10 … the minimum possible configuration",
/// GR7 for DISPLAY, GR12 for the fixed-width binary usages; §4.2.16 and Annex A.1 items 205/215 make documenting
/// them a REQUIRED item). <see cref="NumericByteForm"/>'s members carry that documentation.
/// </para>
/// </summary>
public sealed class NumericByteFormDriftTests
{
    /// <summary>The whole table, usage by usage: the byte representation and the capacity discipline. Two
    /// ORTHOGONAL axes — a row where they seem to track each other (DISPLAY/BINARY both DigitCount; PACKED both
    /// packed) is a coincidence of this table, never a rule to lean on.</summary>
    public static readonly IReadOnlyDictionary<Usage, (NumericByteForm Form, NumericTruncation Truncation)> Table =
        new Dictionary<Usage, (NumericByteForm, NumericTruncation)>
        {
            // ── The profile-carrying fixed-point usages: these reach a byte boundary ──
            [Usage.Display] = (NumericByteForm.Zoned, NumericTruncation.DigitCount),
            [Usage.Binary] = (NumericByteForm.Binary, NumericTruncation.DigitCount),
            [Usage.Packed] = (NumericByteForm.Packed, NumericTruncation.PackedDecimal),
            [Usage.Comp5] = (NumericByteForm.Binary, NumericTruncation.BinaryCapacity),
            [Usage.BinaryChar] = (NumericByteForm.Binary, NumericTruncation.BinaryCapacity),
            [Usage.BinaryShort] = (NumericByteForm.Binary, NumericTruncation.BinaryCapacity),
            [Usage.BinaryLong] = (NumericByteForm.Binary, NumericTruncation.BinaryCapacity),
            [Usage.BinaryDouble] = (NumericByteForm.Binary, NumericTruncation.BinaryCapacity),
            // ── USAGE INDEX (the R40 owner decision, 2026-08-30): the occurrence number as an 8-byte
            // BIG-ENDIAN two's-complement binary — the documented 64-bit carrier's bytes in the byte order
            // every other pinned form uses (A.1 item 211), so an INDEX-leaf group crosses images/CALL/MOVE/
            // DISPLAY. §13.18.60.3 SR10 still restricts what may REFERENCE the item. ──
            [Usage.Index] = (NumericByteForm.Binary, NumericTruncation.DigitCount),
            // ── The float family (kb/Work PB164 wave 2): the IEEE 754 interchange forms — binary32 for the
            // 4-byte usages, binary64 for the 8-byte ones (§13.18.60.4 GR14/GR15 pin the FLOAT-BINARY formats;
            // GR13/GR21 leave the rest to the implementor, one encoding serving both). EmitProfiles emits
            // float profiles; the byte ORDER is the profile's FloatLittleEndian axis (§11.9.8), not a row here. ──
            [Usage.Float] = (NumericByteForm.Ieee32, NumericTruncation.DigitCount),
            [Usage.Double] = (NumericByteForm.Ieee64, NumericTruncation.DigitCount),
            [Usage.FloatShort] = (NumericByteForm.Ieee32, NumericTruncation.DigitCount),
            [Usage.FloatLong] = (NumericByteForm.Ieee64, NumericTruncation.DigitCount),
            [Usage.FloatExtended] = (NumericByteForm.Ieee64, NumericTruncation.DigitCount),
            [Usage.FloatBinary32] = (NumericByteForm.Ieee32, NumericTruncation.DigitCount),
            [Usage.FloatBinary64] = (NumericByteForm.Ieee64, NumericTruncation.DigitCount),
            // ── The processor-dependent non-support formats (rejected at ParseUsage, COBOLNET1564) — no byte
            // form; None states that rather than defaulting to a lie. ──
            [Usage.FloatBinary128] = (NumericByteForm.None, NumericTruncation.DigitCount),
            [Usage.FloatDecimal16] = (NumericByteForm.None, NumericTruncation.DigitCount),
            [Usage.FloatDecimal34] = (NumericByteForm.None, NumericTruncation.DigitCount),
            // ── USAGE NATIONAL is ZONED, exactly as DISPLAY is (data-model design D-N7, kb/Work PB646). A
            // national-form numeric item is its DISPLAY twin COMPOSED with the one national byte transform:
            // §13.18.40.4 GR1 — "When the usage of the subject of the entry is national, each symbol
            // representing a character position defines a national character position" — makes its digits, its
            // separate-sign position and its insertion characters the SAME characters the twin holds, in a
            // different character set, and §13.18.60.4 GR8 pins only the SIZE of one of those characters (D-N1:
            // two bytes, UTF-16BE). So the FORM those characters take is the zoned digit run and CobolNum owns
            // every digit / sign / de-edit rule ONCE; the CHARACTER→BYTE step is applied separately and once, by
            // NationalWindow.PositionsOf + CobolBits.NatBytes at every byte boundary. A second "NationalZoned"
            // byte form would have had to re-derive all of them. ⛔ This row read None while §13.18.60.3 SR12's
            // numeric and numeric-edited shapes were refused by name at COBOLNET0899 — the table stated a
            // representation for a population that could not be compiled, so nothing could contradict it. ──
            [Usage.National] = (NumericByteForm.Zoned, NumericTruncation.DigitCount),
            [Usage.Bit] = (NumericByteForm.None, NumericTruncation.DigitCount),
            [Usage.Pointer] = (NumericByteForm.None, NumericTruncation.DigitCount),
            [Usage.ProgramPointer] = (NumericByteForm.None, NumericTruncation.DigitCount),
            [Usage.FunctionPointer] = (NumericByteForm.None, NumericTruncation.DigitCount),
            // ── USAGE MESSAGE-TAG (kb/Work PB487): DECLINED non-support, refused by name in ParseUsage
            // (COBOLNET1943) exactly like FLOAT-BINARY-128 above. It has no numeric byte form and cannot acquire
            // one: §13.18.60.4 GR9 — "The class and category of a message-tag data item are message-tag" — so the
            // item is not of class numeric at all, and None states that rather than defaulting to a lie. ──
            [Usage.MessageTag] = (NumericByteForm.None, NumericTruncation.DigitCount),
            [Usage.ObjectReference] = (NumericByteForm.None, NumericTruncation.DigitCount),
        };

    private static PicInfo Pic(Usage usage, int digits = 4, bool signed = false) =>
        // Category numeric throughout: ByteForm/Truncation read the USAGE (and the WITH NO SIGN phrase), never
        // the category, so one shape exercises every row.
        new(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: 0, Signed: signed);

    /// <summary>A NEW usage lands here first: the table must name it, so its byte representation is a decision
    /// somebody made rather than whatever the fallback arm happened to return.</summary>
    [Fact]
    public void EveryUsage_IsInTheTable()
    {
        var missing = Enum.GetValues<Usage>().Where(u => !Table.ContainsKey(u)).ToList();
        Assert.True(missing.Count == 0,
            "USAGE without a stated byte form — add it to the table (and decide its byte representation): "
            + string.Join(", ", missing));
        var stale = Table.Keys.Where(u => !Enum.IsDefined(u)).ToList();
        Assert.True(stale.Count == 0, "table row for a usage that no longer exists: " + string.Join(", ", stale));
    }

    [Theory]
    [MemberData(nameof(AllUsages))]
    public void EveryUsage_MapsToItsStatedFormAndDiscipline(Usage usage)
    {
        var (form, truncation) = Table[usage];
        Assert.Equal(form, Pic(usage).ByteForm);
        Assert.Equal(truncation, Pic(usage).Truncation);
    }

    public static TheoryData<Usage> AllUsages()
    {
        var data = new TheoryData<Usage>();
        foreach (var u in Enum.GetValues<Usage>()) data.Add(u);
        return data;
    }

    /// <summary>PACKED-DECIMAL WITH NO SIGN (§13.18.60.4 GR11, COBOL-2023) is a DIFFERENT byte form, not a
    /// narrower one: it reserves no sign nibble at all, so a decoder that assumed one would read the last digit
    /// as a sign. The width follows.</summary>
    [Fact]
    public void PackedWithNoSign_IsItsOwnForm()
    {
        var withSign = Pic(Usage.Packed);
        var noSign = Pic(Usage.Packed) with { PackedNoSign = true };
        Assert.Equal(NumericByteForm.Packed, withSign.ByteForm);
        Assert.Equal(NumericByteForm.PackedNoSign, noSign.ByteForm);
        Assert.Equal(3, withSign.StorageWidth);   // 4 digits + sign nibble → 3 bytes
        Assert.Equal(2, noSign.StorageWidth);     // 4 digit nibbles → 2 bytes
    }

    /// <summary>The two properties must agree about whether the item HAS bytes of its own: a byte form is
    /// exactly a positive pinned width, and <see cref="NumericByteForm.Zoned"/>'s width is its digit run
    /// (<see cref="PicInfo.StorageWidth"/> stays 0 — the zoned digits ARE the bytes).</summary>
    [Theory]
    [MemberData(nameof(AllUsages))]
    public void ByteForm_AndPinnedWidth_Agree(Usage usage)
    {
        var pic = Pic(usage);
        bool hasOwnBytes = pic.ByteForm is NumericByteForm.Binary or NumericByteForm.Packed
            or NumericByteForm.PackedNoSign or NumericByteForm.Ieee32 or NumericByteForm.Ieee64;
        Assert.Equal(hasOwnBytes, pic.StorageWidth > 0);
    }

    /// <summary>Nothing that reaches the record image may be formless — that is the state the image codec must
    /// never be handed, and the arm it will reject loudly.</summary>
    [Theory]
    [MemberData(nameof(AllUsages))]
    public void EveryImageCapableLeaf_HasAByteForm(Usage usage)
    {
        var item = new DataItem { Level = 5, CobolName = "X", CsName = "X", Pic = Pic(usage) };
        if (!item.IsImageCapable) return;
        Assert.NotEqual(NumericByteForm.None, item.Pic!.ByteForm);
    }

    /// <summary>The emitted profile carries BOTH axes to the runtime — the record-image codec reads the form,
    /// the store path reads the discipline. A profile that stated only one would put the codec back where V59
    /// found it.</summary>
    [Fact]
    public void EmittedProfile_StatesTheFormAndItsWidth()
    {
        Assert.Contains("ByteForm = NumericByteForm.Binary", Pic(Usage.Binary).ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("StorageLength = 2", Pic(Usage.Binary).ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("ByteForm = NumericByteForm.Packed", Pic(Usage.Packed).ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("StorageLength = 3", Pic(Usage.Packed).ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("ByteForm = NumericByteForm.Zoned", Pic(Usage.Display).ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("Truncation = NumericTruncation.PackedDecimal", Pic(Usage.Packed).ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("ByteForm = NumericByteForm.Ieee32", PicInfo.FloatItem(Usage.Float).ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("StorageLength = 8", PicInfo.FloatItem(Usage.Double).ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("ByteForm = NumericByteForm.Binary", PicInfo.IndexItem.ProfileInitializer(SignEncoding.Ibm));
        Assert.Contains("StorageLength = 8", PicInfo.IndexItem.ProfileInitializer(SignEncoding.Ibm));
    }

    /// <summary>The R40 pin at BYTE level, against the decision rather than the lane's own inverse: an index
    /// item's image is its occurrence number as 8 big-endian two's-complement bytes through the ordinary
    /// Binary lane (occurrence 3 → seven zero bytes then 0x03), and the store lane inverts it.</summary>
    [Fact]
    public void IndexImage_IsEightBigEndianOccurrenceBytes()
    {
        var profile = new NumProfile
        {
            Digits = 0,
            FractionDigits = 0,
            Signed = true,   // the two's-complement pin — see IndexItem's doc (the R40 fleet round)
            Truncation = NumericTruncation.DigitCount,
            ByteForm = NumericByteForm.Binary,
            StorageLength = 8,
        };
        string image = CobolNum.FormatImage(3L, profile);
        Assert.Equal(8, image.Length);
        for (int i = 0; i < 7; i++) Assert.Equal((char)0, image[i]);
        Assert.Equal((char)3, image[7]);
        Assert.Equal(3, (long)CobolNum.ParseImage(image, profile));
        // TWO'S COMPLEMENT, not magnitude (the R40 review fleet — an unsigned profile over the signed long
        // carrier encoded |value| and decoded zero-extended, so the codec was not an involution and a group
        // MOVE of HIGH-VALUES rewrote the index window where §14.9.25.4 GR4 requires a representation copy):
        // −1 is eight 0xFF bytes (each imaged U+FFFF by the storage-byte law, kb/Work PB1759), and those bytes
        // parse back to −1.
        string neg = CobolNum.FormatImage(-1L, profile);
        for (int i = 0; i < 8; i++) Assert.Equal(StorageByte.ToChar(0xFF), neg[i]);
        Assert.Equal(-1, (long)CobolNum.ParseImage(neg, profile));
    }

    /// <summary>The FLOAT-BINARY endianness axis (§13.18.60.4 GR19 + §11.9.8, kb/Work PB164 wave 2), applied
    /// ONCE in <see cref="PicInfo.FloatItem"/>: an effective HIGH-ORDER-RIGHT flips the profile's
    /// <c>FloatLittleEndian</c> for the STANDARD binary float usages ONLY — the implementor-defined usages
    /// (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) are pinned big-endian regardless (GR13/GR21; GR19c scopes
    /// the clause to the standard usages), and the no-clause default is our documented HIGH-ORDER-LEFT
    /// (§11.9.8.3 SR1, Annex A.1 item 48).
    /// <para>The parameter is the item's EFFECTIVE endianness, so this test cannot see WHICH source supplied it.
    /// The two-level derivation §11.9.8.3 SR2/SR3 legislates — the item's own §13.18.60.2 endianness-phrase beats
    /// the OPTIONS-implied one — is merged in <c>DataBinder</c> and pinned END TO END, in BOTH directions, by the
    /// goldens <c>tests/conformance/2023/pb174_float_item_endianness{,_default}</c> (kb/Work PB174). Adding
    /// "precedence" rows here would pin nothing: both arms reach FloatItem identically.</para></summary>
    [Theory]
    [InlineData(Usage.FloatBinary32, FloatEndianness.HighOrderRight, true)]
    [InlineData(Usage.FloatBinary64, FloatEndianness.HighOrderRight, true)]
    [InlineData(Usage.FloatBinary32, FloatEndianness.HighOrderLeft, false)]
    [InlineData(Usage.FloatBinary32, FloatEndianness.Unspecified, false)]
    [InlineData(Usage.Float, FloatEndianness.HighOrderRight, false)]
    [InlineData(Usage.Double, FloatEndianness.HighOrderRight, false)]
    [InlineData(Usage.FloatShort, FloatEndianness.HighOrderRight, false)]
    [InlineData(Usage.FloatLong, FloatEndianness.HighOrderRight, false)]
    [InlineData(Usage.FloatExtended, FloatEndianness.HighOrderRight, false)]
    public void FloatBinaryEndianness_ReachesOnlyStandardBinaryFloatProfiles(
        Usage usage, FloatEndianness effective, bool expectLittle)
    {
        var pic = PicInfo.FloatItem(usage, effective);
        Assert.Equal(expectLittle, pic.FloatLittleEndian);
        Assert.Equal(expectLittle, pic.ProfileInitializer(SignEncoding.Ibm).Contains("FloatLittleEndian = true"));
    }

    /// <summary>The conflation V59 exists to retire, asserted directly: one capacity discipline, two byte
    /// representations. Any future attempt to derive the representation from the discipline fails here.</summary>
    [Fact]
    public void OneTruncationDiscipline_CarriesTwoRepresentations()
    {
        Assert.Equal(Pic(Usage.Display).Truncation, Pic(Usage.Binary).Truncation);
        Assert.NotEqual(Pic(Usage.Display).ByteForm, Pic(Usage.Binary).ByteForm);
    }
}
