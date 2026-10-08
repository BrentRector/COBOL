# WiseOwl COBOL — Data Model (records, tables, references) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the WiseOwl COBOL rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §3; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

DECISION-COMPLETE DESIGN: COBOL DATA DIVISION → typed-native C#, for WiseOwl COBOL.

== 0. THE CENTRAL IDEA: every reference is a Place ==
The whole subsystem is organized around ONE abstraction — a `Place` — that names a typed C# location and serves MOVE, arithmetic, file I/O, and CALL-by-reference identically. This replaces the legacy `(byte[],offset,length)` IrLocation. A Place has two emission methods used by every consumer (the RoslynBackend's rendering of the Place's backend-neutral structured resolution — see the §11 backend note / SSOT §18 #23):
  • `string ReadExpr()`   — a C# rvalue expression of the item's CLR type (e.g. `WsRec.Count`, `Tbl[i-1]`, `Name.Substring(s-1, l)`).
  • `string WriteStmt(string rhs)` — a C# statement that stores `rhs` into the place (e.g. `WsRec.Count = …;`, `Tbl[i-1] = …;`, a runtime ref-mod splice call).
Because both `record struct` members and array elements are addressable lvalues in C#, a Place composes by string concatenation of a *member access path*. The Place is built by ONE resolver, `ReferenceResolver.Resolve(DataReferenceContext) → Place`, that consumes the parse tree's base-word + flat suffix list once and is the single entry point for every operand.

This is the load-bearing decision: the legacy compiler had a byte offset model that every verb had to special-case; here every verb just calls `Read()`/`Write()` on a Place. Owner constraint satisfied: no byte[], no offsets — the .NET member path IS the storage.

== 1. THE SHAPE: groups → record struct, elementary → field, table → array ==
A 01/77 item becomes a STATIC FIELD (program data) or INSTANCE FIELD (OO object data) of a generated C# type. A group becomes a nested `record struct` TYPE (named `_T_<csname>`) declared at class scope; the group item itself is a field/member of that struct type. An elementary item is a member of the struct type whose C# type is its PicInfo.ClrType (string/long/decimal-free→long/Int128/float/double/bool). A fixed OCCURS n is an array member `T[]` of length n. Nested groups nest structs; an OCCURS group is `_T_Row[]`.

Example:
  01 WS-REC.            ->  record struct _T_WsRec { public string Name; public long Count;
     05 NAME PIC X(10).                                public _T_Item[] Items; }
     05 COUNT PIC 9(4). ->  record struct _T_Item { public long Val; }
     05 ITEMS OCCURS 3.     private static _T_WsRec WsRec = new _T_WsRec { Name=new string(' ',10), Count=0L, Items=…3… };
        10 VAL PIC 9(2).
Member access falls straight out: `VAL OF ITEMS(2) OF WS-REC` → `WsRec.Items[2-1].Val`.

== 2. THE HARD PART: parse-tree shape forces a two-phase resolver ==
The grammar (CobolParserCore.g4 §336-403) gives `dataReference: cobolWord dataReferenceSuffix*` where each suffix is subscriptPart | refModPart | qualification, AND subscript/ref-mod content is captured as a RAW token stream `subToken*` (SUBSCRIPT lexer mode) that the binder must re-parse — the grammar deliberately does NOT distinguish a subscript list `(I J)` from a ref-mod `(s:l)`; the presence of SUB_COLON decides. So `Resolve` runs two phases:
  Phase A (syntactic flatten): walk suffixes into an ordered list of {Qualifier names}, {Subscript token-groups}, {RefMod token-group}. Since kb/Work PB2113 (D10) the grammar parses both: a `subscriptPart` (`REF_LPAREN subscriptList? REF_RPAREN`, one `subscriptItem` per subscript, separated by §8.3.5's space or comma/semicolon-plus-space) or a `refModPart` (the colon decides). The subscript list becomes one token segment per item through `ReferenceResolver.SegmentsOf` **plus the declaration-informed `'('` rule**: a depth-0 `'('` after a name begins a NEW subscript only when no subscript may be written on that name, which is §8.4.2.3.3 SR2 and is asked through the one predicate `ReferenceResolver.CannotBeSubscripted` (`ResolveUnqualified(name) is { IsTableElement: false }`). All three callers — `InterpretSubscripts`, `SubscriptSegments` (the §14.9.37.3 SR8/SR9 SEARCH-ALL source-text reader) and `IntrinsicBinder`'s table(ALL) argument split — pass that predicate, because two of them once spelled their own copy of it and the copies disagreed with the rule (kb/Work PB136, PB877).
  Phase B (semantic resolve): resolve the base name + qualifiers to a DataItem via right-to-left narrowing, build the member-access Place, attach subscripts to the OCCURS levels (outer→inner), then wrap in a ref-mod Place if present.

== 3. QUALIFIED RESOLUTION (X OF Y OF Z), ISO §8.4.2.2 ==
Algorithm (port legacy ResolveQualifiedName): resolve the rightmost qualifier Z as a 01/standalone, then FindChild(Z,Y), then FindChild(ctxY, X) — successively LESS inclusive. FindChild searches the group subtree (recursive). A bare unqualified name resolves by: unique-in-program → that item; else require qualification (diagnose CBL-ambiguous if >1 candidate and no qualifier — ISO §8.4.2.2 rule: uniqueness must be established). The DataBinder.ByName index is a MULTIMAP (`Dictionary<string, List<DataItem>>` — IMPLEMENTED, `src/Cobol.Net.Compiler/Binding/DataBinder.cs`) because COBOL permits duplicate names disambiguated only by qualification. The Place records the full member path (Z.Y.X chain) so emission is `Z.Y.X`.

== 4. SUBSCRIPTING / INDEXING, ISO §8.4.2.3 ==
OCCURS dimensions are collected by walking item→ancestors (legacy LocationResolver does exactly this). COBOL subscripts are 1-BASED and listed OUTER→INNER (`T(outer, inner)`); C# arrays are 0-based — so each subscript emits as `[expr - 1]`. Multi-dim: COBOL-85 caps a table at 7 dimensions (the 3-dim cap was ANSI-74, out of scope); 2002+ removes the fixed cap — store dims as a list, no fixed cap; >7 dims at `--std 85` ⇒ diagnostic (G1, per-edition gating section). Each dimension is a SEPARATE C# array index because a 2-D OCCURS is an array-of-structs-containing-array (`Rows[i-1].Cols[j-1]`), NOT a flattened 1-D — this is the natural .NET shape and removes all the legacy multiplier/stepSize offset arithmetic.
  • Subscript forms: ISO §8.4.2.3.2's general format has exactly THREE alternatives — `ALL`, **arithmetic-expression-1**, and `index-name-1 [ {+|-} integer-1 ]` → `idx ± lit - 1`. The middle one is the wide one and it is NOT a closed list of shapes: §8.8.1.1 admits "an identifier referencing a numeric data item, a numeric literal, the figurative constant ZERO …, such identifiers, figurative constants, and literals separated by arithmetic operators", and §8.4.3.1.2 Format 2 makes that identifier a qualified-data-name-WITH-SUBSCRIPTS — so an integer literal, a plain or qualified data-name, `I + 1`, `(A + B) / 2`, `FUNCTION INTEGER(X)` and **a subscripted identifier such as `X(1)`** are all one alternative. The resolver's direct position walk (`ReferenceResolver.BindPosition`, kb/Work PB2151) reads the parse node into a typed `Position` for the shapes that map to an ordinal term by term and is never the arbiter of what is legal: anything else routes to the D18 materializer, which binds the same node through the expression binder; what did not parse as an `arithmeticExpression` is refused by name (D18 below; DESIGN-binder-bound-tree.md §3.9).
  • **Which names may carry a subscript, and how many — ISO §8.4.2.3.3 SR2 and SR3, and they are ONE predicate each, in `DataItem`.** SR2: the entry "shall contain an OCCURS clause **or shall be subordinate to** a data description entry that contains an OCCURS clause" = `DataItem.IsTableElement`. SR3: "the number of subscripts shall equal the number of OCCURS clauses in the description of the table element being referenced" = `DataItem.SubscriptArity`, with `SubscriptLevels()` the same walk as a list, outermost first (SR3's "order of successively less inclusive dimensions"). `ReferenceResolver.ScreenSubscriptArity` reports both by name (COBOLNET2096 / COBOLNET2097) at the ONE resolution site, so the rule reaches the SENDING and RECEIVING sides of a statement identically. It fires only on a reference that WRITES subscripts: SR5's seven exceptions all admit an OMITTED list (a SEARCH subject, a REDEFINES clause, an OCCURS KEY IS phrase, a SORT key or table subject, a screen entry's FROM/TO/USING phrase, a report SUM addend) and belong to the owning binders. ⛔ `DataItem.IsTable` is the answer to a DIFFERENT question — "does THIS entry carry an OCCURS clause", a table-RECOGNITION test — and using it where SR2 belongs is what made `Y (X(1))` a two-subscript reference (kb/Work PB877); `SubscriptAdmissionDriftTests` pins both the predicate and its single call site.
  • INDEX-NAMEs (INDEXED BY): an index-name is a DISTINCT entity, NOT a data item (ISO §8.4.2.3 / §13.18.38). DECISION: an index-name → a C# `long` field holding a 1-BASED OCCURRENCE NUMBER (not a byte displacement; the legacy byte-displacement model is rejected as it leaks layout). SET idx TO n → `idx = n`; SET idx UP/DOWN BY k → `idx ±= k`; using idx as a subscript → `[idx - 1]`. (Rationale: occurrence-number semantics are layout-free and make SEARCH/SEARCH ALL emit as plain integer loops; the only observable difference — idx surviving a redefine of the table element width — is implementor-defined and not in the conformance corpus.) Index-name lives in the same static/instance scope as its table.

== 5. REFERENCE MODIFICATION x(s:l), ISO §8.4.3.3 (general rules §8.4.3.3.4; spec lines 6952-7020) ==
A typed substring over the item's CHARACTER image. DECISION: ref-mod always operates on the STRING image of the item (ISO §8.4.3.3.4 rule 2: a non-alphanumeric DISPLAY item is treated as if redefined alphanumeric of the same size; rule 3: NATIONAL likewise). So:
  • Read: `CobolString.RefMod(<charImage>, s, l)` → `charImage.Substring(s-1, lengthOrRest)`; l omitted → to end (ISO §8.4.3.3.4 rule 5c: default length = remaining). `<charImage>` is the item's display image (string field directly; a numeric item via `CobolNum.FormatDisplay` first — but a numeric ref-mod is rare and the spec says treat-as-alphanumeric-redefinition, so we render the raw digit/zoned image).
  • Write (ref-mod as a RECEIVER, the genuinely hard case): cannot reassign a substring in place on an immutable C# string. Runtime helper `CobolString.SpliceInto(ref string field, int start1, int len, string value)` rebuilds the string: `field = field[..(s-1)] + value-fitted-to-len + field[(s-1+len)..]`. The Place's WriteStmt for a ref-mod emits this. For ref-mod over a numeric/COMP item used as a receiver, route through the byte-image fallback (G6, deferred) — flag loud meanwhile. s and l are arbitrary arithmetic expressions (evaluated once into temps to avoid double-eval; ISO requires single evaluation of subscripts/positions).
  • REF-MOD OVER A **VALUE** THAT HAS NO PLACE (ISO §8.4.3.3.3 SR2 — the result of a function-identifier; fix-queue PB8). `RefModPlace` decorates a `Place`, and a function result is not storage, so the value form attaches instead as `RefModSpec` on `BoundIntrinsicCall.RefMod` and renders through the SAME `CobolString.RefMod` — one slicer, so the §8.4.3.3.4 item-5c bounds check and EC-BOUND-REF-MOD are shared rather than re-implemented. It is READ-ONLY by construction (§8.4.3.2.3 SR1 makes a function-identifier a non-receiving operand), so there is no `SpliceInto` counterpart to write. A USER-DEFINED function's result is different and needs nothing new: it is already a real `Place` (the §8.4.3.2.4 GR1 caller temp cloned from the RETURNING item), so it goes through `RefModPlace` exactly as a data item does. **A RIDER ON THE CALL NODE, NOT A WRAPPER AROUND IT** — the alphanumeric string channel is selected by pattern-matching `BoundComputedOperand { Expr: BoundIntrinsicCall }` at several sites (`OperandText.AsString`, the nested-argument visitor, `IntrinsicArgumentRules`, `EcBinder`), and a wrapper node would have silently stopped matching at every one, whose failure mode is a DROPPED ref-mod rather than a compile error. §8.4.3.3.4 GR6 preserves class and category for the three categories SR2 admits, so the rider leaves `ResultCategory` correct with no extra rule.
  • ONE CARRIER, ONE READER. Every ref-mod is a parsed `refModPart` (kb/Work PB2113 retired the second carrier, a SUBSCRIPT-mode token group with a depth-0 colon); its opening paren is REF_LPAREN straight after a data name, FNARG_LPAREN after a zero-argument function name, LPAREN after a `)`. `ReferenceResolver.ReadRefMod` reads it into `RefModSpec`, so "how a ref-mod's start and length are read off the source" is written down once. Which paren a given shape gets is pinned by `CobolLexerModeDriftTests`, because the PB8 fix depends on it: a ref-mod after a FUNCTION name or after an argument list's `)` is no reference paren, and widening the reference trigger would silently break the `functionCall` ref-mod tail.
  • ZERO-LENGTH ref-mod (l=0) is EDITION-VARYING (`VERSION_CHANGE_REFERENCE.md` #30): pre-2023 the result is undefined; at `--std 2023` it is allowed (yields "") ONLY when the REF-MOD-ZERO-LENGTH directive (§7.3.23) is in effect — otherwise EC-BOUND-REF-MOD is raised; FLAG-14 flags the ambiguous case (spec line 4523). Gate the emit by edition + directive state.

== 6. NATIVE NUMERIC MODEL (owner-locked, reaffirm) ==
Fixed-point = native `long` holding the UNSCALED value; scale is compile-time metadata on PicInfo (already implemented). 19-38 digit pictures → `Int128` (PicInfo gains a `WidePrecision` flag selecting Int128 vs long for ClrType + the runtime overloads; CobolNum must gain Int128 overloads — currently long-only). COMP-1/2 → float/double; COMP-5 → native int by width with binary wrap (PicInfo.StorageWidth already computes the byte width; runtime needs the wrap path, deferred). decimal/BigInteger essentially unused. This is settled; the data-model design only needs to thread `WidePrecision` into ClrType, DefaultInitializer, ProfileInitializer, and the NumX scale-tracking expression type so wide items pick Int128 literals (`123` not `123L`).

== 7. LEVELS 66 (RENAMES) and 88 (condition-names) ==
  • 88 condition-name: NOT a storage item — a named boolean predicate over its parent (the conditional variable). DECISION: emit each 88 as a C# `static bool` PROPERTY (or a method) over the parent Place: `private static bool LvlOk => CobolCond.In(Parent.Read(), <value-or-range-set>);` where the value set comes from the (possibly multi-valued, THRU-ranged) VALUE clause. SET cond TO TRUE → assign the parent its first/low value (ISO §14.9.39.4 GR6). The binder captures 88 entries as `Condition88` records on their conditional variable (IMPLEMENTED — `DataBinder.Conditions` multimap); ensure the captured VALUE list covers THRU ranges + multiple literals.
  • 66 RENAMES: a re-grouping alias. Without THROUGH it forwards to data-name-2's place (§13.18.45.4 GR1); with THROUGH it is a `RenamesPlace` that concatenates on read, and splits on write, the STORAGE IMAGES of the parts that tile its window (GR2: "an alphanumeric group item"). Resolution is a post-build pass (`DataBinder`'s RENAMES resolution, `RenamesOwners()`).
    A THROUGH alias answers two DIFFERENT questions from two fields, and each rule asks the one it means: its STORAGE
    (`RenamesInfo.Span` — the record's storage BYTES tiled once each by whichever leaves cover them, a REDEFINES view's
    leaves included, a leaf under an OCCURS group once per occurrence of that group, `RenamesSpanPart.Outer`, kb/Work
    PB986; atoms, images, reads and writes) and
    its MEMBERSHIP (`RenamesInfo.IncludedElementaryItems` — §13.18.45.4 GR2's "all elementary items starting with
    data-name-2 … and concluding with data-name-3", in declaration order; §14.7.6's "a data item in D1" for MOVE
    CORRESPONDING, kb/Work PB966, determination D-CORR66 in docs/CONFORMANCE.md). A rule about WHICH ENTRY a
    reference names — "described with level-number 66", "shall not contain a RENAMES clause" — asks
    `Place.DenotedItem`, never `Place.Item`: the no-THROUGH alias resolves to data-name-2's own place.
    ⛔ A span part is a range of its cell's STORAGE BYTES, never of its character positions (kb/Work PB2466, PB1902).
    `ReferenceResolver.SpanLeafPlace` makes every cell its storage image — a numeric leaf through `NumericImagePlace`,
    a usage-national leaf through `NationalBytesPlace` (its UTF-16BE pairs, D-N1) — and a partial part is a
    `RefModPlace` over that image in bytes. So a REDEFINES view of another layout that puts a window boundary, or
    leaves an interior byte, on the odd byte of a national character is tiled and addressed like any other byte;
    COBOLNET1655 "do not tile" is left to a window no leaf reaches.

== 8. REDEFINES — the storage-overlay boundary ==
> **Canonical REDEFINES design.** The 4-tier one-canonical-backing model below is the design; the SSOT is
> `COBOLNET_DESIGN.md` §4 and the deep-dive `COBOLNET_REDEFINES_DESIGN.md`. A write through one view must be visible
> through every other view of the same storage — two independent typed fields cannot guarantee that, which is why a
> single stored canonical + computed accessors (not separate fields) is used.

REDEFINES makes two differently-typed views share storage. **Model (4 tiers; priority cascade D>C>B>A — see
`COBOLNET_REDEFINES_DESIGN.md`):** a "redefines class" (all entries over one storage area) has exactly ONE *stored*
backing — the **canonical** — and EVERY other view is a **computed accessor (a C# property/`Place`)** over it; never
two stored fields per area, so a write through any view is coherent across all.
  • **A — Alias** (identical PIC+USAGE / RENAMES-without-THRU): one typed field; other names are pass-through accessors.
  • **B — StringCanonical** (whole class is USAGE DISPLAY — the dominant case, and the ENTIRE near-term NIST path):
    canonical = ONE `string` of class-max width under the char==byte convention; each view is a typed
    `(offset,width)` accessor over it, holding the leaf's PINNED `NumericByteForm` bytes — zoned characters for
    DISPLAY, radix-2 for BINARY/COMP-5/BINARY-*, BCD for PACKED-DECIMAL, big-endian IEEE for the float family, the
    8-byte occurrence number for INDEX (V59 + kb/Work PB164 waves 1–2 + R40). Since the Step D arm-1 dissolution
    this covers every mixed-USAGE pun too.
  • ~~**C — ByteCanonical**~~ — **DELETED (Step D, kb/Work PB164).** No class-scoped `byte[]` canonical exists;
    the puns it was reserved for are ordinary Tier-B byte windows, so the owner fork at §18 #1 is moot.
  • **D — Reject loud** (object/pointer/strongly-typed/variable-length puns — already spec-illegal): a diagnostic.
The redefining view emits NO stored VALUE field (init only from the original, REDEFINES SR9); a numeric view still
emits its `_P_` NumProfile (an arithmetic target must compile). FILLER under a REDEFINES needs no field. Tier selection
(`DataBinder.ClassifyRedefinesClasses` → `ComputeTier`) runs a transitive-closure over each redefines class, verdicted
to the A⊑B⊑C⊑D lattice (join = max tier). The one-canonical-backing model keeps Tiers A/B 100% typed (no `byte[]`)
while staying coherent, and confines bytes to genuine mixed-USAGE puns only.

== 9. CLAUSES: SYNCHRONIZED / JUSTIFIED / BLANK WHEN ZERO ==
  • SYNCHRONIZED: alignment is a BYTE-LAYOUT concept. In a typed-native model there are no byte boundaries to align to (a `long` field is naturally aligned by the CLR). DECISION: SYNC is a NO-OP for in-memory typed data; it only matters at the file/byte serialization boundary (G6), where the record-image builder honors it. Capture IsSynchronized for that future use. (Rationale: the only observable effect of SYNC absent byte access is on REDEFINES-overlay size, which is already the G6 byte path.)
  • JUSTIFIED RIGHT: already threaded — CobolString.Store(value,width,justifiedRight) pads/truncates on the left. `DataItem.Justified` carries the flag; the Place's WriteStmt for an alphanumeric receiver passes it.
  • BLANK WHEN ZERO: a STORE-time rule (ISO §13.18.8.4 GR1) — "the content of the data item is set to all spaces when the item is a receiving operand and THE VALUE BEING STORED is zero". DECISION: a property of the item's rendering, applied in the ONE numeric-edited formatter (`CobolEdit.Render`, and `CobolLocaleEdit.Format` for PICTURE format 2), and applied to THE STORED VALUE — the aligned, truncated digit positions, NOT the sending value. §14.9.25.4 GR6 d) sends the store to §14.6.8 and §14.6.8.2 r4 transfers "with zero fill or truncation on either end as required", so `MOVE 0.4 TO PIC 9(3) BLANK WHEN ZERO` and `MOVE 100 TO PIC 99 BLANK WHEN ZERO` both store zero and both blank. ⛔ The guard therefore sits AFTER the mask rescale, never before it: a guard on the raw sending pair blanks for the arithmetic arms (whose emitters hand the formatter a value already at the resultant's scale) and not for MOVE, on identical items (kb/Work PB566). GR3 is the same rule read backwards — an all-spaces BLANK WHEN ZERO item that is a SENDER has the value zero whenever the object of the operation is a numeric or numeric-edited data item, which reaches the de-editing MOVE and the relation condition alike (kb/Work PB509). Capture BlankWhenZero on PicInfo.

== 10. VALUE INITIALIZATION incl. TABLES, ISO §13.18.63 / §14.9.4 ==
Default (no VALUE): alphanumeric→spaces, numeric→0 (unscaled), index→1, pointer→null. Already implemented for flat items. Extensions needed:
  • Group with VALUE: initialize every subordinate per the group literal spread across the group's char positions (rare; the common form is per-elementary VALUE). 
  • OCCURS with VALUE: ISO permits VALUE on a table item → every element initialized to that value (Format 1 GR9). Emit a collection-expression / array initializer: `Items = [.. Enumerable.Repeat(elemInit, n)]` or an explicit `new _T_Item[]{ init, init, … }`. The **Format 2 (table) VALUE** (ISO §13.18.63.2, COBOL-2002) is NOT a bare one-literal-per-element positional list — it REQUIRES a `FROM (subscript-1)` phrase (optional `TO (subscript-2)`) keying a literal list to occurrence RANGES with cyclic reuse (GR13), no-TO = fill to the maximum (GR14), later-FROM-wins on overlap (GR15), and the GR16 dynamic-capacity computation. Emit per-occurrence: a fixed table becomes `new _T_Item[]{ ElemInit(1), …, ElemInit(n) }` (default outside every range — §13.18.63.4 leaves those occurrences UNDEFINED, so this is the implementation default, not a spec guarantee), a dynamic table a `CobolDynTable` opened at the GR16 initial capacity with a per-occurrence seed. **ONE SUBSCRIPT-TUPLE → LITERAL map serves BOTH storage lanes**, and it is built in the BINDER: `DataBinder.ResolveTableValues` (`DataBinder.TableValue.cs`) validates §13.18.63.3 SR18–SR23 and resolves GR12–GR16 into a `TableValuePlan` on the entry — the OCCURS chain (`Dims`, most inclusive first, SR20's order) plus a `Subscripts → literal` map. `DataItem.ValueAt(subs)` is THE reader: it answers with the Format-1 `RawValue` (the same for every occurrence, GR9) or the Format-2 literal keyed to that tuple, so the THREE storage lanes cannot disagree about a table element's value — `ValueInitializer.InitializerFor` (the record-struct fields), `GroupImageCodec.ImageInitOf` (the character-image backings: Tier-B REDEFINES, EXTERNAL cells, BASED records, OO backings) and `GroupImageCodec.InitialBitCarrierOf` (the §8.5.1.6.3 packed BOOLEAN POSITIONS of a bit group). **Each of those two image lanes has independently dropped a format-2 VALUE**, by reading `RawValue` alone and repeating one occurrence: the character lane in kb/Work PB208, the bit carrier in PB505 (measured all-zero where the record-struct lane gave the seeded bits). `ValueFormat2Tests.AllStorageLanes_SeedTheSameOccurrences` is the drift test — it compiles each shape twice, once natively and once behind a REDEFINES alias that moves it onto its image lane, and requires the same answer (kb/Work PB208: the image lane used to read only `item.RawValue` — null for a table VALUE — and `StrRepeat` the VALUE-less default, discarding the seed silently for every image-stored leaf). **Both emit lanes thread an OCCURRENCE CONTEXT** — the subscripts of every OCCURS level entered on the way down from the record root — through `FieldInit` / `ComposedInit` / `PhysicalChildrenOf` / `ImageInitOf`, which is what lets the VALUE live on an entry SUBORDINATE to the OCCURS (SR18) and span several dimensions (GR12's odometer). The per-occurrence composition is taken ONLY where `DataItem.ContainsTableValue` says the subtree needs it; everywhere else one element initializer is composed and repeated, and `PhysicalModel`'s memo stands (the ~49-level CCVS nesting stays linear). **The literals are screened exactly as a format-1 literal is**: §13.18.63.3 SR2 is an ALL FORMATS rule and SR16 carries SRs 10–15 into format 2, so `DataBinder.ScreenTableValueLiterals` — at ENTRY BIND, beside the format-1 call site — puts each occurrence-literal through `DataBinder.ScreenValueLiteral`, the ONE funnel the format-1 `RawValue` takes, and stores what it returns, so the `--permissive` numeric rewrite reaches the emitter's per-occurrence override too. The GEOMETRY cannot be screened there: SR18/SR20/SR21/SR22/SR23 are written against the entry's OCCURS ANCESTORS and `DataItem.Parent` is assigned only after the entry binds — the post-forest pass is where they live (kb/Work PB505, which found ONE staged COBOLNET0899 standing in for all of them and refusing the conforming shapes alongside the violations). A GROUP entry's table VALUE is a group-level VALUE: SR13/SR14 through `CheckGroupValueDeclarations`, and §13.18.63.4 GR5's area deposit through `GroupValueSlicer.AreaOf`, which reads `ValueAt` and so deposits the area PER OCCURRENCE. Its LITERAL is screened in `CheckGroupValueDeclarations` too, against the GROUP as subject (SR13 sentence 1's category and SR4/SR5/SR10's group size sentences, through the same `ValidateValueCategory`) — that arm read only the format-1 carrier, which was harmless only while the group's table VALUE was discarded; the deposit and the screen landed together.
  • Figurative constants (ZERO/SPACE/HIGH-VALUE/LOW-VALUE/QUOTE/ALL "x"): map to the typed default of the right width — ZERO→0L or "0000", SPACE→new string(' ',w), ALL "AB"→repeat to width. Capture FigurativeInit + AllLiteralPattern (legacy already models these; port).
  The whole 01 field's initializer is a single C# object-initializer expression composed recursively from the leaves — emitted in the static field declaration (program) or the instance ctor (OO), one initializer per instance.

== 11. THE PLACE MODEL — concrete C# emission contract ==
`abstract record Place { abstract string Read(); abstract string Write(string rhs); PicInfo Pic; bool IsNumeric/IsAlpha; }`

> **Backend note (dual backend, SSOT §18 #23).** The backend-NEUTRAL content of a `Place` is its STRUCTURED
> resolution — the member-path segments, the bound subscript expressions, the ref-mod start/length bound
> expressions, the REDEFINES tier/backing, and the bound `DataItem`/`PicInfo`. The `Read()`/`Write()` C#-text
> methods are the RoslynBackend's RENDERING of that structure; the future CilBackend (behind `ICodeGenBackend`,
> `--backend roslyn|cil`) consumes the same structured `Place` and performs its OWN private lowering — it never
> sees the C# strings. Keep the structure primary and the string emission in the renderer; never let a bound
> node carry a pre-rendered C# fragment where the structured form is feasible (G4 discipline).
Concrete kinds:
  • MemberPlace(path)        Read=`path`            Write=`path = rhs;`           (qualified+nested+array indices folded into one access string)
  • RefModPlace(inner,s,l)   Read=`CobolString.RefMod(inner.Read(),s,l)`  Write=`{var t=inner.Read(); inner.Write(CobolString.SpliceInto(t,s,l,rhs));}`
    — its `Category` is THE ONE reader of the unique item's category (§8.4.3.3.4 GR6 via `CategoryOf` over a PICTURE; ALPHANUMERIC over a group).
    GR6's exception list is EXHAUSTIVE (edited → un-edited, numeric → alphanumeric/national) and GR2's "as if redefined as alphanumeric" governs the
    OPERATION's positions, not the result (kb/Work PB73, adjudicated 2026-08-18, reversing PB72's 2026-08-09 erasure): a BOOLEAN slice — bit-form or
    display-form — stays boolean, an ALPHABETIC slice stays alphabetic (`Table16Operand.Of(Place)` carries the rider); `--permissive` keeps GnuCOBOL's
    any-slice-is-alphanumeric reading as a warning.
  • NumericImagePlace(inner) — a numeric USAGE DISPLAY item viewed as its zoned character image (§8.4.3.3.4 GR2/GR6): Read=`FormatImage(inner)`, Write=decode back.
  • OdoGroupPlace(inner,…)  — wraps ANY group place whose subtree holds an occurs-depending table (§13.18.38 GR8): a record-struct member OR a Tier-B / BASED class-tier window
    (kb/Work PB80, 2026-08-18 — a BASED ODO record sent its maximum image; `ReferenceResolver.WrapIfOdoGroup` is the ONE wrap rule, `PlaceRenderer.GroupImage` the ONE image reader
    over both storage shapes: a struct's `AsImage()`, a window's `Read`, a wrapper's inner). LENGTH/BYTE-LENGTH's ODO term (`BoundOdoExtent.BasedAddress`) answers r4a's maximum for an
    UNASSOCIATED based entry through the runtime's null-pointer test (`RuntimeApi.PtrIsNull` — the null pointer is a singleton, never a C# null).
  • GroupImagePlace(inner)   — a GROUP viewed as its character image for reference modification (§8.4.3.3.3 SR1 bullets 3/9; kb/Work PB70, 2026-08-18):
    Read=`inner.AsImage()` (an occurs-depending group with data-name-1 outside: its CURRENT-count part, §13.18.38 GR8), Write=`PlaceRenderer.WriteGroupImage(inner, image)` — THE ONE
    group-image store every image-depositing verb calls (MOVE, INSPECT, STRING, UNSTRING, ACCEPT, the ref-mod splice): FromImage / the GR8a current-extent splice / the Tier-B window /
    the OCCURS DYNAMIC receiving accessor; a group with a float / COMP-5 / INDEX leaf is the loud Tier-C island. Its sibling `PlaceRenderer.WriteFullGroupImage` is the STORAGE-BOUNDARY
    store (a record area receiving a READ, a FILE STATUS group, CALL's copy-in/out — the WHOLE image, never GR8's current-extent slice; it unwraps an occurs-depending wrapper — kb/Work PB80:
    an FD record holding an ODO is an `OdoGroupPlace` over a class-tier window, and a `Read(record).FromImage(…)` spelled at an emitter was CS1061). No Place-based emitter spells `.FromImage(` itself (the codec generator and OoEmitter's fresh method-LINKAGE root are the record struct's own member).
    `ReferenceResolver.RefModExclusion` is THE §8.4.3.3.3 SR1 admissibility rule
    (COBOLNET1647 for a strongly-typed / variable-length group, a numeric item of a non-DISPLAY/NATIONAL usage, an edited or numeric leaf under a strongly-typed group, an index / pointer /
    object reference); `ExpressionBinder.ResolveReceiving` never drops a receiver silently — a DEFERRED resolver answer (kb/Work PB1030's `RefResolution`) is COBOLNET0899 recognized-not-implemented at the chokepoint, and its null always means reported. **Every verb binder states an operand's ROLE when it resolves it** (kb/Work PB881): a RECEIVING operand through `ExpressionBinder.ResolveReceiving`, every other reference through `ExpressionBinder.ResolveSending`; no binder under `Verbs/` calls `ctx.Refs.Resolve` directly (`ReceivingResolutionDriftTests`), because the receiving-operand prohibitions (§13.18.15.3 SR2 CONSTANT RECORD, §8.4.3.15.3 SR3, §8.4.3.14.3 SR2, §8.4.3.6.3 SR1, §13.10.4 GR1) are rules over the role, not over a statement — a binder that reached the resolver directly for INSPECT REPLACING, READ/RETURN INTO, a CORRESPONDING receiving group, SEARCH VARYING, INVOKE RETURNING/BY REFERENCE or a SET pointer receiver opted out of all of them.
  • Condition88Place(parent,valueset)  Read=`CobolCond.In(parent.Read(),…)`  Write(true)=set parent to value.
Every verb emitter (MOVE/ADD/COMPUTE/file READ INTO/WRITE FROM/CALL USING) takes Places and never touches layout — the unification the task demands. CALL BY REFERENCE passes the receiver Place's address: since a `record struct` member or array element is a C# variable, emit `ref` (e.g. `Sub(ref WsRec.Count)`) for BY REFERENCE; BY CONTENT copies the Read(). (A ref-mod or 88 receiver cannot be passed by ref → diagnose or pass by content per ISO.)

== 12. SUMMARY OF REQUIRED CHANGES TO EXISTING CODE ==
> **Note.** The list below is the DATA DIVISION design inventory; most of it is implemented in
> `src/Cobol.Net.Compiler` (`Place`/`ReferenceResolver`, nested record-struct emission, the `ByName` multimap,
> `Condition88` capture, `WidePrecision`/Int128). ⛔ **What is still OUTSTANDING is in `kb/Work/` — the ONE work
> register (`python scripts/spec/work.py next`) — not in this section and NOT in plan §0**, which carries live
> state and never a worklist (CLAUDE.md rule 8).
DataItem: add IsJustifiedRight, IsSynchronized, BlankWhenZero, RedefinesName/Redefines, RenamesInfo, OccursInfo (min/max/dependingOn/indexNames/keys), IsGroup-aware ClrType using `_T_` struct names + array `[]`, Level-88 ValueSet, FigurativeInit/AllLiteralPattern, WidePrecision. DataBinder: stop skipping 66/88; capture all clauses; build a name MULTIMAP; collect OCCURS dims + INDEXED BY index-names as their own entities; deferred-resolution pass for REDEFINES/RENAMES/DEPENDING-ON targets. PicInfo: WidePrecision (Int128), BlankWhenZero, IsJustifiedRight; ParseUsage already covers usages. New `ReferenceResolver`(→Place) + the suffix flattener (port legacy SUB_* interpretation). Emitter (`CodeGen/DataDivision/{DataEmitter,RecordStructEmitter}`): emit nested record-struct TYPES + composed initializers rather than flattening groups to leaves; route every operand through Place. Runtime: CobolString.RefMod/SpliceInto, CobolCond.In, Int128 overloads of CobolNum, COMP-5 wrap.

## Decisions

### D1. A single `Place` abstraction (Read()/Write() emitting C# rvalue/lvalue text) is the universal lvalue model, built by ONE ReferenceResolver and consumed identically by MOVE, arithmetic, files, and CALL.

**Rationale.** The task explicitly requires one lvalue model that serves all consumers identically; record-struct members and array elements are genuine C# lvalues so a member-access path composes by string concatenation, eliminating the per-verb special-casing the legacy byte-offset model forced.

**Rejected alternatives.** (a) Keep the legacy IrLocation (Area,Offset,Length) — rejected: it IS the byte substrate the owner abolished. (b) Emit a runtime accessor object per reference — rejected: defeats idiomatic/readable C# and adds indirection the JIT must undo.

### D2. Multi-dimensional OCCURS maps to array-of-struct-containing-array (Rows[i-1].Cols[j-1]), one C# index per COBOL subscript — NOT a flattened 1-D buffer with stepSize multipliers.

**Rationale.** This is the natural .NET shape, layout-free, and removes all offset arithmetic; subscript emission is a uniform [expr-1] per dimension.

**Rejected alternatives.** Flattened 1-D long[] with computed multipliers (the legacy ComputeMultipliers/stepSize model) — rejected: reintroduces byte-offset arithmetic and is unreadable.

### D3. Index-names (INDEXED BY) are C# `long` holding 1-based occurrence numbers, not byte displacements.

**Rationale.** Occurrence-number semantics are layout-independent, make SET / SEARCH emit as plain integer ops, and produce readable code; the displacement model leaks table-element width into a program-visible value.

**Rejected alternatives.** Legacy byte-displacement index — rejected: layout leak, and undefined under element-width redefines (not in the corpus).

**⛔ THE CARRIER IS THE IMPLEMENTOR'S INDEX RANGE, AND THE RANGE IS ENFORCED IN ONE PLACE** (kb/Work PB459).
§13.18.38.4 GR2 makes "the rules for the range of values allowed in the index defined by index-name-1"
implementor-defined and requires them to be DOCUMENTED (A.1 item 128) — so this decision, `long`, IS the
determination: the range is the full signed 64-bit interval, written down as `docs/CONFORMANCE.md` §7 row
`DOC-A.1-128` and as `CobolIndex.MinIndex`/`MaxIndex`. GR2 also names the three statements that may create a
value for an index — PERFORM VARYING, SEARCH, SET — and makes a value outside the range EC-RANGE-INDEX. All
three funnel through `Cobol.Net.Runtime/Values/Tables/CobolIndex.cs`, which is the ONLY place the guard is
written: `TryAmount`/`TryAmountReal` land a SET-family AMOUNT (§14.9.39.4 GR2 a) 1., GR3, GR29 — the integrality
and sign tests, which must run BEFORE the value reaches the `long`), and `Augment` forms a SET / PERFORM VARYING
augment in `Int128` (GR4 a) — so the 64-bit boundary is a value the runtime can see rather than a wrap it cannot.
A SEARCH's own scan cannot reach the boundary: §14.9.37.4 GR4's initial-index guard bounds it by the table's
occurrence count.

⛔ **The range is NOT the table's bounds.** §14.9.39.4 GR2 a) 1. c and GR4 b) each end "even if that occurrence
is not a valid occurrence within this table", so an index legitimately points outside its own table and a
table-bounds test here would reject conforming programs. `SetIndexAmountLandingDriftTests` keeps both halves
true: every SET-family amount site lands, and no emitter narrows an amount with a bare `(long)` cast first.

### D4. Level-88 condition-names emit as C# bool properties over the parent Place; level-66 RENAMES emit as alias properties (overlapping-byte RENAMES deferred to G6).

**Rationale.** An 88 is a predicate, not storage; a property is the idiomatic, zero-storage encoding and SET TO TRUE just assigns the parent. RENAMES of single/whole items composes from member images; only the overlapping-byte case needs the byte fallback.

**Rejected alternatives.** Materializing 88s as stored booleans kept in sync on every parent write — rejected: redundant state, sync bugs, non-idiomatic.

**⛔ WHICH ENTRY A CONDITION-NAME MAY BE ASSOCIATED WITH IS A RULE, AND IT IS ITS OWN PASS** (kb/Work PB488). ISO §13.16.3 SR24 carries two obligations beyond the VALUE decoding above: the condition-name entries "shall immediately follow the entry describing the item with which the condition-name is associated", and the associated entry may not be one of EIGHT lettered shapes (another level 88; a level 66; an alphanumeric group containing items of a usage other than display; a group containing items described with JUSTIFIED or SYNCHRONIZED; a data item of class index, message-tag, object or pointer; an ANY LENGTH item; a STRONG type declaration or a group subordinate to one; a variable-length group). None of it had a site: `BindCondition` hung the `Condition88` off whatever item the caller passed and the caller passed `stack.Peek()` unconditionally. TWO of the failures were worse than permissive — a level-66 alias never enters the level stack, so `66 R1 RENAMES A THRU B.` followed by `88 COND-R VALUE "CD".` bound COND-R to the PRECEDING `05 B` and answered about it; and an 88 with an empty stack was dropped silently and surfaced as a RUN-TIME `NotImplementedCobolFeatureException`. **THREE STRUCTURAL DECISIONS.** (1) `BindEntries` tracks `lastDescribed` — SR24's own subject, the entry the 88 IMMEDIATELY FOLLOWS — instead of the level stack's top; the two coincide for every entry that enters the hierarchy and diverge exactly for the entries that do not (a 66 alias, a §13.10 constant entry, and the start of a section), which is where the wrong answers were. `BindRenames` returns its alias for that reason. (2) The exclusions are ONE TABLE — `DataBinder.Sr24Exclusions`, one row per letter in the standard's order carrying the standard's words — not eight scattered tests, so a ninth exclusion is a row; `ConditionalVariableExclusion` returns the first matching row and the diagnostic quotes it. (3) It is a POST-BIND PASS (`CheckConditionNameAssociations`, `DataBinder.ConditionName.cs`, registered in `BindPipeline` at `PassPhase.UsageResolved`) and cannot be a test inside `BindCondition`: four of the eight exclusions ask about the conditional variable's SUBORDINATES, and a condition-name on a group is written BEFORE them, so `parent.Children` is still empty at bind time. The association is RECORDED where source order is known and ADJUDICATED once the forest is complete — which is also what carries the two shapes `ConformanceForest` cannot reach, a level-66 alias and an association with no item at all. Exclusion e) resolves its class question through §13.18.60.3 SR4's own predicate (`Sr4PhraseOf`), because SR4's six USAGE phrases produce exactly SR24 e)'s four classes and §13.18.60.3 SR11 states the same prohibition in the other direction; g) shares `UnderStrongTypeDeclaration` with §13.18.60.3 SR14, whose second arm is the same words; h) delegates to `ReferenceResolver.HasVariableLengthSubordinate`, which IS §8.5.1.12.1's definition — a group with a dynamic-length elementary item or a dynamic-capacity table subordinate, **not** an occurs-depending-on group. Diagnostics **1976** (an excluded conditional variable, naming the letter) and **1977** (no conditional variable at all). UNGATED across the four editions: COBOL-85's own condition-name rule states the same exclusions for every shape COBOL-85 can write, and the later letters name constructs their own introduction gates already refuse. Goldens: 20 negatives `negative/pb488-*` (one per arm, each verified to carry its diagnostic at every edition it names) and the over-rejection positives `2023/pb488_condition_name_associations_ok` + `85/pb488_condition_name_associations_ok_85`; `ConditionNameAssociationDriftTests` holds the table complete and pins that widening h) to ODO, or c) to non-alphanumeric groups, fails.

**⛔ WHAT A CONSTANT RECORD'S SUBTREE MAY NOT CONTAIN IS A POSITIONAL RULE, ASKED OF THE COMPOSED FOREST** (kb/Work PB1262, PB487).
§13.16.3 SR13 (the excluded clauses), §13.18.40.3 SR32 (no format 2 PICTURE) and §13.18.38.3 SR19 / SR23 / SR33 (no
DEPENDING phrase, no dynamic-capacity table) all say "any data item subordinate to" a CONSTANT RECORD. They are one
`BindPipeline` pass, `CheckConstantRecordSubtrees` (`DataBinder.ConstantRecord.cs`), run right after `ExpandTypes` over
`CompositionForest()`: a rule about an item's position is a property of the COMPOSED entry, so a `TYPE` / `SAME AS` clone is asked
exactly like a written entry. They were attach-time screens in `BindEntries`, which see written entries only, so the clone arm was
unscreened for every one of them. The same-entry half of SR13 stays in `BindEntry` (it reads the written clause set).

**⛔ THE REDEFINES CLAUSE'S ENTRY-LEVEL RULES ARE ASKED OF THE RESOLVED PAIR, IN ONE FILE** (kb/Work PB1280;
`DataBinder.RedefinesEntry.cs`). Data-name-2 is selected as the original definition: the nearest same-named
preceding entry that is not itself a redefinition (§13.18.44.3 SR7 + NOTE 1, §8.4.2.2.1 rule 2), among the subject's
siblings, and at top level among the roots of the subject's own section (SR10). `ScreenLeadingClausePosition` (asked in
`BindEntry`, where the written clause list is: SR1 "immediately follows" is "is the first clause"; it lives in
`DataBinder.ClausePlacement.cs` because §13.16.3 SR4 fixes the TYPEDEF clause's position in the same words, kb/Work
PB486) and
`ScreenRedefinesEntry` (asked once per resolved clause: SR2 identical level-numbers, SR3 no REDEFINES in a level-1
file-section entry, SR8 the size screen in BITS with its level-1-without-EXTERNAL exemption, SR10 no entry that defines new
storage between data-name-2 and the subject — every entry of the resolution scope between the pair must itself be a
redefinition, kb/Work PB2518 — SR13 no CONSTANT RECORD
target, SR15 alignment — a subject that needs a byte boundary over a data-name-2 that begins mid-byte) are COBOLNET2739
(SR8 keeps COBOLNET1539). The storage classification (`ClassifyRedefinesClasses`) runs after them and may assume them.
`ResolveRedefines` walks the COMPOSED forest (`CompositionForest`: roots, `TYPE` / `SAME AS` clones and the `TYPEDEF`
templates, which are off `Roots`) so every redefiner has its target, but it screens only the entries the programmer WROTE
(`ClonedFrom` null): a redefiner inside a type declaration is diagnosed once, at the declaration, used or not (kb/Work PB2518).

**⛔ THE RENAMES CLAUSE'S ENTRY-LEVEL RULES ARE THE SAME SHAPE** (kb/Work PB1283; `DataBinder.RenamesEntry.cs`,
COBOLNET2740). The operands are found by name in the owner's subtree, which includes the owner, so each resolved operand is
ASKED what it is: `ScreenRenamesOperand` (§13.18.45.3 SR5 level 1 / 77 / the owner, SR3 the operand's own and its ANCESTORS'
OCCURS — never the items between the endpoints, which may legally hold a table — and SR6 CONSTANT RECORD) and
`ScreenRenamesWindowOrder` (SR11: data-name-3 neither begins before data-name-2 nor ends at or before its end, in bits for
a record holding a bit item). SR2 (the entries immediately follow their record's last entry) is asked in the `BindEntries` walk, the
one place the entry order exists, and a level-66 entry binds to the record that walk last opened. A TYPEDEF-cloned alias goes
through the same loop. A data-name-3 that is a shorter redefinition of an EARLIER item is refused (SR11), which is why the
older "ends at data-name-3's last character" acceptance was removed from the CONFORMANCE.md PB96 determination.

**⛔ RECORDS BIND IN DEPENDENCY ORDER AND STAND IN SOURCE ORDER; CONSTANT ENTRIES ARE NOT ORDERED** (kb/Work PB1231).
No clause of §13.10 or §8.4 makes a reference follow the constant entry it names, and §13.10.3 SR4/SR5 forbid only a
length or value that depends "directly or indirectly" on constant-name-1 — a rule that could not be broken if every
reference had to follow its entry. So `DataBinder.DeclareDataEntries` collects every constant entry (both hosts, the
data description entry and the REPORT SECTION's `constantEntry`) and every data description entry's place in its
section run BEFORE anything binds; `FindConstant` is the one lookup and binds a declared entry on demand; a re-entry
while it evaluates is the reported cycle (`ReportConstantCycle`, SR4 when a length phrase is on it, else SR5). A
length phrase whose operand is described later waits when reached in order; when its value is DEMANDED, the operand's
whole record (its 01/77 entry up to the next 01/77/constant entry, 66 and 88 entries included) is bound then through
the same `BindEntries` walk with `outOfOrder: true` and parked, and the section's own walk places the parked roots at
their source position (`DataBinder.EntryOrder.cs`), so `Roots`, a file's record list and every post-build pass see
source order. The SR4 cycle is an item whose own description, or an ancestor group's, is open where the constant is
referenced (`IsBeingDescribed`, `IsDescriptionOpen`). An operand described later INSIDE the record that is still open
binds ahead at the granularity of its own ENTRY (kb/Work PB1941, `BindLaterEntriesOfOpenRecord`): the entry with its
subordinate run, and each group entry between it and its nearest already-bound ancestor ALONE, each LINKED to its
parent at once (so qualification, the OCCURS depth and the ancestor screens see the real chain) but joined to the
parent's members only when the walk reaches it (`TakePreboundEntry` → the walk's one `AttachToHierarchy` tail), so
member order stays source order; a group bound alone is completed — its remaining subordinates bound under it — before
anything measures it (`CompletePreboundGroup`). An operand SUBORDINATE to the very entry whose description references
the constant has no bound ancestor to link to (that entry's item does not exist yet) and stays recognized-not-implemented
COBOLNET0899 `constant-length-operand-bound-later`.

**⛔ EVERY `integer-n` POSITION IS A LITERAL POSITION A CONSTANT-NAME STANDS IN, AND THE GRAMMAR SAYS SO ONCE** (kb/Work
PB1947 report writer, PB1948 the rest). §5.5 1) calls each `integer-n` "a fixed-point integer literal" and §13.10.3 SR2
lets a constant-name stand "anywhere that a format specifies a literal of the class and category of constant-name-1",
so a clause that prints an `integer-n` writes `integerOperand : integerLiteral | cobolWord` (`CobolExpressions.g4`),
never the bare `integerLiteral`. `IntegerOperandRules.LiteralOnlySlots` is the short list of rules that may still
spell the bare one, each with the argument (an '85 element deleted at 2002, the edition constants first exist; a vendor
extension; the declined SCREEN SECTION; a "data-name-1 or integer-1" format whose constant-name parses as the data-name
arm), and `IntegerOperandSlotDriftTests` derives the rules that DO spell it from the generated parser and holds them to
that list both ways, so the clause added next is either an `integerOperand` or argued. `DataBinder.IntegerOperandValue`
is the one reader (`IntegerOperandText` for the full-width DYNAMIC LENGTH LIMIT, which has no host limit): the literal as
written, or the constant's integer asked what the written literal is asked — §5.5 1)'s unsigned and nonzero default with
the owning clause's zero permission (`IntegerOperandRules.Slots`, by OWNER, never by the carrier rule), the host limit
(COBOLNET2427) and, for a constant that is not an integer, COBOLNET1547. Two positions need more than the grammar rule:
LINAGE prints "data-name-1 or integer-1", so a constant-name parses as the data-name and `BindLinageClause` asks
`ConstantOf` first (`ConstantOperandValue`, the same reader); and SYMBOLIC CHARACTERS writes its names and its ordinals
as WORDS side by side, so `symbolicCharacterEntry` has three ordered alternatives — literal ordinals first (a program
without constants parses exactly as it always did), `IS|ARE` then `integerOperand`s, and words only; whichever parsed,
`DataBinder.SymbolicEntryGroups` re-reads the entry's tokens in source order and calls each word a name or an ordinal by
whether the unit defines it as a constant, because only the constant table tells them apart. BLOCK CONTAINS models nothing, so `BindBlockContainsClause` only reads its
integers (a refused constant is the program's error whether or not the clause changes the I-O). Goldens
`2002/pb1948_integer_n_constants`, `2002/pb1948_locale_size_constant`, `2014/pb1948_dynamic_length_limit_constant`
and `negative/pb1948-*`.

**⛔ THE SPECIAL-NAMES LITERAL POSITIONS ADMIT A CONSTANT-NAME THROUGH ONE RULE AND ONE READER** (kb/Work PB1942). The
paragraph's literal-1 … literal-9 (§12.3.7.2) are literal positions too, and the constant entry stands LATER, in the DATA
DIVISION. CLASS literal-5/-6, CURRENCY SIGN literal-7/-8 and ORDER TABLE literal-9 are `specialNamesLiteral : literal |
cobolWord` (`CobolSpecialNames.g4`); the ALPHABET literal phrase and the LOCALE clause already admitted a word with a
meaning of its own (code-name, figurative synonym, external-locale-name-1) and keep their shape. The CLASS value list's
LATER operands are steered by `classOperandAhead()` (`CobolParserCoreBase`): a word continues the list only when the unit
declares it a constant-name, read off the token stream (`{1|01} word CONSTANT`, never `CONSTANT RECORD`), so a switch or
device entry written after the clause without a period keeps its reading. In the binder `DataBinder.SpecialNamesConstant`
is the one constant reading of such a word (it binds the later entry on demand through `FindConstant`, kb/Work PB1231),
and each clause holds the constant's literal-1 to the rules of the literal it stands for (§13.10.4 GR1): `CurrencyOperandOf`
(SR18/SR26/SR19 from the specification as written, SR28's class from the category), `TryClauseTextLiteral` (SR10/SR11)
and `LiteralPhraseOperand` (an integer constant is an ORDINAL, SR14 b1 / SR17 b2). A word the unit declares as a
constant-name is that constant, never an ALPHABET code-name or an external-locale-name of the same spelling
(docs/CONFORMANCE.md §7 items 181, 184). Goldens `2002/pb1942_special_names_constant_literals`,
`negative/pb1942-currency-numeric-constant`.

**⛔ A CONDITION-NAME IS A THIRD KIND OF VALUE-CLAUSE SUBJECT, and it indicates NO SIZE** (kb/Work PB598). `DataBinder.BindCondition` screens a Format-3 VALUE literal through the ONE screen the item VALUE uses (`ValidateValueCategory` — §13.18.63.3 SR4/SR5 are ALL FORMATS rules and SR24, "Syntax rules 10 and 17 above apply", carries SR10 in whole), and the screen takes the subject as a `ValueSubject` descriptor (`Binding/ValueSubject.cs`) rather than a bare size, because WHICH SUBJECT it is decides which SENTENCES of those rules bind. Each of SR4/SR5/SR10 is a sentence pair: a CLASS sentence over "the item" / "the subject of the entry", which reaches a condition-name; and a SIZE sentence naming only "an elementary item" (bounded by "the size indicated by an **explicit** PICTURE clause") and a group item. A Format-3 entry is `88 condition-name-1 value-clause .` (§13.16.2), which admits no PICTURE clause; SR33 makes that level-88 entry the subject of the entry; §8.5.1.3.2 item 3 gives a condition-name entry "no true concept of level", so it is neither of §8.5.1.3.1's record subdivisions; and §13.18.63.4 GR19 gives it its conditional variable's characteristics only **implicitly** — which is what "explicit" excludes. So `ValueSubject.ForConditionName()` carries no size, and the three level-88 call sites (the THROUGH-range pair and the singleton leg) pass it; passing the conditional variable's size instead made `01 XV PIC X. 88 XC VALUE "cd".` COBOLNET1740, a rejection of legal source and a GnuCOBOL differential flip. The standard defines what the oversize literal MEANS, which a size rule would make dead text: §8.8.4.5.3 item 2 sends the comparison to the relation-condition rules, so it never compares equal (a permanently-false condition), and §14.9.39.4 GR6 → §13.18.63.4 GR7 → §14.6.8.5 places it "with space fill or truncation to the right". Goldens `85/pb598_condition_name_value_size`, `2002/pb598_condition_name_value_size_national_bit`, and `ConditionNameValueSizeTests` for the edition axis and the drift proof that the two subjects the sentences DO name are still measured.

**⛔ WHICH VALUE FORMAT MAY APPEAR WHERE — TWO SETS, NOT TWO LISTS OF CONDITIONS** (kb/Work PB556, PB557).
Both rules are stated by the standard over a CLASS of things, and both had been written as a couple of the
members. (a) §13.18.63.3 SR33 — "*Formats 3 and 5 may be specified only when the level-number of the subject of
the entry is 88*". The grammar admits every VALUE format through ONE rule on purpose (formats 3 and 5 share
their literal / THROUGH list), so this is a bind rule: `DataBinder.ConditionOnlyPhraseOf` is the SET of
format-3/5-only phrases — the THROUGH phrase, `IN alphabet-name-1`, `WHEN SET TO FALSE IS literal-4`,
`{IS INVALID | ARE VALID}` — and `BindEntry`, which binds only non-88 entries, reports **COBOLNET2167** and binds
nothing when one appears. Nothing screened it before: `VALUE 1 THRU 5` on a numeric level-01 entry reached the
code generator as the glued text `1THRU5` and failed the Roslyn compilation, and `VALUE "A" THRU "C"` on an
alphanumeric one compiled clean and stored that glued text truncated to the picture width. (b) §13.16.3 SR10 —
"*The VALUE clause shall not be specified for data items of class index, message-tag, object, or pointer*" —
of which §13.18.63.3 SR9 restates four by usage (FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE,
PROGRAM-POINTER). `UsageFamilies.AdmitsNoValueLiteral` is SR10's class set spelled as the usages §8.5.2.1 Table 2
files under those classes — INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, PROGRAM-POINTER —
beside `UsageFamilies.IsPictureless` and in the same shape, and it is read by TWO screens reporting ONE
**COBOLNET2168**: `BindEntry` for the entry's own USAGE clause and `UsageInheritanceElementary` for the usage an
elementary item acquires from its group (§13.18.60.4 GR1), with the cited rule from ONE sentence,
`UsageFamilies.NoValueClauseRule`. The set was first written from SR9 alone and covered two of its four usages
(kb/Work PB557: an OBJECT REFERENCE VALUE ran clean with the literal discarded, `VALUE NULL` failed the backend);
then it lacked USAGE INDEX, which SR9 does not name and SR10 does (kb/Work PB515: `77 I USAGE INDEX VALUE 7.`
compiled at every edition and seeded the index item). §13.18.63.4 GR4 initializes the object and pointer classes
to null with no clause written, so the prohibition costs nothing. `Cobol.Net.Tests.Unit.ValueClauseUsageSetDriftTests`
re-derives the set from SR10 and Table 2 (and SR9's usages) out of `specs/ISO_COBOL.md` on every run, because a
behavioural probe cannot contradict a wrong set.

**⛔ THE VALUE CLAUSE'S PLACEMENT RULES ARE ONE SCREEN** (`DataBinder.ValuePlacement`, kb/Work PB550 + PB551,
**COBOLNET2406**). §13.18.63.3 SR12 (format 1; SR16 carries it into format 2) bars a VALUE clause in an entry
that contains a REDEFINES clause or is subordinate to one; SR25 bars a format 3, 4 or 5 VALUE clause in or under
a CONSTANT RECORD entry. Both ask one question of the entry's ancestor chain, so both are asked at the moment the
chain exists — `BindEntries` right after an entry is attached (formats 1/2), `BindCondition` for a level-88
(format 3) — per WRITTEN entry, so a TYPE clone never re-reports. SR12 keys on the WRITTEN clause
(`RedefinesTargetName`), never the storage fact: the redefined ANCHOR keeps its VALUE, an FD's implicit
redefinitions and a SAME RECORD AREA record write no clause, and a level-88 under a REDEFINES entry is format 3,
which SR24 does not bring under SR12. SR25's format-4 leg is vacuous under today's grammar (a report group entry
admits no CONSTANT RECORD clause) and its format-5 leg belongs to the declined VALIDATE facility. Before the
screen both shapes compiled clean: a redefining entry's VALUE was silently discarded (a REDEFINES view never
seeds the storage it shares), and a condition-name under a CONSTANT RECORD was live.

**⛔ §13.18.63.3 SR2/SR3 ARE ALL-FORMATS RULES, AND ONE FUNNEL ASKS THEM FOR EVERY FORMAT** (kb/Work PB208,
PB586). `DataBinder.ScreenValueLiteral` is the ONE literal screen — SR2's class half (`ValidateValueCategory`),
its range half and SR3's sign (`ValidateNumericValue`, **COBOLNET1625**), SR6's edition gate — and every VALUE
literal of every format now takes it: format 1's item VALUE, format 2's per-occurrence literals, format 3's
literal-2, both ends of a THROUGH range and literal-4 (the conditional variable's picture as the subject,
§13.18.63.4 GR19), and format 4's report-section literals. Format 3 used to call the class half directly, so
`88 C-BIG VALUE 12345` on `PIC 9(2)` compiled at every edition; format 4 reached only SR6's edition gate, so
`COLUMN 1 PIC 9(2) VALUE 12345` PRINTED `45` and `PIC 9(2) VALUE "AB"` leaked Roslyn CS0103. The group-level
VALUE is the one direct caller of the class half left, because a group's category is never numeric (§8.5.2.1).

**⛔ AND §13.18.63.3 SR30 NEEDS NO SCREEN OF ITS OWN** (kb/Work PB558) — "*Condition-name and
content-validation formats shall not be specified in the report section*" is a CONSEQUENCE of two rules each
screened where it belongs: SR33 confines those formats to level 88, and §13.18.33.3 SR4 bounds report group
description entries to level-numbers 1 through 49 (**COBOLNET1746**, `LevelNumberPass`, over the whole parse
tree). The conjunction is exact, so a third screen would report the same source line twice. The
content-validation half is separately answered by the declined A.4.14 facility (COBOLNET1708).

**⛔ A VALUE OPERAND IS A LITERAL POSITION, AND ONE SCREEN SAYS SO FOR EVERY FORMAT** (kb/Work PB732). Every general format of the VALUE clause writes *literal-n* in every operand position (§13.18.63.2 — Format 1 `literal-1`, Format 2 `{literal-1}…`, Format 3 `literal-2 [THROUGH literal-3]` **and its second position `[WHEN SET TO FALSE IS literal-4]`**, Format 4 `{literal-1}…`, Format 5 `literal-5 [THROUGH literal-6]`; verified against the printed general-format figures, not only the transcription). The `valueClauseOperand : unaryExpression | nonNumericLiteral` grammar is a deliberate SUPERSET — a constant-name and a symbolic-character are spelled as ordinary user words, and only the symbol tables tell them from a data-name — so this is a BIND rule, never a grammar screen. `DataBinder.IsLiteralValueOperand` is that rule, PURE (no folding, no diagnostics) so a caller may ask about an operand it will not read: it admits a literal, a keyword figurative constant (§8.3.3.6.3 SR1 — "A figurative constant may be used whenever 'literal' appears in a format"), `ALL symbolic-character-1` whose word §8.3.3.6.3 SR4 requires to be a SYMBOLIC CHARACTERS name, a bare symbolic-character, a constant-name (§13.10.3 SR2), and a numeric literal under its written sign (§8.3.3.3.2 rule 2). `DataBinder.ReportNonLiteralValueOperand` is the ONE report, deduped per operand node: a BARE word draws **COBOLNET1639** (the position admits a constant-name or a symbolic-character and the word is neither, so it identifies no resource — §8.4.2.1), any other non-literal shape draws **COBOLNET1902** (nothing is undefined; the general format itself is violated — §13.18.63.2 / §4.2.2). `RawValueOperandText` asks the predicate FIRST and returns null on failure, so the compile fails with a named diagnostic and the emitter never sees the operand. **Every caller threads it** — `BindCondition`'s singleton and THROUGH legs, its `ScreenFalsePhraseOperand` (literal-4), `BindEntry`'s Format-1 funnel through `ExtractValue`, `BuildTableValueSpecs` (Format 2), and `DataBinder.Reports`' Format-4 entry — because the sink was the reader's last arm, an unconditional `op.GetText()`: an undefined word became an alphanumeric literal OF ITS OWN SPELLING on an alphanumeric subject (`01 C4 PIC X(7) VALUE NOSUCHW.` ran to exit 0 and printed `NOSUCHW`, a silent wrong answer) and reached Roslyn as a C# identifier on a numeric one (`CS0103`, exit 70). This is the DATA-DIVISION arm of the R30 "a word that names nothing" chokepoint: `ReferenceResolver.Resolve` closes the PROCEDURE DIVISION arm, and the VALUE clause never entered it because it resolves its own operands. The other three §13.10.3 SR2 literal-position seams — `IntegerOperandValue` (every integer-n position: the OCCURS bounds, the report-writer operands since kb/Work PB1947 and, since PB1948, RESERVE, BLOCK CONTAINS, RECORD CONTAINS, DYNAMIC LENGTH LIMIT, PICTURE LOCALE SIZE, LINAGE and the SYMBOLIC CHARACTERS ordinals), `ExpandPicConstants`, `ConstantValueRawText` — already reported an undefined word; VALUE was the only one that did not. **literal-4 also needed the GRAMMAR widened, and the widening and the screen are one change**: the `[WHEN SET TO FALSE IS literal-4]` phrase wrote its operand as the narrow `literal` rule, so the two word spellings a literal position admits died as a PARSE error — `88 CN VALUE 1 WHEN SET TO FALSE IS K` with K a constant-name gave COBOL0001 + COBOL0309 "A literal value is expected here, not a data-name", a rejection of conforming source under §13.10.3 SR2. The phrase is now its own grammar rule, `valueClauseFalsePhrase : WHEN? SET? TO? FALSE_ IS? valueClauseOperand`, so the binder names the node it screens instead of relying on "the only direct valueClauseOperand child", and the widened operand cannot re-open the sink. **⛔ AND THE SCREENED VALUE IS NOW BOUND, NOT DROPPED** (kb/Work PB555). It used to be screened and discarded, on the reasoning that `SET condition-name TO FALSE` declined loudly anyway "so a field would be a lookup nothing reads" — but the causality ran the other way: literal-4 having no field on `Condition88` is *why* §13.18.63.4 GR20 could not be implemented and *why* §13.18.63.3 SR27 had nothing to compare against, so one unmodelled operand held two rules shut at once. `Condition88.FalseValue` now carries it in the same raw-operand-text convention as `Values`, `BindFalsePhraseOperand` stores it, and the two rules follow (below). Negatives `negative/pb732-value-*` (one per arm, `reject-at` all four editions); positive twins `2023/pb732_value_word_substitutions` (whose `WHEN SET TO FALSE IS K-ZERO` / `IS SYM-A` legs fail to PARSE on the pre-fix compiler) and `85/pb732_value_symbolic_character` prove the legal word spellings still bind.

**⛔ LITERAL-4 IS FORMAT 3's THIRD OPERAND-BEARING CONSTITUENT, AND ITS TWO RULES ARE ONE BINDING** (kb/Work PB555). §13.18.63.2 Format 3 prints three operand positions after `VALUE`/`VALUES` — the `literal-2 [THROUGH literal-3]` group, `[ IN alphabet-name-1 ]`, and `[ WHEN SET TO FALSE IS literal-4 ]` — and `Condition88` now carries all three (`Values`, `Alphabet`, `FalseValue`). The two rules that had nowhere to live while literal-4 was dropped:

- **§13.18.63.3 SR27 — `DataBinder.CheckFalseValueDistinct`, COBOLNET2048.** "*The value of literal-4 shall not be equal to the value of any occurrence of literal-2. When the THROUGH phrase is specified: a) when literal-2 is of a class other than alphanumeric or national, the value of literal-4 shall not be equal to any value in the range of any occurrence of literal-2 through literal-3, inclusive. b) when literal-2 is of class alphanumeric or national, and the runtime collating sequence is known, …*" The first sentence carries **no class qualifier and no THROUGH precondition**, so it is tested against every occurrence of literal-2, singleton or range low bound; a) and b) then add RANGE MEMBERSHIP and differ only in the ordering that decides it — a) §14.7.8 rule 1's algebraic order, b) rule 2's collating sequence. b)'s "*any literal-2*" clause is the first sentence restated and is not tested twice. **b) is enforceable here because the sequence is a compile-time fact**: the `IN alphabet-name-1` alphabet when written, else the PROGRAM COLLATING SEQUENCE, and SR26's NOTE names the only unknowable arm ("*The runtime collating sequence is unknown when the collating sequence is defined by a locale or the collating sequence is otherwise determined at runtime*") — so a LOCALE alphabet drops the range half of the screen rather than guessing at it. The ordering itself is the **runtime's own** `CobolCollation.Compare`, reached through `CollatingTable.Collation(national)`: the compiler materializes the same carrier class `CollationEmit` renders into the generated program, so there is exactly one §12.3.7.4 GR7 k weighting and one §8.8.4.2.7 comparison in the product. An operand whose value is not known at compile time (a non-ZERO figurative, an `ALL` literal, a symbolic-character) is **skipped, not guessed**: SR27 speaks of "the value of literal-4", and inventing one would reject conforming source. **⛔ AND "THE VALUE OF" IS ONE READER, NOT A DECODER PER CLASS** (kb/Work PB920). The screen used to branch on `CobolLiteral.IsStringLiteral(literal-2)` and compare only SAME-KIND pairs, which is a rule about SPELLING where SR27 speaks of VALUE — and on a NUMERIC-EDITED subject one value has two spellings, so `01 X PIC ZZ9.99. 88 X-TEN VALUE " 10.00" WHEN SET TO FALSE IS 10.` compiled clean and `SET X-TEN TO FALSE` left the condition TRUE (measured: `X-STILL-TRUE`, `XF=[ 10.00]`). `DataBinder.ConditionValueOf` now answers what an operand's value IS over its conditional variable — the **SR6 edited image** on a numeric-edited subject, composed by the ONE composer `ValueInitializer.EditedImageOfNumericValue` ("*literals in formats 1, 2, and 4 of the VALUE clause may be numeric when they shall be converted to their numeric-edited forms according to the rules for the MOVE statement*"), and the literal's own numeric or character value otherwise — and `CompareConditionValues` orders a pair or answers *unknown*. §8.8.4.2.1's NOTE is the rule that makes the two spellings one value here: "*All comparisons involving numeric-edited data items are alphanumeric or national comparisons, including when the associated VALUE clause is a numeric literal*", with §8.8.4.5.3 GR2 making the condition-name test one of them. The composer takes `(dialectLevel, decimalPointIsComma)` rather than an `EmitContext` **so the binder can ask SR6's question at bind time without a second copy of it**; `ConditionValueRecipeDriftTests` names its three readers and pins that signature. Negatives `negative/pb920-false-value-crosses-edited-image` and its `-mirrored` twin (both arms — the fix does not branch on literal-2's class), positive `2023/pb920_false_value_cross_class_distinct`.

- **§13.18.63.3 SR31 — `DataBinder.BindCondition` + `DataBinder.Switches.TryResolveRangeAlphabet`, COBOLNET1997/1998/1999 and COBOLNET2176.** "*Alphabet-name-1 may be specified only when the literals specified in the THROUGH phrase are of class alphanumeric or national. If literal-2 and literal-3 or literal-5 and literal-6 are of class national, alphabet-name-1 shall reference an alphabet that defines a national collating sequence; otherwise, alphabet-name-1 shall reference an alphabet that defines an alphanumeric collating sequence.*" All four obligations are enforced: a NUMERIC through-pair under `IN` is COBOLNET1998; a name that is undeclared or is not an alphabet is COBOLNET1997; each half of sentence 2's class agreement is COBOLNET1999; and **`IN alphabet-name-1` written with NO THROUGH phrase at all is COBOLNET2176** (kb/Work PB890) — with no THROUGH phrase there are no "*literals specified in the THROUGH phrase*", so the permission the words "only when" grant is never satisfied. That last arm was held open by a guard, `cond.Values.Any(v => v.High is not null)`, whose stated reason was that the phrase is "inert, not erroneous" over a value LIST — a GENERAL-RULE answer to a SYNTAX-RULE question — and whose real reason was a green golden that violated the rule (`2002/pb695_value_false_optional_words`'s `88 CN-ORDER VALUE 1 IN AL1 …`, since rewritten as a conforming alphanumeric THROUGH range). Negative `negative/pb890-value-alphabet-without-through`.

- **§13.18.63.2's LEADING WORDS — `DataBinder.CheckValueConnective`, COBOLNET2177** (kb/Work PB559). Format 1 prints `VALUE IS literal-1` — VALUE underlined, IS not, and neither VALUES nor ARE anywhere — while Formats 2, 3 and 4 print a two-line REQUIRED CHOICE between `VALUE IS` and `VALUES ARE`, which §5.2.6.3 makes exactly one of its alternatives. So VALUE pairs with IS, VALUES with ARE, and `VALUE ARE` / `VALUES IS` / a Format-1 `VALUES` are spellings no figure prints; §13.18.63.3 SR17 ("*The words VALUE and VALUES are equivalent*") is what admits VALUES at all and is a **Format 2** rule, reached by Format 3 through SR24 and Format 4 through SR34, which Format 1 applies to neither, and SR39 states the identical pairing in words for Format 5 — the one format that detaches the connective. **The screen takes the format from its CALLER and is deliberately not a grammar rule**: Format 1 and Format 3 are the same token sequence and differ by the entry's level-number (SR33), Format 4 by the section, and a parse rule cannot see either — so the grammar keeps the `(VALUE|VALUES) (IS|ARE)?` superset and all three live bind arms screen it (the condition-name arm, the data-item/table arm, and the report-section arm in `DataBinder.Reports`; the screen-section VALUE clause is refused whole as a declined facility by `ScreenFacility`, and Format 5 by COBOLNET1708). Negatives `negative/pb559-value-connective-crossed` and `negative/pb559-value-format1-values`; the four LEGAL heads stay pinned by `2002/l1_value_values_equivalent_2002` and `2023/l1_value_values_equivalent`.
- **§13.18.63.4 GR20 / §14.9.39.4 GR7 — the SET store.** See `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D7.

**Edition.** The phrase and the `SET … TO FALSE` arm it feeds are a COBOL-2002 introduction, gated recognition-first in `VersionConformancePass.ParseArm` (`VisitValueClauseFalsePhrase`, `VisitSetBooleanStatement`) on `constructs.json` rows `value-false-phrase-2002` and `set-condition-false-2002`. The edge is DERIVED, not quoted, and the derivation is one line so it can be overturned in one: the repo holds no 2002 or 2014 text, the 2023 Annex E carries no 85→2002 VALUE or SET row (the A.1 authority gap, D18), and the reserved-word evidence cannot date it because `FALSE` is already reserved at COBOL-85 for EVALUATE. Goldens `2002/pb555_value_false_phrase` (positive) and `negative/pb555-*` (SR27 both arms, SR7, and the 85 edition edge).

### D5. REDEFINES uses the 3-tier ONE-canonical-backing model (A Alias / B StringCanonical / D reject-loud — Tier C `ByteCanonical` is DELETED, kb/Work PB164); every non-canonical view is a computed `Place` accessor over the single backing — never two stored fields per storage area. [Canonical: `COBOLNET_DESIGN.md` §4 + `COBOLNET_REDEFINES_DESIGN.md`]

**Rationale.** A write through any view must be visible through every other view of the same area (ISO §13.18.44 "same storage area"). One stored canonical + computed accessors guarantees that coherence with NO byte substrate for the dominant DISPLAY-homogeneous case (Tiers A/B — the entire near-term NIST path), and confines `byte[]` to genuine mixed-USAGE puns only (Tier C, owner decision §18 #1).

**Rejected alternatives.** (a) **Separate independent typed fields** — rejected: two independent fields cannot stay coherent under a cross-type pun, reproducing the silent-stale-read the no-byte-substrate model exists to prevent — even a loud cross-type-read guard only *detects* it, it does not make the program correct. (b) Global `byte[]` for all REDEFINES — rejected: the abolished substrate. (c) `[StructLayout(Explicit)]`/`[FieldOffset]` overlay — rejected: cannot overlay a `string` on a `long`, which is the dominant pun.

### D6. SYNCHRONIZED is a no-op for in-memory typed data; honored only at the file/byte-serialization boundary (G6). BLANK WHEN ZERO and JUSTIFIED are display/store-time rules on PicInfo.

**Rationale.** Alignment has no meaning without byte addresses (the CLR aligns a `long` naturally); its only observable effect is on overlay/serialization size, which is already the byte path. BLANK-WHEN-ZERO/JUSTIFIED are pure value-rendering rules.

**Rejected alternatives.** Modeling SYNC by inserting padding fields — rejected: reintroduces layout and is invisible to typed access.

### D7. Fixed-point stays native long (unscaled, compile-time scale); 19-38 digits → Int128 via a WidePrecision flag; no decimal/BigInteger.

> **Refinement.** The wide selector is the DERIVED property `PicInfo.IsWide`
> (`Numeric && !IsFloat && Digits > 18`) driving `ClrType`/`DefaultInitializer` — not a stored flag (`Digits` is
> already on PicInfo; a parallel flag could drift from it). Literals wider than long emit `Int128.Parse("…")`
> (`EmitCore.IntLiteral` — C# has no Int128 literal form); the store boundary narrows via the width-aware
> `ArithmeticEmitter.Narrow`. The SURFACE cap is per-edition (18 at `--std 85`, 31 at 2002+, >31 rejected everywhere —
> `EditionContext.CheckDigitCapacity`, COBOLNET0801/0802); Int128's 38 digits are substrate headroom only.

**Rationale.** Owner-locked: hardware-native, exact, DISPLAY image falls out for free; Int128 is a fixed-size value type far cheaper than BigInteger.

**Rejected alternatives.** decimal (software, not hardware-native) and BigInteger (heap-allocating) — both rejected by the owner.

### D8. National and boolean data ride the fixed-width STRING substrate (the full decision set D-N1..D-N4/D-B1 lives in `PHASE4_RECONCILIATION.md`).

- **D-N1 national**: an elementary national item's VALUE CARRIER is a plain C# `string` of `Length` characters —
  .NET strings are natively UTF-16, so "two bytes per character position" is the documented implementor choice
  (§13.18.60.4 GR8 + §8.1.2 NOTE 2). An ELEMENTARY national item's `ImageWidth` counts CHARACTER POSITIONS
  (never byte-doubled); `ByteWidth` counts the STORAGE the item occupies and is `2 × Length`. **For an elementary
  item the two are equal for every other leaf kind, and national is the ONE place they differ.** A GROUP's
  `ImageWidth` is in the unit of its class (kb/Work PB1665, `RecordLayout.ImageWidth` — the one copy of the rule,
  `DataItem.ImageWidth` is that answer): a `GROUP-USAGE NATIONAL` group is as-if `PICTURE N(m)`, so its width is
  national POSITIONS; any other group is an ALPHANUMERIC group (§8.5.2.1), a string of STORAGE characters in which
  a national position counts as the two it occupies — so for an alphanumeric group `ImageWidth == ByteWidth`, and
  every consumer of a group's image (MOVE, comparison, boundary windows, ref-mod positions, RENAMES) sizes by
  it. A site counting what an elementary item OCCUPIES reads `ByteWidth`; a site counting an elementary
  carrier's positions reads `ImageWidth`.
- **D-N6 national-edited IS category National carrying an `EditMask`** (kb/Work PB492, 2026-09-12). §8.5.2.11's
  category has no `PicCategory` member of its own, exactly as alphanumeric-edited has none: an item is
  national-edited iff `{ Category: National, EditMask: not null }` and alphanumeric-edited iff
  `{ Category: Alphanumeric, EditMask: not null }`. §8.5.2.1 Table 2 is why that is sound — each edited category
  shares a CLASS with its plain one, so only the rules worded in CATEGORIES need to tell them apart, and
  `PicInfo.EditMask` is the one axis that does. **`PicInfo.IsCharacterEdited` is the ONE predicate for the rules
  that DON'T** — §13.18.40.5 Table 7 gives both categories "Simple insertion" and nothing else, so one renderer
  (`CobolEdit.FormatSimpleInsertion`, reached only through `RuntimeApi.EditFormatSimpleInsertion(value, pic)`)
  and one MOVE/ACCEPT/STRING receiver arm serve both. The rules that DO tell them apart read `Category` beside
  `EditMask`: Table 16's separate national-edited ROW (`MoveValidity.Table16Refusal`, through `Table16Operand.Heading`), `InitializeCategory.NationalEdited`
  (§14.9.20.4 GR5c's category-name match, even though GR6c fills both with national SPACES), and the edition
  gate (`VersionConformancePass.PictureConstructId` → `national-edited-2002`, the FINER identity, so the usage
  arm does not also fire `national-data-2002` on the same entry), and every rule worded "category alphanumeric"
  or "category national" (§8.5.2.1: a category name in a rule means the category), which asks `ItemCategory`'s
  one screen — its `Admits` reads `Table16Operand.IsEdited` beside the category (kb/Work PB850: until then the
  record keys, FILE STATUS, ASSIGN USING, READ / RETURN INTO, the program-address-identifier and INVOKE's
  identifier-2 all accepted an edited item silently). A message, the activation description and START's SR6 b) 2.
  comparison name a category through `ItemCategory.CategoryName`, the ONE spelling of a Table 2 category.
  ⛔ **The category was a staged skeleton until PB492 and the shape of the bug is the reason this entry exists:**
  the analyzer RECOVERED the category to Alphanumeric and raised COBOLNET0899, so no such item could be defined
  at any edition — and every consumer that had grown an edited arm had grown only the ALPHANUMERIC half of it,
  including two whole-width-fill guards in `MoveEmitter.ConvertSource` that sent a figurative or ALL-literal
  source AROUND the editor instead of through it. Recognition also read the GR7/GR10 insertion set as a literal
  list that left out the declared EDITING character-1, so `PIC NNTNN EDITING "T" IS N":"` — a shape GR10 names
  outright — was refused as an invalid PICTURE. The set is now `CobolEdit.IsEditedCategorySymbol`, ONE definition
  derived from rule 3's simple-insertion set, read by BOTH recognition arms.
- **`ItemCategory` IS THE ONE §8.5.2 CLASS-AND-CATEGORY READER, not the category one alone** (kb/Work PB391,
  2026-09-13). Its original charter was the rules worded in CATEGORIES (`IsAlphanumeric`,
  `IsAlphanumericOrNational`, `IsAlphanumericGroup`, `Face` — the narrative is in
  `docs/COBOLNET_FILES_DESIGN.md`, where the first three askers live). It now also owns the one rule worded in
  CLASSES that more than one subsystem asks: **class index, message-tag, object or pointer**, which THREE rules
  name with word-for-word identical text — §13.16.3 SR24 e) (not a conditional variable), §13.18.60.3 SR11 (the
  same prohibition the other way round) and §14.7.6 rule 4 (*"Neither data item contains an OCCURS, REDEFINES,
  or RENAMES clause or is of class index, message-tag, object, or pointer"*, the CORRESPONDING exclusion).
  `ItemCategory.IsIndexMessageTagObjectOrPointer` is that one predicate and `Sr14PhraseOf` / `Sr4PhraseOf` — the
  §13.18.60.3 phrase readers that STATE the population, moved here from `DataBinder.UsageDeclaration` — are how
  it is derived rather than hand-listed. **The third asker is why it moved**: `CorrespondingBinder` is outside
  `DataBinder` and was carrying a one-usage copy (`Pic?.Usage is not Usage.Index`) under a comment claiming the
  other three classes had "no representation in this data model yet", which `PicCategory`'s Pointer /
  ProgramPointer / FunctionPointer / ObjectReference members and `Usage.MessageTag` contradict. A POINTER
  namesake pair was excluded from MOVE CORRESPONDING only by the ACCIDENT of a private Table-16 copy's
  `_ => false` default; `ConditionNameAssociationDriftTests` and `CorrespondingRule2DriftTests` pin both halves.
- **A GROUP ITEM'S KIND IS A SET, AND `ItemCategory.GroupKindsOf` IS THE ONE READER OF IT** (kb/Work PB392,
  2026-09-19). The standard distinguishes FIVE kinds of group item — alphanumeric (§3.11, defined as the
  complement), bit and national (§13.18.29.4 GR1/GR2), strongly-typed (§8.5.3.3) and variable-length
  (§8.5.1.12.1) — and several syntax rules are worded as a LIST of them: ADD §14.9.2.3 SR6 and SUBTRACT
  §14.9.44.3 SR6 admit *"alphanumeric group items, national group items, variable-length groups, or
  strongly-typed group items"*; MOVE §14.9.25.3 SR12 admits every kind ("group data items", and NOTE 5 under
  §14.9.25.4 GR11 confirms it for bit and national groups). Asked as the boolean `DataItem.IsGroup`, such a rule admits the kinds it names AND
  the ones it does not: a `GROUP-USAGE BIT` group passed SR6 and the statement then ran as a silent no-op,
  because §14.7.6 rule 3 requires both items of an implied ADD/SUBTRACT pair to be numeric and every child of a
  bit group is category boolean. **`GroupKinds` is `[Flags]`, not a scalar classification**: §13.18.29.3 SR1
  (*"The GROUP-USAGE clause may be specified only if the subject of the entry is a group item that is not
  strongly typed and not a variable-length group"*) keeps bit and national disjoint from the other two and §3.11
  makes alphanumeric the complement, but NOTHING keeps STRONGLY-TYPED and VARIABLE-LENGTH apart — a scalar enum
  would have to pick one and would then answer a rule admitting only the other incorrectly. A rule is therefore
  `(kinds & admitted) != 0`, its admitted set is a `CorrespondingOperandRule` ROW per verb (clause + admitted
  kinds + whether the rule's text names level-66), and the diagnostic's admitted-set phrase is GENERATED from
  the set by `ItemCategory.Spell` so the message cannot disagree with the check. `IsAlphanumericGroup` and
  `Face`'s group arms — two of the three hand-written copies of this classification — now read the one reader;
  `GroupKindDriftTests` pins the §3.11 complement, the SR1 disjointness and the three rule rows. ⚠ The
  CORRESPONDING verbs are the only askers converted so far: `InspectBinder` still admits every group kind where
  §14.9.22.3 SR1 names *"an alphanumeric or national group item"*, so `INSPECT <a bit group> TALLYING …` is
  accepted and tallies the bit characters — measured, and registered as a lead for a note of its own rather than
  swept in with a row this change set cannot re-verdict.
- **D-N7 a national-form NUMERIC / NUMERIC-EDITED / BOOLEAN item is its DISPLAY twin COMPOSED with the one
  national byte transform** (kb/Work PB646, 2026-09-13). §13.18.60.3 SR12 admits FIVE picture shapes under usage
  national — boolean, national, national-edited, numeric and numeric-edited — and all five are live. The
  national-form three are NOT new categories: §13.18.40.4 GR1 ("When the usage of the subject of the entry is
  national, each symbol representing a character position defines a national character position") says the item's
  digits, its SIGN IS SEPARATE position (§13.18.52.3 SR2 admits the clause here by name; GR6a makes that position a
  character position and not a digit position) and its insertion characters are the SAME characters its DISPLAY
  twin holds, in a different character set, and §13.18.60.4 GR8 pins only the SIZE of one of those characters
  (D-N1: two bytes, UTF-16BE). So:
  - the **VALUE CARRIER is the twin's** — `long`/`Int128` for category numeric, the edited image string for
    numeric-edited, the `'0'`/`'1'` run for boolean. `PicInfo.ClrType` needed no national arm.
  - the **CHARACTER IMAGE is the twin's** — `PicInfo.ByteForm` is `Zoned` for usage NATIONAL exactly as for
    DISPLAY, so `CobolNum.FormatImage`/`ParseImage` own every digit, sign and de-edit rule once.
    `PicInfo.IsCharacterFormNumeric` is THE ONE predicate for "this item's image is a run of digit characters",
    read by every site that VIEWS an item through its characters (reference modification §8.4.3.3.4 GR3/GR5a, a
    RENAMES span's `NumericImagePlace`, a STRING receiver). ⚠ It is **not** read by the sites that decide whether
    the carrier may be REPLACED by that string (`MarkImageForced` / `StorageFormPass`'s whole-group promotion):
    promotion pins the carrier at the leaf's `ImageWidth` CHARACTER positions, which is the whole storage of a
    DISPLAY item and half of a national one. A national-form numeric leaf IS promoted since kb/Work PB1665 made
    an alphanumeric group's `ImageWidth` count storage characters (the group MOVE that corrupted across such a
    leaf is sized by the image it carries), which is what lets `IS NUMERIC` read its stored characters
    (kb/Work PB1466); only `USAGE INDEX` stays out.
  - only the **CHARACTER→BYTE step differs**, and it is applied ONCE, by the one national coding, at every byte
    boundary: `NationalWindow.PositionsOf` (now a single test — `Pic.Usage is National` → `ElementaryImageWidth`,
    the count §13.18.40.4 GR1 defines and §15.50.4 r2 returns) plus `CobolBits.NatBytes`/`NatReadWindow`.
    `GroupImageCodec` COMPOSES it over the carrier codec (`CarrierImageOf` / `CarrierFromChars`); the two used to
    be mutually exclusive arms, sound only while the shape was staged loud and could not reach emit.
  - the **SIGN CLAUSE an item INHERITS** reaches its `SignKind` through ONE method, `DataBinder.ApplyEffectiveSign`
    — the SIGN twin of `ApplyEffectiveUsage` — because there are TWO routes by which an item acquires a clause its
    own entry did not write: §13.18.52.4 GR1 group inheritance and the §13.18.49.4 GR5 SAME-AS ancestor transform
    ("If an alphanumeric group item, national group item, or strongly-typed group item to which data-name-1 is
    subordinate contains a SIGN clause, the effect is as though that SIGN clause had been specified for the subject
    of the entry"). Each carried its own copy of the §13.18.52.3 SR2 usage guard and the copies DID drift: the
    SAME-AS one stayed at DISPLAY alone, so `01 TN USAGE NATIONAL SIGN IS LEADING SEPARATE. 05 TNE PIC S9(3).
    01 SNX SAME AS TNE.` measured LENGTH 3 / BYTE-LENGTH 6 / `04N` against the inheritance route's 4 / 8 / `-045`.
  - a **GROUP-LEVEL VALUE distributes over a national-form numeric leaf** exactly as over its DISPLAY twin —
    `GroupValueSlicer.DistributableSubtree` and `SliceInit` read `IsCharacterFormNumeric`, so the positional
    CHARACTER slice is decoded by the same `ParseDisplay` in both. ⛔ This is the one place where the national
    form is NOT screened out upstream: §13.18.63.3 SR14's usage-DISPLAY requirement is written about items
    "subordinate to an alphanumeric group item", while a `GROUP-USAGE NATIONAL` group's subordinates are required
    to be usage national by §13.18.29.3 SR3, which contemplates numeric ones by name ("Any signed numeric data
    items shall be described with the SIGN IS SEPARATE clause"). A DISPLAY-only arm there made the WHOLE subtree
    non-distributable: `01 NG GROUP-USAGE NATIONAL VALUE N"AB123". 05 NGA PIC N(2). 05 NGB PIC 9(3).` measured
    `NGA=[  ] NGB=[000]` against its alphanumeric twin's `[AB]` / `[123]`.
  ⛔ **The category was refused BY NAME until this landing** — `PictureAnalyzer` raised COBOLNET0899 "Phase 4a
  residue" from FOUR hand-written arms (the boolean arm and the numeric arm of the usage screen, plus private
  copies in the float-edited and locale-edited analyses that were ALSO the only SR12 screen those two paths had,
  so a float-edited or format-2 picture under USAGE BINARY/COMP/PACKED met no §13.18.60.3 SR3 screen at all).
  All four are gone: the two analyses now call `ScreenUsageAgainstPicture`, the ONE screen. ⛔ **Both of the
  defects above were found by the four-lens review of that landing's own diff, and they are the same shape it
  fixed elsewhere** — a rule written down in two places with only one copy widened. Every such pair in this
  design is now ONE method with the second caller pointing at it.
- **D-N2 byte≠char containment** (NARROWED 2026-09-05, kb/Work PB231 — RESIDUE-11 DISCHARGED on the byte-window
  channel). The BYTE-WINDOW surfaces — REDEFINES (`ComputeTier`), and the EXTERNAL / ADDRESS-OF / BASED cells
  (`ForceStringCanonical`) — now CARRY a national leaf: the class walk advances by each member's storage extent
  (`BitLayout.RunBits` → `WidthBits` → `ElementaryByteWidth`), the class width is the members' maximum
  `ByteWidth`, the in-class OCCURS stride is `ByteWidth`, and `Place.NationalWindow` transcodes the window's
  UTF-16BE pairs through `CobolBits.NatReadWindow`/`NatWriteWindow` — the same serialization `NatBytes` already
  gave CONVERT's raw-storage channel. The §13.18.44.3 SR8 size screen and the Tier-A alias arm read `ByteWidth`
  for the same reason. **A NATIONAL GROUP's own window is a `NationalWindow` too** (kb/Work PB1653,
  `NationalWindow.WindowPositionsOf` — §13.18.29.4 GR2 b): its VALUE is its m national positions over the 2m bytes
  of its window, exactly as a bit group's window is a `BitWindow`, so every value reader/writer (`Read`/`Write`,
  `WriteGroupValue`, ACCEPT, the CALL crossing) takes the window's own coding; its STORAGE image — what a file
  record, a group MOVE or a status group deposits — is the byte window (`PlaceRenderer.ByteWindowRead/Write`,
  reached by `GroupImage`/`WriteGroupImage`). A BIT GROUP's view has the same two channels (kb/Work PB1904): its
  `BitWindow` read is the m boolean positions of its VALUE, and its STORAGE image is those positions PACKED into
  ceil(m/8) characters (§13.18.60.4 GR5; `CobolBits.Pack`, the law a record-struct bit group's `AsImage` uses) —
  `GroupImage` packs the window's read and `WriteGroupImage` unpacks an image into it, so a bit-group record that
  redefines another FD record writes its bytes, not "0100…" (`GroupViewAliasDriftTests.ViewOfGroup_StorageImage_IsThePlainGroups`
  pins both group kinds' storage channel, view against plain). **WIDENED AGAIN 2026-09-05, kb/Work PB327:** the FD/SD RECORD is the same channel after all — see D-N5, now
  discharged — and its gate is the same predicate (`DataBinder.GateFileRecordByteSurface`). **STILL REFUSED:**
  a pointer-class leaf on any byte-window surface, the file record included (§13.18.60.3 SR14 / PB183 — no byte
  image at all; PB231's remaining third).
- **D-BW1 THE ONE BYTE-WINDOW CARRIAGE GATE** (kb/Work PB231, 2026-09-04). The question "may this LEAF ride a
  shared byte-window storage area?" has exactly ONE answer in the tree — `DataBinder.ByteWindowResidueOf`,
  which returns null when the leaf may and the residue clause naming why not when it may not. It is an
  ALLOW-list, deliberately: a category added to the model with no pinned byte image must be DENIED by default
  rather than silently laid out as a zero-width alias. **Carried:** the character categories (one byte per
  position, item 209); every NUMERIC usage on its pinned byte form (D19/PB164 — THE ONE image predicate
  `HasImageByteForm`); and BOOLEAN in **both** representations — a DISPLAY boolean as one '0'/'1' character
  (D-B1) and a **USAGE BIT run as the §13.18.60.4 GR5 sub-byte packing** laid out by the §8.5.1.6.3 cursor
  (§13.18.29.4 GR1c for a bit group) and windowed by `CobolBits.ReadWindow`/`WriteWindow`; NATIONAL in both
  spellings (D-N2 / RESIDUE-11); and the POINTER family, which rides the area's MANAGED SLOTS rather than its
  bytes (D-SLOT). **Refused: nothing — the gate has no residue left.** Its default arm is kept as the loud
  guard for a category added to the model with no pinned representation, which is what the ALLOW-list
  direction is for.
  ⛔ It replaced TWO hand-written lists — `ComputeTier`'s deny-list and `ForceStringCanonical`'s allow-list —
  which drifted the moment only one was widened: PB203 opened the REDEFINES surface to USAGE BIT leaves and
  left the cell surface refusing them, so a BASED/EXTERNAL/ADDRESS-OF record rejected legal source that its
  byte-identical REDEFINES twin compiled and ran. `ByteWindowResidueDriftTests` pins all four surfaces to one
  verdict per leaf; discharging either residue is now a single edit that opens every surface at once.
  **⛔ THAT CLAIM WAS TESTED AND HELD, TWICE (2026-09-05):** discharging the NATIONAL residue was one edit to
  the gate — two arms from a residue clause to `null` — plus the geometry behind it, and every one of the four
  surfaces opened together; the POINTER residue then cost one more arm plus the slot carriage (D-SLOT). The
  carried population is now the character categories, every NUMERIC usage on its pinned byte form, BOOLEAN in
  both representations, NATIONAL in ALL its spellings — the category, the national-edited form, and the
  national-form numeric / numeric-edited / boolean of §13.18.60.3 SR12, whose arm stopped being a derived-only
  answer when kb/Work PB646 removed the COBOLNET0899 staging (D-N7); the predicate is now the single test
  `Pic.Usage is National` — and the POINTER family. **Nothing is refused today.**
- **D-SLOT, the DYNAMIC-LENGTH half (kb/Work PB1026, 2026-09-23).** A dynamic-length item of a CELL-BACKED
  class (an EXTERNAL record, an EXTERNAL file's out-of-line record, an ADDRESS-OF-taken record) has no fixed
  character positions — §8.5.1.10.3 lets it be "located elsewhere in the computer's memory" — so it rides the
  SAME cell's slots too: the DYNAMIC slots, keyed by the item's ordinal among the class's dynamic-length items
  (`DataItem.ClassDynOrdinal`, assigned once by `ForceStringCanonical`; negative keys, so they never meet a pointer
  slot's byte offset). It occupies zero bytes of the image — its `ByteWidth`, §8.5.1.12.3's accounting of the fixed
  run — and contributes nothing to the cell seed (`GroupImageCodec.ImageInitOfOne`); its VALUE seeds the slot
  (`GroupImageCodec.CellDynSeeds` → `StorageCell.SeedDyn`, §8.6.4). Two codings carry it: `Place.DynSlotWindow`
  (the leaf — `StorageCell.DynAt` / `SetDynAt`) and `Place.VarGroupWindow` (a group over such leaves, which reads
  and writes as its CONTIGUOUS image, §8.5.1.11.2 — `StorageCell.ContiguousAt` / `StoreContiguousAt`, the D-FRA
  decomposition by the record's extent table else the take step, with `ContiguousExtentsAt` the table a WRITE sends
  beside it — and crosses a variable-length MOVE / boundary as its `CobolVarGroup` carrier —
  `VarGroupAt` / `StoreVarGroupAt`). Both derive from `Place.CellWindowCoding`, the one base every re-anchoring
  consumer reads. Before this, such a leaf was a ZERO-WIDTH byte window: `01 G EXTERNAL.
  05 A PIC X(3). 05 D PIC X DYNAMIC LENGTH.` displayed only A, silently.
- **D-SLOT, the CELL COMPONENTS (kb/Work PB1042, 2026-10-05).** The dynamic-length half generalized: a cell-backed
  area's VARIABLE-LENGTH COMPONENTS are its dynamic-length items AND its dynamic-capacity tables (§8.5.1.9.1 3): a
  table "may be defined in any place, other than the file section, in which a fixed-capacity table may be defined",
  and an EXTERNAL or ADDRESS-OF-taken record is such a place). `Place.CellComponents` is THE ONE model: components
  are numbered in storage order with ONE ORDINAL PER OCCURRENCE of every enclosing fixed table
  (`CellComponents.PerOccurrence` is the subscript stride; `DataBinder.AssignCellComponents` is the one writer of
  `ClassDynOrdinal`), so a dynamic-length item inside a fixed table element has its own slot per occurrence. A
  table's slot holds a `CobolDynTable<StorageCell>` (`StorageCell.DynTableAt` / `SeedDynTable`, a key family apart
  from the dynamic-length and pointer slots) whose every occurrence is an ELEMENT CELL: the element's fixed run in
  its `Ref`, the element's own components in its own slots — a SCOPE of its own, where the element's members are
  re-laid out from offset zero and numbered from zero. So a table, or a dynamic-length item, inside a table element is
  the same mechanism one level down, and every capacity rule (§8.5.1.9.2–.6, SET Format 14, EC-BOUND-*) stays
  `CobolDynTable`'s. In the fixed run a table RESERVES its one-element `ByteWidth` (the D-SLOT pointer reason: every
  following offset is the byte-addressed walk's) and the cell composers cut the reservations out
  (`VarGroupWindow.DynTable` = each component's element width, 0 for a dynamic-length item). A reference walks its
  subscript levels through ONE walk (`ReferenceResolver.WindowScopeOf`): a fixed level displaces offset, bit offset
  and ordinal; a dynamic-capacity level enters the occurrence's element cell (`CellTableSegment` +
  `DynTableSegment`, its accessor the reference's direction). The whole-table path is `BuildTablePath` (its string
  twin was deleted), and the INITIALIZE / MOVE element / CORRESPONDING cursors re-anchor on the window's scope
  (`RedefViewPlace.Cell` / `DynOrdinal`, `PlaceCursor.DynamicElement`). The composer's own exclusion is the declared
  group's (`DataItem.CurrentExtentImageCapable`, `CurrentImageCapable` for the one-way image): a cell-backed group
  whose table element holds a component of its own is the named loud for whole-group image operations, while each
  element and the element's members are referenced normally. The OCCURS DEPENDING table of a cell-backed group is
  the window's trailing storage (`VarGroupWindow.Odo`, `CellOdoTail`; see the variable-length group section).
- **D-SLOT the MANAGED SLOTS of a shared storage area** (kb/Work PB231, 2026-09-05 — the pointer third). A
  data item of class pointer or class object holds a MANAGED REFERENCE, which is not a byte sequence, so it is
  the one leaf kind a byte window genuinely cannot express. It does not ride the area's bytes: **`StorageCell`
  carries MANAGED SLOTS keyed by BYTE OFFSET beside its byte image**, the member's bytes there are RESERVED
  placeholder positions of its `ByteWidth` (8 — §13.18.60.4 GR23 leaves the size to the implementor), and
  `Place.SlotWindow` — the third `WindowCoding`, beside `BitWindow` and `NationalWindow` — reads and writes
  the slot at the same byte offset the byte window would have used, through `CobolPtr.SlotRead`/`SlotWrite`.
  Reserving the bytes is what keeps §14.9.3.4 GR3's byte quantity and every following member's class offset
  exactly what a byte-addressed area says they are.
  - **§14.9.3.4 GR9 is satisfied BY CONSTRUCTION**: an unwritten slot reads null, so "data items of class
    object or class pointer in the allocated storage are initialized to null" holds for every allocation path
    that exists or will exist, and the read renders the item's own `PicInfo.DefaultInitializer` as the null
    state — the same expression an ordinary declared pointer field is seeded from. FREE drops the slots with
    the image (§14.9.15.4 GR1a); they are one storage area.
  - **The member's POSITIONS hold the pointer's storage image (kb/Work PB1071; docs/CONFORMANCE.md DOC-A.1-216).**
    The slot holds the VALUE; the member's 8 reserved positions hold `PointerImage`'s image of it (NULL = eight zero
    positions), so a byte window, a group MOVE/DISPLAY and the CALL boundary's BY CONTENT record agree. One author,
    three keep-in-step sites: `CobolPtr.SlotWrite<T>` writes the image beside every slot store (a class-object member
    has none and keeps its placeholder spaces); the cell seeds (`GroupImageCodec.SlotSeedOf`) start the positions at
    `PointerImage.NullImage`; and ALLOCATE form 2 passes `CobolPtr.Allocate` the member offsets
    `SlotWindow.PointerImageOffsetsOf` (each OCCURS occurrence, at the stride a reference applies) so its uniform fill
    does not cover them (§14.9.3.4 GR9). `SlotWindow.CarriesPointerImage` is the one test of which slot members have an
    image. Only a byte WRITE over the positions can disagree afterwards, and it is one-way: a pointer is never decoded
    back from its bytes.
  - **The RECORD-STRUCT twin: a ONE-WAY transfer image (kb/Work PB244).** A strongly-typed group holding such a
    leaf in an ordinary record struct has no cell, but it has the same STORAGE: `DataItem.TransferImageCapable`
    admits it and `GroupImageCodec.EmitImageMethods` emits its `AsImage()` with each slot leaf contributing
    `GroupImageCodec.SlotImageOf` — the pointer's live `PointerImage` (PB1071) or an object leaf's placeholder,
    the same two recipes the cell seed (`ImageInitOfOne` → `SlotSeedOf`) spells — and NO
    `FromImage`. Only the TRANSFER readers ask it (DISPLAY, a MOVE's sending group, through the `transfer:`
    flag of `PlaceRenderer.GroupImage` / `SendingGroupImage` / `OperandText.AsStorageImage`). ⛔ It is NOT
    folded into `IsImageCapable`: the image has no inverse (a pointer's token decodes to no reference, and two
    groups differing only in an object leaf render alike), so every read-back (CALL/INVOKE copy-back, READ, a group MOVE
    receiver) keeps asking the two-way capability and stays refused. (A strongly-typed group RELATION never
    reads an image at all: §8.8.4.2.12 compares it element by element — see the TYPEDEF paragraph below.)
  - **The slots belong to the CELL, never to the item**, and that is the whole point: EXTERNAL sharing,
    ADDRESS OF aliasing and `SET ADDRESS OF` re-pointing all mean "two descriptions of ONE storage area", so a
    pointer member re-pointed through one description is visible through every other. Each cell surface now
    emits `StorageCell {RedefinesClass.BackingCellCsName}` FIRST and defines the `ref string` backing as
    `ref {cell}.Ref`, so the two halves being one area is an identity, not a coincidence.
  - **A REDEFINES class has no cell, and that is a CARRIER question, asked once** — in `ComputeTier`, not in
    the shared gate. It is unreachable on conforming source: §13.18.44.3 SR12/SR14 bar a class-object /
    message-tag / pointer item and a strongly-typed group from either side of a REDEFINES entry, and
    §13.18.60.3 SR14 admits a pointer USAGE only at level 1 or under a STRONG type declaration.
  - **Two SCREENS landed with it**, because the residue diagnostic COBOLNET1695 had been rejecting two
    NONCONFORMING shapes for the wrong reason and opening the gate would have made them compile clean:
    **COBOLNET1797** (§13.18.5.3 SR1 — a BASED subject of class object) and **COBOLNET1796** (§13.18.22.3 SR4
    — an EXTERNAL item of class object or pointer). The asymmetry is the standard's own.
- **D-N5 the RECORD-IMAGE channel IS the byte channel — DISCHARGED 2026-09-05, kb/Work PB327.**
  `GroupImageCodec.AsImage()`/`FromImage()` is a group's **STORAGE** face: a numeric leaf already contributed
  its radix-2 / BCD / IEEE bytes and a `USAGE BIT` run its §8.5.1.6.3 packing, and NATIONAL was the last leaf
  kind whose carrier was shipped instead of its bytes. It now contributes its two UTF-16BE bytes per position,
  through the same `CobolBits.NatBytes` / `NatReadWindow` pair the Tier-B window and the cell seed ride.
  **The width authority moved with it:** `RecordLayout.PhysicalWidth`/`OffsetOf` and
  `PhysicalModel.BuildPhysicals` read `ByteWidth`, which IS `ImageWidth` for every other leaf kind — so the
  change is byte-identical for national-free programs, and an ELEMENTARY item's `ImageWidth` stays the CARRIER
  authority (`StorageFormPass`'s form widths). A GROUP's `ImageWidth` is in the unit of its class
  (`RecordLayout.ImageWidth`, kb/Work PB1665): national positions for a `GROUP-USAGE NATIONAL` group (§13.18.29.4
  GR2b's as-if `PICTURE N(m)` length), storage characters for an ALPHANUMERIC group — whose operand value is its
  bytes: DISPLAY, group MOVE, group comparison, the CALL boundary windows and group ref-mod all see the bytes,
  the answer §13.18.29.4 GR2's own NOTE predicts ("Without the GROUP-USAGE NATIONAL clause, the content of such
  a group item would be treated as category alphanumeric, possibly leading to corruption or invalid handling of
  data"). `FUNCTION LENGTH(G)` and the width a `MOVE G TO R` copies are therefore one number: 8 for
  `01 G. 05 A PIC X(2). 05 N PIC N(3).`.
  <br>A **national group** keeps its character alphabet through a second generated face, `AsNat()`/`FromNat()`,
  exactly as a bit group keeps its boolean alphabet through `AsBits()`/`FromBits()` — §13.18.29.4 GR2b's as-if
  `PICTURE N(m)`, reached by `Place.NatImagePlace` / `PlaceRenderer.SendingNat`. CONFORMANCE.md DOC-A.1-57's
  "one sanctioned width divergence" is gone with the same edit: the displayed width of a variable-length group
  now EQUALS `FUNCTION LENGTH`.
  ⛔ The SEED lane is the one part of the record codec that IS byte-addressed — `ImageInitOf` seeds only
  byte backings — and it was corrected with the rest: an uninitialized national member of a class used to
  read back U+2020 (two 0x20 fill bytes paired) instead of national spaces.
- **D-N3 collating**: national comparisons order by UTF-16 code-unit ordinal (the implementor default national
  sequence); the ALPHANUMERIC program collating sequence never applies (separate sequences — §8.8.4.2.9; the
  alphanumeric table is a distinct full-UTF-16-range sequence, never consulted for national comparisons and no longer `& 0xFF`-masked, post-CA26). A NON-native national sequence exists via
  `ALPHABET … FOR NATIONAL` literal phrases (§12.3.7) + `PROGRAM COLLATING SEQUENCE FOR NATIONAL` (§12.3.6) — the
  sparse `NationalCollatingTable`/`__COLLATE_NAT` channel; the coded-set names collapse to the D-N3 identity:
  UCS-4's ISO 10646 order over one-code-unit-per-position characters IS code-unit order (§8.5.1.4 — COBOL
  recognizes no surrogate pairs, so the supplementary-plane codepoint/code-unit divergence is unreachable), and
  UTF-8/UTF-16 name coded character sets only (§12.3.7 GR7 Table 6 — never a collating sequence).
- **D-N4 repertoire**: the FULL national repertoire — one UTF-16 code unit per position (the Latin-1-only
  staged guard was lifted with the DISPLAY-OF/NATIONAL-OF wave). **The alphanumeric↔national correspondence is
  the TOTAL UTF-16 IDENTITY in both directions** (Annex A.1 item 33, CONFORMANCE.md §7 — declared with the
  PB59 landing 2026-08-09): no character lacks a correspondent, the §15.26.4/§15.66.4 r2/r3 substitution
  machinery is vacuous by declaration, and EC-DATA-CONVERSION is unreachable from any character pathway. The
  8-bit BYTE serialization of usage DISPLAY (CONVERT's ANUM→HEX/BYTE legs) is a SEPARATE §8.1.2 NOTE 2
  determination and still substitutes above 0xFF — open residue of the PB59 CONVERT rework (RV-15.19.4-2).
- **D-B1 boolean (SUPERSEDED 2026-08-04 by D19 — see below).** It read: "one alphanumeric character '0'/'1' per
  boolean position for BOTH usage display AND usage BIT — the §13.18.40.4 GR14 R14 license, a PERMANENTLY
  conforming choice." **The display half is right and stays. The USAGE BIT half was wrong**, and the word
  "PERMANENTLY" is what a reader should distrust: it rested on GR14 without reading §13.18.60.4 GR5. The **VALUE
  CARRIER** is still a `'0'`/`'1'` string for both usages — that part never changed and is not observable to a
  COBOL program; what changed is the LAYOUT (fix-queue PB43).

**Rationale.** The string substrate gives MOVE/compare/ref-mod/images for free on the ONE proven machinery;
the alternatives (a 1-byte national size, C# bool for PIC 1, bit-packing) each break a spec surface —
see the reconciliation design's F4 and the residue ledger.

**Rejected alternatives.** (a) `PIC 1 → bool` — rejected (multi-position
PIC 1(n)/fills/ref-mod need character semantics; a bool cannot carry them). (b) GR8's size-equal-alphanumeric
national — rejected for forward-compat (the non-Latin-1/NX"/BYTE-LENGTH residue presumes 2-byte). (c) ~~True
bit-packing for USAGE BIT — optional forever under R14~~ — **that alternative was not ours to reject; see D19.**

### D19. `USAGE BIT` OCCUPIES BITS. The value carrier stays a string; the LAYOUT becomes bit-granular. (Supersedes the USAGE BIT half of D-B1; fix-queue PB43, owner decision 2026-08-04.)

**The rule D-B1 did not read.** §13.18.40.4 GR14 says a boolean character "can be represented in storage as a bit,
an alphanumeric character, or a national character" — but that lists the AVAILABLE representations; **the USAGE
clause SELECTS one**, and both selections are mandatory:

| declaration | governing rule | required representation |
|---|---|---|
| `PIC 1(n)` with no USAGE | **§13.18.60.3 SR13(b)** implies USAGE DISPLAY → **§13.18.60.4 GR7** "an alphanumeric coded character set shall be used" | one character per boolean position — **what WiseOwl COBOL already does; unchanged** |
| `PIC 1(n) USAGE BIT` | **§13.18.60.4 GR5** "the USAGE BIT clause specifies that **bits shall be used** … alignment … is specified in 8.5.1.6.3" | **bits**, aligned per §8.5.1.6.3 |

Two storage forms for one category is **not** the two-mechanisms anti-pattern — it is precisely what the USAGE
clause is for. And §8.5.1.6.3 exists solely to align items that occupy bits: a clause with nothing to say if
USAGE BIT were char-per-position.

⛔ **A GROUP OF BIT ITEMS IS *NOT* A BIT GROUP.** §13.18.29.4 GR3 — a group with no GROUP-USAGE clause specified
or implied "is an **alphanumeric group item**"; §13.16.4 GR1 implies GROUP-USAGE BIT only for a group
*subordinate to a bit group*. So `01 G. 05 A PIC 1(5) BIT. 05 B PIC 1(3) BIT.` is an alphanumeric group and
`FUNCTION LENGTH(G)` is **§15.50.4 r3** — character positions, = **1** — not r1's boolean positions. Misreading
"bit group item" as "a group of bit items" is the same error as misreading GR14, one level up.

#### The architecture: V59's numeric byte forms, one granularity finer

A `PIC 9(4) COMP` already holds its VALUE in a native `long` and occupies its **byte form**
(`PicInfo.StorageWidth`) in the record image; COMP-3 images as BCD. **The C# carrier is not what the standard
constrains** — sizes, offsets, overlays and the record image are. So a bit item keeps its `'0'`/`'1'` string
carrier (every MOVE/compare/ref-mod/fill path is untouched) and gains a **bit width** and a **bit-granular
offset**. The one thing bit items need that no numeric byte form does is **sub-byte sharing** — two same-level bit
items inside one byte — which is exactly why `RecordLayout`'s `int` character offset is insufficient.

#### The layout function (ISO §8.5.1.6.3, transcribed to a walk)

`BitLayout.ExtentBits(group)` walks the non-redefining children, carrying a bit cursor:

1. A **bit item immediately following an elementary bit item or bit group of the SAME LEVEL** → placed at the next
   bit position (**no padding** — this is the only case that shares a byte).
2. **Any other bit item** → advance the cursor to the next byte boundary, then place. (Covers a bit item after a
   character item, after a bit item of a *different* level, and the first item of a group.)
3. A **non-bit item** → advance to the next byte boundary (its natural boundary), then place `ByteWidth × 8` bits.
   The implicit filler this generates is §8.5.1.6.3's "as needed to advance alignment to a required natural
   boundary for the next item within that group".
4. **At the group's end**, if the cursor is not byte-aligned → pad to a byte boundary. §8.5.1.6.3's trailing-filler
   rule is stated for "a record that is an alphanumeric group or strongly-typed group item", and GR3 makes every
   GROUP-USAGE-less group alphanumeric, so this fires for all of them.
   ⚠ Its NOTE excludes "a record that is entirely a bit group, a level 77 item, or a level 1 elementary item" —
   which is why an **elementary** bit item is not padded and keeps its exact bit count.
5. **§15.50.4 r5** requires every implicit FILLER position generated above to be COUNTED. It is, by construction:
   the cursor advances through filler.

**Bits per character position is a pinned implementor choice** (§8.1.2 makes it implementor-specified): **8**,
consistent with `ByteWidth`'s existing "DISPLAY = 1 byte per character position". Documented in
`docs/CONFORMANCE.md` §4.2.16.

#### What derives from it

| surface | rule |
|---|---|
| `DataItem.ElementaryBitWidth` | a USAGE BIT leaf → `Pic.Length`; anything else → `ElementaryByteWidth × 8` |
| `DataItem.ElementaryByteWidth` for USAGE BIT | `ceil(Length / 8)` (was: `Length`) |
| `DataItem.ElementaryImageWidth` for USAGE BIT | `ceil(Length / 8)` — the character positions it OCCUPIES |
| group `ImageWidth` / `ByteWidth` | `ceil(BitLayout.ExtentBits / 8)` **iff the group has a USAGE BIT descendant**, else the existing sum, byte-for-byte unchanged |
| `FUNCTION LENGTH` of an ELEMENTARY bit item or a bit group | **§15.50.4 r1** — `Pic.Length` **boolean** positions, NOT `ImageWidth`. These coincided only by accident before; they must now be read from different members |
| `FUNCTION BYTE-LENGTH` | `ByteWidth`, so `PIC 1(8) BIT` = 1 |

⛔ **THE GATE IS "HAS A USAGE BIT DESCENDANT", AND THAT IS A CORRECTNESS CHOICE, NOT TIMIDITY.** Without a bit
item there are no sub-byte runs, so the bit walk and the character sum agree *by construction* — gating on it
makes the change provably byte-identical for every program that writes no `USAGE BIT`, which is the overwhelming
majority and includes the entire existing corpus. (The same discipline as PB41's scale-0 fast path.)

#### Scope boundary, stated rather than discovered later

**IN:** the layout function, the sizing surfaces above, `FUNCTION LENGTH`/`BYTE-LENGTH`, and the record image
codec packing a bit run into its bytes.
**OUT:** `GROUP-USAGE BIT` (§13.18.29) — modelled by **D20** (kb/Work PB79).

#### The REDEFINES bit window — IN, and the scope boundary that said otherwise was wrong twice (kb/Work PB203)

This section used to read "a sub-byte `REDEFINES` overlay (a redefiner starting mid-byte) is **refused** rather
than given a rounded offset". ⛔ **Both halves were false, and neither had ever been measured.** Nothing refused
anything: `01 A PIC X(1). 01 V REDEFINES A. 05 F1 PIC 1(4) USAGE BIT. 05 F2 PIC 1(4) USAGE BIT.` compiled clean
and answered `F1` = the raw character of byte 0 and `F2` = a space read past the one-byte backing. And the bit
GROUP shapes did not even reach a rounded offset — they failed BACKEND compilation with four CS0103.

**The standard settles the direction, so there was never a rejection to design.** §13.18.44.4 GR1 states the
association in BITS — "Storage association for the subject of the entry starts at the first bit of the data item
referenced by data-name-2 and continues over an area sufficient to contain the number of bits required by the data
item referenced by the subject of the entry" — §13.18.29.4 GR1 c) sends a bit group's members through §8.5.1.6.3,
and §14.6.8.6's NOTE says it outright: "When an item is of usage bit, the item is not necessarily aligned on a
byte boundary and the item need not occupy an integral number of bytes."

**As built.** A Tier-B member that is a bit item (a `USAGE BIT` leaf or a `GROUP-USAGE BIT` group, `BitLayout.IsBitItem`)
carries a `RedefViewPlace.Bit` payload — the ABSOLUTE bit offset within the class's ONE byte backing and its
boolean-position count — and reads/writes through `CobolBits.ReadWindow`/`WriteWindow`, which touch only those
bits. Three consequences, each of them a rule rather than a case:

| surface | rule |
|---|---|
| `DataItem.ClassBitOffset` | the member's in-class offset in BITS — `8 × ClassOffset` for every byte-aligned item, and different only where §8.5.1.6.3 shares a byte. `DataBinder.AssignClassBitOffsets` runs NO cursor of its own: it places each child at the group's absolute bit offset plus `BitLayout.ChildStarts`, the RELATIVE §8.5.1.6.3 walk that `ExtentBits` and `StartBitWithin` also read (kb/Work PB1572 — a private absolute-seeded cursor rounded a bit group's first member to the next byte). The per-member ADVANCE inside that walk has ONE authority for every shape — `BitLayout.RunBits` → `WidthBits`, which is where the national two-bytes-per-position extent lives (kb/Work PB231). The advance used to be spelled a second way, `ImageWidth × 8`, and that second spelling was exactly half the truth for a national member |
| the window builder | ONE factory, `RedefViewPlace.For`. Three sites compose a Tier-B window (the resolver, the INITIALIZE receiver cursor, the MOVE CORRESPONDING leaf cursor) and each carried its own copy of the offset law, which is how the bit unit came to be missing from all three at once |
| the subscript stride | a bit member's occurrences lie at successive BIT positions, so the stride is `BitLayout.WidthBits`, not the item's `ceil(n/8)` byte ceiling — `PIC 1(4) USAGE BIT OCCURS 6` strides 4 bits |

**And Tier A gained the unit test it always needed.** `ComputeTier`'s alias arm compared CLR carrier type and
`ImageWidth`, which does NOT prove one field serves both members: a `USAGE BIT` leaf's carrier holds its PICTURE
1(n) boolean positions while its `ImageWidth` is the `ceil(n/8)` bytes it occupies, so `01 A PIC X(1).
01 B REDEFINES A PIC 1(8) USAGE BIT.` matched on both counts and B aliased A's one-CHARACTER field. Members share
one field only when they agree on the UNIT (both bit or neither) and, being bit, on the boolean-position COUNT.

**What is still OUT, and it is a REJECTION rather than a wrong answer:** a class containing a POINTER-CLASS leaf
stays Tier-D (§13.18.60.3 SR14 / PB183 — no byte image for §14.9.3.4 GR9's null-seeding to write into). That is
the only shape of §13.18.44.4 GR1 this model does not carry. The NATIONAL leaf joined the model on 2026-09-05
(kb/Work PB231 — RESIDUE-11; see D-N2).

#### The RUN is the unit of composition, and it has ONE computation (kb/Work PB584, 2026-09-22)

Rule 1 above — the only byte-sharing case — makes a maximal stretch of consecutive SAME-LEVEL bit members
one **run**, and a run occupies one image slice rather than one per member. Every composer of a group's
character image therefore has to know where the runs are, and `BitLayout.RunsOf` is the ONE place that decides:
it returns a `BitRunMap` answering which member LEADS a run and which members are in one, and a composer emits
the whole run's slice at the leader and nothing at a continuation.

It is stated here because there are TWO composers and they were not the same code. `PhysicalModel` builds the
physical field list the record-struct lane and the generated `AsImage()` walk, and it owned a private run scan;
the COMPILE-TIME image seed (`GroupImageCodec.ImageInitOf` — the string a Tier-B REDEFINES backing, an
EXTERNAL run-unit cell, a BASED cell or an OO backing starts from) walked `item.Children` and had NO run scan
at all. A MIXED bit/character group is where that difference is observable: `01 CTL. 05 H1 PIC 1(4) USAGE BIT
VALUE B"0100". 05 H2 PIC 1(4) USAGE BIT VALUE B"0001". 05 H3 PIC X(1) VALUE "B". 01 CV REDEFINES CTL PIC X(2).`
seeded 0x40 0x10 where rule 1 gives 0x41 ('A') and §13.18.44.4 GR1 then puts "B" in `CV(2:1)` — and the
identical group WITHOUT the alias was right, because only the alias forces the seed lane.

⚠ The seed lane cannot simply walk `PhysicalModel.PhysicalChildrenOf` instead: `DataBinder.AssignClassOffsets`
marks every DESCENDANT of a redefines class `IsCanonical = false`, so that walk answers EMPTY for the children
of a Tier-B canonical — the exact group the seed exists for. What the two composers share is the LAW, not the
field list, which is why the extraction is a run MAP and not a common walk.

⛔ A run CONTINUES only where `BitLayout.SharesByteWith` — the placement predicate the extent walk (`ExtentBits`), the offset walk (`StartBitWithin`) and the REDEFINES class walk (`AssignClassOffsets`) read — says the member shares its predecessor's byte. A run packs its members' carriers back to back, so a member those walks start on a fresh byte must LEAD a new run: an ALIGNED member (§13.18.1.4 GR1), and a group-typed TYPE subject, which §13.18.57.4 GR2 d) aligns "as though it were a level 1 item" and §8.5.1.6.3 therefore puts at the first bit of a byte (`DataItem.AlignedAsLevelOne`, set by `ExpandType`; carried by a SAME AS copy of such a subject, since §13.18.49.4 GR1 re-states data-name-1's description, TYPE clause included). A run scan that tested only "same level" packed `05 F PIC 1 USAGE BIT. 05 G PIC 1 USAGE BIT ALIGNED.` into ONE byte while `FUNCTION BYTE-LENGTH` counted two (kb/Work PB1569).

#### A boolean item's VALUE has ONE carrier answer, and the USAGE decision is separate (kb/Work PB584)

`ValueInitializer.BooleanCarrierOf` is the ONE reader of a boolean item's VALUE clause — its declared boolean
POSITIONS — in the three arms the standard writes: a figurative filling them (§8.3.3.6.4 GR4 makes the zero
format "one or more of the boolean character '0'"), a Format-6 `ALL literal-1` repeated to them (GR2), and a
plain boolean literal zero-padded on the right (§13.18.63.3 SR10 / §14.6.8.6). Whether those positions are then
PACKED is `GroupImageCodec.BooleanImageOf` alone.

THREE lanes used to answer the VALUE question and each was missing a different arm: the record-struct lane had
all three, the §8.5.1.6.3 bit-run carrier had no ALL-literal arm (`VALUE ALL B"1"` read back all zeros through
a REDEFINES alias and all ones without one), and the image seed reached its GENERIC figurative arm before its
own boolean arm, so `PIC 1(4) USAGE BIT VALUE ZERO` seeded four carrier characters where the packed image is
one byte (0x30, not 0x00). Separating "what are the positions" from "how are they stored" is what makes a
fourth lane impossible to get half-right.

### D20. GROUP-USAGE (§13.18.29): a bit group / national group is STRUCTURALLY a group and SEMANTICALLY an elementary boolean / national item — ONE as-if PICTURE on the DataItem, consulted by every category reader; the layout stays the group's. (kb/Work PB79.)

*Load-bearing spec anchors: §13.18.29.2 general format (`GROUP-USAGE IS {BIT | NATIONAL}`); §13.18.29.3 SR1 (only a
group that is not strongly typed and not variable-length), SR2 (BIT: USAGE BIT implied; no explicit USAGE on the
subject; every subordinate elementary item usage bit / category boolean, every subordinate group GROUP-USAGE BIT
explicitly or implicitly), SR3 (NATIONAL: USAGE NATIONAL implied; every subordinate elementary item usage
national; signed numerics SIGN IS SEPARATE; subordinate groups GROUP-USAGE NATIONAL); §13.18.29.4 GR1 (a bit
group is a bit data item, class/category boolean, "treated as though it were an elementary data item … described
with PICTURE 1(m), where m is the bit length of the group"; storage per §8.5.1.6.3), GR2 (a national group is
class/category national, "as though … PICTURE N(m), where m is the length of the group"), GR3 (no clause ⇒ an
alphanumeric group item); §8.5.2.10 item 3 (category national includes a national group); §8.5.1.6.3 (the
trailing-filler NOTE excludes "a record that is entirely a bit group"); §15.50.4 r1/r2 (LENGTH of a bit / national
group in boolean / national positions); §14.9.24 Table 16 (a national group sends/receives as category
national); §8.4.3.3.3 SR1 last sentence (a bit/national group is treated as elementary for reference
modification).*

**The problem.** `GROUP-USAGE` is not lexed as a keyword and no clause consumes it: `01 NG GROUP-USAGE NATIONAL.`
is a parse error. Seven inventory rows plus the group legs of RV-15.50.4-1/2 (PB61) are GAP for that reason alone.

**The model — one as-if picture, two structural predicates.** Today `IsGroup ⇔ Pic is null ∧ Children > 0` and
`IsElementary ⇔ Pic is not null` — 116 + 27 sites — and every category reader answers ALPHANUMERIC for a group
because that was the only group there was (GR3). A GROUP-USAGE group must stay a GROUP for everything
STRUCTURAL (children, layout, the record struct, REDEFINES tiers, INITIALIZE/CORRESPONDING walks, file record
images) and become an ELEMENTARY boolean / national item for everything CATEGORICAL (class/category, Table 16,
comparisons, INSPECT/STRING/UNSTRING legality, LENGTH, ref-mod). Therefore:

- `DataItem.GroupUsage : GroupUsage { None, Bit, National }` (init-only, from the clause; INHERITED by every
  subordinate group per SR2/SR3 "explicitly or implicitly", set by the usage-inheritance walk —
  `DataBinder.UsageInheritancePass`, the same walk that adjudicates a group-level `USAGE BIT / NATIONAL`, because
  GROUP-USAGE IMPLIES that usage for the subject: one walk, two spellings; that walk now also APPLIES the implied
  bit form to PICTURE-1 leaves without their own USAGE, §13.18.60.4 GR1 — which the group-level clause never had).
- `DataItem.AsIfPic : PicInfo?` — the GR1b/GR2b picture: `new PicInfo(PicCategory.Boolean, Usage.Bit, m, 0, 0, false)` for a bit
  group, `new PicInfo(PicCategory.National, Usage.National, m, 0, 0, false)` for a national group, null
  otherwise — where m is the group's BIT extent (`BitLayout.ExtentBits` WITHOUT rule 4: §8.5.1.6.3's trailing
  filler is excluded for "a record that is entirely a bit group") or its NATIONAL length (`ImageWidth` of a
  national group — beneath a national group every leaf is counted in national positions, and every subordinate is
  usage national by SR3). Computed lazily from the forest (no new pass): the extent is a pure function of the
  children, exactly as `ImageWidth`/`ByteWidth` are today.
- **`DataItem.OperandPic => Pic ?? AsIfPic`** — THE ONE READER for category. Every site that today asks
  `Pic?.Category` / `Pic is {} p` to decide class, category, Table 16 row, LENGTH positions, ref-mod
  admissibility, comparison class, string-operand-ness, DISPLAY-OF class screen, or the intrinsic-argument
  category screens switches to `OperandPic`. The structural predicates keep their meaning: `IsGroup` (children,
  no own Pic) and `IsElementary` (own Pic) are unchanged, so the 116 structural sites are untouched by
  construction; a NEW predicate `IsAsIfElementary => AsIfPic is not null` names the GR1b/GR2b cases where an
  operation must take the elementary path over a group (MOVE — a national group is a category-national SENDER
  and RECEIVER, padded/truncated in national positions, never the GR4 conversion-free group copy; comparisons —
  class national / boolean, not the alphanumeric group comparison; INSPECT/STRING/UNSTRING — a national/boolean
  operand; INITIALIZE — a national group is initialized as a national item? NO: §14.9.19 walks elementary
  items regardless — structural; CORRESPONDING — structural).
- **Storage is unchanged.** A national group's record image is the concatenation of its national subordinates
  (already what `AsImage()` yields); a bit group's image is the packed bit run `BitLayout` already lays out for a
  USAGE-BIT-descendant group, minus rule 4's trailing filler (`ExtentBits(group, trailingFiller: !isBitGroup)`).
  The value a bit group presents as an OPERAND is its bit string (`PICTURE 1(m)`): `AsBits()` on the record
  struct is what the boolean MOVE / compare / INSPECT paths read; the packed image stays the file/REDEFINES form.
  ⛔ **`AsBits()` is a PLACEMENT, not a concatenation** (`GroupImageCodec.BitAreaOf`, kb/Work PB207): each member
  sits at `BitLayout.StartBitWithin`, with §8.5.1.6.3's implicit-filler zeros between and after, for the group's
  whole `ExtentBits`. Concatenating the subordinate bit strings — and taking their SUM as the width — is right
  only while every member is "immediately following … an item of the same level"; a member whose LEVEL NUMBER
  differs starts at the next byte, and `01 BG GROUP-USAGE BIT. 05 B1 PIC 1(2). 03 B2 PIC 1(2).` measured
  `DISPLAY BG` = four positions against `FUNCTION LENGTH(BG)` = ten, the two reading the same group through
  different laws. The compile-time image seed (`GroupImageCodec.ImageInitOf`) rides the same composition, which
  is what makes a bit group's REDEFINES backing agree with its own `AsImage()`. A national group's
  operand value IS its `AsImage()` (UTF-16 national positions).

**Diagnostics (next free codes from session-probe):** SR1 — the clause on a non-group, a strongly-typed group, or
a variable-length group; SR2/SR3 — an explicit USAGE on the subject; a subordinate elementary item of the wrong
usage/category (a DISPLAY item inside a national group; a non-boolean inside a bit group); a signed numeric
subordinate of a national group without SIGN IS SEPARATE; a subordinate group whose OWN GROUP-USAGE disagrees.
Each ONE descriptor, positioned at the entry through the PB82 cursor.

**Grammar / edition:** `GROUP_USAGE : 'GROUP-USAGE'` (a hyphenated keyword token like CLOCK-UNITS/B-AND);
`groupUsageClause : GROUP_USAGE IS? (BIT | NATIONAL)` as a `dataDescriptionClause` alternative; `GROUP_USAGE`
admitted to `cobolWord` as a user word BELOW 2002 and listed in the funnel's `CheckedTokenTypes` so a ≥2002 use
as a name is 0901'd (the B-AND / SHARING precedent); `constructs.json` row `group-usage-2002` (introducedIn 2002;
VCR row) with the ParseArm gate on `VisitGroupUsageClause`; the edition-gate sweep (the 85 corpus keeps parsing).

**Consumers to switch to `OperandPic` (the sweep list, each with its rule):** `IntrinsicResultType.OperandCategory`
(the National/Boolean arms are already written against a group Pic — they become live), `ClassOfPlace`,
`RefModPlace.Category` (§8.4.3.3.3 SR1 last sentence), `ReferenceResolver.RefModExclusion`, `MoveBinder`'s
`MoveReceiverCategory` + the sender category (Table 16 national row), `Table16Operand.Of(Place)`,
`ConditionBinder`'s comparison class, `InspectBinder`/`StringBinder`/`UnstringBinder` operand screens,
`ItemLength.Positions` (r1/r2 — drop the `IsElementary` guard, read `OperandPic`),
`IsStringOperand`, `DISPLAY-OF`/`NATIONAL-OF` argument screens, `DataBinder`'s §13.18.49.3 SR9 SIGN guard (the
"GROUP-USAGE is not modeled" branch becomes the real check),
⛔ **`PlaceRenderer.Write`'s reference-modification BOOLEAN PAD and `MoveEmitter`'s `RefModSlice` FIGURATIVE
FILL** — the two WRITE-SIDE sites this sweep list ORIGINALLY OMITTED, and that omission is the origin of
[[PB173]]. Both read raw `Pic`, which is **null for any group**, so a bit-group slice padded and filled with
SPACE where §14.6.8.6 requires the value be "transferred … into the corresponding boolean positions of the
receiving data item, with zero fill or truncation to the right". One rule, two arms, nine files apart, and the
list named only the classifier (`RefModPlace.Category`) that made the units disagree.

⛔ **And the CONDITION-NAME path — five more sites the list omitted, the origin of [[PB728]] + [[PB575]]
(LANDED).** The list named "`ConditionBinder`'s comparison class" and stopped there, so the level-88 path kept
raw `Pic` in both phases and in both directions. ISO §8.8.4.5.3 2) routes a condition-name test through the
relation-condition rules, and §8.8.4.2.1's group sentence — "A national group item or a bit group item shall be
treated as an elementary national data item or an elementary bit data item, respectively" — is what those rules
then say, so the SUBJECT'S CLASS selects §8.8.4.2.9 / §8.8.4.2.8 / §8.8.4.2.7 for a group exactly as for an
elementary item. The five:

- `ConditionRenderer.RenderMembershipTest` — the COLLATING SEQUENCE and the boolean `pad: '0'`. Measured: a
  `GROUP-USAGE NATIONAL` 88 rendered a national image (`.AsNat()`) weighed on the ALPHANUMERIC table and answered
  the OPPOSITE of its elementary twin; a `GROUP-USAGE BIT` 88 lost §8.8.4.2.8's right zero extension. The category
  is now classified ONCE in `RenderCondition88` (through `StringCategoryOf`, which reads `OperandPic`) and passed
  down, so the operand's TEXT and its comparison RULES can no longer disagree about what the operand is.
- `ConditionRenderer.StringMembershipValue` — the figurative WIDTH. `Pic?.Length ?? ImageWidth` fell back to the
  group's STORAGE unit, so `88 GB-ALL1 VALUE ALL B"1"` on a 3-bit group repeated to ONE position; §8.3.3.6.4 GR2
  wants the as-if PICTURE's character-position count.
- `ConditionRenderer.IsNationalOperand` — a private two-case copy of the class rule, feeding the `EVALUATE … THRU`
  range membership and the `__CLASSIFY` argument. ⚠ Its identical twin `IntrinsicRenderer.IsNationalArg` (the
  UPPER-CASE / LOWER-CASE `__CLASSIFY` argument) survived this sweep because the drift rule was scoped to one
  FILE; [[PB760]] deleted both and routed §12.3.6.4 GR7's two consumers through ONE selector,
  `ObjectComputerEmit.ClassificationArg`, and widened the semgrep rule
  `cobolnet-condition-category-from-raw-picture` to every file that renders a `__CLASSIFY` argument.
- `DataBinder.BindCondition` — all four Format-3 guards (the §13.18.63.3 SR29 boolean THROUGH ban, the national
  THROUGH stage, and both category funnels). [[PB575]]: the group leg slipped every one of them.
- `SetEmitter.EmitSet` — the WRITE side, §14.9.39.4 6), which names the population outright: "when the conditional
  variable is an alphanumeric group item, bit group item, or national group item …". All three fell to the loud
  default; the ordinary-group arm emitted a bare struct assignment from a string and did not even compile.
  ⚠ **That site no longer reads a picture at all** ([[PB560]]): GR6 places its literal "according to the rules
  for the VALUE clause", so the store is now `ValueInitializer.InitializerFrom`, and it is THAT method which
  reads `OperandPic` (with §13.18.63.3 SR4's description for an ordinary group — alphanumeric literals bounded by the size of the group item). The rule PB728
  established is unchanged — it simply lives at the one recipe now instead of at one of its three callers.

`ConditionRenderer.cs` now has **zero** raw picture reads, held there by the semgrep invariant
`cobolnet-condition-category-from-raw-picture`; the behavioural twin is
`Cobol.Net.Tests.Unit.ConditionNameSubjectClassDriftTests`, which asserts that the group and elementary forms of
a class render the same comparison AND draw the same screens.

**Reference modification of a bit group — `BitImagePlace` ([[PB173]], LANDED).** A bit group ref-modifies in
**BIT POSITIONS**, and that is a chain of three rules: §8.4.3.3.3 SR1's last sentence ("For reference
modification, bit group items and national group items are treated as elementary data items") admits it AS AN
ELEMENTARY ITEM rather than through the group bullet; §13.18.29.4 GR1b makes that item `PICTURE 1(m)` of usage
bit; §8.4.3.3.4 GR5a then states it outright — "If the usage of identifier-1 is bit, positions used in
evaluation are bit positions". So `ReferenceResolver` wraps a bit group in `BitImagePlace` (rendered
`AsBits()`/`FromBits(…)`, the pair `GroupImageCodec.EmitBitMethods` already emits — no new runtime code) instead
of `GroupImagePlace`, and because `RefModPlace` sits OVER it, the start/length index the boolean string directly:
GR5a is satisfied STRUCTURALLY, and §8.4.3.3.4 GR6's unique item really does keep usage bit / category boolean.
⛔ **A NATIONAL group deliberately keeps `GroupImagePlace`** — §13.18.29.4 GR2b's as-if `PICTURE N(m)` is in
NATIONAL positions and a national group's `ImageWidth` counts its national leaves as `Length` character positions
(only an enclosing ALPHANUMERIC group doubles them — kb/Work PB1665), so its `AsNat()` is its national-position string. The
asymmetry is derived, not an oversight, and it is written into the type's XML doc so the split is not
"simplified" back: two types because they carry two UNITS, which is GR5a's own two-branch sentence.
This also DELETED the PB157 containment it made unnecessary — `ArithmeticBinder`'s 0899 stage, the
`RefModBitGroupSlice` descriptor, and BOTH `ConditionBinder` exclusions that kept a bit-group ref-mod on the
general relation channel. That channel was not merely "byte-consistent": it compared the PACKED image at bit
offsets, so `IF G(1:3) = B"110"` read the wrong characters — a live silent wrong answer, not a deferral. The
golden gap closed with it: this doc's "As built" paragraph below listed ref-mod read+write for
`pb79_group_usage_national` and OMITTED it for `pb79_group_usage_bit`, which is where the legs now live
(including a 12-bit group whose slice spans a packed-byte boundary, and a nested bit group).

**As built (2026-08-18, DEVLOG 1317):** exactly the above, with the emitter half spelled out — a bit group's
elementary face is `AsBits()` / `FromBits()` (`GroupImageCodec.EmitBitMethods`; read through `OperandText`'s as-if
arm and `BooleanRenderer`, stored through `PlaceRenderer.Write`'s as-if arm — the ONE place a MemberPlace that is
an as-if group receives), the packed image stays `AsImage()`/`FromImage()`, and a bit group is a §8.5.1.6.3 RUN
MEMBER inside an alphanumeric group (`PhysicalModel`: run members are bit items; a group member's carrier is
`X.AsBits()`, its distribution `X.FromBits(slice)`, `BitLayout.RunBits` its width as a RUN MEMBER — while its
own internal composition is `BitLayout.ExtentBits`, see the placement note above). A national group's face is its
character image, stored by `WriteGroupImage`. The MOVE dispatch: `MoveClassifier.Kind` sends an as-if receiver
down the Convert path over `OperandPic` (`ConvertSource` reads `target.OperandPic`), and `IsGroupSender` excludes
an as-if sender. Diagnostics: COBOLNET1653 (`GroupUsageRule`) for SR1 + SR2/SR3's explicit-USAGE and
subordinate-conflict halves; the leaf conformance is the shared §13.18.60.3 screen — 0881 for the shapes SR5 /
SR12 / SR20 refuse, and nothing staged: SR12's five picture shapes all bind (D-N7, kb/Work PB646), so a national
group's leaves may be numeric, numeric-edited or boolean as well as national and national-edited, and
§13.18.29.3 SR3's own wording ("Any signed numeric data items shall be described with the SIGN IS SEPARATE
clause") is live rather than hypothetical.
**Goldens:** `2023/pb79_group_usage_national` (LENGTH 5 / BYTE-LENGTH 10; MOVE pad/truncate both ways; national
comparison; INSPECT; ref-mod read + write; a nested national group), `2023/pb79_group_usage_bit` (LENGTH 12 /
BYTE-LENGTH 2; MOVE to bit / display / shorter receivers; MOVE into the group distributing to its leaves; equality;
`B-OR`; a nested bit group; the group-level USAGE BIT leaves; a bit group sharing a byte inside an alphanumeric
group), negatives `pb79-group-usage-explicit-usage` / `-not-a-group` / `-subordinate-conflict` (1653) and `-85`
(0900). Found on the way and fixed with it: **PB95** — `[USAGE IS]` is optional for EVERY usage (§13.18.60.2);
`usageClause` is one `(USAGE IS?)? usageKeyword` alternative (`2023/pb95_bare_usage_keywords`), with the clause's
optional TAILS as siblings of the keyword rather than inside it — `binarySign? noSignPhrase? floatFormatPhrase*`.
That placement is load-bearing, not cosmetic: `DataBinder.UsageKeyword` derives the canonical keyword from the
`usageKeyword` node's text, so a tail written INSIDE it glues (`"FLOAT-BINARY-32HIGH-ORDER-LEFT"`) and lands on
`PictureAnalyzer.ParseUsage`'s internal-error arm — the failure bare `BINARY-CHAR SIGNED` produced historically.
**PB174** added `floatFormatPhrase*` for §13.18.60.2's per-item `endianness-phrase` / `encoding-phrase`; each is
grammatically tolerated after any keyword and narrowed by the binder (COBOLNET1716/1706/1707), the same
parse-wide/bind-narrow posture `noSignPhrase` established. `UsageFloatFormatPhraseDriftTests` reads the keyword
inventory out of the `.g4` and fails when a usage is added without deciding which tails the printed diagram
gives it.

### D21. Floating-point numeric-edited data (the PICTURE symbol E — §13.18.40.4 GR13 b): a numeric-EDITED item whose value channel is EXACT DECIMAL (`CobolDec`), stored as its character image; the form dispatch lives in the four existing edited-store / edited-read choke points. (kb/Work PB66; adopts `docs/rearchitecture/pb66-extfloat-design-draft-2026-08-09.md`, re-derived 2026-08-18.)

*Load-bearing spec anchors: §13.18.40.4 GR13 b (two parts separated by `E`; the significand a numeric or fixed
numeric-edited string with no floating insertion and no zero suppression with replacement; the exponent `+9`,
`+99`, `+999`, `+9999`); §13.18.40.6 (Table 10 is applied to the two strings separately; row `E` admits only
`B 0 /`, `,`, `.`, the leftmost fixed `+ −` and `9` before it); §13.18.40.3 SR12 b (`E` and `.` at most once),
SR15 (1–36 significand digit positions — SR14's 31 does NOT apply), SR23 + NOTE 3 (the significand's `−` and the
exponent's `+` are the one sanctioned two-sign form), SR4 (63 characters); §13.18.40.5 Table 7 (simple, special
and fixed insertion for the significand, none for the exponent) and rule 8 (zero: all digit positions zero, both
signs positive); §14.6.8.4 (normalize so the significand's most significant digit is nonzero; then §13.18.40's
alignment / zero fill / truncation); §14.9.25.4 GR6 item 4 (MOVE: farther than permitted → EC-DATA-OVERFLOW,
content undefined; nearer to zero than permitted → treated as zero) vs §14.7.5 cases 3/4 (arithmetic: both are the
size error condition); §14.9.25.4 GR5 + §14.6.13.2 rule 4 (de-editing; incompatible content → EC-DATA-INCOMPATIBLE);
§8.5.2.13 (category numeric-edited) + §8.5.2.1 Table 2 (class alphanumeric); §13.18.63.3 SR6 (VALUE: a
floating-point literal, or ZERO / the zero forms); §15.43.4 r1 / §15.58.4 r1 + §8.8.4.4.4 GR3 l
(HIGHEST/LOWEST-ALGEBRAIC's well-formedness on the entry, keyed on the arithmetic mode's intermediate); Annex A.3
item 1 c/d (the 31-digit / 3-digit latitude is available only to a processor supporting none of the standard
float usages / arithmetics — WiseOwl COBOL supports FLOAT-BINARY-* and STANDARD-DECIMAL, so the FULL range is
mandatory).*

**Decisions (the draft's D-EF1..D-EF9, adopted):** (1) storage is the existing numeric-edited `string` image —
no new `PicCategory`, no new CLR carrier; every character-position surface works unchanged. (2) the full range:
significand 1–36 digits, exponent 1–4. (3) the VALUE channel is EXACT DECIMAL — the de-edited value is a
`CobolDec (Int128 Sig, int Exp)` on the existing `NumX.Dec` lane, never a `double` (a 36-digit significand cannot
round-trip binary64); `NumX.Dec` now means "a `CobolDec`-carried exact value with its own exponent — a
standard-decimal intermediate OR a floating-point numeric-edited item's de-edited value". (4) `PicInfo` gains ONE
flag, `IsFloatEdited` — the sole discriminator (`Scale`/`DigitPositions` are 0 for the form; `EditMask` keeps the
full string); the parsed shape is NOT a second compile-time model — every reader that needs the significand's
digits / scale / sign or the exponent's width (the VALUE checks in `DataBinder`, `IntrinsicBinder`'s algebraic
fold, the runtime itself) parses `EditMask` through the ONE runtime parser `CobolEdit.FloatMask.Parse`. (5) the
form dispatch lives in the choke points — `RuntimeApi.EditFormatFor` / `EditTryFormatFloat` / `EditComposeFloat`
(the emitter's edited STORE entries: MOVE, ACCEPT, DISPLAY, STRING, the arithmetic resultant, VALUE) and the edited
READ (`NumericRenderer.FieldNum`) — never at the call sites. (6) the runtime (`CobolEdit.Float.cs`) gains the
parsed-mask value type `CobolEdit.FloatMask` (parsed per call from the picture literal the generated code already
carries — no per-item static field; the parse is a few dozen characters and the image store dominates) and the
entry points `FormatFloatMove(value, picture, …)` (an Int128+scale, a `CobolDec` or a `double` sender — the MOVE
disposition), `TryFormatFloat(…, out image)` (the arithmetic store — false on either divergence),
`DeEditFloat(image, picture, …) → CobolDec`, `FloatExtremeImage` (the pinned overflow image, also the
HIGHEST/LOWEST-ALGEBRAIC value's image). (7) store = normalize EXACTLY (integer arithmetic to the
mask's integer-digit count, truncating), then render — no float on the store path (a binary64 sender enters
through `CobolDec.FromDouble`). (8) the MOVE overflow "undefined" content is PINNED to the saturated extreme image
(all-nines significand at the maximum exponent, the value's sign) — a CONFORMANCE.md determination; MOVE
underflow is the rule-8 zero image with no exception; the arithmetic store raises the size error on BOTH
(receiver unchanged). (9) the §15.43.4 / §15.58.4 r1 well-formedness rule is enforced AT THE FUNCTION REFERENCE:
`HIGHEST-ALGEBRAIC` of a mask whose extreme exceeds the mode's intermediate (binary64 under NATIVE, decimal128
under STANDARD-DECIMAL) is a diagnostic; the returned values are the all-nines-significand extremes (LOWEST = 0
for an unsigned mask, as the standard's fixed-point NOTE table shows).

**Clause interactions:** SIGN on the entry is an SR1 violation (no `S`); DECIMAL-POINT IS COMMA swaps `.`/`,` in
the significand exactly as the fixed-point path (`SwapSeparators`); CURRENCY is structurally inapplicable;
BLANK WHEN ZERO wins over rule 8 (spaces — the fixed-point precedent, and §13.18.40.5 rule 10's stated
precedence for its sibling); VALUE admits a floating-point literal or the zero forms (a nonzero fixed-point
literal is an SR6 violation) and initializes through `FormatFloat`; the 2023 EDITING phrase is banned for the form.

**Test plan:** the analyzer's negative legs (one per rule: two `E`, a bad exponent, each banned significand
symbol, >36 digits, no `9`); the positive masks; MOVE store goldens (normalization, zero, sign forms, DECIMAL-POINT
IS COMMA, overflow → EC-DATA-OVERFLOW + the pinned image, underflow → zero); arithmetic store goldens (both
divergences raise SIZE ERROR); de-edit goldens (float-edited → numeric / numeric-edited / alphanumeric,
incompatible content → EC-DATA-INCOMPATIBLE); HIGHEST/LOWEST-ALGEBRAIC per mode; the edition gate at 85; VALUE.

**As landed (DEVLOG 1322, 2026-08-18):** `PictureAnalyzer.AnalyzeFloatEdited` (COBOLNET1658 per violated rule —
Table 10 rendered from PDF pages 489–490 to confirm the OCR'd row E); the 2002 introduction gate keys on
`IsFloatEdited` through the ONE `VersionConformancePass.PictureConstructId` (the forest, report printables and the
report SUM-counter Analyze alike — since kb/Work PB492 that ONE function also keys national-edited off the
category-national picture's own `EditMask`, and `PicInfo.SkeletonGate`, which existed only to carry the recovered
national-edited identity forward, is gone); the
EC-DATA-INCOMPATIBLE flag / fatal ambient gate; COBOLNET1659 (§13.18.63.3 SR6's literal-FORM rule, BOTH
directions, decided once in `DataBinder.ValidateValueCategory`, which every item VALUE reaches through the ONE
screen `DataBinder.ScreenValueLiteral` — format 1's `RawValue` and format 2's per-occurrence literals alike — and
which the level-88 arms call directly) and COBOLNET1660
(§15.43.4 r1 / §15.58.4 r1); `ValidateNumericValue` serves the numeric-edited subject (SR6's "no truncation of
digits or sign", SR3's sign representation — for the floating form the significand's own sign symbol);
`ValueInitializer.EditedImageOfNumericValue` is the ONE compose of a numeric-edited item's numeric VALUE image,
shared by the initializer and the level-88 membership test (`ConditionRenderer.StringMembershipValue`) so a
numeric-edited conditional variable compares EDITED images (kb/Work PB97 — that compare read the raw literal text
and was silently false for every fixed-point numeric-edited condition-name too); the comma-decimal floating-point
literal `1,5E+3` (kb/Work PB98 — `FLOAT_COMMA_BODY` and its twin set). Goldens `2023/pb66_float_edited_picture`,
`pb66_float_edited_comma_mode`, `pb66_float_edited_algebraic_and_checking`, `pb97_value_literal_form_and_88_edited`;
the negatives listed in kb/Work PB66 / PB97 / PB98. CONFORMANCE.md §3 pins the overflow (unchecked → the
saturated image; checked → the receiver unchanged) and incompatible-content (unchecked → digit-for-digit, a
non-digit as zero) dispositions.

**⛔ SR6 IS ONE RULE WITH TWO HALVES, AND BOTH ARE ASKED IN ONE PLACE** (kb/Work PB921, 2026-09-21). Beside the
literal-FORM half above, SR6 carries an EDITION: "literals in formats 1, 2, and 4 of the VALUE clause may be
numeric" is dated by Annex E.3.3 item 43 ("It is now permitted to allow numeric-edited data items to be assigned
values specified as numeric literals") as a COBOL-2023 addition, so below 2023 the edited image had to be written
as the alphanumeric or national literal §13.18.63.3 SR7 governs. That half used to sit INLINE in
`DataBinder.ScreenValueLiteral`, which only the item VALUE reaches, while the form half sat in
`ValidateValueCategory`, which the item VALUE, the level-88 set AND the group VALUE all funnel through — one rule
in two places, and the two places covered different formats. Measured: at `--std 85` the format-1 spelling was
refused and the format-3 spelling `01 X PIC ZZ9.99. 88 X-TEN VALUE 10.` was ACCEPTED and stored the 2023 edited
image, while a format-4 report entry `03 COLUMN 1 PIC ZZ9.99 VALUE 10.` — a format SR6 NAMES — was accepted and
printed it, because a report entry's VALUE operands pass through neither screen. Both halves now live in
`DataBinder.ScreenNumericEditedNumericLiteral`, asked from `ValidateValueCategory` (formats 1, 2, 3 and the group
VALUE) and from `DataBinder.Reports.cs` once the printable item's picture is settled (format 4). SR6's zero
exemption — "the figurative constant ZERO or ZEROES and the integer and decimal forms of the literal zero" — is
edition-free and is the screen's own complement. **DETERMINATION:** SR6's text enumerates formats 1, 2 and 4 and
omits format 3; the omission is read as naming the DATA-ITEM formats rather than as excluding the condition-name
one, because §13.18.63.4 GR19 gives a condition-name "the characteristics of ... its conditional variable",
§14.9.39.4 GR6 places the literal "according to the rules for the VALUE clause" (i.e. SR6's conversion), and
§13.18.63.3 SR36 carries SR11 — the numeric-edited EDITING rule — into format 5, whose subject is a level-88
condition-name exactly as format 3's is. The strict reading and this one agree below 2023, which is the half the
fix closes; at 2023 the reading that does not reject source is taken. Goldens
`2023/pb921_numeric_edited_value_formats` (the over-rejection guard across formats 3 and 4 plus the exempt
spellings) and the negatives `pb921-condition-name-numeric-edited-below-2023`,
`pb921-report-numeric-edited-below-2023`; `NumericEditedValueEditionGateDriftTests` pins the PROPERTY over all
five format spellings at 85/2002/2014 and at 2023.

### D9. OCCURS DYNAMIC (dynamic-capacity tables, §13.18.38 Format 4, COBOL-2014) — an out-of-line growable `CobolDynTable<T>`; sending/receiving direction carried by `Place`; a CORE ships whole, variable-length-group ops staged LOUD.

*Load-bearing spec anchors: §8.5.1.9.1 :8189 (dynamic-capacity definition — physical=logical capacity, current
capacity, non-contiguous implementor-defined allocation, FROM/VALUE initial, TO expected); §13.18.38 Format 4 :19858
(`OCCURS DYNAMIC [CAPACITY IN data-name-3] [FROM integer-4] [TO integer-5] [INITIALIZED]` + ASC/DESC KEY + INDEXED
BY); `ExceptionCatalog` registers EC-BOUND-OVERFLOW(nonfatal)/-TABLE-LIMIT(fatal)/EC-FLOW-SEARCH(fatal).*

**Storage (decided).** A dynamic table is NOT an inline record run (the spec permits non-contiguous occurrences and
adjacent fixed record fields keep their positions, §8.5.1.9.1 :8197). A new runtime class
**`CobolNet.Runtime.CobolDynTable<T>`** (out-of-line, referenced by a field on the owning `record struct`): a growable
`T[] _store`, `int _count` (= current capacity, §8.5.1.9.1 :8189), a `Func<T> _seed`, immutable `_min`/`_expected`
limits; it exposes `ref T` element access (rejected `List<T>` — its value-copy indexer breaks the `ref T` write
contract; rejected `Array.Resize`+a loose counter — no home for the grow/EC/register/seed policy). `T` stays a value
type (element `record struct`) or `string`. This is the singular home for grow-on-receiving, the capacity counter,
the CAPACITY bridge, the SEARCH bound, and every capacity EC.

**Growth (decided; §8.5.1.9.3/.9.4).** TWO paths, direction carried by `Place` (no new sender/receiver plumbing):
`Read()`→sending, `Write(rhs)`→receiving. (1) IMPLICIT — a *receiving* subscript > current capacity grows to it (+
seeds skipped intermediates), no CAPACITY phrase needed; a *sending* OOB subscript is EC-BOUND-SUBSCRIPT (fatal,
§8.4.2.3). (2) EXPLICIT — SET Format 14 (`SET dn {TO|UP BY|DOWN BY} n`, syntactically the existing SET — the binder
re-routes) only if CAPACITY present; may raise/lower. New occurrences seed with the one-occurrence element
initializer (reuse `GroupValueSlicer.ComposedInit`/`ValueInitializer.InitializerFor`; EVERY occurrence — including skipped intermediates — must be seeded) —
satisfies INITIALIZED exactly, and is crash-safe for the "undefined" (INITIALIZED-absent) case.

**CAPACITY register (decided).** A view over `table.Capacity` (no own storage): the binder maps `name → owning
dynamic-table DataItem` (`DataBinder._capacityRegisters`); resolution returns a `CapacityRegisterPlace(table)` whose
`Read()` emits `{tablePath}.Capacity` (synthetic unsigned-integer PicInfo) and whose `Write()` is illegal except via
SET Format 14 (SR30–32). Initial capacity = `FROM ?? 0` (§8.5.1.9.1 :8199). `FUNCTION LENGTH` over a dynamic
table/containing group = `Capacity * elemWidth` (§15.50, not a static width).

**The register's REFERENCE forms (kb/Work PB457).** The register is off `ByName` — it is a view, never storage — so
`ReferenceResolver.CapacityRegisterFor` is the END of resolution for its name, not a first attempt. It therefore
judges the reference **as written** rather than testing a parse shape: the base name identifies the register outright
(§13.18.38.3 SR30 first sentence — "Data-name-3 shall not be defined elsewhere in the source element"), and every
remaining question is a syntax rule reported by `CapacityPlaceOf`, never a "not defined".
- The register carries a real `Parent` — the OCCURS entry's parent, SR30's "treated as though implicitly defined at
  the same level as the entry containing the OCCURS clause" — set where it is minted. It is NOT added to
  `Parent.Children`: it takes no record slot.
- **Qualified** (`WS-CAP OF WS-TABLE`, `IN` likewise) resolves through the ONE §8.4.2.2 matcher,
  `DataBinder.QualifierChainMatches`, off that `Parent`. §8.4.2.2.3 SR2 permits qualifying a name that needs no
  qualification; a qualifier that names no context of the register is **COBOLNET2127**.
- **Subscripted** is **COBOLNET2125** — §13.18.38.3 SR31, "Data-name-3 shall not be subscripted."
- **Reference-modified** goes to the ONE §8.4.3.3.3 SR1 screen (`RefModExclusion` → COBOLNET1647): the register is a
  numeric item of an implementor-chosen binary usage, and SR1 admits only usage DISPLAY or NATIONAL.
- **⚠ DETERMINATION — a register on a table NESTED inside another table has no writable reference form**
  (**COBOLNET2126**). Defining such a table is legal (§8.5.1.9.1 item 3: it "may be nested in any combination to the
  same number of levels as a fixed-capacity table") and naming its register is legal, but SR30 places the register
  *inside* the outer table — deliberately unlike the occurs-depending item, which §13.18.38.3 SR20 forces *outside*
  the table it sizes — so §8.4.2.3.3 SR3/SR5 require one subscript per enclosing OCCURS clause while SR31 forbids
  any. The reading REJECTED was the occurs-depending analogy, "the bare name means every occurrence's capacity": it
  has no textual support and contradicts the per-outer-occurrence capacity model §15.3's `table(ALL)` enumeration
  already rests on (kb/Work PB62). `CapacityRegisterReferenceDriftTests` pins the one lookup path, the written-
  reference read, the fault/diagnostic pairing and the SR30 `Parent`.

**Group image (decided).** A dynamic table is NOT image-capable (`IsCharacterImage`/`IsImageCapable` return false) —
a containing group drops out of the STATIC record codec exactly like the Tier-C float/COMP-5 island; the element
`record struct` keeps its own AsImage/FromImage, and a SUBSCRIPTED element is an ordinary fixed-length group operand
(§8.5.1.12.1 — the dynamic axis is the table's): `Place.ImageCapable` answers `ElementImageCapable` for a
`DynTablePlace` over the table entry, so DISPLAY / MOVE / ref-mod / CALL of `T(n)` use the element image (kb/Work PB189). A containing group instead carries a
**CURRENT-EXTENT** codec, `AsVarImage()`/`FromVarImage()` over `CobolVarGroup` (gated on
`DataItem.CurrentExtentImageCapable`; kb/Work PB204) — the §8.5.1.12 model as a wire form: the fixed run with every
variable component collapsed to nothing, plus each component's current content in order. **That codec, not a byte
image, is what every whole-group operation the standard admits over such a group now runs on** — the §14.8.2.2 /
§14.8.3.2 activation-boundary crossing, since kb/Work PB393 §14.9.25.4 GR9's MOVE, and since kb/Work PB1467 the
§8.8.4.2.17 RELATION: `PlaceRenderer.VarGroupCarrier` (the ONE reader of a group paired with a variable-length group,
shared by the MOVE and the comparison) decomposes both operands and `CobolVarGroup.Compare` walks them at the
variable side's component offsets — fixed material and component pairs in order, each a §8.8.4.2.7 comparison with
space extension (a dynamic-length item at its current length, a table element-wise with the larger table's remainder
against spaces, §14.6.9.3). The relation is screened at bind by §8.5.1.12.1 through the MOVE's own reader
(`VariableLengthCompatibility.PairRefusal`) → **COBOLNET2492**. Only the operations that genuinely need a FIXED record
window (WRITE/RELEASE) and the shapes the composer cannot reach (CONFORMANCE.md A.1 item 57) stay staged LOUD.

**The component model of a variable-length group (kb/Work PB244 shape (b)).** `GroupImageCodec.VarParts` is the ONE
classification of a variable-length group's members, read by the DISPLAY/WRITE composer (`CurrentImage`), the
boundary carrier (`AsVarImage`/`FromVarImage`) and the record layout (`CobolContiguousLayout`), and
`DataItem.CurrentExtentComposes` is the matching gate, so the format on screen, the crossing and the file image
cannot disagree about a member. The member kinds: **Fixed** (the member-image law), **DynLeaf**, **DynTable**,
**Nested** (a scalar variable-length group, flattened in place), **NestedTable** (a FIXED-OCCURS table of
variable-length elements: `Occurs` flattened copies, a compile-time multiplicity — also `Atoms`' unrolling in the
§8.5.1.12 correspondence and the per-occurrence `FUNCTION LENGTH` walk), **OdoFixed** (a member of fixed image
that holds the group's OCCURS DEPENDING table as its trailing storage, §13.18.38.3 SR22) and **OdoTable** (that table
when its ELEMENTS are variable-length groups: `NestedTable` at a count that is a parameter - the first `__odo`
occurrences' carriers at a statement, all of them at a boundary - with the maximum's fixed width and component
count, so every earlier component keeps its position; the receiving `FromVarImage` loops to the maximum and a
slice the carrier did not carry is the §14.9.25.4 GR9b excess part; `Atoms` unrolls it at the maximum). A
statement's RECEIVING operand uses the table only to the count §13.18.38.4 GR8 a) names (`FromVarImage`'s `__odo`,
supplied by `PlaceRenderer.WriteVarGroupImage(receivingOperand: true)` through the same `UsesCurrentExtent` law the
fixed-length receiver asks; `OdoFixed` splices the member's current image over the unused occurrences, `OdoTable` loops
to the count, and a cell-backed window leaves them and their components alone - `StorageCell.StoreVarGroupAt`); the
maximum is GR8 b) (a depending item inside a receiving group), the activation boundary and a record read back. The OCCURS DEPENDING count
reaches the struct as the `__odo` parameter of `CurrentImage` / `AsVarImage` / `CurrentExtents` (a group that holds
the table; `GroupImageCodec.OdoParameter`), supplied by `PlaceRenderer.PeelOdo` from the operand's `OdoGroupPlace`:
the clamped CURRENT count for a statement's operand (§13.18.38.4 GR8), the MAXIMUM at an activation boundary
(`VarGroupBoundaryImage`; §14.8.2.2). The boundary carrier holds the table in its fixed run (a shorter fixed run
when the count is smaller); the record layout makes it the LAST variable-length component (`OdoTail`:
`CobolContiguousLayout` converts at its two doors with `CobolVarGroup.SplitTail`/`JoinTail`), because the record's own
length — not a constant — says how many occurrences it holds.

**A cell-backed group holds the table too (kb/Work PB244).** An EXTERNAL / ADDRESS-OF-taken group that holds a
dynamic-length item AND the OCCURS DEPENDING table composes through the cell helpers (`StorageCell.VarGroupAt` /
`ContiguousAt` / `ContiguousExtentsAt` / `StoreContiguousAt`) the way a declared group does: the window reserves the
table at its maximum as the last of the fixed run, `VarGroupWindow.Odo` is its element width and maximum, the
runtime's `CellOdoTail` carries it beside the component layout (the layout's last component, `OdoTail: true`,
so the take step never hands the table's positions to a dynamic item before it), and the operand's count
(`PlaceRenderer.PeelOdo`, as for a declared group) cuts the fixed run to the current extent.
A table of variable-length ELEMENTS in a cell is composed the same way: the cell numbers a table's components per
occurrence (`CellComponents`), `CellOdoTail.Comps` says how many one occurrence holds (the table's are the group's last
`Max * Comps`), and the occurrences beyond the count drop their components with their fixed run
(`CellOdoTail.CutComponents`; the carrier `VarGroupAt` truncates its component list the same way). A dynamic-capacity
table's elements are element cells that are a scope of their own, so an element that is itself a variable-length
group is composed with ITS window shape (`CellGroupShape`, built by `VarGroupWindow` from the same
`CellComponents` walk and passed beside the group's layout) - one level down as many times as the tables nest.

**Three uses of a variable-length group's current extent (kb/Work PB244).** `DataItem.CanCompose(CurrentExtentUse)` is the
one spelling of which capability a consumer asks, and the three are nested: the **one-way image**
(`CurrentImageCapable` - DISPLAY and FUNCTION LENGTH / BYTE-LENGTH through `BoundIntrinsicCall.OverCurrentImage`),
the **component carrier** (`CurrentExtentImageCapable` - the activation boundary, MOVE, comparison, INITIALIZE,
CORRESPONDING) and the **record** (`RecordImageCapable` - WRITE / REWRITE / RELEASE, READ / RETURN, a sort or indexed
key). The image alone is well defined for every table of variable-length ELEMENTS: each occurrence's own current
image, in occurrence order, up to the current count (§13.18.38.4 GR8) or capacity (§8.5.1.9.1), for a declared group
(`VarPartKind.OdoTable` through `CobolTable.ConcatImages`; a dynamic table's element lane calls the element's own
`CurrentImage`) and for a cell-backed one alike. The carrier takes an OCCURS DEPENDING table of such elements
(`VarPartKind.OdoTable`, above) - a RUN-TIME multiplicity that is only a parameter, because the table is the group's
trailing storage - but not a DYNAMIC-CAPACITY table of them: that component is one string, which cannot be split back
into elements that each hold components of their own, so the carrier would need nested element carriers (the standard
does define the operation: §14.6.9.2 moves "correspondingly numbered elements" by the rules of MOVE, §14.6.9.3
compares them element by element). A RECORD cannot take an OCCURS DEPENDING table of them either: a record read back is
decomposed by a FIXED list of components, and the record's own length cannot say how many occurrences it holds
(the DEPENDING item is data, not layout), so those two stay the named loud (`TierCIsland.Reason`).

**CORE ships whole:** declaration (all phrases, each once and in the printed order — §13.18.38.2 with §5.2.1, refused otherwise by `DataBinder.NarrowToDataDivisionFormats`, **COBOLNET2789**, kb/Work PB1265) · out-of-line growable storage · CAPACITY
read + SET Format 14 write · implicit + explicit growth · INITIALIZED seeding · bounds/capacity ECs
(EC-BOUND-SUBSCRIPT/-OVERFLOW/-TABLE-LIMIT/-SET, EC-FLOW-SEARCH via a per-table `_inSearch` guard) · SEARCH/SEARCH
ALL over current capacity · `INITIALIZE <dynamic-table>` · **`INITIALIZE` of a group CONTAINING one** (§14.9.20.4
GR10 — the per-occurrence loop over the current capacity, capacity unchanged) · **whole-group `MOVE` of a
variable-length group** in either direction and to or from a compatible FIXED group (§14.9.25.4 GR9 over the
§8.5.1.12 component carrier, screened at bind by §14.9.25.3 SR9 → **COBOLNET1931**) · **the relation condition over
a compatible pair** (§8.8.4.2.17, screened by §8.5.1.12.1 → **COBOLNET2492**) · **the CORRESPONDING verbs
over an occurs-depending group** · the 2014 edition gate + matrix/VCR rows. **Staged LOUD
(diagnostic, not a silent wrong answer):** whole-group image of a containing group in the statements that need a
FIXED record window — WRITE/RELEASE (§14.6.9; the two shapes CONFORMANCE.md A.1 item 57 excludes - a dynamic-capacity
table of variable-length elements as a MOVE / comparison / CALL operand, and a record holding an OCCURS DEPENDING
table of them - stay loud) · ref-mod of a
subordinate (**1526**, §13.7.1 SR6) · REDEFINES **object** carries an OCCURS clause of any format, dynamic included
(**1701**, §13.18.44.3 SR5 sentence 1) · REDEFINES **subject** IS a dynamic table (**1525**, §13.18.44.4 GR1 +
§8.5.1.9.1 — the one side no syntax rule names) · REDEFINES either side a variable-length group, i.e. with a
dynamic-length item or dynamic-capacity table SUBORDINATE (**1698**, §13.18.44.3 SR17 + §8.5.1.12.1).

**Grammar (SHARED .g4 → FULL legacy guard; additive).** A new `CAPACITY` lexer token; a DYNAMIC alt on
`occursClause` (LL-disjoint — DYNAMIC is not an integerLiteral; superset parse — no edition predicate) with an
SUPERSET `occursDynamicPhrase*` (any order and number, narrowed at bind time — see above); the COBOL-2014 introduction gate is the bind-time
`ConstructRegistry.Check(OccursDynamic2014)` at `OdoModel.OdoBindOccursSpec` → **COBOLNET0900** (the
`VersionConformancePass` funnel at end state — `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`).
NO SET grammar change (Format 14 is the
existing SET syntax, binder-rerouted). Diagnostics: **1522** (declaration/placement/SR28 — FILE SECTION, ODO-nesting,
FROM/TO bounds, dup phrase), **1523** (CAPACITY register misuse SR30–32), **1524** (SET Format 14 misuse), 1525–1527
(staged-loud). (08xx band exhausted; 15xx, last-used 1521.) **COBOLNET1528 was DELETED** with the PB500 landing —
the rule it cited, §13.18.63.4 GR16, is a FORMAT 2 general rule and never reached the FORMAT 1 VALUE it refused;
its code is never reallocated.

**Implemented (the CORE increments):** (1)
grammar (`CAPACITY` token + the `OCCURS DYNAMIC occursDynamicPhrase* …` alt, superset-parsed and introduction-gated
by `VersionConformancePass`, no parse-time predicate) + `OccursSpec`
dynamic fields + `DataItem.IsDynamicTable`/`IsTable`/`FieldType` (+ image-capable exclusions) + `CobolDynTable<T>` +
`FieldInit` dynamic branch + `OdoBindOccursSpec` Format-4 branch + `EditionGateHints` gate + the matrix row
(`occurs-dynamic-2014`, active) — the ONLY grammar/legacy-guard slice → golden `dyn_declare` (a group-element table,
greenfield-only). **Two plan refinements:** (a) NO VCR row — the VCR's own preamble (line 20)
states 2002→2014 *introductions* are captured by the matrix `introducedIn` tag, not the VCR (which carries only
2014→2023 Annex-E deltas + a few 2002→2014 behavior rows); the `constructs.json` + `ConstructDialectStatus` pair IS the
canonical introduction record. (b) NO separate `dyn_pre2014` corpus golden — the corpus runner asserts compile+run
SUCCESS only; the below-2014 **COBOLNET0900** rejection is asserted by the matrix row's `expectDiagnostic` at editions
85/2002 (`VersionMatrixTests`), the one place negative gating belongs. `DynamicResolve` (CAPACITY-register/access
resolution) belongs with increments 2/3 where the register becomes a readable item;
(2) CAPACITY register read + SET Format 14. The register is a `CapacityRegisterPlace`
(a VIEW over `{tablePath}.Capacity`, synthesized in a `DataBinder.DynamicResolve` post-build pass, indexed by
`DataBinder.CapacityRegisters`, resolved via an early `ReferenceResolver.Resolve` hook + the new
`ReferenceResolver.TablePath` whole-table-path helper); an unsigned-integer native-binary `PicInfo` so numeric reads
hit the plain scale-0 branch, its `NumProfile` emitted (not a field) for the DISPLAY/`FormatDisplay` path. SET Format
14 (TO/UP BY/DOWN BY) reroutes in `BindSetTo`/`BindSetUpDown` on a `CapacityRegisterPlace` first target →
`BoundSetCapacity` → `SetCapacity`/`CapacityUpBy`/`CapacityDownBy`. Diagnostics: **1523** (register as an ordinary
receiver — the `ResolveReceiving` chokepoint; and the SR30 implicit-definition name collision in `DynamicResolve`),
**1524** (SET F14 with a second/mixed target). A whole (unsubscripted) dynamic-table reference fails LOUD (a
`PlaceForItem` guard) rather than emitting a bare `CobolDynTable<T>` object. Goldens `dyn_capacity_read`/
`dyn_capacity_set`/`dyn_capacity_bounds` (greenfield-only). **Landed since (P13):** (a) **EC-BOUND-OVERFLOW** on exceeding the
TO/expected capacity is NONFATAL and checking-gated — the raise is LIVE (`ExceptionEngine.BoundOverflowError` via the
ambient `BoundOverflowChecking` statement gate; first crossing only, §8.5.1.9.6 GR1); with checking off (the default)
the operation continues either way, so the runtime `_expected` enforcement + the `>>TURN … CHECKING ON` gate
ride the EC-integration pass (`dyn_capacity_bounds` proves the checking-off continue). (b) **FUNCTION LENGTH over a
dynamic table** (= `Capacity × elemWidth`) + the `DynWholeTablePlace` whole-table place + its value-funnel loud belong
with the §14.6.9 **1527** whole-/variable-length-group work (one home for whole-dynamic-table operations); the clean
whole-table loud guard is the interim. (3) subscripted element access. A single `MemberPlace` path cannot carry
read-vs-write polarity (`ref T` covers the fixed case only), so an `AccessDir { Sending, Receiving }` is threaded
through `AccessPath`, and a `DynTablePlace(SendingPath, ReceivingPath, Item)` whose `Read()` emits `…RefSending(occ)`
and `Write(rhs)` emits `…RefReceiving(occ) = rhs;` (the receiving side grows-and-seeds skipped intermediates). Arity
recognition uses `IsTable` at the OCCURS-level count; the dynamic segment renders the accessor and any group-
field/fixed-OCCURS tail is appended after it (`{tbl}.RefSending(i).Field`). A sending occurrence outside
1..current capacity (§8.5.1.9.2 — "the same as for a fixed-capacity table whose number of occurrences is the current
capacity") and a receiving occurrence below one set EC-BOUND-SUBSCRIPT exactly as `CobolTable.At` does, then continue
through the benign scratch slot with checking off (kb/Work PB1268). A FIXED OCCURS DEPENDING level's
`FixedTableSegment` likewise carries §13.18.38.4 GR7's reference-time bound check (`OdoReferenceCheck` →
`CobolTable.At(path, index, count, min, max)`) when the compilation group can enable EC-BOUND-ODO. → `dyn_implicit_grow`/`dyn_initialized`; (4) SEARCH/SEARCH ALL over
current capacity + INITIALIZE. SEARCH: the table guard accepts `IsTable` (dynamic too), `OdoModel.SearchBound` gains a
dynamic branch returning `{tablePath}.Capacity` (a run-time bound, threaded via `BoundSearch.DependCount`), and
`BoundSearch.DynTable` carries the table path so `EmitSearch` brackets the scan in
`{tbl}.EnterSearch(); try { … } finally { {tbl}.ExitSearch(); }` — a SET Format 14 on that same table WHILE searching
raises EC-FLOW-SEARCH (GR31). INITIALIZE: per §14.9.20 GR10 (":28023") all
occurrences up to current capacity are initialized by the INITIALIZE statement's OWN stores (the CATEGORY DEFAULTS /
REPLACING / VALUE-phrase), NOT the OCCURS grow-seed, capacity unchanged (a naive re-seed with the VALUE-inclusive
image would be WRONG for a VALUE element). Implemented as an `InitializeDynLoop(var,
{tablePath}.Capacity, body)` (a RUN-TIME-bounded loop, sibling of `InitializeLoop`) over a `DynElementCursor` (the shared `PlaceCursor`)
that yields a `DynTablePlace` (writes via `RefReceiving`, within bounds so no growth). A group CONTAINING a dynamic
table (the other GR10 case) is a variable-length group → staged LOUD (the §14.6.9 1527 family). → `dyn_search`/
`dyn_initialize`; (5) the staged-loud
guards. **1522** (`DynamicResolve`): SR28 (:19987) — TO ≤ FROM rejected. **The REDEFINES-over-a-dynamic-table
family is THREE screens, one per rule, not one walk** (kb/Work PB177 arm C and its follow-up; the deleted
`ContainsDynamicTable` subtree walk conflated all three under one citation): **1701**
(`ClassifyRedefinesClasses`, per WRITTEN ENTRY, `RedefinesTarget.IsTable`) — §13.18.44.3 SR5 **sentence 1**, "The
data description entry for data-name-2 shall not contain an OCCURS clause", which reaches EVERY format including
Format 4, so a dynamic table cannot be the REDEFINES OBJECT (nor can a plain `OCCURS 4` entry, which had been
screened nowhere and compiled clean); **1698** (per written entry, `Sr17Shape`) — §13.18.44.3 SR17 for either side
being a variable-length group per §8.5.1.12.1, i.e. with a dynamic-length item or dynamic-capacity table
SUBORDINATE; **1525** (the class loop, narrowed to a NON-canonical member) — the SUBJECT that IS itself a dynamic
table, the one shape in the family no syntax rule names, decided by §13.18.44.4 GR1's fixed association area
against §8.5.1.9.1's capacities that "may vary during execution" (§13.18.44 NOTE 3 permits REDEFINES only
SUBORDINATE to a dynamic table). Each forces the class `Rejected`. **1527** (the containing-
group INITIALIZE `InitializeErrorAction` message; the whole-dynamic-table value op stays a runtime `NotImplemented`
loud) — the §14.6.9 variable-length-group family. **1526 is unnecessary and omitted:** reference modification of a
dynamic-table element works correctly (a `RefModPlace` over the
`DynTablePlace` — `WS-E(i)(1:2)` gives the right substring), and the cited "§13.7.1 SR6" restriction is actually the
§8.4.3.11.4 ADDRESS-OF/bit-item SR6, not a general ref-mod prohibition — so a 1526 guard would over-restrict valid
code. Negative tests: `OccursDynamicGuardTests` (1522/1525 + the positive companions, which now include both halves
of the Format 1 VALUE pair — see the §13.18.63 BAND SCOPING paragraph below). **EC-BOUND-OVERFLOW**
is LIVE since P13 (the ambient checking-gated raise); **FULL variable-length-group MOVE/COMPARE** (a bind-time 1527 + a
`DynWholeTablePlace` carrying FUNCTION LENGTH = `Capacity × elemWidth`) remains the flagged follow-on (an EC-integration pass + a
whole-dynamic-table-operations pass); today both are LOUD, never silently wrong.

**Hardening (current invariants).** A whole-GROUP receiving MOVE into a group nested BELOW the dynamic level routes
through `ReceivingPath` (`RefReceiving`) via a `DynTablePlace` arm in `EmitGroupMove`, so it grows rather than
silently losing an out-of-capacity write. `CorrEligible` gates on `!IsTable` (not `Occurs is null`) so CORRESPONDING
never emits member access on a `CobolDynTable<T>` field (§14.7.6 rule 4). SEARCH of a dynamic table nested under a
fixed OCCURS (`TablePath` null) fails LOUD rather than scanning ZERO occurrences (a subscripted-capacity path is a
later increment). OCCURS DYNAMIC in the FILE SECTION is rejected **COBOLNET1526** (§8.5.1.9.1 item 3 — "any place
OTHER THAN the file section"). The SET Format 14 capacity peek uses a PURE `ReferenceResolver.CapacityRegisterFor`
(never `refs.Resolve`, which would route an OO `prop OF obj` first target through the property hook and enqueue a
spurious pending op).

**⛔ §13.18.63 BAND SCOPING — A FORMAT 1 VALUE ON OR UNDER A DYNAMIC ENTRY CARRIES NO CAPACITY DERIVATION** (PB500).
The VALUE-derived initial capacity is the job of §13.18.63.4 **GR16 alone, and GR16 is a FORMAT 2 general rule**
(band GR11–GR16). §13.18.63 states cross-band application explicitly and in ONE direction only — GR11 ("General
rules 1, 2, 3, 4, 5, 6, 7, 8, and 10 above apply"), GR17, GR21 and GR24 all import FORMAT 1 rules into a later
band, and nothing imports GR12–GR16 back into FORMAT 1. The syntax rules settle it independently: §13.18.63.3 SR22,
the rule that keeps GR16b from having no operand, is itself FORMAT 2 (band SR16–SR23) with no FORMAT 1 counterpart,
so a Format 1 VALUE on `OCCURS DYNAMIC FROM 2.` would otherwise hit GR16b with no expected capacity to set. A
Format 1 VALUE here is therefore conforming source with a fully determined meaning: the capacity is the ordinary
§14.6.2.3.2 item-6 / §13.18.38.4 GR16 **minimum** (the OCCURS FROM, zero when absent — cross-checked by §8.5.1.9.1,
which gives the same number from the other side), and §13.18.63.4 GR9 plus §13.18.38.4 GR1 (band "FORMATS 1, 2 AND
4") give **every occurrence** the value. `ValueInitializer.FieldInit` already does both with no code of its own,
opening the `CobolDynTable` at `OccursSpec.InitialCap ?? 0` and seeding every occurrence from `DataItem.ValueAt`.
A `COBOLNET1528` refusal stood at `DynamicResolve` for this, reading GR16b's "no TO phrase … in the VALUE clause"
as covering a Format 1 `VALUE IS literal-1`; it rejected legal source, and its two arms disagreed with each other —
the GROUP arm fired only when the OCCURS carried a TO, so `OCCURS DYNAMIC FROM 3.` with subordinate VALUEs compiled
and seeded correctly while `OCCURS DYNAMIC FROM 3 TO 9.` was refused, while the ELEMENTARY arm ignored the TO and
refused both. Golden `tests/conformance/2014/pb500_format1_value_dynamic_table` pins all six shapes, including the
mixed entry where a Format 1 and a Format 2 VALUE sit over one dynamic table and only the latter moves the capacity.

`CobolDynTable` wires **EC-BOUND-OVERFLOW** since P13 through ONE raise, `RaiseImplicitOverflow`, for every
IMPLICIT capacity change (§8.5.1.9.6 1) — the receiving-subscript grow and, since kb/Work PB1144, the §14.6.9.2
recreation `FromCurrentImage` performs for a variable-length group MOVE / boundary transfer, which is its own
primitive and never `SetCapacity`: it builds a new array of the sender's occurrences raised to the FROM minimum with
space-filled elements) through the ambient `BoundOverflowChecking` gate, first crossing only, and a declarative's
RESUME AT NEXT STATEMENT lets the change proceed (§8.5.1.9.6 1); kb/Work PB1269). Growth that the runtime cannot
allocate is EC-BOUND-TABLE-LIMIT with the table unchanged (kb/Work PB1410). New occurrences a statement creates
take the INITIALIZED phrase's §8.5.1.9.5 seed (`SeedRecipe.Initialize`, a second constructor seed; kb/Work
PB1267). **EC-BOUND-SET** (kb/Work
PB460): `SetCapacity` raises it on EVERY SET Format 14 whose new capacity exceeds the expected maximum capacity
(§14.9.39.4 GR30's second arm — no first-crossing exemption), BEFORE the change, through `BoundSetChecking`, which
the EC binder enables precisely on `BoundSetCapacity`. `docs/CONFORMANCE.md` A.4.4 records the two determinations.

**The ELEMENTS of a moved table go through the MOVE statement, not the carrier (kb/Work PB1144).** The §14.9.25.4 GR9
transfer (`CobolVarGroup`, `FromCurrentImage`) moves each corresponding table as element IMAGES, which is right for a
group element pair (GR4: a fixed-length group move is an alphanumeric move "without conversion") and for two
identically described elementary elements, and wrong for every other elementary pair: §14.6.9.2 ends "Correspondingly
numbered elements are moved according to the rules of the MOVE statement", and §8.5.1.12.3 admits any pair of equal
element byte length, so `9(4)` 1234 into `99V99` is 34.00, not the image 12.34. The binder owes the rest, at
`MoveBinder.BindMoveOf` — the one method every explicit and implicit MOVE passes through — as
`BoundMove.ElementMoves`: per receiving operand that GR9 governs (`MoveClassifier.VariableLengthGroupSender`, the
antecedent the emitter's transfer asks too) and per corresponding table pair (`VariableLengthCompatibility
.CorrespondingTables`, the walk that matched them) whose elementary elements differ (`OoConformance
.DescriptionMismatch`), one `TableElementMove`: the elementary MOVE bound by `BindMoveOf` itself over the two element
places at a loop-local occurrence number (reached from each group's resolved place through `PlaceCursor`), so its
§14.9.25.3 legality is asked of the element pair (a refusal is a diagnostic at the MOVE), plus the two §14.6.9.1
current counts (`OccurrenceCounts.Current`, shared with INITIALIZE). `MoveEmitter` renders, after the group transfer
into that receiver, a loop to the smaller count: §14.6.9.2 rule 1's superfluous sending elements are not moved, and
rule 2's remaining receiving elements stay space filled. Golden `tests/conformance/2014/pb1144_dyn_move_elements`;
negative `pb1144-dyn-element-move-illegal`.
Resolved open questions: the VALUE-derived capacity is §13.18.63.4 GR16's, implemented over the whole FORMAT 2
surface by `DataBinder.ResolveTableValues` and reaching no FORMAT 1 VALUE (the band-scoping paragraph above);
EC-FLOW-SEARCH in CORE.

### D17. TYPEDEF + the TYPE clause (§13.18.58 / §13.18.57 / §13.16, COBOL-2002) — a template registry + a subtree clone spliced into the forest at declaration-bind; a FRONT-END + BINDER-ONLY feature (ZERO emitter change).

**Key architectural fact.** A weak TYPE reference is pure MACRO-EXPANSION — the cloned subtree emits exactly as a
hand-written group would (§8.5.3.2 NOTE); a STRONG type is stored IDENTICALLY and adds only COMPILE-TIME checks
(§8.5.3.3). So the whole feature = grammar (one token + one clause) + a template registry + a subtree clone spliced
into the forest during declaration binding + a same-type guard at MOVE/compare. **No emitter change.**

**CORE (ships whole, program/global scope).** (1) A **`TypeDecls`** registry (name → template subtree; case-insensitive,
mirrors `ByName`). The TYPEDEF entry allocates NO storage (not in `Roots`) and its subordinate names are NOT globally
referenceable (§13.18.58.4 GR2/GR1) — the template is built WITHOUT `RegisterName`. (2) **TYPE reference cloning**
(`TYPE [IS] type-name`, the grammar ALREADY exists at `CobolData.g4:253`): a deep clone of the template subtree into
the referencing entry (§13.18.57.4 GR1/GR2), handling elementary + group types, subject-owned OCCURS (array-of-type,
§13.16 SR14) and subject VALUE override (GR3), GR1 exclusions (the type's level/name/GLOBAL/SELECT WHEN/TYPEDEF are
not copied). (3) **STRONG typing** (§13.18.58.2): declaration SRs (§13.18.57.3 SR3/SR4/SR6) + same-type gating at MOVE
(§14.9.25.3 SR2 — a strongly-typed group RECEIVER wants a same-type sender), comparison (§8.8.4.2.3 SR1),
class-condition (§8.8.4.4.3 SR1);
intra-element same-type = template identity (`TypeName` + relative path, §8.5.3 NOTE). (4) **level-88s inside a TYPEDEF** (GR1 — part of the
type): cloned + re-registered. (5) recursion/placement guards. **This also fixes a current SILENT-DROP bug:** at 2002+
`TYPE IS name` parses and is silently dropped (no `typeClause` binder branch) — CORE wires it (unresolved → 1530).

**Mechanism.** `DataItem` gains `TypeName`/`StrongType`. A Pass-0 **`CollectTypeDecls`** (in `BindDeclarations`, before
`BindEntries`) builds each TYPEDEF template (fresh `Uid`s, `Parent`/`Children`, but NO `Roots.Add`/`RegisterName`) and
returns the consumed entries so the main pass SKIPS them. A recursive **`CloneSubtree`/`CloneItem`** (generalizes the
flat `CreateCompilerTemp`, `DataBinder.Oo.cs:362`): a fresh `Uid` per node (CRITICAL — `StructName`/`ProfileName` ride
on it), shares the immutable `Pic`, copies the description fields, re-uniquifies `CsName` in the NEW scope, and DOES
`RegisterName` (clones ARE referenceable, unlike the template). **`ExpandType`** runs inside `BindEntries` right after
an item is placed (so the clone is in the forest BEFORE `BindResolve` — every post-build pass sees it automatically,
the same invariant `CreateCompilerTemp` relies on). STRONG equivalence: `StrongTypeModel.StrongRoot` (walk
to the outermost `StrongType` ancestor) + `SameType(a,b)` — ISO §8.5.3.1's own two alternatives over
`EquivalentTypeDeclarations` — checked in `BindMove` (`CheckStrongMove`) / `CheckedRelational` (the ONE relation
chokepoint) / the class-condition arm (`CheckClassConditionOperand`) / `OoConformance` (the activation boundary).

**Grammar (shared-.g4 rules → FULL legacy guard per change).** `typedefClause : IS? TYPEDEF STRONG? ;` and
`sameAsClause : SAME AS cobolWord ((OF|IN) cobolWord)* ;` on `dataDescriptionClause` (superset parse — no edition
predicates); the TYPE-reference rule pre-existed. The COBOL-2002 introduction gates are the `VersionConformancePass`
parse arms (`VisitTypedefClause` / `VisitTypeClause` / `VisitSameAsClause`, recognition-based) → **COBOLNET0900**
(rows `typedef-def-2002` / `type-clause-2002` / `same-as-clause-2002`). The `AS` token is shared with the
CONSTANT-entry surface (both §8.9-interval words; `AS` is nameSlot-only — the FU-1 ledger). `POINTER TO type-name`
stays out.

**SAME AS (§13.18.49) rides the SAME machinery** — it is the TYPE expansion with a DATA-NAME source:
`DataItem.SameAsName`/`SameAsQualifiers` → `ExpandSameAs` (inside the ONE `ExpandTypes` pass, AFTER the TYPE loop so
targets copy their expanded description; chains recurse, cycles = the expanding-set + subject-ancestor walks). The
copy = the shared `CopyEntryDescription` (also ExpandType's §13.18.58.4 GR3 body; SYNCHRONIZED copies for SAME AS
only — §13.18.49 GR1 has no alignment exclusion, §13.18.57.4 GR1 does) + `CloneItem` with a `levelDelta` (GR2b
subordinate renumbering relative to the subject, may exceed 49 per GR2c; TYPE flows pass 0). GR1 exclusions honored:
data-name-1's level/name/CONSTANT RECORD/EXTERNAL/GLOBAL/REDEFINES are not copied. GR3/GR5: a USAGE/SIGN on a group
containing data-name-1 applies as though specified for the subject (mirrored onto the copied Pic at expansion —
the subject's chain cannot see the target's ancestors). A copied TYPE identity keeps the §8.5.3 anchors
(`SameType` holds across `B SAME AS A` pairs) and re-checks the §13.18.57.3 SR6 strong placement.

**EXTERNAL type declarations (§13.18.22) are LIVE** — a conformance surface + record-external attribution, NOT a
cross-program type registry: `DataItem.IsExternalTypedef` on the template; `ExpandType` enforces GR2 (a data
description containing an external type shall be level-1) and SR5 (an external record of a STRONG type requires the
type external too) → **1558**, and marks GR3 records `ExternalFromType`; `CallBindExternalAndGlobal` re-bases those
roots onto the run-unit `ExternalStore` cell exactly like explicitly-EXTERNAL records (GR6 matching by externalized
name rides the existing mechanism). Cross-source-unit §8.5.3.1 same-type equivalence is LIVE: `SameType` decides it
from the two DECLARATIONS, so two source elements declaring the same type-name are equivalent only when their
STRONG/EXTERNAL presence and their elementary items' positions, lengths and clauses agree (kb/Work PB427).

**Diagnostics (15xx).** **1529** malformed TYPEDEF (SR15 level-1/named; SR1 STRONG-on-elementary; TYPEDEF ×
REDEFINES/BASED/CONSTANT RECORD/PROPERTY); **1530** TYPE unresolved/recursive; **1531** illegal TYPE-reference
context (immediate subordinate/88; disallowed sibling clause; 77-of-group) — its former type-with-INDEXED-BY-used-≥2× arm is retired (kb/Work PB919: every clone declares its own index cells);
**1532** STRONG declaration violation; **1533** STRONG use incompatibility (MOVE/compare non-same-type; strong group
in a class condition — split by descriptor: `strong-move-mismatch`/`strong-compare-mismatch`/`strong-class-condition`);
**1535** `strong-compare-ordering` (§8.8.4.2.3 SR4 — a boolean/object/pointer-bearing
strong group compares for equality/inequality only; the NAMED spec rule; its second descriptor, the staged
RENAMES-in-TYPEDEF refusal, is gone — a TYPEDEF's RENAMES clauses are cloned onto each subject, kb/Work PB1304); **1555/1556/1557** SAME AS subject-entry / referenced-entry / cycle rule families
(§13.16.3 SR12 + §13.18.49.3); **1558** EXTERNAL-type conformance (§13.18.22 GR2/SR5); **0899**
`strong-group-comparison-member` (a strong-group relation that reaches a dynamic-capacity table member, an OCCURS
DEPENDING ON object inside a table, or a REDEFINES storage tier the element walk cannot reach — the residue of the
§8.8.4.2.12 element order below).

**Strongly-typed group RELATIONS compare element by element (§8.8.4.2.12; kb/Work PB1469).** "Each elementary item
of the first operand is compared with the corresponding elementary item of the second operand, in accordance with
the rules for comparison of elementary items and in the order in which the elementary items are specified." The ONE
relation construction site (`ConditionBinder.CheckedRelational`) asks the ONE pair classifier (`RelationPair`,
`Binding/RelationPairShape.cs`: Flat · StrongElementOrder · VariableLengthGroup) and LOWERS a same-type strong pair
(`StrongGroupComparison`) into `BoundRelational`s between corresponding leaves, composed lexicographically
(`a₁ < b₁ ∨ (a₁ = b₁ ∧ …)`, ending FALSE for a strict operator and TRUE for `<=`/`>=`; equality is the conjunction).
Each leaf pair is therefore rendered by the SAME rule a relation written between those two items uses — algebraic
for every numeric usage (float, signed DISPLAY, packed), the collating sequence for character leaves, identity for
pointer leaves — so no leaf kind is special-cased and the next one is automatic. Leaves are reached by the shared
`PlaceCursor` (lifted from INITIALIZE, `Binding/Procedure/PlaceCursor.cs`) from the operand's RESOLVED place, so a
subscripted strong group keeps its subscripts; a fixed OCCURS expands per occurrence, and an OCCURS DEPENDING
occurrence beyond the minimum is guarded by `data-name-1 >= k` (§13.18.38.4 GR8 — one count serves both operands:
outside the group it is one item, inside it SR20 places it before the table so it was already compared equal).
Correspondence is by leaf POSITION (§8.5.3.1 defines equivalence over elementary items, not group structure);
REDEFINES entries are set aside (⚠ determination — the redefined entry's storage is already compared at its
position). §8.8.4.2.3 SR4's equality-only screen asks the §8.5.2.1 class lattice (`IntrinsicArgumentRules.ClassOfItem`),
so all three pointer categories are class pointer.

**Implemented (the CORE increments).** (1)
grammar (`STRONG` token + `typedefClause`; `EditionGateHints.TypedefClause` → 0900; the
`typedef-def-2002` matrix/registry row) + the weak-TYPE spine (`DataItem` `IsTypedef`/`TypedefStrong`/`TypeRefName`;
`BindEntries` routes a TYPEDEF root to `TypeDecls`, off `Roots`/`ByName`; `RegisterTypeDecl` → **1529**; a post-build
`ExpandTypes` at the top of `BindResolve` clones each `TYPE [TO] type-name` (§13.18.57 Format 1 — TO is the optional word) via `CloneItem` — fresh `Uid`/re-uniquified
`CsName`/registered — elementary→copy PIC, group→clone children, forward refs OK; unresolved/recursive → **1530**).
`TypeName`/`StrongType` are populated here (the STRONG checks are increment (2)). — **the ONLY grammar/legacy-guard slice** → goldens
`typedef_weak_elem`/`typedef_weak_group`. (2) STRONG typing (all BINDER-ONLY): the
`DataItem.StrongRoot` walk (outermost `StrongType` ancestor — §8.5.3.1, a group SUBORDINATE to a strong group is
itself strongly typed) + `IsStrongGroup`/`IsStronglyTyped` + the static `SameType(a,b)` — ISO §8.5.3.1 exactly:
declaration EQUIVALENCE (same type-name, same EXTERNAL/STRONG presence, corresponding elementary items at the same
relative position and length with the same ALIGNED / BLANK WHEN ZERO / DYNAMIC LENGTH / JUSTIFIED / PICTURE / SIGN /
SYNCHRONIZED / USAGE clauses), plus the same relative position and length for a SUBORDINATE pair. USE gates → **1533**: `CheckStrongMove` in `BindMove` (§14.9.25.3 SR2),
the strong-group same-type check in the ONE `CheckedRelational` chokepoint (§8.8.4.2.3 SR1 — so it also covers
EVALUATE/PERFORM UNTIL/SEARCH WHEN), a strong-group guard in `CheckClassConditionOperand` (§8.8.4.4.3 SR1). DECL gates
→ **1532**: SR6 at clone time in `ExpandType` (level-1-or-under-strong; also catches SR7's 77→group), SR3/SR4 in a
post-resolution `CheckStrongTypeDeclarations` pass (a RENAMES/REDEFINES over any part of a strong subtree, INTERNAL
template redefines excluded via the shared-strong-root test). Golden `typedef_strong_ok` (same-type whole-record
MOVE+compare byte-verified), `pb427_same_type_by_position` (§8.5.3.1's two alternatives, the comparison arm and a
cross-source-element equivalent pair) + `TypedefStrongTests` (SR2/SR1/class-cond/SR6/SR4/SR3, the position-vs-path
pair, the non-equivalent and equivalent same-named cross-element pairs, the PICTURE conjunct, the §14.8.2.2
argument sentence, and a clean companion). (3) level-88 condition-names inside a TYPEDEF (§13.18.58.4 GR1):
`DataItem.Own88s` (the item's own 88s), `BindCondition(…, registerGlobal: !rootIsTemplate)` keeps a template's 88s OFF
the global by-name index (GR1), and `ExpandType`/`CloneItem` call `CloneConditionOnto` to clone them onto each
reference (registered globally — clones ARE referenceable). Golden `typedef_88` + `TypedefConditionTests` ×2.
(3b) level-66 RENAMES inside a TYPEDEF (§13.18.58.4 GR1, kb/Work PB1304) — the same shape for the other member of the
GR1 sentence: `BindEntries` tracks the template's root and `BindRenames(entry, owner)` hangs the alias on it (kept OFF the
by-name index), `ExpandType` calls `CloneRenamesOnto` for each, which gives the TYPE's subject — a record OR a group
inside one — an alias of its own with the same operand names, and the one post-build RENAMES pass
(`RenamesOwners()`, every item that owns level-66 entries, not only roots) resolves, screens and tiles each in its owner's
scope with offsets measured from the record's root. Two groups of one type get two independent aliases (`AB OF R1`,
`AB OF R2`). Golden `2002/pb1304_typedef_renames`.
(4) the residue rules: **1558** EXTERNAL-type conformance (§13.18.22 GR2/SR5 in
`ExpandType` — the external-record attribution itself is LIVE, see the EXTERNAL-type paragraph above), **1535**
the NAMED §8.8.4.2.3 SR4 equality-only rule for a strong group with boolean/object/pointer elements (descriptor
`strong-compare-ordering`, in the relation checkpoint; asked of the CLASS), and the §8.8.4.2.12 element-order
relation (golden `strong_group_element_order`). An INDEXED-BY type referenced ≥2× is LEGAL and works: each clone's
INDEXED BY names are re-declared with their own cells (`DataBinder.RegisterIndexes`, kb/Work PB919), referenced
as `IX OF A` / `IX OF B` (§8.4.2.2.3 SR6). Goldens `typedef_indexed` (a single INDEXED-type reference works),
`typedef_same_as` (elementary+VALUE / group+qualified / nested renumbered / OCCURS composition / strong-copy
relations), `typedef_external` (two programs, one ExternalStore cell) + `TypedefResidueTests`/`SameAsTests`.
**Matrix note:** the STRONG phrase rides the SAME `typedefClause` gate as `typedef-def-2002` (introduction gating
already covered); SAME AS carries its own row `same-as-clause-2002`; an EXTERNAL-typedef row is NOT warranted
(TYPEDEF is unreachable below 2002, so the composition needs no second 0900); 1531–1535/1555–1558 are
edition-INVARIANT compile-time diagnostics (no cross-edition behavior variance). The restricted data-pointer
`USAGE POINTER TO type-name` (ISO §13.18.60.2 / Annex D.9.2.2) stays deferred — catalogued as the pending matrix
row `usage-pointer-to-type-2014` (the P12 re-scout re-anchored the former "`TYPE TO` pointer-target" label: plain
`TYPE [TO]` is the TYPE clause's optional word, §13.18.57.2, already live).

(5) **Declaration-first expansion (kb/Work PB1302, PB1301, PB1476).** `ExpandTypes` completes every one of the unit's
type declarations AS A DECLARATION before any storage item copies one: `ExpandTemplate` expands the template's own
chained TYPE clause (`01 S2 TYPEDEF TYPE S.`) and then every TYPE and SAME AS reference among its members, in source
order, and a declaration reached while it is still IN PROGRESS is the recursion §13.18.58.3 SR2 forbids (**1530**) —
a property of the declaration, diagnosed whether or not anything references it, and independent of declaration
order. A copy therefore never re-expands anything (`CloneItem` asserts its source carries no pending TYPE / SAME AS
reference), and a SAME AS target's whole subtree is likewise completed, on the current chain, before it is copied,
so §13.18.49.3 SR3 is ONE order-independent test (the target is or contains a subject whose expansion is in progress
→ **1557**) and a TYPE clause in that subtree naming a declaration in progress is §13.18.57.3 SR1 (**1557**). Copies
made INSIDE a declaration stay off the name, condition-name and index-name indexes (`InTypeDeclaration`); the
reference's own copy registers them, and also reproduces a member's composed RENAMES aliases. A failed expansion
leaves a recovery profile on its subject, so the §13.16.3 SR8 guard does not re-report it. Strength is ONE predicate:
`DataItem.DeclaresStrongType` (a declaration's own STRONG phrase, or the strength its own TYPE clause composed —
§13.18.57.4 GR1's "coded in place" makes data typed through S2 `TYPE S`, strongly typed by §8.5.3.1), read by SR6,
§13.18.22.3 SR5 (now asked over every typed entry a composed subject in an external record holds), §13.16.3 SR13,
§8.5.3.1's equivalence (`StrongTypeModel.DeclaredStrong`) and §13.18.58.3 SR1, which is asked when the declaration is
COMPLETED so `01 T TYPEDEF STRONG TYPE E.` over an elementary E is refused (**1529**). §8.4.2.2.1 rule 4's antecedent
is `DataItem.ReferencedByTypeClause`, recorded from every WRITTEN TYPE clause before anything expands; once set, the
declaration's members join a name's storage candidates in `SymbolTable.TryResolve` (`DataBinder.WithLapsedTypeMembers`)
as definitions that COUNT for uniqueness but never resolve alone, except a member the candidate is itself a copy of
(`DataItem.ClonedFrom`), which is §13.18.58.4 GR1's one-group case.

**Additional hardening (current invariants).** `ExpandType` sets `TypeName`/`StrongType` BEFORE cloning children, so a
nested TYPE ref's SR6 ancestor walk sees the enclosing strong item (no false SR6 strong-in-strong rejection).
`StrongTypeModel.TypeAnchor` (the NEAREST TYPE-carrying ancestor) drives `SameType`, so a nested `TYPE INNER-T`
subgroup matches a standalone INNER-T item (§8.5.3.1 alternative 1). A cloned OCCURS DEPENDING ON resolves data-name-1 in the clone's
OWN record subtree first (`OdoResolve` reads `SubtreeCandidates` over the record before the scope-wide
`QualifiedCandidates` — §13.18.57.4 GR1's "coded in place"), not a globally-first same-named counter; each tier is
COUNTED, and several survivors are §8.4.2.2.3 SR1's ambiguity (COBOLNET1639 through `UniqueOrReportAmbiguous`,
kb/Work PB978), never the first declared. Three §13.18.57.3 syntax rules are enforced: **1536** SR7 (a level-77
subject needs an elementary type — weak-invariant, not just STRONG), **1537** SR2 (a TYPE entry must be followed
immediately by a subordinate or level-88 entry, else a silent member-merge / CS1061 leak), **1538** SR5 (no USAGE/SIGN
on a group superordinate to a TYPE subject). The 15xx TYPEDEF band spans **1529–1538**.

**RISKS flagged:** `OccursSpec` sharing on clone (verify it holds NAMES re-resolved by `OdoResolve`, not cached
resolved items); `INDEXED BY` in a TYPEDEF used ≥2× — RESOLVED (kb/Work PB919: per-declaration index cells); method/OO-scope
typedefs are program/global-scope-first (the `OoRootOwner` parallel forest → staged loud follow-up). §13.18.57.4
GR2 d) (a group-typed subject "is aligned as though it were a level 1 item") is carried by
`DataItem.AlignedAsLevelOne` into the one bit-placement predicate `BitLayout.SharesByteWith` — bit alignment is the
only level-1 placement WiseOwl COBOL has, since it inserts no word-boundary slack at any level (§8.5.1.6.4; kb/Work
PB1569).

### D18. A FUNCTION-IDENTIFIER in a subscript or reference-modification position materializes into a COMPILER TEMP hoisted as a statement pre-op — never a new arm on the direct position walk.

**The problem (fix-queue PB17).** `MOVE W-E(FUNCTION INTEGER(3)) TO W-R` and
`MOVE W-A(FUNCTION INTEGER(3):2) TO W-R` are **legal source** that compiles clean and throws
`NotImplementedCobolFeatureException` at run time — the PB7/DA7 wrong-stage family. The chain, every link
`cite.py --check`ed: **§8.4.3.1.2** Format 1 makes a function-identifier an identifier → **§15.4** "the evaluation
of a function produces a returned value in a temporary elementary data item" → **§15.2** items 5–6 put integer and
numeric functions "of the class and category numeric" → **§8.8.1.1** "an arithmetic expression may be an identifier
referencing a numeric data item" → **§8.4.2.3.2** + **§8.4.2.3.4 GR1b** admit `arithmetic-expression-1` as a
subscript, and **§8.4.3.3.3 SR4** as a ref-mod position.
⚠ **§8.4.3.2.3 SR11/SR12 do NOT bar it** — they bar functions where an *integer* or *unsigned integer* is
required, and a subscript is neither: **GR1b sets EC-BOUND-SUBSCRIPT when the expression does not evaluate to an
integer**, a runtime condition that would be pointless if the position required one syntactically (SR14 confirms
it from the other side, having to impose that restriction *specially* for a BY REFERENCE bit item).

**Root cause.** The subscript reader was a hand-rolled renderer over a subscript's tokens that emitted C# text at
BIND time (`RenderSegment`, deleted by kb/Work PB2151 for the typed position walk); a function-identifier is not a
shape it could read term by term.

**Two tempting fixes, both REJECTED.**
· *Add a FUNCTION arm to the subscript reader* — that hand-writes intrinsic rendering, i.e. a THIRD expression
  compiler beside `ExpressionBinder` and `IntrinsicRenderer`.
· *Bind every position as a `BoundExpr` rendered by the numeric renderer* — that would lose the direct
  occurrence-number forms (an index-name's cell, a profile-checked item read) the place model renders today. D10's
  second half (kb/Work PB2151) instead made the position a TYPED carrier, `Position`, whose general arm is this
  temporary (DESIGN-binder-bound-tree.md §3.9).

**The decision.** Materialize what §15.4 already describes. Bind the function through the ONE function pipeline
(`IntrinsicBinder.BindIntrinsicCore` — shared by the FUNCTION-keyword form, the keyword-omitted re-parse and every
nested recursion, so a USER-defined function in a subscript falls out of the same change); synthesize the temp via
`DataBinder.CreateCompilerTemp`, already "THE ONE synthesized-compiler-temp constructor"; register a statement-scoped
pending PRE-op drained at the `BindStatement` chokepoint — the mark-on-entry / drain-own-suffix protocol that
ALREADY serves two clients (`Udf.Mark`, `data.OoPendingPropertyOps`). The subscript segment then renders
as an ordinary data-name through the existing `ResolveSubscriptName`.
⭐ **Prefer GENERALIZING the UDF pending list to a third list**: a function-identifier is never a receiving operand
(**§8.4.3.2.3 SR1**), so intrinsic and user-function activations are both unconditional pre-ops with identical
hoist rules — two mechanisms for one job would be the banned anti-pattern.

**⛔ THE §15.4 TEMPORARY'S DESCRIPTION IS A CORRECTNESS DECISION, NOT A FORMALITY — AND THE FIRST ANSWER WAS
WRONG.** §15.4.1 leaves the temporary's "characteristics and representation … defined by the implementor" when
native arithmetic is in effect, so the shape is a genuine implementor choice; the ONE constraint on it is that it
must not destroy the fact §8.4.2.3.4 GR1b tests. This design originally specified `Scale: 0` on the reasoning that
a subscript is an occurrence number. **That is exactly wrong.** GR1b makes a subscript whose expression "does not
result in an integer" set EC-BOUND-SUBSCRIPT, and §8.4.3.3.4 item 5)c says the same for a ref-mod position — so a
scale-0 temp TRUNCATES the value on the way in and `W-E(FUNCTION SQRT(2))` silently indexes occurrence 1 instead
of raising. The temp's own description would have turned legal source into a wrong answer, in the very code
written to stop legal source from throwing. **The temp is `PIC S9(21)V9(9)` — 30 digits, scale 9** — so it takes
the `Int128` wide tier (`PicInfo.IsWide`, >18 digits) and no subscript a program can express overflows it, which
matters because high-order truncation could WRAP an out-of-range subscript into an in-range one and convert a
detectable error into a silent one. It is synthetic, never enters `DataBinder.ConformanceForest`, so the edition
digit-capacity gates (18 at COBOL-85) do not reach it — it is not a PICTURE the programmer wrote. (The temp prune
lives in the SPINE both forests share, so it is absent from `CompositionForest` too — see
`COBOLNET_DESIGN.md`'s group-VALUE bullets for why there are two: `ConformanceForest` is the WRITTEN-ENTRY set
for per-entry attribute gates, `CompositionForest` adds the TYPE / SAME AS clone subtrees back for rules whose
subject is the COMPOSED entry, with `DataItem.ValueCopiedFrom` carrying the provenance that keeps such a rule
answering once per WRITTEN clause.)

**⛔ AND THE INTEGRALITY RULE IS ONE READ SHARED WITH ORDINARY SCALED SUBSCRIPTS (fix-queue PB41).** Asking what a
scale-0 temp would do to a NUMERIC function result is what exposed the pre-existing sibling: a WiseOwl COBOL numeric
item stores UNSCALED — `PIC 9V9 VALUE 2.0` is the field `20L` at scale 1 — and `ResolveSubscriptName` never
consulted `Pic.Scale`, so `W-E(W-S)` with `W-S = 2.0` indexed **occurrence 20**, fell outside `1..5`, and read the
benign scratch slot. Compiled clean, ran to completion, wrong answer. Both clauses are about the VALUE, so both
resolve at **ONE place — `ReferenceResolver.PositionRead`** — which renders `CobolTable.Occ(path, _P_n)` for a
subscript and `CobolString.RefModPosition(path, _P_n)` for a ref-mod position, passing the NUMERIC item's own
profile; the shared de-scale/integrality arithmetic is `CobolNum.HasFraction`/`PositionOf` (a negative, trailing-P
scale multiplies), and the two wrappers exist ONLY because the positions name different Table 13 conditions
(EC-BOUND-SUBSCRIPT vs EC-BOUND-REF-MOD). **The position kind therefore travels as a parameter
(`SegmentPosition`), never as renderer state** — `RenderSegment` is re-entrant through `ReadRefMod`, and ambient
state goes stale across a re-entrant descent.

⛔ **A POSITION IS ITEM IDENTIFICATION, AND §14.6.13.2 RULE 2 IS CHECKED THERE (kb/Work PB1117).** The profile is
what lets the read apply every rule a numeric SENDING reference owes when the post-bind whole-group analysis stores
the item as its character image: the scale, the sign, and the byte form `CobolNum.ParseImageSending` tests (rule 1's
second sentence: the condition "is set to exist for a class condition and a VALIDATE statement when invalid data is
detected during item identification" — so a subscript inside a class test is checked, NOTE 1). The codegen-time
twin — every integer count read at an operation site: the OCCURS DEPENDING object, a RECORD VARYING DEPENDING
length, a report DEPENDING count, SEARCH's bound — is `PlaceRenderer.CountRead`, the one renderer of the same
profile read. Only a NON-numeric `--permissive` operand (COBOLNET0844) keeps the profile-less `CobolTable.Occ(path)`
digit decode.

⛔ **AND THE FAST PATH MAY NAME ONLY A CARRIER THE EMITTED HELPER DECLARES A PARAMETER FOR (kb/Work PB201).** The bind-time text names the field and lets C# overload resolution supply the conversion — the same device that lets ONE text serve a `long` field and the `string` image the post-bind whole-group analysis may promote it to. But the bet is good only as far as the helper's own overload set reaches. The NUMERIC position read — `CobolTable.Occ(x, in NumProfile)` and `CobolString.RefModPosition(x, in NumProfile)` — takes `long | string | Int128 | ulong | UInt128` at every scale (the profile carries the scale, so no second arity exists), and the NON-numeric `CobolTable.Occ(x)` takes `long | string`. `Int128` must be numeric-admissible also because the D18 segment temp — the 19-digit integer POSITION a subscript or reference-modifier expression denotes, stored after its integrality is asked on the expression's own carrier (kb/Work PB1890) — is read back through this very path. Before kb/Work PB201 the unscaled set was `long | string` alone and NOTHING checked the operand against it. `DataItem.ElementType` produces more carriers than any overload set ever will: `double`/`float` (a COMP-1/COMP-2 leaf), `ManagedPointer`, `ProgramPointer`, an object-reference type, and a GROUP's per-program `record struct` — these still have no overload and are exactly what the route below exists for — plus, back then, the unscaled `Int128`/`ulong`/`UInt128` of the >18-digit and unsigned-binary tiers, which have one now. For every one of them the emitted text was not C# that compiles. MEASURED in the DEFAULT strict lane, on source §8.8.1.1 and §8.4.2.3.4 GR1b make legal: `E(W-FL)` with `USAGE FLOAT-LONG` → `CS1503 cannot convert from 'double' to 'long'`; `E(W-BIG)` with `PIC 9(20) COMP` → the same from `System.Int128`; `E(W-U)` with `BINARY-DOUBLE UNSIGNED` → from `ulong`; and `W-W(W-FL:2)` at the reference-modification bound. A GROUP subscript under `--permissive` emitted the record struct (CS1503) or, for a class-tier BASED group, a bare COBOL word that is not a C# name at all (CS0103) — so the warning's promise to decode "its digit characters as an unsigned integer" was never kept, and the two group shapes failed DIFFERENTLY.

⚠ **The answer is a ROUTE, not a wider overload set.** `PositionRead` admits only the declared carriers — `ReferenceResolver.NumericPositionCarriers` / `NonNumericPositionCarriers`, paired with `PositionCarrierOverloadDriftTests`, which compares them to the runtime methods' ACTUAL overloads by reflection, asserts the subscript and ref-mod arms admit the same numeric set, and forbids a profile-less numeric arity from returning — and returns null otherwise, so `RenderSegment` takes the D18 route it already documents as the answer for anything it cannot render. There `ExpressionBinder` screens the operand under §8.8.1.1 (COBOLNET0844, both lanes) and `NumericRenderer.FieldNum` — THE ONE numeric read — supplies the carrier-correct decode, including a group's §8.5.2.1 alphanumeric IMAGE through the ONE sending reader, so an occurs-depending group sends its §13.18.38.4 GR8 current-count part. The three INTEGER carriers `Int128`/`ulong`/`UInt128` were genuinely MISSING OVERLOADS and got them — `CobolTable.Occ` needs the capability, not an escape, because the SECOND emitter of the same text has no route: `PlaceRenderer.CountRead` renders the OCCURS DEPENDING ON current-count read at CODEGEN time from a Place, and §13.18.38.3 SR17 admits any item that "shall describe an integer", 20 digits included. All three narrow through **the ONE narrowing, `CobolNum.Position`, which SATURATES**: §8.4.2.3.4 GR2 requires "greater than the highest permissible occurrence number" to be DETECTED, and a plain `(long)` cast can wrap an out-of-range position back inside `1..n` and lose the condition. ⚠ The FLOAT carriers are deliberately absent: a `double` can be fractional and GR1b's integrality test runs over an unscaled/profile pair a float has no equivalent of, so a float operand keeps taking the D18 route. What overloads can NEVER close is the group: its carrier is a per-program generated type.

⚠ **A COMPOUND SEGMENT CARRYING A SCALED OPERAND ROUTES TO THE TEMP, NOT TO OPERAND-WISE DE-SCALING.** GR1b tests
the integrality of **the result** of the whole expression, not of each operand: `W-E(W-P + W-Q)` with
`W-P = W-Q = 1.5` has the integral result 3.0 and is a legal subscript, while de-scaling each operand first yields
`1 + 1 = 2` **and** raises the condition twice on source that never violated it. Such a segment therefore takes
the same materialization route, which evaluates it at full precision and applies the rule exactly once, where the
standard applies it. A SINGLE scaled operand IS the result, so it needs no detour.

⛔ **THE CORRECTNESS TRAP — DO NOT HOIST OUT OF A REPEATEDLY-EVALUATED CONDITION.** §8.8.4.13 r2 evaluates a
function "if and when the conditions containing them are evaluated", so a subscript inside a PERFORM UNTIL /
SEARCH WHEN / EVALUATE object must not be lifted to a statement pre-op. `UdfBinder` already solves this
(`UdfAttachPerEvaluation` / `BoundUdfEvaluated`) — **follow that precedent exactly.** (The loud residue stage it
used to carry for the windows it did not reach, COBOLNET1509, is DELETED: its last two callers — an EVALUATE
condition subject and a partial-expression object's subject splice — now bind the subject once, kb/Work PB912.)

**One parse, one tree (kb/Work PB2151).** The route binds the position's `arithmeticExpression` NODE of the main
tree, where the grammar parsed it. Until PB2151 it recovered the segment's verbatim text and re-parsed it through an
isolated `subscriptExpressionFragment` rule; that rule and its re-parse are deleted.

**Kept by D10's second half (a correction of the earlier plan to delete it).** The temporary is the general arm of
the typed position carrier: a position the direct walk cannot read — a function, `/`, `**`, a decimal literal, a
scaled operand in a compound — is this temporary's `PositionItemRead`, so GR1b's integrality rule is asked once, of
the result.
⚠ **The PB41 half is NOT deleted with it.** The integrality rule belongs to the two clauses, not to the carrier:
after CUT 2.5 the position read moves into the expression renderer with the rest of the carrier, still naming the
position's own Table 13 condition. Only the *materialization* is transitional; `HasFraction`/`PositionOf` and the
two wrappers are permanent, and the goldens `pb41_scaled_position` / `pb41_position_not_integer` pin them across
the migration.

### D22. The USAGE clause's DECLARATION-PLACEMENT rules (§13.18.60.3 SR14 / SR15 / SR4) are ONE bind pass over the WRITTEN-ENTRY forest — never a copy of the rule at each usage-acquisition site. (kb/Work PB183.)

**The rules.** SR14: "A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or
PROGRAM-POINTER phrase may be specified only for an elementary data item at level 1 or an elementary data item
subordinate to a type declaration that includes the STRONG phrase." SR15: "The USAGE OBJECT REFERENCE clause
shall not be specified in the file section." SR4: the same five phrases *plus INDEX* barred from a CONSTANT
RECORD entry "or in any item subordinate to" one. Three rules of one clause, none previously enforced anywhere;
all three are now `CheckUsageDeclarations`
(`src/Cobol.Net.Compiler/Binding/DataBinder.UsageDeclaration.cs` — COBOLNET1724 / 1725 / 1726), a declared
`BindPipeline` pass at `UsageResolved`.

**Why ONE pass and not five checks.** A pointer usage reaches a data item by FOUR routes: a written USAGE clause,
§13.18.60.4 GR1 group inheritance, a TYPE clone, and a SAME AS copy. The structural twin §13.18.57.3 SR6 is
implemented in this tree as two hand-written parent walks (in `ExpandType` and `ExpandSameAs`), so writing SR14
that way would have duplicated a rule at four sites instead of two. `DataBinder.ConformanceForest()` already means
"every entry the programmer WROTE, once each" — it walks `Roots` (which already holds the LINKAGE roots) + the `TypeDecls` templates and
prunes TYPE-clone subtrees — so one predicate over it covers all four routes, anchors the verdict at the entry
that must change (a weak typedef's pointer member is reported ONCE at the template, not once per `TYPE` use), and
screens a fifth route added later for free.

**Two determinations recorded here rather than left implicit.**
1. **A level-77 entry SATISFIES SR14's "at level 1" arm.** §8.5.1.3.2 places a 77 entry outside the level system
   ("three types of entries exist for which there is no true concept of level"), and §13.11.1 declares the level-1
   and level-77 spellings ALTERNATIVES for one data element that "bear[s] no hierarchical relationship to any
   other data item" — which is the property SR14's first arm stands for. Surveyed implementations agree. Held by
   ONE named predicate (`Sr14PermittedLevel`) so a re-reading is a one-line change.
2. **SR14's list omits INDEX; SR4's includes it, and the two must never be unified.** `05 IX USAGE INDEX.` inside
   an ordinary group is legal COBOL. The SR4 predicate is written as *SR14's set plus INDEX* so the one-phrase
   difference is the code's own structure, and a drift test asserts `Sr4 \ Sr14 == {INDEX}` in both directions.
   Over-rejection, not under-rejection, is this screen's hazard.

**Consequence for the storage model, worth stating because it bounds other subsystems' input.** On conforming
source, the ONLY way to write a GROUP containing a pointer/object-class leaf is inside a `TYPEDEF STRONG`
template. Everything else — an ordinary group member, a weak typedef member, a SAME AS copy into a group, a
group-level pointer USAGE — is now rejected at the declaration. That is what makes `ComputeTier`'s nested
pointer/object Tier-D backstop (D5 / `COBOLNET_REDEFINES_DESIGN.md`) unreachable-by-construction on conforming
source; the backstop is kept as the recovery-path guard, never deleted.

**Forward obligations, held by a test rather than by memory.** `Usage.MessageTag` EXISTS as of kb/Work PB487,
but the usage itself is DECLINED non-support — the data-item half of the Annex A.3 item 4 asynchronous messaging
facility (`docs/CONFORMANCE.md` §4 item 1), refused BY NAME in `PictureAnalyzer.ParseUsage` with COBOLNET1943 at
every edition. The member is there for the FLOAT-BINARY-128 reason: `ParseUsage` must return something and the
entry is PICTURE-less (§13.16.3 SR8 exempts message-tag), so without it the errored compile carried a null
`PicInfo` into the emitter — which is exactly the unhandled `NullReferenceException` PB487 measured.

⛔ **A DECLINED USAGE STILL HAS SYNTAX RULES OVER IT, and all three of this one's are now ENFORCED and
WITNESSED** (kb/Work PB544). The earlier reading here — that because no message-tag item can ever BIND, SR4's
and SR14's message-tag arms and SR21 were "unreachable rather than unimplementable" — conflated a declined
SEMANTIC with an unwritten SYNTAX RULE, and it was wrong on the measurement as well: a syntax rule is decided
on the written entry, ahead of and independently of whether the usage is supported. MEASURED at `--std 2023`:
`01 CFG CONSTANT RECORD. 05 Q USAGE MESSAGE-TAG.` reports COBOLNET1726 naming **SR4**;
`01 G USAGE MESSAGE-TAG. 05 M PIC X(3).` reports COBOLNET1724 naming **SR14**; and
`01 M USAGE MESSAGE-TAG USAGE DISPLAY PIC X(3).` reports COBOLNET2158 naming **SR21** — each beside the
COBOLNET1943 decline, never instead of it. SR21's screen is `DataBinder.CheckMessageTagExclusivity`, and the
reason it could not exist before is structural rather than semantic: `BindEntry` kept a single `usageText`
that each successive USAGE clause OVERWROTE, so `01 M USAGE MESSAGE-TAG USAGE DISPLAY PIC X(3).` bound as a
plain DISPLAY item and compiled with NO diagnostic at all. A rule whose subject is the SET of usage clauses
in an entry cannot be written against a scalar; the entry now keeps the clause LIST.
`UsageDeclarationPlacementDriftTests` still requires a recorded verdict for every `Usage` member, and the
verdict recorded for this one is the refusal. The pre-2023 shape of these rules is not derivable
from the 2023 text (Annex E's scope is 2014→2023 only, so its silence is a missing observation) — the arms ship
ungated below 2023, recorded as kb/Work PB296, and PB544's witness pins the 2023 cell ONLY so that a golden
cannot freeze the below-2023 routing defect it is waiting on.

**The other half of §13.16.3 SR8 — its SR9 exception — is SYNTHESIZED, not excused** (kb/Work PB504/PB831/PB828;
mechanism design `docs/rearchitecture/DESIGN-data-model.md` §2.7a). SR9 says the PICTURE clause "may be omitted
for an elementary item when an alphanumeric, boolean, or national literal that is not a zero-length literal is
specified in the data-item format of the VALUE clause", and implies `X(length)` / `1(length)` / `N(length)`. The
`SynthesizeImpliedPictures` pass (`DataBinder.ImpliedPicture.cs`, registered FIRST — before `ExpandTypes`, so a
TYPEDEF template's implied PICTURE is part of the description a TYPE subject receives, §13.18.57.4 GR3, kb/Work
PB1300 — and before `UsageInheritancePass`) builds that character-string — the arm from `CobolLiteral.ClassOf`, the length from
`CobolLiteral.Decode(…).Length`, a figurative constant's from §8.3.3.6.4 GR3 b/c with GR1/GR4 choosing its
category from the context — and hands it to the SAME `PictureAnalyzer.Analyze` call a WRITTEN clause takes, so
the item is thereafter indistinguishable from one whose source wrote the clause (§13.18.60.3 SR13 a) and SR20 are
written over the "explicit or IMPLICIT picture character-string", which is the standard licensing exactly that).
The entry is then no longer picture-less, so the SR8 closing guard never sees it and carries NO carve-out. The
standard states this rule once per entry kind: §13.15.3 SR14 is the report group entry's copy and shares this
classifier (`ImpliedReportPicture`, called from `DataBinder.Reports.cs`); §13.17.3 SR10 is the screen entry's and
needs no site, the SCREEN SECTION being the declined Annex A.4.2 module this doc already owns. Below COBOL-2002
the construct row `value-implied-picture-2002` refuses it by name (COBOLNET0900).

### D23. The initial-state BACKGROUND (§11.9.10 OPTIONS INITIALIZE + §14.6.2.3.2 action 1) is ONE choke point, `InitialStateBackground`, that all three consuming arms route through — the fill is a background laid UNDER the VALUE seed, never a substitute for a missing one. (kb/Work PB152.)

**The rule, in order.** §14.6.2.3.2 numbers the initial-state actions: action 1 — "If the INITIALIZE clause is
specified in the OPTIONS paragraph, the storage allocated for the implied or associated sections is set to the
specified-fill-character"; action 2 — "The internal data described in the working-storage section and
local-storage section is initialized as described in 13.18.63, VALUE clause." §11.9.10.4 GR2/GR3/GR4 route the
three sections to that consumer point and GR1 folds `ALL` into all three. **No sequencing code implements the
ordering:** a generated field initializer is one expression, and only its no-VALUE case changed, so the
re-application sites (`RecordStructEmitter`'s field initializers and `__ResetStatics`, the LOCAL-STORAGE
per-activation re-seed in `ProgramEmitter`, the OO instance cell) inherit it by construction.

**Why one choke point is the design and not a tidiness preference.** "What fills storage that has no VALUE
clause" was written in THREE places and exactly one had ever been fixed: kb/Work PB151 wired the ALLOCATE arm
(§14.9.3.4 GR8/GR9) with its fill decoder **private to `PtrEmitter`**, so `PicInfo.DefaultInitializer` (the
native-field arm) and `GroupImageCodec`'s fall-through (the Tier-B image arm) went on hardcoding `' '` / `'0'` /
`0L` and *could not* have reused it. The private copy also carried its own §11.9.10.4 GR5 map, which spelled
HIGH-VALUES as U+FFFF while every other HIGH-VALUE in the compiler was then U+00FF — one rule, three places, two
different answers, and the arm with the private copy was the one that disagreed. `InitialStateBackground` is now
the sole producer of a background seed; a unit drift test pins that it has exactly three public producers, and
`pb152_options_initialize_arm_agreement` asks all three arms the same question inside one COBOL program.

**Where each fact is resolved, and why not elsewhere.**
- **literal-1's byte — at BIND time**, on `OptionsInitialize.LiteralFillChar`, because §11.9.10.3 SR1 ("Literal-1
  shall specify a one-byte hexadecimal-alphanumeric literal") is a SYNTAX rule and that is its diagnostic site
  (COBOLNET1727). ⛔ "Hexadecimal-alphanumeric" is §8.3.3.2.2's FORMAT 2 (`X"nn"`), a defined term — *not* loose
  wording for "a short quoted literal", so `INITIALIZE ALL TO "Z"` is nonconforming.
- **the FIGURATIVE fills — at CODEGEN**, through `FigurativeConstants.FillChar`, the compiler's single definition
  of the alphanumeric extremes. §8.3.3.6.4 GR6 makes the high-value format "the character … that has the highest
  ordinal position in the program collating sequence", so it is a PCS-dependent fact and a second map is exactly
  the defect described above. ⛔ GR5 a (BINARY ZEROES, "a string of binary zeros") stays a SEPARATE arm from
  GR5 d (LOW-VALUES, "the alphanumeric low value character"): they coincide natively and diverge under a
  PROGRAM COLLATING SEQUENCE whose minimum is not NUL.
- **section selection and the GR7 exclusion — once**, in `FillFor`, keyed on root membership in the binder's own
  `WorkingStorageRoots` / `LocalStorageRoots`, never on "not a file record" (§13.18.63.4 GR2/GR3 put the file and
  linkage sections outside this rule's reach entirely). This is the first consumer `OptionsInitialize.Sections`
  has ever had — the binder built the flag set, GR1 fold included, and nothing read it.

**Numeric items take the fill too** (owner decision kb/Work R53, overturning this section's former "a
native-numeric carrier takes its zero"; kb/Work PB1134; recorded in `CONFORMANCE.md`). Action 1 names "the storage
allocated for the implied or associated sections" with no numeric exception, and §13.18.63.4 GR4 c)'s "undefined"
is §11.9.10.4 GR6's NO-clause baseline, not a license over a clause the program wrote. A typed-native carrier holds a
decoded value, so the storage decision moves to BIND time: `StorageFormPass` gives every VALUE-less fixed-point
numeric item of a governed section (`OptionsInitialize.Governs` — the one section predicate the seed also asks) its
IMAGE storage, the same promotion a group move's receiver gets, and `InitialStateBackground.Seed` fills that image:
a USAGE DISPLAY item holds the fill CHARACTERS, a BINARY / COMP-5 / PACKED item the fill BYTES (the fill character's
byte by the storage-byte law — HIGH-VALUES is 0xFF), a Tier-B window the same, and a floating-point carrier the value
its fill bytes encode. An item under a VALUE (its own or a group's) is not promoted: action 2 overwrites the
background there. A USAGE INDEX item's eight bytes take the fill byte (`InitialStateBackground.Seed`), a NATIONAL-usage
numeric item is promoted like any other (kb/Work PB1466), and an OO method's LOCAL-STORAGE is walked at bind time under
the method's effective OPTIONS clause (`StorageFormPass` step 5b); `PhysicalModel`'s member memo is keyed by that
initial-state context too (the OPTIONS model in force and the activation's LOCAL-STORAGE roots), so a method-local
group is not answered from the class-level entry (kb/Work PB1134, no residual). Class object / message-tag / pointer take NULL, which §13.18.63.4
GR4 c) states as a positive requirement; USAGE BIT keeps its packed ceil(n/8) zero seed (D19). **With NO clause the
seed is byte-unchanged** — `PicInfo.DefaultInitializer`
remains the §11.9.10.4 GR6 baseline it correctly is, which is the invariant covering the entire existing corpus.

### D24. The COMPOSITION of a Format-1 PICTURE (§13.18.40.3's composition syntax rules + §13.18.40.6's Table 10) is ONE pass in `PictureComposition`, with the table as DATA and symbol ORDER decided by ROLE ASSIGNMENT — never by hand-written positional `if`s. (kb/Work PB528, PB530.)

**The rule.** §13.18.40.3 SR2 states two obligations and the compiler had only ever answered the first:
"Character-string-1 shall consist of an allowable combination of characters used as picture symbols" — the
symbols shall be picture symbols, which `PictureAnalyzer`'s whitelist loop tests — and "The allowable
combinations of symbols for a PICTURE clause are specified in 13.18.40.6, Precedence rules", which nothing read,
because nothing anywhere tested a picture symbol's POSITION. `Analyze` derived geometry from the symbol MULTISET
(`Contains('S')`, `Contains('V')`, `Count(c is '9' or 'Z' or '*')`, the leading/trailing-P classifier, a
floating-string detector that was a bare occurrence count), so every composition rule of the clause was
unenforced at once: SR2's second half, SR12 a) and b), SR16, SR17, SR18, SR19, SR20, SR21, SR22, SR23 and SR24.
The harm was not only a missing check — several illegal strings RAN and produced an image no rule defines
(`PIC 99.99.99` turned 123456 into `56.00.00`; `PIC ZZ**9` rendered 7 as `  **7`, two replacement characters in
one item where §13.18.40.5 rule 7 defines exactly one), and `PIC +999+` / `PIC $999$` were silently
REINTERPRETED as floating strings, the compiler answering a question the source never asked.

**The shape, and why it is not eleven `if`s.** Table 10 is 24 symbol ROLES against 24, and eight symbols occupy
two rows and columns apiece because their precedence depends on WHERE they stand — the standard says exactly
which is which ("the leftmost column and the uppermost row for this symbol represent its use as the first or
second symbol in character-string-1"; "…their use to the left of the decimal point position"). So the
composition question decomposes into (a) ASSIGN every symbol occurrence its Table-10 role and (b) require an 'x'
for every ordered pair, the relation binding non-adjacent pairs because an 'x' means the column symbol "may
precede (BUT NOT NECESSARILY IMMEDIATELY)" the row symbol. Two kinds of occurrence are position-ambiguous — a
non-floating `+`/`-` is either the leading or the trailing sign, a non-floating currency symbol either the
leading or the trailing one — so the assignment is SEARCHED, not guessed: the string is allowable when SOME
assignment satisfies the matrix. SR24 bounds each to one occurrence, so the search is at most four assignments.

That is what makes the next case automatic. **SR25 and SR26 need no code of their own**: Table 10's leading-sign
ROW is entirely blank, so nothing may precede a leading sign; its trailing-sign and CR/DB COLUMNS are entirely
blank, so nothing may follow one; and the leading-currency row admits only the leading-sign column, which is
SR26's "leftmost symbol in character-string-1, optionally preceded by one of the symbols '+' or '-'" exactly.
SR27's "no more than one of … a string of two or more currency symbols … a string of two or more symbols '+'"
falls out the same way (row floating-currency-left has a blank floating-sign-left column). A rule the matrix
already carries is not written down twice. That claim is MEASURED, not argued: PB530 re-derived all three rules
against a build carrying the walk — SR25's `9+9` / `99-9`, SR26's `99$9` / `9$99+`, SR27's `+++$$$` (NOTE 5's own
invalid example) / `ZZZ$$$` are COBOLNET1935 at all four editions, while SR26 NOTE 4's six valid strings, SR27
NOTE 5's valid `+$$$` and the extended-sign spellings `L$999` / `999$L` all bind AND edit correctly.
`PictureTable10DriftTests` re-parses the table out of
`specs/ISO_COBOL.md` on every build and compares all 576 cells plus the 163-mark glyph count, so "the table is
the standard's" stays a measured fact; the markdown transcription was itself verified cell-for-cell against the
canonical PDF (printed folios 459-460) by geometry.

**The comma and the period are a PARAMETER, not a second rule set.** SR13 — "When the DECIMAL-POINT IS COMMA
clause is specified, the symbol comma is the decimal separator and the symbol period is the grouping separator.
The rules for the symbol period apply to the symbol comma, and the rules for the symbol comma apply to the
symbol period" — and §13.18.40.6's closing sentence for the precedence rules. `Analyze` therefore takes
`decimalPointIsComma` and hands it down; SR12 b), SR17, SR20 and the Table-10 roles all read which character
plays which part. This is not optional polish: without it `PIC 9.999.999,99` (NIST NC107A) and
`PIC ZZ.ZZZ.ZZZ,99` (SM103A) are rejected as legal source, and SR13's second sentence — which was VACUOUS while
the period rules it transfers did not exist — regresses the moment the rules land.

**ONE floating-string definition.** §13.18.40.5 rule 6 makes floating insertion an ADJACENCY property ("a string
of at least two identical floating insertion editing symbols", with the simple insertion symbols and, by rule
6 b, the decimal point embedded), never a count. `PictureComposition.FloatingString` is that definition and both
readers use it — the composition rules and `PictureAnalyzer`'s geometry — so the two can no longer disagree.

**ONE decimal-point-position derivation, and SR29 is the rule that needed it (kb/Work PB530).** SR29 — "For
floating insertion, at least one insertion symbol shall be specified to the left of the decimal point position" —
is the one composition rule the matrix genuinely cannot carry. Table 10 refuses most right-of-point floating
strings because its right-of-point floating rows have blank '9' columns, but those rows ADMIT the decimal
separator, so a string with nothing but the point to its left (`PIC .$$`, `PIC V$$`, `PIC .++`, `PIC V--`,
`PIC B.$$`) passed the walk, bound, and rendered an image no rule derives: `.$$` turned `MOVE 0.5` into `.00`.
§13.18.40.5 rule 6 is why the rule exists — a) edits "the leading numeric character positions to the LEFT of the
decimal point position" and b) all of them, whose result "is the same as if the floating insertion editing were
defined only to the left of the decimal point position" — so a string lying wholly right of the point has no
defined image at all. The check is one comparison against the point, and the point now has ONE derivation,
`PictureComposition.DecimalPointPosition` ('V' or the decimal separator; else a leading 'P' string's implied
point, §13.18.40.4 GR14; else past the last symbol), read by SR29 AND by the Table-10 role assignment that splits
the eight two-row symbols — the two cannot disagree about which side a symbol is on. "Insertion symbol" here is
the FLOATING insertion symbol, not any insertion symbol: a simple-insertion `B` to the left (`PIC B.$$`) leaves
the floating string with no digit position of its own before the point, which is exactly what rule 6 cannot edit.
The rule names floating insertion ALONE — the standard states no analogue for zero suppression — so `PIC .ZZ`
stays legal, and the positive golden `85/pb530_picture_floating_anchor_85` pins that scope beside the legal
anchored shapes (`$$.$$`, `$$V99`, `++.++`, `$$$PP`); `negative/pb530-picture-floating-left-of-point` holds the
seven rejections at all four editions.

**An IS-form PICTURE EDITING character-1 is deliberately TRANSPARENT to the walk; a non-floating FOR-form one takes
the `cs` role (kb/Work PB528, PB855).** §13.18.40.6 gives a Table-10 precedence to `es` alone — "If the EDITING
phrase is specified, the precedence of 'es' … has the same precedence as the 'cs' symbol in the column and row of
non-floating insertion symbols" — and SR12 makes `es` the EXTENDED (FOR-phrase) symbol only, the IS form being a
*fixed* editing sign control symbol whose editing is simple insertion (§13.18.40.5 rule 3) with no Table-10
precedence at all: it constrains no neighbour (`PIC LL EDITING "L" IS ":"` is the shape SR12 a) names as
sufficient). A NON-FLOATING `es` is position-ambiguous exactly like a fixed currency symbol (`CurrencyLeading` /
`CurrencyTrailing` in `PictureComposition.Precedence`), asked of every other symbol by the matrix: `PIC 99L99`
refuses (COBOLNET1935), `PIC L999`, `PIC 9999L`, `PIC L$999`, `PIC 999$L` bind. A FLOATING `es` string (rule 6) is
floating insertion and Table 10 states the `cs` mapping for the non-floating column and row alone, so it stays
transparent. **The one pair the matrix does NOT judge is one currency-like symbol against another** (`es`/`es`,
`es`/`cs`): the blank leading-currency-before-trailing-currency cell would refuse the two ends of Annex D.24's `PIC
IS L9999.99F` and SR26's `PIC L$999`, so the walk exempts `CurrencyLikeRoles` from each other (a fixed `cs` is bounded
to one by SR24, so the exemption removes nothing for a picture with no `es`) and SR25 / SR26 own the order. An `es`
placed beside a `cs` in the other order (`PIC $L99`, `PIC 999L$`) is accepted: nothing but the blank cell refuses it
and the cell is the one the standard overrides for its sibling pairs — the reading that rejects LESS.

**character-1 IS WRITTEN BARE, and its render is VARIABLE-WIDTH (kb/Work PB568, PB491).** The Format 1 general
format — re-rendered from the canonical PDF, printed page 441 — is `EDITING character-1 { IS literal-1 | FOR {
NEGATIVE IS literal-2 | POSITIVE IS literal-3 } }`: character-1 carries **no quotation marks** and is named
character-1 exactly as character-string-1 is, while literal-1/-2/-3 are named as literals. §13.18.40.3 SR8 types
it as a basic letter, §13.18.40.4 GR14 ('es') and §13.18.40.5 rule 3 make it a PICTURE SYMBOL occurring in
character-string-1, and SR9 — the rule that types the phrase's literals — enumerates only literal-1/-2/-3. The
grammar used to DEMAND the quoted spelling, so every conforming EDITING phrase was a parse error and SR8/SR10/SR11
were reachable only through a spelling the standard does not define; `editingPhrase` now reads
`EDITING ( cobolWord | literal ) …` and the quoted spelling draws the named **COBOLNET2149** at bind
(parse-wide/bind-narrow), never an ANTLR syntax error.

**literal-1/-2/-3 are LITERAL POSITIONS, a different operand kind from character-1** (kb/Work PB778). They are the
`editingLiteral` rule — the VALUE clause's literal-position superset `valueClauseOperand` — and bind through the ONE
literal-position chokepoint `DataBinder.RawValueOperandText` (parameterised by a `LiteralPosition` that names the
operand and the format in its diagnostics), so a constant-name (§13.10.3 SR2 / §13.10.4 GR1), a symbolic-character,
`ALL literal-1` and a concatenation expression (§8.8.3.3 GR3) all arrive as the literal they stand for, and a data
name or undefined word is refused by name (COBOLNET1639/1902). A keyword figurative constant is ONE character here
(§8.3.3.6.4 GR3b — the EDITING rules specify no length) whose class is the subject's (GR1): `EditLiteral` carries it
as `EditLiteralClass.Figurative` in both representations (its character from `ConcatFolder.FigurativeChar`, the one
figurative-character table) and `PictureAnalyzer` resolves it against the subject before SR9 is asked. A numeric or
boolean literal — or NULL — is `NotAlphanumericOrNational` and SR9 refuses it (COBOLNET1955). Before PB778 a
constant-name was a parse error and `EDITING T IS SPACE` / `IS "-" & "/"` inserted their SOURCE TEXT.

Two shapes of the phrase used to be STAGED LOUD (COBOLNET0899) although Annex D.24 demonstrates both, and they
are now rendered. A literal may be 50 characters (SR9) and §13.18.40.5 rule 6's first sentence lists "the
extended editing sign control symbols, if specified" among the FLOATING insertion symbols — so a FOR-phrase
character-1 repeated twice or more is a floating string, exactly as `$$$` is. Two structural consequences:

* `PictureComposition.MarkFloating` takes the FOR-phrase character-1 letters as floating CANDIDATES (they arrive
  as their own set, `char1Extended`, because only the binder saw which form declared them); the IS-form letters
  stay transparent per the paragraph above, since rule 3 makes those simple insertion symbols and rule 6's
  "embedded … or to the immediate right" sentence is what absorbs them.
* `CobolEdit` splits the render in two. `Render` produces the LOGICAL image — one character per picture SYMBOL,
  which is the image every editing rule of §13.18.40.5 is written about — and `Materialize` gives each symbol the
  width §13.18.40.4 GR14 gives it; `DeEdit` walks the same widths in reverse with a physical cursor. That is ONE
  variable-width mechanism for the TWO symbols GR14 widens: the currency string ('cs') and the editing literal
  ('es'). The search-based `ExpandCurrency`/`CollapseCurrency` pair that served the currency string alone is
  DELETED. The size half is `PictureAnalyzer.EditingPositions`, the exact shape `currencyExtra` already had.

Goldens `2023/pb491_editing_variable_width` (Annex D.24's own images and sizes) and
`negative/pb568-editing-char1-quoted`, `negative/pb491-editing-unequal-literal-widths`.

**The PICTURE character-string's own shape is screened before any symbol is read (kb/Work PB531, PB532).**
§13.18.40.3 SR4 caps character-string-1 at 63 characters AS WRITTEN — SR6's second sentence ("the length of the
integer, not the length of the constant-name, is counted toward the maximum number of characters in
character-string-1") is what fixes that reading, and it is what keeps `PIC X(30000)` legal — reported as
**COBOLNET2146**. SR6's repetition factor is validated ONCE, in `PictureAnalyzer.TryExpandRepeats`, against "an
unsigned nonzero integer": a sign, a zero, an empty or non-numeric factor and an unclosed parenthesis each draw
**COBOLNET2147**, by the literal route and by the constant-name route alike, since
`DataBinder.Constants.ExpandPicConstants` rewrites `(constant-name)` to `(integer)` in the source string and
hands it to the same expander. The expansion is BOUNDED as it accumulates: past WiseOwl COBOL's
**⚠ implementor-defined maximum of 134 217 728 (2²⁷) character positions in one elementary item** — the standard
sets none, SR4 bounding only the written string, SR14 only a numeric item's digit positions, and Annex A.1
carrying no maximum-item-size item — the entry draws **COBOLNET2148**. Before this, `PIC X(-3)` left the binder
as an unhandled `ArgumentOutOfRangeException` and `PIC X(2000000000)` as an `OutOfMemoryException`: a compiler
crash with no diagnostic and no source location. PICTURE format 2's SIZE obeys the same cap (§13.18.40.4 GR17
makes integer-1 a character-position count), asked at the one place the limit lives.

**The extended editing sign control symbols are a SET, and two rules are stated over it (kb/Work PB530, PB855).**
SR24's and SR25's SECOND sentences are the two the matrix cannot reach (they order one currency-like symbol against
another, which Table 10's blank cell would refuse). SR24: "either one or two extended editing sign control symbols
may be used in character-string-1" — a third is COBOLNET1985, asked in `PictureComposition.Validate` over the PICTURE,
because a SYMBOL is an occurrence (a FLOATING string of one character-1 is one, each fixed occurrence is one): a count
of EDITING phrases missed `PIC L9L9L` under one phrase, and `PIC 9L9F9G` with three FOR phrases once bound and
rendered `MOVE -12` as `0(1)2]`. The same bound keeps the position-ambiguous role search at 2^4 assignments. SR25: "the
first occurrence of the EDITING phrase shall be for the leftmost symbol in character-string-1 and the second occurrence
shall be for the rightmost symbol" — asked at the end of `PictureAnalyzer.ValidateEditing`, over the FOR-form phrases it
collected (SR12: the FOR form alone is an extended symbol; an IS-form phrase is simple insertion and is not counted),
and refused as COBOLNET1984 unless the first phrase's character-1 IS the string's first symbol and the second's its last.
The order is not cosmetic, because each extended symbol renders its own literal at its own position: `PIC F999.99L`
with the phrases reversed rendered `-1.5` as `)001.50(` where the conforming spelling renders `(001.50)`.
**⛔ THE READING (PB855, replacing PB530's)**: SR25 is read AS WRITTEN. PB530 read "the leftmost symbol" as the
leftmost OF THE TWO extended symbols (a phrase-ORDER rule only), because §13.18.40.6's `es`-takes-`cs`-precedence
sentence "cannot be applied literally" against Table 10's blank leading-currency-before-trailing-currency cell and
would reject Annex D.24's `PIC IS L9999.99F`. The premise was true of the cell and false of the reading: the walk now
applies the sentence to every symbol but another currency-like one, so the stricter SR25 refuses nothing the standard
shows, and the weaker reading had left `PIC LF$999` (two extended symbols before the currency symbol, SR26's "optionally
preceded by character-1" taken as unbounded) and `PIC 9L9F` legal against the words of the rule. SR26's singular is the
same fact seen from the currency symbol: with SR25 an `es` before the currency symbol is the string's leftmost symbol.
An IS-form character-1 beside the currency symbol (`PIC T$999 EDITING T IS ":"`) stays unconstrained — rule 3 makes it
simple insertion and no rule counts it against SR26.
**The floating test is the same adjacency for an extended character-1** (`PictureComposition.HasFloatingString`, read
by `ValidateEditing` for `EditRule.Floating`): the flag was a bare count of the letter's occurrences, so `PIC L999L` —
two FIXED occurrences — rendered as one floating string (`(0012`); it renders `(012(`.
Goldens: `2023/pb530_picture_editing_order_2023` (the conforming placements, including D.24's own example, a single
trailing symbol, a currency symbol between a pair and the two-fixed-occurrences case),
`negative/pb530-picture-editing-phrase-order`, `negative/pb530-picture-editing-extended-count`,
`negative/pb855-picture-editing-es-mid-string` and `negative/pb855-picture-editing-es-two-before-cs`, all at 2023 — the
EDITING phrase is a COBOL-2023 introduction and `negative/pb490-picture-editing-at-85` is the below-2023 gate.

**Where it runs.** `Analyze` calls it on the fixed-point Format-1 path, after the SR2 membership whitelist and
the national / boolean / floating-point arms have taken their strings and BEFORE the geometry derivation — which
presumes a well-formed string (the scale, the digit-position count and the floating-string detector all assume
the P run, the decimal point and the floating symbol are where the standard requires them). A violation is
COBOLNET1934 (a named §13.18.40.3 composition rule) or COBOLNET1935 (the §13.18.40.6 Table 10 precedence) and the
item recovers, the compile having already failed. Table 10's `E` and exponent-`+` rows and columns are carried
for fidelity and for the drift test but are not walked from this path: §13.18.40.4 GR13 b pins the floating-point
form's two parts more narrowly than Table 10 does, and `AnalyzeFloatEdited` is that rule's one place.

**Closing it carries regression risk, and the risk is MEASURED, not argued.** A prohibition written one symbol
too wide rejects legal source, which is worse than the under-rejection it replaces. Every PICTURE
character-string in the repo's corpora (730 distinct, ~70 700 occurrences) was run through the validator before
it was enabled, and all 1 339 conformance and NIST programs after: the only rejections were an authentic NIST
card error (`IX110A`'s `01 STATUS-TEST-10 PIC P VALUE ZERO`, which every sibling program writes as `PIC 9` and
which SR12 a) and §13.18.40.4 GR11 both refuse) and the deliberate format-2 negatives. Goldens
`85/pb528_picture_composition_legal_85`, `85/pb528_picture_composition_comma_85` and
`2023/pb528_picture_editing_transparency_2023` are the standing drift test over the LEGAL shapes, each rendered
so a wrong accept and a wrong render are both visible.

### D25. A Format-1 PICTURE's CATEGORY is decided by the general rule that DEFINES it, at the arm that answers it, and its SIZE is ISO §13.18.40.4 GR4 asked ONCE — never a per-category whitelist of the symbols that arm expected. (kb/Work PB535.)

**The rule.** §13.18.40.4 GR3 says a PICTURE clause "defines the subject of the entry to fall into ONE OF the
following categories of data" and lists eight, and GR5–GR13 each state the character-string that defines their
own: GR5 alphabetic ("only one or more occurrences of the symbol 'A'"), GR6 alphanumeric ("a combination of
symbols from the set 'A', 'X', and '9', that includes — at least one symbol 'X', or — at least two different
symbols from this set"), GR7 alphanumeric-edited, GR8 boolean, GR9 national, GR10 national-edited, GR11
fixed-point numeric ("shall include at least one symbol '9', and — may contain a combination of symbols from the
set 'P', 'S', and 'V'"), GR13 numeric-edited. GR4 then gives every one of them ONE size rule: "the number of
symbols in character-string-1 that represent either boolean positions or character positions", and GR14 names
the only three symbols that represent none — `'P'` and `'V'` ("not counted in the size of the item") and the
`'S'` of an item with no SIGN SEPARATE phrase — while saying of each of the rest "is counted in the size of the
item".

**The shape.** `PictureAnalyzer.Analyze` classifies down a chain of arms, and the LAST arm is a fall-through.
Two properties keep that honest, and both are structural rather than case-by-case:

1. **Each arm states its own general rule as a loud assertion.** The national arm carries GR9/GR10, the boolean
   arm GR8, the alphanumeric arm GR6/GR7's alphabet, and the pure-numeric fall-through GR11's "at least one
   symbol '9'" — each a COBOLNET0808 naming the rule. Every one of them is unreachable while `PictureComposition`
   (D24) and GR5–GR13 agree; they exist so that the agreement is ASSERTED rather than assumed, because a
   fall-through arm reached by exhaustion is exactly where a character-string that defines NO category ends up.
   Before the composition validator existed, `PIC S`, `PIC PPP`, `PIC SV` and `PIC SPPP` each bound a
   ZERO-LENGTH category-numeric item, silently, at every edition.
2. **GR4 is one function, `PictureAnalyzer.CharacterPositions`, and it states the rule as an EXCLUSION.** Every
   arm that sizes an item calls it. The exclusion form is load-bearing: the alphanumeric arm used to count a
   hard-coded WHITELIST of the symbols it expected (`'X'`, `'A'`, `'9'` plus the insertion set), which answers a
   symbol it does not know about by DROPPING it — `PIC XX,XX` sized 4 and `PIC XXCR` sized 2 where GR4 gives 5
   and 4, leaving the item SHORTER than its picture and shifting every following member of a group image.
   Stated as "every symbol except 'P', 'V' and an unseparated 'S'", plus GR14's currency widening, the rule
   cannot silently drop a symbol nobody anticipated; a picture the validator should have refused is sized
   honestly instead of being quietly shortened.

**The drift test measures the ACCEPTED SET, not a list of cases.** `PictureCategoryDriftTests` walks the whole
Format-1 symbol alphabet (7 239 strings of one to three symbols, plus the shapes three symbols cannot spell) and
asks of every string the compiler ACCEPTS whether the category it bound satisfies that category's defining
general rule and whether its size is GR4's count. It carries its own POPULATION guard — the accepted count and a
per-category histogram, with every one of GR3's eight required to appear — because both properties are stated
over the accepted set and would otherwise pass vacuously if the validator ever widened into a blanket refusal.
That is what makes the next symbol automatic: a prohibition relaxed in `PictureComposition`, or a category arm
widened here, changes the accepted set and is measured against the standard's own rules.

## C# mapping

CONCRETE COBOL→C# MAPPINGS:

— Elementary —
  05 NAME  PIC X(10) VALUE "BOB".        →  public string Name; … Name = CobolString.Store("BOB",10);
  05 CT    PIC 9(4).                      →  public long Ct;  // unscaled, scale 0; init 0L
  05 AMT   PIC S9(5)V99 COMP-3.           →  public long Amt; // unscaled (7 digits), scale=2; profile threads truncation=Packed
  05 BIG   PIC 9(30).                     →  public Int128 Big;  // WidePrecision → Int128
  05 RATE  COMP-2.                        →  public double Rate;
  05 FLAG  PIC 1(4).                      →  public string Flag; // boolean: one '0'/'1' CHAR per position (D8) — NOT a C# bool (multi-position PIC 1(n), MOVE fills, and ref-mod need character semantics; §13.18.40.4 GR14 R14 licenses the character representation). 2002+ — 0900 at --std 85.
  05 NNAME PIC N(4).                      →  public string Nname; // national: one UTF-16 char per national position (D8); usage NATIONAL implied (§13.18.60.4 SR13a). 2002+ — 0900 at --std 85.

— Group → nested record struct —
  01 WS-REC.                              →  record struct _T_WsRec { public string Name; public long Ct; }
     05 NAME PIC X(10).                       private static _T_WsRec WsRec =
     05 CT   PIC 9(4).                            new() { Name = new string(' ',10), Ct = 0L };
  reference NAME OF WS-REC                 →  WsRec.Name

— Fixed OCCURS (1-D) —
  05 TBL OCCURS 3 PIC 9(2).               →  public long[] Tbl;  init: Tbl = [0L,0L,0L];
  TBL(2)                                   →  Tbl[2 - 1]
  TBL(I)                                   →  Tbl[I - 1]        (I a numeric data-name)
  TBL(IDX + 1)                             →  Tbl[IDX + 1 - 1]  (relative index, IDX an index-name = occurrence #)

— OCCURS group (2-D), the array-of-structs shape —
  05 ROW OCCURS 4.                         →  record struct _T_Row { public long Col; }   // 1 col here
     10 COL OCCURS 5 PIC 9.                    public long[] Col;  // per-row
  COL OF ROW(2)(3)  (i.e. ROW(2), COL(3))  →  Row[2 - 1].Col[3 - 1]
  (each COBOL subscript → its own [n-1]; NO flattening, NO offset multipliers)

— OCCURS DEPENDING ON —
  05 N PIC 9(3).                           →  public long N;            // the length var
  05 ITM OCCURS 1 TO 100 DEPENDING ON N    →  public _T_Itm[] Itm;      // allocated at MAX (100); N bounds the live range
      PIC X(4).                                // sending op uses [0..N); a receiving whole-group uses [0..N) when N is OUTSIDE the group (ISO §13.18.38 GR8a), MAX only when N is INSIDE (GR8b)
  ITM(K)                                   →  Itm[K - 1]                // bounds checked vs N at runtime if EC enabled

— Index-name (INDEXED BY) = occurrence number —
  05 T OCCURS 10 INDEXED BY IX PIC X.      →  public string[] T; … private static long Ix = 1;
  SET IX TO 5                              →  Ix = 5;
  SET IX UP BY 2                           →  Ix += 2;
  T(IX)                                    →  T[Ix - 1]

— Reference modification —
  NAME(3:4)        (read)                  →  CobolString.RefMod(WsRec.Name, 3, 4)        // = Name.Substring(2,4)
  NAME(3:)         (read, length omitted)  →  CobolString.RefMod(WsRec.Name, 3, -1)       // -1 = to end
  MOVE "XY" TO NAME(3:2)  (write)          →  { var _t = WsRec.Name; WsRec.Name = CobolString.SpliceInto(_t, 3, 2, CobolString.Store("XY",2)); }

— Level 88 condition name —
  05 ST PIC 9.                             →  public long St;
     88 OK   VALUE 1.                       →  private static bool Ok      => St == 1;
     88 BAD  VALUE 2 THRU 9.                →  private static bool Bad     => St >= 2 && St <= 9;
     88 MIX  VALUE 0 5 7.                   →  private static bool Mix     => St==0 || St==5 || St==7;
  SET OK TO TRUE                           →  St = 1;     // parent set to the (first) condition value (ISO §14.9.39.4 GR6)

— Qualified + subscripted + ref-modded together —
  VAL OF ITEMS(I) OF WS-REC (3:2)          →  CobolString.RefMod(WsRec.Items[I - 1].Val_as_image, 3, 2)
                                              (Val numeric → its display image first per ISO §8.4.3.3.4 rule 2)

— CALL BY REFERENCE (Place → C# ref) —
  CALL "SUB" USING CT.                      →  Sub(ref WsRec.Ct);
  CALL "SUB" USING BY CONTENT NAME.         →  Sub(WsRec.Name);   // copy

— VALUE on a table —
  05 TBL OCCURS 3 PIC 9 VALUE ZERO.         →  Tbl = [0L, 0L, 0L];
  05 DAYS OCCURS 2 PIC X(3) VALUE "JAN".    →  Days = [CobolString.Store("JAN",3), CobolString.Store("JAN",3)];

## Hard problems

### The grammar captures subscript and ref-mod content as ONE undifferentiated raw token stream (subToken* in SUBSCRIPT lexer mode); `(I J)` (2 subscripts) and `(3:2)` (ref-mod) and `(I)(3:2)` (subscript THEN ref-mod) are syntactically identical at the rule level — only the presence of SUB_COLON distinguishes them.

**⛔ The group may be EMPTY (kb/Work PB969).** The same capture carries a keyword-omitted function-identifier's argument list, and §8.4.3.2.2 brackets the arguments INSIDE the parentheses, so `F()` is the zero-argument spelling (for a function-pointer, §8.4.3.2.3 SR5 makes it the only keyword-omitted one); `subToken+` refused it with COBOL0001 while `F( )` parsed. An empty — or whitespace-only — group after a name that resolves to a DATA item is refused by name in the resolver (`ReferenceResolver.ScreenEmptyParentheses` / `IsEmptyGroup`, COBOLNET2309: §8.4.2.3.2 writes `( subscript … )`, §8.4.3.3.2 a required leftmost-position); before it the whitespace-only spelling compiled clean on the sending side and aborted at run time.

Port the legacy ExpressionBinder's SUB_* token interpreter verbatim (it is proven over 364 NIST tests): for each `(...)` suffix group, scan tokens — if it contains SUB_COLON it is a ref-mod (split into start/length sub-expressions at the colon), else it is a subscript list (split on SUB_WS / SUB_COMMA into N subscript expressions, each itself possibly a relative `idx ± lit`). Phase-A flatten produces a clean {qualifiers[], subscriptGroups[][], refMod?} that Phase-B resolves. Do NOT try to re-grammar this — the SUBSCRIPT-mode design is intentional and reusing the interpreter avoids re-deriving COBOL subscript edge cases.

⚠ **The position walk is a reader, not an expression compiler, and the difference is where it ends (D18).** `ReferenceResolver.BindPosition` reads integer literals, data-names, index-names, `+ - *`, unary minus and parentheses off the parse node into a typed `Position` (kb/Work PB2151); **everything else binds the same node through the real pipeline into the §15.4 temporary** — never grown a new hand-written arm, which is how a reader turns into a third expression compiler. **The walk is an OPTIMIZATION over that route, never the arbiter of what is legal in the position, and stating it the other way round cost a defect: PB42.** For one commit the D18 gate asked "is this segment FUNCTION-bearing?" instead of "can the renderer render it", so `W-E(W-I ** 2)` and `W-E(2.0)` — plain arithmetic under §8.8.1.1 ("a numeric literal … separated by arithmetic operators") with §8.3.2.4.2 listing `**` as an arithmetic operator — kept compiling clean and throwing at run time. ⛔ **Routing everything is NOT the unaudited-table mistake PB1 taught, and the reason is structural: THE GRAMMAR ADJUDICATES.** A shape §8.8.1.1 does not admit — an alphanumeric literal, an `ALL` figurative constant (legal only in the §8.4.2.3.3 r6 positions) — does not parse as an `arithmeticExpression`, and the D18 hook refuses it by name (COBOLNET2363). Admission is by parsing, not by assertion, which is why the next arithmetic form needs no edit here. ⚠ Those two remain **run-time** throws where they should be bind-time diagnostics — right position, wrong stage; that is PB42's recorded residue.

### REDEFINES is a byte-level storage overlay with no clean typed-native equivalent: two differently-typed C# fields cannot share memory, so a write through one view is invisible to the other.

**Resolution (4-tier one-canonical-backing; see `COBOLNET_REDEFINES_DESIGN.md`).** One STORED canonical per redefines class + every other view a computed `Place` accessor. **Tier A** (identical PIC/USAGE) = pass-through accessor; **Tier B** (whole class USAGE DISPLAY — the corpus majority) = a single `string` canonical with substring/`ParseDisplay`/`FormatDisplay` accessors (NO bytes); **Tier C** (genuine mixed-USAGE pun) = one class-scoped `byte[]` canonical with per-leaf typed codecs, confined to the class and never persisted; **Tier D** (unmodelable) = reject loud. Independent typed fields + a loud cross-type-read guard (SSOT §14.3) cannot stay coherent under a pun, so detection alone never makes the program correct — only the single shared canonical does.

### Reference modification as a RECEIVER: C# strings are immutable, so `NAME(3:2) = x` cannot splice in place.

Place.Write for a RefModPlace rebuilds the whole string: `field = field[..(s-1)] + fit(value,len) + field[(s-1+len)..]` via runtime CobolString.SpliceInto(string,start1,len,value). Evaluate s and l into temps ONCE (ISO requires single evaluation of positions/subscripts) to avoid double-eval and side-effect duplication. For numeric/COMP receivers under ref-mod (rare), defer to the byte-image fallback and flag loud.

### OCCURS DEPENDING ON: the array size varies at runtime, but a C# array has a fixed allocated length; and ISO OCCURS GR8 (§13.18.38.4) governs the extent of a whole occurs-depending group operand in TWO cases — GR8a: when the DEPENDING-ON item (data-name-1) is OUTSIDE the group, the CURRENT count is used for BOTH sending and receiving; GR8b: when data-name-1 is INSIDE the group, a SENDING operand uses the CURRENT count while a RECEIVING operand uses the MAXIMUM length (data-name-1 is itself overwritten by the operation and so cannot bound it).

Allocate the array at MAX occurrences once; the length variable (DEPENDING ON item) bounds the LIVE range. Element access `Itm[K-1]` is unaffected. Whole-group operations consult the GR8 quadrant carried by `OdoGroupPlace.DependingInside`: **sending** (either quadrant) → the current extent [0..N); **receiving with data-name-1 OUTSIDE the group** (GR8a, the common case) → the same current extent [0..N), positions past N left unmodified; **receiving with data-name-1 INSIDE the group** (GR8b) → the full MAX length. Bounds-check K vs N only when the EC-bound checking class is enabled (later).

### Duplicate data-names disambiguated only by qualification — a single-value name index would silently overwrite and resolve the WRONG item. (IMPLEMENTED: `DataBinder.ByName` is the multimap described below.)

Make ByName a MULTIMAP (Dictionary<string,List<DataItem>>). Unqualified resolution: if the list has one entry use it; if >1 and a qualifier is required for uniqueness, emit the ISO §8.4.2.2 ambiguity diagnostic. Qualified resolution: right-to-left narrowing (resolve outermost qualifier, FindChild inward) — port legacy ResolveQualifiedName + FindChild exactly.

### Multi-dimensional OCCURS: legacy flattened to a 1-D byte buffer with per-dimension stepSize multipliers — an offset-arithmetic model that is exactly the byte substrate the owner rejected.

Use the natural .NET shape: a 2-D OCCURS is an array-of-(struct-containing-array), accessed `Rows[i-1].Cols[j-1]`. Each COBOL subscript maps to its own C# array index; NO multipliers, NO stepSize, NO flattened offset. This is simpler AND layout-free. Collect dimensions by walking item→ancestors (the only piece of LocationResolver worth keeping).

### Index-names (INDEXED BY) — legacy modeled them as byte displacements into the table, which leaks layout into a value the program can SET/compare.

Model an index-name as a C# `long` holding a 1-BASED OCCURRENCE NUMBER, not a displacement. SET TO n → assign n; SET UP/DOWN BY k → ±= k; subscript use → [idx-1]; SEARCH/SEARCH ALL emit as integer loops over the occurrence range. The only behavioral difference (an index surviving a table-element-width redefine) is implementor-defined and absent from the conformance corpus.

## Edge cases

- FILLER: no C# member name needed when it carries no VALUE and is never referenced; but a FILLER WITH a VALUE must still initialize its position (matters for whole-group reads and the G6 byte image). Generate a synthetic _fillerN member only when it has a VALUE or affects group serialization.
- Group item used as an alphanumeric operand (MOVE WS-REC TO X, or IF WS-REC = SPACES): a group has no scalar field — its char image is the left-to-right concatenation of all leaf display images. A generated `string AsImage()`/`FromImage()` per struct concatenates/distributes members; whole-group MOVE/compare uses it (all-string leaves and numeric-DISPLAY leaves alike). **Numeric-DISPLAY leaf refinement:** ISO §14.9.25.4 MOVE GR4 fills a group "without consideration for the individual elementary or group items" with **no conversion**, so a numeric-DISPLAY subordinate can receive non-numeric characters (e.g. spaces). A native `long` cannot hold that, so a numeric-DISPLAY leaf **under a group used as a whole operand** is stored as its CHARACTER IMAGE (a `string`); `DataItem.StoreAsImage` — a read-only projection of the item's computed `Storage` form, settled once by `StorageFormPass` — reports it, making `AsImage`/`FromImage` byte-faithful with NO byte[]. Numeric use of such a leaf decodes via `CobolNum.ParseDisplay` / formats via `FormatDisplay`. **The elementary twin (kb/Work PB992):** the same promotion applies to an ELEMENTARY numeric-DISPLAY item that a CHARACTER CHANNEL writes — the receiver of a GR4 group move (`MoveKind.GroupToElementary`), a CALL argument passed BY REFERENCE and every BY REFERENCE formal parameter (§14.2.3 GR8: the formal "occupies the same storage area as the argument", and §14.8.2.3.2 rule 1 asks a Format-1 pair only for "the same length", so either side may be written as characters), and a CALL RETURNING receiver (§14.6.5: the result "is the content of the data item"); the INVOKE statement has the same three (kb/Work PB1064): a BY REFERENCE INVOKE argument (every argument through a universal receiver, §14.9.23.3 SR6), every elementary METHOD formal (all are BY REFERENCE — the method-side BY VALUE phrase is refused — PROPERTY accessors excepted, whose "formal" is the object's own field), and an INVOKE RETURNING receiver (§14.9.23.4 GR8). `UsageCollectionPass` collects them into `DataBinder.CharacterChannelItems` beside the whole-group set, and `StorageFormPass` promotes them under the ONE eligibility test (`IsImagePromotable`) the whole-group arm uses. The price is deliberate: arithmetic on such an item decodes/encodes its image, where the alternative (a native carrier plus a side image) would have been a second representation of one storage area. A user-defined function's result temporary is exempt — it already mirrors its model's form. A BINARY / COMP-5 / PACKED leaf reached the same way — under a whole-group operand or by a character channel — is promoted by the same test (kb/Work PB1466): its native carrier held a DECODED value, so a packed nibble A–F or a binary value past the PICTURE's range was normalized away on the way in, and the numeric class condition (§8.8.4.4.4 GR3 n) 1. c., "a valid representation for the usage … within the range of values implied by the PICTURE clause") had no content left to test; the image-stored leaf is tested on its bytes (`CobolNum.IsNumericImage`). A USAGE NATIONAL numeric leaf is not promoted yet: its carrier would hold half its storage (D-N1), and a character-image group's MOVE width counts positions where its image counts bytes. A leaf never referenced as part of a whole group, and never written by a character channel, stays a native `long` (locked invariant #2). A group with a COMP/COMP-3/COMP-5/float leaf is the genuine mixed-usage byte-island (Tier-C), still deferred/loud.
- Numeric item under reference modification (ISO §8.4.3.3.4 rule 2): operate as if redefined alphanumeric of the same size — render the raw zoned/digit display image, ref-mod that, and (if receiving) re-parse back. Defer receiving-numeric-refmod to byte fallback, flag loud.
- Subscript/position single-evaluation (ISO): `TBL(F(X)) (G(Y):H(Z))` — F,G,H must each evaluate exactly once. Emit temps for every non-trivial subscript and ref-mod position before composing the Place.
- Relative subscript `idx - 1` where idx is at occurrence 1 → index 0-1 = -1: a runtime bounds violation (EC-BOUND-SUBSCRIPT). Honor with a checked path when EC enabled; otherwise undefined (matches no-EC corpus behavior).
- Level-88 VALUE with multiple literals AND THRU ranges mixed (VALUE 0 5 THRU 9 12): the condition is an OR over each literal/range — `St==0 || (St>=5&&St<=9) || St==12`. Capture the full value-item list, not just the first (DataBinder.ExtractValue currently grabs only FirstOrDefault — a bug for 88s and for multi-literal table VALUEs).
- SET cond-name TO TRUE picks the FIRST value of a THRU range (the low bound) or the first literal (ISO §14.9.39.4 GR6); SET TO FALSE places `Condition88.FalseValue` — literal-4 of the `WHEN SET TO FALSE` phrase (ISO §14.9.39.4 GR7 / §13.18.63.4 GR20) — through the SAME store, selected by `BoundSetConditions.ToTrue`; §14.9.39.3 SR7 refuses the arm when the phrase is absent (COBOLNET2049).
- JUSTIFIED RIGHT interacts with ref-mod and with numeric MOVE: JUST only applies to alphanumeric/alphabetic receivers (ISO §13.18.32) — diagnose/ignore on numeric. Already plumbed in CobolString.Store(justifiedRight).
- COMP-5 / BINARY-* with no PIC: width-bounded native int with TWO'S-COMPLEMENT WRAP on overflow (not digit truncation) — PicInfo.StorageWidth picks the byte width. `CobolNum.Store` WRAPs and `TryStore` range-checks by `NumProfile.StorageLength` (`WrapBinary`/`InBinaryRange`, branching signed vs unsigned; §14.9.25 GR8 magnitude for unsigned). The BINARY-CHAR family synthesizes via `PicInfo.BinaryItem` (PICTURE-less, §13.16.3 SR8 prohibits a picture → COBOLNET0870).
- 19-38 digit pictures overflow `long` (max 18 digits) → Int128. PicInfo.ClrType/DefaultInitializer/ProfileInitializer and the NumX literal renderer must branch on WidePrecision; CobolNum needs Int128 overloads. Pictures >38 (NATIONAL/2014) are out of scope for v1.
- REDEFINES of a table or by a table; REDEFINES chains (A redefines B redefines C): resolve the ultimate base; the whole chain is ONE redefines class with ONE canonical backing per the 4-tier verdict (A alias / B string canonical / C class-scoped byte canonical / D reject loud — `COBOLNET_REDEFINES_DESIGN.md`); never independent stored fields per view.
- Qualification of an index-name or a LINAGE-COUNTER by file/report name (grammar dataReference alts): index-name qualification is by table name (ISO §8.4.2.2 rule 6) — resolve via the owning table; LINAGE-COUNTER/LINE-COUNTER/PAGE-COUNTER are special registers, not data items — they get dedicated Places.

## The SCREEN SECTION — a DECLINED data-division section (Annex A.4.2)

The data division has one section this model deliberately does **not** map: **`SCREEN SECTION` (ISO §13.9)**.
It is the root of Annex **A.4.2**, *ACCEPT and DISPLAY screen handling* — the largest optional module in the
standard (27 listed elements) — which `docs/CONFORMANCE.md` §5 records as **Not claimed**. There is no
`DataItem`, no `record struct` and no `Place` for a screen item, and there is not meant to be.

**The posture is REFUSAL, not accept-and-ignore, and the licence decides which.** Annex **A.4.1**: *"An
implementation shall accept the syntax and provide the functionality for an optional element **only when**
support for that language element is claimed by the implementor."* That is a prohibition on accepting, so an
unclaimed A.4 module is a named **error** (`COBOLNET1560` / `COBOLNET1707`). Contrast the Annex **A.3
processor-dependent** facilities — MCS, COMMIT/ROLLBACK — whose licence is §4.2.6 and whose posture is
accept-inert plus a mandatory compile-time **warning** (`COBOLNET1578`, `COBOLNET1579`; `COBOLNET1580` VALIDATE
rides the same band). The severity is carried by the `DiagnosticDescriptor` and dispatched once in
`EditionContext.Report(descriptor, message)` — never by a local test at an emit site.

**One funnel: `src/Cobol.Net.Compiler/Binding/ScreenFacility.cs`.** It owns a table mapping every alternative of
the grammar's `screenClause` rule to the clause's name and its ISO §, so the diagnostic says *which* construct
the user wrote (`the AUTO clause (ISO §13.18.3) …`) rather than "something screen-shaped". That precision is
load-bearing for the witnesses: every screen **statement** program must declare a SCREEN SECTION to have a
screen-name, so a single undiscriminated code would let a statement witness pass on the section's own
diagnostic. `ScreenFacilityConstructDriftTests` reads the `.g4` and fails when an alternative has no row.

**Six source shapes, all now diagnosed** (before kb/Work PB260 only the first was, and by a bare string literal
with no descriptor — so the code had no `docs/DIAGNOSTICS.md` row at all):

| Shape | ISO | Code | What it did before |
|---|---|---|---|
| `SCREEN SECTION` + each screen description entry clause | §13.9 / §13.17 / §13.18.x | 1560 | a WARNING; the program compiled |
| SPECIAL-NAMES `CURSOR` / `CRT STATUS` | §12.3.7 (A.4.2 item 25) | 1560 | parsed, read by **no** binder — a clean compile, zero diagnostics |
| `ACCEPT screen-name-1` (format 3) | §14.9.1 (item 1) | 1707 | token-identical to the device format ⇒ bound as one, reading device input into a screen record |
| `DISPLAY screen-name-1` (format 2) | §14.9.11 (item 9) | 1707 | bound as a device DISPLAY and **printed** the screen record; `DISPLAY SG COLUMN 5` bound as a *three-operand* device DISPLAY |
| `SET screen-name-1 ATTRIBUTE …` (format 6) | §14.9.39 (item 24) | 1707 | no grammar alternative ⇒ a generic COBOL0001 parse error |
| `EC-SCREEN-*` in the six item-10 contexts | §14.6.13.1.1 (item 10) | 1707 | catalogued in `ExceptionCatalog` with no raise site and no reader ⇒ `>>TURN`/`RAISE` compiled against nothing |

The EC arm lives in `EcNameResolution.Advise`, the ONE resolution funnel, which is what makes it cover
`>>TURN`, `USE`, the PERFORM `WHEN` phrase and `RAISE` together; the two RAISING sites that call `Advise`
directly already refuse a non-`EC-USER` name with COBOLNET0858. §14.6.13.1.1 licenses raising *no* condition for
an optional element's level-3 name, but it does not license silently accepting the name.

The OPTIONS paragraph's `INITIALIZE … SCREEN …` target (§11.9.10.4 GR3) is a seventh shape and draws 1560 too;
**only the explicit leg**, because GR1's `ALL` implies two supported sections as well and refusing it would
reject legal source over a section the program does not have.

**The grammar parses all of it on purpose** (`Grammar/Core/CobolScreen.g4`) — superset-parse, bind-narrow. A
named refusal that says *which facility* is strictly better than a parse error, and for the two statement
formats a surface is the only way to fail at all. Completing that surface from the rendered general formats
added `MINUS`/`-` to the LINE and COLUMN clauses, the `END OF LINE` / `END OF SCREEN` spellings to ERASE, and
`usageClause` to `screenClause`.

⛔ **The ACCEPT/DISPLAY exception phrases are bound to the positioning phrase (`screenTail`), and that coupling
is load-bearing.** `ON EXCEPTION` / `NOT ON EXCEPTION` is spelled the same on a screen statement as on the
statements that already own one, and DISPLAY sits inside every one of their imperative-statement slots — so a
free-standing optional `screenExceptionPhrases` made the inner DISPLAY of
`DELETE FILE F ON EXCEPTION DISPLAY "EXC" NOT ON EXCEPTION DISPLAY "NOEXC" END-DELETE` swallow DELETE's own
NOT-arm. Requiring the AT/LINE/COLUMN phrase first makes the tail unambiguous, because no other statement's
exception arm can begin with it. `NestedDisplayInAnExceptionArm_DoesNotStealTheEnclosingStatementsNotArm` pins
it; the cost is one spelling (an unpositioned `DISPLAY screen-name ON EXCEPTION …`), which stays a parse error
exactly as it was before.

**Declining the module must not cost the user the words.** §8.10 makes fifteen screen words — `AUTO`,
`BACKGROUND-COLOR`, `BELL`, `BLINK`, `EOL`, `EOS`, `ERASE`, `FOREGROUND-COLOR`, `FULL`, `HIGHLIGHT`, `LOWLIGHT`,
`REQUIRED`, `REVERSE-VIDEO`, `SECURE`, `UNDERLINE` — **context-sensitive** (§8.9 reserves none of them at any
edition), and §8.9 reserves `COL`, `COLS`, `COLUMNS`, `CRT`, `CURSOR` and `SCREEN` only from 2002, leaving
`COLUMN` the one screen-surface word reserved at every edition. §8.3.2.1 rule 3 makes each of the other
twenty-one a legal user-defined word wherever §8.9 leaves it free — yet five were lexer tokens that `cobolWord`
did not admit, so a legal `01 REVERSE-VIDEO PIC X.` was a parse error at every edition and `01 CURSOR PIC X.` at
COBOL-85. All twenty-two now carry `nameSlot` rows in `tests/version-matrix/cobol-words.json` (the §8.9 seven
reservation-gated from `reserved-words.json`, never by hand), and `specialNameEntry`'s CURSOR / CRT STATUS arms
are gated on `reservedHere` so a COBOL-85 `SPECIAL-NAMES. CURSOR IS FOO.` keeps its implementor-switch reading.
The population is DERIVED from `CobolScreen.g4` + the two SPECIAL-NAMES screen clauses rather than listed, and
`ScreenFacilityConstructDriftTests` fails when a screen token has no name slot or a screen refusal names a rule
no scrape reaches — so the next word this module tokenizes cannot cost the user that word in silence
(kb/Work PB301). The complement — the same words still recognized in their own clause and refused BY NAME — is
the `a42-screen-*` negative set.

**At `--std 85` the refusal is the same one.** Screen handling does not exist in COBOL-85 at all, so there is no
"introduction gate then decline" ladder to build: the construct is refused by name at every edition, and the
message says the facility exists from COBOL-2002.

## Per-edition gating (the G1 obligation)

`cobol.exe` is four compilers in one (`--std 85|2002|2014|2023`, default COBOL-2023). Every edition-varying
construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition
that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests
(NIST etc.) only VERIFY; they never SCOPE. Framework: `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row
edition-change checklist — 2002→2023 deltas ONLY; it has NO 85→2002 rows; derive 85↔2002 gating from the 2002
standard / the ISO2023_CONFORMANCE_PLAN M2 catalog) + `docs/VERSION_TEST_MATRIX_DESIGN.md` (the
(construct × edition) matrix; Phase 0 done). Data-model constructs that MUST be gated:
  • Table dimensions: COBOL-85 caps a table at 7 dimensions; 2002+ removes the fixed cap. >7 dims at
    `--std 85` ⇒ diagnostic; dims are stored as a list with no engine cap.
  • Numeric size: COBOL-85 caps fixed-point at 18 digits; 2002+ raises the cap to 31. PIC 9(19+) at `--std 85`
    ⇒ diagnostic; >31 digits ⇒ diagnostic at every edition (Int128's 38-digit headroom is substrate, not surface).
  • Boolean data (PICTURE symbol `1`, USAGE BIT): 2002 introduction (derive from the 2002 standard) — diagnose
    at `--std 85`.
  • Format 2 (table) VALUE — literals keyed to occurrence ranges via a mandatory `FROM (subscript)` phrase
    (§13.18.63.2): 2002 introduction (derived — the 2023 Annex E VALUE rows are numeric-edited only, the A1
    authority gap) — diagnose at `--std 85` (COBOLNET0900); construct `value-table-format-2002`. Its SYNTAX
    RULES carry no version proviso: SR18/SR20/SR21/SR22/SR23 are reported at all four editions, ALONGSIDE the
    introduction gate (and, for the SR22/SR23 shapes, alongside OCCURS DYNAMIC's 2014 gate), never instead
    of it.
  • SET condition-name TO FALSE / the `VALUE … WHEN SET TO FALSE` phrase: 2002 introduction — diagnose at
    `--std 85` (COBOLNET0900; rows `set-condition-false-2002` / `value-false-phrase-2002`, fired
    recognition-first by `VersionConformancePass.ParseArm`).
  • Zero-length reference modification: edition-varying, 2023-only directive control — see §5
    (REF-MOD-ZERO-LENGTH / EC-BOUND-REF-MOD, `VERSION_CHANGE_REFERENCE.md` #30).
  • `BINARY-CHAR…BINARY-DOUBLE` usages: 2002 introduction — diagnose at `--std 85`; `COMP-5` is a dialect
    extension — flag per the extension policy.
Each bullet gets a (construct × edition) case in the version test matrix: accepted-with-correct-behavior at
editions that have it, rejected-with-the-right-diagnostic below its intro edition.

## ISO citations

- ISO/IEC 1989:2023 §8.4.2.2 Qualification — uniqueness of reference; qualifiers specified in order of successively more inclusive levels; rules 4-6 cover 88 (includes the conditional variable in the hierarchy), condition-name, and index-name (by table-name) qualification
- §8.4.2.3 Subscripts — 1-based occurrence numbers; integer literal / data-name / index-name / relative index ± integer forms; outer-to-inner ordering for multi-dim
- §8.4.3.3 Reference-modification (general rules §8.4.3.3.4, spec lines 6952-7020) — leftmost position + length define a unique subset; rule 2 (non-alphanumeric DISPLAY treated as redefined alphanumeric of same size), rule 3 (NATIONAL likewise), rule 5 (creates a unique data-item subset); rule 5c default length = remaining characters
- §13.18.38 OCCURS clause; GR8 (occurs-depending whole-group extent: with data-name-1 OUTSIDE the group the CURRENT count is used for both directions (GR8a), with it INSIDE the group sending uses the current count and receiving uses the MAX length (GR8b)); INDEXED BY index-names are distinct entities
- §13.18.44 REDEFINES — same storage area; (file-record implicit redefinition)
- §13.18.45 RENAMES (level 66) — alternative grouping over a contiguous run
- §13.18.55 SYNCHRONIZED — natural-boundary alignment (a byte-layout concern; no-op for typed in-memory data)
- §13.18.32 JUSTIFIED — right-justification for alphanumeric/alphabetic receivers only
- §13.18.8 BLANK WHEN ZERO — all spaces when THE STORED value is zero (GR1), and an all-spaces image reads back as zero (GR3)
- §13.18.63 / §14.9.4 VALUE clause and initial state — default initial values; table initialization; figurative constants
- §13.18.60.3 USAGE syntax rules — the DECLARATION-PLACEMENT family (D22): SR14 (the five pointer/object phrases only at level 1/77 or under a STRONG type declaration), SR15 (no USAGE OBJECT REFERENCE in the file section), SR4 (those five PLUS INDEX barred in or under a CONSTANT RECORD). ⛔ SR14 omits INDEX where SR4 includes it — two lists, deliberately
- §13.18.60.4 GR1 — a group-level USAGE "applies only to each elementary item in the group", which is how a group entry's pointer phrase becomes an SR14 violation at each subordinate's own level
- §8.5.1.3.2 Level-numbers / §13.11.1 — a level-77 entry has "no true concept of level" and is an ALTERNATIVE spelling of the level-1 single-elementary record; together they settle that 77 satisfies SR14's first arm
- §14.9.39 SET statement — condition-name SET TO TRUE assigns the first/low value (GR6), SET TO FALSE the WHEN-SET-TO-FALSE literal (GR7); index-name SET semantics
- §8.8.1 / §14.9.25 arithmetic on the algebraic value regardless of representation; MOVE rules (justify, truncation, GR8 unsigned magnitude)
- Conditional-flag REF-MOD-ZERO-LENGTH (spec line 4523) — zero-length reference modification permitted, yields empty

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- Int128 substrate timing — **RESOLVED:** the value engine is Int128-monomorphic (SSOT: the `CobolInt(Int128,scale)` carrier) and `CobolNum`/the numeric renderer carry Int128 support; `WidePrecision` selects the stored type. The SURFACE digit cap stays per-edition (18 at `--std 85`, 31 at 2002+ — see the per-edition gating section); Int128's 38 digits are substrate headroom only.
- COMP-5 / BINARY-* two's-complement WRAP semantics — **RESOLVED (SSOT numeric model; §6 above):** true binary-width wrap by storage width (PIC S9(4) COMP-5 wraps at ±32768), NOT digit-count truncation. The wrap path is in `CobolNum.Store` (`WrapBinary`) and `CobolNum.TryStore` (`InBinaryRange` → SIZE ERROR), keyed off `NumProfile.StorageLength`, signed vs unsigned; the `BINARY-CHAR…DOUBLE` family rides it. Note `BINARY-CHAR…DOUBLE` are 2002+ (per-edition gating section); `COMP-5` is a dialect extension.
- Whole-group-as-alphanumeric — **RESOLVED (SSOT §18 #21):** the generated `string AsImage()`/`FromImage()` per struct IS the permanent typed-native mechanism for whole-group MOVE/compare of **DISPLAY-homogeneous** groups, INCLUDING numeric-DISPLAY leaves (those store their character image via `StoreAsImage` when whole-referenced — see the edge case above). Only groups with a COMP/COMP-3/COMP-5/float (non-character) leaf are the genuine mixed-usage byte-island routed to the Tier-C codec (§4); national-member groups use the same `AsImage` over UTF-16. No byte[] for any DISPLAY-homogeneous group.
- REDEFINES cross-type-read detection — **RESOLVED (the 4-tier model, SSOT §14.3):** with ONE canonical backing per redefines class every write is visible through every view, so no cross-type-read guard exists or is needed; genuine mixed-USAGE puns are Tier C (class-scoped byte canonical) and unmodelable puns are Tier D (reject loud). See `COBOLNET_REDEFINES_DESIGN.md`.
- Passing a ref-modded or subscripted-with-variable receiver as CALL BY REFERENCE: C# `ref` to an array element is legal but `ref` to a ref-mod splice is not. Confirm the policy — diagnose (strict) vs silently promote to BY CONTENT (lenient) — and whether it should be dialect-gated.
