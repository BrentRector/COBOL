      *> reject-at: 2002 2014 2023
      *> ISO §11.3.3 SR4 — object-class-name-2 shall not inherit from
      *> object-class-name-1 INDIRECTLY.
      *>   cite.py --check 11.3.3 "Object-class-name-2 shall not inherit
      *>     from object-class-name-1 directly or indirectly." -> OK 4)
      *>   cite.py --check 11.3.3 "Object-class-name-2 shall be the name
      *>     of a class specified in the REPOSITORY paragraph of this
      *>     source element." -> OK 2)
      *> Three classes, a cycle of length three:
      *>   L1CXA INHERITS FROM L1CXC
      *>   L1CXB INHERITS FROM L1CXA
      *>   L1CXC INHERITS FROM L1CXB
      *> For L1CXA's CLASS-ID, object-class-name-2 is L1CXC; L1CXC
      *> inherits from L1CXB, which inherits from L1CXA - L1CXC inherits
      *> from L1CXA INDIRECTLY (through one intermediate class), so SR4
      *> is violated; no pair of the three inherits from each other
      *> directly in both directions, so only the "indirectly" arm of SR4
      *> can reject this source. SR2 (each base is in its REPOSITORY) and
      *> SR3 (no class names itself) are satisfied.
      *> The .err holds the cycle arm's own message text (COBOLNET0820 is
      *> shared with other arms; see the -direct twin and kb/Work
      *> PB1505 for the citation in that message).
      *> Reject-at names 2002 onward: class definitions are COBOL-2002.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1CXA INHERITS FROM L1CXC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1CXC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK-A.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A".
       END METHOD SPEAK-A.
       END OBJECT.
       END CLASS L1CXA.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1CXB INHERITS FROM L1CXA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1CXA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK-B.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "B".
       END METHOD SPEAK-B.
       END OBJECT.
       END CLASS L1CXB.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1CXC INHERITS FROM L1CXB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1CXB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK-C.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "C".
       END METHOD SPEAK-C.
       END OBJECT.
       END CLASS L1CXC.
