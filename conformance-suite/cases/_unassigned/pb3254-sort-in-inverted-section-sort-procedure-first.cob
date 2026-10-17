*>--- conformance-case
*> id: pb3254-sort-in-inverted-section-sort-procedure-first
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3254 - AN INVERTED THRU OVER SECTIONS KEEPS BOTH OPERANDS' EXTENT.
      *>   cite.py --check 14.9.40.3 "A SORT statement shall not appear in imperative-statement-1 of an
      *>     exception-checking PERFORM statement, in an input or output procedure, or in a declarative procedure."
      *>     -> OK §14.9.40.3 3)
      *>   cite.py --check 14.9.28.4 "There is no necessary relationship between procedure-name-1 and
      *>     procedure-name-2 except that a consecutive sequence of operations is to be executed beginning at the
      *>     procedure named procedure-name-1" -> OK §14.9.28.4 6)
      *> INPUT PROCEDURE IS S-B THRU S-A names SECTIONS with S-A (A1, A2) standing BEFORE S-B (B1, B2, B3), which
      *> GR6 permits. The procedure is every paragraph from the first statement of S-B to the last of S-A, i.e.
      *> A1 through B3. The file SORT in B2 is in procedure-name-1 itself, the middle of S-B; the composed pair
      *> (B1, A2) forgot that S-B runs on to B3 and that S-A starts at A1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3254N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB3254N1.tmp".
           SELECT SW2 ASSIGN TO "PB3254N1s2.tmp".
           SELECT F1 ASSIGN TO "PB3254N11.dat".
           SELECT F2 ASSIGN TO "PB3254N12.dat".
           SELECT F3 ASSIGN TO "PB3254N13.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC X(4).
       SD SW2.
       01 SR2.
          05 SK2 PIC X(4).
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       FD F3.
       01 R3 PIC X(4).
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           SORT SW ASCENDING KEY SK
               INPUT PROCEDURE IS S-B THRU S-A
               GIVING F3
           STOP RUN.
       S-A SECTION.
       A1.
           CONTINUE.
       A2.
           CONTINUE.
       S-B SECTION.
       B1.
           CONTINUE.
       B2.
           SORT SW2 ASCENDING KEY SK2 USING F1 GIVING F2.
       B3.
           CONTINUE.
