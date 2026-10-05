      *> kb/Work PB152 - ISO 11.9.10.4 GR7, the EXCLUSION: "External items in the Working-storage section are
      *> not initialized when runtime elements are put into the initial state, except for those with the
      *> CONSTANT RECORD clause."
      *>
      *> An EXTERNAL record's storage is shared across the whole run unit and outlives any one runtime
      *> element's initial state, so the background must NOT be laid over it. The CONSTANT RECORD case is the
      *> stated exception - its content IS its initialization (13.18.15.4 GR1) - and it is written here as the
      *> POSITIVE half, so the exclusion cannot be implemented as a blanket "skip everything EXTERNAL".
      *>
      *> EXPECTED, with INITIALIZE ALL TO X"5A" in force:
      *>   E  EXTERNAL PIC X(4), no VALUE   -> four SPACES (GR7: not initialized; the baseline stands)
      *>   P  PIC X(4), no VALUE            -> ZZZZ        (an ordinary working-storage item IS filled)
      *>   CT EXTERNAL CONSTANT RECORD      -> its own content, "kv" padded (GR7's exception; 13.18.15.4 GR1
      *>      initializes it as though by INITIALIZE ... TO VALUE THEN TO DEFAULT)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152EXT.
       OPTIONS.
           INITIALIZE ALL TO X"5A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E IS EXTERNAL PIC X(4).
       01 P PIC X(4).
      *> 13.16.3 SR13 requires an EXTERNAL CONSTANT RECORD to name a STRONGLY typed definition at 2023.
       01 CTT TYPEDEF STRONG IS EXTERNAL.
          05 CT-K PIC X(4) VALUE "kv".
       01 CT IS EXTERNAL CONSTANT RECORD TYPE CTT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "E=[" E "]".
           DISPLAY "P=[" P "]".
           DISPLAY "CTK=[" CT-K IN CT "]".
           STOP RUN.
