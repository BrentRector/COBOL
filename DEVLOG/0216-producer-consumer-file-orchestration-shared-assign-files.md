## Entry 216 — 2026-05-29: Producer/consumer file orchestration — shared ASSIGN files + COPY-before-NIST order

NIST file-bound tests pass data through companion files: SM203A writes a file that SM204A reads;
ST104A produces the input ST105A sorts. They share by the X-card **number** in their ASSIGN
targets — `XXXXP###` (produce) and `XXXXD###` (consume) with the same number are the *same*
physical file — even though the producer and consumer use different SELECT names (SORTOUT-1D vs
SORTIN-1E). Two bugs blocked this:

1. **Placeholders were not mapped to a shared file.** `XXXXP###`/`XXXXD###` were left
   unsubstituted, so the Binder fell back to `fileSym.Name` for the (unquoted) ASSIGN target —
   sharing only by coincidence of matching SELECT names (SM works, ST does not). Added a
   NistPreprocessor mapping `XXXX[PD](\d+)` → `"TF$1"`, so both ends resolve to one filename keyed
   by the number, regardless of SELECT name.

2. **NIST substitution ran before COPY expansion.** A producer often gets its ASSIGN from a COPY'd
   FILE-CONTROL (SM203A copies K3FCB, whose `SELECT … ASSIGN TO XXXXP002`), so the placeholder
   lived in library text the NIST pass never saw — the producer wrote `test-file.txt` while the
   consumer read `tf002.txt`. Reordered `Compilation.Preprocess` to normalize → **COPY** → NIST, so
   placeholders inside copied text are mapped too. COPY library-name qualifiers stay raw
   placeholders resolved against the copy-library directory, so expanding first is safe.

Verified end-to-end: SM203A→SM204A and ST104A→ST105A now share a file and the consumer reaches
0 FAIL* (ST105A was a hang/NO_OUTPUT before). Guard ALL GREEN (1000 / 336 / 149) — the reorder
did not change any guarded report.

