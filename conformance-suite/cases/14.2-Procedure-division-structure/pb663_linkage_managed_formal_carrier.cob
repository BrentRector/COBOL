*>--- conformance-case
*> id: pb663_linkage_managed_formal_carrier
*> rules: [GR-14.2.3-9, GR-14.2.3-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.2.3 GR8 / GR10 + §14.8.2.3.2 — a LINKAGE formal of class
      *> POINTER or class OBJECT-REFERENCE is carried by ITS OWN class's
      *> carrier, exactly as a numeric formal is (kb/Work PB663).
      *> GR8: "If the argument is passed by reference, the activated
      *> runtime element operates as if the formal parameter occupies the
      *> same storage area as the argument" — so a BY REFERENCE pointer
      *> formal IS the caller's pointer, and a SET through it is visible
      *> to the activator at activation end.
      *> GR10, BY VALUE: the allocated record is filled by "a SET
      *> statement" when "the formal parameter is of class object or
      *> pointer" — a DETACHED copy, so the callee's SET never reaches
      *> the caller.
      *> §14.8.2.3.2: "If either the argument or the formal parameter is
      *> of class pointer, the corresponding formal parameter or argument
      *> shall be of class pointer and the corresponding items shall be
      *> of the same category" — the two sides carry the same shape, so
      *> the crossing needs no conversion at all.
      *> §14.9.4.3 SR10 bars a FORMAT 1 CALL from passing such an item BY
      *> REFERENCE, so every activation here is Format 2 (AS NESTED) —
      *> which is exactly why this crossing had no arm for so long.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB663MAINP10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-AREA  PIC X(4) VALUE "ABCD".
       01 W-PTR   USAGE POINTER.
       01 W-PPTR  USAGE PROGRAM-POINTER.
       01 W-OREF  USAGE OBJECT REFERENCE.
       01 V-4 BASED PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
      *> BY REFERENCE, NULL: the formal sees the caller's NULL pointer.
           SET W-PTR TO NULL.
           CALL "PB663SUBP10" AS NESTED USING BY REFERENCE W-PTR.
      *> BY REFERENCE, a real address: the formal dereferences the
      *> caller's own storage (§14.2.3 GR8 — the same storage area).
           SET W-PTR TO ADDRESS OF W-AREA.
           CALL "PB663SUBP10" AS NESTED USING BY REFERENCE W-PTR.
      *> The callee SET the formal to NULL under BY REFERENCE, so GR8
      *> makes that store the CALLER's (the storage is shared).
           IF W-PTR = NULL
               DISPLAY "AFTER-REF=NULL"
           ELSE
               DISPLAY "AFTER-REF=SET"
           END-IF.
      *> BY CONTENT: GR9's second branch allocates "a data item with the
      *> same description ... as the formal parameter", fills it — "if
      *> the formal parameter is of class index, object, or pointer, a
      *> SET statement" — and then treats it "as if it were the argument
      *> and it were passed by reference". So the callee's SET reaches
      *> the ALLOCATED record, never the caller's storage.
           SET W-PTR TO ADDRESS OF W-AREA.
           CALL "PB663SUBP10" AS NESTED USING BY CONTENT W-PTR.
           IF W-PTR = NULL
               DISPLAY "AFTER-CON=NULL"
           ELSE
               DISPLAY "AFTER-CON=SET"
           END-IF.
      *> BY VALUE: GR10's detached record — the callee's SET TO NULL
      *> must NOT reach the caller's pointer.
           SET W-PTR TO ADDRESS OF W-AREA.
           CALL "PB663SUBVP10" AS NESTED USING BY VALUE W-PTR.
           IF W-PTR = NULL
               DISPLAY "AFTER-VAL=NULL"
           ELSE
               DISPLAY "AFTER-VAL=SET"
           END-IF.
      *> FORMAT 1 BY CONTENT. §14.9.4.3 SR10 bars only the BY REFERENCE
      *> phrase, so an ordinary Format-1 CALL may pass a class-pointer
      *> item BY CONTENT — and that takes GR9's FIRST branch ("no
      *> program-specifier ... and no NESTED phrase"), where the
      *> allocated record is "of the same length as the argument" and
      *> "that argument is moved to this allocated record without
      *> conversion", which for a managed reference IS the reference
      *> copy. The activator's pointer must survive the callee's SET.
           SET W-PTR TO ADDRESS OF W-AREA.
           CALL "PB663F1SUBP10" USING BY CONTENT W-PTR.
           IF W-PTR = NULL
               DISPLAY "AFTER-F1C=NULL"
           ELSE
               DISPLAY "AFTER-F1C=SET"
           END-IF.
      *> A PROGRAM-POINTER formal: the address crosses, and the CALL
      *> through the formal reaches the addressed program (§8.4.3.13
      *> GR1 — SET ... TO ENTRY; §14.9.4 Format 1 identifier-1).
           SET W-PPTR TO ENTRY "PB663TGTP10".
           CALL "PB663PPSUBP10" AS NESTED USING BY REFERENCE W-PPTR.
      *> An OBJECT REFERENCE formal: its initial state is NULL
      *> (§13.18.63) and it crosses as the reference, not an image.
           CALL "PB663ORSUBP10" AS NESTED USING BY REFERENCE W-OREF.
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB663SUBP10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V-IN BASED PIC X(4).
       LINKAGE SECTION.
       01 L-PTR USAGE POINTER.
       PROCEDURE DIVISION USING BY REFERENCE L-PTR.
       SUBMAIN.
           IF L-PTR = NULL
               DISPLAY "REF=NULL"
           ELSE
               SET ADDRESS OF V-IN TO L-PTR
               DISPLAY "REF=" V-IN
           END-IF.
           SET L-PTR TO NULL.
           GOBACK.
       END PROGRAM PB663SUBP10.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB663SUBVP10.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-VPTR USAGE POINTER.
       PROCEDURE DIVISION USING BY VALUE L-VPTR.
       SUBVMAIN.
           IF L-VPTR = NULL
               DISPLAY "VAL=NULL"
           ELSE
               DISPLAY "VAL=SET"
           END-IF.
           SET L-VPTR TO NULL.
           GOBACK.
       END PROGRAM PB663SUBVP10.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB663PPSUBP10.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-PPTR USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING BY REFERENCE L-PPTR.
       PPSUBMAIN.
           IF L-PPTR = NULL
               DISPLAY "PP=NULL"
           ELSE
               DISPLAY "PP=SET"
           END-IF.
           CALL L-PPTR.
           GOBACK.
       END PROGRAM PB663PPSUBP10.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB663ORSUBP10.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-OREF USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING BY REFERENCE L-OREF.
       ORSUBMAIN.
           IF L-OREF = NULL
               DISPLAY "OR=NULL"
           ELSE
               DISPLAY "OR=SET"
           END-IF.
           GOBACK.
       END PROGRAM PB663ORSUBP10.
       END PROGRAM PB663MAINP10.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB663F1SUBP10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V-F1 BASED PIC X(4).
       LINKAGE SECTION.
       01 L-F1 USAGE POINTER.
       PROCEDURE DIVISION USING L-F1.
       F1SUBMAIN.
           IF L-F1 = NULL
               DISPLAY "F1C=NULL"
           ELSE
               SET ADDRESS OF V-F1 TO L-F1
               DISPLAY "F1C=" V-F1
           END-IF.
           SET L-F1 TO NULL.
           GOBACK.
       END PROGRAM PB663F1SUBP10.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB663TGTP10.
       PROCEDURE DIVISION.
       TGTMAIN.
           DISPLAY "TGT-RAN".
           GOBACK.
       END PROGRAM PB663TGTP10.
