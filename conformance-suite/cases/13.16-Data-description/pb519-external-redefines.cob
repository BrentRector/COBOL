*>--- conformance-case
*> id: pb519-external-redefines
*> rules: [SR-13.16.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.3 SR5: "The EXTERNAL clause shall not be specified in the same data description entry as
      *> the REDEFINES or BASED clause." Before the screen this compiled clean and DISPLAY A printed four
      *> SPACES: the external re-basing folded B's whole redefines class - A included - onto the run-unit
      *> cell and discarded A's VALUE (kb/Work PB519). The BASED sibling was already refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB519EXR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X(4) VALUE "ABCD".
       01  B REDEFINES A IS EXTERNAL PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY A
           STOP RUN.
