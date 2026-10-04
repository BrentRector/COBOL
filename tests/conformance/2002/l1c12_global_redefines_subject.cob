      *> ISO §13.18.27.4 GR3 — GLOBAL on a REDEFINES entry makes only its subject a global name
      *> "If the GLOBAL clause is used in a data description entry that contains the REDEFINES clause, it
      *> is only the subject of that REDEFINES clause that possesses the global attribute."
      *>   cite.py --check 13.18.27.4 "If the GLOBAL clause is used in a data description entry that
      *>     contains the REDEFINES clause, it is only the subject of that REDEFINES clause that possesses
      *>     the global attribute."  -> OK  §13.18.27.4 3)  (General rules)
      *> GR2: "A statement in a program contained directly or indirectly within a program that describes a
      *> global name may reference that name without describing it again."
      *>   cite.py --check 13.18.27.4 "A statement in a program contained directly or indirectly within a
      *>     program that describes a global name may reference that name without describing it again."
      *>     -> OK  §13.18.27.4 2)  (General rules)
      *> POSITIVE HALF: WS-B (the subject of "REDEFINES WS-A GLOBAL") is global, so the contained program
      *> L1C12D references it without describing it (GR2), and it IS the storage of WS-A (a redefinition).
      *> The NEGATIVE HALF (WS-A, the redefined item, is NOT global) is the twin
      *> negative/l1g4-global-redefines-object-not-global.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   MOVE "ABCD" TO WS-A; CALL L1C12D, which DISPLAYs WS-B -> the same four bytes  => IN:ABCD
      *>   L1C12D then MOVEs "WXYZ" TO WS-B; back in L1C12C WS-A shows it           => OUT:WXYZ
      *> The main program runs at 2002 (the row's lowest listed edition).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C12C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC X(4) VALUE SPACES.
       01  WS-B    REDEFINES WS-A GLOBAL PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCD" TO WS-A.
           CALL "L1C12D".
           DISPLAY "OUT:" WS-A.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C12D.
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "IN:" WS-B.
           MOVE "WXYZ" TO WS-B.
           GOBACK.
       END PROGRAM L1C12D.
       END PROGRAM L1C12C.
