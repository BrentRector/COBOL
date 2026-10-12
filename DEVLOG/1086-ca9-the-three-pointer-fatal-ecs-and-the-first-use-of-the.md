## Entry 1086 — 2026-07-28 16:21 PDT — CA9: the three pointer fatal ECs, and the first use of the checking-OFF rule

Step 6 — Track A opens. The first finding implemented under the owner's decided checking-OFF doctrine, and the
first to benefit from the flag restructure.

**The rules, all four read verbatim first.** §13.18.5.4 GR3 — "If the subject of the entry or any item
subordinate to it is referenced directly or indirectly while its address is NULL, the EC-DATA-PTR-NULL
exception condition is set to exist"; GR4 — "referenced while its address is not NULL and not a valid address
of storage" → EC-BOUND-PTR; §14.9.39 Format 10 GR18 — a NULL identifier-9 → EC-DATA-PTR-NULL; GR19 — a
non-integer amount → EC-SIZE-ADDRESS, "the execution of the SET statement is unsuccessful, and the content of
identifier-9 is unchanged". All three names were already registered Fatal, so no catalog change.

**⛔ THE RECIPE AS WRITTEN IN THE FINDING IS WRONG AND THE OWNER'S RULE CORRECTED IT.** The finding says to
keep "the existing loud throw as the checking-OFF fall-through" at all six raise sites. The decided rule is
LENIENT wherever the standard names the outcome, LOUD ABORT wherever it names none — so the sites split:
  · `UpBy` / `UpByScaled` (SET pointer UP/DOWN BY) now return the operand UNCHANGED when checking is off,
    because GR19 states exactly that outcome.
  · The four `Deref` sites keep their unconditional throw, because GR3/GR4 name no outcome and `Deref` must
    return a `StorageCell` — "lenient" there could only mean fabricating a cell and running on garbage. The
    five-compiler survey behind that decision is recorded at the site.
The helper is still called FIRST on the Deref path, so a CHECKING-ON dereference sets the last-exception status
and raises the correctly-NAMED condition for the statement guard — which is the whole observable difference.

**The restructure paid off exactly as intended.** The three new flags are three fields in `CheckingFlags` plus
three delegating properties; `ExceptionCheckingFlagsDriftTests` went from 7 flags to 10 and passed unchanged,
because PUSH ALL is a struct copy. Under the hand-rolled save/restore this commit would have had to remember to
extend it, and nothing would have failed if it had not.

**Ambient, not precise, and the comment says why:** a BASED dereference renders INLINE through the generated
bridge property that aliases `CobolPtr.Deref`, so there is no single node kind for a precise `QueryFor` case to
match — the same reason EC-BOUND-REF-MOD is ambient. Conservative wrapping is harmless: the raise fires only at
an actual pointer operation, so the guard around a pointer-free statement never catches anything.

**Golden** `2023/ec_data_ptr_null`, registered same commit: a BASED item whose address is never SET, so
`DISPLAY B` dereferences NULL; the declarative reports via FUNCTION EXCEPTION-STATUS and RESUMEs past it.
Expected value is spec-derived — the EC name from GR3, the 31-character width from §15.33 — not copied from a
run. Without the fix: abnormal termination with the declarative never selected.

**Gate:** characterization 33/33 · Conformance CorpusRunner+ExceptionCondition+Pointer+Based 424/424 · the
flags drift test 3/3.

