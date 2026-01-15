Imports System.Net.Http
Imports System.Text
Imports System.Text.Json

Public Class RegisterForm
    Private ReadOnly httpClient As New HttpClient()
    Private Const API_BASE_URL As String = "http://localhost:5000/api"

    Private Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click
        Dim username = txtUsername.Text.Trim()
        Dim password = txtPassword.Text.Trim()
        Dim confirmPassword = txtConfirmPassword.Text.Trim()

        If String.IsNullOrEmpty(username) OrElse String.IsNullOrEmpty(password) Then
            MessageBox.Show("Please enter username and password", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If password <> confirmPassword Then
            MessageBox.Show("Passwords do not match", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Cursor = Cursors.WaitCursor
            Dim registerData = New With {
                .username = username,
                .password = password
            }

            Dim json = JsonSerializer.Serialize(registerData)
            Dim contentData = New StringContent(json, Encoding.UTF8, "application/json")

            Dim response = httpClient.PostAsync($"{API_BASE_URL}/auth/register", contentData).Result

            If response.IsSuccessStatusCode Then
                MessageBox.Show("Registration successful. You can now login.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Else
                Dim errorText = response.Content.ReadAsStringAsync().Result
                MessageBox.Show($"Registration failed: {errorText}", "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error during registration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class