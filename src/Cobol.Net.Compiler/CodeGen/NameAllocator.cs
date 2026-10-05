// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.CodeGen;

/// <summary>
/// The ONE unique-name allocator for emitted C# temporaries and labels (P7 Step 9a; DESIGN-codegen-backend §2.5
/// collaborator table). Exactly ONE instance per RUN UNIT — created by the run-unit emission entry and threaded
/// onto every per-unit <see cref="Emit.EmitContext"/> as <c>Names</c> — so minted names never collide across the
/// units, classes, and interfaces of one generated module (the counters were formerly 15 run-unit-lifetime
/// <c>CSharpEmitter</c> instance fields scattered over 9 partials). Each counter keeps its OWN sequence: the
/// per-counter <c>Next*()</c> methods reproduce the historical per-field sequences byte-exactly (the emitted-C#
/// characterization snapshots are the gate — a shared sequence would renumber every temp).
/// </summary>
internal sealed class NameAllocator
{
    private int _dep;         // GO TO … DEPENDING selectors (__dep)
    private int _sizeErr;     // ON SIZE ERROR flags (__sizeErr)
    private int _storeTmp;    // checked-store out-vars / snapshot temps (__ie/__q/__be/__sv; OO invoke stores)
    private int _read;        // READ out-image temporaries (__rd)
    private int _ec;          // EC locals (__ior/__sizeEc/…; shared by the core hook, the EC wrappers, and CALL)
    private int _loop;        // PERFORM TIMES loop locals (nested inline performs must not collide)
    private int _set;         // SET sender temporaries (__set/__cap)
    private int _search;      // SEARCH loop labels
    private int _call;        // CALL activation temporaries
    private int _inspectTmp;  // INSPECT image/count/magnitude locals (__ins…)
    private int _identTmp;    // item-identification hoists, shared by every verb (__ident…, PlaceIdentification.Hoister)
    private int _keyedSeq;    // keyed status/image temporaries (__kstN/__kimN)
    private int _ooInvoke;    // OO INVOKE temporaries
    private int _ptr;         // pointer temporaries (__ptrBy/__notAlloc)
    private int _sort;        // sort-family temporaries (__srt)
    private int _strUnstr;    // STRING/UNSTRING locals
    private int _vary;        // PERFORM VARYING index-range-check temporaries (__pv)

    public int NextDep() => _dep++;
    public int NextSizeErr() => _sizeErr++;
    public int NextStoreTmp() => _storeTmp++;
    public int NextRead() => _read++;
    public int NextEc() => _ec++;
    public int NextLoop() => _loop++;
    public int NextSet() => _set++;
    public int NextSearch() => _search++;
    public int NextCall() => _call++;
    public int NextInspectTmp() => _inspectTmp++;
    public int NextIdentTmp() => _identTmp++;
    public int NextKeyedSeq() => _keyedSeq++;
    public int NextOoInvoke() => _ooInvoke++;
    public int NextPtr() => _ptr++;
    public int NextSort() => _sort++;
    public int NextStrUnstr() => _strUnstr++;
    public int NextVary() => _vary++;
}
