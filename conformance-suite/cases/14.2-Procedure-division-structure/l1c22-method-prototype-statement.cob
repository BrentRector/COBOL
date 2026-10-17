      *>--- conformance-case
      *> id: l1c22-method-prototype-statement
      *> rules: [SR-14.2.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.2.2 SR11 — a method prototype (a method in an
      *> interface) whose procedure division holds a statement.
      *> RULE (14.2.2 SR11): "A procedure division in a method prototype
      *> shall contain only a procedure division header."
      *> cite.py --check 14.2.2 "A procedure division in a method
      *>   prototype shall contain only a procedure division header"
      *>   -> OK  §14.2.2 11)  (Syntax rules)
      *> cite.py --check 14.1 "The procedure division in an interface
      *>   contains method prototypes" -> OK  §14.1   (General)
      *> Interface L1C22I's method SPEAK is a method prototype (14.1);
      *> its procedure division carries a paragraph and a DISPLAY
      *> statement after the header, so SR11 is violated. The same
      *> restriction is restated for every prototype kind at 10.6.2
      *> SR4 f) (cite.py --check 10.6.2 "The procedure division shall
      *> contain only a procedure division header" -> OK  §10.6.2
      *> 4) f)).
      *> Removing P-MAIN and its DISPLAY yields a valid program.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C22I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "MEOW".
       END METHOD SPEAK.
       END INTERFACE L1C22I.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22N.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM L1C22N.
