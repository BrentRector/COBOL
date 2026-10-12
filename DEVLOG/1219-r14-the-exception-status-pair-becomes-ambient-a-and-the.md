## Entry 1219 â 2026-08-08 10:14 PDT â R14: the exception-status pair becomes AMBIENT â and the probe that found "no wrapper" was really finding a same-line enable hole

R14 asked why FUNCTION EXCEPTION-STATEMENT answered 63 spaces under WITH LOCATION for every raise site the
emitter could not hand literals to. The ledger's fix shape held up exactly: the (Table-12 name, location)
pair now travels on an AMBIENT statement context â `ExceptionState.EnterStatement/ExitStatement`
(save/restore, so a CALL inside a checked statement restores its caller's context) emitted once per checked
statement with EXACTLY the WITH-LOCATION names, and the 2-argument `Set` resolves the pair from it
per-condition. SEARCH's range Sets, CONTINUE AFTER, and the runtime string/storage sites inherit the channel
by construction; the per-site `EcStmtLoc`/`EcStmtLocExpr` literal-baking is DELETED â one rule, one place.

Three of my own note's claims did not survive contact, and re-measuring them first changed the work:

- R04/R06/R07 had already landed their thirds (the Table-12 kind resolver â the probe prints
  `CAUGHT=[GO TO]` for the fiftieth row â the per-condition location rule, and the RAISING threading).
- The startling probe result â `GO TO â¦ DEPENDING` raising NOTHING inside a WITH LOCATION F3 PERFORM â
  was not a missing QueryFor arm at all: the GR14 implicit enable is spliced AT the PERFORM's line and
  `TurnState.Fold` breaks at `Line >= statementLine`, so imperative-statement-1 STARTING ON THE PERFORM'S
  OWN LINE silently escaped checking entirely. The handler floor had the mirror-image hole in the disabling
  direction. Both synthetics are now `Inclusive`; real directives (which own their lines) keep the strict
  compare. The one-line and two-line spellings of the same PERFORM now behave identically.
- `__IoCheckEc` deliberately KEEPS its positional channel: file-scoped WITH LOCATION is per-(name, FILE),
  which a name set cannot express â file-scoped entries are excluded from the ambient set and explicit
  operands always win, so the ambient fallback cannot mis-stamp a sibling file's raise.

Golden `exception_statement_ambient` pins all three shapes (SEARCH â `[SEARCH]`, the mixed statement still
spaces â R06 re-pinned across the channel change â and the one-line F3 CONTINUE AFTER â `[CONTINUE]`);
`AmbientExceptionContextTests` pins the engine contract including nesting restore and positional precedence.
Doc-sync: the conditions/exceptions deep-dive's "recorded at the raise site" claim corrected, as ledger F3
predicted it would have to be.

Gate: Unit 4083/4083 Â· characterization 33/33 Â· wave-local Exception|Turn|Ec|Search|Corpus 1949/1949 Â· the
new golden green by name. work.py next: R18 (numerics) Â· R24 (date/time) Â· R12 (interprogram), 13 actionable.

