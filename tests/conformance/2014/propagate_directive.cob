      *> >>PROPAGATE directive (ISO 7.3.21, COBOL-2002+): recognized and
      *> edition-gated (COBOLNET0900 below 2002). It controls automatic
      *> exception-condition propagation to the activating runtime element
      *> (GR1/GR2, default OFF per GR4). This program raises nothing, so
      *> ON changes nothing here: it compiles and runs. The propagation
      *> itself is conformance:2002/w73d2_pb1119_propagate_on.
      >>PROPAGATE ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PROPAGATE-DIRECTIVE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PROPAGATE OK".
           STOP RUN.
