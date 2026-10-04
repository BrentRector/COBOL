      *> kb/Work PB1165 -- an ACTIVE-CLASS returning item of an INHERITED
      *> method, invoked through the SUBCLASS, delivers into a receiver
      *> typed as that subclass.
      *> ISO 14.8.3.3 rule 2): with an ACTIVE-CLASS returning item the
      *> conformance rules are those of a SET in the ACTIVATING element
      *> whose sending operand is described by the INVOCATION:
      *>   b) 1. "If the activated method is invoked with an
      *>         object-class-name, the sending object reference is
      *>         described with that same object-class-name and an ONLY
      *>         phrase."
      *>   b) 4. "If the activated method is invoked with any other object
      *>         reference, the sending operand has the same description
      *>         as that object reference."
      *>   and "the presence or absence of the FACTORY phrase is the same
      *>   as in the returning item of the activated element" (absent).
      *> ISO 14.9.39.3 SR12 a) then admits each sender:
      *>   1  INVOKE P1165SB "MK" RETURNING OS -- b) 1.: sender P1165SB
      *>      ONLY into OS (P1165SB, no ONLY): a) 2., the same class.
      *>   2  INVOKE P1165SB "MK" RETURNING OO -- b) 1.: sender P1165SB
      *>      ONLY into OO (P1165SB ONLY): a) 1., the same class, both ONLY.
      *>   3  INVOKE OS "MA" RETURNING OS2 -- b) 4.: sender described
      *>      P1165SB (OS's own description) into OS2 (P1165SB): a) 2.
      *> The declaring class is P1165BA, so reading the ACTIVE-CLASS sender
      *> as "the class containing it" (P1165BA) would refuse all three
      *> under SR12 b) 2. -- the refusal this note recorded.
      *> DERIVATION of the output: MK is a FACTORY method; invoked on
      *> P1165SB, its SELF is P1165SB's factory object, so INVOKE SELF
      *> "NEW" creates and returns a P1165SB object (16.2.1.2 GR1) and
      *> lines 1 and 2 hold one; MA returns SELF, the P1165SB object OS
      *> holds. WHO is overridden in P1165SB, so each line prints DERIVED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1165AC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1165SB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OS  USAGE OBJECT REFERENCE P1165SB.
       01 OO  USAGE OBJECT REFERENCE P1165SB ONLY.
       01 OS2 USAGE OBJECT REFERENCE P1165SB.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE P1165SB "MK" RETURNING OS.
           DISPLAY "1=" WITH NO ADVANCING.
           INVOKE OS "WHO".
           INVOKE P1165SB "MK" RETURNING OO.
           DISPLAY "2=" WITH NO ADVANCING.
           INVOKE OO "WHO".
           INVOKE OS "MA" RETURNING OS2.
           DISPLAY "3=" WITH NO ADVANCING.
           INVOKE OS2 "WHO".
           STOP RUN.
       END PROGRAM PB1165AC.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1165BA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LA.
       MAIN.
           INVOKE SELF "NEW" RETURNING LA.
       END METHOD MK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA2 USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LA2.
       MAIN.
           SET LA2 TO SELF.
       END METHOD MA.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BASE".
       END METHOD WHO.
       END OBJECT.
       END CLASS P1165BA.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1165SB INHERITS FROM P1165BA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1165BA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DERIVED".
       END METHOD WHO.
       END OBJECT.
       END CLASS P1165SB.
