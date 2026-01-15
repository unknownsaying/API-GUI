Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports System.Windows.Data

Namespace DeploymentAutomationGUI.Converters
    
    ''' 多值转换器基类
    
    Public MustInherit Class MultiValueConverter
        Implements IMultiValueConverter
        
        Public MustOverride Function Convert(values() As Object, targetType As Type, 
                                            parameter As Object, culture As CultureInfo) As Object _
                                            Implements IMultiValueConverter.Convert
        
        Public Overridable Function ConvertBack(value As Object, targetTypes() As Type, 
                                               parameter As Object, culture As CultureInfo) As Object() _
                                               Implements IMultiValueConverter.ConvertBack
            Return Enumerable.Repeat(DependencyProperty.UnsetValue, targetTypes.Length).ToArray()
        End Function
    End Class
    
    
    ''' 多值布尔到可见性转换器
    
    Public Class MultiBooleanToVisibilityConverter
        Inherits MultiValueConverter
        
        Public Overrides Function Convert(values() As Object, targetType As Type, 
                                         parameter As Object, culture As CultureInfo) As Object
            
            If values Is Nothing OrElse values.Length = 0 Then
                Return Visibility.Collapsed
            End If
            
            Dim logic As String = If(parameter?.ToString(), "AND")
            
            Select Case logic.ToUpper()
                Case "AND"
                    ' 所有值都为True时显示
                    For Each value In values
                        If value Is Nothing OrElse Not (TypeOf value Is Boolean AndAlso CBool(value)) Then
                            Return Visibility.Collapsed
                        End If
                    Next
                    Return Visibility.Visible
                    
                Case "OR"
                    ' 任意一个值为True时显示
                    For Each value In values
                        If value IsNot Nothing AndAlso TypeOf value Is Boolean AndAlso CBool(value) Then
                            Return Visibility.Visible
                        End If
                    Next
                    Return Visibility.Collapsed
                    
                Case "XOR"
                    ' 只有一个值为True时显示
                    Dim trueCount As Integer = 0
                    For Each value In values
                        If value IsNot Nothing AndAlso TypeOf value Is Boolean AndAlso CBool(value) Then
                            trueCount += 1
                        End If
                    Next
                    Return If(trueCount = 1, Visibility.Visible, Visibility.Collapsed)
                    
                Case Else
                    Return Visibility.Collapsed
            End Select
        End Function
    End Class
    
    ''' 字符串连接转换器
    
    Public Class StringConcatenationConverter
        Inherits MultiValueConverter
        
        Public Property Separator As String = " "
        
        Public Overrides Function Convert(values() As Object, targetType As Type, 
                                         parameter As Object, culture As CultureInfo) As Object
            
            If values Is Nothing Then
                Return String.Empty
            End If
            
            Dim separator = If(parameter?.ToString(), Me.Separator)
            Dim result As New StringBuilder()
            
            For i As Integer = 0 To values.Length - 1
                If values(i) IsNot Nothing Then
                    Dim strValue = values(i).ToString()
                    If Not String.IsNullOrEmpty(strValue) Then
                        If result.Length > 0 Then
                            result.Append(separator)
                        End If
                        result.Append(strValue)
                    End If
                End If
            Next
            
            Return result.ToString()
        End Function
    End Class
End Namespace