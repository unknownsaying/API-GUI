Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Windows.Forms
Imports System.Drawing


Public Class LoginForm
    Private ReadOnly httpClient As New HttpClient()
    Private Const API_BASE_URL As String = "http://localhost:5000/api"

    Public Property AccessToken As String
    Public Property UserId As Integer

    Private Sub btnLogin_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
        Dim username = txtUsername.Text.Trim()
        Dim password = txtPassword.Text.Trim()

        If String.IsNullOrEmpty(username) OrElse String.IsNullOrEmpty(password) Then
            MessageBox.Show("Please enter username and password", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Cursor = Cursors.WaitCursor
            Dim loginData = New With {
                .username = username,
                .password = password
            }

            Dim json = JsonSerializer.Serialize(loginData)
            Dim contentData = New StringContent(json, Encoding.UTF8, "application/json")

            Dim response = httpClient.PostAsync($"{API_BASE_URL}/auth/login", contentData).Result

            If response.IsSuccessStatusCode Then
                Dim responseJson = response.Content.ReadAsStringAsync().Result
                Dim apiResponse = JsonSerializer.Deserialize(Of ApiResponse)(responseJson, New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})

                If apiResponse.Success Then
                    AccessToken = apiResponse.Access_token
                    UserId = apiResponse.User_id

                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    MessageBox.Show($"Login failed: {apiResponse.Message}", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Else
                Dim errorText = response.Content.ReadAsStringAsync().Result
                MessageBox.Show($"Login failed: {errorText}", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error during login: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click
        Dim registerForm As New RegisterForm()
        registerForm.ShowDialog()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class

Public Class ApiResponse
    Public Property Success As Boolean
    Public Property Message As String
    Public Property Access_token As String
    Public Property User_id As Integer
End Class


Public Class LoginForm
    Private ReadOnly httpClient As New HttpClient()
    Private Const API_BASE_URL As String = "http://localhost:5000/api"
    Private loginAttempts As Integer = 0
    Private Const MAX_LOGIN_ATTEMPTS As Integer = 5
    
    Public Property AuthToken As String = ""
    Public Property RefreshToken As String = ""
    Public Property CurrentUser As UserInfo = Nothing
    Public Property IsAuthenticated As Boolean = False
    
    Public Class UserInfo
        Public Property Id As Integer
        Public Property Uuid As String
        Public Property Username As String
        Public Property Email As String
        Public Property IsAdmin As Boolean
        Public Property CreatedAt As String
    End Class
    
    Public Class AuthResponse
        Public Property Success As Boolean
        Public Property Message As String
        Public Property Data As AuthData
    End Class
    
    Public Class AuthData
        Public Property Access_token As String
        Public Property Refresh_token As String
        Public Property Token_type As String
        Public Property Expires_in As Integer
        Public Property User As UserInfo
    End Class
    
    Private Sub LoginForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "UnknownSaying - Login"
        Me.Icon = My.Resources.LoginIcon
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen
        
        LoadSavedCredentials()
        CheckAutoLogin()
    End Sub
    
    Private Sub LoadSavedCredentials()
        Try
            Dim username = My.Settings.SavedUsername
            Dim remember = My.Settings.RememberMe
            
            If Not String.IsNullOrEmpty(username) AndAlso remember Then
                txtUsername.Text = username
                chkRememberMe.Checked = True
                txtPassword.Focus()
            End If
        Catch ex As Exception
            ' Ignore settings errors
        End Try
    End Sub
    
    Private Sub CheckAutoLogin()
        Try
            Dim autoLogin = My.Settings.AutoLogin
            Dim savedToken = My.Settings.AuthToken
            
            If autoLogin AndAlso Not String.IsNullOrEmpty(savedToken) Then
                ' Try to refresh token
                If RefreshAccessToken(savedToken) Then
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                End If
            End If
        Catch ex As Exception
            ' Ignore auto-login errors
        End Try
    End Sub
    
    Private Async Sub btnLogin_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
        If loginAttempts >= MAX_LOGIN_ATTEMPTS Then
            MessageBox.Show("Too many failed login attempts. Please try again later.", 
                          "Account Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        
        Dim username = txtUsername.Text.Trim()
        Dim password = txtPassword.Text
        
        If String.IsNullOrEmpty(username) OrElse String.IsNullOrEmpty(password) Then
            MessageBox.Show("Please enter both username and password.", 
                          "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtUsername.Focus()
            Return
        End If
        
        Try
            btnLogin.Enabled = False
            btnLogin.Text = "Authenticating..."
            Cursor = Cursors.WaitCursor
            
            Dim loginData = New With {
                .username = username,
                .password = password
            }
            
            Dim json = JsonSerializer.Serialize(loginData)
            Dim contentData = New StringContent(json, Encoding.UTF8, "application/json")
            
            Dim response = Await httpClient.PostAsync($"{API_BASE_URL}/auth/login", contentData)
            
            If response.IsSuccessStatusCode Then
                Dim responseJson = Await response.Content.ReadAsStringAsync()
                Dim authResponse = JsonSerializer.Deserialize(Of AuthResponse)(responseJson, 
                    New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                
                If authResponse.Success AndAlso authResponse.Data IsNot Nothing Then
                    ' Save authentication data
                    AuthToken = authResponse.Data.Access_token
                    RefreshToken = authResponse.Data.Refresh_token
                    CurrentUser = authResponse.Data.User
                    IsAuthenticated = True
                    
                    ' Save credentials if Remember Me is checked
                    If chkRememberMe.Checked Then
                        My.Settings.SavedUsername = username
                        My.Settings.RememberMe = True
                        My.Settings.AuthToken = AuthToken
                        My.Settings.RefreshToken = RefreshToken
                        My.Settings.Save()
                    End If
                    
                    ' Save auto-login setting
                    If chkAutoLogin.Checked Then
                        My.Settings.AutoLogin = True
                        My.Settings.Save()
                    End If
                    
                    ' Reset login attempts
                    loginAttempts = 0
                    
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Else
                    loginAttempts += 1
                    MessageBox.Show($"Login failed: {authResponse.Message}", 
                                  "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Else
                loginAttempts += 1
                Dim errorText = Await response.Content.ReadAsStringAsync()
                MessageBox.Show($"Login failed: {errorText}", 
                              "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
            
        Catch ex As Exception
            loginAttempts += 1
            MessageBox.Show($"Error during login: {ex.Message}", 
                          "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnLogin.Enabled = True
            btnLogin.Text = "Login"
            Cursor = Cursors.Default
        End Try
    End Sub
    
    Private Function RefreshAccessToken(refreshToken As String) As Boolean
        Try
            Dim refreshData = New With {
                .refresh_token = refreshToken
            }
            
            Dim json = JsonSerializer.Serialize(refreshData)
            Dim contentData = New StringContent(json, Encoding.UTF8, "application/json")
            
            Dim response = httpClient.PostAsync($"{API_BASE_URL}/auth/refresh", contentData).Result
            
            If response.IsSuccessStatusCode Then
                Dim responseJson = response.Content.ReadAsStringAsync().Result
                Dim authResponse = JsonSerializer.Deserialize(Of AuthResponse)(responseJson,
                    New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True})
                
                If authResponse.Success AndAlso authResponse.Data IsNot Nothing Then
                    AuthToken = authResponse.Data.Access_token
                    RefreshToken = refreshToken ' Keep the same refresh token
                    IsAuthenticated = True
                    Return True
                End If
            End If
        Catch ex As Exception
            ' Ignore refresh errors
        End Try
        
        Return False
    End Function
    
    Private Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click
        Dim registerForm As New RegisterForm()
        If registerForm.ShowDialog() = DialogResult.OK Then
            txtUsername.Text = registerForm.Username
            txtPassword.Focus()
        End If
    End Sub
    
    Private Sub btnForgotPassword_Click(sender As Object, e As EventArgs) Handles btnForgotPassword.Click
        Dim resetForm As New PasswordResetForm()
        resetForm.ShowDialog()
    End Sub
    
    Private Sub chkShowPassword_CheckedChanged(sender As Object, e As EventArgs) Handles chkShowPassword.CheckedChanged
        txtPassword.UseSystemPasswordChar = Not chkShowPassword.Checked
    End Sub
    
    Private Sub LoginForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Me.DialogResult <> DialogResult.OK Then
            Application.Exit()
        End If
    End Sub
    
    Public Function GetAuthenticatedHttpClient() As HttpClient
        Dim client = New HttpClient()
        If Not String.IsNullOrEmpty(AuthToken) Then
            client.DefaultRequestHeaders.Authorization = 
                New System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken)
        End If
        Return client
    End Function
End Class