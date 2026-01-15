Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks

Namespace DeploymentAutomationGUI.Core
    
    ''' 提供各种实用方法的类
    
    Public Class Utility
        
        ''' 私有构造函数，防止实例化
        
        Private Sub New()
        End Sub
        
        
        ''' 子程序示例：清空临时目录
        
        ''' <param name="directoryPath">目录路径</param>
        ''' <param name="daysOld">删除多少天前的文件（默认30天）</param>
        Public Shared Sub CleanupTempDirectory(directoryPath As String, Optional daysOld As Integer = 30)
            If Not Directory.Exists(directoryPath) Then
                Return
            End If
            
            Try
                Dim cutoffDate = DateTime.Now.AddDays(-daysOld)
                
                ' 删除旧文件
                For Each file In Directory.GetFiles(directoryPath)
                    Try
                        Dim fileInfo = New FileInfo(file)
                        If fileInfo.LastWriteTime < cutoffDate Then
                            fileInfo.Delete()
                        End If
                    Catch
                        ' 忽略删除错误
                    End Try
                Next
                
                ' 删除空目录
                For Each dir In Directory.GetDirectories(directoryPath)
                    Try
                        Dim dirInfo = New DirectoryInfo(dir)
                        If dirInfo.GetFiles().Length = 0 AndAlso dirInfo.GetDirectories().Length = 0 Then
                            dirInfo.Delete()
                        End If
                    Catch
                        ' 忽略删除错误
                    End Try
                Next
                
                Debug.WriteLine($"Cleaned up temporary directory: {directoryPath}")
            Catch ex As Exception
                Debug.WriteLine($"Error cleaning up directory: {ex.Message}")
            End Try
        End Sub
        
        
        ''' 函数示例：生成唯一的文件名
        
        ''' <param name="originalName">原始文件名</param>
        ''' <param name="directory">目标目录</param>
        ''' <returns>唯一的文件名</returns>
        Public Shared Function GenerateUniqueFileName(originalName As String, directory As String) As String
            If String.IsNullOrEmpty(originalName) Then
                Throw New ArgumentException("Original name cannot be null or empty", NameOf(originalName))
            End If
            
            If Not Directory.Exists(directory) Then
                Directory.CreateDirectory(directory)
            End If
            
            Dim fileName = Path.GetFileNameWithoutExtension(originalName)
            Dim extension = Path.GetExtension(originalName)
            Dim counter = 1
            Dim uniqueName = originalName
            
            While File.Exists(Path.Combine(directory, uniqueName))
                uniqueName = $"{fileName}_{counter}{extension}"
                counter += 1
            End While
            
            Return uniqueName
        End Function
        
        
        ''' 函数示例：验证电子邮件地址格式
        
        ''' <param name="email">电子邮件地址</param>
        ''' <returns>是否有效</returns>
        Public Shared Function IsValidEmail(email As String) As Boolean
            If String.IsNullOrWhiteSpace(email) Then
                Return False
            End If
            
            Try
                Dim addr = New System.Net.Mail.MailAddress(email)
                Return addr.Address = email
            Catch
                Return False
            End Try
        End Function
        
        
        ''' 函数示例：计算字符串的相似度（Levenshtein距离）
        
        ''' <param name="s1">字符串1</param>
        ''' <param name="s2">字符串2</param>
        ''' <returns>相似度百分比（0-100）</returns>
        Public Shared Function CalculateStringSimilarity(s1 As String, s2 As String) As Double
            If String.IsNullOrEmpty(s1) OrElse String.IsNullOrEmpty(s2) Then
                Return If(s1 = s2, 100, 0)
            End If
            
            Dim n = s1.Length
            Dim m = s2.Length
            
            If n = 0 Then
                Return If(m = 0, 100, 0)
            End If
            
            If m = 0 Then
                Return 0
            End If
            
            Dim distance = New Integer(n, m) {}
            
            For i = 0 To n
                distance(i, 0) = i
            Next
            
            For j = 0 To m
                distance(0, j) = j
            Next
            
            For i = 1 To n
                For j = 1 To m
                    Dim cost = If(s1(i - 1) = s2(j - 1), 0, 1)
                    
                    distance(i, j) = Math.Min(
                        Math.Min(
                            distance(i - 1, j) + 1,     ' 删除
                            distance(i, j - 1) + 1),    ' 插入
                        distance(i - 1, j - 1) + cost)  ' 替换
                Next
            Next
            
            Dim maxLength = Math.Max(n, m)
            Dim similarity = (1.0 - distance(n, m) / maxLength) * 100
            
            Return Math.Max(0, Math.Min(100, similarity))
        End Function
        
        
        ''' 子程序示例：在资源管理器中打开目录
        
        ''' <param name="directoryPath">目录路径</param>
        Public Shared Sub OpenInExplorer(directoryPath As String)
            If Not Directory.Exists(directoryPath) Then
                Throw New DirectoryNotFoundException($"Directory not found: {directoryPath}")
            End If
            
            Process.Start("explorer.exe", directoryPath)
        End Sub
        
        
        ''' 子程序示例：在默认浏览器中打开URL
        
        ''' <param name="url">URL地址</param>
        Public Shared Sub OpenInBrowser(url As String)
            If String.IsNullOrWhiteSpace(url) Then
                Throw New ArgumentException("URL cannot be null or empty", NameOf(url))
            End If
            
            If Not url.StartsWith("http://") AndAlso Not url.StartsWith("https://") Then
                url = "http://" & url
            End If
            
            Process.Start(url)
        End Sub
        
        
        ''' 异步函数示例：延迟执行
        
        ''' <param name="milliseconds">延迟的毫秒数</param>
        ''' <param name="cancellationToken">取消令牌</param>
        Public Shared Async Function DelayAsync(milliseconds As Integer, 
                                               Optional cancellationToken As Threading.CancellationToken = Nothing) As Task
            If milliseconds <= 0 Then
                Return
            End If
            
            Await Task.Delay(milliseconds, cancellationToken)
        End Function
        
        
        ''' 函数示例：安全执行操作并返回结果
        
        ''' <typeparam name="T">返回类型</typeparam>
        ''' <param name="action">要执行的操作</param>
        ''' <param name="defaultValue">默认值</param>
        ''' <returns>操作结果或默认值</returns>
        Public Shared Function ExecuteSafely(Of T)(action As Func(Of T), 
                                                  Optional defaultValue As T = Nothing) As T
            Try
                Return action()
            Catch ex As Exception
                Debug.WriteLine($"Error executing action: {ex.Message}")
                Return defaultValue
            End Try
        End Function
        
        
        ''' 子程序示例：安全执行操作
        
        ''' <param name="action">要执行的操作</param>
        Public Shared Sub ExecuteSafely(action As Action)
            Try
                action()
            Catch ex As Exception
                Debug.WriteLine($"Error executing action: {ex.Message}")
            End Try
        End Sub
        
        
        ''' 函数示例：格式化字节大小
        
        ''' <param name="bytes">字节数</param>
        ''' <returns>格式化后的字符串</returns>
        Public Shared Function FormatBytes(bytes As Long) As String
            Dim units = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}
            
            If bytes <= 0 Then
                Return "0 B"
            End If
            
            Dim i As Integer = 0
            Dim size As Double = bytes
            
            While size >= 1024 AndAlso i < units.Length - 1
                size /= 1024
                i += 1
            End While
            
            Return $"{size:0.##} {units(i)}"
        End Function
        
        
        ''' 函数示例：生成随机字符串
        
        ''' <param name="length">字符串长度</param>
        ''' <param name="includeNumbers">是否包含数字</param>
        ''' <param name="includeSpecialChars">是否包含特殊字符</param>
        ''' <returns>随机字符串</returns>
        Public Shared Function GenerateRandomString(length As Integer, 
                                                   Optional includeNumbers As Boolean = True,
                                                   Optional includeSpecialChars As Boolean = False) As String
            If length <= 0 Then
                Return String.Empty
            End If
            
            Dim chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
            
            If includeNumbers Then
                chars &= "0123456789"
            End If
            
            If includeSpecialChars Then
                chars &= "!@#$%^&*()_+-=[]{}|;:,.<>?"
            End If
            
            Dim random = New Random()
            Dim result = New StringBuilder(length)
            
            For i As Integer = 0 To length - 1
                result.Append(chars(random.Next(chars.Length)))
            Next
            
            Return result.ToString()
        End Function
    End Class
End Namespace