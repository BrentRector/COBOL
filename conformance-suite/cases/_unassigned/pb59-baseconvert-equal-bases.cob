*>--- conformance-case
*> id: pb59-baseconvert-equal-bases
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.12.3 r1: argument-2 and argument-3 shall be "... with UNEQUAL
      *> values in the range 2 to 16". Two equal LITERAL bases are a violation
      *> visible at compile time (4.2.2 para 3); equal DATA-ITEM bases are the
      *> runtime EC-ARGUMENT-FUNCTION twin. Before the PB59 screen
      *> BASECONVERT("FF" 16 16) silently returned "FF".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGBC2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("FF" 16 16) TO WS-R
           STOP RUN.
