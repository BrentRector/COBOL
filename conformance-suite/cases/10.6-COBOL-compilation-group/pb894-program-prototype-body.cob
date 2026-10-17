*>--- conformance-case
*> id: pb894-program-prototype-body
*> rules: [SR-10.6.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 10.6.2 SR4 e) "The data division may contain only a linkage
      *> section." and f) "The procedure division shall contain only a
      *> procedure division header." -- a program prototype (11.10.2
      *> Format 2) describes a signature; storage and statements have no
      *> meaning there. kb/Work PB894: the ONE prototype-body screen shared
      *> by program, function and method prototypes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNB IS PROTOTYPE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       LINKAGE SECTION.
       01 L PIC 9.
       PROCEDURE DIVISION USING L.
       P-MAIN.
           MOVE 1 TO L.
       END PROGRAM PGNB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNBM.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PGNBM.
