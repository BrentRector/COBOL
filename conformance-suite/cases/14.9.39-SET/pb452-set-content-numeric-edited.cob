*>--- conformance-case
*> id: pb452-set-content-numeric-edited
*> rules: [SR-14.9.39.3-31, GR-14.9.39.4-32, GR-14.9.39.4-36]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.39.3 SR31: "If FARTHEST-FROM-ZERO or NEAREST-TO-ZERO is specified, identifier-14
      *> shall reference a numeric data item." 8.5.2.12 lists what a NUMERIC data item is and 8.5.2.13 gives a
      *> numeric-EDITED item its own separate category, so an edited receiver is not admitted - and this is
      *> exactly where SET format 15 is NARROWER than the HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC functions Annex
      *> D.32 offers as its alternatives, whose 15.43.3 / 15.58.3 rule 1 admits "category numeric or
      *> numeric-edited". Reading the rules as interchangeable would silently accept this. COBOLNET1938
      *> (kb/Work PB452).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB452NUMERICEDITED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-EDITED PIC ZZ,ZZ9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET CONTENT OF WS-EDITED TO FARTHEST-FROM-ZERO
           STOP RUN.
