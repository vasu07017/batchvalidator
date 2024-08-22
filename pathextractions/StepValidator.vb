Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Collections.Generic
Imports System.Reflection.Metadata

Public Class StepValidator
    Sub StepValidate()



        ' Initialize log writer
        Dim logMessage As String = ""
        WriteOutput("", outputFilePath)
        WriteOutput("###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   Step Validation Started" + "    ###################################", outputFilePath)

        ' Verify that the batch file exists
        If Not File.Exists(batchFilePath) Then
            logMessage = $"Batch file not found: {batchFilePath}"
            WriteOutput(logMessage, outputFilePath)
            Return
        End If

        ' Read the batch file content
        Dim scriptLines() As String
        Try
            scriptLines = File.ReadAllLines(batchFilePath)
        Catch ex As Exception
            logMessage = "Error reading the batch file: " & ex.Message
            WriteOutput(logMessage, outputFilePath)
            Return
        End Try

        ' Initialize output string
        Dim stepsOutput As New Text.StringBuilder()
        Dim stepDictionary As New SortedDictionary(Of Integer, Tuple(Of String, Integer))()

        ' Define regex pattern to match step comments
        Dim stepPattern As String = "\*+ STEP(\d+)_([A-Z_ ]+)\*+"

        ' Process each line to extract step numbers, names, and line numbers
        For lineIndex As Integer = 0 To scriptLines.Length - 1
            Dim line As String = scriptLines(lineIndex)
            Dim match As Match = Regex.Match(line, stepPattern)
            If match.Success Then
                Dim stepNumber As Integer
                Dim stepName As String = match.Groups(2).Value.Trim()
                If Integer.TryParse(match.Groups(1).Value, stepNumber) Then
                    stepDictionary(stepNumber) = Tuple.Create(stepName, lineIndex + 1)
                End If
            End If
        Next

        ' Initialize variables to track missing steps and unordered steps
        Dim missingSteps As New List(Of Integer)()
        Dim unorderedSteps As New List(Of Integer)()
        Dim previousLineNumber As Integer = -1
        Dim sequenceCorrect As Boolean = True

        ' Check for missing steps and unordered steps
        For Each stepNumber In stepDictionary.Keys
            Dim stepInfo As Tuple(Of String, Integer) = stepDictionary(stepNumber)
            Dim stepName As String = stepInfo.Item1
            Dim lineNumber As Integer = stepInfo.Item2

            ' Check if the line numbers are in increasing order
            If previousLineNumber <> -1 AndAlso lineNumber < previousLineNumber Then
                sequenceCorrect = False
                unorderedSteps.Add(stepNumber)
            End If

            ' Check for missing steps in gaps greater than 10
            If previousLineNumber <> -1 Then
                Dim previousStepNumber As Integer = stepDictionary.Keys.First(Function(k) stepDictionary(k).Item2 = previousLineNumber)
                If stepNumber - previousStepNumber > 10 Then
                    For missingStep As Integer = previousStepNumber + 10 To stepNumber - 10 Step 10
                        If Not stepDictionary.ContainsKey(missingStep) Then
                            missingSteps.Add(missingStep)
                        End If
                    Next
                End If
            End If

            previousLineNumber = lineNumber
        Next

        ' Write the result to steps.txt
        Try
            ' Ensure the directory exists
            Dim outputDirectory As String = Path.GetDirectoryName(outputFilePath)
            If Not Directory.Exists(outputDirectory) Then
                Directory.CreateDirectory(outputDirectory)
            End If

            ' Write to the file
            Using writer As New StreamWriter(outputFilePath, append:=True)
                ' Write all steps with their line numbers
                For Each stepNumber In stepDictionary.Keys
                    Dim stepInfo As Tuple(Of String, Integer) = stepDictionary(stepNumber)
                    Dim stepName As String = stepInfo.Item1
                    Dim lineNumber As Integer = stepInfo.Item2
                    writer.WriteLine($"STEP({stepNumber}): {stepName} (line {lineNumber})")
                Next

                ' Write sequence correctness
                writer.WriteLine($"⇒Step sequence correct: {sequenceCorrect.ToString().ToLower()}")

                ' Write missing steps if any
                If missingSteps.Count > 0 Then
                    writer.WriteLine("⇒ Missing steps: " & String.Join(", ", missingSteps))
                End If

                ' Write unordered steps if any
                If unorderedSteps.Count > 0 Then
                    writer.WriteLine("⇒ Unordered steps: " & String.Join(", ", unorderedSteps))
                End If
            End Using

        Catch ex As Exception
            logMessage = "Error writing to the output file: " & ex.Message
            WriteOutput(logMessage, outputFilePath)
        End Try
        WriteOutput("###################################   " + $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}   Step Validation Completed" + "    ###################################", outputFilePath)
        WriteOutput("", outputFilePath)

    End Sub


End Class

