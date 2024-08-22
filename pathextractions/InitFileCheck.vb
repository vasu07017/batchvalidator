Imports System.IO
Imports System.Text.RegularExpressions

Public Class InitFileCheck
    Sub ValidateInitFile()
        WriteOutput("###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   InitFile Check Started" + "    ###################################", outputFilePath)
        ' Verify that the batch file exists
        If Not File.Exists(batchFilePath) Then
            WriteOutput("Batch file not found: " & batchFilePath, outputFilePath)
            Return
        End If

        ' Read the batch file content
        Dim batchFileContent As String
        Try
            batchFileContent = File.ReadAllText(batchFilePath)
        Catch ex As Exception
            WriteOutput("Error reading the batch file: " & ex.Message, outputFilePath)
            Return
        End Try

        ' Extract the init file name from the batch file
        Dim initFileName As String = ExtractInitFileName(batchFileContent)
        If String.IsNullOrEmpty(initFileName) Then
            WriteOutput("Init file name not found in batch file.", outputFilePath)
            Return
        End If

        ' Specify the init file path
        Dim initFilePath As String = Path.Combine("C:\Deepak\Visa\Vasu_2024\pathextractions\pathextractions\SourceBatch", initFileName)

        ' Verify that the init file exists
        If Not File.Exists(initFilePath) Then
            WriteOutput("Init file not found: " & initFilePath, outputFilePath)
            Return
        End If

        ' Extract the paths from the STEP10_INIT_PARAMETER CHECK section of the batch file
        Dim batchPaths As List(Of String) = ExtractBatchPaths(batchFileContent)

        ' Extract the paths from the init file and any nested init files
        Dim initPaths As Dictionary(Of String, Boolean) = ExtractInitPathsRecursive(initFilePath)

        ' Compare the paths and prepare the output
        Dim missingPaths As New List(Of String)
        For Each path In batchPaths
            If Not initPaths.ContainsKey(path) Then
                missingPaths.Add(path)
            End If
        Next

        ' Write the results to the output file
        Try
            Using writer As New StreamWriter(outputFilePath, True)
                writer.WriteLine("⇒ Paths checked from STEP10_INIT_PARAMETER CHECK:")
                For Each path In batchPaths
                    writer.WriteLine(path)
                Next

                writer.WriteLine()
                writer.WriteLine("⇒ Paths found in init file and nested init files:")
                For Each path In initPaths.Keys
                    writer.WriteLine(path)
                Next

                writer.WriteLine()
                If missingPaths.Count = 0 Then
                    writer.WriteLine("⇒ All paths OK")
                Else
                    writer.WriteLine("⇒ Paths missing in init file:")
                    For Each missingPath In missingPaths
                        writer.WriteLine(missingPath)
                    Next
                End If
            End Using
        Catch ex As Exception
            WriteOutput("Error writing to the output file: " & ex.Message, outputFilePath)
        End Try
        WriteOutput("###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   InitFile Check Completed" + "    ###################################", outputFilePath)
        WriteOutput("", outputFilePath)
    End Sub

    ' Function to extract the init file name from the batch file
    Function ExtractInitFileName(batchContent As String) As String
        Dim initFileName As String = ""
        Dim initFilePattern As String = "(?<!@)\bcall\s+([^\s]+\.bat)"

        Dim matches As MatchCollection = Regex.Matches(batchContent, initFilePattern, RegexOptions.IgnoreCase)
        If matches.Count > 0 Then
            ' Taking the first match assuming the init file is the first called batch file without '@'
            initFileName = matches(0).Groups(1).Value
        End If

        Return initFileName
    End Function

    ' Function to extract paths from the STEP10_INIT_PARAMETER CHECK section
    Function ExtractBatchPaths(batchContent As String) As List(Of String)
        Dim paths As New List(Of String)
        Dim stepStartPattern As String = ":\*+\s*STEP10_INIT_PARAMETER CHECK\s*\*+:"
        Dim pathPattern As String = "%([^%]+)%"

        Dim regex As New Regex(stepStartPattern, RegexOptions.IgnoreCase Or RegexOptions.Multiline)
        Dim match As Match = regex.Match(batchContent)

        If match.Success Then
            Dim startPosition As Integer = match.Index + match.Length
            Dim endPosition As Integer = batchContent.IndexOf(":", startPosition)
            Dim section As String

            If endPosition >= 0 Then
                section = batchContent.Substring(startPosition, endPosition - startPosition)
            Else
                section = batchContent.Substring(startPosition)
            End If

            Dim pathMatches As MatchCollection = Regex.Matches(section, pathPattern)
            For Each pathMatch As Match In pathMatches
                paths.Add(pathMatch.Groups(1).Value)
            Next
        End If

        Return paths
    End Function

    ' Function to recursively extract paths from init file and any nested init files
    Function ExtractInitPathsRecursive(initFilePath As String) As Dictionary(Of String, Boolean)
        Dim allPaths As New Dictionary(Of String, Boolean)

        ' Queue to process each init file, starting with the main init file
        Dim initFilesQueue As New Queue(Of String)
        initFilesQueue.Enqueue(initFilePath)

        While initFilesQueue.Count > 0
            Dim currentInitFile As String = initFilesQueue.Dequeue()

            Dim initContent As String
            Try
                initContent = File.ReadAllText(currentInitFile)
            Catch ex As Exception
                Continue While
            End Try

            ' Extract paths from the current init file
            Dim pathPattern As String = "SET\s+([^=\s]+)="
            Dim regex As New Regex(pathPattern, RegexOptions.IgnoreCase)
            Dim matches As MatchCollection = regex.Matches(initContent)

            For Each match As Match In matches
                If Not allPaths.ContainsKey(match.Groups(1).Value) Then
                    allPaths(match.Groups(1).Value) = True
                End If
            Next

            ' Find any nested init files to process
            Dim nestedInitFilePattern As String = "(?<!@)\bcall\s+([^\s]+\.bat)"
            Dim nestedMatches As MatchCollection = Regex.Matches(initContent, nestedInitFilePattern, RegexOptions.IgnoreCase)

            For Each nestedMatch As Match In nestedMatches
                Dim nestedInitFilePath As String = Path.Combine(Path.GetDirectoryName(currentInitFile), nestedMatch.Groups(1).Value)
                If File.Exists(nestedInitFilePath) Then
                    initFilesQueue.Enqueue(nestedInitFilePath)
                End If
            Next
        End While

        Return allPaths
    End Function

End Class
