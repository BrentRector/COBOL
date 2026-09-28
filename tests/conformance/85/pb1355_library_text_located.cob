      *> kb/Work PB1355 - locating library text (the DOC-A.1-40
      *> determination in docs/CONFORMANCE.md, GnuCOBOL's order).
      *> RULES (each run through scripts/spec/cite.py --check):
      *>   7.2.3.4 1) "Text-name-1 or literal-1 identifies the library
      *>     text to be processed by the COPY statement"     -> OK 1)
      *>   7.2.3.4 2) "Library-name-1 names a resource that shall be
      *>     available to the compiler and shall provide access to
      *>     the library text"                                -> OK 2)
      *>   7.2.3.4 3) "The implementor defines the mechanism for
      *>     identifying the default COBOL library"           -> OK 3)
      *>   7.2.3.3 SR3 "Within one COBOL library, each text-name
      *>     shall be unique"                                  -> OK
      *> The runner names this directory as a --copy search path.
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [CPY] pb1355a.cpy and pb1355a.cbl both sit here; the name
      *>         is tried with .CPY .CBL .COB .cpy .cbl .cob in that
      *>         order, so the .cpy text IS PB1355A.
      *>   [LIB] COPY pb1355b OF pb1355lib: the library is the
      *>         subdirectory pb1355lib, and the text is located in it
      *>         alone - a pb1355b.cpy beside this program holds DFLT.
      *>   [TXT] literal-1 "pb1355c.txt" contains a period, so it is
      *>         tried only as spelled.
      *>   [CBL] pb1355lib holds pb1355d.cbl and pb1355d.cob: .CBL/.cbl
      *>         is tried before .COB/.cob, so the text is the .cbl one
      *>         (the old '' .cpy .cob .cbl order took the .cob: COB).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1355LOC85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY pb1355a.
       COPY pb1355b OF pb1355lib.
       COPY "pb1355c.txt".
       COPY pb1355d IN pb1355lib.
       PROCEDURE DIVISION.
           DISPLAY "[" PB1355A-V "]"
           DISPLAY "[" PB1355B-V "]"
           DISPLAY "[" PB1355C-V "]"
           DISPLAY "[" PB1355D-V "]"
           STOP RUN.
