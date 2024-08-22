Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Collections.Generic
Imports System.Configuration
Imports Microsoft.Extensions.Configuration
Module BatchValidator
    ' Create configuration object
    Dim configuration = New ConfigurationBuilder() _
            .SetBasePath(Directory.GetCurrentDirectory()) _
            .AddJsonFile("appsettings.json", optional:=False, reloadOnChange:=True) _
            .Build()
    Public batchFilePath As String = configuration("BatchFilePath")
    Public outputFilePath As String = configuration("OutputFilePath")

    ' Helper function to write output to the log file
    Sub WriteOutput(message As String, outputPath As String)
        Try
            Using writer As New StreamWriter(outputPath, True)
                writer.WriteLine(message)
            End Using
        Catch ex As Exception
            ' Log error to a file or handle as needed; no console output
        End Try
    End Sub


    Sub Main()
        Dim fileName As String = Path.GetFileName(batchFilePath)
        WriteOutput($"batch name: {fileName}", outputFilePath)
        WriteOutput("", outputFilePath)
        Dim CheckStep As New StepValidator()
        CheckStep.StepValidate()


        Dim checkPath As New PathValidator()
        checkPath.PathValidate()



        Dim CheckDir As New DirCheck()
        CheckDir.CheckDir()

        Dim InitFileCheck As New InitFileCheck()
        InitFileCheck.ValidateInitFile()

        Dim CheckComand As New CommandValidators()
        CheckComand.ValidateCommand()

    End Sub
End Module
