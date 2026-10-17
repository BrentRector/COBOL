*>--- conformance-case
*> id: pb59-baseconvert-noninteger-base
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.12.3 r1: the bases shall be "positive nonzero numeric INTEGER
      *> literals or data items". A literal 2.5 is not an integer - before the
      *> PB59 screen the render channel TRUNCATED it to 2 silently and
      *> BASECONVERT("11" 2.5 10) answered 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGBC4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("11" 2.5 10) TO WS-R
           STOP RUN.
