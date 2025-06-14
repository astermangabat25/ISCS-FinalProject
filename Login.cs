﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml;

namespace FinalProject
{
    public partial class Login : UserControl
    {
        string IPAddress = "127.0.0.1";
        private XmlDocument? doc;
        private XmlElement? root;
        private static readonly string relativePath = @"XMLFiles\data.xml";
        private readonly string path = Path.Combine(Environment.CurrentDirectory, relativePath);

        public Login()
        {
            InitializeComponent();
            LoadXmlData();

            // Set focus to the username field when the form loads
            this.Load += (s, e) => username.Focus();

            // Add hover effect to submit button
            submit.MouseEnter += (s, e) => submit.BackColor = Color.FromArgb(32, 33, 36);
            submit.MouseLeave += (s, e) => submit.BackColor = Color.FromArgb(23, 24, 29);

            // Add Enter key handling for password field
            password.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    Submit_Click(submit, EventArgs.Empty);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
        }

        private void LoadXmlData()
        {
            try
            {
                doc = new XmlDocument();
                doc.Load(path);
                root = doc.DocumentElement;

                if (root == null)
                {
                    MessageBox.Show("No data found in XML file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading XML data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Submit_Click(object sender, EventArgs e)
        {
            string enteredUsername = username.Text.Trim();
            string enteredPassword = password.Text.Trim();

            if (string.IsNullOrEmpty(enteredUsername) || string.IsNullOrEmpty(enteredPassword))
            {
                MessageBox.Show("Please fill in both the username and password.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var loginRequest = new
            {
                action = "login",
                username = enteredUsername,
                password = enteredPassword
            };

            string requestJson = JsonSerializer.Serialize(loginRequest);
                Session.LoggedInUsername = enteredUsername;
                MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Navigate to the posts view
                var form = FindForm() as Form1;
                form?.LoadView(new newPosts());
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool VerifyLogin(string enteredUsername, string enteredPassword)
        {
            if (root == null) return false;

            XmlNode? userNode = root.SelectSingleNode($"/data/users/user[username='{enteredUsername}']");

            try
            {
                using (TcpClient client = new TcpClient(IPAddress, 8888))
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    writer.WriteLine(requestJson);
                    string responseJson = reader.ReadLine();

                    var response = JsonSerializer.Deserialize<Dictionary<string, string>>(responseJson);
                var passwordNode = userNode["password"];
                if (passwordNode != null)
                {
                    string storedHashedPassword = passwordNode.InnerText;
                    return LoginPasswordHelper.VerifyPassword(enteredPassword, storedHashedPassword);
                }
            }

                    if (response["status"] == "success")
                    {
                        Session.LoggedInUsername = enteredUsername;
                        MessageBox.Show("Login successful!");
                    }
                    else
                    {
                        MessageBox.Show(response["message"]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }

    public static class LoginPasswordHelper
    {
        public static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                10000,
                HashAlgorithmName.SHA256,
                20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(storedHash);

                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                    enteredPassword,
                    salt,
                    10000,
                    HashAlgorithmName.SHA256,
                    20);

                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
