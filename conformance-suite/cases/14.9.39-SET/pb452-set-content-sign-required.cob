*>--- conformance-case
*> id: pb452-set-content-sign-required
*> rules: [GR-14.9.39.4-32]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.39.3 SR31 a): "If FARTHEST-FROM-ZERO is specified, and identifier-14 describes a
      *> signed numeric item for which the absolute value of the positive value farthest from zero is different
      *> from the absolute value of the negative value farthest from zero, the SIGN phrase shall be specified."
      *> A PIC S9(4) COMP-5 item owns its 2-byte container's whole two's-complement range (13.18.60.4 GR12),
      *> -32768 .. 32767, so |min| is not |max| and GR32 a)'s "the value farthest away from zero permitted by the
      *> specifications of identifier-14" names two different magnitudes. COBOLNET1939 (kb/Work PB452).
      *> The same statement WITH the phrase is legal and appears in 2014/pb452_set_content_numeric lines K and L.
      *> Rejected at 2014 and 2023 only because the format itself is a 2014 addition: below that the statement
      *> draws the introduction diagnostic COBOLNET0900 instead (constructs.json set-numeric-content-2014).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB452SIGNREQUIRED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-BINARY PIC S9(4) COMP-5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET CONTENT OF WS-BINARY TO FARTHEST-FROM-ZERO
           STOP RUN.
