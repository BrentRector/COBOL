*>--- conformance-case
*> id: pb2740-omitted-branch-turn-unknown-name
*> rules: [SR-7.3.25.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.2.1 (cite.py --check 7.2.1 "syntactically correct in the initial source text and library text" -> OK):
*> compiler directives shall be syntactically correct in the initial source text, and text-1 of an IF directive may be any kind
*> of source lines, compiler directives included (7.3.16.3 SR6, cite.py OK), so the omitted branch of >>IF 1 = 0 is such text.
*> 7.3.25.3 SR2 (cite.py --check 7.3.25.3 "Exception-name-1 shall be one of the exception names listed in 14.6.13.1" -> OK):
*> EC-NOSUCH is not one of them, so this TURN directive is malformed exactly as it is outside the >>IF (COBOLNET0711 - the
*> same code the compiled twin, pb1531-ec-imp-suffix-turn, pins). It compiled clean: the SR2 check lived in the binder,
*> which reads compiled events alone. kb/Work PB2740.
       >>IF 1 = 0
       >>TURN EC-NOSUCH CHECKING ON
       >>END-IF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2740N01.
       PROCEDURE DIVISION.
           DISPLAY "A".
           STOP RUN.
