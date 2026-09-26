      *> reject-at: 2002 2014 2023
      *> ISO §11.7.3 SR2 — OVERRIDE on a METHOD-ID inside an
      *> INTERFACE-ID (a method prototype).
      *> Rule: "The OVERRIDE phrase shall not be specified in a method
      *> prototype."
      *> cite.py: OK  §11.7.3 2)  (Syntax rules)
      *> "Method definitions within an interface definition define
      *> method prototypes."
      *> cite.py: OK  §9.3.7   (Method prototypes)
      *> General format §11.7.2: METHOD-ID. method-name-1 [ OVERRIDE ]
      *> [ IS FINAL ] . — so `METHOD-ID. PING OVERRIDE.` is otherwise
      *> well-formed, and IS FINAL is NOT written, so SR8 (the sibling
      *> fixture l1c18-method-prototype-final) is not violated.  The
      *> prototype body is the same empty shape as that fixture's.
      *> Expected: COBOLNET0840 with the prototype message head.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7OVM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM L1M7OVM.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1M7OVI.
       PROCEDURE DIVISION.
       METHOD-ID. PING OVERRIDE.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE L1M7OVI.
