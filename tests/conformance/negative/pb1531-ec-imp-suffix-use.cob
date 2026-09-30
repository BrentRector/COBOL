      *> reject-at: 2002 2014 2023
      *> kb/Work PB1531 - the USE AFTER EXCEPTION CONDITION form of the
      *> EC-IMP-suffix refusal. USE Format 3 (§14.9.49.2) takes an
      *> exception-name, the term §14.6.13.1.1 defines; an EC-IMP-suffix
      *> is the implementor's to define ("The implementor defines the
      *> action to be taken, the fatality, and when any of these
      *> exceptions are raised"), and this implementation defines none
      *> (docs/CONFORMANCE.md DOC-A.1-99), so the declarative names no
      *> exception-name and is refused with COBOLNET0711.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1531N3.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-X SECTION.
           USE AFTER EXCEPTION CONDITION EC-IMP-WIBBLE.
       D-X-P.
           DISPLAY "DECL".
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           DISPLAY "HI"
           STOP RUN.
