      *>--- conformance-case
      *> id: pb810_read_46_takes_no_at_end
      *> rules: [GR-14.9.30.4-21, GR-14.9.30.4-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB810 -- a sequential READ whose I-O status is '46'
      *> takes NEITHER the AT END nor the NOT AT END phrase; it reaches
      *> the USE declarative through input-output exception processing.
      *>
      *> THE RULES.
      *>   python scripts/spec/cite.py --check 14.9.30.4 "the I-O status
      *>   is set to '46' and execution proceeds as indicated in General
      *>   rule 24"                          -> OK  §14.9.30.4 21)
      *>   python scripts/spec/cite.py --check 14.9.30.4 "If, during the
      *>   execution of the READ statement, the at end condition exists,
      *>   the following occurs in the order specified"
      *>                                     -> OK  §14.9.30.4 24)
      *>   python scripts/spec/cite.py --check 9.1.13.7 "I-O status = 46.
      *>   A sequential READ statement is attempted"
      *>                                     -> OK  §9.1.13.7 6)
      *>   python scripts/spec/cite.py --check 14.9.30.4 "If neither an
      *>   at end nor an invalid key condition occurs during the execution
      *>   of a READ statement, the AT END phrase or the INVALID KEY phrase
      *>   is ignored"                       -> OK  §14.9.30.4 13)
      *>   python scripts/spec/cite.py --check 14.9.30.4 "If an exception
      *>   condition that is not an at end or an invalid key condition
      *>   exists, control is transferred according to the rules in
      *>   9.1.12"                           -> OK  §14.9.30.4 13) b)
      *>
      *> THE DETERMINATION (kb/Work PB810). GR24's actions are
      *> conditioned on the at end condition, and '46' is not it: it is
      *> a LOGIC ERROR (§9.1.13.7 6)). So §14.9.30.4 GR13 governs --
      *> the AT END and NOT AT END phrases are ignored, and GR13 b)
      *> transfers control
      *> through §9.1.12 to the applicable USE declarative, then to the
      *> end of the READ.
      *>
      *> WHY EACH LEG CAN FAIL. The first READ of each empty file is the
      *> at end condition ('10') and MUST take AT END. The second READ
      *> (and, for RLF, the READ after an unsuccessful START) MUST set
      *> '46', run the declarative once, and print neither the AT END
      *> nor the NOT AT END line -- an emitter that treated every
      *> unsuccessful status as at end prints "AT-END 46", and one that
      *> treated it as success prints "NAE". The three organizations
      *> reach the READ through two different emitter arms (sequential,
      *> and relative/indexed), so each is pinned separately.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB810R46.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb810-s.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SFS.
           SELECT RLF ASSIGN TO "pb810-r.dat"
               ORGANIZATION IS RELATIVE ACCESS IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS RFS.
           SELECT IXF ASSIGN TO "pb810-x.dat"
               ORGANIZATION IS INDEXED ACCESS IS SEQUENTIAL
               RECORD KEY IS XK
               FILE STATUS IS XFS.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQR PIC X(4).
       FD RLF.
       01 RLR PIC X(4).
       FD IXF.
       01 IXR.
          02 XK PIC X(4).
       WORKING-STORAGE SECTION.
       01 SFS PIC XX.
       01 RFS PIC XX.
       01 XFS PIC XX.
       01 RK  PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SQF RLF IXF.
       D1P.
           DISPLAY "DECL " SFS " " RFS " " XFS.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT SQF CLOSE SQF
           OPEN OUTPUT RLF CLOSE RLF
           OPEN OUTPUT IXF CLOSE IXF
           OPEN INPUT SQF
           READ SQF AT END DISPLAY "SQF-1 AT-END " SFS
               NOT AT END DISPLAY "SQF-1 NAE " SFS END-READ
           READ SQF AT END DISPLAY "SQF-2 AT-END " SFS
               NOT AT END DISPLAY "SQF-2 NAE " SFS END-READ
           DISPLAY "SQF-AFTER " SFS
           CLOSE SQF
           OPEN INPUT RLF
           MOVE 5 TO RK
           START RLF KEY IS EQUAL TO RK
               INVALID KEY DISPLAY "RLF-START INVALID " RFS END-START
           READ RLF NEXT AT END DISPLAY "RLF-1 AT-END " RFS
               NOT AT END DISPLAY "RLF-1 NAE " RFS END-READ
           DISPLAY "RLF-AFTER " RFS
           CLOSE RLF
           OPEN INPUT IXF
           READ IXF AT END DISPLAY "IXF-1 AT-END " XFS
               NOT AT END DISPLAY "IXF-1 NAE " XFS END-READ
           READ IXF AT END DISPLAY "IXF-2 AT-END " XFS
               NOT AT END DISPLAY "IXF-2 NAE " XFS END-READ
           DISPLAY "IXF-AFTER " XFS
           CLOSE IXF
           STOP RUN.
