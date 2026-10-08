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

    /// <summary>⛔ THE ONE STATEMENT OF WHAT A VARIABLE-LENGTH GROUP AREA FORMAL BRINGS TO ITS AREA DECISION (kb/Work PB2094),
    /// for the program ABI and the method ABI alike: its §8.5.1.12 atoms — <c>CobolArgAdapt.Area</c> lays it over its
    /// argument's area only when the two have the same storage — and the seeded cell its own description starts from
    /// when it is not laid over one (a fresh area holds every dynamic-capacity table at its FROM capacity, §8.5.1.9.1,
    /// so a reference through it never meets a missing table). §13.18.63.4 GR3: "In the linkage section, VALUE clauses
    /// take effect only during the execution of an explicit or implicit INITIALIZE statement", so the seed is the
    /// space-filled image with no VALUE applied. ("null",
    /// "null") for every other area formal.</summary>
    public (string Shape, string Fresh) AreaFormalShape(DataItem formal) =>
        VarGroupWindow.Applies(formal) && VariableLengthCompatibility.GroupAtoms(formal) is { } atoms
            ? RuntimeApi.AreaFormalShape(RuntimeApi.GroupAtomsNew(atoms),
                $"new StorageCell {{ Ref = new string(' ', {formal.Class!.Width}) }}{CellDynSeeds(formal, useValues: false)}")
            : RuntimeApi.AreaFormalShape(null, null);

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
