Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Threading.Tasks

Namespace DeploymentAutomationGUI.Core
    
    ''' 配置管理器类
    
    Public Class ConfigurationManager
        Private Shared _instance As ConfigurationManager
        Private ReadOnly _configPath As String
        Private _configData As Dictionary(Of String, Object)
        Private ReadOnly _lockObject As New Object()
        
        
        ''' 配置更改时引发的事件
        
        Public Event ConfigurationChanged As EventHandler(Of ConfigurationChangedEventArgs)
        
        
        ''' 获取配置管理器的单例实例
        
        Public Shared ReadOnly Property Instance As ConfigurationManager
            Get
                If _instance Is Nothing Then
                    _instance = New ConfigurationManager()
                End If
                Return _instance
            End Get
        End Property
        
        
        ''' 私有构造函数
        
        Private Sub New()
            Dim appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            Dim appFolder = Path.Combine(appDataPath, "DeploymentAutomationGUI")
            
            If Not Directory.Exists(appFolder) Then
                Directory.CreateDirectory(appFolder)
            End If
            
            _configPath = Path.Combine(appFolder, "config.json")
            _configData = New Dictionary(Of String, Object)()
            
            LoadConfiguration()
        End Sub
        
        
        ''' 子程序：加载配置
        
        Private Sub LoadConfiguration()
            Try
                If File.Exists(_configPath) Then
                    Dim json = File.ReadAllText(_configPath)
                    _configData = JsonSerializer.Deserialize(Of Dictionary(Of String, Object))(json)
                End If
            Catch ex As Exception
                Debug.WriteLine($"Error loading configuration: {ex.Message}")
                _configData = New Dictionary(Of String, Object)()
            End Try
        End Sub
        
        
        ''' 子程序：保存配置
        
        Private Sub SaveConfiguration()
            Try
                Dim json = JsonSerializer.Serialize(_configData, New JsonSerializerOptions With {
                    .WriteIndented = True,
                    .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                })
                
                File.WriteAllText(_configPath, json)
            Catch ex As Exception
                Debug.WriteLine($"Error saving configuration: {ex.Message}")
            End Try
        End Sub
        
        
        ''' 函数：获取配置值
        
        ''' <typeparam name="T">值类型</typeparam>
        ''' <param name="key">配置键</param>
        ''' <param name="defaultValue">默认值</param>
        ''' <returns>配置值</returns>
        Public Function GetValue(Of T)(key As String, Optional defaultValue As T = Nothing) As T
            SyncLock _lockObject
                If _configData.ContainsKey(key) Then
                    Try
                        Dim value = _configData(key)
                        If value Is Nothing Then
                            Return defaultValue
                        End If
                        
                        If GetType(T) = GetType(String) AndAlso TypeOf value Is JsonElement Then
                            Dim jsonElement = DirectCast(value, JsonElement)
                            Return CType(CObj(jsonElement.GetString()), T)
                        ElseIf GetType(T).IsPrimitive AndAlso TypeOf value Is JsonElement Then
                            Dim jsonElement = DirectCast(value, JsonElement)
                            Dim jsonValue = jsonElement.ToString()
                            Return CType(Convert.ChangeType(jsonValue, GetType(T)), T)
                        Else
                            Return CType(value, T)
                        End If
                    Catch
                        Return defaultValue
                    End Try
                Else
                    Return defaultValue
                End If
            End SyncLock
        End Function
        
        
        ''' 子程序：设置配置值
        
        ''' <param name="key">配置键</param>
        ''' <param name="value">配置值</param>
        Public Sub SetValue(key As String, value As Object)
            SyncLock _lockObject
                Dim oldValue = If(_configData.ContainsKey(key), _configData(key), Nothing)
                _configData(key) = value
                SaveConfiguration()
                
                RaiseEvent ConfigurationChanged(Me, New ConfigurationChangedEventArgs With {
                    .Key = key,
                    .OldValue = oldValue,
                    .NewValue = value
                })
            End SyncLock
        End Sub
        
        
        ''' 函数：检查配置是否存在
        
        ''' <param name="key">配置键</param>
        ''' <returns>是否存在</returns>
        Public Function ContainsKey(key As String) As Boolean
            SyncLock _lockObject
                Return _configData.ContainsKey(key)
            End SyncLock
        End Function
        
        
        ''' 子程序：删除配置
        
        ''' <param name="key">配置键</param>
        Public Sub RemoveKey(key As String)
            SyncLock _lockObject
                If _configData.ContainsKey(key) Then
                    Dim oldValue = _configData(key)
                    _configData.Remove(key)
                    SaveConfiguration()
                    
                    RaiseEvent ConfigurationChanged(Me, New ConfigurationChangedEventArgs With {
                        .Key = key,
                        .OldValue = oldValue,
                        .NewValue = Nothing
                    })
                End If
            End SyncLock
        End Sub
        
        
        ''' 函数：获取所有配置键
        
        ''' <returns>配置键列表</returns>
        Public Function GetAllKeys() As List(Of String)
            SyncLock _lockObject
                Return _configData.Keys.ToList()
            End SyncLock
        End Function
        
        
        ''' 子程序：清空所有配置
        
        Public Sub ClearAll()
            SyncLock _lockObject
                _configData.Clear()
                SaveConfiguration()
                
                RaiseEvent ConfigurationChanged(Me, New ConfigurationChangedEventArgs With {
                    .Key = "ALL",
                    .OldValue = Nothing,
                    .NewValue = Nothing
                })
            End SyncLock
        End Sub
        
        
        ''' 异步函数：异步保存配置
        
        Public Async Function SaveAsync() As Task
            Await Task.Run(Sub() SaveConfiguration())
        End Function
        
        
        ''' 异步函数：异步加载配置
        
        Public Async Function LoadAsync() As Task
            Await Task.Run(Sub() LoadConfiguration())
        End Function
    End Class
    
    
    ''' 配置更改事件参数
    
    Public Class ConfigurationChangedEventArgs
        Inherits EventArgs
        
        
        ''' 配置键
        
        Public Property Key As String
        
        
        ''' 旧值
        
        Public Property OldValue As Object
        
        
        ''' 新值
        
        Public Property NewValue As Object
    End Class
End Namespace