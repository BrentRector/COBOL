*>--- conformance-case
*> id: pb132-call-returning-screen-name
*> rules: [SR-14.9.4.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR7: identifier-3 shall reference a data item defined in the file, working-storage,
      *> local-storage, or linkage section. A SCREEN SECTION entry is none of these - and the old resolve
      *> failure staged the answer to RUN time, the PB88 wrong-stage shape (kb/Work PB132; R32 posture:
      *> "declared in an unsupported section" is not "undefined").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9(4).
       SCREEN SECTION.
       01 SCR-A.
          02 LINE 1 COLUMN 1 VALUE "HI".
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" RETURNING SCR-A
           STOP RUN.
