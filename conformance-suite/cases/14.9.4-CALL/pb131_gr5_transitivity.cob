      *>--- conformance-case
      *> id: pb131_gr5_transitivity
      *> rules: [GR-14.9.4.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB131 - ISO 14.9.4.4 GR5 (FORMAT 1): BY CONTENT and BY REFERENCE are TRANSITIVE across
      *> the parameters that follow them until another BY CONTENT or BY REFERENCE phrase; BY REFERENCE is
      *> assumed before the first phrase. Bare B rides the preceding BY CONTENT (callee's store invisible),
      *> bare D rides the preceding BY REFERENCE (store visible). Expected values derived from 14.2.3
      *> GR8/GR9: reference formals alias the argument, content formals are detached copies.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB131GR5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 1.
       01 B PIC 9(4) VALUE 2.
       01 C PIC 9(4) VALUE 3.
       01 D PIC 9(4) VALUE 4.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB131GR5S" USING BY CONTENT A B BY REFERENCE C D
           DISPLAY "A=" A " B=" B " C=" C " D=" D
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB131GR5S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC 9(4).
       01 LB PIC 9(4).
       01 LC PIC 9(4).
       01 LD PIC 9(4).
       PROCEDURE DIVISION USING LA LB LC LD.
       P.
           ADD 10 TO LA.
           ADD 10 TO LB.
           ADD 10 TO LC.
           ADD 10 TO LD.
           EXIT PROGRAM.
       END PROGRAM PB131GR5S.
       END PROGRAM PB131GR5.
