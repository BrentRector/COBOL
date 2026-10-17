*>--- conformance-case
*> id: apply-commit-rewrite-lock
*> rules: [SR-14.9.35.3-5]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-14.9.35.3-5, one of the seven vacuously-satisfied rules kb/Work PB371 raised (owner
*> decision 2026-09-02: CONFORMS, witnessed by the refusal of the antecedent).
*> THE RULE (ISO 14.9.35.3 SR5): "If the rewrite file is subject to an APPLY COMMIT clause, neither the WITH
*> LOCK phrase nor the WITH NO LOCK phrase shall be specified."
*> REWRITE and its lock phrases are CLAIMED; the APPLY COMMIT clause is Annex A.4.3 item 2 and is refused by
*> name (COBOLNET1709), so no compilable program reaches the antecedent.
*> ⚠ A DIFFERENT A.4 DECLINE LIVES IN THE SAME STATEMENT and is NOT what this fixture pins: REWRITE's FILE
*> phrase is Annex A.4.13 item 2 and draws COBOLNET1706. This fixture writes `REWRITE record-name`, the
*> mandatory alternative, so the only decline it can draw is the APPLY COMMIT clause's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCREWRL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcrewrl.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN I-O F.
           READ F
               AT END CONTINUE
           END-READ.
           REWRITE F-REC WITH LOCK.
           CLOSE F.
           STOP RUN.
