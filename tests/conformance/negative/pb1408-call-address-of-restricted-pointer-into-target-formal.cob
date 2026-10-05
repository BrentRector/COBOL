      *> reject-at: 2002 2014 2023
      *> kb/Work PB1408. ISO 14.8.2.3.2 (class pointer): "if either is a restricted pointer, both shall be restricted
      *> and of the same type". ADDRESS OF RP is, by 8.4.3.11.4 GR2, restricted to the type of RP -- PT -- and the
      *> formal FR is restricted to T-REC, the type RP's value addresses, so the argument does not conform: refused
      *> COBOLNET1688 (the CALL arm of the one AddressOfRestriction accessor the SET arm shares). The conforming
      *> spellings are the positive golden 2002/pb1408_address_of_restricted_pointer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1408N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 RP TYPE PT.
       PROCEDURE DIVISION.
       MAIN.
           CALL "ONREC" AS NESTED USING BY CONTENT ADDRESS OF RP
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ONREC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       LINKAGE SECTION.
       01 FR TYPE PT.
       PROCEDURE DIVISION USING FR.
           GOBACK.
       END PROGRAM ONREC.
       END PROGRAM PB1408N3.
