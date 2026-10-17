      *>--- conformance-case
      *> id: pb3120-group-formal-data-pointer-declaration-mismatch
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3120 - a strongly-typed group formal's restricted
      *> DATA-pointer member is typed by the DECLARATION it is
      *> restricted to (13.18.60.4 GR23; cite.py --check 13.18.60.4
      *> -> OK 13.18.60.4 23)), as the elementary formal's is. 8.5.3.1
      *> (OK 8.5.3.1) makes two declarations of T-REC equivalent only
      *> when each elementary item has the same PICTURE: PX's prototype
      *> (T-REC holds a PIC 9(4)) and its definition (a PIC X(4)) hold
      *> two types, so the pointer members differ, the signatures
      *> differ, and 10.6.2 SR2 (OK 10.6.2 2)) refuses the pair
      *> (COBOLNET1513). Comparing the NAME T-REC alone accepted it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DPX IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 R-A PIC 9(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM DPX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3120D.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB3120D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DPX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 R-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           GOBACK.
       END PROGRAM DPX.
