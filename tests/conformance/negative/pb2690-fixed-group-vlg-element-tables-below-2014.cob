      *> reject-at: 85 2002
      *> kb/Work PB2690 - a fixed-length group meets a dynamic-capacity table
      *> of variable-length elements across a CALL (ISO 14.8.2.2 2) with
      *> 8.5.1.12.1 and 8.5.1.12.3), but the OCCURS DYNAMIC CAPACITY clause is
      *> a COBOL-2014 addition (ISO 8.5.1.9), so below 2014 the formal's
      *> description is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FA.
          05 FH PIC X VALUE "h".
          05 TF OCCURS 2.
             10 DF PIC X.
             10 FI PIC X OCCURS 2.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB2690SR" AS NESTED USING BY REFERENCE FA
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LH PIC X.
          05 TL OCCURS DYNAMIC CAPACITY IN CL FROM 1.
             10 DL PIC X.
             10 IL PIC X OCCURS DYNAMIC CAPACITY IN CIL FROM 1.
       PROCEDURE DIVISION USING L.
           GOBACK.
       END PROGRAM PB2690SR.
       END PROGRAM PB2690NEG.
