      *> ISO/IEC 1989:2023 7.3.16.2 / 7.3.13.2 general formats (kb/Work PB1363): IF [ELSE] END-IF and
      *> EVALUATE WHEN... [WHEN OTHER] END-EVALUATE nest freely, and the phrase directives may carry an inline comment
      *> (7.3.3 SR3, cite.py --check 7.3.3 "may be followed only by space characters and an optional inline comment"
      *> -> OK 3)). Expected output derived from 7.3.16.4 / 7.3.13.4: with V = 1 the outer IF arm is taken, the EVALUATE
      *> selects WHEN 1 (its nested IF takes its THEN arm), and the omitted ELSE branch's EVALUATE is never compiled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363OK.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V = 1
       >>EVALUATE V
       >>WHEN 2
           DISPLAY "W2".
       >>WHEN 1
           DISPLAY "W1".
       >>IF V = 1 *> nested
           DISPLAY "NESTED-IF".
       >>ELSE *> nested else
           DISPLAY "NESTED-ELSE".
       >>END-IF *> nested end
       >>WHEN OTHER
           DISPLAY "OTHER".
       >>END-EVALUATE *> done
       >>ELSE
       >>EVALUATE V
       >>WHEN 1
           DISPLAY "NO".
       >>END-EVALUATE
       >>END-IF
           STOP RUN.
