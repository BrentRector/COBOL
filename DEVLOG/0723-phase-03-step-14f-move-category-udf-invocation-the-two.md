## Entry 723 — 2026-07-09 16:39 PDT — PHASE-03 Step 14f: MOVE-category + UDF-invocation (the two genuinely-semantic statement gates)

The two remaining statement gates that GENUINELY need resolved bound facts (not a token/phrase presence) move
binder→`VersionConformancePass`. Everything else still binder-side is SYNTACTIC (a token/phrase) and lands in the
parse-arm (14h) — so this completes the bound-arm STATEMENT-gate relocation.

- **MOVE figurative-constant category** (§14.9.25.3 SR5 — `MoveAllDigitIntegerObsolete2023` /
  `MoveQuoteNumericObsolete2014` / `MoveAlphanumericFigurativeRemoved2023`). Which of the three rows applies depends on
  the source figurative × each receiver's RESOLVED picture — a `GateMove` in the pass re-derives the binder's former
  classification EXACTLY (same figText, per-target exemptions, integer/QUOTE/digit-only split, where-string). The binder
  method `MoveFigurativeEditionGates` is trimmed to keep ONLY the version-invariant SR1 class-index error (0809) and the
  pre-removal `StoreAsImage` marking (which is storage, not a gate — needed at 85/2002/2014 + 2023 --permissive
  regardless); the storage eligibility is byte-identical (a digit-only ALL never set it, so dropping the old
  digit-only-integer `continue` is neutral).
- **UserFunctionInvocation2002** — a user-defined FUNCTION reference lowers to a HOISTED `BoundCallProgram` with
  `IsFunction=true` and `LiteralName=fn.Name`; the pass gates it in the `BoundCallProgram` arm with the exact
  where-string `FUNCTION {name}`. A regular CALL has `IsFunction=false` (mutually exclusive). A-caveat: a MALFORMED
  below-2002 UDF call (arity/RETURNING error) creates no hoist, so it loses the 0900 — but it is already rejected by its
  1505/1506/1507/1510 error (a terminal unit).

**Battery:** UDF/MOVE/edition tripwires **1856** (incl. `MoveEditionDifferentialTests` — the MOVE byte-identity net),
conformance **3114**, characterization **32**, unit **227** GREEN. (Full legacy guard deferred to 14h — greenfield-only;
the greenfield battery + characterization is the neutrality net, legacy is untouched.) NEXT — **14g:** the data / PICTURE
/ OO gates (R4/R5) via a complete DataItem + FileModel enumerator.

