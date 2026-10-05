      *> kb/Work PB1042 — variable-length components of a CELL-BACKED record: an ADDRESS-OF-taken
      *> record holding a dynamic-capacity table, a dynamic-length item in each occurrence of a
      *> fixed table of an EXTERNAL record, and a dynamic-capacity table nested in another's element.
      *> ISO/IEC 1989:2023 §8.4.3.11.4 GR1: ADDRESS OF DT "contains the address of identifier-1",
      *> and §8.5.1.9.1 3) lets DT hold the table ("may be nested in any combination to the same
      *> number of levels as a fixed-capacity table"). §8.5.1.9.5: an occurrence a statement
      *> creates is INITIALIZEd TO VALUE (the INITIALIZED phrase). §8.5.1.12.1: every occurrence
      *> of E holds its own dynamic-length item, truncated at its LIMIT (§8.5.1.10.4), and
      *> FUNCTION LENGTH counts each at its current length (§15.50.4 r7).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042CV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 DT.
          05 DH PIC X(2) VALUE "HD".
          05 DX OCCURS DYNAMIC CAPACITY IN DC FROM 2 INITIALIZED.
             10 DY PIC X(2) VALUE "YY".
             10 DN PIC 9(2) VALUE 7.
       01 B BASED.
          05 BH PIC X(2).
       01 PB1042XE EXTERNAL.
          05 E OCCURS 3.
             10 EF PIC X.
             10 ED PIC X DYNAMIC LENGTH LIMIT 5.
       01 PB1042XN EXTERNAL.
          05 NO1 OCCURS DYNAMIC CAPACITY IN NC1.
             10 NI OCCURS DYNAMIC CAPACITY IN NC2.
                15 NV PIC X(2).
             10 NL PIC X DYNAMIC LENGTH LIMIT 4.
       01 I PIC 9(4).
       01 K PIC 9(4).
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF DT
           SET ADDRESS OF B TO P
           DISPLAY "1 " DC " [" DT "] " BH
           MOVE "ZZ" TO DY(4)
           DISPLAY "2 " DC " [" DT "]"
           SET DC UP BY 1
           DISPLAY "3 " DC " [" DT "]"
           MOVE 0 TO K
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > DC
              ADD DN(I) TO K
           END-PERFORM
           DISPLAY "4 " K " " FUNCTION LENGTH(DT)
           INITIALIZE DT
           DISPLAY "5 " DC " [" DT "]"
           MOVE "a" TO EF(1) EF(2) EF(3)
           MOVE "BC" TO ED(2)
           MOVE "DEFGHIJ" TO ED(3)
           DISPLAY "6 [" PB1042XE "] [" ED(2) "] [" ED(3) "] "
                   FUNCTION LENGTH(E(3)) " " FUNCTION LENGTH(PB1042XE)
           MOVE "PQ" TO NV(2, 3)
           MOVE "LMN" TO NL(2)
           DISPLAY "7 " NC1 " [" NV(2, 3) "] [" NL(2) "] [" NO1(2) "]"
           STOP RUN.
