      *> kb/Work PB2095 - ISO 1989:2023 14.2.3 GR8: "If the argument is
      *> passed by reference, the activated runtime element operates as
      *> if the formal parameter occupies the same storage area as the
      *> argument." A BIT item (a GROUP-USAGE BIT group, a boolean bit
      *> leaf) passed BY REFERENCE is storage like any other: two formals
      *> passed the same bit item, a bit group formal over a bit group in
      *> the middle of a record, a REDEFINED bit formal, and an INVOKE's
      *> two method formals all see every store at once.
      *> cite.py --check 14.2.3 "If the argument is passed by reference,
      *>   the activated runtime element operates as if the formal
      *>   parameter occupies the same storage area as the argument"
      *>   -> OK 14.2.3 8)
      *> cite.py --check 14.9.4.3 "aligned on a byte boundary" -> OK
      *>   14.9.4.3 6) (a BY REFERENCE bit item starts on a byte, so its
      *>   area is a whole number of characters into its storage)
      *> Before the fix a bit item stated no area, so each bit formal
      *> held a copy: G G printed L2=1010 and L1=0101, and the second
      *> copy-back undid the first formal's store.
      *> DERIVATION: BG2S stores 0101 through L1A -> L2A = 0101; 1111
      *>   through L2A -> L1A = 1111; G = 1111. BG = 1010 0011: L1A
      *>   reads 10100011, stores 01010101 -> L2A L2B = 0101 0101; 1111
      *>   through L2B -> L1A = 01011111; RX and RY untouched. BL =
      *>   11110000: 00000001 through M2 -> M1H 0000, M1L 0001. INVOKE
      *>   MB: 0110 through K1A -> K2A 0110; 1001 through K2A -> K1A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2095B02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P2095C02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE P2095C02.
       01 G GROUP-USAGE BIT. 05 G1 PIC 1(4) USAGE BIT VALUE B"1010".
       01 R.
          05 RX PIC X(2) VALUE "AB".
          05 BG GROUP-USAGE BIT.
             10 B1 PIC 1(4) USAGE BIT VALUE B"1010".
             10 B2 PIC 1(4) USAGE BIT VALUE B"0011".
          05 RY PIC X(2) VALUE "CD".
       01 BL PIC 1(8) USAGE BIT VALUE B"11110000".
       01 H GROUP-USAGE BIT. 05 H1 PIC 1(4) USAGE BIT VALUE B"0000".
       PROCEDURE DIVISION.
           CALL "P2095S02" USING G G
           DISPLAY "G=" G
           CALL "P2095T02" USING BG BG
           DISPLAY "BG=" BG " RX=" RX " RY=" RY
           CALL "P2095U02" USING BL BL
           DISPLAY "BL=" BL
           INVOKE P2095C02 "NEW" RETURNING O
           INVOKE O "MB" USING H H
           DISPLAY "H=" H
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2095S02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 GROUP-USAGE BIT. 05 L1A PIC 1(4) USAGE BIT.
       01 L2 GROUP-USAGE BIT. 05 L2A PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING L1 L2.
           MOVE B"0101" TO L1A
           DISPLAY "L2=" L2A
           MOVE B"1111" TO L2A
           DISPLAY "L1=" L1A.
       END PROGRAM P2095S02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2095T02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 GROUP-USAGE BIT. 05 L1A PIC 1(8) USAGE BIT.
       01 L2 GROUP-USAGE BIT. 05 L2A PIC 1(4) USAGE BIT.
                              05 L2B PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING L1 L2.
           DISPLAY "L1=" L1A
           MOVE B"01010101" TO L1A
           DISPLAY "L2=" L2A L2B
           MOVE B"1111" TO L2B
           DISPLAY "L1=" L1A.
       END PROGRAM P2095T02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2095U02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 M1 PIC 1(8) USAGE BIT.
       01 M1R REDEFINES M1 GROUP-USAGE BIT.
          05 M1H PIC 1(4) USAGE BIT.
          05 M1L PIC 1(4) USAGE BIT.
       01 M2 PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING M1 M2.
           MOVE B"00000001" TO M2
           DISPLAY "M1H=" M1H " M1L=" M1L.
       END PROGRAM P2095U02.
       END PROGRAM P2095B02.
       IDENTIFICATION DIVISION.
       CLASS-ID. P2095C02 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 K1 GROUP-USAGE BIT. 05 K1A PIC 1(4) USAGE BIT.
       01 K2 GROUP-USAGE BIT. 05 K2A PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING K1 K2.
           MOVE B"0110" TO K1A
           DISPLAY "K2=" K2A
           MOVE B"1001" TO K2A
           DISPLAY "K1=" K1A.
       END METHOD MB.
       END OBJECT.
       END CLASS P2095C02.
