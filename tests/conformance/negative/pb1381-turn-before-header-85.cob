      *> reject-at: 85
      *> ISO §7.3.25 - the >>TURN directive is a COBOL-2002 introduction,
      *> so the header-scoped placement kb/Work PB1381 fixed (a TURN
      *> between the DATA DIVISION and the PROCEDURE DIVISION header,
      *> §7.3.25.4 GR6: "procedure division statements and procedure
      *> division headers that follow in the compilation group" -
      *> cite.py --check 7.3.25.4 -> OK §7.3.25.4 6)) is refused at
      *> COBOL-85 by the edition gate (positive twin:
      *> 2002/pb1381_turn_at_procedure_header).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1381N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(4).
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       PROCEDURE DIVISION USING X.
       P-MAIN.
           GOBACK.
       END PROGRAM PB1381N.
