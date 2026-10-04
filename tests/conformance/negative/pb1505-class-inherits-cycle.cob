      *> reject-at: 2002 2014 2023
      *> kb/Work PB1505 - ISO/IEC 1989:2023 section 11.3.3 SR4: "Object-class-name-2 shall not inherit from
      *> object-class-name-1 directly or indirectly."
      *>   cite.py: OK  11.3.3 4)
      *> PB1505A inherits from PB1505B, which inherits from PB1505A.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505A INHERITS FROM PB1505B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1505B.
       END CLASS PB1505A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505B INHERITS FROM PB1505A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1505A.
       END CLASS PB1505B.
