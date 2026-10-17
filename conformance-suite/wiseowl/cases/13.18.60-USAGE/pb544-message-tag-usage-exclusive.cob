*>--- conformance-case
*> id: pb544-message-tag-usage-exclusive
*> rules: [SR-13.18.60.3-21]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB544 — §13.18.60.3 syntax rule 21: "If MESSAGE-TAG is specified, no other usage clauses
      *> shall be specified in the data description entry."
      *> ⛔ THE RULE HAD NO SUBJECT. USAGE MESSAGE-TAG is an Annex A.3 item-4 processor-dependent element this
      *> compiler declines BY NAME (COBOLNET1943) — but a declined usage still has syntax rules over it, and
      *> this entry used to compile with NO DIAGNOSTIC AT ALL, not even the decline: the binder kept a single
      *> `usageText` that each successive USAGE clause overwrote, so the entry bound as a plain DISPLAY item
      *> and SR21's subject — the SET of usage clauses in the entry — did not exist. COBOLNET2158 is raised
      *> from the clause LIST, BEFORE and independently of the decline, so the rule is enforced whichever
      *> order the two clauses are written in.
      *> ⚠ PINNED AT 2023 ONLY, on purpose. MESSAGE-TAG is an Annex E.2 item-25 COBOL-2023 addition, so 2023
      *> is the edition where SR21 is unambiguously the right answer. Below 2023 the word is a user-defined
      *> word and the edition-correct diagnostic is the introduction gate; that routing is broken for the
      *> whole MESSAGE-TAG surface (COBOLNET1943 fires at 2002 and 2014 against its own message text) and is
      *> registered as its own mechanism. A golden that also pinned 2002 would freeze that defect.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB544EX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  M USAGE MESSAGE-TAG USAGE DISPLAY PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
