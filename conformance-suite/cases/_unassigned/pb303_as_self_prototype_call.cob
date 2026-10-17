*>--- conformance-case
*> id: pb303_as_self_prototype_call
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the SECOND arm of the two-name split, found by the self-review
       *> and fixed with this golden: BinderDriver.SelfPrototype built its ProgramPrototype
       *> from the declared name TWICE, so the prototype record carried the declared word
       *> where its EXTERNALIZED name belongs.
       *>
       *> ISO 8.4.6.8 admits a program-prototype-name with no specifier at all - "the
       *> program-name of a containing program definition" - and 12.3.8.3 SR15 makes a
       *> specifier that names its own definition ignored, so the definition supplies the
       *> details either way.  CallBinder then emits the activation by the prototype record
       *> EXTERNALIZED name, because 14.9.4.4 GR3 b) resolves it "as described in 8.3.2.2".
       *> With both fields set from the declared word this self-call emitted PB303SP while
       *> the run-unit registry held the program under PB303SPX, and the activation raised
       *> EC-PROGRAM-NOT-FOUND.  Only a unit that BOTH carries an AS phrase and calls
       *> itself by the word can show it - which is why it needs its own golden.
       *>
       *> RECURSIVE (11.10.4 GR4) is what makes the self-activation legal at all; without
       *> it 14.9.4.4 GR3 f) raises EC-PROGRAM-RECURSIVE-CALL.  WS-DEPTH is static data
       *> (13.5.4 GR1 - a recursive program WS is static, one copy for all activations),
       *> so the counter carries across the two activations and the guard terminates it.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303SP AS "PB303SPX" IS RECURSIVE PROGRAM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB303SP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-DEPTH PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO WS-DEPTH.
           DISPLAY "DEPTH=" WS-DEPTH.
           IF WS-DEPTH < 2
               CALL PB303SP
           END-IF.
           GOBACK.
       END PROGRAM PB303SP.
