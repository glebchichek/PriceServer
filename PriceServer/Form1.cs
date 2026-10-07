namespace PriceServer
{
    public partial class Form1 : Form
    {
        private UdpPriceServer server;
        private TextBox txtLog;
        private TextBox txtPort;
        private NumericUpDown nudMaxRequests;
        private NumericUpDown nudIdleMinutes;
        private Button btnStart;
        private Button btnStop;
        private Label lblStatus;
        private ListView lvClients;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();

            server = new UdpPriceServer();
            server.LogMessage += Server_LogMessage;
            server.ClientsChanged += Server_ClientsChanged;
        }

        private void InitializeCustomComponents()
        {
            Text = "Сервер цен на комплектующие";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Label lblTitle = new Label
            {
                Text = "СЕРВЕР ЦЕН НА КОМПЛЕКТУЮЩИЕ",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(280, 10),
                AutoSize = true,
                ForeColor = Color.DarkBlue
            };
            Controls.Add(lblTitle);

            Panel panel = new Panel
            {
                Location = new Point(20, 50),
                Size = new Size(950, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            Controls.Add(panel);

            Label lblPort = new Label
            {
                Text = "Порт:",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Arial", 10)
            };
            panel.Controls.Add(lblPort);

            txtPort = new TextBox
            {
                Location = new Point(70, 12),
                Size = new Size(80, 25),
                Text = "8888",
                Font = new Font("Arial", 10)
            };
            panel.Controls.Add(txtPort);

            Label lblMax = new Label
            {
                Text = "Лимит (зап/час):",
                Location = new Point(170, 15),
                AutoSize = true,
                Font = new Font("Arial", 10)
            };
            panel.Controls.Add(lblMax);

            nudMaxRequests = new NumericUpDown
            {
                Location = new Point(300, 12),
                Size = new Size(70, 25),
                Minimum = 1,
                Maximum = 1000,
                Value = 10,
                Font = new Font("Arial", 10)
            };
            panel.Controls.Add(nudMaxRequests);

            Label lblIdle = new Label
            {
                Text = "Тайм-аут (мин):",
                Location = new Point(390, 15),
                AutoSize = true,
                Font = new Font("Arial", 10)
            };
            panel.Controls.Add(lblIdle);

            nudIdleMinutes = new NumericUpDown
            {
                Location = new Point(510, 12),
                Size = new Size(70, 25),
                Minimum = 1,
                Maximum = 120,
                Value = 10,
                Font = new Font("Arial", 10)
            };
            panel.Controls.Add(nudIdleMinutes);

            btnStart = new Button
            {
                Text = "Запустить",
                Location = new Point(610, 10),
                Size = new Size(130, 30),
                BackColor = Color.LimeGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnStart.Click += BtnStart_Click;
            panel.Controls.Add(btnStart);

            btnStop = new Button
            {
                Text = "Остановить",
                Location = new Point(750, 10),
                Size = new Size(130, 30),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnStop.Click += (s, e) => server.Stop();
            panel.Controls.Add(btnStop);

            lblStatus = new Label
            {
                Text = "Статус: Сервер не запущен",
                Location = new Point(15, 55),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Red
            };
            panel.Controls.Add(lblStatus);

            Label lblLog = new Label
            {
                Text = "ЛОГ СЕРВЕРА:",
                Location = new Point(20, 165),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            Controls.Add(lblLog);

            txtLog = new TextBox
            {
                Location = new Point(20, 190),
                Size = new Size(650, 460),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.LightGreen
            };
            Controls.Add(txtLog);

            Label lblClients = new Label
            {
                Text = "ПОДКЛЮЧЁННЫЕ КЛИЕНТЫ:",
                Location = new Point(690, 165),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            Controls.Add(lblClients);

            lvClients = new ListView
            {
                Location = new Point(690, 190),
                Size = new Size(280, 460),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Arial", 9),
                BackColor = Color.White
            };
            lvClients.Columns.Add("Клиент", 150);
            lvClients.Columns.Add("Запросов/час", 120);
            Controls.Add(lvClients);
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            int port;
            if (!int.TryParse(txtPort.Text, out port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Введите корректный порт!", "Ошибка");
                return;
            }

            try
            {
                server.MaxRequestsPerHour = (int)nudMaxRequests.Value;
                server.IdleTimeout = TimeSpan.FromMinutes((double)nudIdleMinutes.Value);

                server.Start(port);

                btnStart.Enabled = false;
                btnStop.Enabled = true;
                txtPort.Enabled = false;
                nudMaxRequests.Enabled = false;
                nudIdleMinutes.Enabled = false;
                lblStatus.Text = $"Статус: Сервер запущен на порту {port}";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Server_LogMessage(object sender, string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Server_LogMessage(sender, message)));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        private void Server_ClientsChanged(object sender, EventArgs e)
        {
            if (lvClients.InvokeRequired)
            {
                lvClients.Invoke(new Action(() => Server_ClientsChanged(sender, e)));
                return;
            }

            lvClients.Items.Clear();
            var clients = server.GetClients();

            foreach (var c in clients)
            {
                var item = new ListViewItem(c.EndPoint);
                item.SubItems.Add($"{c.RequestsInLastHour()}/{server.MaxRequestsPerHour}");
                lvClients.Items.Add(item);
            }

            if (!server.IsRunning)
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                txtPort.Enabled = true;
                nudMaxRequests.Enabled = true;
                nudIdleMinutes.Enabled = true;
                lblStatus.Text = "Статус: Сервер остановлен";
                lblStatus.ForeColor = Color.Red;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (server.IsRunning) server.Stop();
            base.OnFormClosing(e);
        }
    }
}
