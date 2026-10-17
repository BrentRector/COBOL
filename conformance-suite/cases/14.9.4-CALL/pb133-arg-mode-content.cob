*>--- conformance-case
*> id: pb133-arg-mode-content
*> rules: [SR-14.9.4.3-19]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR19: with BY CONTENT (or BY REFERENCE) specified or implied for an argument,
      *> BY REFERENCE shall be specified or implied for the corresponding formal parameter - here the
      *> formal is BY VALUE (kb/Work PB133 wave C2; a keyword-less argument DERIVES its mode from the
      *> formal per GR9 and can never disagree - only a written phrase can).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB133MC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING BY CONTENT W-A
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L-A.
       P.
           GOBACK.
       END PROGRAM S1.
       END PROGRAM PB133MC.
