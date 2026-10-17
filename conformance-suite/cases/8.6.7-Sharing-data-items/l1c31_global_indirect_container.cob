      *>--- conformance-case
      *> id: l1c31_global_indirect_container
      *> rules: [GR-8.6.7-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.6.7 2) — a contained program shares the GLOBAL data of
      *> its direct AND its indirect containing programs.
      *> Rule: "If a program is contained within another program, both
      *> programs may refer to data possessing the global attribute
      *> either in the containing program or in any program that
      *> directly or indirectly contains the containing program."
      *> cite.py --check 8.6.7 "either in the containing program or in
      *>   any program that directly or indirectly contains the
      *>   containing program" -> OK  §8.6.7 2)  (Sharing data items)
      *> Structure: L1C31E (outer) contains L1C31F (mid) contains
      *> L1C31G (inner). G1 / G1T are GLOBAL in the outer program (they
      *> INDIRECTLY contain the inner one); G2 is GLOBAL in mid (the
      *> inner program's DIRECT container). "Refer to" the same data =
      *> one storage: a store by either program is seen by the other.
      *> Derivation (G1 = 100, G1T = "AABBCC", G2 = 020 initially;
      *> nothing is INITIAL, so values persist between CALLs):
      *>  call 1: inner ADD 2 TO G1 -> 102, ADD 5 TO G2 -> 025,
      *>          MOVE "ZZ" TO G1E(2) -> AAZZCC.
      *>          mid shows the direct-container item and the outer
      *>          item it also shares:          MID G1=102 G2=025
      *>          outer sees the inner store:   OUTER G1=102 G1T=AAZZCC
      *>  outer then stores G1 = 500, G1E(1) = "QQ" (the inner program
      *>  must see these; a per-program copy would not):
      *>  call 2: inner 500+2 = 502, G2 025+5 = 030, G1T QQZZCC.
      *>          MID G1=502 G2=030 / OUTER G1=502 G1T=QQZZCC
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C31E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1  PIC 999 VALUE 100 GLOBAL.
       01 G1T GLOBAL VALUE "AABBCC".
          05 G1E PIC XX OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1C31F".
           DISPLAY "OUTER G1=" G1 " G1T=" G1T.
           MOVE 500 TO G1.
           MOVE "QQ" TO G1E (1).
           CALL "L1C31F".
           DISPLAY "OUTER G1=" G1 " G1T=" G1T.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C31F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2  PIC 999 VALUE 20 GLOBAL.
       PROCEDURE DIVISION.
       MID-PARA.
           CALL "L1C31G".
           DISPLAY "MID G1=" G1 " G2=" G2.
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C31G.
       PROCEDURE DIVISION.
       IN-PARA.
           ADD 2 TO G1.
           ADD 5 TO G2.
           MOVE "ZZ" TO G1E (2).
           EXIT PROGRAM.
       END PROGRAM L1C31G.
       END PROGRAM L1C31F.
       END PROGRAM L1C31E.
