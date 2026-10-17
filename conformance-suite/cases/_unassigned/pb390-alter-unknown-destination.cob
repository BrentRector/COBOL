*>--- conformance-case
*> id: pb390-alter-unknown-destination
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB390 - ALTER's SECOND procedure-name, the arm that used to be silent. ALTER is ANSI
*> X3.23-1985 only (deleted by ISO/IEC 1989:2002, where the statement itself is refused), so this case is
*> scoped to 85. The rule is the general one: 8.4.2.1 - a statement's reference shall uniquely identify a
*> resource - with 8.4.6.1's scope for paragraph-names and section-names. Both ALTER operands now enter
*> the ONE procedure-name resolution, so neither can fail in silence; before PB390 an unknown destination
*> compiled and aborted the run unit claiming COBOL.NET had not implemented a feature.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB390ALTERDST.
PROCEDURE DIVISION.
MAIN.
    ALTER P1 TO PROCEED TO NO-SUCH-PARA.
    STOP RUN.
P1.
    GO TO P2.
P2.
    DISPLAY "P2".
