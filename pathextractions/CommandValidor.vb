Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Collections.Generic

Public Class CommandValidators

    Sub ValidateCommand()
        WriteOutput("###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}    Command Analysis Started" + "    ###################################", outputFilePath)
        ' Regular expression patterns
        Dim delPattern As String = "DEL\s+(/[^""\s\/]+(?:\s/[^""\s\/]+)*)\s+(""[^""]*""|\S+)"
        Dim delRegex As New Regex(delPattern, RegexOptions.Compiled)

        Dim robocopyPattern As String = "ROBOCOPY\s+([^/\s]+(?:\s[^/\s]+)*)\s+([^/\s]+(?:\s[^/\s]+)*)\s*(.*)"
        Dim robocopyRegex As New Regex(robocopyPattern, RegexOptions.Compiled)

        Dim renamePattern As String = "RENAME\s+([^/\s]+(?:\s[^/\s]+)*)\s+([^/\s]+(?:\s[^/\s]+)*)"
        Dim renameRegex As New Regex(renamePattern, RegexOptions.Compiled)

        Dim sleepPattern As String = "\bSLEEP\b"
        Dim sleepRegex As New Regex(sleepPattern, RegexOptions.IgnoreCase Or RegexOptions.Compiled)

        Try
            ' Create or open the log file for writing
            Using logWriter As New StreamWriter(outputFilePath, append:=True)
                Dim lineNumber As Integer = 0
                Dim lines As List(Of String) = File.ReadLines(batchFilePath).ToList()

                Dim insideParentheses As Boolean = False

                ' Read the contents of the batch file line by line
                For i As Integer = 0 To lines.Count - 1
                    lineNumber += 1
                    Dim line As String = lines(i)

                    ' Update insideParentheses state
                    If line.Contains("(") Then
                        insideParentheses = True
                    End If
                    If line.Contains(")") Then
                        insideParentheses = False
                    End If

                    ' Extract and log DEL commands
                    Dim delMatches As MatchCollection = delRegex.Matches(line)
                    If delMatches.Count > 0 Then
                        For Each match As Match In delMatches
                            If match.Success Then
                                Dim options As String = match.Groups(1).Value.Trim()
                                Dim filePath As String = match.Groups(2).Value.Trim()
                                Dim parameters As String = String.Join(", ", ExtractParameters(options))

                                Dim delOutput As String = $"DEL Command ({lineNumber})" & Environment.NewLine &
                                                           $"Path: {filePath}" & Environment.NewLine &
                                                           $"Parameters: {parameters}"

                                ' Only include sleep status if DEL is not inside parentheses
                                If Not insideParentheses Then
                                    Dim sleepStatus As String = "false"

                                    ' Check the next two lines for the SLEEP command
                                    If i + 1 < lines.Count AndAlso sleepRegex.IsMatch(lines(i + 1)) Then
                                        sleepStatus = "true"
                                    ElseIf i + 2 < lines.Count AndAlso sleepRegex.IsMatch(lines(i + 2)) Then
                                        sleepStatus = "true"
                                    End If

                                    delOutput &= Environment.NewLine & $"Sleep: {sleepStatus}"
                                End If

                                logWriter.WriteLine(delOutput)
                            End If
                        Next
                    End If

                    ' Extract and log ROBOCOPY commands
                    Dim robocopyMatches As MatchCollection = robocopyRegex.Matches(line)
                    If robocopyMatches.Count > 0 Then
                        For Each match As Match In robocopyMatches
                            If match.Success Then
                                Dim sourcePath As String = match.Groups(1).Value.Trim()
                                Dim destinationPath As String = match.Groups(2).Value.Trim()
                                Dim options As String = match.Groups(3).Value.Trim()

                                Dim parameters As String = String.Join(", ", ExtractParameters(options))

                                Dim robocopyOutput As String = $"ROBOCOPY Command ({lineNumber})" & Environment.NewLine &
                                                                $"Source Path: {sourcePath}" & Environment.NewLine &
                                                                $"Destination Path: {destinationPath}" & Environment.NewLine &
                                                                $"Parameters: {parameters}"

                                logWriter.WriteLine(robocopyOutput)
                            End If
                        Next
                    End If

                    ' Extract and log RENAME commands
                    Dim renameMatches As MatchCollection = renameRegex.Matches(line)
                    If renameMatches.Count > 0 Then
                        For Each match As Match In renameMatches
                            If match.Success Then
                                Dim oldName As String = match.Groups(1).Value.Trim()
                                Dim newName As String = match.Groups(2).Value.Trim()

                                Dim renameOutput As String = $"RENAME Command ({lineNumber})" & Environment.NewLine &
                                                              $"Old Name: {oldName}" & Environment.NewLine &
                                                              $"New Name: {newName}"

                                logWriter.WriteLine(renameOutput)
                            End If
                        Next
                    End If
                Next

                ' Notify user that logging is complete
                WriteOutput(outputFilePath, "Paths and parameters have been logged to CommandReport.log")

            End Using
        Catch ex As Exception
            WriteOutput(outputFilePath, "Error during command validation: " & ex.Message)
        End Try
        WriteOutput("", outputFilePath)
        WriteOutput("###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   Command Analysis Completed" + "    ###################################", outputFilePath)
    End Sub

    ' Function to extract parameters from a command string
    Private Function ExtractParameters(commandString As String) As List(Of String)
        Dim parameters As New List(Of String)()
        Dim parameterPattern As String = "/[^""\s]+"
        Dim parameterRegex As New Regex(parameterPattern, RegexOptions.Compiled)

        Dim matches As MatchCollection = parameterRegex.Matches(commandString)
        For Each match As Match In matches
            parameters.Add(match.Value)
        Next

        Return parameters
    End Function


End Class


