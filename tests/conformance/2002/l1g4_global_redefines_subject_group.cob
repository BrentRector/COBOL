      *> ISO §13.18.27.4 GR3 — GLOBAL on a REDEFINES entry: its subject (a group) is global
      *> GR3: "If the GLOBAL clause is used in a data description entry
      *>   that contains the REDEFINES clause, it is only the subject of
      *>   that REDEFINES clause that possesses the global attribute."
      *>   cite.py --check 13.18.27.4 -> OK §13.18.27.4 3)
      *> GR1: "All data-names subordinate to a global name are global
      *>   names."  cite.py --check 13.18.27.4 -> OK §13.18.27.4 1)
      *> GR2: "A statement in a program contained directly or indirectly
      *>   within a program that describes a global name may reference
      *>   that name without describing it again."
      *>   cite.py --check 13.18.27.4 -> OK §13.18.27.4 2)
      *> POSITIVE HALF: WS-B, the subject of "REDEFINES WS-A GLOBAL", is a
      *>   global name, so are WS-B1/WS-B2 (GR1); contained L1G4GRB
      *>   references them undeclared (GR2). WS-B IS WS-A's storage.
      *> NEGATIVE HALF (WS-A is NOT global): the twin
      *>   negative/l1g4-global-redefines-object-not-global.
      *> ⚠ The ELEMENTARY-subject form of this rule crashes the backend
      *>   today (kb/Work PB1523, held repro adjudication/golden-lane-1/
      *>   held-repros/l1c12_global_redefines_subject.cob), so this group
      *>   form is a witness, not the row's closure.
      *> DERIVATION:
      *>   WS-A = "1234"; L1G4GRB DISPLAYs WS-B (the same 4 characters)
      *>   and WS-B2 (characters 3-4)                    => IN:1234 34
      *>   L1G4GRB MOVEs 99 TO WS-B1 (characters 1-2); the container then
      *>   DISPLAYs WS-A, the shared storage             => OUT:9934
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G4GRA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC X(4) VALUE "1234".
       01  WS-B    REDEFINES WS-A GLOBAL.
           05  WS-B1   PIC 99.
           05  WS-B2   PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "L1G4GRB".
           DISPLAY "OUT:" WS-A.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G4GRB.
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "IN:" WS-B " " WS-B2.
           MOVE 99 TO WS-B1.
           GOBACK.
       END PROGRAM L1G4GRB.
       END PROGRAM L1G4GRA.
