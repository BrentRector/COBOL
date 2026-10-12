## Entry 099 — 2026-03-17: IrLocation Complete — Multi-Dimensional OCCURS + Subscript Validation

Completed the full IrLocation migration and extended it to multi-dimensional OCCURS (1D/2D/3D),
all in one session. The architecture is now clean end-to-end: bound tree → lowering → IR → emitter.

**Architecture delivered**:
- `IrLocation` (abstract) → `IrStaticLocation` | `IrElementRef` replaces `StorageLocation` in
  ALL 30+ IR instruction types. Zero `StorageLocation` leakage into IR.
- `ResolveLocation(BoundIdentifierExpression)` — single gateway to storage. Constant-folds literal
  subscripts to `IrStaticLocation`, builds `IrElementRef` for variable subscripts. Handles 1D/2D/3D
  with precomputed row/plane multipliers.
- `ResolveLocation(DataSymbol)` — overload for non-subscriptable references (records, file status,
  INITIALIZE items, PERFORM VARYING index, condition parents).
- `EmitLocationArgs`/`EmitLocationArgsWithPic`/`EmitElementAddress` — three CilEmitter helpers
  used by every emit method. `EmitElementAddress` loops over dimensions generically.
- Zero direct `_semantic.GetStorageLocation` calls in the Binder outside `ResolveLocation`.

**Bound tree cleanup**:
Changed 9 bound statement types from `DataSymbol` to `BoundIdentifierExpression`:
`BoundArithmeticTarget`, `BoundAcceptStatement`, `BoundInspectStatement`,
`BoundInspectTallyingItem`, `BoundGoToStatement.DependingOn`, `BoundSetIndexStatement`,
`BoundReadStatement.Into`, `BoundMultiplyStatement.GivingTarget`,
`BoundDivideStatement.RemainderTarget`. Updated ~25 sites in BoundTreeBuilder to call
`BindIdentifierWithSubscripts` instead of `identifier().IDENTIFIER().GetText()`.

**Multi-dimensional OCCURS**:
- `IrElementRef` generalized: `IReadOnlyList<StorageLocation> SubscriptLocations` +
  `IReadOnlyList<int> Multipliers` instead of single subscript.
- Multiplier formula: `multiplier[i] = product of all inner dimension OCCURS counts × elementSize`.
  For 3D [X,Y,Z]: multipliers = [Y×Z×E, Z×E, E].
- Offset: `base + sum_i((sub_i - 1) × multiplier_i)`.
- Tests pass for 2D constant, 2D variable, 3D constant subscripts.

**Subscript validation diagnostics** (in `BindIdentifierWithSubscripts`):
- CS0850: subscripted non-OCCURS item
- CS0851: too many subscripts for OCCURS depth
- CS0852: exceeds 3 OCCURS levels (COBOL-85 limit)
- CS0853: exceeds 3 subscripts
- CS0854: too few subscripts for elementary item

**AI failure and recovery**: Attempted to propagate `IrLocation` by wrapping every
`_semantic.GetStorageLocation` call with `new IrStaticLocation(loc.Value)` at 40+ sites —
a transitional hack that violated `feedback_production_quality_always`. User caught it,
explained the correct layered approach (change bound types first, then lowering uses
`ResolveLocation`), and the wrapping was undone and replaced with proper architecture.
Lesson saved: `feedback_no_transitional_hacks.md`.

**Dead code removed**: `IrMoveToElement`, `IrMoveFromElement`, `IrDisplayElement`,
`IrLoadElementNumeric` — replaced by the general `IrLocation` mechanism.

119 unit tests, 101 integration tests, 1 skip. All green.

---

