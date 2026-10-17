*>--- conformance-case
*> id: pb973_method_formal_named_like_emitter_member
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB973 - a METHOD formal parameter may carry ANY user-
      *> defined word as its data-name. ISO 8.3.2.2: "Within a source
      *> element, a given user-defined word may be used as only one type
      *> of user-defined word" - N, V, INV and PROP are each used only as
      *> a data-name here, so every one is legal. They are also the
      *> spellings of the compiler's own generated members (__N the
      *> paragraph count, __V, __INV, __PROP): the formal's C# carrier
      *> once shared that namespace, and a formal named N crashed the
      *> backend (CS1628) on this legal source.
      *> Expected values, from the rules:
      *> 14.2.3 GR8 - "If the argument is passed by reference, the
      *> activated runtime element operates as if the formal parameter
      *> occupies the same storage area as the argument": SETN's ADD 1 TO N
      *> is W's, so W = 7 + 1 = 008 after the call; MIX's MOVE TO PROP is
      *> C's.
      *> TOTAL: SETN 7; ADDV +8 (W's value at the call) = 15; MIX + INV
      *> (2) + PROP (3) = 20, then MOVE TOTAL TO PROP makes C = 020.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB973BOX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TOTAL PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. SETN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(3).
       PROCEDURE DIVISION USING N.
           MOVE N TO TOTAL
           ADD 1 TO N.
       END METHOD SETN.
       METHOD-ID. ADDV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V PIC 9(3).
       PROCEDURE DIVISION USING V.
           ADD V TO TOTAL.
       END METHOD ADDV.
       METHOD-ID. MIX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 INV PIC 9(3).
       01 PROP PIC 9(3).
       PROCEDURE DIVISION USING INV PROP.
           ADD INV PROP TO TOTAL
           MOVE TOTAL TO PROP.
       END METHOD MIX.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "TOTAL=" TOTAL.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB973BOX.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB973MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB973BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE PB973BOX.
       01 W PIC 9(3) VALUE 7.
       01 A PIC 9(3) VALUE 2.
       01 C PIC 9(3) VALUE 3.
       PROCEDURE DIVISION.
           INVOKE PB973BOX "NEW" RETURNING B
           INVOKE B "SETN" USING W
           DISPLAY "W=" W
           INVOKE B "ADDV" USING W
           INVOKE B "MIX" USING A C
           DISPLAY "C=" C
           INVOKE B "SHOW"
           STOP RUN.
       END PROGRAM PB973MAIN.
