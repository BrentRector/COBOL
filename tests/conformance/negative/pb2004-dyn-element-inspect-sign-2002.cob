      *> reject-at: 85 2002
      *> kb/Work PB2004 - the edition floor under conformance:2014/pb2004_dyn_element_character_channels.
      *> A dynamic-capacity table (8.5.1.9; OCCURS Format 4, 13.18.38) is a COBOL-2014 introduction, so at
      *> COBOL-85 and COBOL-2002 the signed element the positive case INSPECTs (14.9.22.4 GR4 d) cannot be
      *> declared at all; COBOLNET0900 is the version gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2004N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G2.
          05 DN PIC S9(3) SIGN TRAILING SEPARATE
             OCCURS DYNAMIC CAPACITY IN CN FROM 1.
       PROCEDURE DIVISION.
           MOVE -5 TO DN (1)
           INSPECT DN (1) REPLACING ALL "5" BY "0"
           DISPLAY DN (1)
           STOP RUN.
