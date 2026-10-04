      *> reject-at: 2002 2014 2023
      *> kb/Work PB1449 / PB1503 - ISO/IEC 1989:2023 section 11.7.3 SR7: "If the SET phrase is specified, then the method
      *> shall have a single USING parameter specified in the procedure division header and no RETURNING phrase."  The
      *> rule is a syntax rule of the METHOD-ID paragraph, whose format a method prototype shares (11.7.2), so it holds
      *> for an interface's SET PROPERTY prototype too.
      *>   cite.py: OK  11.7.3 7)
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1449K.
       PROCEDURE DIVISION.
       METHOD-ID. SET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-A PIC 9(5).
       01 LK-B PIC 9(5).
       PROCEDURE DIVISION USING LK-A LK-B.
       END METHOD.
       END INTERFACE PB1449K.
