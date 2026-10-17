*>--- conformance-case
*> id: pb873_by_value_image_carried_formal
*> rules: [GR-14.2.3-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB873 - the GR9/GR10 COMPUTE arm of the same crossing: a
      *> NESTED activation, whose formal description the activating
      *> element knows, into an IMAGE-CARRIED (redefined) numeric formal.
      *> ISO 14.2.3 GR10: "The allocated record is a data item of the same
      *> description as the formal parameter" and the argument is its
      *> sending operand in "a COMPUTE statement without the ROUNDED
      *> phrase"; GR9's NESTED branch performs the same COMPUTE for BY
      *> CONTENT. The record's description is S9(4)V99 - SIGNED - so the
      *> image the callee sees carries the operational sign.
      *> Sign characters: the IBM trailing over-punch (DOC-A.1-177):
      *> -4 = "M", -0 = "}".
      *> Derived:
      *>   BY VALUE SM (-12.34)       -> 00123M, negative
      *>   BY VALUE -7                -> -7.00  = 00070}, negative
      *>   BY VALUE SX (-123.4)       -> -123.40 = 01234}, negative
      *>   BY CONTENT -45.5           -> -45.50 = 00455}, negative
      *>   BY CONTENT SM              -> 00123M; the callee's SUBTRACT
      *>     reaches only the allocated record, so SM stays -12.34.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB873MAIN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SM  PIC S9(4)V99 VALUE -12.34.
       01 SX  PIC S9(6)V9 VALUE -123.4.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB873VAL02" AS NESTED USING BY VALUE SM
           CALL "PB873VAL02" AS NESTED USING BY VALUE -7
           CALL "PB873VAL02" AS NESTED USING BY VALUE SX
           CALL "PB873CNT02" AS NESTED USING BY CONTENT -45.5
           CALL "PB873CNT02" AS NESTED USING BY CONTENT SM
           DISPLAY "SM-AFTER " SM
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB873VAL02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV  PIC S9(4)V99.
       01 LVX REDEFINES LV PIC X(6).
       PROCEDURE DIVISION USING BY VALUE LV.
       VAL-PARA.
           IF LV < 0
               DISPLAY "VALUE [" LVX "] NEGATIVE"
           ELSE
               DISPLAY "VALUE [" LVX "] NOT-NEGATIVE"
           END-IF
           GOBACK.
       END PROGRAM PB873VAL02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB873CNT02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN  PIC S9(4)V99.
       01 LNX REDEFINES LN PIC X(6).
       PROCEDURE DIVISION USING LN.
       CNT-PARA.
           IF LN < 0
               DISPLAY "CONTENT [" LNX "] NEGATIVE"
           ELSE
               DISPLAY "CONTENT [" LNX "] NOT-NEGATIVE"
           END-IF
           SUBTRACT 1 FROM LN
           GOBACK.
       END PROGRAM PB873CNT02.
       END PROGRAM PB873MAIN02.
