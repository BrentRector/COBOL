      *> reject-at: 2002 2014 2023
      *> kb/Work PB1505 - ISO/IEC 1989:2023 section 11.3.3 SR3: "Object-class-name-2 shall not be the name of the class
      *> declared by this class definition."  The rule is a syntax rule of the CLASS-ID paragraph, whose general format
      *> (11.3.2) includes the USING phrase, so it governs a parameterized class definition too, expanded or not.
      *>   cite.py: OK  11.3.3 3)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505P INHERITS FROM PB1505P USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1505P
           CLASS T.
       END CLASS PB1505P.
