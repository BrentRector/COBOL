      *>--- conformance-case
      *> id: l1c16_io_section_paragraphs
      *> rules: [FMT-12.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.2 General format — both paragraphs of the
      *>   input-output section are optional
      *> "INPUT-OUTPUT SECTION. [ file-control-paragraph ] [
      *>   i-o-control-paragraph ]"
      *>   cite.py: OK  §12.4.2   (General format)
      *> The paragraphs themselves (the forms used below):
      *> "FILE-CONTROL. [ file-control-entry ] ..."
      *>   cite.py: OK  §12.4.4.2   (General format)
      *> "I-O-CONTROL . [ [ apply-commit-clause] . ] [ [ { same-clause
      *>   }... ]. ]"
      *>   cite.py: OK  §12.4.6.2   (General format)
      *> Each arm of the format is written once, one program per arm:
      *>   L1C16A  INPUT-OUTPUT SECTION. with NEITHER paragraph (both
      *>     optional)
      *>   L1C16B  I-O-CONTROL paragraph only (the
      *>     file-control-paragraph omitted)
      *>   L1C16C  FILE-CONTROL paragraph only (the
      *>     i-o-control-paragraph omitted)
      *>   L1C16D  both paragraphs, FILE-CONTROL first (the format's
      *>     order)
      *> The ordering arm (I-O-CONTROL before FILE-CONTROL) is the
      *>   negative
      *> l1c16-io-control-before-file-control.
      *> DERIVATION: every arm is legal source, so the run unit compiles
      *>   and each
      *> program DISPLAYs its line in CALL order:
      *>   "A NEITHER PARAGRAPH"            L1C16A's own DISPLAY
      *>   "B I-O-CONTROL ONLY"             CALL "L1C16B"
      *>   "C FILE-CONTROL ONLY CCCC"       CALL "L1C16C": the record
      *>     written and
      *>                                    read back through the one
      *>                                      SELECTed file
      *>   "D BOTH PARAGRAPHS DDDD"         CALL "L1C16D": likewise
      *>     through F2
      *>   "A END"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C16A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       PROCEDURE DIVISION.
       A-MAIN.
           DISPLAY "A NEITHER PARAGRAPH".
           CALL "L1C16B".
           CALL "L1C16C".
           CALL "L1C16D".
           DISPLAY "A END".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C16B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
       PROCEDURE DIVISION.
       B-MAIN.
           DISPLAY "B I-O-CONTROL ONLY".
           EXIT PROGRAM.
       END PROGRAM L1C16B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C16C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C16C.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       PROCEDURE DIVISION.
       C-MAIN.
           OPEN OUTPUT F1.
           MOVE "CCCC" TO R1.
           WRITE R1.
           CLOSE F1.
           MOVE SPACES TO R1.
           OPEN INPUT F1.
           READ F1.
           DISPLAY "C FILE-CONTROL ONLY " R1.
           CLOSE F1.
           EXIT PROGRAM.
       END PROGRAM L1C16C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C16D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F2 ASSIGN TO "L1C16D2.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "L1C16D3.DAT"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME RECORD AREA FOR F2 F3.
       DATA DIVISION.
       FILE SECTION.
       FD  F2.
       01  R2 PIC X(4).
       FD  F3.
       01  R3 PIC X(4).
       PROCEDURE DIVISION.
       D-MAIN.
           OPEN OUTPUT F2.
           MOVE "DDDD" TO R2.
           WRITE R2.
           CLOSE F2.
           MOVE SPACES TO R2.
           OPEN INPUT F2.
           READ F2.
           DISPLAY "D BOTH PARAGRAPHS " R2.
           CLOSE F2.
           EXIT PROGRAM.
       END PROGRAM L1C16D.
       END PROGRAM L1C16A.
