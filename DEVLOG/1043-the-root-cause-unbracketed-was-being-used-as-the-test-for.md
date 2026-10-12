## Entry 1043 — 2026-07-27 02:05 PDT — The root cause: "unbracketed" was being used as the test for "required word"

Widened the audit and found the thing underneath the last three entries. **One wrong criterion, five sites.**

The codebase repeatedly justifies a "leniency" with: *this word is unbracketed in the ISO format, therefore
required*. That criterion is wrong. §5.2.2/§5.2.3 make **underlining** the test for a required word. Bracketing
marks whether a whole PHRASE may be omitted; underlining marks whether a WORD inside it must be written. They are
independent, and conflating them turns conforming source into a diagnosed "extension".

Measured per word off the printed pages: `KEY` is un-underlined in `INVALID KEY` in **all five** statements that
have the phrase (DELETE 635, READ 722, REWRITE 740, START 784, WRITE 816) and in the `RECORD KEY` clause (359,
where `RECORD` and `SOURCE` are underlined and `KEY`/`IS` are not); `COLLATING` is un-underlined (687, 776); `AT`
is un-underlined in **0 of 11** occurrences; `PRINTING` is un-underlined (795). So `INVALID <imperative>`,
`RECORD data-name`, `SEQUENCE alphabet-name`, `SEARCH … END …` and bare `SUPPRESS` are all CONFORMING ISO.

**A first: the audit found the grammar calling standard COBOL a vendor extension.** `searchAtEndClause` was the
lone hold-out on `AT` — `readAtEnd`, `returnAtEndPhrase` and `writeAtEndOfPage` already had `AT?` — and instead
of `AT?` it admitted the AT-less form through a separate alternative commented *"NIST / IBM extension: AT-less
END"*. That mis-stated the standard AND silently denied the AT-less spelling to the NOT branch. `AT?` subsumes it.

**The other half of the discovery is worse and I want it on the record.** The legacy compiler doesn't just accept
these — under strict mode it REPORTS them: `CBL3611` (error) / `CBL3612` (warning) when `KEY` is omitted, and the
same for `COLLATING` (leniency L5). It diagnoses legal COBOL. It lives in `src/CobolSharp.Compiler`, the
differential oracle deleted at P15, and `src/Cobol.Net.*` has no equivalent — so the LIVE compiler is unaffected,
which is why this is filed (SR2) rather than fixed tonight. Do not port it.

Landed: `searchAtEndClause` → `AT?`; `suppressStatement` → `PRINTING?`; the two misclassified-leniency comments
in `CobolIO.g4` corrected in place.

Golden `2002/rw_suppress_bare` is `rw_suppress` with `PRINTING` omitted and **nothing else changed**, pinning the
SAME expected output byte-for-byte. That is the actual claim of §5.2.3 — omitting an optional word is a different
SPELLING of the same statement, not a different statement — and asserting it against identical bytes is stronger
than asserting the statement merely parses.

**What is deliberately NOT changed.** `KEY`, `ON`, `RECORD` and `WITH` measure SPLIT across pages 600-829 —
required in some constructs, optional in others. The tool reports them as needing per-site judgement and I left
them, because that is exactly the shape of evidence that made `AFTER`/`SECONDS` a mistake yesterday.

Gates: build clean · filtered suppress/search gate 19/19 · full Conformance 3916/3916 (10m10s).

