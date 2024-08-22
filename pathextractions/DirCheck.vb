Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Collections.Generic
Imports System.Reflection.Metadata

Public Class DirCheck

    Sub CheckDir()
        LogError(outputFilePath, "###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   Dir Check Started" + "    ###################################")

        If Not File.Exists(batchFilePath) Then
            LogError(outputFilePath, $"Batch file not found: { batchFilePath}")
            Return
        End If

        ' Read the batch file content
        Dim scriptContent As String
        Try
            scriptContent = File.ReadAllText(batchFilePath)
        Catch ex As Exception
            LogError(outputFilePath, "Error reading the batch file: " & ex.Message)
            Return
        End Try

        ' Define patterns to identify the steps and extract dir commands
        Dim stepPattern As String = "^\*+\s+STEP\d+_(BEFORE|AFTER)_DIR_CHECK\s+\*+:"
        Dim dirPattern As String = "dir\s+/S\s+(\%[^%\s]+(?:\\[^%\s]*)*\%)"
        Dim ifErrorPattern As String = "^\s*if\s+errorlevel\s+\d+"

        Dim lines() As String = scriptContent.Split(New String() {Environment.NewLine}, StringSplitOptions.None)

        Dim capturingBefore As Boolean = False
        Dim capturingAfter As Boolean = False
        Dim currentStepType As String = ""
        Dim capturedDirsBefore As New List(Of String)
        Dim capturedDirsAfter As New List(Of String)

        For i As Integer = 0 To lines.Length - 1
            Dim line As String = lines(i).Trim()

            ' Check for step lines
            Dim stepMatch As Match = Regex.Match(line, stepPattern)

            If stepMatch.Success Then
                ' Stop capturing if previously capturing
                If capturingBefore OrElse capturingAfter Then
                    capturingBefore = False
                    capturingAfter = False
                End If

                ' Start capturing for the new step
                currentStepType = stepMatch.Groups(1).Value & "_DIR_CHECK"

                If currentStepType = "BEFORE_DIR_CHECK" Then
                    capturingBefore = True
                ElseIf currentStepType = "AFTER_DIR_CHECK" Then
                    capturingAfter = True
                End If
            ElseIf capturingBefore OrElse capturingAfter Then
                ' Check for 'if errorlevel' to determine end of capturing
                If Regex.IsMatch(line, ifErrorPattern, RegexOptions.IgnoreCase) Then
                    capturingBefore = False
                    capturingAfter = False
                Else
                    ' Collect dir paths
                    Dim dirMatches As MatchCollection = Regex.Matches(line, dirPattern)
                    For Each dirMatch As Match In dirMatches
                        Dim dirPath As String = dirMatch.Groups(1).Value.Trim()
                        If capturingBefore Then
                            capturedDirsBefore.Add(dirPath)
                        ElseIf capturingAfter Then
                            capturedDirsAfter.Add(dirPath)
                        End If
                    Next
                End If
            End If
        Next

        ' Write results to output file
        Try
            Using writer As New StreamWriter(outputFilePath, True)
                writer.WriteLine("⇒Paths from BEFORE_DIR_CHECK:")
                If capturedDirsBefore.Count > 0 Then
                    For Each dirPath In capturedDirsBefore
                        writer.WriteLine(dirPath)
                    Next
                Else
                    writer.WriteLine("No paths found")
                End If

                writer.WriteLine()
                writer.WriteLine("⇒Paths from AFTER_DIR_CHECK:")
                If capturedDirsAfter.Count > 0 Then
                    For Each dirPath In capturedDirsAfter
                        writer.WriteLine(dirPath)
                    Next
                Else
                    writer.WriteLine("No paths found")
                End If

                ' Perform verification check
                writer.WriteLine()
                writer.WriteLine("Verification Results:")
                If capturedDirsBefore.SequenceEqual(capturedDirsAfter) Then
                    writer.WriteLine("⇒ verified: ok")
                Else
                    writer.WriteLine("⇒ Mismatch found:")
                    Dim beforePaths As New HashSet(Of String)(capturedDirsBefore)
                    Dim afterPaths As New HashSet(Of String)(capturedDirsAfter)

                    Dim extraInBefore = beforePaths.Except(afterPaths)
                    Dim extraInAfter = afterPaths.Except(beforePaths)

                    If extraInBefore.Any() Then
                        writer.WriteLine("⇒ Extra in BEFORE_DIR_CHECK:")
                        For Each path In extraInBefore
                            writer.WriteLine(path)
                        Next
                    End If

                    If extraInAfter.Any() Then
                        writer.WriteLine("⇒ Extra in AFTER_DIR_CHECK:")
                        For Each path In extraInAfter
                            writer.WriteLine(path)
                        Next
                    End If
                End If
            End Using
        Catch ex As Exception
            LogError(outputFilePath, "Error writing to DirCheckReport.log: " & ex.Message)
        End Try
        LogError(outputFilePath, "###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   Dir Check Completed" + "    ###################################")
        LogError(outputFilePath, "")
    End Sub

    ' Function to log errors to the output file
    Sub LogError(filePath As String, message As String)
        Try
            Using writer As New StreamWriter(filePath, True)
                writer.WriteLine($"{message}")
            End Using
        Catch ex As Exception
            ' If logging fails, there's no further action we can take here
        End Try
    End Sub
End Class
