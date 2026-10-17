*>--- conformance-case
*> id: pb59-baseconvert-comp-argument
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.12.3 r1: "Argument-1 ... shall be a usage display or national
      *> data item or literal" - a COMP item's storage is a binary word, not a
      *> digit string. Before the PB59 screen this compiled and read the item's
      *> DISPLAY digits as base-16 ("0255" -> 597), a silent wrong-usage read.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGBC1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9(4) COMP VALUE 255.
       01 WS-R PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT(WS-C 16 10) TO WS-R
           STOP RUN.
