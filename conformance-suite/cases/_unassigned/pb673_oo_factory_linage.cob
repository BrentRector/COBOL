*>--- conformance-case
*> id: pb673_oo_factory_linage
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2002/2023 §13.18.34.4 GR6 - THE LINAGE OPERAND VALUES REACH A CLASS'S FILE TOO. The rule is
      *> written over "the indicated statement" and its file, with no exemption for the runtime element that
      *> executes it, and §9.3.14.2's factory object owns file connectors exactly as a program does
      *> (conformance:2002/oo_factory_file pins the connector itself).
      *>
      *> This is the third face of kb/Work PB673, and it is a TWO-ARM DISPATCH with one arm fixed: the LINAGE
      *> operand values used to be INSTALLED as a closure beside the connector's registration, and only the
      *> program registration arm (DispatchEmitter.__Activate) installed them - the class-ctor arm
      *> (OoEmitter.EmitFileMembers) installed the ASSIGN ... USING source next to it and never the LINAGE
      *> evaluator. So a factory's or object's LINAGE FD had NO page model at all: no counter, no end-of-page,
      *> LINAGE-COUNTER frozen. MEASURED at 1a3cc30d - this program printed NOEOP1 / NOEOP2. Moving the
      *> operands onto the statement removes the second arm rather than adding a second install.
      *>
      *> EXPECTED, derived from the rule (LINAGE IS 4 LINES WITH FOOTING AT 2):
      *>   §13.18.34.4 GR7 d) sets LINAGE-COUNTER to one at the OPEN OUTPUT; each WRITE AFTER ADVANCING 1 LINE
      *>   increments it by one (GR7 c) 2.), so the two writes leave it at 2 and 3.
      *>   EOP1  §14.9.51.4 GR26 b) - "the associated LINAGE-COUNTER is equal to or exceeds the current value of
      *>         the footing start and is less than the page size": 2 >= 2 and 2 < 4.
      *>   EOP2  the same: 3 >= 2 and 3 < 4.
      *>   (Neither write reaches the page size, so GR26 a) never applies and this golden does not depend on the
      *>   GR26 a)/b) boundary at counter = page size, which LinageConformanceTests pins.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB673OO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB673LN.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB673LN "PRINTIT".
           STOP RUN.
       END PROGRAM PB673OO.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB673LN.
       IDENTIFICATION DIVISION.
       FACTORY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FL ASSIGN TO "pb673oo.dat".
       DATA DIVISION.
       FILE SECTION.
       FD FL LINAGE IS 4 LINES WITH FOOTING AT 2.
       01 FL-REC PIC X(6).
       PROCEDURE DIVISION.
       METHOD-ID. PRINTIT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FL.
           MOVE "LINE-A" TO FL-REC.
           WRITE FL-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP1"
               NOT AT END-OF-PAGE DISPLAY "NOEOP1"
           END-WRITE.
           MOVE "LINE-B" TO FL-REC.
           WRITE FL-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP2"
               NOT AT END-OF-PAGE DISPLAY "NOEOP2"
           END-WRITE.
           CLOSE FL.
       END METHOD PRINTIT.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS PB673LN.
