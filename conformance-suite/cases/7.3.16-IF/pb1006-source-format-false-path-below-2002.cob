*>--- conformance-case
*> id: pb1006-source-format-false-path-below-2002
*> rules: [GR-7.3.16.4-2, GR-7.3.16.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:2002/pb1006_source_format_in_false_path
      *> (kb/Work PB1006). The DEFINE, IF and SOURCE FORMAT directives are
      *> 2002 introductions (Annex E), so at 1985 the >>DEFINE line draws the
      *> compiler-directive introduction gate, COBOLNET0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1006NG.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V = 2
       >>SOURCE FREE
       >>END-IF
           DISPLAY "UNREACHED"
           STOP RUN.
