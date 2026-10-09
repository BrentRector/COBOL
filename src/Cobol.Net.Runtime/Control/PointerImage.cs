// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE STORAGE-IMAGE CODEC FOR A CLASS-POINTER ITEM — data-pointer, program-pointer and function-pointer
/// (kb/Work PB970 arm 2; <c>docs/CONFORMANCE.md</c> §7 DOC-A.1-216). A pointer's VALUE is a managed reference
/// (<see cref="ManagedPointer"/> / <see cref="ProgramPointer"/> / <see cref="FunctionPointer"/>) and its STORAGE
/// is 8 character positions; this is the one place that says what those 8 positions hold, for every consumer
/// that must see the bytes rather than the reference — today the CALL boundary's BY CONTENT record (ISO §14.2.3
/// GR9 — "That argument is moved to this allocated record without conversion"; <c>CobolArgAdapt</c>) and the byte
/// view of a pointer MEMBER of a shared storage area (a BASED, EXTERNAL or ADDRESS-OF cell, or an ALLOCATEd one):
/// <c>CobolPtr.SlotWrite</c> keeps the member's positions equal to its slot's image, the cell seeds and ALLOCATE
/// start them at <see cref="NullImage"/>, and the one-way group image of a record struct renders it (kb/Work
/// PB1071). The slot holds the VALUE, the positions hold this image of it, and this codec is the only author.
/// <para>THE DETERMINATION (CLAUDE.md rule 1's ISO → GnuCOBOL → IBM/Micro Focus precedence; the standard leaves
/// size and representation to the implementor, §13.18.60.4 GR23/GR24/GR26): GnuCOBOL stores a pointer as the
/// machine address — an unsigned integer in the byte order of the platform's native binary integers, so a
/// pointer passed BY CONTENT to a BINARY-DOUBLE formal reads as its address — and the predefined NULL as the zero
/// address, which is also IBM's and Micro Focus's NULL. This implementation has no machine address to store, so
/// the image is an ADDRESS TOKEN in the same shape, in THIS implementation's binary byte order — big-endian, the
/// order DOC-A.1-205/207 pin for every BINARY and BINARY-DOUBLE item — so the GnuCOBOL property (the image read
/// as an 8-byte binary integer IS the address) holds here too:</para>
/// <list type="bullet">
/// <item>NULL, of every category → eight X"00" positions.</item>
/// <item>A data-pointer into a storage area → the area's BASE plus the pointer's displacement, as a signed 64-bit
/// integer in this implementation's binary order, big-endian. Each area is given its base the first time one of its addresses is imaged: the k-th
/// base is k × 2^32, so two areas' images never meet while a displacement stays inside ±2^31, and
/// <c>SET P UP BY n</c> moves the image by exactly n — the address arithmetic GnuCOBOL programs expect.</item>
/// <item>A program-pointer / function-pointer → a base drawn from the SAME allocator, one per (category,
/// case-folded externalized name) — so equal pointers (§8.8.4.2.16) have equal images and no image of one
/// category is ever an image of another.</item>
/// </list>
/// The token is stable for the run unit and never reused; it is not decodable back into a reference, and needs
/// not be — every conforming pairing that delivers a pointer's BYTES to a non-pointer item is a one-way copy
/// (§14.8.2.3.2 forbids the BY REFERENCE pairing; §14.8.2.3.3 1) admits BY CONTENT into a same-length item).
/// </summary>
public static class PointerImage
{
    /// <summary>The storage size of every class-pointer item — the 8 positions <c>FUNCTION BYTE-LENGTH</c>
    /// reports (DOC-A.1-210 / DOC-A.1-216).</summary>
    public const int Width = 8;

    /// <summary>The NULL image — eight X"00" positions, the zero address.</summary>
    public const string NullImage = "\0\0\0\0\0\0\0\0";

    private const int BaseShift = 32;
    private static long s_nextBase;   // k of the last base handed out; bases are k << 32
    private static readonly object s_lock = new();
    private static readonly ConditionalWeakTable<object, StrongBox<long>> s_areaBases = new();
    private static readonly Dictionary<string, long> s_nameBases = new(StringComparer.Ordinal);

    /// <summary>The storage image of a data-pointer VALUE (null or NULL → <see cref="NullImage"/>).</summary>
    public static string Of(ManagedPointer? p) => p switch
    {
        null or { IsNull: true } => NullImage,
        CellPointer c => Render(unchecked(AreaBase(c.Cell) + c.Offset)),
        { } other => Render(AreaBase(other)),   // an address with no area geometry: its own identity token
    };

    /// <summary>The storage image of a program-pointer VALUE.</summary>
    public static string Of(ProgramPointer p) => p.IsNull ? NullImage : Render(NameBase('P', p.Name!));

    /// <summary>The storage image of a function-pointer VALUE.</summary>
    public static string Of(FunctionPointer p) => p.IsNull ? NullImage : Render(NameBase('F', p.Name!));

    /// <summary>The storage image of a class-pointer CARRIER — the slot a pointer item's value rides — or null
    /// when <paramref name="carrier"/> is not a class-pointer slot (an object reference has no image here).</summary>
    public static string? OfCarrier(ManagedPointer carrier) => carrier switch
    {
        ManagedPointer<ManagedPointer> d => Of(d.Value),
        ManagedPointer<ProgramPointer> pp => Of(pp.Value),
        ManagedPointer<FunctionPointer> fp => Of(fp.Value),
        _ => null,
    };

    /// <summary>The storage image of the value a pointer MEMBER of a shared storage area keeps in its managed slot
    /// (kb/Work PB1071), or null when the member has none: a class-object member (an object reference has no image
    /// here, <see cref="OfCarrier"/>). <typeparamref name="T"/> is the member's own carrier type, because a null
    /// data-pointer and a null object reference are both a null reference and only the type says which.</summary>
    public static string? OfSlot<T>(T value) => value switch
    {
        ProgramPointer pp => Of(pp),
        FunctionPointer fp => Of(fp),
        ManagedPointer mp => Of(mp),
        _ when typeof(ManagedPointer).IsAssignableFrom(typeof(T)) => NullImage,   // a null data-pointer: the zero address
        _ => null,
    };

    private static long AreaBase(object area)
    {
        lock (s_lock)
            return s_areaBases.GetValue(area, static _ => new StrongBox<long>(++s_nextBase << BaseShift)).Value;
    }

    private static long NameBase(char category, string name)
    {
        // §8.3.2.2 — externalized names compare by the Annex C fold, the key ProgramTable's ExternalizedNames.Comparer
        // compares by (kb/Work PB1965): never the host's ToUpperInvariant, which merges what the fold keeps apart
        // (final sigma and sigma at 2023) and splits what it merges (the Kelvin sign and K).
        string key = category + CobolNames.UpperFold(name);
        lock (s_lock)
        {
            if (!s_nameBases.TryGetValue(key, out long b))
                s_nameBases[key] = b = ++s_nextBase << BaseShift;
            return b;
        }
    }

    /// <summary>Big-endian two's complement, one character per byte — this implementation's binary integer
    /// order (DOC-A.1-205/207), so the image read as a BINARY-DOUBLE is the address.</summary>
    private static string Render(long address) => string.Create(Width, address, static (span, a) =>
    {
        for (int i = 0; i < Width; i++) span[i] = StorageByte.ToChar((byte)(a >> (8 * (Width - 1 - i))));
    });
}
