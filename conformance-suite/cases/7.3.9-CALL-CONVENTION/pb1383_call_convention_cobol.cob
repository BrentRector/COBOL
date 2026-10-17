*>--- conformance-case
*> id: pb1383_call_convention_cobol
*> rules: [GR-7.3.9.3-2, GR-8.3.2.2-L5.2, DOC-A.1-68]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1383 -- ISO 7.3.9.3 GR2 a): "When COBOL is specified,
      *> that program-name or method-name is treated as a COBOL word that
      *> maps to the externalized name" by the mapping used for a name with
      *> no AS phrase -- WiseOwl COBOL's is case-insensitive
      *> (docs/CONFORMANCE.md DOC-A.1-68). COBOL is the one convention
      *> WiseOwl COBOL defines, so >>CALL-CONVENTION COBOL is accepted and
      *> CALL "p1383s" reaches PROGRAM-ID P1383S: IN SUB, then BACK.
       >>CALL-CONVENTION COBOL
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1383M.
       PROCEDURE DIVISION.
           CALL "p1383s"
           DISPLAY "BACK"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1383S.
       PROCEDURE DIVISION.
           DISPLAY "IN SUB"
           GOBACK.
       END PROGRAM P1383S.
       END PROGRAM P1383M.
