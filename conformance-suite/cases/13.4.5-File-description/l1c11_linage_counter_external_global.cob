      *>--- conformance-case
      *> id: l1c11_linage_counter_external_global
      *> rules: [GR-13.4.5.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.4.5.4 GR3 — LINAGE-COUNTER of an EXTERNAL file is
      *>   external; of a GLOBAL file, a global name
      *> Rule: "If the file description entry for a sequential file
      *>   contains the LINAGE clause and the EXTERNAL clause, the
      *>   LINAGE-COUNTER data item is an external data item. If the
      *>   file description entry for a sequential file contains the
      *>   LINAGE clause and the GLOBAL clause, LINAGE-COUNTER is a
      *>   global name."
      *>   cite.py --check 13.4.5.4 "the LINAGE-COUNTER data item is
      *>   an external data item" -> OK  §13.4.5.4 3)  (General rules)
      *>   cite.py --check 13.4.5.4 "LINAGE-COUNTER is a global name"
      *>   -> OK  §13.4.5.4 3)  (General rules)
      *>   cite.py --check 13.18.34.4 "The value of LINAGE-COUNTER is
      *>   automatically set to one at the time an OPEN statement with
      *>   the OUTPUT phrase is executed for the associated file"
      *>   -> OK  §13.18.34.4 7) d)  (printed under "7) a)")
      *>   cite.py --check 13.18.34.4 "When the ADVANCING phrase of the
      *>   WRITE statement is not specified, the LINAGE-COUNTER is
      *>   incremented by the value one"
      *>   -> OK  §13.18.34.4 7) c) 3.  (printed under "7) a)")
      *> GF is GLOBAL in L1C11C; the contained program L1C11CG names
      *> LINAGE-COUNTER with and without the qualifier OF GF (GF's is
      *> the only LINAGE-COUNTER visible there). XF is EXTERNAL and
      *> described identically in L1C11C and in the separately
      *> compiled program L1C11CX, which names its LINAGE-COUNTER.
      *> Page sizes (20, 10) are never exceeded, so every WRITE adds 1.
      *> Derivation:
      *>   MAIN GF=0003 XF=0002  after OPEN OUTPUT (both 1), two
      *>                         WRITEs to GF and one to XF.
      *>   CG GF=0003            the global name in the containee is
      *>                         the containing program's counter.
      *>   CG GF=0004            after the containee's WRITE to GF
      *>                         (unqualified reference).
      *>   MAIN GF=0004          the containing program sees it.
      *>   CX XF=0002            the external item is the one counter
      *>                         of the run unit: L1C11C's value.
      *>   CX XF=0003            after L1C11CX's own WRITE to XF.
      *>   MAIN XF=0003          L1C11C sees L1C11CX's increment.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GF ASSIGN TO "L1C11CG.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT XF ASSIGN TO "L1C11CX.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD GF IS GLOBAL
           LINAGE IS 20 LINES.
       01 GF-REC PIC X(4).
       FD XF IS EXTERNAL
           LINAGE IS 10 LINES.
       01 XF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 W-G PIC 9(4).
       01 W-X PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT GF XF.
           MOVE "G001" TO GF-REC.
           WRITE GF-REC.
           MOVE "G002" TO GF-REC.
           WRITE GF-REC.
           MOVE "X001" TO XF-REC.
           WRITE XF-REC.
           MOVE LINAGE-COUNTER OF GF TO W-G.
           MOVE LINAGE-COUNTER OF XF TO W-X.
           DISPLAY "MAIN GF=" W-G " XF=" W-X.
           CALL "L1C11CG".
           MOVE LINAGE-COUNTER OF GF TO W-G.
           DISPLAY "MAIN GF=" W-G.
           CALL "L1C11CX".
           MOVE LINAGE-COUNTER OF XF TO W-X.
           DISPLAY "MAIN XF=" W-X.
           CLOSE GF XF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11CG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C-G PIC 9(4).
       PROCEDURE DIVISION.
       CG-P.
           MOVE LINAGE-COUNTER OF GF TO C-G.
           DISPLAY "CG GF=" C-G.
           MOVE "G003" TO GF-REC.
           WRITE GF-REC.
           MOVE LINAGE-COUNTER TO C-G.
           DISPLAY "CG GF=" C-G.
           EXIT PROGRAM.
       END PROGRAM L1C11CG.
       END PROGRAM L1C11C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11CX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "L1C11CX.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD XF IS EXTERNAL
           LINAGE IS 10 LINES.
       01 XF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 C-X PIC 9(4).
       PROCEDURE DIVISION.
       CX-P.
           MOVE LINAGE-COUNTER TO C-X.
           DISPLAY "CX XF=" C-X.
           MOVE "X002" TO XF-REC.
           WRITE XF-REC.
           MOVE LINAGE-COUNTER OF XF TO C-X.
           DISPLAY "CX XF=" C-X.
           EXIT PROGRAM.
       END PROGRAM L1C11CX.
