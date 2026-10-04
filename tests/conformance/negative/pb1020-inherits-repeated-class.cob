      *> reject-at: 2002 2014 2023
      *> kb/Work PB1020 - ISO/IEC 1989:2023 section 11.3.3 SR7: "A given class name shall not appear more than once in an
      *> INHERITS clause."  The repeated name breaks this syntax rule; it is not a use of multiple inheritance (one class
      *> is named), so the diagnostic is the rule's and not the declined Annex A.4.10 facility's (COBOLNET0849).
      *>   cite.py: OK  11.3.3 7)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1020B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB1020B.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1020C INHERITS FROM PB1020B PB1020B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1020B.
       END CLASS PB1020C.
