      *> kb/Work PB845 and PB2501. ISO 13.16.3 SR4: "If no entry-name
      *> clause is specified, it is as though the filler format of the
      *> entry-name clause were specified"; 13.18.60.2 prints
      *> [ USAGE IS ] as optional. From COBOL-2002 8.9 reserves
      *> NATIONAL, BIT, BINARY-LONG, FLOAT-LONG and PROGRAM-POINTER, and
      *> 8.3.2.1 1) says "Reserved words shall not be used as
      *> user-defined words", so the word after each level number below
      *> can only be the USAGE clause of an unnamed item. Each unnamed
      *> group is compared with a control written as FILLER with the
      *> USAGE clause spelled out: the same description, so the same
      *> size and the same contents. Before the fix every entry was read
      *> as a NAME and refused with COBOLNET0901 (and the PICTURE-less
      *> ones with a false COBOLNET0881).
      *> EXPECTED, by the rule: G1 EQ, G2 EQ, G3 EQ, G4 EQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB845A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 NATIONAL PIC N(3) VALUE N"ABC".
       01 C1.
          05 FILLER PIC N(3) USAGE NATIONAL VALUE N"ABC".
       01 G2.
          05 BIT PIC 1(8) VALUE B"10100101".
          05 BIT PIC 1(8) VALUE B"00001111".
       01 C2.
          05 FILLER PIC 1(8) USAGE BIT VALUE B"10100101".
          05 FILLER PIC 1(8) USAGE BIT VALUE B"00001111".
       01 G3.
          05 NATIONAL PIC 9(3) VALUE 123.
       01 C3.
          05 FILLER PIC 9(3) USAGE NATIONAL VALUE 123.
       01 G4.
          05 BINARY-LONG VALUE 5.
          05 FLOAT-LONG.
       01 C4.
          05 FILLER USAGE BINARY-LONG VALUE 5.
          05 FILLER USAGE FLOAT-LONG.
       01 PROGRAM-POINTER.
       PROCEDURE DIVISION.
           IF FUNCTION BYTE-LENGTH(G1) = FUNCTION BYTE-LENGTH(C1)
              AND G1 = C1
               DISPLAY "G1 EQ"
           ELSE
               DISPLAY "G1 NE"
           END-IF
           IF FUNCTION BYTE-LENGTH(G2) = FUNCTION BYTE-LENGTH(C2)
              AND G2 = C2
               DISPLAY "G2 EQ"
           ELSE
               DISPLAY "G2 NE"
           END-IF
           IF FUNCTION BYTE-LENGTH(G3) = FUNCTION BYTE-LENGTH(C3)
              AND G3 = C3
               DISPLAY "G3 EQ"
           ELSE
               DISPLAY "G3 NE"
           END-IF
           IF FUNCTION BYTE-LENGTH(G4) = FUNCTION BYTE-LENGTH(C4)
               DISPLAY "G4 EQ"
           ELSE
               DISPLAY "G4 NE"
           END-IF
           STOP RUN.
