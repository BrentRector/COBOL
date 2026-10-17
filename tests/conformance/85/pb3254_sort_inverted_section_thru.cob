      *> kb/Work PB3254 - AN INVERTED THRU OVER SECTIONS IS LEGAL AND RUNS (ISO §14.9.28.4 GR6)
      *>   cite.py --check 14.9.28.4 "There is no necessary relationship between procedure-name-1 and
      *>     procedure-name-2 except that a consecutive sequence of operations is to be executed beginning at the
      *>     procedure named procedure-name-1" -> OK §14.9.28.4 6)
      *> INPUT PROCEDURE IS S-B THRU S-A with S-A physically BEFORE S-B. The sort procedure's range is the
      *> consecutive sequence beginning at B1 and ending with the last statement of S-A, and the return mechanism
      *> sits after A2 (§14.9.40.4 GR11); B3's GO TO A1 is the transfer that makes the sequence consecutive. The
      *> binder's lexical span for the ban on a SORT in the procedure (SR3) covers A1..B3, and this program has no
      *> SORT or MERGE there, so it compiles and runs.
      *> Derivation:
      *>  Released in execution order: B1 -> C, B2 -> B, (GO TO) A1 -> A, A2 -> D.
      *>  Keys are distinct; SORT ASCENDING on SK -> A B C D, so the output procedure displays
      *>  OUT AA1 / OUT BB2 / OUT CB1 / OUT DA2 (key, then the releasing paragraph).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3254A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB3254A.TMP".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR.
           05 SK PIC X(1).
           05 SP PIC X(2).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           SORT SF ON ASCENDING KEY SK
               INPUT PROCEDURE IS S-B THRU S-A
               OUTPUT PROCEDURE IS S-O.
           DISPLAY "END".
           STOP RUN.
       S-A SECTION.
       A1.
           MOVE "A" TO SK. MOVE "A1" TO SP.
           RELEASE SR.
       A2.
           MOVE "D" TO SK. MOVE "A2" TO SP.
           RELEASE SR.
       S-B SECTION.
       B1.
           MOVE "C" TO SK. MOVE "B1" TO SP.
           RELEASE SR.
       B2.
           MOVE "B" TO SK. MOVE "B2" TO SP.
           RELEASE SR.
       B3.
           GO TO A1.
       S-O SECTION.
       O1.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SF
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "OUT " SK SP
               END-RETURN
           END-PERFORM.
