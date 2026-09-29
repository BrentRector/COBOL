      *> reject-at: 2002 2014 2023
      *> kb/Work PB1394 - ISO 8.3.3.4.3 SR3: "Hexadecimal-digit-1 shall
      *>  be a hexadecimal digit". BX"G1" is a hexadecimal-boolean
      *>  literal with a non-hexadecimal digit; beside the item BX it
      *>  used to compile as BX followed by "G1". COBOLNET2630.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGW73ABX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BX PIC X(3) VALUE "ZZZ".
       PROCEDURE DIVISION.
           DISPLAY BX"G1".
           STOP RUN.
