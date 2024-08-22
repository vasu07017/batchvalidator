Imports System.IO
Imports System.Reflection.Metadata
Imports System.Text.RegularExpressions

Public Class PathValidator


    Sub PathValidate()

        ' Create or open the log file for writing
        Using logWriter As New StreamWriter(outputFilePath, append:=True)
            logWriter.WriteLine("")
            logWriter.WriteLine("###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   Path Validation Started" + "    ###################################")
            ' Verify that the batch file exists
            If Not File.Exists(batchFilePath) Then
                WriteError(logWriter, $"Batch file not found: {batchFilePath}")
                Return
            End If

            ' Read the batch file content
            Dim scriptContent As String
            Try
                scriptContent = File.ReadAllText(batchFilePath)
            Catch ex As Exception
                WriteError(logWriter, "Error reading the batch file: " & ex.Message)
                Return
            End Try

            ' Define the regex pattern to match paths enclosed in %
            Dim strictPattern As String = "%[^%""\s]+%(?:\\%[^%""\s]+%)*(?=[\s""]|$)"
            Dim lenientPattern As String = "%[^%]+%" ' A more lenient pattern that captures path-like structures

            Dim strictRegex As New Regex(strictPattern, RegexOptions.Compiled)
            Dim lenientRegex As New Regex(lenientPattern, RegexOptions.Compiled)

            ' Create lists to store paths
            Dim initPaths As New List(Of String)
            Dim otherPaths As New List(Of String)
            Dim pathLookalikes As New List(Of String)
            Dim allPaths As New Dictionary(Of String, String) ' Dictionary to store path with line number

            ' Track if we are in the "init parameter" section
            Dim isInInitParameterSection As Boolean = False

            ' Split content into lines for easier processing
            Dim lines() As String = scriptContent.Split(New String() {Environment.NewLine}, StringSplitOptions.None)

            For i As Integer = 0 To lines.Length - 1
                Dim line As String = lines(i)

                ' Check if we are in the "init parameter" section
                If line.Contains("STEP10_INIT_PARAMETER CHECK") Then
                    isInInitParameterSection = True
                ElseIf line.StartsWith("***********") Then
                    isInInitParameterSection = False
                End If

                ' Find strict matches
                Dim strictMatches As MatchCollection = strictRegex.Matches(line)
                For Each match As Match In strictMatches
                    Dim path As String = match.Value.Trim()
                    Dim pathWithLine As String = $"{path} (line {i + 1})"

                    If isInInitParameterSection Then
                        initPaths.Add(path)
                    Else
                        otherPaths.Add(pathWithLine)
                    End If

                    ' Add to allPaths dictionary
                    If Not allPaths.ContainsKey(path) Then
                        allPaths.Add(path, pathWithLine)
                    End If
                Next

                ' Find lenient matches
                Dim lenientMatches As MatchCollection = lenientRegex.Matches(line)
                For Each match As Match In lenientMatches
                    Dim path As String = match.Value.Trim()
                    If Not strictMatches.Cast(Of Match).Any(Function(m) m.Value = path) Then
                        ' If this path was not matched in the strict pattern, it is a lookalike
                        If Not allPaths.ContainsKey(path) Then
                            Dim failedCondition = CheckFailedConditions(path)
                            pathLookalikes.Add($"{path} (line {i + 1}): {failedCondition}")
                        End If
                    End If
                Next
            Next

            ' Identify paths that are used but not declared in the init parameter section
            Dim usedButNotInitPaths As New List(Of String)
            For Each path In otherPaths
                Dim pathKey = path.Split(" "c)(0).Trim() ' Extract the actual path part from the dictionary
                If Not initPaths.Contains(pathKey) Then
                    usedButNotInitPaths.Add(path)
                End If
            Next

            ' Write the paths to the output file with the specified format
            Try
                ' Ensure the directory exists
                Dim outputDirectory As String = Path.GetDirectoryName(outputFilePath)
                If Not Directory.Exists(outputDirectory) Then
                    Directory.CreateDirectory(outputDirectory)
                End If

                logWriter.WriteLine("⇒ Init Parameters Paths are listed below:")
                For Each path In initPaths
                    logWriter.WriteLine(path)
                Next
                logWriter.WriteLine()
                logWriter.WriteLine()

                logWriter.WriteLine("⇒ Other then Init Parameters Paths are listed below:")
                For Each path In otherPaths
                    logWriter.WriteLine(path)
                Next
                logWriter.WriteLine()
                logWriter.WriteLine()

                logWriter.WriteLine("⇒ Paths used but not declared in Init Parameters:")
                For Each path In usedButNotInitPaths
                    logWriter.WriteLine(path)
                Next
                logWriter.WriteLine()
                logWriter.WriteLine()

                logWriter.WriteLine("⇒ Ambiguous Path - Confirmation Required:")
                For Each lookalike In pathLookalikes
                    logWriter.WriteLine(lookalike)
                Next
                logWriter.WriteLine("")
                logWriter.WriteLine("###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   Path Validation Completed" + "    ###################################")
                logWriter.WriteLine("")

            Catch ex As Exception
                WriteError(logWriter, "Error writing to log file: " & ex.Message)
            End Try
        End Using

    End Sub

    ' Helper function to write errors to the log file
    Sub WriteError(logWriter As StreamWriter, message As String)
        logWriter.WriteLine(message)
    End Sub

    Function CheckFailedConditions(path As String) As String
        Dim failedConditions As New List(Of String)

        ' Condition: Starts and ends with %
        If Not (path.StartsWith("%") And path.EndsWith("%")) Then
            failedConditions.Add("Path does not start and end with %")
        End If

        ' Condition: No spaces or quotes within path
        If path.Contains(" ") OrElse path.Contains("""") Then
            failedConditions.Add("Path contains spaces or quotes")
        End If

        ' Condition: Only allowed internal % must be followed by \
        If path.Contains("%") Then
            Dim segments As String() = path.Split(New Char() {"%"c}, StringSplitOptions.RemoveEmptyEntries)
            For i As Integer = 1 To segments.Length - 2
                If Not segments(i).StartsWith("\") Then
                    failedConditions.Add("Internal % not followed by \")
                    Exit For
                End If
            Next
        End If

        Return String.Join(", ", failedConditions)
    End Function

End Class


