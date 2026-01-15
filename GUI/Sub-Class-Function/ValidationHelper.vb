Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions

Namespace DeploymentAutomationGUI.Core
    
    ''' 验证助手类
    
    Public Class ValidationHelper
        
        ''' 验证结果
        
        Public Class ValidationResult
            
            ''' 是否有效
            
            Public Property IsValid As Boolean
            
            
            ''' 错误消息
            
            Public Property ErrorMessage As String
            
            
            ''' 字段名称
            
            Public Property FieldName As String
            
            
            ''' 创建成功的验证结果
            
            Public Shared Function Success() As ValidationResult
                Return New ValidationResult With {
                    .IsValid = True,
                    .ErrorMessage = String.Empty
                }
            End Function
            
            
            ''' 创建失败的验证结果
            
            Public Shared Function Failure(fieldName As String, errorMessage As String) As ValidationResult
                Return New ValidationResult With {
                    .IsValid = False,
                    .FieldName = fieldName,
                    .ErrorMessage = errorMessage
                }
            End Function
        End Class
        
        
        ''' 函数：验证电子邮件地址
        
        Public Shared Function ValidateEmail(email As String, fieldName As String) As ValidationResult
            If String.IsNullOrWhiteSpace(email) Then
                Return ValidationResult.Failure(fieldName, "Email cannot be empty")
            End If
            
            Try
                Dim mailAddress = New System.Net.Mail.MailAddress(email)
                If mailAddress.Address <> email Then
                    Return ValidationResult.Failure(fieldName, "Invalid email format")
                End If
                
                Return ValidationResult.Success()
            Catch
                Return ValidationResult.Failure(fieldName, "Invalid email format")
            End Try
        End Function
        
        
        ''' 函数：验证URL
        
        Public Shared Function ValidateUrl(url As String, fieldName As String) As ValidationResult
            If String.IsNullOrWhiteSpace(url) Then
                Return ValidationResult.Failure(fieldName, "URL cannot be empty")
            End If
            
            If Not Uri.TryCreate(url, UriKind.Absolute, Nothing) Then
                Return ValidationResult.Failure(fieldName, "Invalid URL format")
            End If
            
            Return ValidationResult.Success()
        End Function
        
        
        ''' 函数：验证电话号码
        
        Public Shared Function ValidatePhoneNumber(phoneNumber As String, fieldName As String) As ValidationResult
            If String.IsNullOrWhiteSpace(phoneNumber) Then
                Return ValidationResult.Failure(fieldName, "Phone number cannot be empty")
            End If
            
            ' 简单的电话号码验证（支持国际格式）
            Dim pattern = "^[\+]?[0-9\s\-\(\)\.]+$"
            If Not Regex.IsMatch(phoneNumber, pattern) Then
                Return ValidationResult.Failure(fieldName, "Invalid phone number format")
            End If
            
            Return ValidationResult.Success()
        End Function
        
        
        ''' 函数：验证密码强度
        
        Public Shared Function ValidatePassword(password As String, fieldName As String, 
                                               Optional minLength As Integer = 8,
                                               Optional requireUppercase As Boolean = True,
                                               Optional requireLowercase As Boolean = True,
                                               Optional requireNumbers As Boolean = True,
                                               Optional requireSpecial As Boolean = True) As ValidationResult
            
            If String.IsNullOrWhiteSpace(password) Then
                Return ValidationResult.Failure(fieldName, "Password cannot be empty")
            End If
            
            If password.Length < minLength Then
                Return ValidationResult.Failure(fieldName, $"Password must be at least {minLength} characters long")
            End If
            
            If requireUppercase AndAlso Not password.Any(AddressOf Char.IsUpper) Then
                Return ValidationResult.Failure(fieldName, "Password must contain at least one uppercase letter")
            End If
            
            If requireLowercase AndAlso Not password.Any(AddressOf Char.IsLower) Then
                Return ValidationResult.Failure(fieldName, "Password must contain at least one lowercase letter")
            End If
            
            If requireNumbers AndAlso Not password.Any(AddressOf Char.IsDigit) Then
                Return ValidationResult.Failure(fieldName, "Password must contain at least one number")
            End If
            
            If requireSpecial AndAlso Not password.Any(Function(c) Not Char.IsLetterOrDigit(c)) Then
                Return ValidationResult.Failure(fieldName, "Password must contain at least one special character")
            End If
            
            Return ValidationResult.Success()
        End Function
        
        
        ''' 函数：验证数字范围
        
        Public Shared Function ValidateNumberRange(number As Integer, fieldName As String, 
                                                  minValue As Integer, maxValue As Integer) As ValidationResult
            If number < minValue OrElse number > maxValue Then
                Return ValidationResult.Failure(fieldName, 
                    $"Value must be between {minValue} and {maxValue}")
            End If
            
            Return ValidationResult.Success()
        End Function
        
        
        ''' 函数：验证字符串长度
        
        Public Shared Function ValidateStringLength(value As String, fieldName As String, 
                                                   minLength As Integer, maxLength As Integer) As ValidationResult
            If value Is Nothing Then
                value = String.Empty
            End If
            
            If value.Length < minLength Then
                Return ValidationResult.Failure(fieldName, 
                    $"Value must be at least {minLength} characters long")
            End If
            
            If value.Length > maxLength Then
                Return ValidationResult.Failure(fieldName, 
                    $"Value cannot exceed {maxLength} characters")
            End If
            
            Return ValidationResult.Success()
        End Function
        
        
        ''' 函数：验证必填字段
        
        Public Shared Function ValidateRequired(value As String, fieldName As String) As ValidationResult
            If String.IsNullOrWhiteSpace(value) Then
                Return ValidationResult.Failure(fieldName, $"'{fieldName}' is required")
            End If
            
            Return ValidationResult.Success()
        End Function
        
        
        ''' 函数：验证文件路径
        
        Public Shared Function ValidateFilePath(filePath As String, fieldName As String, 
                                               Optional mustExist As Boolean = True) As ValidationResult
            If String.IsNullOrWhiteSpace(filePath) Then
                Return ValidationResult.Failure(fieldName, "File path cannot be empty")
            End If
            
            Try
                Dim fullPath = Path.GetFullPath(filePath)
                
                If mustExist AndAlso Not File.Exists(fullPath) Then
                    Return ValidationResult.Failure(fieldName, $"File does not exist: {filePath}")
                End If
                
                Return ValidationResult.Success()
            Catch ex As Exception
                Return ValidationResult.Failure(fieldName, $"Invalid file path: {ex.Message}")
            End Try
        End Function
        
        
        ''' 函数：验证目录路径
        
        Public Shared Function ValidateDirectoryPath(directoryPath As String, fieldName As String, 
                                                    Optional mustExist As Boolean = True) As ValidationResult
            If String.IsNullOrWhiteSpace(directoryPath) Then
                Return ValidationResult.Failure(fieldName, "Directory path cannot be empty")
            End If
            
            Try
                Dim fullPath = Path.GetFullPath(directoryPath)
                
                If mustExist AndAlso Not Directory.Exists(fullPath) Then
                    Return ValidationResult.Failure(fieldName, $"Directory does not exist: {directoryPath}")
                End If
                
                Return ValidationResult.Success()
            Catch ex As Exception
                Return ValidationResult.Failure(fieldName, $"Invalid directory path: {ex.Message}")
            End Try
        End Function
        
        
        ''' 函数：验证IP地址
        
        Public Shared Function ValidateIpAddress(ipAddress As String, fieldName As String) As ValidationResult
            If String.IsNullOrWhiteSpace(ipAddress) Then
                Return ValidationResult.Failure(fieldName, "IP address cannot be empty")
            End If
            
            Dim pattern = "^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$"
            If Not Regex.IsMatch(ipAddress, pattern) Then
                Return ValidationResult.Failure(fieldName, "Invalid IP address format")
            End If
            
            Dim parts = ipAddress.Split("."c)
            For Each part In parts
                If Not Integer.TryParse(part, Nothing) Then
                    Return ValidationResult.Failure(fieldName, "Invalid IP address format")
                End If
                
                Dim number = Integer.Parse(part)
                If number < 0 OrElse number > 255 Then
                    Return ValidationResult.Failure(fieldName, "Invalid IP address range")
                End If
            Next
            
            Return ValidationResult.Success()
        End Function
        
        
        ''' 函数：批量验证多个字段
        
        Public Shared Function ValidateAll(validations As IEnumerable(Of ValidationResult)) As ValidationResult
            Dim errors = validations.Where(Function(v) Not v.IsValid).ToList()
            
            If errors.Count = 0 Then
                Return ValidationResult.Success()
            End If
            
            Dim errorMessages = String.Join(Environment.NewLine, 
                errors.Select(Function(e) $"{e.FieldName}: {e.ErrorMessage}"))
            
            Return ValidationResult.Failure("Multiple fields", errorMessages)
        End Function
        
        
        ''' 子程序：抛出验证异常
        
        Public Shared Sub ThrowIfInvalid(result As ValidationResult)
            If Not result.IsValid Then
                Throw New ValidationException(result.ErrorMessage)
            End If
        End Sub
    End Class
    
    
    ''' 自定义验证异常
    Public Class ValidationException
        Inherits Exception
        
        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
        
        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End Namespace