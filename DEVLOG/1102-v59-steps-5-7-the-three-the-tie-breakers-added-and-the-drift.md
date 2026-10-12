## Entry 1102 — 2026-07-29 10:57 PDT — V59 steps 5-7: the three the tie-breakers added, and the drift test that proved itself

V59's correctness landed in step 4 — the image IS the bytes. This entry is the other three of the owner's five
standing tie-breakers: support, understanding, maintenance. Correctness alone would have shipped a compiler that
silently ate every data file written by an earlier build.

**Step 5 — the support diagnostic.** Changing the on-disk record layout is a migration hazard with no
self-description to lean on: a fixed-length record-sequential file is just bytes, so a 5-byte record description
reading an 8-byte-record file produces rubbish, not an error. The provable half of that hazard is arithmetic — a
file whose byte length is not a whole multiple of its record length cannot match the description about to read
it — and `RecordLayoutNotice.CheckFixedLengthFile` now says so at OPEN, naming the file, both lengths and the
remainder, before a single record is misread.

The design discipline here was in what it does NOT do. §14.9.30.4 GR14 already defines the conforming detection:
a record whose byte count falls outside the description's min/max is a SUCCESSFUL read with I-O status '04'.
That fires only on the trailing partial record, only when the total is not a multiple, and only if the program
inspects FILE STATUS — too late and too quiet for a layout migration, but it IS the standard's answer. So the
notice changes nothing conforming: OPEN still succeeds, the I-O status is untouched, '04' still arrives at the
short read. Inventing an OPEN condition the standard does not define would have been non-conformance dressed up
as helpfulness. It writes to STANDARD ERROR, so no golden's stdout moves. LINE SEQUENTIAL and RECORD IS VARYING
are excluded by construction rather than by a flag — delimited records (§9.1.13.2) and per-record length
prefixes have no arithmetic relationship to a fixed width, so for them a differing physical length is NORMAL.

The sibling sweep earned its keep. The first cut wired the check into INPUT and I-O and stopped there, which is
the mode a reader thinks of. EXTEND is the one that matters MORE: appending to a file whose existing layout
disagrees writes a file interleaving two record layouts, and that is permanent corruption rather than a wrong
computation you can re-run. OUTPUT remains excluded, but on a principle rather than an omission — it truncates,
so nothing survives to disagree with the description. Each of the four modes now has a fact asserting which way
it goes.

**Step 6 — the documentation.** §4.2.16's implementor-documentation obligation is the vehicle, but the audience
is the COBOL developer who needs to know what lands on disk, not an auditor ticking A.1 rows. `CONFORMANCE.md`
gained the whole storage-representation family — items 205, 206, 207, 208, 211, 215 — each with the width
ladder, the radix, the byte order and a WORKED BYTE EXAMPLE. Writing them forced three facts into the open that
the code knew and no prose stated: BINARY widths are sign-INDEPENDENT (GR12's precedent), the unsigned packed
sign nibble is `F` and not our legacy engine's `0C`, and INDEX has no character image at all.

**Step 7 — the drift test, which had to justify its own existence.** `v59_length_agrees` compares FUNCTION
LENGTH against FUNCTION BYTE-LENGTH across 17 rows: every fixed-point usage, every width tier, SIGN SEPARATE,
alphanumeric, edited, a mixed-usage group, an OCCURS table. §15.14.4 r1 counts bytes and §15.50.4 r3 counts
alphanumeric character positions, and in a single-byte-character model those cannot disagree — so every line
must read "OK".

The obvious objection is that `ImageWidthIsStorageWidthTests` already pins `ImageWidth == ByteWidth` at the
model level, generatively, so the golden is redundant. I MEASURED it instead of assuming, and the objection is
wrong. Step 4 introduced a THIRD width — `DataItem.DisplayTextWidth`, for the text-facing surfaces — which is
exactly the shape a future maintainer would plausibly mistake for the right answer. Repointing the FUNCTION
LENGTH fold at it turns the golden RED at `B-2` (`PIC 9(2) COMP`: prints 2 digits, occupies 1 byte) while
**all 324 model-level facts stay GREEN**. The model tests pin that two widths agree; only the golden pins WHICH
width the intrinsic actually folds to. That evidence is now written into the golden's own comment header, so the
next reader does not have to re-derive it before "simplifying" the test away.

While extending the grid I also covered the 16-byte BINARY tier (`S9(19)`/`S9(31)` COMP → 16). Step 3 added that
tier and noted it had zero corpus instances — which made it the newest and least-observed width in the compiler,
and the one place drift would have gone unnoticed longest. It answers correctly.

**Gates.** Greenfield Unit **913/913, zero skipped** — the 904 baseline plus exactly the 9 new
`RecordLayoutNotice` facts, including the discriminating pair (13 bytes warns, 15 bytes silent) that proves the
check actually looks at the file rather than always firing, and one fact per OPEN mode. Full Conformance
**4116/4116, zero skipped, nothing red** — the 4115 baseline plus exactly the one new golden; re-run in full
because the connector change touches every sequential OPEN. One process note: the first Conformance run was
started BEFORE the EXTEND sweep landed, so its binary was already superseded — it was killed and re-run against
a verified-current build rather than reported, since a green leg on a stale artifact is not evidence.

**A citation defect the mechanical check caught, exactly as rule 1 predicts.** Validating the new documentation's
clauses with `cite.py --check` failed on `§15.14.4`, and the failure was instructive. The clause number is right
and the quoted text is right — but §15.14.4 and §15.50.4 are titled **"Returned value rules"**, not "General
rules", so labelling them `GR1`/`GR3` names a rule category the clause does not have. `§13.18.60.4` and
`§14.9.30.4` genuinely ARE General rules, so the `GR` labels around them are correct, which is precisely why the
wrong ones read as plausible. This is the INHERITED-citation failure mode: nobody invented it, it propagated.
A repo-wide sweep finds **34 occurrences across three clauses** (§15.14.4, §15.50.4, §15.97.4) in code comments,
design docs, the fix queue and the DEVLOG — including two I had just written into this entry. The V59 change set
is corrected to the `r<N>` form the intrinsic binder already used; the remaining pre-existing sites are swept in
a separate commit so this one stays about V59.

**Friction, for the record.** This session began as a resume after a Windows-update reboot killed the previous
one mid-step. The three steps were already drafted in the working tree and unverified; the reboot cost the
verification, not the work. Recovery was cheap because the tree was self-describing — the plan's §0 named the
remaining steps in order and the untracked files mapped one-to-one onto them.

Separately, and worth logging honestly: all file edits were blocked partway through by a `Semgrep Guardian`
plugin hook that had been enabled three days earlier and failed closed after the reboot lost its session. It
registered PreToolUse/PostToolUse hooks on every Write, Edit and Bash call, and its binary carries
`scanner.semgrep.dev` and `telemetry.semgrep.dev` endpoints. The owner had not knowingly authorized it and
removed it, along with `serena` (whose MCP entry ran `uvx --from git+https://github.com/oraios/serena`,
unpinned, fetching and executing from a GitHub repo at every start). No repository file was written while the
block was active, and none was written by routing around it.

