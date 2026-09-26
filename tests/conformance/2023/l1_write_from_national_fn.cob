      *> ISO §14.9.51.3 11) — WRITE record-name-1 FROM a NATIONAL function, no FILE phrase: admitted, and its value is written
      *> THE RULE: "If identifier-1 references a function and the FILE
      *>   phrase is not specified, identifier-1 shall reference an
      *>   alphanumeric, boolean, or national function."
      *>   cite.py --check 14.9.51.3 "If identifier-1 references a
      *>     function and the FILE phrase is not specified, identifier-1
      *>     shall reference an alphanumeric, boolean, or national
      *>     function" -> OK §14.9.51.3 11)
      *> SR4 of the same clause holds unconditionally:
      *>   cite.py --check 14.9.51.3 "If identifier-1 is a function-
      *>     identifier, it shall reference an alphanumeric or national
      *>     function" -> OK §14.9.51.3 4)
      *> A national function satisfies BOTH, so this arm is legal source
      *> under every reading. The refusal arm (an integer function) is
      *> conformance:negative/pb348-write-from-integer-function and the
      *> alphanumeric arm is conformance:2023/pb10_function_identifier_
      *> sending; this golden is the NATIONAL arm neither of them pins.
      *> (The boolean arm is deliberately not pinned here: SR11 admits
      *> it and SR4 does not — see the row's notes.)
      *> SUPPORTING RULES:
      *>   cite.py --check 14.9.51.4 "The result of the execution of a
      *>     WRITE statement specifying record-name-1 and the FROM phrase
      *>     is equivalent to the execution of the following statements
      *>     in the order specified" -> OK §14.9.51.4 5) (MOVE, then WRITE)
      *>   cite.py --check 15.66.1 "The type of the function is national"
      *>     -> OK §15.66.1
      *>   cite.py --check 15.66.4 "A character string is returned with
      *>     each alphanumeric character in argument-1 converted to its
      *>     corresponding national coded character set representation"
      *>     -> OK §15.66.4 1)
      *>   cite.py --check 15.97.1 "National | National" -> OK §15.97.1
      *>     (UPPER-CASE of a national argument is a national function)
      *>   cite.py --check 15.97.4 "A character string with the content
      *>     of argument-1 is returned, with any lowercase letters
      *>     replaced by their corresponding uppercase letters"
      *>     -> OK §15.97.4 1) ("CD" has no lowercase letter, so it is
      *>     returned unchanged whatever the case correspondence)
      *>   cite.py --check 14.9.25.4 "alignment and any necessary space
      *>     filling shall take place as defined in 14.6.8"
      *>     -> OK §14.9.25.4 6) a)
      *>   cite.py --check 9.1.13.2 "I-O status = 00. The input-output
      *>     statement is successfully executed and no further
      *>     information is available concerning the input-output
      *>     operation" -> OK §9.1.13.2 1)
      *> DERIVATION of every .out line:
      *>   W1 00  WRITE NREC FROM NATIONAL-OF("AB") succeeds
      *>   W2 00  WRITE NREC FROM UPPER-CASE(N"CD") succeeds
      *>   R1 [AB  ] 00  the 2-character national value MOVEd to PIC N(4)
      *>                 is left-aligned and space filled (GR5 a))
      *>   R2 [CD  ] 00  likewise
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1W11NF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "L1W11NF.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD NF.
       01 NREC PIC N(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT NF
           WRITE NREC FROM FUNCTION NATIONAL-OF("AB")
           DISPLAY "W1 " FS
           WRITE NREC FROM FUNCTION UPPER-CASE(N"CD")
           DISPLAY "W2 " FS
           CLOSE NF
           OPEN INPUT NF
           READ NF AT END DISPLAY "EOF1" END-READ
           DISPLAY "R1 [" NREC "] " FS
           READ NF AT END DISPLAY "EOF2" END-READ
           DISPLAY "R2 [" NREC "] " FS
           CLOSE NF
           STOP RUN.
