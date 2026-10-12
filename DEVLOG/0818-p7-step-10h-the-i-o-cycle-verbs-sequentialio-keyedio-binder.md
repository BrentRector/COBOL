## Entry 818 — 2026-07-11 20:12 PDT — P7 Step 10h: the I/O cycle — `Verbs/{SequentialIo,KeyedIo}Binder` + `MnemonicRegistry` on ctx + the AcceptDisplayBinder DETACH

**What (the AS-BUILT PLAN's 10h batch — the emitter-mirror I/O pair lands as ONE batch).**
- **`Verbs/SequentialIoBinder`** — OPEN/CLOSE/WRITE/READ/REWRITE carved off the core with the keyed reroutes
  (`keyedIo.Bind*` by ctor ref — the SeqIo↔KeyedIo cycle mirrors the Step-9 emitter pair), plus
  WriteSource/BindAdvancing/MapOpenMode/MapSharingMode/FileOfRecord/UnsupportedOrg. `BindRetry` STAYS a host
  spine member (FileLockBinder + OPEN consume it). The Sort partial's WriteSource/FileOfRecord uses retarget
  through the internal host accessor (`SeqIo.…` — they flip to a ctor ref when SortBinder lands at 10i).
- **`Verbs/KeyedIoBinder`** — the whole partial over `(ctx, host, fileLock)`; `_keyedCheckedFiles` keeps its
  per-unit first-keyed-verb-wins lifetime; the nine bound types → records-only `BoundKeyedIo.cs` (the
  VersionConformancePass gates fire on their SHAPE — untouched); `MapOperator` stays a host internal static
  (the Boolean partial shares it); WriteSource reaches back through `host.SeqIo` (lazy — always wired).
- **`MnemonicRegistry` on `BinderContext`** — the SPECIAL-NAMES walk-up moved off the ACCEPT partial; the
  ONE per-unit lazy map now serves ACCEPT-FROM and the WRITE SR13 / ADVANCING zero-advance legs.
- **`AcceptDisplayBinder` DETACHED** — the Step-5 file finally becomes a real class (and absorbs
  `BindDisplay`, making the name honest binder-side); `BoundAccept`/`AcceptKind` → records-only
  `BoundAccept.cs`; the 0815 four-digit-year gate moved VERBATIM.
- **StatementValidation grows four I/O lifts:** OPEN SR8 (1512), WRITE SR19 (0860) / SR18 (0861) / SR13
  (0862 — the mnemonic test resolved by the caller through the registry).

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.

