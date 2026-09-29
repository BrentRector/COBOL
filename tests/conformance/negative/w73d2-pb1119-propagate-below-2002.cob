      *> reject-at: 85
      *> The REJECT half of conformance:2002/w73d2_pb1119_propagate_on
      *> (kb/Work PB1119). The PROPAGATE directive (ISO 7.3.21) belongs to
      *> the ISO/IEC 1989:2002 exception-condition facility; at COBOL-85
      *> the directive does not exist, so the compiler refuses it with the
      *> registry's introduction band COBOLNET0900 rather than compiling a
      *> program whose unhandled conditions would no longer propagate.
       >>PROPAGATE ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73D2N85.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
