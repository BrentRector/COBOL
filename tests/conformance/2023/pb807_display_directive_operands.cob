      *> kb/Work PB807 + PB1538 - ISO 7.3.12.2: the operands of the
      *> DISPLAY directive repeat - an arithmetic expression, a boolean
      *> expression, a literal, a compilation-variable-name or a
      *> PARAMETER phrase - and an optional UPON phrase follows. Every
      *> directive below is a legal operand list. The directive writes
      *> at compile time (DOC-A.1-53, not captured by this corpus) and
      *> adds nothing to the program output; the PARAMETER operand names
      *> an environment variable that is not set, so that directive
      *> transfers nothing (7.3.12.4 GR3) and compiles all the same.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB807A.
000300 >>DEFINE N AS 3
000400 >>DISPLAY "HELLO-COMPILE" 42 N
000500 >>DISPLAY B"1100" B-AND B"1010"
000600 >>DISPLAY 1 + 2 "AFTER"
000700 >>DISPLAY PARAMETER PB807_NOT_SET_IN_ANY_ENVIRONMENT
000800 >>DISPLAY "to listing" UPON LISTING
000900 >>DISPLAY "to stderr" UPON SYSERR
001000 >>DISPLAY "both" UPON CONSOLE LISTING
001100 PROCEDURE DIVISION.
001200     DISPLAY "DISPLAY-DIRECTIVE-RUN".
001300     STOP RUN.
