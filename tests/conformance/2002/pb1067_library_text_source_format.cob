      *> kb/Work PB1067 - library text is logically converted by the
      *> same 6.5 walker as the source text, starting in the format in
      *> effect for its COPY statement; its own SOURCE FORMAT
      *> directives switch it, and the COPY's text reverts after it.
      *> RULES (each run through scripts/spec/cite.py --check):
      *>   7.3.24.3 1) "shall be treated as fixed form if FIXED is
      *>     specified, or as free form if FREE is specified" -> OK 1)
      *>   7.3.24.3 3) "The default reference format of library text
      *>     is the reference format that was in effect for the COPY
      *>     statement"                                     -> OK 3)
      *>   7.3.24.3 5) "the reference format shall revert to the
      *>     reference format that was in effect for the COPY
      *>     statement"                                     -> OK 5)
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [CPY-FIXED]  pb1067a starts fixed (the COPY is in fixed
      *>                form): its sequence area is not program text.
      *>   [CPY-FREE]   pb1067a's >>SOURCE FREE makes its next line
      *>                free form; read as fixed it was dropped.
      *>   [MAIN-FIXED] after pb1067a the main text is fixed form
      *>                again: 000400 is its sequence area.
      *>   [LIB-FREE]   pb1067b, copied from free-form text, starts
      *>                free: its DISPLAY in column 1 is program text.
      *>   [LIB-FIXED]  pb1067b switches itself to fixed form.
      *>   [MAIN-FREE]  after pb1067b the main text is free form
      *>                again, so a DISPLAY in column 1 is program text.
       >>SOURCE FORMAT FIXED
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1067SRC02.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY pb1067a.
000400     DISPLAY "[MAIN-FIXED]"
       >>SOURCE FORMAT FREE
COPY pb1067b.
DISPLAY "[MAIN-FREE]"
STOP RUN.
