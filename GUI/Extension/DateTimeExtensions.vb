Imports System
Imports System.Globalization

Namespace DeploymentAutomationGUI.Extensions
    
    ''' 日期时间扩展方法
    
    Public Module DateTimeExtensions
        
        
        ''' 检查日期时间是否为默认值
        
        <Runtime.CompilerServices.Extension>
        Public Function IsDefault(dateTime As DateTime) As Boolean
            Return dateTime = DateTime.MinValue OrElse dateTime = DateTime.MaxValue
        End Function
        
        
        ''' 将日期时间转换为Unix时间戳（秒）
        
        <Runtime.CompilerServices.Extension>
        Public Function ToUnixTimeSeconds(dateTime As DateTime) As Long
            Dim epoch = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            Dim utcDateTime = dateTime.ToUniversalTime()
            Return Convert.ToInt64((utcDateTime - epoch).TotalSeconds)
        End Function
        
        
        ''' 将日期时间转换为Unix时间戳（毫秒）
        
        <Runtime.CompilerServices.Extension>
        Public Function ToUnixTimeMilliseconds(dateTime As DateTime) As Long
            Dim epoch = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            Dim utcDateTime = dateTime.ToUniversalTime()
            Return Convert.ToInt64((utcDateTime - epoch).TotalMilliseconds)
        End Function
        
        
        ''' 从Unix时间戳（秒）转换为日期时间
        
        <Runtime.CompilerServices.Extension>
        Public Function FromUnixTimeSeconds(unixTime As Long) As DateTime
            Dim epoch = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            Return epoch.AddSeconds(unixTime).ToLocalTime()
        End Function
        
        
        ''' 从Unix时间戳（毫秒）转换为日期时间
        
        <Runtime.CompilerServices.Extension>
        Public Function FromUnixTimeMilliseconds(unixTime As Long) As DateTime
            Dim epoch = New DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            Return epoch.AddMilliseconds(unixTime).ToLocalTime()
        End Function
        
        
        ''' 获取日期时间所在月份的第一天
        
        <Runtime.CompilerServices.Extension>
        Public Function FirstDayOfMonth(dateTime As DateTime) As DateTime
            Return New DateTime(dateTime.Year, dateTime.Month, 1)
        End Function
        
        
        ''' 获取日期时间所在月份的最后一天
        
        <Runtime.CompilerServices.Extension>
        Public Function LastDayOfMonth(dateTime As DateTime) As DateTime
            Return New DateTime(dateTime.Year, dateTime.Month, 
                              DateTime.DaysInMonth(dateTime.Year, dateTime.Month))
        End Function
        
        
        ''' 获取日期时间所在季度的第一天
        
        <Runtime.CompilerServices.Extension>
        Public Function FirstDayOfQuarter(dateTime As DateTime) As DateTime
            Dim quarter = (dateTime.Month - 1) \ 3 + 1
            Dim firstMonth = (quarter - 1) * 3 + 1
            Return New DateTime(dateTime.Year, firstMonth, 1)
        End Function
        
        
        ''' 获取日期时间所在季度的最后一天
        
        <Runtime.CompilerServices.Extension>
        Public Function LastDayOfQuarter(dateTime As DateTime) As DateTime
            Dim quarter = (dateTime.Month - 1) \ 3 + 1
            Dim lastMonth = quarter * 3
            Dim daysInMonth = DateTime.DaysInMonth(dateTime.Year, lastMonth)
            Return New DateTime(dateTime.Year, lastMonth, daysInMonth)
        End Function
        
        
        ''' 获取日期时间所在年份的第一天
        
        <Runtime.CompilerServices.Extension>
        Public Function FirstDayOfYear(dateTime As DateTime) As DateTime
            Return New DateTime(dateTime.Year, 1, 1)
        End Function
        
        
        ''' 获取日期时间所在年份的最后一天
        
        <Runtime.CompilerServices.Extension>
        Public Function LastDayOfYear(dateTime As DateTime) As DateTime
            Return New DateTime(dateTime.Year, 12, 31)
        End Function
        
        
        ''' 获取日期时间是星期几（中文）
        
        <Runtime.CompilerServices.Extension>
        Public Function GetChineseDayOfWeek(dateTime As DateTime) As String
            Select Case dateTime.DayOfWeek
                Case DayOfWeek.Sunday
                    Return "星期日"
                Case DayOfWeek.Monday
                    Return "星期一"
                Case DayOfWeek.Tuesday
                    Return "星期二"
                Case DayOfWeek.Wednesday
                    Return "星期三"
                Case DayOfWeek.Thursday
                    Return "星期四"
                Case DayOfWeek.Friday
                    Return "星期五"
                Case DayOfWeek.Saturday
                    Return "星期六"
                Case Else
                    Return String.Empty
            End Select
        End Function
        
        
        ''' 获取日期时间是星期几（英文缩写）
        
        <Runtime.CompilerServices.Extension>
        Public Function GetShortDayOfWeek(dateTime As DateTime) As String
            Return dateTime.ToString("ddd", CultureInfo.InvariantCulture)
        End Function
        
        
        ''' 获取日期时间的友好显示字符串（例如："刚刚"、"5分钟前"）
        
        <Runtime.CompilerServices.Extension>
        Public Function ToFriendlyString(dateTime As DateTime) As String
            Dim now = DateTime.Now
            Dim span = now - dateTime
            
            If span.TotalSeconds < 0 Then
                ' 未来时间
                span = -span
                If span.TotalDays > 365 Then
                    Dim years = CInt(Math.Floor(span.TotalDays / 365))
                    Return $"{years}年后"
                ElseIf span.TotalDays > 30 Then
                    Dim months = CInt(Math.Floor(span.TotalDays / 30))
                    Return $"{months}个月后"
                ElseIf span.TotalDays > 1 Then
                    Return $"{CInt(Math.Floor(span.TotalDays))}天后"
                ElseIf span.TotalHours > 1 Then
                    Return $"{CInt(Math.Floor(span.TotalHours))}小时后"
                ElseIf span.TotalMinutes > 1 Then
                    Return $"{CInt(Math.Floor(span.TotalMinutes))}分钟后"
                Else
                    Return "即将"
                End If
            Else
                ' 过去时间
                If span.TotalDays > 365 Then
                    Dim years = CInt(Math.Floor(span.TotalDays / 365))
                    Return $"{years}年前"
                ElseIf span.TotalDays > 30 Then
                    Dim months = CInt(Math.Floor(span.TotalDays / 30))
                    Return $"{months}个月前"
                ElseIf span.TotalDays > 7 Then
                    Dim weeks = CInt(Math.Floor(span.TotalDays / 7))
                    Return $"{weeks}周前"
                ElseIf span.TotalDays > 1 Then
                    Return $"{CInt(Math.Floor(span.TotalDays))}天前"
                ElseIf span.TotalHours > 1 Then
                    Return $"{CInt(Math.Floor(span.TotalHours))}小时前"
                ElseIf span.TotalMinutes > 1 Then
                    Return $"{CInt(Math.Floor(span.TotalMinutes))}分钟前"
                ElseIf span.TotalSeconds > 30 Then
                    Return $"{CInt(Math.Floor(span.TotalSeconds))}秒前"
                Else
                    Return "刚刚"
                End If
            End If
        End Function
        
        
        ''' 检查日期时间是否在指定范围内
        
        <Runtime.CompilerServices.Extension>
        Public Function IsBetween(dateTime As DateTime, startDate As DateTime, endDate As DateTime) As Boolean
            Return dateTime >= startDate AndAlso dateTime <= endDate
        End Function
        
        
        ''' 获取日期时间所在周的星期一的日期
        
        <Runtime.CompilerServices.Extension>
        Public Function StartOfWeek(dateTime As DateTime, Optional startOfWeek As DayOfWeek = DayOfWeek.Monday) As DateTime
            Dim diff = (7 + (dateTime.DayOfWeek - startOfWeek)) Mod 7
            Return dateTime.AddDays(-diff).Date
        End Function
        
        
        ''' 获取日期时间所在周的星期日的日期
        
        <Runtime.CompilerServices.Extension>
        Public Function EndOfWeek(dateTime As DateTime, Optional startOfWeek As DayOfWeek = DayOfWeek.Monday) As DateTime
            Dim start = dateTime.StartOfWeek(startOfWeek)
            Return start.AddDays(6)
        End Function
        
        
        ''' 获取日期时间所在天的开始时间（00:00:00）
        
        <Runtime.CompilerServices.Extension>
        Public Function StartOfDay(dateTime As DateTime) As DateTime
            Return dateTime.Date
        End Function
        
        
        ''' 获取日期时间所在天的结束时间（23:59:59.999）
        
        <Runtime.CompilerServices.Extension>
        Public Function EndOfDay(dateTime As DateTime) As DateTime
            Return dateTime.Date.AddDays(1).AddTicks(-1)
        End Function
        
        
        ''' 获取日期时间所在小时的开始时间
        
        <Runtime.CompilerServices.Extension>
        Public Function StartOfHour(dateTime As DateTime) As DateTime
            Return New DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0)
        End Function
        
        
        ''' 获取日期时间所在小时的结束时间
        
        <Runtime.CompilerServices.Extension>
        Public Function EndOfHour(dateTime As DateTime) As DateTime
            Return dateTime.StartOfHour().AddHours(1).AddTicks(-1)
        End Function
        
        
        ''' 获取日期时间所在分钟的开始时间
        
        <Runtime.CompilerServices.Extension>
        Public Function StartOfMinute(dateTime As DateTime) As DateTime
            Return New DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 
                              dateTime.Hour, dateTime.Minute, 0)
        End Function
        
        
        ''' 获取日期时间所在分钟的结束时间
        
        <Runtime.CompilerServices.Extension>
        Public Function EndOfMinute(dateTime As DateTime) As DateTime
            Return dateTime.StartOfMinute().AddMinutes(1).AddTicks(-1)
        End Function
        
        
        ''' 获取日期时间的年龄
        
        <Runtime.CompilerServices.Extension>
        Public Function GetAge(dateTime As DateTime, Optional referenceDate As DateTime? = Nothing) As Integer
            Dim reference = If(referenceDate, DateTime.Today)
            Dim age = reference.Year - dateTime.Year
            
            ' 如果生日还没过，年龄减1
            If reference.Month < dateTime.Month OrElse 
               (reference.Month = dateTime.Month AndAlso reference.Day < dateTime.Day) Then
                age -= 1
            End If
            
            Return age
        End Function
        
        
        ''' 检查年份是否为闰年
        
        <Runtime.CompilerServices.Extension>
        Public Function IsLeapYear(dateTime As DateTime) As Boolean
            Return DateTime.IsLeapYear(dateTime.Year)
        End Function
        
        
        ''' 获取日期时间所在季度的序号
        
        <Runtime.CompilerServices.Extension>
        Public Function GetQuarter(dateTime As DateTime) As Integer
            Return (dateTime.Month - 1) \ 3 + 1
        End Function
        
        
        ''' 获取日期时间的中文农历日期
        
        <Runtime.CompilerServices.Extension>
        Public Function GetChineseLunarDate(dateTime As DateTime) As String
            Try
                Dim chineseCalendar = New ChineseLunisolarCalendar()
                Dim year = chineseCalendar.GetYear(dateTime)
                Dim month = chineseCalendar.GetMonth(dateTime)
                Dim day = chineseCalendar.GetDayOfMonth(dateTime)
                
                ' 获取闰月
                Dim leapMonth = chineseCalendar.GetLeapMonth(year)
                Dim monthPrefix = If(month = leapMonth, "闰", "")
                
                ' 农历月份名称
                Dim monthNames = {"正月", "二月", "三月", "四月", "五月", "六月", 
                                 "七月", "八月", "九月", "十月", "冬月", "腊月"}
                
                ' 农历日期名称
                Dim dayNames = {"初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
                               "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
                               "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十"}
                
                Dim monthName = If(month <= 12, monthNames(month - 1), "")
                Dim dayName = If(day <= 30, dayNames(day - 1), "")
                
                Return $"{monthPrefix}{monthName}{dayName}"
            Catch
                Return String.Empty
            End Try
        End Function
        
        
        ''' 获取日期时间的星座
        
        <Runtime.CompilerServices.Extension>
        Public Function GetZodiacSign(dateTime As DateTime) As String
            Dim month = dateTime.Month
            Dim day = dateTime.Day
            
            Select Case month
                Case 1
                    Return If(day <= 19, "摩羯座", "水瓶座")
                Case 2
                    Return If(day <= 18, "水瓶座", "双鱼座")
                Case 3
                    Return If(day <= 20, "双鱼座", "白羊座")
                Case 4
                    Return If(day <= 19, "白羊座", "金牛座")
                Case 5
                    Return If(day <= 20, "金牛座", "双子座")
                Case 6
                    Return If(day <= 21, "双子座", "巨蟹座")
                Case 7
                    Return If(day <= 22, "巨蟹座", "狮子座")
                Case 8
                    Return If(day <= 22, "狮子座", "处女座")
                Case 9
                    Return If(day <= 22, "处女座", "天秤座")
                Case 10
                    Return If(day <= 23, "天秤座", "天蝎座")
                Case 11
                    Return If(day <= 21, "天蝎座", "射手座")
                Case 12
                    Return If(day <= 21, "射手座", "摩羯座")
                Case Else
                    Return String.Empty
            End Select
        End Function
        
        
        ''' 获取日期时间的工作日序号（周一=1, 周二=2, ..., 周日=7）
        
        <Runtime.CompilerServices.Extension>
        Public Function GetWorkDayNumber(dateTime As DateTime) As Integer
            Return If(dateTime.DayOfWeek = DayOfWeek.Sunday, 7, CInt(dateTime.DayOfWeek))
        End Function
        
        
        ''' 检查日期时间是否为工作日（周一至周五）
        
        <Runtime.CompilerServices.Extension>
        Public Function IsWorkDay(dateTime As DateTime) As Boolean
            Return dateTime.DayOfWeek >= DayOfWeek.Monday AndAlso 
                   dateTime.DayOfWeek <= DayOfWeek.Friday
        End Function
        
        
        ''' 检查日期时间是否为周末（周六或周日）
        
        <Runtime.CompilerServices.Extension>
        Public Function IsWeekend(dateTime As DateTime) As Boolean
            Return dateTime.DayOfWeek = DayOfWeek.Saturday OrElse 
                   dateTime.DayOfWeek = DayOfWeek.Sunday
        End Function
        
        
        ''' 获取日期时间的时区名称
        
        <Runtime.CompilerServices.Extension>
        Public Function GetTimeZoneName(dateTime As DateTime) As String
            Return TimeZoneInfo.Local.StandardName
        End Function
    End Module
End Namespace