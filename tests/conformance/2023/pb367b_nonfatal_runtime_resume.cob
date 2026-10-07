      *> kb/Work PB367b - RESUME OUT OF A DECLARATIVE ENTERED FROM A RUNTIME RAISE SITE.
      *> The companion of conformance:2023/pb367b_nonfatal_runtime_dispatch: there the declaratives
      *> complete normally and the interrupted statement finishes; here they RESUME, which is an
      *> explicit transfer of control OUT of that statement.
      *>
      *>  . 14.9.33.4 GR2 a) 1. - RESUME AT NEXT STATEMENT transfers control to "an implicit CONTINUE
      *>    statement [that] immediately follows the end of the statement that was executing when
      *>    control was transferred to the exception processing procedure", and the applicable
      *>    statement "is the one in which the exception condition was raised"; GR2 a) 3. adds that
      *>    for a contained statement it is "the lowest level statement, not the containing statement".
      *>    The first case below is that rule's OWN NOTE 1: "IF a GO TO x ELSE GO TO y END-IF. If an
      *>    exception condition was raised during the evaluation of 'a', transfer would be after the
      *>    END-IF even though control normally would never be passed there." The inverted THROUGH
      *>    range (14.7.8 rule 2) is raised during the evaluation of the IF's condition, so NEITHER
      *>    branch runs and control lands after the END-IF => RI-DECL then AFTER-IF, with no
      *>    RI-THEN / RI-ELSE line. (That is the whole point of the NOTE: without the RESUME the
      *>    range is empty and RI-ELSE would run - which is what pb367b_nonfatal_runtime_dispatch
      *>    pins.)
      *>  . 14.9.33.4 GR3 - "If procedure-name-1 is specified, control is transferred to
      *>    procedure-name-1 as if a GO TO procedure-name-1 were executed." The second case raises
      *>    EC-BOUND-OVERFLOW (8.5.1.9.6 GR1) inside a MOVE whose receiving subscript grows a
      *>    dynamic-capacity table past its expected capacity; the declarative resumes at
      *>    RESUMED-P, so BO-NOT-REACHED never displays => BO-DECL then RESUMED-AT.
      *>  . 14.9.49.3 SR3 - a declarative may reference a nondeclarative procedure "only in a RESUME
      *>    statement", which is why RESUMED-P may be named here and nowhere else in these sections.
      *>  . Both declaratives are selected by 14.9.49.4 GR3 e) and neither completes normally
      *>    (14.6.13.1.2 #1 names RESUME), so 14.6.13.1.4 #3's "completes normally" arm does not
      *>    apply and the transfer stands.
      >>TURN EC-BOUND-OVERFLOW CHECKING ON
      >>TURN EC-RANGE-INVALID CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB367BNFR.
      *> INV-RANGE is ordered IN a LOCALE alphabet: §13.18.63.3 SR26 refuses an inverted range whose runtime
      *> collating sequence is known (COBOLNET2961, kb/Work PB552), and a locale's is not.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 4.
       01 WS-C  PIC X VALUE "M".
          88 INV-RANGE VALUE "Z" THRU "A" IN LOC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-RI SECTION.
           USE AFTER EC EC-RANGE-INVALID.
       D-RI-P.
           DISPLAY "RI-DECL".
           RESUME AT NEXT STATEMENT.
       D-BO SECTION.
           USE AFTER EC EC-BOUND-OVERFLOW.
       D-BO-P.
           DISPLAY "BO-DECL".
           RESUME AT RESUMED-P.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           IF INV-RANGE DISPLAY "RI-THEN" ELSE DISPLAY "RI-ELSE" END-IF.
           DISPLAY "AFTER-IF".
           MOVE 22 TO WS-E (5).
           DISPLAY "BO-NOT-REACHED".
       RESUMED-P.
           DISPLAY "RESUMED-AT".
           STOP RUN.
