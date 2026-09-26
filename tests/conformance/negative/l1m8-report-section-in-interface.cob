      *> reject-at: 2002 2014 2023
      *> ISO §13.8.3 1) — a REPORT SECTION within an INTERFACE definition
      *> Rule: "The report section shall not be specified within an
      *>   interface definition."
      *>   cite.py: OK  §13.8.3 1)  (Syntax rule)
      *> The interface-definition general format (§10.6.1) has NO
      *> data-division slot - only the options paragraph, the
      *> environment division and the procedure division - so the one
      *> place a data division, and so a REPORT SECTION, can stand
      *> inside an interface definition is a method prototype ("A
      *> method-definition in an interface-definition defines a method
      *> prototype", cite.py: OK §10.6.1, the NOTE). That is the
      *> shape here: prototype SPEAK carries an (empty) REPORT SECTION
      *> and is otherwise a bare header.
      *> Expected: COBOLNET2272, the prototype-body screen, arm e):
      *>   §10.6.2 4) e) "The data division may contain only a linkage
      *>   section." (cite.py: OK §10.6.2 4) e)) - the rule through
      *>   which §13.8.3's interface prohibition is enforced for the one
      *>   reachable shape. The .err pins that arm's message.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M8NI.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM L1M8NI.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1M8NIF.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       REPORT SECTION.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE L1M8NIF.
