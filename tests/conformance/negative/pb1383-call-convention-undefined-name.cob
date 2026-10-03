      *> reject-at: 2002 2014 2023
      *> kb/Work PB1383 -- ISO 7.3.9.2: >>CALL-CONVENTION { COBOL |
      *> call-convention-name-1 }; 7.3.9.3 GR2 b): "When
      *> call-convention-name-1 is specified, that program-name or
      *> method-name is treated as a literal" mapped in a manner defined
      *> by the implementor. WiseOwl COBOL defines no call-convention-name
      *> (docs/CONFORMANCE.md DOC-A.1-68), so XYZZY selects no convention
      *> and no mapping: the directive is malformed, COBOLNET1911. (Before
      *> the fix any COBOL word was accepted and the COBOL-word mapping
      *> silently applied.)
       >>CALL-CONVENTION XYZZY
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1383M.
       PROCEDURE DIVISION.
           CALL "n1383s"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1383S.
       PROCEDURE DIVISION.
           DISPLAY "IN SUB"
           GOBACK.
       END PROGRAM N1383S.
       END PROGRAM N1383M.
