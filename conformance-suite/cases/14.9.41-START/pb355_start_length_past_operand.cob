      *>--- conformance-case
      *> id: pb355_start_length_past_operand
      *> rules: [GR-14.9.41.4-13, GR-14.9.41.4-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> !! START'S TEMPORARY KEY AREA IS CUT OUT OF THE RECORD AREA
      *> (kb/Work PB355). ISO 1989:2023 14.9.41.4 GR17 a): "The
      *> specified key is set up by moving the relevant parts of the
      *> record area into a temporary data area." b): "The length of
      *> this temporary area is considered to be the length specified
      *> in the LENGTH clause, if specified, or else the length of
      *> record-key-name-1, if specified, or else the length of
      *> data-name-1."  GR16 says which key a) means: "The key
      *> specified in the KEY phrase, or that shares a leftmost
      *> character with the data item specified in the KEY phrase,
      *> becomes the key of reference."
      *> So the search key is THE KEY OF REFERENCE'S CHARACTERS IN THE
      *> RECORD AREA, cut to the b) length - data-name-1 names the key
      *> and supplies a default length, and is never itself the source.
      *> The two readings coincide for every length no longer than
      *> data-name-1 (a generic key's own content IS the area's content
      *> at those positions - 13.18.33.4 GR3: "Multiple level 1 entries
      *> subordinate to a FD or SD entry represent implicit
      *> redefinitions of the same area"), so only a LENGTH counting
      *> PAST the operand can tell them apart. A1-A4 do; A5-A6 are the
      *> complement, pinning that the short case did not move.
      *>
      *> THE FILE (written in ascending prime-key order, 14.9.51):
      *>   prime  alt    data        prime order   alternate order
      *>   AB01   ZZ01   REC-01      AB01 AB99 CD01   YY01 ZZ01 ZZ99
      *>   AB99   ZZ99   REC-99
      *>   CD01   YY01   REC-CD
      *> GN-PFX (X(2) at the prime key's leftmost position) and GN-APFX
      *> (X(2) at the alternate key's) are 14.9.41.3 SR6 b) generic
      *> keys: same class, category and usage, and shorter.
      *> 14.9.30.4 GR21 b) makes the START-selected record the one the
      *> following READ NEXT delivers; 9.1.13.2 rule 1 gives '00'.
      *>
      *> DERIVATION, arm by arm - every value below is computed from
      *> the rules above, none from the compiler:
      *>  A1  area prime = "AB01", LENGTH 4 -> temp "AB01"; keys cut to
      *>      4 are AB01/AB99/CD01; EQUAL stops on AB01     -> REC-01.
      *>      Building the temp from GN-PFX instead gives "AB" padded
      *>      to "AB  ", which equals no key at all: '23'.
      *>  A2  area prime = "AB99", LENGTH 3 -> temp "AB9"; keys cut to
      *>      3 are AB0/AB9/CD0; EQUAL stops on AB99        -> REC-99.
      *>      From GN-PFX: "AB " - again no match, '23'.
      *>  A3  area prime = "AB50", LENGTH 4, operator GREATER -> temp
      *>      "AB50"; the first key cut to 4 that exceeds it is AB99
      *>                                                   -> REC-99.
      *>      From GN-PFX: "AB  ", and "AB01" > "AB  " is TRUE, so the
      *>      padded reading answers REC-01 - a WRONG RECORD, silently.
      *>  A4  the ALTERNATE key of reference: area alt = "YY01",
      *>      LENGTH 4 -> temp "YY01"; the alternate keys cut to 4 are
      *>      YY01/ZZ01/ZZ99 and EQUAL stops on YY01, whose record is
      *>      the CD01 one                                 -> REC-CD.
      *>      From GN-APFX: "YY  ", no match, '23'.
      *>  A5  the complement - LENGTH 2, no longer than GN-PFX, so both
      *>      readings give "AB" out of the area's "ABZZ"; keys cut to
      *>      2 are AB/AB/CD and NOT LESS stops on AB01     -> REC-01.
      *>      (Had the LENGTH been ignored, "AB01" >= "ABZZ" is false
      *>      and the answer would be REC-CD, so the arm also pins that
      *>      the count is honoured.)
      *>  A6  no LENGTH phrase: GR17 b)'s "else the length of
      *>      data-name-1" makes the temp TWO characters, "AB" out of
      *>      the same "ABZZ", and EQUAL stops on AB01      -> REC-01.
      *>      (Defaulting to the KEY's length 4 would compare "ABZZ"
      *>      and find nothing: '23'.)
      *>  A7  GR14 - "If arithmetic-expression-1 does not evaluate to a
      *>      positive nonzero integer that is less than or equal to
      *>      the length of the associated key, the I-O status value
      *>      ... is set to '23', the invalid key condition exists":
      *>      LENGTH 5 exceeds the four-character prime key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB355SLP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb355slp.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-PRIME
               ALTERNATE RECORD KEY IS IX-ALT
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-PRIME PIC X(4).
          05 IX-ALT   PIC X(4).
          05 IX-DATA  PIC X(6).
       01 IX-VIEW.
          05 GN-PFX   PIC X(2).
          05 GN-MID   PIC X(2).
          05 GN-APFX  PIC X(2).
          05 GN-REST  PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "AB01" TO IX-PRIME
           MOVE "ZZ01" TO IX-ALT
           MOVE "REC-01" TO IX-DATA
           WRITE IX-REC
           MOVE "AB99" TO IX-PRIME
           MOVE "ZZ99" TO IX-ALT
           MOVE "REC-99" TO IX-DATA
           WRITE IX-REC
           MOVE "CD01" TO IX-PRIME
           MOVE "YY01" TO IX-ALT
           MOVE "REC-CD" TO IX-DATA
           WRITE IX-REC
           CLOSE IXF
           OPEN INPUT IXF
      *> A1 - LENGTH 4 reaches two characters past the two-character
      *> generic key, into the rest of the prime key in the area.
           MOVE "AB01" TO IX-PRIME
           START IXF KEY IS = GN-PFX WITH LENGTH 4
               INVALID KEY DISPLAY "A1=INVALID"
           END-START
           DISPLAY "A1-ST=" ST
           READ IXF NEXT AT END DISPLAY "A1=EOF" END-READ
           DISPLAY "A1=" IX-DATA
      *> A2 - the same reach with an ODD length, one past the operand.
           MOVE "AB99" TO IX-PRIME
           START IXF KEY IS = GN-PFX WITH LENGTH 3
               INVALID KEY DISPLAY "A2=INVALID"
           END-START
           DISPLAY "A2-ST=" ST
           READ IXF NEXT AT END DISPLAY "A2=EOF" END-READ
           DISPLAY "A2=" IX-DATA
      *> A3 - GREATER, where the invented spaces answer a DIFFERENT
      *> record instead of failing: the arm that makes this a
      *> wrong-answer defect and not merely a missed positioning.
           MOVE "AB50" TO IX-PRIME
           START IXF KEY IS > GN-PFX WITH LENGTH 4
               INVALID KEY DISPLAY "A3=INVALID"
           END-START
           DISPLAY "A3-ST=" ST
           READ IXF NEXT AT END DISPLAY "A3=EOF" END-READ
           DISPLAY "A3=" IX-DATA
      *> A4 - the same rule on the ALTERNATE key of reference (GR16's
      *> "shares a leftmost character" arm over ALTERNATE RECORD KEY).
           MOVE "YY01" TO IX-ALT
           START IXF KEY IS = GN-APFX WITH LENGTH 4
               INVALID KEY DISPLAY "A4=INVALID"
           END-START
           DISPLAY "A4-ST=" ST
           READ IXF NEXT AT END DISPLAY "A4=EOF" END-READ
           DISPLAY "A4=" IX-DATA
      *> A5 - the complement: a LENGTH no longer than the operand, on
      *> which both readings agree and nothing may move.
           MOVE "AB" TO GN-PFX
           MOVE "ZZ" TO GN-MID
           START IXF KEY IS >= GN-PFX WITH LENGTH 2
               INVALID KEY DISPLAY "A5=INVALID"
           END-START
           DISPLAY "A5-ST=" ST
           READ IXF NEXT AT END DISPLAY "A5=EOF" END-READ
           DISPLAY "A5=" IX-DATA
      *> A6 - no LENGTH phrase: GR17 b) falls back to data-name-1's own
      *> length, NOT to the key's.
           MOVE "AB" TO GN-PFX
           MOVE "ZZ" TO GN-MID
           START IXF KEY IS = GN-PFX
               INVALID KEY DISPLAY "A6=INVALID"
           END-START
           DISPLAY "A6-ST=" ST
           READ IXF NEXT AT END DISPLAY "A6=EOF" END-READ
           DISPLAY "A6=" IX-DATA
      *> A7 - GR14 still bounds the count by the key's own length.
           MOVE "AB" TO GN-PFX
           START IXF KEY IS = GN-PFX WITH LENGTH 5
               INVALID KEY DISPLAY "A7=INVALID"
               NOT INVALID KEY DISPLAY "A7=ACCEPTED"
           END-START
           DISPLAY "A7-ST=" ST
           CLOSE IXF
           STOP RUN.
