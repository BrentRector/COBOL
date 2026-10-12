## Entry 1643 — 2026-09-22 15:36 PDT — REGISTRAR #11: PB968–PB984 filed from waves 46–47's leads, every probe re-run on main

**What ran.** A lead-filing pass over the 56 leads the orchestrator extracted from the workflow journals of waves 46
and 47, the wave-45 finishers and landing train 47 (about 30 distinct once the lander's restatements are merged). It
used the reports those leads cite. Every lead was grepped against `kb/Work/` first. Every probe was rebuilt and re-run
on this registrar's own `dotnet build CobolSharp.sln -c Debug` of main `6871b6e10` (0 warnings / 0 errors). Every
citation went through `cite.py --check`. Leads were clustered by mechanism. **The register is 1000 → 1017 notes**, and
`work.py check` says all are well-formed. No verdict was recorded, so GAP is unchanged at 2208.

**Filed (seventeen notes, one per mechanism):**
- **PB968** (MAJOR, wrong answer). RESUME AT NEXT STATEMENT after a failed hoisted FUNCTION activation resumes INTO
  the carrying COMPUTE, which then stores 0. §14.9.33.4 GR2 a) puts the CONTINUE after the statement's end.
- **PB969** (MAJOR, rejects legal source). The keyword-omitted empty argument list `F()` is a parse error. For a
  zero-argument function-pointer it is the only legal keyword-omitted spelling (SR5).
- **PB970** (MAJOR, wrong answer). The CALL channel crosses an image-carried COMP-3 argument as DISPLAY text:
  `PK AFTER 02303` for -42. The same missing storage-image channel makes a POINTER passed BY CONTENT to a PIC X(8)
  formal a run-time EC-PROGRAM-ARG-MISMATCH. That arm needs a determination.
- **PB971** (MAJOR, wrong answer). A reference to an OMITTED formal never raises its fatal *-ARG-OMITTED under
  checking, in the program, method AND function arms. The lead claimed the program arm worked. It does not: the
  callee's statement scope never enables `ProgramArgOmittedChecking`. GR-14.9.4.4-12 is CONFORMS on the lenient half
  only.
- **PB972** (MINOR, under-rejects). §9.3.8.2.3 rule 9 (RAISING conformance) is unenforced, and the override pass is a
  second copy of the rules. The source report's rules 7 and 8 are enforced on main, and its rule-9 repro had the
  direction backwards.
- **PB973** (MAJOR, crash). A method LINKAGE formal named `N` collides with the `__N` paragraph-count const, giving
  CS1628.
- **PB974** (MAJOR, rejects legal source). The REPOSITORY class, interface, property and function specifiers have no
  `AS literal`.
- **PB975** (process). OoClassTable pass-1 diagnostics carry no source position, and a comment carries an inherited
  §8.4.5 citation.
- **PB976** (MINOR, under-rejects). The CLASS clause's FOR phrase is ignored, so all four SR17 class rules go
  unenforced (measured). The lead's latent wrong answer does not reproduce, because both native sets are UTF-16.
- **PB977** (MINOR, under-rejects). The ALPHABET postfix FOR is accepted silently, while the CLASS postfix FOR draws
  a bare COBOL0001.
- **PB978** (MINOR, under-rejects). The report binder's private `LookupQualified` and the ODO fallback `cands[0]` bind
  an ambiguous reference to the first candidate.
- **PB979** (MAJOR, wrong answer + crash). UNSTRING GR11 b)'s examination size is a static compile-time integer.
  An ANY LENGTH receiver examines ONE character (`W     ` for `WXYZ  `), and a reference-modified receiver is staged
  COBOLNET1756. The note carries PB887's "every MOVE-rules transfer binds a MOVE" lead as the structural fix.
- **PB980** (MINOR, under-rejects). UNSTRING SR3 (all-or-nothing national) is unenforced. This was found while
  probing PB887's national-receiver lead. Its float arm cannot arise, because SR4 refuses the receiver.
- **PB981** (MAJOR, rejects legal source + crash). An FD's implicitly shared record area cannot hold a pointer or
  dynamic-length record. Separately, a single FD record with a dynamic-length member aborts the run unit at WRITE.
- **PB982** (MAJOR, crash). A bare non-condition word as an IF condition compiles clean and throws NotImplemented at
  run time, because `BindSoleOperandCondition` returns an error node with no diagnostic.
- **PB983** (MAJOR, rejects legal source). The THROUGH range's `IN alphabet-name` models IN as required. IN is not
  underlined in EVALUATE, VALUE Format 3 or VALUE Format 5. FMT-14.9.13.2 is CONFORMS. The same note covers
  COBOLNET1639 echoing references without spaces.
- **PB984** (process). CONFORMANCE.md carries DOC-A.1-56 twice, and `audit_annex_a1` compares key SETS.

**Extended:**
- **PB715**: the optional-words audit's second blindness, a construct whose anchor keyword is optional. It was not
  re-measured, because the audit needs the private PDF submodule.
- **PB454**: the two walkers of the SPECIAL-NAMES implementor entry.

**Dropped or held, with reasons:**
- The grammar-diagram temp-dir race is PB376, which is open.
- CALL/INVOKE RETURNING into a dynamic-length receiver was measured correct when both sides carry DYNAMIC LENGTH. A
  one-sided pair is correctly refused (§14.8.3.3), and XML/JSON GENERATE are declined vendor extensions.
- PB805's lead L1 is fixed on PB655's in-flight wave-48 branch (the token-level reservation gate).
- The PB388 prose remainder, the seven underived prose sites and the test-assert citations belong to PB388's
  in-flight wave-48 finisher.
- The TYPE Format 2 audit is an unclaimed GAP row with no measured defect, so it stays in the adjudication lane.
- **HELD:** PB830's "leftover literal names the wrong construct". On main, PB830's sink still accepts
  `SOURCE-COMPUTER. IBM-370 "X".` silently, so the residue exists only on PB830's unlanded branch. Re-file it after
  that lands.
- PB887, PB160, PB830, PB836, PB888 and PB889 are in flight on other branches, so they were linked through `cluster:`
  and not edited.

**Take first:** PB971. It is a fatal condition that never fires, in every activation kind. Then PB979 with PB980,
which share the UNSTRING seam, and PB970 with PB965 and PB962, which share the CALL carrier.

