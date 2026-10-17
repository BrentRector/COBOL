      *>--- conformance-case
      *> id: l1c08_environment_division_shapes
      *> rules: [FMT-12.2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.2.1 FMT — ENVIRONMENT DIVISION. with each optional
      *> section: neither, configuration only, input-output only, both
      *> Format: "ENVIRONMENT DIVISION." followed by
      *>   "[ configuration-section ]" then "[ input-output-section ]"
      *> cite.py:
      *>   OK  §12.2.1   (General format)  [ configuration-section ]
      *>   OK  §12.2.1   (General format)  [ input-output-section ]
      *> Both sections are optional and, when both appear, the
      *> configuration section comes first. Four programs cover the
      *> four legal shapes; each section that is present is shown to be
      *> in effect (DECIMAL-POINT IS COMMA edits with a comma; a file
      *> SELECTed in FILE-CONTROL is written and read back). The order
      *> violation is the negative l1c08-env-division-section-order.
      *> DERIVATION OF EVERY EXPECTED LINE:
      *> G-START       L1C08G: the division header alone (both sections
      *>               omitted) is a complete environment division.
      *> H ED=12,50    L1C08H: configuration section only; its
      *>               DECIMAL-POINT IS COMMA makes the Z9,99 edit of
      *>               12,5 print a comma.
      *> I REC=IREC    L1C08I: input-output section only; the file
      *>               SELECTed there is written and read back.
      *> J ED=03,25 REC=JREC  L1C08J: both sections in format order;
      *>               both are in effect (99,99 edit of 3,25).
      *> G-END         control returns to L1C08G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08G.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       G-MAIN.
           DISPLAY "G-START".
           CALL "L1C08H".
           CALL "L1C08I".
           CALL "L1C08J".
           DISPLAY "G-END".
           STOP RUN.
       END PROGRAM L1C08G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08H.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 99V99 VALUE 12,5.
       01 ED PIC Z9,99.
       PROCEDURE DIVISION.
       H-MAIN.
           MOVE N TO ED.
           DISPLAY "H ED=" ED.
           EXIT PROGRAM.
       END PROGRAM L1C08H.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "L1C08I.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD IF1.
       01 IR PIC X(4).
       PROCEDURE DIVISION.
       I-MAIN.
           OPEN OUTPUT IF1.
           MOVE "IREC" TO IR.
           WRITE IR.
           CLOSE IF1.
           MOVE SPACES TO IR.
           OPEN INPUT IF1.
           READ IF1.
           DISPLAY "I REC=" IR.
           CLOSE IF1.
           EXIT PROGRAM.
       END PROGRAM L1C08I.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08J.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT JF1 ASSIGN TO "L1C08J.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD JF1.
       01 JR PIC X(4).
       WORKING-STORAGE SECTION.
       01 N  PIC 99V99 VALUE 3,25.
       01 ED PIC 99,99.
       PROCEDURE DIVISION.
       J-MAIN.
           OPEN OUTPUT JF1.
           MOVE "JREC" TO JR.
           WRITE JR.
           CLOSE JF1.
           MOVE SPACES TO JR.
           OPEN INPUT JF1.
           READ JF1.
           MOVE N TO ED.
           DISPLAY "J ED=" ED " REC=" JR.
           CLOSE JF1.
           EXIT PROGRAM.
       END PROGRAM L1C08J.
