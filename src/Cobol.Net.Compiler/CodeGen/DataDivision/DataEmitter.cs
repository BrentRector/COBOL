// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The DATA DIVISION emission facade (P7 Step 9l — was <c>Emit/FieldEmitter.cs</c>, split per
/// DESIGN-codegen-backend §2.5 into <see cref="RecordStructEmitter"/> · <see cref="GroupImageCodec"/> ·
/// <see cref="GroupValueSlicer"/> · <see cref="ValueInitializer"/> over the shared memoized
/// <see cref="PhysicalModel"/>): constructs and WIRES the five (they are mutually recursive by design —
/// a field's Init is a VALUE initializer, a Tier-B backing's seed is an image init, and both walk the
/// physical model), and forwards the public surface the per-unit emitters consume.</summary>
internal sealed class DataEmitter
{
    private readonly RecordStructEmitter _structs;
    private readonly GroupImageCodec _codec;
    private readonly ValueInitializer _values;

    public DataEmitter(EmitContext ctx)
    {
        var phys = new PhysicalModel(ctx);
        PhysicalModel.Observer.Value?.Invoke(phys);
        _values = new ValueInitializer(ctx);
        var slicer = new GroupValueSlicer(ctx, phys);
        _codec = new GroupImageCodec(ctx, phys, _values);
        phys.Values = _values;
        phys.Codec = _codec;
        _values.Slicer = slicer;
        _structs = new RecordStructEmitter(ctx, phys, _codec, _values);
    }

    /// <summary>Emit every WORKING-STORAGE / FILE-SECTION type, profile, index field, and root field.</summary>
    public void Emit() => _structs.Emit();

    /// <summary>See <see cref="RecordStructEmitter.RootDecl"/>.</summary>
    public (string Type, string Init) RootDecl(DataItem item) => _structs.RootDecl(item);

    /// <summary>See <see cref="RecordStructEmitter.MethodRedefinesBackingDecl"/>.</summary>
    public (string Name, string Init)? MethodRedefinesBackingDecl(DataItem root) => _structs.MethodRedefinesBackingDecl(root);

    /// <summary>See <see cref="GroupImageCodec.ImageInitOf"/>.</summary>
    public string ImageInitOf(DataItem item, bool useValues = true) => _codec.ImageInitOf(item, useValues);

    /// <summary>See <see cref="GroupImageCodec.CellDynSeeds"/>.</summary>
    public string CellDynSeeds(DataItem item, bool useValues = true) => _codec.CellDynSeeds(item, useValues);

    /// <summary>⛔ THE ONE STATEMENT OF WHAT AN AREA FORMAL BRINGS TO ITS AREA DECISION (kb/Work PB2094, PB2671), for the
    /// program ABI and the method ABI alike: its §8.5.1.12 atoms when it is a VARIABLE-LENGTH group
    /// (<c>CobolArgAdapt.Area</c> lays it over its argument's area only when the two have the same storage; "null" for
    /// every other area formal), and the factory of the fresh cell it takes whenever it is NOT laid over an argument's
    /// area — an omitted argument, an argument whose storage is not a cell, BY CONTENT / BY VALUE, and (through
    /// <c>CobolArgAdapt.Unbound</c>) the main-program entry that binds no argument. That cell is
    /// <see cref="UnboundFormalRecord"/>: the description's category-default RECORD image (a fresh area holds every
    /// dynamic-capacity table at its FROM capacity, §8.5.1.9.1, so a reference through it never meets a missing
    /// table). Before kb/Work PB2671 a fixed-shape area formal took a SPACE-filled cell, which an omitted national or
    /// numeric formal read as byte pairs and invalid data — the record image is the one composition its own codec
    /// reads.</summary>
    public (string Shape, string Fresh) AreaFormalShape(DataItem formal) =>
        (VarGroupWindow.Applies(formal) && VariableLengthCompatibility.GroupAtoms(formal) is { } atoms
                ? RuntimeApi.GroupAtomsNew(atoms) : "null",
            $"static () => new StorageCell {{ Ref = {UnboundFormalRecord(formal, formal.Class!.Width)} }}"
            + CellDynSeeds(formal, useValues: false));

    /// <summary>⛔ THE UNBOUND VALUE OF A LINKAGE FORMAL (kb/Work PB2671; docs/CONFORMANCE.md DOC-A.1-116): what a formal
    /// refers to when no activation bound an argument to it — the main-program entry <c>ICobolProgram.Activate</c>, or
    /// an omitted argument's storage. ISO §13.7.4 GR3 and GR5 leave the access and the initial value to the implementor
    /// when a non-COBOL element activates the program, and §13.18.63.4 GR3 lets a linkage VALUE take effect only during
    /// INITIALIZE, so it is the CATEGORY-DEFAULT value with no VALUE applied: spaces for a character item, the zero
    /// encoding for a numeric item of every usage, length zero for a dynamic-length item, a group's composed from its
    /// elementary items'. ⛔ ITS SHAPE FOLLOWS THE STORAGE, NEVER THE ITEM'S KIND: a CARRIER-resident elementary formal
    /// (<see cref="UnboundFormalCarrier"/>) holds its own field string — one UTF-16 character per national position —
    /// while every RECORD-shaped storage (a group carrier, and any AREA formal's cell, an addressed, REDEFINED or
    /// floating-point elementary one included) holds the record image (<see cref="ImageInitOf"/>, where a national
    /// position is two byte characters). Choosing by <c>IsElementary</c> gave an addressed <c>PIC N(2)</c> area a
    /// 2-character image in a 4-position cell, and its first reference died with EC-BOUND-PTR.</summary>
    public string UnboundFormalRecord(DataItem formal, int width) =>
        RuntimeApi.StrStore(ImageInitOf(formal, useValues: false), $"{width}");

    /// <summary>The unbound value of a CARRIER-resident formal's <c>ManagedPointer&lt;string&gt;</c> (see
    /// <see cref="UnboundFormalRecord"/>): an elementary carrier holds the item's own field string with no VALUE applied, which
    /// is the linkage item's category baseline (<see cref="ValueInitializer.InitializerFrom"/>); a group carrier holds
    /// its record image.</summary>
    public string UnboundFormalCarrier(DataItem formal, int width) =>
        formal.IsElementary ? _values.InitializerFrom(formal, effRaw: null) : UnboundFormalRecord(formal, width);

    /// <summary>See <see cref="ValueInitializer.InitializerFrom"/> — the §13.18.63 VALUE recipe over an operand
    /// the caller supplies (the report section's format-4 lane; kb/Work PB506).</summary>
    public string ValueImageOf(DataItem item, string raw) => _values.InitializerFrom(item, raw);

    /// <summary>The SEED image of one EXTERNAL record's run-unit cell — the expression handed to
    /// <c>ExternalStore.Cell(name, seed)</c>. ⛔ THE ONE COMPOSER, because there are TWO call sites that must
    /// agree: the backing property (<c>OoEmitter.EmitExternalBackings</c>) and the ADDRESS-OF cell reference
    /// (<c>PtrEmitter</c>). Whichever runs first CREATES the cell, so a divergence between them would make the
    /// record's initial content depend on statement order. A plain external item seeds with the category
    /// DEFAULT image (§13.18.63 GR4a — its VALUE takes effect only during INITIALIZE); a CONSTANT RECORD seeds
    /// with its §13.18.15.4 GR1 INITIALIZE-composed image (§11.9.10.4 GR7, the one external item initialized at
    /// initial state; the recipe is chosen at the root by <see cref="ValueInitializer.RecipeFor"/>, kb/Work PB1233).</summary>
    public string ExternalCellSeed(CallExternalBacking ext) =>
        RuntimeApi.StrStore(ImageInitOf(ext.Record, useValues: ext.Record.IsConstantRecord), $"{ext.Width}");
}
