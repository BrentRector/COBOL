      *> kb/Work PB130 / PB1135 — CALL Format 2's keyword-less and BY-less argument forms, pinned from the legal
      *> side. ISO 14.9.4.2 prints BY un-underlined before VALUE (5.2.3: optional word) — `USING VALUE N`
      *> parses; Format 2's BY phrases are plain brackets so literal-2 and arithmetic-expression-1 are legal
      *> keyword-less arguments — `USING 42`, `USING (N + 1)` and `USING N + 1` pass a VALUE (GR9 a)2, BY CONTENT).
      *>
      *> DECISION R59 (kb/Work PB1135): a keyword-less `N + 1` is ONE argument, the arithmetic-expression-1 of the
      *> printed format, evaluated before the call. The format prints it under an OPTIONAL `[ BY CONTENT ]` and
      *> cite.py --check 14.9.4.3 "BY CONTENT shall not be omitted when identifier-4 is an identifier that is
      *> permitted as a receiving operand" -> OK 14.9.4.3 20 forbids omitting BY CONTENT only for an identifier
      *> that can receive, which an expression is not; 14.9.4.3 17 makes N in it a sending operand; and
      *> cite.py --check 8.7.1 "preceded by a space and followed by a space" -> OK 8.7.1 makes the `+` the BINARY
      *> operator (a separated sign is no literal, 8.3.3.3.2 2). The earlier "list reading" (the two arguments N and
      *> +1) is gone. The negatives pb130-* pin Format 1's rejections of the same spellings.
      *>
      *> Derivation (N=7): N + 1 = 0008; N - 1 = 0006; N * 2 = 0014; 5 + 1 = 0006 (a numeric literal is the left
      *> operand); BY CONTENT N + 1 = 0008; SUB3 USING N N + 1 is the two arguments N and N + 1: 0007 0008;
      *> SUB3 USING N + 1 N is N + 1 and N: 0008 0007 (an identifier after the literal 1 ends the expression).
      *> Sole operands keep their own kind (14.9.4.4 GR8): `USING N` is identifier-2, passed BY REFERENCE, so
      *> SUB4's store to its formal is visible in N (0099); `USING 42` stays literal-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB130F2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB1" AS NESTED USING VALUE N
           CALL "SUB2" AS NESTED USING 42
           CALL "SUB2" AS NESTED USING (N + 1)
           CALL "SUB2" AS NESTED USING N + 1
           CALL "SUB2" AS NESTED USING N - 1
           CALL "SUB2" AS NESTED USING N * 2
           CALL "SUB2" AS NESTED USING 5 + 1
           CALL "SUB2" AS NESTED USING BY CONTENT N + 1
           CALL "SUB3" AS NESTED USING N N + 1
           CALL "SUB3" AS NESTED USING N + 1 N
           CALL "SUB4" AS NESTED USING N
           DISPLAY "N " N
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC 9(4).
       PROCEDURE DIVISION USING VALUE LA.
       M1.
           DISPLAY "S1 " LA
           GOBACK.
       END PROGRAM SUB1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 9(4).
       PROCEDURE DIVISION USING LB.
       M2.
           DISPLAY "S2 " LB
           GOBACK.
       END PROGRAM SUB2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC PIC 9(4).
       01 LD PIC 9(4).
       PROCEDURE DIVISION USING LC LD.
       M3.
           DISPLAY "S3 " LC " " LD
           GOBACK.
       END PROGRAM SUB3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE PIC 9(4).
       PROCEDURE DIVISION USING LE.
       M4.
           MOVE 99 TO LE
           GOBACK.
       END PROGRAM SUB4.
       END PROGRAM PB130F2.
