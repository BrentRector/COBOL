      *> reject-at: 2002 2014 2023
      *> kb/Work PB1505 - ISO/IEC 1989:2023 section 11.3.3 SR5: "Object-class-name-2 shall not be the name of a class
      *> defined with the FINAL clause."  A syntax rule of every CLASS-ID paragraph, the parameterized (USING) form
      *> included, whether or not anything expands it.
      *>   cite.py: OK  11.3.3 5)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505F FINAL.
       END CLASS PB1505F.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505Q INHERITS FROM PB1505F USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1505F
           CLASS T.
       END CLASS PB1505Q.
