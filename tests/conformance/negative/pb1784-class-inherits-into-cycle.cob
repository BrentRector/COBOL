      *> reject-at: 2002 2014 2023
      *> kb/Work PB1784 - ISO/IEC 1989:2023 section 11.3.3 SR4: "Object-class-name-2 shall not inherit from
      *> object-class-name-1 directly or indirectly."
      *>   cite.py: OK  11.3.3 4)
      *> P1784B and P1784C inherit from each other, so EACH breaks SR4; P1784A only inherits INTO the cycle
      *> and breaks nothing. The .err is P1784C's report: the walk that cut links while it went reported
      *> P1784A instead, and never reached P1784C.
       IDENTIFICATION DIVISION.
       CLASS-ID. P1784A INHERITS FROM P1784B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1784B.
       END CLASS P1784A.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1784B INHERITS FROM P1784C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1784C.
       END CLASS P1784B.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1784C INHERITS FROM P1784B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1784B.
       END CLASS P1784C.
