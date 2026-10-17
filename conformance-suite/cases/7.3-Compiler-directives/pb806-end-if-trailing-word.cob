*>--- conformance-case
*> id: pb806-end-if-trailing-word
*> rules: [SR-7.3.3-3, SR-7.3.16.3-5, FMT-7.3.16.2]
*> source-format: free
*> portable: unverified
*>---
*> 7.3.3 SR3 (cite.py --check 7.3.3 "may be followed only by space characters and an optional inline comment"
*> -> OK 3)): the general format writes >> END-IF with no operand, so a word after it is neither space nor comment.
*> kb/Work PB806. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB806N02.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V = 1
           DISPLAY "A".
       >>END-IF JUNK
           STOP RUN.
