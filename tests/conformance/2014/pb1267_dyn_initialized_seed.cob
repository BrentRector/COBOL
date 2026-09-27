      *> kb/Work PB1267 - an element a STATEMENT creates in a dynamic-capacity table whose OCCURS says
      *> INITIALIZED is initialized "as though they had been the subject of a statement of the form
      *> INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO DEFAULT" (ISO 8.5.1.9.5; 13.18.38.4 GR18), NOT from
      *> the initial-state recipe:
      *> - a VALUE-less NUMERIC-EDITED leaf receives "Figurative constant ZEROES" (14.9.20.4 GR6 c) by MOVE,
      *>   i.e. its EDITED zero: ZZ9 -> "  0", $Z9.99 -> "$ 0.00", 999- -> "000 ", 99/99 -> "00/00";
      *> - a GROUP-level VALUE is not an INITIALIZE sender (GR5 c) 1. b names "a data-item format VALUE clause
      *>   ... in the data description entry of the elementary data item"), so RG's leaves take their own
      *>   default, spaces;
      *> - an elementary VALUE is the sender GR6 a) 3. derives from it: RV = 42.
      *> Occurrence 1 is the table's OPENING occurrence (FROM 1) - initial state, so its group VALUE "GV"
      *> applies (13.18.63.4 GR5). Occurrence 2 is created by the MOVE, occurrence 3 by SET ... UP BY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1267-DYN-INITIALIZED-SEED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 R OCCURS DYNAMIC CAPACITY IN RCAP FROM 1 INITIALIZED.
             10 RX PIC X.
             10 RE1 PIC ZZ9.
             10 RE2 PIC $Z9.99.
             10 RE3 PIC 999-.
             10 RDT PIC 99/99.
             10 RG VALUE "GV".
                15 RG1 PIC X.
                15 RG2 PIC X.
             10 RV PIC 9(2) VALUE 42.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "O1=[" RG (1) "][" RV (1) "]".
           MOVE "A" TO RX (2).
           DISPLAY "C2=[" RX (2) "][" RE1 (2) "][" RE2 (2) "][" RE3 (2)
                   "][" RDT (2) "][" RG (2) "][" RV (2) "]".
           SET RCAP UP BY 1.
           DISPLAY "C3=[" RX (3) "][" RE1 (3) "][" RE2 (3) "][" RE3 (3)
                   "][" RDT (3) "][" RG (3) "][" RV (3) "]".
           STOP RUN.
