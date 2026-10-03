      *> reject-at: 2002 2014 2023
      *> 7.3.3 SR8: 'A compiler directive may be specified anywhere in
      *> a compilation group ... except ... b) within a source text
      *> manipulation statement'. The >>DEFINE below stands between
      *> the COPY keyword and its text-name (kb/Work PB1384).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1384C.
000300 PROCEDURE DIVISION.
000400     COPY
000500 >>DEFINE VV AS 1
000600         PB1384CB.
000700     STOP RUN.
