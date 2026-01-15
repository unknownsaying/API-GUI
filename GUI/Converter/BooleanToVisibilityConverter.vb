Imports System
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace DeploymentAutomationGUI.Converters
    
    ''' 布尔值到可见性转换器
    
    Public Class BooleanToVisibilityConverter
        Implements IValueConverter
        
        
        ''' 是否反转逻辑（True时隐藏，False时显示）
        Public Property Invert As Boolean = False
        
        
        ''' 转换布尔值到可见性

        Public Function Convert(value As Object, targetType As Type, 
                               parameter As Object, culture As CultureInfo) As Object _
                               Implements IValueConverter.Convert
            Try
                If value Is Nothing Then
                    Return If(Invert, Visibility.Visible, Visibility.Collapsed)
                End If
                
                Dim boolValue As Boolean = False
                
                If TypeOf value Is Boolean Then
                    boolValue = DirectCast(value, Boolean)
                ElseIf TypeOf value Is String Then
                    Boolean.TryParse(value.ToString(), boolValue)
                ElseIf TypeOf value Is Integer Then
                    boolValue = (DirectCast(value, Integer) <> 0)
                End If
                
                If Invert Then
                    boolValue = Not boolValue
                End If
                
                Return If(boolValue, Visibility.Visible, Visibility.Collapsed)
            Catch
                Return Visibility.Collapsed
            End Try
        End Function
        
        Public Function ConvertBack(value As Object, targetType As Type, 
                                   parameter As Object, culture As CultureInfo) As Object _
                                   Implements IValueConverter.ConvertBack
            If value Is Nothing Then
                Return False
            End If
            
            If TypeOf value Is Visibility Then
                Dim visibility = DirectCast(value, Visibility)
                Dim result = (visibility = Visibility.Visible)
                Return If(Invert, Not result, result)
            End If
            
            Return False
        End Function
    End Class
End Namespace