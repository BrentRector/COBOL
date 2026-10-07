      *> ISO 15.65.4 r5 - "If the ACTIVATING keyword is specified and the function
      *> is in a COBOL main program, then the returned value shall be a single
      *> space... If the function is not specified in a main program, then the
      *> returned value is the name of the runtime element that activated the
      *> currently running runtime element. This may be by a CALL statement, AN
      *> INVOKE STATEMENT, a function reference, or an inline invocation."
      *>
      *> FOUR activation mechanisms are named. INVOKE was not among the implemented
      *> ones (fix-queue PB36), and a method is not a registry node, so inside a
      *> method NO frame existed at all. The consequences were three wrong answers:
      *>   CURRENT     returned the CALLER's name, not the running element (r7)
      *>   ACTIVATING  returned the SINGLE SPACE r5 reserves for a MAIN PROGRAM -
      *>               the method claimed to be one
      *>   STACK       omitted the method entirely (r9)
      *>
      *> THE FORMER JUSTIFICATION CITED REAL RULES THAT DO NOT GOVERN: r3 is about
      *> elements that are NOT COBOL runtime elements, and r4 is about the FORM of
      *> the name - it even lists "method-id" among the forms an implementor may
      *> return, which presumes the element is on the stack. Latitude over which
      *> name string, never over whether the frame exists.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB36MNFN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       F.
           DISPLAY "F-CUR=[" FUNCTION MODULE-NAME(CURRENT) "]".
           DISPLAY "F-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "]".
           MOVE 1 TO L-R.
           GOBACK.
       END FUNCTION PB36MNFN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB36MODNAME.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB36MN.
           FUNCTION PB36MNFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CPB36MN.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> The MAIN program: r5's single space, r7 CURRENT, r10 TOP-LEVEL.
           DISPLAY "P-CUR=[" FUNCTION MODULE-NAME(CURRENT) "]".
           DISPLAY "P-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "]".
           DISPLAY "P-STACK=[" FUNCTION MODULE-NAME(STACK) "]".
      *> INVOKE - the mechanism that was missing.
           INVOKE CPB36MN "NEW" RETURNING O.
           INVOKE O "SHOW".
      *> A FUNCTION REFERENCE - r5's third mechanism, already correct; kept as the
      *> regression that proves the fix did not disturb it.
           COMPUTE R = FUNCTION PB36MNFN(1).
      *> THE FRAME MUST POP. A leaked frame makes every later MODULE-NAME read one
      *> element too deep, which is why the emitted push sits in a try/finally.
           DISPLAY "AFTER-CUR=[" FUNCTION MODULE-NAME(CURRENT) "]".
           DISPLAY "AFTER-STACK=[" FUNCTION MODULE-NAME(STACK) "]".
           CALL "PB36MNSUB".
           CALL "PB36MNREC".
           STOP RUN.
       END PROGRAM PB36MODNAME.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB36MNSUB.
       PROCEDURE DIVISION.
       S.
      *> A CALL - r5's first mechanism, already correct.
           DISPLAY "C-CUR=[" FUNCTION MODULE-NAME(CURRENT) "]".
           DISPLAY "C-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "]".
           DISPLAY "C-STACK=[" FUNCTION MODULE-NAME(STACK) "]".
           GOBACK.
       END PROGRAM PB36MNSUB.

      *> ⛔ THE RECURSION WITNESS, AND THE REASON THE COLLAPSE HAD TO GO. Three
      *> nested activations of ONE RECURSIVE program are three runtime elements.
      *> The old builder collapsed consecutive frames sharing a compilation unit
      *> and reported `PB36MNREC;PB36MODNAME; ` - one entry for three activations -
      *> while ACTIVATING in the same frame correctly answered PB36MNREC. STACK and
      *> ACTIVATING contradicted each other on plain COBOL with no OO in sight, and
      *> a stack list that silently drops recursion depth is worse than a visible
      *> repeat: the repeat can be read, the missing frames cannot.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB36MNREC RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       R.
           ADD 1 TO D.
           IF D < 3
               CALL "PB36MNREC"
           ELSE
               DISPLAY "R-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "]"
               DISPLAY "R-STACK=[" FUNCTION MODULE-NAME(STACK) "]"
           END-IF.
           GOBACK.
       END PROGRAM PB36MNREC.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB36MN INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       M.
      *> r7 CURRENT = the outermost element of the compilation unit running, i.e.
      *> the CLASS. r5 ACTIVATING = the invoking program. r10 TOP-LEVEL is
      *> unaffected by the method frame. r9 STACK now carries the method's unit.
           DISPLAY "I-CUR=[" FUNCTION MODULE-NAME(CURRENT) "]".
           DISPLAY "I-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "]".
           DISPLAY "I-TOP=[" FUNCTION MODULE-NAME(TOP-LEVEL) "]".
           DISPLAY "I-STACK=[" FUNCTION MODULE-NAME(STACK) "]".
           INVOKE SELF "DEEPER".
       END METHOD SHOW.
       METHOD-ID. DEEPER.
       PROCEDURE DIVISION.
       M.
      *> A METHOD ACTIVATED BY A METHOD. ACTIVATING names the invoking METHOD;
      *> r4 permits the method-id form, and it is the more informative choice.
           DISPLAY "D-ACT=[" FUNCTION MODULE-NAME(ACTIVATING) "]".
      *> r9 builds STACK from the OTHER keywords: entry 1 is CURRENT, every entry
      *> after it is "the names of the runtime elements that would have been
      *> returned if the ACTIVATING keyword were specified within the previous
      *> module in the list", the penultimate is TOP-LEVEL and the last is a single
      *> space. So the second entry must be SHOW - the same answer D-ACT gives one
      *> line above. It used to be the CLASS, because the builder collapsed
      *> consecutive frames sharing a compilation unit; owner decision 2026-08-05
      *> removed that collapse (see PB36MNREC above for why).
           DISPLAY "D-STACK=[" FUNCTION MODULE-NAME(STACK) "]".
           GOBACK.
       END METHOD DEEPER.
       END OBJECT.
       END CLASS CPB36MN.
