using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PriceServer
{
    public class UdpPriceServer
    {
        private UdpClient udpServer;
        private CancellationTokenSource cts;
        private bool isRunning;

        private readonly Dictionary<string, ComputerPart> parts =
            new Dictionary<string, ComputerPart>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ClientInfo> clients =
            new Dictionary<string, ClientInfo>();
        private readonly object clientsLock = new object();

        public int MaxRequestsPerHour { get; set; } = 10;
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(10);
        public int MaxImageSizeBytes { get; set; } = 60 * 1024;

        public event EventHandler<string> LogMessage;
        public event EventHandler ClientsChanged;

        public bool IsRunning => isRunning;
        public int Port { get; private set; }

        public UdpPriceServer()
        {
            InitializeParts();
        }

        private void InitializeParts()
        {
            AddPart("Intel Core i9-13900K", 589.99m, "24 ядра, 5.8 GHz");
            AddPart("AMD Ryzen 9 7950X", 549.00m, "16 ядер, 5.7 GHz");
            AddPart("NVIDIA RTX 4090", 1599.00m, "24 GB GDDR6X");
            AddPart("NVIDIA RTX 4070 Ti", 799.00m, "12 GB GDDR6X");
            AddPart("AMD Radeon RX 7900 XTX", 999.00m, "24 GB GDDR6");
            AddPart("Samsung 990 PRO 2TB", 189.99m, "NVMe M.2, 7450 MB/s");
            AddPart("WD Black SN850X 1TB", 109.99m, "NVMe M.2, 7300 MB/s");
            AddPart("Corsair Vengeance 32GB DDR5", 149.99m, "2x16GB, 6000 MHz");
            AddPart("G.Skill Trident Z5 64GB DDR5", 279.99m, "2x32GB, 6400 MHz");
            AddPart("ASUS ROG Maximus Z790", 699.00m, "LGA1700, DDR5");
            AddPart("MSI MAG B650 Tomahawk", 219.99m, "AM5, DDR5");
            AddPart("Seasonic PRIME TX-1000", 329.99m, "1000W, 80+ Titanium");
        }

        private void AddPart(string name, decimal price, string description)
        {
            parts[name] = new ComputerPart
            {
                Name = name,
                Price = price,
                Description = description,
                ImageBytes = GeneratePartImage(name, price)
            };
        }
        private byte[] GeneratePartImage(string name, decimal price)
        {
            using (var bmp = new Bitmap(200, 120))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(240, 240, 250));
                g.DrawRectangle(Pens.SteelBlue, 1, 1, 197, 117);

                using (var font = new Font("Arial", 9, FontStyle.Bold))
                using (var small = new Font("Arial", 8))
                {
                    g.DrawString("Комплектующая", font, Brushes.SteelBlue, 10, 8);

                    string shortName = name.Length > 25
                        ? name.Substring(0, 22) + "..."
                        : name;
                    g.DrawString(shortName, small, Brushes.Black, 10, 35);

                    using (var priceFont = new Font("Arial", 14, FontStyle.Bold))
                        g.DrawString($"${price:F2}", priceFont, Brushes.DarkGreen, 10, 60);

                    g.DrawString($"Размер: 200x120", small, Brushes.Gray, 10, 95);
                }

                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        public List<string> GetPartNames()
        {
            return new List<string>(parts.Keys);
        }

        public void Start(int port)
        {
            if (isRunning) throw new InvalidOperationException("Уже запущен");

            Port = port;
            udpServer = new UdpClient(port);
            isRunning = true;
            cts = new CancellationTokenSource();

            RaiseLog($"Сервер запущен на UDP-порту {port}");
            RaiseLog($"Комплектующих в базе: {parts.Count}");
            RaiseLog($"Лимит: {MaxRequestsPerHour} запросов/час на клиента");
            RaiseLog($"Тайм-аут неактивности: {IdleTimeout.TotalMinutes} мин");

            Task.Run(() => ReceiveLoop(cts.Token));
            Task.Run(() => IdleCheckLoop(cts.Token));
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            cts?.Cancel();
            udpServer?.Close();

            lock (clientsLock) clients.Clear();

            RaiseLog("Сервер остановлен");
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await udpServer.ReceiveAsync();
                    _ = Task.Run(() => HandleRequestAsync(result, token), token);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        RaiseLog($"Ошибка приёма: {ex.Message}");
                }
            }
        }
        private async Task IdleCheckLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), token);

                    List<string> toRemove = new List<string>();
                    lock (clientsLock)
                    {
                        foreach (var kvp in clients)
                        {
                            if (kvp.Value.IsIdle(IdleTimeout))
                                toRemove.Add(kvp.Key);
                        }
                        foreach (var ep in toRemove)
                        {
                            clients.Remove(ep);
                            RaiseLog($"Клиент отключён по тайм-ауту: {ep}");
                        }
                    }

                    if (toRemove.Count > 0)
                        ClientsChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    RaiseLog($"Ошибка IdleCheck: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(UdpReceiveResult result, CancellationToken token)
        {
            string endPoint = result.RemoteEndPoint.ToString();
            string request = Encoding.UTF8.GetString(result.Buffer).Trim();

            ClientInfo client;
            bool isNew = false;
            lock (clientsLock)
            {
                if (!clients.TryGetValue(endPoint, out client))
                {
                    client = new ClientInfo(endPoint);
                    clients[endPoint] = client;
                    isNew = true;
                }
            }

            if (isNew)
            {
                RaiseLog($"Новый клиент: {endPoint}");
                ClientsChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                client.LastActivity = DateTime.Now;
            }
            string[] parts = request.Split(new[] { '|' }, 2);
            string cmd = parts[0].ToUpperInvariant();
            string arg = parts.Length > 1 ? parts[1] : "";

            switch (cmd)
            {
                case "GET":
                    await ProcessGetRequest(client, arg, result.RemoteEndPoint);
                    break;

                case "PING":
                    break;

                default:
                    await SendTextAsync(result.RemoteEndPoint, $"ERROR|Неизвестная команда: {cmd}");
                    break;
            }
        }

        private async Task ProcessGetRequest(ClientInfo client, string partName,
            IPEndPoint clientEndPoint)
        {
            int waitSec = client.SecondsUntilNextAllowed();
            if (waitSec > 0)
            {
                RaiseLog($"[RATE LIMIT] {client.EndPoint} подождать {waitSec} сек");
                await SendTextAsync(clientEndPoint,
                    $"RATELIMIT|Превышен лимит ({MaxRequestsPerHour}/час). Подождите {waitSec} сек");
                return;
            }

            if (!parts.TryGetValue(partName, out ComputerPart part))
            {
                RaiseLog($"[НЕ НАЙДЕНО] {client.EndPoint} запросил \"{partName}\"");
                await SendTextAsync(clientEndPoint, $"ERROR|Комплектующая не найдена: {partName}");
                client.RecordRequest();
                return;
            }
            client.RecordRequest();
            int used = client.RequestsInLastHour();

            RaiseLog($"[ЗАПРОС] {client.EndPoint} -> {partName} (${part.Price:F2}) " +
                     $"[{used}/{MaxRequestsPerHour} за час]");

            string header = $"PRICE|{part.Name}|{part.Price:F2}|{part.ImageBytes.Length}\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);

            byte[] packet = new byte[headerBytes.Length + part.ImageBytes.Length];
            Buffer.BlockCopy(headerBytes, 0, packet, 0, headerBytes.Length);
            Buffer.BlockCopy(part.ImageBytes, 0, packet, headerBytes.Length, part.ImageBytes.Length);


            if (packet.Length > 60000)
            {
                RaiseLog($"[ОШИБКА] Изображение слишком большое: {packet.Length} байт");
                await SendTextAsync(clientEndPoint,
                    "ERROR|Изображение слишком большое для отправки");
                return;
            }

            await udpServer.SendAsync(packet, packet.Length, clientEndPoint);
        }

        private async Task SendTextAsync(IPEndPoint target, string text)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(text);
                await udpServer.SendAsync(data, data.Length, target);
            }
            catch { }
        }

        public List<ClientInfo> GetClients()
        {
            lock (clientsLock)
                return new List<ClientInfo>(clients.Values);
        }

        private void RaiseLog(string message)
        {
            LogMessage?.Invoke(this, message);
        }
    }
}
