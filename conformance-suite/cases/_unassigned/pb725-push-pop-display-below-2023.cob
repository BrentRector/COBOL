*>--- conformance-case
*> id: pb725-push-pop-display-below-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> >>PUSH (ISO 7.3.22), >>POP (ISO 7.3.20) and >>DISPLAY (ISO 7.3.12) are COBOL-2023
      *> ADDITIONS - Annex E.3.3 item 38 ("The PUSH and POP directives are added to allow
      *> saving and restoration of the state of compiler directives"), Annex E.3.3 item 16
      *> ("The DISPLAY directive allows the display of compile-time information during the
      *> compilation of COBOL source") and Annex E.2 item 5, which lists DISPLAY, POP and
      *> PUSH among the compiler-directive words added in 2023 - so below 2023 each is the
      *> introduction diagnostic COBOLNET0900.
      *>
      *> Note the clause pairing: 7.3.20 is POP and 7.3.22 is PUSH, the reverse of the
      *> order the pre-PB725 code comment listed them in.
      *>
      *> This is the defect kb/Work PB725 was filed on: all three compiled CLEAN at every
      *> edition, while their siblings from the SAME Annex E.2 item 5 list
      *> (>>COBOL-WORDS, >>REF-MOD-ZERO-LENGTH) were correctly gated.
      *>
      *> 7.3.20.3 SR3 / 7.3.22.3 SR3 confine the ALL form to a compilation unit, between
      *> statements in the procedure division - which is where they are written here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB725N23.
       PROCEDURE DIVISION.
       MAIN.
       >>DISPLAY "PB725 COMPILE-TIME NOTE"
       >>PUSH ALL
           DISPLAY "A".
       >>POP ALL
           STOP RUN.
