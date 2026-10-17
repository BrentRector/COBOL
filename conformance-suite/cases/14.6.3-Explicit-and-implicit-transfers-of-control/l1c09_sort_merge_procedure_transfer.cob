      *>--- conformance-case
      *> id: l1c09_sort_merge_procedure_transfer
      *> rules: [GR-14.6.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.3 2) — SORT and MERGE transfer control implicitly to
      *> their input and output procedures.
      *> Rule: "When a SORT or MERGE statement is executed, an implicit
      *> transfer of control occurs to any associated input or output
      *> procedures."
      *> cite.py: OK  §14.6.3 2)  (Explicit and implicit transfers of
      *>            control)
      *> cite.py: OK  §14.9.40.4 11)  (General rules) SORT "control is
      *>          passed to the input procedure before the file ... is
      *>          sequenced"
      *> cite.py: OK  §14.9.40.4 14)  (General rules) SORT "control
      *>          passes to it after the file ... has been sequenced"
      *> cite.py: OK  §14.9.24.4 9)  (General rules) MERGE "control
      *>          passes to it during execution of the MERGE statement"
      *> Neither procedure is named by a PERFORM or GO TO: the only way
      *> they can run is the implicit transfer the rule requires.
      *>   BEFORE-SORT                 statement before the SORT
      *>   IN-PROC                     GR11: input procedure runs; it
      *>                               RELEASEs CCC, AAA, BBB
      *>   OUT AAA / OUT BBB / OUT CCC GR14: output procedure runs after
      *>                               sequencing (ASCENDING key)
      *>   OUT-END                     last paragraph of the procedure
      *>   AFTER-SORT                  return mechanism -> next
      *>            statement
      *>   BEFORE-MERGE
      *>   MOUT AAA .. MOUT DDD        MERGE GR9: output procedure runs
      *>                               over F1 (AAA CCC) + F2 (BBB DDD)
      *>   MOUT-END
      *>   AFTER-MERGE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C09S1.DAT".
           SELECT F2 ASSIGN TO "L1C09S2.DAT".
           SELECT SF ASSIGN TO "L1C09S3.TMP".
           SELECT MF ASSIGN TO "L1C09S4.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(3).
       FD F2.
       01 R2 PIC X(3).
       SD SF.
       01 SR.
          05 SK PIC X(3).
       SD MF.
       01 MR.
          05 MK PIC X(3).
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       M1.
           OPEN OUTPUT F1.
           MOVE "AAA" TO R1.
           WRITE R1.
           MOVE "CCC" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "BBB" TO R2.
           WRITE R2.
           MOVE "DDD" TO R2.
           WRITE R2.
           CLOSE F2.
           DISPLAY "BEFORE-SORT".
           SORT SF ON ASCENDING KEY SK
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "AFTER-SORT".
           DISPLAY "BEFORE-MERGE".
           MERGE MF ON ASCENDING KEY MK
               USING F1 F2
               OUTPUT PROCEDURE IS MOUT-PROC.
           DISPLAY "AFTER-MERGE".
           STOP RUN.
       IN-PROC SECTION.
       I1.
           DISPLAY "IN-PROC".
           MOVE "CCC" TO SK.
           RELEASE SR.
           MOVE "AAA" TO SK.
           RELEASE SR.
           MOVE "BBB" TO SK.
           RELEASE SR.
       OUT-PROC SECTION.
       O1.
           RETURN SF AT END GO TO O9.
           DISPLAY "OUT " SK.
           GO TO O1.
       O9.
           DISPLAY "OUT-END".
       MOUT-PROC SECTION.
       N1.
           RETURN MF AT END GO TO N9.
           DISPLAY "MOUT " MK.
           GO TO N1.
       N9.
           DISPLAY "MOUT-END".
