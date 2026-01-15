Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

Namespace DeploymentAutomationGUI.Extensions
    
    ''' 字符串扩展方法
    
    Public Module StringExtensions
        
        
        ''' 检查字符串是否为null或空
        
        <Runtime.CompilerServices.Extension>
        Public Function IsNullOrEmpty(value As String) As Boolean
            Return String.IsNullOrEmpty(value)
        End Function
        
        
        ''' 检查字符串是否为null、空或仅包含空白字符
        
        <Runtime.CompilerServices.Extension>
        Public Function IsNullOrWhiteSpace(value As String) As Boolean
            Return String.IsNullOrWhiteSpace(value)
        End Function
        
        
        ''' 如果字符串为null则返回空字符串
        
        <Runtime.CompilerServices.Extension>
        Public Function DefaultIfNull(value As String, Optional defaultValue As String = "") As String
            Return If(value, defaultValue)
        End Function
        
        
        ''' 将字符串转换为整数
        
        <Runtime.CompilerServices.Extension>
        Public Function ToInt(value As String, Optional defaultValue As Integer = 0) As Integer
            If Integer.TryParse(value, defaultValue) Then
                Return defaultValue
            End If
            Return defaultValue
        End Function
        
        
        ''' 将字符串转换为布尔值
        
        <Runtime.CompilerServices.Extension>
        Public Function ToBoolean(value As String, Optional defaultValue As Boolean = False) As Boolean
            If Boolean.TryParse(value, defaultValue) Then
                Return defaultValue
            End If
            
            Select Case value.ToLower()
                Case "1", "yes", "y", "true", "on", "ok"
                    Return True
                Case "0", "no", "n", "false", "off", "cancel"
                    Return False
                Case Else
                    Return defaultValue
            End Select
        End Function
        
        
        ''' 安全截取字符串
        
        <Runtime.CompilerServices.Extension>
        Public Function SafeSubstring(value As String, startIndex As Integer, Optional length As Integer = -1) As String
            If value Is Nothing Then
                Return String.Empty
            End If
            
            If startIndex < 0 Then
                startIndex = 0
            End If
            
            If startIndex >= value.Length Then
                Return String.Empty
            End If
            
            If length < 0 OrElse startIndex + length > value.Length Then
                length = value.Length - startIndex
            End If
            
            Return value.Substring(startIndex, length)
        End Function
        
        
        ''' 将字符串转换为标题格式（每个单词首字母大写）
        
        <Runtime.CompilerServices.Extension>
        Public Function ToTitleCase(value As String, Optional culture As CultureInfo = Nothing) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            If culture Is Nothing Then
                culture = CultureInfo.CurrentCulture
            End If
            
            Return culture.TextInfo.ToTitleCase(value.ToLower(culture))
        End Function
        
        
        ''' 移除字符串两端的空白字符，如果为null则返回空字符串
        
        <Runtime.CompilerServices.Extension>
        Public Function TrimSafe(value As String) As String
            Return If(value?.Trim(), String.Empty)
        End Function
        
        
        ''' 检查字符串是否包含指定的子字符串（不区分大小写）
        
        <Runtime.CompilerServices.Extension>
        Public Function ContainsIgnoreCase(value As String, searchString As String, 
                                          Optional comparisonType As StringComparison = StringComparison.OrdinalIgnoreCase) As Boolean
            If value Is Nothing OrElse searchString Is Nothing Then
                Return False
            End If
            
            Return value.IndexOf(searchString, comparisonType) >= 0
        End Function
        
        
        ''' 检查字符串是否以指定的子字符串开头（不区分大小写）
        
        <Runtime.CompilerServices.Extension>
        Public Function StartsWithIgnoreCase(value As String, searchString As String) As Boolean
            If value Is Nothing OrElse searchString Is Nothing Then
                Return False
            End If
            
            Return value.StartsWith(searchString, StringComparison.OrdinalIgnoreCase)
        End Function
        
        
        ''' 检查字符串是否以指定的子字符串结尾（不区分大小写）
        
        <Runtime.CompilerServices.Extension>
        Public Function EndsWithIgnoreCase(value As String, searchString As String) As Boolean
            If value Is Nothing OrElse searchString Is Nothing Then
                Return False
            End If
            
            Return value.EndsWith(searchString, StringComparison.OrdinalIgnoreCase)
        End Function
        
        
        ''' 计算字符串的MD5哈希值
        
        <Runtime.CompilerServices.Extension>
        Public Function ComputeMD5Hash(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return String.Empty
            End If
            
            Using md5 = MD5.Create()
                Dim bytes = Encoding.UTF8.GetBytes(value)
                Dim hashBytes = md5.ComputeHash(bytes)
                
                Dim builder = New StringBuilder()
                For Each b In hashBytes
                    builder.Append(b.ToString("x2"))
                Next
                
                Return builder.ToString()
            End Using
        End Function
        
        
        ''' 计算字符串的SHA256哈希值
        
        <Runtime.CompilerServices.Extension>
        Public Function ComputeSHA256Hash(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return String.Empty
            End If
            
            Using sha256 = SHA256.Create()
                Dim bytes = Encoding.UTF8.GetBytes(value)
                Dim hashBytes = sha256.ComputeHash(bytes)
                
                Dim builder = New StringBuilder()
                For Each b In hashBytes
                    builder.Append(b.ToString("x2"))
                Next
                
                Return builder.ToString()
            End Using
        End Function
        
        
        ''' 将字符串转换为Base64编码
        
        <Runtime.CompilerServices.Extension>
        Public Function ToBase64(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return String.Empty
            End If
            
            Dim bytes = Encoding.UTF8.GetBytes(value)
            Return Convert.ToBase64String(bytes)
        End Function
        
        
        ''' 从Base64字符串解码
        
        <Runtime.CompilerServices.Extension>
        Public Function FromBase64(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return String.Empty
            End If
            
            Try
                Dim bytes = Convert.FromBase64String(value)
                Return Encoding.UTF8.GetString(bytes)
            Catch
                Return String.Empty
            End Try
        End Function
        
        
        ''' 检查字符串是否为有效的电子邮件地址
        
        <Runtime.CompilerServices.Extension>
        Public Function IsValidEmail(value As String) As Boolean
            If String.IsNullOrWhiteSpace(value) Then
                Return False
            End If
            
            Try
                ' 使用正则表达式验证电子邮件格式
                Dim pattern = "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
                Return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250))
            Catch
                Return False
            End Try
        End Function
        
        
        ''' 检查字符串是否为有效的URL
        
        <Runtime.CompilerServices.Extension>
        Public Function IsValidUrl(value As String) As Boolean
            If String.IsNullOrWhiteSpace(value) Then
                Return False
            End If
            
            Try
                Return Uri.TryCreate(value, UriKind.Absolute, Nothing)
            Catch
                Return False
            End Try
        End Function
        
        
        ''' 反转字符串
        
        <Runtime.CompilerServices.Extension>
        Public Function Reverse(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            Dim charArray = value.ToCharArray()
            Array.Reverse(charArray)
            Return New String(charArray)
        End Function
        
        
        ''' 统计字符串中单词的数量
        
        <Runtime.CompilerServices.Extension>
        Public Function WordCount(value As String) As Integer
            If String.IsNullOrWhiteSpace(value) Then
                Return 0
            End If
            
            ' 分割字符串并统计非空单词
            Dim words = value.Split(New Char() {" "c, ControlChars.Tab, ControlChars.Lf, ControlChars.Cr}, 
                                   StringSplitOptions.RemoveEmptyEntries)
            Return words.Length
        End Function
        
        
        ''' 移除字符串中的所有空白字符
        
        <Runtime.CompilerServices.Extension>
        Public Function RemoveWhitespace(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            Return New String(value.Where(Function(c) Not Char.IsWhiteSpace(c)).ToArray())
        End Function
        
        
        ''' 将字符串转换为驼峰命名法
        
        <Runtime.CompilerServices.Extension>
        Public Function ToCamelCase(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            Dim words = value.Split(New Char() {" "c, "-"c, "_"c}, StringSplitOptions.RemoveEmptyEntries)
            If words.Length = 0 Then
                Return String.Empty
            End If
            
            Dim result = words(0).ToLower()
            For i As Integer = 1 To words.Length - 1
                result += words(i).ToTitleCase().Replace(" ", "")
            Next
            
            Return result
        End Function
        
        
        ''' 将字符串转换为帕斯卡命名法
        
        <Runtime.CompilerServices.Extension>
        Public Function ToPascalCase(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            Dim camelCase = value.ToCamelCase()
            If String.IsNullOrEmpty(camelCase) Then
                Return String.Empty
            End If
            
            Return Char.ToUpper(camelCase(0)) & camelCase.Substring(1)
        End Function
        
        
        ''' 将字符串转换为蛇形命名法
        
        <Runtime.CompilerServices.Extension>
        Public Function ToSnakeCase(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            Dim result = Regex.Replace(value, "([a-z])([A-Z])", "$1_$2")
            Return result.ToLower()
        End Function
        
        
        ''' 将字符串转换为凯巴命名法
        
        <Runtime.CompilerServices.Extension>
        Public Function ToKebabCase(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            Dim result = Regex.Replace(value, "([a-z])([A-Z])", "$1-$2")
            Return result.ToLower()
        End Function
        
        
        ''' 将字符串用指定字符填充到指定长度
        
        <Runtime.CompilerServices.Extension>
        Public Function PadBoth(value As String, totalWidth As Integer, paddingChar As Char) As String
            If value Is Nothing Then
                value = String.Empty
            End If
            
            If totalWidth <= value.Length Then
                Return value
            End If
            
            Dim padding = totalWidth - value.Length
            Dim leftPadding = padding \ 2
            Dim rightPadding = padding - leftPadding
            
            Return New String(paddingChar, leftPadding) & value & New String(paddingChar, rightPadding)
        End Function
        
        
        ''' 安全地获取字符串的指定索引处的字符
        
        <Runtime.CompilerServices.Extension>
        Public Function SafeCharAt(value As String, index As Integer, Optional defaultValue As Char = Nothing) As Char
            If value Is Nothing OrElse index < 0 OrElse index >= value.Length Then
                Return defaultValue
            End If
            
            Return value(index)
        End Function
        
        
        ''' 使用指定分隔符连接字符串列表
        
        <Runtime.CompilerServices.Extension>
        Public Function Join(list As IEnumerable(Of String), Optional separator As String = ", ") As String
            If list Is Nothing Then
                Return String.Empty
            End If
            
            Return String.Join(separator, list.Where(Function(s) Not String.IsNullOrEmpty(s)))
        End Function
        
        
        ''' 将字符串重复指定次数
        
        <Runtime.CompilerServices.Extension>
        Public Function Repeat(value As String, count As Integer) As String
            If String.IsNullOrEmpty(value) OrElse count <= 0 Then
                Return String.Empty
            End If
            
            Dim builder = New StringBuilder(value.Length * count)
            For i As Integer = 0 To count - 1
                builder.Append(value)
            Next
            
            Return builder.ToString()
        End Function
        
        
        ''' 获取字符串的字节长度（UTF-8编码）
        
        <Runtime.CompilerServices.Extension>
        Public Function ByteLength(value As String) As Integer
            If String.IsNullOrEmpty(value) Then
                Return 0
            End If
            
            Return Encoding.UTF8.GetByteCount(value)
        End Function
        
        
        ''' 检查字符串是否只包含字母
        
        <Runtime.CompilerServices.Extension>
        Public Function IsAlpha(value As String) As Boolean
            If String.IsNullOrEmpty(value) Then
                Return False
            End If
            
            Return value.All(Function(c) Char.IsLetter(c))
        End Function
        
        
        ''' 检查字符串是否只包含字母和数字
        
        <Runtime.CompilerServices.Extension>
        Public Function IsAlphaNumeric(value As String) As Boolean
            If String.IsNullOrEmpty(value) Then
                Return False
            End If
            
            Return value.All(Function(c) Char.IsLetterOrDigit(c))
        End Function
        
        
        ''' 检查字符串是否只包含数字
        
        <Runtime.CompilerServices.Extension>
        Public Function IsNumeric(value As String) As Boolean
            If String.IsNullOrEmpty(value) Then
                Return False
            End If
            
            Return value.All(Function(c) Char.IsDigit(c))
        End Function
        
        
        ''' 将字符串中的HTML特殊字符转义
        
        <Runtime.CompilerServices.Extension>
        Public Function EscapeHtml(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            Return value.Replace("&", "&amp;") _
                       .Replace("<", "&lt;") _
                       .Replace(">", "&gt;") _
                       .Replace("""", "&quot;") _
                       .Replace("'", "&#39;")
        End Function
        
        
        ''' 将转义的HTML特殊字符还原
        
        <Runtime.CompilerServices.Extension>
        Public Function UnescapeHtml(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return value
            End If
            
            Return value.Replace("&amp;", "&") _
                       .Replace("&lt;", "<") _
                       .Replace("&gt;", ">") _
                       .Replace("&quot;", """") _
                       .Replace("&#39;", "'") _
                       .Replace("&apos;", "'")
        End Function
    End Module
End Namespace