using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScannerDemo
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            await PerformLogin();
        }

        private async Task PerformLogin()
        {
            try
            {
                btnLogin.Enabled = false;
                txtEmail.Enabled = false;
                txtPassword.Enabled = false;
                lblStatus.Text = "Logging in...";
                lblStatus.ForeColor = Color.Blue;

                string email = txtEmail.Text;
                string password = txtPassword.Text;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    lblStatus.Text = "Please Enter email and password";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }

                var token = await AuthenticateUser(email, password);

                if (!string.IsNullOrEmpty(token))
                {
                    Properties.Settings.Default.BearerToken = token;
                    Properties.Settings.Default.Save();

                    lblStatus.Text = "Login successful!";
                    lblStatus.ForeColor = Color.Green;

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblStatus.Text = "Invalid email or password";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Login failed: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
            finally
            {
                btnLogin.Enabled = true;
                txtEmail.Enabled = true;
                txtPassword.Enabled = true;
            }
        }

        private async Task<string> AuthenticateUser(string email, string password)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var formData = new Dictionary<string, string>
                    {
                        { "username", email },
                        { "password", password },
                        { "client_id", "cloud-loom-auth-client" },
                        { "grant_type", "password" }
                    };
                    var content = new FormUrlEncodedContent(formData);

                    string apiUrl = "https://keycloak-app.graysand-a3c87220.eastus.azurecontainerapps.io";
                    var response = await client.PostAsync($"{apiUrl}/realms/CloudLoom/protocol/openid-connect/token", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<LoginResponse>(responseContent);
                        return result?.AccessToken;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) 
                    {
                        lblStatus.Text = "Incorrect email or password.";
                        lblStatus.ForeColor = Color.Red;
                        return string.Empty;
                    }
                    else
                    {
                        throw new Exception($"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Login failed: {ex.Message}", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        private class LoginResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
