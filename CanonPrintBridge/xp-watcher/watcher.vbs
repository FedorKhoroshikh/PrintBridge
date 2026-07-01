' ============================================================================
'  Canon LBP-1120 print-queue watcher  (runs INSIDE the Windows XP guest)
'
'  Watches the shared queue folder for <id>.job.json, prints the matching
'  PDF on the Canon CAPT printer via SumatraPDF, and writes the job status
'  back as status\<id>.status.json.
'
'  Comments are intentionally in English: XP / cscript handle non-ASCII
'  source files poorly. User-facing text lives in the .md docs instead.
' ============================================================================

Option Explicit

' ---- CONFIG ----------------------------------------------------------------
Const QUEUE_DIR      = "\\vboxsvr\Shared\Queue"
Const SUMATRA        = "C:\Program Files\SumatraPDF\SumatraPDF.exe"
Const PRINTER_BASE   = "Canon LBP-1120"   ' queues: "Canon LBP-1120 A4" / "...A5" / "...B5"
Const POLL_MS        = 2000               ' how often to scan the queue
Const FLIP_TIMEOUT_S = 300                ' max wait for the manual-duplex "continue" flag
Const SPOOL_TIMEOUT_S = 180               ' max wait for the print spooler queue to drain
Const POLL_SPOOL_MS   = 1000              ' how often to poll the spooler while draining
Const SPOOL_SETTLE_MS = 2000              ' spooler must read empty this long before "done"
' ----------------------------------------------------------------------------

Dim fso, sh, statusDir, printedDir
Set fso = CreateObject("Scripting.FileSystemObject")
Set sh  = CreateObject("WScript.Shell")

statusDir  = QUEUE_DIR & "\status"
printedDir = QUEUE_DIR & "\printed"
EnsureDir QUEUE_DIR
EnsureDir statusDir
EnsureDir printedDir

' Main loop -- runs forever (placed in XP Startup folder).
Do
    WriteHealth
    If fso.FolderExists(QUEUE_DIR) Then
        Dim f
        For Each f In fso.GetFolder(QUEUE_DIR).Files
            If LCase(Right(f.Name, 9)) = ".job.json" Then
                ProcessJob f.Path
            End If
        Next
    End If
    WScript.Sleep POLL_MS
Loop

' ============================================================================

Sub ProcessJob(jobPath)
    On Error Resume Next

    Dim json, id, pdf, copies, paper, scale, pages, duplex, pdfPath, printer, rc
    json = ReadAll(jobPath)
    If json = "" Then Exit Sub          ' file mid-write; retry next round

    id     = GetVal(json, "id")
    pdf    = GetVal(json, "file")
    copies = GetVal(json, "copies")
    paper  = GetVal(json, "paper")
    scale  = GetVal(json, "scale")
    pages  = GetVal(json, "pages")
    duplex = GetVal(json, "duplex")
    If id = "" Or pdf = "" Then Exit Sub

    pdfPath = QUEUE_DIR & "\" & pdf
    If Not fso.FileExists(pdfPath) Then Exit Sub   ' PDF not copied in yet

    printer = PRINTER_BASE & " " & paper
    WriteStatus id, "printing", "paper " & paper & ", copies " & copies

    If LCase(duplex) = "manual" Then
        ' Pass 1 -- odd pages
        rc = RunPrint(printer, BuildSettings("odd", copies, scale, pages), pdfPath)
        If rc <> 0 Then FailJob id, jobPath, "SumatraPDF rc=" & rc & " (odd)" : Exit Sub
        WaitForSpoolDrain printer   ' let odd pages finish before prompting the flip

        WriteStatus id, "awaiting-flip", "flip the stack, then continue"
        If Not WaitForContinue(id) Then FailJob id, jobPath, "flip timeout" : Exit Sub

        ' Pass 2 -- even pages
        WriteStatus id, "printing", "even pages"
        rc = RunPrint(printer, BuildSettings("even", copies, scale, pages), pdfPath)
        If rc <> 0 Then FailJob id, jobPath, "SumatraPDF rc=" & rc & " (even)" : Exit Sub
        WaitForSpoolDrain printer
    Else
        rc = RunPrint(printer, BuildSettings("", copies, scale, pages), pdfPath)
        If rc <> 0 Then FailJob id, jobPath, "SumatraPDF rc=" & rc : Exit Sub
        WaitForSpoolDrain printer   ' wait for real completion, not just SumatraPDF exit
    End If

    MoveAside pdfPath, printedDir
    DeleteFile jobPath
    WriteStatus id, "done", ""
End Sub

' Build the SumatraPDF -print-settings value (comma-separated list).
Function BuildSettings(sel, copies, scale, pages)
    Dim parts
    parts = ""
    If sel   <> "" Then parts = AppendCsv(parts, sel)
    If pages <> "" Then parts = AppendCsv(parts, pages)
    If scale <> "" Then parts = AppendCsv(parts, scale)
    If IsNumeric(copies) Then
        If CLng(copies) > 1 Then parts = AppendCsv(parts, CLng(copies) & "x")
    End If
    BuildSettings = parts
End Function

' Run SumatraPDF hidden, wait for it, return its exit code.
Function RunPrint(printer, settings, pdfPath)
    Dim cmd
    cmd = """" & SUMATRA & """ -print-to """ & printer & """"
    If settings <> "" Then cmd = cmd & " -print-settings """ & settings & """"
    cmd = cmd & " -silent """ & pdfPath & """"
    RunPrint = sh.Run(cmd, 0, True)
End Function

' Wait for <id>.continue (dropped by the WPF app after the user flips the stack).
Function WaitForContinue(id)
    Dim flag, waited
    flag = QUEUE_DIR & "\" & id & ".continue"
    waited = 0
    Do While waited < FLIP_TIMEOUT_S * 1000
        If fso.FileExists(flag) Then
            DeleteFile flag
            WaitForContinue = True
            Exit Function
        End If
        WScript.Sleep 500
        waited = waited + 500
    Loop
    WaitForContinue = False
End Function

Sub FailJob(id, jobPath, msg)
    WriteStatus id, "error", msg
    DeleteFile jobPath
End Sub

' ---- health heartbeat ------------------------------------------------------

' Rewrite status\bridge.health.json every loop so the Win11 app can gauge guest
' liveness (by the file's host-side mtime, since the XP clock is unreliable) and
' whether the printer is online. Atomic write, like WriteStatus.
Sub WriteHealth()
    On Error Resume Next
    Dim present, pname, p, tmp, ts
    pname = ""
    If PrinterOnline(pname) Then present = "true" Else present = "false"
    p   = statusDir & "\bridge.health.json"
    tmp = p & ".tmp"
    Set ts = fso.CreateTextFile(tmp, True)
    ts.Write "{""watcher"":true,""printerPresent"":" & present & _
             ",""printerName"":""" & JsonEsc(pname) & """,""tick"":""" & Now & """}"
    ts.Close
    If fso.FileExists(p) Then fso.DeleteFile p, True
    fso.MoveFile tmp, p
End Sub

' True if a "Canon LBP-1120 ..." printer exists and is not marked offline.
' Sets outName to the matched queue name. WMI failures leave it False (unknown).
Function PrinterOnline(ByRef outName)
    On Error Resume Next
    Dim wmi, items, prn
    outName = ""
    PrinterOnline = False
    Set wmi = GetObject("winmgmts:\\.\root\cimv2")
    If Err.Number <> 0 Then Exit Function
    Set items = wmi.ExecQuery("SELECT Name, WorkOffline FROM Win32_Printer")
    If Err.Number <> 0 Then Exit Function
    For Each prn In items
        If InStr(1, prn.Name, PRINTER_BASE, 1) = 1 Then
            outName = prn.Name
            If prn.WorkOffline = False Then PrinterOnline = True
            Exit Function
        End If
    Next
End Function

' ---- real print completion -------------------------------------------------

' Block until the printer's Windows spooler queue drains (the best available
' proxy for physical completion of a host-based CAPT job) plus a short settle.
' A WMI failure makes the count 0, so this returns at once -- no worse than before.
Sub WaitForSpoolDrain(printer)
    On Error Resume Next
    Dim waited, settle
    waited = 0 : settle = 0
    Do While waited < SPOOL_TIMEOUT_S * 1000
        WriteHealth   ' keep the heartbeat fresh while this blocks the main loop
        If SpoolJobCount(printer) = 0 Then
            settle = settle + POLL_SPOOL_MS
            If settle >= SPOOL_SETTLE_MS Then Exit Sub
        Else
            settle = 0
        End If
        WScript.Sleep POLL_SPOOL_MS
        waited = waited + POLL_SPOOL_MS
    Loop
End Sub

' Count spooler jobs belonging to the given printer. Win32_PrintJob.Name is
' formatted "<printer name>, <job id>", so a prefix match selects our jobs.
Function SpoolJobCount(printer)
    On Error Resume Next
    Dim wmi, jobs, j, c
    c = 0
    Set wmi = GetObject("winmgmts:\\.\root\cimv2")
    If Err.Number <> 0 Then SpoolJobCount = 0 : Exit Function
    Set jobs = wmi.ExecQuery("SELECT Name FROM Win32_PrintJob")
    If Err.Number <> 0 Then SpoolJobCount = 0 : Exit Function
    For Each j In jobs
        If InStr(1, j.Name, printer, 1) = 1 Then c = c + 1
    Next
    SpoolJobCount = c
End Function

' ---- helpers ---------------------------------------------------------------

' Extract a value from flat JSON: "key":"str"  or  "key":number
Function GetVal(json, key)
    Dim re, m
    Set re = New RegExp
    re.IgnoreCase = True
    re.Global = False

    re.Pattern = """" & key & """\s*:\s*""([^""]*)"""
    Set m = re.Execute(json)
    If m.Count > 0 Then GetVal = m(0).SubMatches(0) : Exit Function

    re.Pattern = """" & key & """\s*:\s*([0-9]+)"
    Set m = re.Execute(json)
    If m.Count > 0 Then GetVal = m(0).SubMatches(0) Else GetVal = ""
End Function

Function AppendCsv(acc, item)
    If acc = "" Then AppendCsv = item Else AppendCsv = acc & "," & item
End Function

Function ReadAll(path)
    On Error Resume Next
    Dim ts, s
    Set ts = fso.OpenTextFile(path, 1, False)
    If Err.Number <> 0 Then ReadAll = "" : Exit Function
    s = ""
    If Not ts.AtEndOfStream Then s = ts.ReadAll
    ts.Close
    ReadAll = s
End Function

Sub WriteStatus(id, state, msg)
    On Error Resume Next
    Dim p, tmp, ts
    p   = statusDir & "\" & id & ".status.json"
    tmp = p & ".tmp"
    Set ts = fso.CreateTextFile(tmp, True)
    ts.Write "{""id"":""" & id & """,""state"":""" & state & _
             """,""message"":""" & JsonEsc(msg) & """,""updatedAt"":""" & Now & """}"
    ts.Close
    If fso.FileExists(p) Then fso.DeleteFile p, True
    fso.MoveFile tmp, p
End Sub

Function JsonEsc(s)
    Dim r
    r = Replace(s, "\", "\\")
    r = Replace(r, """", "\""")
    JsonEsc = r
End Function

Sub MoveAside(filePath, destDir)
    On Error Resume Next
    Dim name, dest
    name = fso.GetFileName(filePath)
    dest = destDir & "\" & name
    If fso.FileExists(dest) Then fso.DeleteFile dest, True
    fso.MoveFile filePath, dest
End Sub

Sub DeleteFile(p)
    On Error Resume Next
    If fso.FileExists(p) Then fso.DeleteFile p, True
End Sub

Sub EnsureDir(d)
    On Error Resume Next
    If Not fso.FolderExists(d) Then fso.CreateFolder d
End Sub
