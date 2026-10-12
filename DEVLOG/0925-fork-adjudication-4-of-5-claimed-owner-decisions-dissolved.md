## Entry 925 — 2026-07-19 19:20 PDT — Fork adjudication: 4 of 5 claimed "owner decisions" dissolved into spec, three ISO drafting defects found, and a new standing rule about the OCR'd spec

The owner ruled that PICTURE EDITING (C4) and PERFORM Format 3 (C5) STAY in the grammar batch and their spec
forks get adjudicated now (so D16's close-line stands, unchanged). Before spending owner decisions I checked the
verifiers' framing against the ISO text — and the very first fork was **misattributed**: §13.18.40.3 SR9 requires
the EDITING phrase's literal-1/2/3 to be *"national literals"* or *"alphanumeric literals"*, and a figurative
constant is a distinct category (§8.3.3.6). So C4 has no figurative-length question at all — it needs an SR9
*reject* — and the fork actually belongs to **C6B SUPPRESS WHEN**, whose §12.4.5.6.3 SR7 does admit figuratives.
Had I relayed the packet as written, the owner would have adjudicated a question about the wrong construct.

An 11-agent adjudication pass then read each fork against the standard and adversarially re-read every
conclusion. **Four of five dissolved into spec; one genuine fork survives**, and it is low-stakes:

- **C4 repeated IS-form character-1 — LEGAL.** Editing rules 3 and 5 *partition* character-1: with literal-1
  specified it is a **simple insertion** symbol, so SR24's *"for fixed insertion with editing sign control
  symbols"* scope never reaches it. Killed twice over — SR25 independently makes SR24 count EDITING *phrases*.
  `PIC 9G9G9 EDITING "G" IS "<>"` is legal; size is per occurrence (3 + 2×2 = 7).
- **C4 both NEGATIVE and POSITIVE in one FOR phrase — YES.** Settled by **rendering the printed Format-1 figure
  at 600 dpi**: it carries the §5.2.6.4 choice-indicator bars that our markdown transcription dropped. The
  "fork" was an artifact of OCR loss, not of the standard.
- **C4 may the IS form FLOAT — NO** (the FOR form does). Editing rule 6's floating-symbol list is closed, and
  proved *selective* by CR/DB's absence from it.
- **C5 FINALLY on the fatal path — DOES NOT RUN.** §14.9.28.4 GR16 makes FINALLY definitionally *the end of the
  PERFORM*, which runs iff control reaches it. Emit plain sequential code, never a C# `finally`. Six-case table
  agreed by both readers.
- **C6B figurative suppression literal — replicates to the key's length** (§8.3.3.6.4 GR2, whose NOTE 1 makes
  *"compared with"* an association). The strict counter-reading was closed off by observing it would make GR2
  the empty set, since MOVE itself has no statement-local length rule.

**Three defects in the STANDARD ITSELF** were identified, and they get recorded rather than silently coded
around — otherwise a future maintainer "fixes" the compiler back to wrong behavior: (a) §13.18.40.3 SR12
sentence 1 mislabels the IS form a *"fixed editing sign control symbol"*, a term-of-art collision GR14 exposes;
(b) §13.18.40.6's precedence borrow, applied literally, would outlaw both `LLLL` and any picture pairing 'X'
with character-1 — repealing GR7/GR10 — so it resolves in favour of editing rule 6; (c) §14.9.28.4 GR18 has two
consecutive contradictory sentences and a malformed cross-reference (*"General rule 14.9.29"* for *"General rule
20"*); implement GR18 as GR20.

**The adversarial re-read earned its keep in a way I did not expect.** It changed the *grounds* but not the
outcome on four of five — and in three of those the first reader's stated rationale was simply WRONG and would
have misled the next maintainer ("rule 3 alone resolves it"; "Table 10 never governs repetition"; "§8.8.4.2.7(2)
is unreachable, assert on it"). The last was **behavior-changing**: it would have shipped a live defect rejecting
the legal `SUPPRESS WHEN "AB"` on a longer key. Lesson recorded: when a verification pass agrees with a verdict,
check whether it agrees with the *reasoning* — a right answer held for a wrong reason is a latent defect.

**New standing rule for this repo:** trust `specs/ISO_COBOL.md` for RULE TEXT (verified faithful — several
apparent garbles are in the printed standard too), but **render the PDF page image whenever a general-format
DIAGRAM is load-bearing**. Figure choice-indicator bars do not survive transcription, and that loss alone
manufactured an entire owner-decision-grade fork.

Remaining for the owner: exactly one question — whether a `>>TURN` written inside imperative-statement-1 of a
format-3 PERFORM should draw a suppressible diagnostic (§7.3.25.3 SR5 read flat vs narrow). Runtime semantics are
identical either way; only the diagnostic differs. Evidence at
`docs/rearchitecture/evidence/PHASE-13-spec-fork-adjudication.json`.

