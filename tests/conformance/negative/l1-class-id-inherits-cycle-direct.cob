      *> reject-at: 2002 2014 2023
      *> ISO §11.3.3 SR4 — object-class-name-2 shall not inherit from
      *> object-class-name-1 DIRECTLY.
      *>   cite.py --check 11.3.3 "Object-class-name-2 shall not inherit
      *>     from object-class-name-1 directly or indirectly." -> OK 4)
      *>   cite.py --check 11.3.3 "Object-class-name-2 shall be the name
      *>     of a class specified in the REPOSITORY paragraph of this
      *>     source element." -> OK 2)
      *> L1CYA INHERITS FROM L1CYB, and L1CYB INHERITS FROM L1CYA. For
      *> L1CYA's CLASS-ID, object-class-name-1 is L1CYA and
      *> object-class-name-2 is L1CYB, which inherits from L1CYA
      *> directly - SR4 is violated (and symmetrically for L1CYB). SR2
      *> is satisfied (each base is in its REPOSITORY) and SR3 is
      *> satisfied (neither class names itself), so SR4 is the only
      *> ground for rejection.
      *> The .err holds the cycle arm's own message text rather than the
      *> bare code: COBOLNET0820 is shared with the duplicate-class and
      *> END CLASS mismatch arms. (The message's citation reads
      *> "ISO §11.3.2", the general format, not this SR4 - kb/Work
      *> PB1505; the .err deliberately avoids the citation text.)
      *> Reject-at names 2002 onward: class definitions are COBOL-2002.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1CYA INHERITS FROM L1CYB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1CYB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK-A.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A".
       END METHOD SPEAK-A.
       END OBJECT.
       END CLASS L1CYA.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1CYB INHERITS FROM L1CYA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1CYA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK-B.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "B".
       END METHOD SPEAK-B.
       END OBJECT.
       END CLASS L1CYB.
