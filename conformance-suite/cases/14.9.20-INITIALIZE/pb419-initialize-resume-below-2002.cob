*>--- conformance-case
*> id: pb419-initialize-resume-below-2002
*> rules: [GR-14.9.20.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB419 - the edition floor of the per-implicit-statement resumption boundary.
      *>
      *> ISO 14.9.20.4 GR3's SECOND sentence ("If an implicit INITIALIZE statement results in the execution of
      *> a declarative procedure that executes a RESUME statement with the NEXT STATEMENT phrase, processing
      *> resumes at the next implicit INITIALIZE statement, if any") can only be reached through the
      *> exception-condition mechanism: >>TURN (7.3.25), USE AFTER EXCEPTION CONDITION (14.9.49 Format 3) and
      *> the RESUME statement (14.9.33).  All three are ISO/IEC 1989:2002 introductions, so at COBOL-85 this
      *> source does not describe a program at all and the compiler must say so rather than silently ignoring
      *> the directive and running the INITIALIZE unchecked.  The FIRST sentence of GR3 (several identifier-1
      *> behave as separate INITIALIZE statements in source order) IS 85 text and keeps working there - it is
      *> the RESUMPTION POINT, and only that, which has no COBOL-85 arm.
      *>
      *> The positive twin is 2002/pb419_initialize_resume_boundary.cob.  The rejection is reported at the
      *> FIRST construct the edition does not have, the >>TURN directive on line 1 of the program text.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB419INIRES85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 TIMES.
             10 EN PIC 9(3) VALUE 111.
       01 A1   PIC 9(3) VALUE 222.
       01 A2   PIC 9(3) VALUE 333.
       01 BADX PIC 9(2) VALUE 9.
       01 D    PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HBOUND SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       HBOUND-P.
           ADD 1 TO D.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INITIALIZE EN (BADX) A1 A2.
           DISPLAY "A=" D " " A1 " " A2.
           STOP RUN.
