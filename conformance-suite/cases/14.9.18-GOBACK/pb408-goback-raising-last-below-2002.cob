*>--- conformance-case
*> id: pb408-goback-raising-last-below-2002
*> rules: [GR-14.9.18.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB408. GOBACK (ISO 14.9.18) and its RAISING phrase are COBOL-2002
      *> introductions, so the whole of 14.9.18.4 GR1 b) - including GR1b3a's
      *> EC-RAISING-NOT-SPECIFIED substitution, whose LAST phrase this statement writes -
      *> has no COBOL-85 spelling at all. The four-compilers rule: the activator-side
      *> enablement behaviour the 2002 positive golden pins shall be unreachable below the
      *> edition that introduced the statement, and the compiler shall say so rather than
      *> accept it.
      *> POSITIVE CONTROL: 2002/pb408_raising_enablement_locus.cob.
      *> ⛔ THE STATEMENT SITS IN A DECLARATIVE PROCEDURE ON PURPOSE (kb/Work PB410): 14.9.18.3 SR5 admits
      *> the LAST phrase "only in a declarative procedure or WHEN phrase of a PERFORM statement", and this
      *> case used to write it in an ordinary paragraph - so from 2002 upward it broke TWO rules and the
      *> edition gate it exists to pin was no longer the only reason it was refused. The USE statement is
      *> 1985-legal, so nothing here but the GOBACK and its RAISING phrase is post-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB408.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "negpb408.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       D-PARA.
           GOBACK RAISING LAST EXCEPTION.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           STOP RUN.
