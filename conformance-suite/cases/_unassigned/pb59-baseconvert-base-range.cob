*>--- conformance-case
*> id: pb59-baseconvert-base-range
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.12.3 r1: the bases shall be "in the range 2 to 16". A LITERAL
      *> base of 20 is a violation the compiler can detect, and 4.2.2 para 3
      *> obliges the flag at COMPILE time - before the PB59 screen the program
      *> compiled and the runtime EC was silently swallowed under checking-off
      *> (an empty result stored).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGBC3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("11" 20 10) TO WS-R
           STOP RUN.
