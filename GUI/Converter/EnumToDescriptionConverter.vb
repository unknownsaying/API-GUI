Imports System
Imports System.ComponentModel
Imports System.Globalization
Imports System.Reflection
Imports System.Windows.Data

Namespace DeploymentAutomationGUI.Converters
    ''' <summary>
    ''' 枚举到描述转换器
    ''' </summary>
    Public Class EnumToDescriptionConverter
        Implements IValueConverter
        
        Public Function Convert(value As Object, targetType As Type, 
                               parameter As Object, culture As CultureInfo) As Object _
                               Implements IValueConverter.Convert
            If value Is Nothing Then
                Return String.Empty
            End If
            
            Dim enumType = value.GetType()
            
            If Not enumType.IsEnum Then
                Return value.ToString()
            End If
            
            Dim fieldInfo = enumType.GetField(value.ToString())
            If fieldInfo Is Nothing Then
                Return value.ToString()
            End If
            
            Dim attributes = fieldInfo.GetCustomAttributes(GetType(DescriptionAttribute), False)
            If attributes.Length > 0 Then
                Return DirectCast(attributes(0), DescriptionAttribute).Description
            Else
                Return value.ToString()
            End If
        End Function
        
        Public Function ConvertBack(value As Object, targetType As Type, 
                                   parameter As Object, culture As CultureInfo) As Object _
                                   Implements IValueConverter.ConvertBack
            If value Is Nothing OrElse String.IsNullOrEmpty(value.ToString()) Then
                Return Nothing
            End If
            
            If targetType.IsEnum Then
                Dim enumValues = [Enum].GetValues(targetType)
                For Each enumValue In enumValues
                    If Convert(enumValue, GetType(String), Nothing, culture).ToString() = value.ToString() Then
                        Return enumValue
                    End If
                Next
            End If
            
            Return Nothing
        End Function
    End Class
End Namespace