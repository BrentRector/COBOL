      *> ISO 13.18.27.4 GR3 + 13.18.27.3 SR1 b): a GLOBAL REDEFINES entry in the
      *>   LINKAGE SECTION whose original is a USING formal.
      *> GR3: "it is only the subject of that REDEFINES clause that
      *>   possesses the global attribute."  cite.py --check 13.18.27.4
      *>   -> OK 13.18.27.4 3)
      *> SR1 b): the GLOBAL clause may be specified in a level-1 data
      *>   description entry in the linkage section.
      *>   cite.py --check 13.18.27.3 -> OK 13.18.27.3 1) b)
      *> XR is the global subject; X (the formal) is not global. XR's
      *>   storage IS the caller's argument (14.2.3 GR8 / 13.18.44.4 GR1).
      *> DERIVATION: A = "INIT". PB1523L2 DISPLAYs XR => INIT, MOVEs
      *>   "NEWV" TO XR, which is the caller's A; the main program then
      *>   DISPLAYs A                                    => NEWV
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1523L1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC X(4) VALUE "INIT".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB1523L2" USING A.
           DISPLAY A.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1523L2.
       DATA DIVISION.
       LINKAGE SECTION.
       01  X       PIC X(4).
       01  XR      REDEFINES X GLOBAL PIC X(4).
       PROCEDURE DIVISION USING X.
       SUB-P.
           CALL "PB1523L3".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1523L3.
       PROCEDURE DIVISION.
       SUB2-P.
           DISPLAY XR.
           MOVE "NEWV" TO XR.
           EXIT PROGRAM.
       END PROGRAM PB1523L3.
       END PROGRAM PB1523L2.
       END PROGRAM PB1523L1.
