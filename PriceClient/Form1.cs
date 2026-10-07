namespace PriceClient
{
    public partial class Form1 : Form
    {
        private UdpPriceClient client;
        private string host;
        private int port;

        private ListBox lstParts;
        private Button btnRequest;
        private Button btnRefresh;
        private PictureBox picImage;
        private Label lblPartName;
        private Label lblPrice;
        private TextBox txtLog;
        private Label lblStatus;
        private Label lblRequestCount;
        private int requestCount = 0;

        private readonly string[] knownParts =
        {
            "Intel Core i9-13900K",
            "AMD Ryzen 9 7950X",
            "NVIDIA RTX 4090",
            "NVIDIA RTX 4070 Ti",
            "AMD Radeon RX 7900 XTX",
            "Samsung 990 PRO 2TB",
            "WD Black SN850X 1TB",
            "Corsair Vengeance 32GB DDR5",
            "G.Skill Trident Z5 64GB DDR5",
            "ASUS ROG Maximus Z790",
            "MSI MAG B650 Tomahawk",
            "Seasonic PRIME TX-1000"
        };

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();

            var connectForm = new ConnectForm();
            if (connectForm.ShowDialog() != DialogResult.OK)
            {
                Load += (s, e) => Close();
                return;
            }

            host = connectForm.Host;
            port = connectForm.Port;

            Text = $"Клиент — {host}:{port}";
            ConnectToServer();
        }

        private void InitializeCustomComponents()
        {
            Size = new Size(1050, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Label lblTitle = new Label
            {
                Text = "ЦЕНЫ НА КОМПЛЕКТУЮЩИЕ",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(360, 10),
                AutoSize = true,
                ForeColor = Color.DarkBlue
            };
            Controls.Add(lblTitle);

            lblStatus = new Label
            {
                Text = "Статус: подключение...",
                Location = new Point(20, 45),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Orange
            };
            Controls.Add(lblStatus);

            lblRequestCount = new Label
            {
                Text = "Запросов сделано: 0",
                Location = new Point(750, 45),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            Controls.Add(lblRequestCount);

            Label lblParts = new Label
            {
                Text = "ДОСТУПНЫЕ КОМПЛЕКТУЮЩИЕ:",
                Location = new Point(20, 80),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            Controls.Add(lblParts);

            lstParts = new ListBox
            {
                Location = new Point(20, 105),
                Size = new Size(400, 400),
                Font = new Font("Arial", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            foreach (var p in knownParts) lstParts.Items.Add(p);
            if (lstParts.Items.Count > 0) lstParts.SelectedIndex = 0;
            lstParts.DoubleClick += async (s, e) => await RequestSelectedAsync();
            Controls.Add(lstParts);

            btnRequest = new Button
            {
                Text = "Запросить цену",
                Location = new Point(20, 515),
                Size = new Size(200, 40),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnRequest.Click += async (s, e) => await RequestSelectedAsync();
            Controls.Add(btnRequest);

            btnRefresh = new Button
            {
                Text = "Ping сервера",
                Location = new Point(230, 515),
                Size = new Size(190, 40),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnRefresh.Click += async (s, e) =>
            {
                try { await client.RequestPriceAsync("__PING__"); }
                catch { }
            };
            Controls.Add(btnRefresh);

            Label lblResult = new Label
            {
                Text = "РЕЗУЛЬТАТ:",
                Location = new Point(440, 80),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            Controls.Add(lblResult);

            picImage = new PictureBox
            {
                Location = new Point(440, 105),
                Size = new Size(300, 180),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };
            Controls.Add(picImage);

            lblPartName = new Label
            {
                Location = new Point(440, 295),
                Size = new Size(580, 25),
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                Text = ""
            };
            Controls.Add(lblPartName);

            lblPrice = new Label
            {
                Location = new Point(440, 325),
                Size = new Size(580, 40),
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                Text = ""
            };
            Controls.Add(lblPrice);

            Label lblLog = new Label
            {
                Text = "ЛОГ:",
                Location = new Point(440, 375),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            Controls.Add(lblLog);

            txtLog = new TextBox
            {
                Location = new Point(440, 400),
                Size = new Size(580, 155),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.LightGreen
            };
            Controls.Add(txtLog);
        }

        private async void ConnectToServer()
        {
            client = new UdpPriceClient();
            client.LogMessage += Client_LogMessage;
            client.ResponseReceived += Client_ResponseReceived;

            try
            {
                await client.ConnectAsync(host, port);
                lblStatus.Text = $"Статус: подключено к {host}:{port}";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Статус: ошибка подключения";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private async Task RequestSelectedAsync()
        {
            if (lstParts.SelectedItem == null) return;
            if (!client.IsConnected)
            {
                MessageBox.Show("Нет соединения с сервером!");
                return;
            }

            string partName = lstParts.SelectedItem.ToString();
            btnRequest.Enabled = false;

            try
            {
                await client.RequestPriceAsync(partName);
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка отправки: {ex.Message}");
            }
            finally
            {
                btnRequest.Enabled = true;
            }
        }

        private void Client_LogMessage(object sender, string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Client_LogMessage(sender, message)));
                return;
            }
            AppendLog(message);
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        private void Client_ResponseReceived(object sender, PriceResponse response)
        {
            if (lblPartName.InvokeRequired)
            {
                lblPartName.Invoke(new Action(() => Client_ResponseReceived(sender, response)));
                return;
            }

            requestCount++;
            lblRequestCount.Text = $"Запросов сделано: {requestCount}";

            if (response.Success)
            {
                lblPartName.Text = response.PartName;
                lblPrice.Text = $"${response.Price:F2}";
                lblPrice.ForeColor = Color.DarkGreen;

                if (response.ImageBytes != null && response.ImageBytes.Length > 0)
                {
                    try
                    {
                        using (var ms = new MemoryStream(response.ImageBytes))
                        {
                            picImage.Image?.Dispose();
                            picImage.Image = Image.FromStream(ms);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Ошибка изображения: {ex.Message}");
                    }
                }

                AppendLog($"Получено: {response.PartName} — ${response.Price:F2} " +
                          $"(изображение: {response.ImageBytes?.Length ?? 0} байт)");
            }
            else if (response.RateLimited)
            {
                lblPartName.Text = "⚠ Лимит превышен";
                lblPrice.Text = "";
                picImage.Image = null;
                AppendLog($"RATE LIMIT: {response.Message}");
                MessageBox.Show(response.Message, "Превышен лимит запросов",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                lblPartName.Text = "Ошибка";
                lblPrice.Text = "";
                picImage.Image = null;
                AppendLog($"Ошибка: {response.Error}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (client != null) client.Disconnect();
            base.OnFormClosing(e);
        }
    }
}
