*>--- conformance-case
*> id: apply-commit-read-lock
*> rules: [SR-14.9.30.3-5]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-14.9.30.3-5, one of the seven vacuously-satisfied rules kb/Work PB371 raised (owner
*> decision 2026-09-02: CONFORMS, witnessed by the refusal of the antecedent).
*> THE RULE (ISO 14.9.30.3 SR5, inside the format-1 sublist): "If file-name-1 is subject to an APPLY COMMIT
*> clause, none of the phrases IGNORING LOCK, WITH LOCK, or WITH NO LOCK shall be specified."
*> READ and its record-locking phrases are CLAIMED; the APPLY COMMIT clause is Annex A.4.3 item 2 and is
*> refused by name (COBOLNET1709), so the antecedent is unreachable and the rule holds.
*> ⚠ NOT to be confused with the neighbouring SR-14.9.30.3-4, whose antecedent is "If automatic locking has
*> been specified for file-name-1" - a LOCK MODE condition that IS reachable, with live content of its own.
*> The two rules forbid the same three phrases under different antecedents; only this one is vacuous.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCREADL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcreadl.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT F.
           READ F WITH LOCK
               AT END CONTINUE
           END-READ.
           CLOSE F.
           STOP RUN.
