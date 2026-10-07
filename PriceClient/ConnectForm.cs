using System;
using System.Collections.Generic;
using System.Text;

namespace PriceClient
{
    public class ConnectForm : Form
    {
        public string Host { get; private set; }
        public int Port { get; private set; }

        private TextBox txtHost;
        private TextBox txtPort;

        public ConnectForm()
        {
            Text = "Подключение к серверу";
            Size = new Size(400, 220);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            Label lblTitle = new Label
            {
                Text = "ПОДКЛЮЧЕНИЕ",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(120, 15),
                AutoSize = true,
                ForeColor = Color.DarkBlue
            };
            Controls.Add(lblTitle);

            Label lblHost = new Label
            {
                Text = "Хост:",
                Location = new Point(20, 60),
                AutoSize = true,
                Font = new Font("Arial", 10)
            };
            Controls.Add(lblHost);

            txtHost = new TextBox
            {
                Location = new Point(120, 57),
                Size = new Size(240, 25),
                Text = "127.0.0.1",
                Font = new Font("Arial", 10)
            };
            Controls.Add(txtHost);

            Label lblPort = new Label
            {
                Text = "Порт:",
                Location = new Point(20, 95),
                AutoSize = true,
                Font = new Font("Arial", 10)
            };
            Controls.Add(lblPort);

            txtPort = new TextBox
            {
                Location = new Point(120, 92),
                Size = new Size(240, 25),
                Text = "8888",
                Font = new Font("Arial", 10)
            };
            Controls.Add(txtPort);

            Button btnOk = new Button
            {
                Text = "Подключиться",
                Location = new Point(120, 135),
                Size = new Size(110, 35),
                BackColor = Color.LimeGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.Click += (s, e) =>
            {
                int port;
                if (!int.TryParse(txtPort.Text, out port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("Неверный порт!");
                    return;
                }
                Host = txtHost.Text.Trim();
                Port = port;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(250, 135),
                Size = new Size(110, 35),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }

}
