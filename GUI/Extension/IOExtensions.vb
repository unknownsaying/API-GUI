Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text

Namespace DeploymentAutomationGUI.Extensions
    
    ''' IO扩展方法
    
    Public Module IOExtensions
        
        
        ''' 安全地读取文件的所有文本，如果文件不存在则返回空字符串
        
        <Extension>
        Public Function ReadAllTextSafe(filePath As String, Optional encoding As Encoding = Nothing) As String
            If Not File.Exists(filePath) Then
                Return String.Empty
            End If
            
            Try
                If encoding Is Nothing Then
                    Return File.ReadAllText(filePath)
                Else
                    Return File.ReadAllText(filePath, encoding)
                End If
            Catch
                Return String.Empty
            End Try
        End Function
        
        
        ''' 安全地读取文件的所有行，如果文件不存在则返回空集合
        
        <Extension>
        Public Function ReadAllLinesSafe(filePath As String, Optional encoding As Encoding = Nothing) As IEnumerable(Of String)
            If Not File.Exists(filePath) Then
                Return Enumerable.Empty(Of String)()
            End If
            
            Try
                If encoding Is Nothing Then
                    Return File.ReadAllLines(filePath)
                Else
                    Return File.ReadAllLines(filePath, encoding)
                End If
            Catch
                Return Enumerable.Empty(Of String)()
            End Try
        End Function
        
        
        ''' 安全地将文本写入文件，自动创建目录
        
        <Extension>
        Public Sub WriteAllTextSafe(filePath As String, text As String, Optional encoding As Encoding = Nothing)
            Try
                Dim directory = Path.GetDirectoryName(filePath)
                If Not String.IsNullOrEmpty(directory) AndAlso Not Directory.Exists(directory) Then
                    Directory.CreateDirectory(directory)
                End If
                
                If encoding Is Nothing Then
                    File.WriteAllText(filePath, text)
                Else
                    File.WriteAllText(filePath, text, encoding)
                End If
            Catch ex As Exception
                Throw New IOException($"Failed to write to file: {filePath}", ex)
            End Try
        End Sub
        
        
        ''' 安全地将多行文本写入文件，自动创建目录
        
        <Extension>
        Public Sub WriteAllLinesSafe(filePath As String, lines As IEnumerable(Of String), Optional encoding As Encoding = Nothing)
            Try
                Dim directory = Path.GetDirectoryName(filePath)
                If Not String.IsNullOrEmpty(directory) AndAlso Not Directory.Exists(directory) Then
                    Directory.CreateDirectory(directory)
                End If
                
                If encoding Is Nothing Then
                    File.WriteAllLines(filePath, lines)
                Else
                    File.WriteAllLines(filePath, lines, encoding)
                End If
            Catch ex As Exception
                Throw New IOException($"Failed to write lines to file: {filePath}", ex)
            End Try
        End Sub
        
        
        ''' 安全地将文本追加到文件，自动创建目录
        
        <Extension>
        Public Sub AppendAllTextSafe(filePath As String, text As String, Optional encoding As Encoding = Nothing)
            Try
                Dim directory = Path.GetDirectoryName(filePath)
                If Not String.IsNullOrEmpty(directory) AndAlso Not Directory.Exists(directory) Then
                    Directory.CreateDirectory(directory)
                End If
                
                If encoding Is Nothing Then
                    File.AppendAllText(filePath, text)
                Else
                    File.AppendAllText(filePath, text, encoding)
                End If
            Catch ex As Exception
                Throw New IOException($"Failed to append to file: {filePath}", ex)
            End Try
        End Sub
        
        
        ''' 获取文件的大小（人类可读格式）
        
        <Extension>
        Public Function GetHumanReadableSize(filePath As String) As String
            If Not File.Exists(filePath) Then
                Return "0 B"
            End If
            
            Dim fileInfo = New FileInfo(filePath)
            Return fileInfo.Length.ToHumanReadableSize()
        End Function
        
        
        ''' 获取文件的扩展名（不包含点）
        
        <Extension>
        Public Function GetExtensionWithoutDot(filePath As String) As String
            Dim extension = Path.GetExtension(filePath)
            Return If(String.IsNullOrEmpty(extension), String.Empty, extension.TrimStart("."c))
        End Function
        
        
        ''' 获取文件的MIME类型
        
        <Extension>
        Public Function GetMimeType(filePath As String) As String
            Dim extension = Path.GetExtension(filePath).ToLower()
            
            Select Case extension
                Case ".txt", ".log", ".ini", ".cfg"
                    Return "text/plain"
                Case ".html", ".htm"
                    Return "text/html"
                Case ".css"
                    Return "text/css"
                Case ".js"
                    Return "application/javascript"
                Case ".json"
                    Return "application/json"
                Case ".xml"
                    Return "application/xml"
                Case ".pdf"
                    Return "application/pdf"
                Case ".zip"
                    Return "application/zip"
                Case ".rar"
                    Return "application/x-rar-compressed"
                Case ".7z"
                    Return "application/x-7z-compressed"
                Case ".tar"
                    Return "application/x-tar"
                Case ".gz"
                    Return "application/gzip"
                Case ".jpg", ".jpeg"
                    Return "image/jpeg"
                Case ".png"
                    Return "image/png"
                Case ".gif"
                    Return "image/gif"
                Case ".bmp"
                    Return "image/bmp"
                Case ".svg"
                    Return "image/svg+xml"
                Case ".ico"
                    Return "image/x-icon"
                Case ".mp3"
                    Return "audio/mpeg"
                Case ".wav"
                    Return "audio/wav"
                Case ".mp4"
                    Return "video/mp4"
                Case ".avi"
                    Return "video/x-msvideo"
                Case ".mov"
                    Return "video/quicktime"
                Case ".doc", ".docx"
                    Return "application/msword"
                Case ".xls", ".xlsx"
                    Return "application/vnd.ms-excel"
                Case ".ppt", ".pptx"
                    Return "application/vnd.ms-powerpoint"
                Case ".exe"
                    Return "application/octet-stream"
                Case ".dll"
                    Return "application/x-msdownload"
                Case Else
                    Return "application/octet-stream"
            End Select
        End Function
        
        
        ''' 检查文件是否是文本文件
        
        <Extension>
        Public Function IsTextFile(filePath As String) As Boolean
            Dim mimeType = GetMimeType(filePath)
            Return mimeType.StartsWith("text/") OrElse 
                   mimeType = "application/json" OrElse 
                   mimeType = "application/xml" OrElse 
                   mimeType = "application/javascript"
        End Function
        
        
        ''' 检查文件是否是图片文件
        
        <Extension>
        Public Function IsImageFile(filePath As String) As Boolean
            Dim mimeType = GetMimeType(filePath)
            Return mimeType.StartsWith("image/")
        End Function
        
        
        ''' 检查文件是否是视频文件
        
        <Extension>
        Public Function IsVideoFile(filePath As String) As Boolean
            Dim mimeType = GetMimeType(filePath)
            Return mimeType.StartsWith("video/")
        End Function
        
        
        ''' 检查文件是否是音频文件
        
        <Extension>
        Public Function IsAudioFile(filePath As String) As Boolean
            Dim mimeType = GetMimeType(filePath)
            Return mimeType.StartsWith("audio/")
        End Function
        
        
        ''' 获取文件的编码
        
        <Extension>
        Public Function GetFileEncoding(filePath As String) As Encoding
            If Not File.Exists(filePath) Then
                Return Encoding.Default
            End If
            
            Try
                Using reader = New StreamReader(filePath, Encoding.Default, True)
                    reader.Peek()
                    Return reader.CurrentEncoding
                End Using
            Catch
                Return Encoding.Default
            End Try
        End Function
        
        
        ''' 计算文件的MD5哈希值
        
        <Extension>
        Public Function ComputeFileMD5(filePath As String) As String
            If Not File.Exists(filePath) Then
                Return String.Empty
            End If
            
            Using md5 = System.Security.Cryptography.MD5.Create()
                Using stream = File.OpenRead(filePath)
                    Dim hashBytes = md5.ComputeHash(stream)
                    
                    Dim builder = New StringBuilder()
                    For Each b In hashBytes
                        builder.Append(b.ToString("x2"))
                    Next
                    
                    Return builder.ToString()
                End Using
            End Using
        End Function
        
        
        ''' 计算文件的SHA256哈希值
        
        <Extension>
        Public Function ComputeFileSHA256(filePath As String) As String
            If Not File.Exists(filePath) Then
                Return String.Empty
            End If
            
            Using sha256 = System.Security.Cryptography.SHA256.Create()
                Using stream = File.OpenRead(filePath)
                    Dim hashBytes = sha256.ComputeHash(stream)
                    
                    Dim builder = New StringBuilder()
                    For Each b In hashBytes
                        builder.Append(b.ToString("x2"))
                    Next
                    
                    Return builder.ToString()
                End Using
            End Using
        End Function
        
        
        ''' 安全地复制文件，覆盖时备份原文件
        
        <Extension>
        Public Sub CopyFileSafe(sourcePath As String, destinationPath As String, Optional backup As Boolean = True)
            If Not File.Exists(sourcePath) Then
                Throw New FileNotFoundException($"Source file not found: {sourcePath}")
            End If
            
            Dim destinationDirectory = Path.GetDirectoryName(destinationPath)
            If Not String.IsNullOrEmpty(destinationDirectory) AndAlso Not Directory.Exists(destinationDirectory) Then
                Directory.CreateDirectory(destinationDirectory)
            End If
            
            ' 如果目标文件已存在且需要备份
            If File.Exists(destinationPath) AndAlso backup Then
                Dim backupPath = $"{destinationPath}.backup_{DateTime.Now:yyyyMMddHHmmss}"
                File.Move(destinationPath, backupPath)
            End If
            
            File.Copy(sourcePath, destinationPath, True)
        End Sub
        
        
        ''' 安全地移动文件，覆盖时备份原文件
        
        <Extension>
        Public Sub MoveFileSafe(sourcePath As String, destinationPath As String, Optional backup As Boolean = True)
            If Not File.Exists(sourcePath) Then
                Throw New FileNotFoundException($"Source file not found: {sourcePath}")
            End If
            
            Dim destinationDirectory = Path.GetDirectoryName(destinationPath)
            If Not String.IsNullOrEmpty(destinationDirectory) AndAlso Not Directory.Exists(destinationDirectory) Then
                Directory.CreateDirectory(destinationDirectory)
            End If
            
            ' 如果目标文件已存在且需要备份
            If File.Exists(destinationPath) AndAlso backup Then
                Dim backupPath = $"{destinationPath}.backup_{DateTime.Now:yyyyMMddHHmmss}"
                File.Move(destinationPath, backupPath)
            End If
            
            File.Move(sourcePath, destinationPath)
        End Sub
        
        
        ''' 安全地删除文件，如果文件不存在则不执行
        
        <Extension>
        Public Sub DeleteFileSafe(filePath As String)
            If File.Exists(filePath) Then
                Try
                    File.Delete(filePath)
                Catch
                    ' 忽略删除错误
                End Try
            End If
        End Sub
        
        
        ''' 安全地删除目录及其所有内容
        
        <Extension>
        Public Sub DeleteDirectorySafe(directoryPath As String, Optional recursive As Boolean = True)
            If Directory.Exists(directoryPath) Then
                Try
                    Directory.Delete(directoryPath, recursive)
                Catch
                    ' 忽略删除错误
                End Try
            End If
        End Sub
        
        
        ''' 获取目录中所有文件的路径（包含子目录）
        
        <Extension>
        Public Function GetAllFiles(directoryPath As String, Optional searchPattern As String = "*.*", 
                                   Optional searchOption As SearchOption = SearchOption.AllDirectories) As IEnumerable(Of String)
            If Not Directory.Exists(directoryPath) Then
                Return Enumerable.Empty(Of String)()
            End If
            
            Try
                Return Directory.EnumerateFiles(directoryPath, searchPattern, searchOption)
            Catch
                Return Enumerable.Empty(Of String)()
            End Try
        End Function
        
        
        ''' 获取目录中所有子目录的路径（包含子目录）
        
        <Extension>
        Public Function GetAllDirectories(directoryPath As String, Optional searchPattern As String = "*", 
                                         Optional searchOption As SearchOption = SearchOption.AllDirectories) As IEnumerable(Of String)
            If Not Directory.Exists(directoryPath) Then
                Return Enumerable.Empty(Of String)()
            End If
            
            Try
                Return Directory.EnumerateDirectories(directoryPath, searchPattern, searchOption)
            Catch
                Return Enumerable.Empty(Of String)()
            End Try
        End Function
        
        
        ''' 计算目录的大小（字节）
        
        <Extension>
        Public Function GetDirectorySize(directoryPath As String, Optional includeSubdirectories As Boolean = True) As Long
            If Not Directory.Exists(directoryPath) Then
                Return 0
            End If
            
            Dim searchOption = If(includeSubdirectories, SearchOption.AllDirectories, SearchOption.TopDirectoryOnly)
            
            Try
                Return GetAllFiles(directoryPath, "*.*", searchOption) _
                       .Select(Function(file) New FileInfo(file).Length) _
                       .Sum()
            Catch
                Return 0
            End Try
        End Function
        
        
        ''' 获取目录的人类可读大小
        
        <Extension>
        Public Function GetDirectoryHumanReadableSize(directoryPath As String, Optional includeSubdirectories As Boolean = True) As String
            Dim size = GetDirectorySize(directoryPath, includeSubdirectories)
            Return size.ToHumanReadableSize()
        End Function
        
        
        ''' 清理目录中的空子目录
        
        <Extension>
        Public Sub CleanEmptyDirectories(directoryPath As String, Optional recursive As Boolean = True)
            If Not Directory.Exists(directoryPath) Then
                Return
            End If
            
            Dim subdirectories = GetAllDirectories(directoryPath, "*", SearchOption.AllDirectories)
            For Each subdirectory In subdirectories.Reverse()
                Try
                    If Directory.GetFiles(subdirectory).Length = 0 AndAlso 
                       Directory.GetDirectories(subdirectory).Length = 0 Then
                        Directory.Delete(subdirectory)
                    End If
                Catch
                    ' 忽略删除错误
                End Try
            Next
        End Sub
        
        
        ''' 创建临时文件
        
        <Extension>
        Public Function CreateTempFile(Optional extension As String = ".tmp", Optional content As String = Nothing) As String
            Dim tempPath = Path.GetTempPath()
            Dim fileName = $"{Guid.NewGuid()}{extension}"
            Dim filePath = Path.Combine(tempPath, fileName)
            
            If content IsNot Nothing Then
                WriteAllTextSafe(filePath, content)
            Else
                File.Create(filePath).Close()
            End If
            
            Return filePath
        End Function
        
        
        ''' 创建临时目录
        
        <Extension>
        Public Function CreateTempDirectory(Optional prefix As String = "Temp") As String
            Dim tempPath = Path.GetTempPath()
            Dim directoryName = $"{prefix}_{Guid.NewGuid()}"
            Dim directoryPath = Path.Combine(tempPath, directoryName)
            
            Directory.CreateDirectory(directoryPath)
            Return directoryPath
        End Function
    End Module
    
    
    ''' 数值扩展方法（用于文件大小格式化）
    
    Public Module NumericExtensions
        
        
        ''' 将字节大小转换为人类可读格式
        
        <Extension>
        Public Function ToHumanReadableSize(size As Long) As String
            Dim units = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}
            
            If size = 0 Then
                Return "0 B"
            End If
            
            Dim i As Integer = 0
            Dim doubleSize As Double = size
            
            While doubleSize >= 1024 AndAlso i < units.Length - 1
                doubleSize /= 1024
                i += 1
            End While
            
            Return $"{doubleSize:0.##} {units(i)}"
        End Function
    End Module
End Namespace