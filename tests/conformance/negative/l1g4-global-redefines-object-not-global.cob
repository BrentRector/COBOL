      *> reject-at: 2002 2014 2023
      *> ISO §13.18.27.4 GR3 — the OBJECT of a GLOBAL REDEFINES entry is not a global name
      *> GR3: "If the GLOBAL clause is used in a data description entry
      *>   that contains the REDEFINES clause, it is only the subject of
      *>   that REDEFINES clause that possesses the global attribute."
      *>   cite.py --check 13.18.27.4 -> OK §13.18.27.4 3)
      *> WS-A is the redefined item, not the subject, so it is not a
      *>   global name; the contained program L1G4GNB does not describe
      *>   it, so its reference identifies no resource. §8.4.2.1: "In
      *>   order to use a resource, a statement shall contain a reference
      *>   that uniquely identifies that resource" (cite.py --check
      *>   8.4.2.1 -> OK). Diagnostic: COBOLNET1639 undefined-reference
      *>   (docs/DIAGNOSTICS.md). Twin of the positive witness
      *>   2002/l1g4_global_redefines_subject_group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G4GNA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC X(4) VALUE "1234".
       01  WS-B    REDEFINES WS-A GLOBAL.
           05  WS-B1   PIC 99.
           05  WS-B2   PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "L1G4GNB".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G4GNB.
       PROCEDURE DIVISION.
       SUB-P.
           DISPLAY "IN:" WS-A.
           GOBACK.
       END PROGRAM L1G4GNB.
       END PROGRAM L1G4GNA.
