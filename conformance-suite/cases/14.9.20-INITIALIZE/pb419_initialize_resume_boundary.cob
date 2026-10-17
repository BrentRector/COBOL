*>--- conformance-case
*> id: pb419_initialize_resume_boundary
*> rules: [GR-14.9.20.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB419 - the PER-IMPLICIT-STATEMENT RESUMPTION BOUNDARY of a multi-operand INITIALIZE.
      *>
      *> ISO 14.9.20.4 GR3, both sentences: "If more than one identifier-1 is specified in an INITIALIZE
      *> statement, the result of executing this INITIALIZE statement is the same as if a separate INITIALIZE
      *> statement had been written for each identifier-1 in the same order as specified in the INITIALIZE
      *> statement.  If an implicit INITIALIZE statement results in the execution of a declarative procedure
      *> that executes a RESUME statement with the NEXT STATEMENT phrase, processing resumes at the next
      *> implicit INITIALIZE statement, if any."
      *>
      *> The second sentence is operative only because 14.9.33.4 GR2 a) leaves room for it - "the implicit
      *> CONTINUE statement immediately follows the end of the statement that was executing when control was
      *> transferred to the exception processing procedure UNLESS GENERAL RULES ASSOCIATED WITH THE APPLICABLE
      *> STATEMENT SPECIFY OTHERWISE" - and GR3 is exactly such a general rule.
      *>
      *> WHY 2002 IS THE INTRODUCING EDITION.  GR3's first sentence is COBOL-85 text, but the second one can
      *> only be exercised by a RESUME statement (14.9.33) reached through a USE AFTER EXCEPTION CONDITION
      *> declarative under >>TURN (7.3.25) - the whole exception-condition mechanism, which arrives in
      *> ISO/IEC 1989:2002.  negative/pb419-initialize-resume-below-2002.cob is the same source one edition
      *> lower, where the construct does not exist.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN.  BADX is 9 and E OCCURS 3 TIMES, so every
      *> reference to EN (BADX) is a subscript outside 1..3: 8.4.2.3.4 GR2 sets EC-BOUND-SUBSCRIPT, Table 13
      *> makes it Fatal, the declarative runs (it counts itself in D) and RESUME AT NEXT STATEMENT ends it.
      *> 14.9.20.4 GR6c gives every un-REPLACED numeric receiver the figurative constant ZERO, so an
      *> identifier-1 that IS initialized ends at 000.
      *>
      *>   A=01 000 000  the raise is in the FIRST implicit INITIALIZE; GR3 resumes at the next implicit
      *>                 INITIALIZE, so A1 AND A2 are both still initialized.  (Before PB419 the resumption
      *>                 point was the end of the whole statement and this line read "01 222 333".)
      *>   B=01 000 000  the raise is in the MIDDLE implicit INITIALIZE: A1 ran before it, A2 is the "next
      *>                 implicit INITIALIZE" GR3 resumes at.
      *>   C=01 000      the raise is in the LAST implicit INITIALIZE, so GR3's "if any" is not satisfied and
      *>                 14.9.33.4 GR2 a)'s implicit CONTINUE after the statement is what remains; A1 ran
      *>                 first and is initialized.
      *>   D=01 777      the one-operand control: GR3's "more than one" premise is false, so the statement is
      *>                 its own single implicit INITIALIZE and 14.9.33.4 GR2 a)'s implicit CONTINUE after it
      *>                 is the resumption point.  A1 is not an operand of it and keeps the 777 a MOVE put
      *>                 there, which is what proves the run reached this DISPLAY at all.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB419INIRES02.
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
           MOVE 0 TO D.
           MOVE 444 TO A1.
           MOVE 555 TO A2.
           INITIALIZE A1 EN (BADX) A2.
           DISPLAY "B=" D " " A1 " " A2.
           MOVE 0 TO D.
           MOVE 666 TO A1.
           INITIALIZE A1 EN (BADX).
           DISPLAY "C=" D " " A1.
           MOVE 0 TO D.
           MOVE 777 TO A1.
           INITIALIZE EN (BADX).
           DISPLAY "D=" D " " A1.
           STOP RUN.
