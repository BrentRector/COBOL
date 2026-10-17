*>--- conformance-case
*> id: pb59-baseconvert-signed-sub11
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.12.3 r1: "if the base specified in argument-2 is less than 11,
      *> [argument-1] shall also be an unsigned integer data item or literal".
      *> A SIGNED numeric item is not an unsigned integer data item. An
      *> alphanumeric STRING under a sub-11 base is deliberately ADMITTED (the
      *> phrase is readable as kind or content, the corpus and GnuCOBOL accept
      *> string arguments at every base, and the runtime r2 digit screen owns
      *> the content) - the SIGNED shape is unambiguous either way.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGBC5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S PIC S9(3) VALUE -5.
       01 WS-R PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT(WS-S 8 10) TO WS-R
           STOP RUN.
