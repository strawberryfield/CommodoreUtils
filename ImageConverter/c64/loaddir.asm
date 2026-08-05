; ========================================================
; LOADDIR.ASM - FAST DIRECTORY SCANNER FOR C64
; ========================================================
; Legge la directory dal drive 8, filtra i file che finiscono
; per ".BM" e salva i nomi (senza ".BM") a partire da $C400.
; Ogni nome è lungo 16 byte (riempito di spazi se più corto).
; A $C3FF scrive il numero totale di file trovati (MAX_FILES).
; ========================================================

* = $C000

BUFFER      = $C400       ; Dove memorizzare i nomi trovati
COUNT_PTR   = $C3FF       ; Numero totale di file trovati
TMP_PTR     = $FB         ; Puntatore temporaneo Zero Page ($FB-$FC)

    ; 1. Inizializzazione
    LDA #$00
    STA COUNT_PTR
    LDA #<BUFFER
    STA TMP_PTR
    LDA #>BUFFER
    STA TMP_PTR+1

    ; 2. Imposta file per la directory "$"
    LDA #$01            ; Logical File = 1
    LDX $BA             ; Usa l'ultimo Device Number usato (default 8)
    BNE DEVICE_OK
    LDX #$08            ; Se 0, usa 8
DEVICE_OK:
    LDY #$00            ; Secondary Address = 0 (canale dati dir)
    JSR $FFBA           ; SETLFS

    LDA #DIR_NAME_LEN   ; Lunghezza del nome directory
    LDX #<DIR_NAME
    LDY #>DIR_NAME
    JSR $FFBD           ; SETNAM

    JSR $FFC0           ; OPEN (Apre il canale della directory)
    BCC OPEN_OK
    JMP ERRORE
OPEN_OK:

    LDX #$01
    JSR $FFC6           ; CHKIN (Imposta File 1 come input)

    ; 3. Salta i primi due byte (Load Address della directory)
    ; NOTA: END_DIR e' troppo lontano per un BCS diretto da qui,
    ; quindi questi controlli usano il trampolino CHRIN_ERROR
    ; (un solo JMP END_DIR condiviso, invece di uno per ciascuno).
    JSR CHRIN_ST
    BCS CHRIN_ERROR
    JSR CHRIN_ST
    BCS CHRIN_ERROR

NEXT_LINE:
    ; Ogni riga della directory ha: 2 byte Next Pointer, 2 byte Line Number
    JSR CHRIN_ST        ; Next Pointer Low
    BCS CHRIN_ERROR
    JSR CHRIN_ST        ; Next Pointer High
    BCS CHRIN_ERROR
    JSR CHRIN_ST        ; Line Number Low
    BCS CHRIN_ERROR
    JSR CHRIN_ST        ; Line Number High
    BCS CHRIN_ERROR

    ; 4. Cerca il carattere di inizio nome: virgolette '"' (ASCII 34)
FIND_QUOTE:
    JSR CHRIN_ST
    BCS CHRIN_ERROR
    CMP #$00
    BEQ NEXT_LINE       ; Fine riga senza virgolette
    CMP #34             ; '"'
    BNE FIND_QUOTE

    ; 5. Legge il nome del file dentro le virgolette
    LDY #$00
READ_NAME:
    JSR CHRIN_ST
    BCS CHRIN_ERROR
    CMP #34             ; Virgoletta di chiusura?
    BEQ PARSE_NAME
    CPY #16             ; Max 16 caratteri
    BCS READ_NAME       ; Se supera 16, ignora i caratteri extra
    STA TBL_NAME,Y
    INY
    JMP READ_NAME

CHRIN_ERROR:            ; trampolino condiviso: END_DIR e' fuori range
    JMP END_DIR         ; per i controlli qui sopra

PARSE_NAME:
    TYA
    BEQ SKIP_LINE
    STY NAME_LEN

    ; 5b. Il nome nella directory e' sempre paddato a 16 caratteri
    ;     con spazi (es. "PICTURE.BM     "). Togliamo gli spazi
    ;     finali per ottenere la lunghezza REALE del nome, altrimenti
    ;     il controllo del suffisso ".BM" confronta degli spazi
    ;     invece delle lettere e non trova mai nessun file.
TRIM_SPACES:
    LDA NAME_LEN
    BEQ SKIP_LINE       ; Nome tutto spazi
    TAY
    DEY
    LDA TBL_NAME,Y
    CMP #$20            ; ' '
    BNE TRIM_DONE
    DEC NAME_LEN
    JMP TRIM_SPACES
TRIM_DONE:

    ; 6. Salta il resto della riga fino al byte 0
SKIP_REST:
    JSR CHRIN_ST
    BCS END_DIR         ; qui END_DIR e' vicino: BCS diretto, niente JMP
    CMP #$00
    BNE SKIP_REST

    ; 7. Verifico se finisce per ".BM"
    ; Lunghezza minima: 3 caratteri (es. "A.BM")
    LDA NAME_LEN
    CMP #$03
    BCC NEXT_LINE

    LDY NAME_LEN
    DEY
    LDA TBL_NAME,Y
    CMP #$4D            ; 'M'
    BNE NEXT_LINE
    DEY
    LDA TBL_NAME,Y
    CMP #$42            ; 'B'
    BNE NEXT_LINE
    DEY
    LDA TBL_NAME,Y
    CMP #$2E            ; '.'
    BNE NEXT_LINE

    ; 8. MATCH TROVATO! Copia il nome (senza ".BM") nel buffer
    ; Lunghezza senza .BM = NAME_LEN - 3
    LDA NAME_LEN
    SEC
    SBC #$03
    STA COPY_LEN

    LDY #$00
COPY_LOOP:
    CPY COPY_LEN
    BCS PAD_SPACES
    LDA TBL_NAME,Y
    STA (TMP_PTR),Y
    INY
    JMP COPY_LOOP

PAD_SPACES:
    ; Riempie i byte rimanenti fino a 16 con spazi ' '
    CPY #16
    BCS DONE_COPY
    LDA #$20            ; ' '
    STA (TMP_PTR),Y
    INY
    JMP PAD_SPACES

DONE_COPY:
    ; Incrementa puntatore buffer ($C400 + 16 per ogni file)
    LDA TMP_PTR
    CLC
    ADC #16
    STA TMP_PTR
    BCC PTR_HI_OK
    INC TMP_PTR+1
PTR_HI_OK:
    INC COUNT_PTR       ; Incrementa contatore file
    JMP NEXT_LINE

SKIP_LINE:
    JSR CHRIN_ST
    BCS END_DIR         ; anche qui END_DIR e' vicino
    CMP #$00
    BNE SKIP_LINE
    JMP NEXT_LINE

END_DIR:
    JSR $FFCC           ; CLRCHN
    LDA #$01
    JSR $FFC3           ; CLOSE 1
    RTS

ERRORE:
    JSR $FFCC
    RTS

; CHRIN_ST: Legge un carattere da CHRIN ($FFE4) e controlla lo status con READST ($FFB7)
; Restituisce: A = carattere letto, Carry Clear = OK, Carry Set = Error/EOF
CHRIN_ST:
    JSR $FFE4           ; CHRIN - legge carattere
    PHA                 ; salva il carattere: READST sotto sovrascrive A!
    JSR $FFB7           ; READST - controlla status I/O
    CMP #$00
    BEQ CS_OK
    PLA                 ; ripristina il carattere (bilancia lo stack)
    SEC                 ; Error/EOF - set carry
    RTS
CS_OK:
    PLA                 ; ripristina il carattere letto in A
    CLC                 ; OK - clear carry
    RTS

; Variabili e buffer temporanei (posti dopo il codice)
NAME_LEN:   .byte 0
COPY_LEN:   .byte 0
TBL_NAME:   .byte 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
DIR_NAME:   .byte "$"   ; Nome directory per LOAD "$",8
DIR_NAME_LEN = * - DIR_NAME  ; Lunghezza = 1