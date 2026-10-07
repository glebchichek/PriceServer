using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PriceClient
{
    public class PriceResponse
    {
        public bool Success { get; set; }
        public string PartName { get; set; }
        public decimal Price { get; set; }
        public byte[] ImageBytes { get; set; }
        public string Error { get; set; }
        public bool RateLimited { get; set; }
        public string Message { get; set; }
    }
    public class UdpPriceClient
    {
        private UdpClient udpClient;
        private IPEndPoint serverEndPoint;
        private CancellationTokenSource cts;
        private bool isConnected;

        public event EventHandler<string> LogMessage;
        public event EventHandler<PriceResponse> ResponseReceived;

        public bool IsConnected => isConnected;

        public async Task ConnectAsync(string host, int port)
        {
            serverEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            udpClient = new UdpClient();
            udpClient.Connect(serverEndPoint);

            isConnected = true;
            cts = new CancellationTokenSource();

            RaiseLog($"Клиент запущен, сервер: {host}:{port}");

            _ = Task.Run(() => ReceiveLoop(cts.Token));

            await SendRawAsync("PING|");
            await Task.Delay(100);
        }

        public void Disconnect()
        {
            if (!isConnected) return;

            isConnected = false;
            cts?.Cancel();
            udpClient?.Close();
            RaiseLog("Клиент отключён");
        }

        public async Task RequestPriceAsync(string partName)
        {
            if (!isConnected) throw new InvalidOperationException("Не подключены");

            string msg = $"GET|{partName}";
            RaiseLog($"Запрос: {partName}");
            await SendRawAsync(msg);
        }

        private async Task SendRawAsync(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            await udpClient.SendAsync(data, data.Length);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync();
                    ProcessResponse(result.Buffer);
                }
                catch (ObjectDisposedException) { break; }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        RaiseLog($"Ошибка приёма: {ex.Message}");
                }
            }
        }

        private void ProcessResponse(byte[] data)
        {
            int newlineIndex = -1;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == (byte)'\n') { newlineIndex = i; break; }
            }

            string header;
            byte[] imageBytes = null;

            if (newlineIndex >= 0)
            {
                header = Encoding.UTF8.GetString(data, 0, newlineIndex);
                int imageStart = newlineIndex + 1;
                int imageLen = data.Length - imageStart;
                if (imageLen > 0)
                {
                    imageBytes = new byte[imageLen];
                    Buffer.BlockCopy(data, imageStart, imageBytes, 0, imageLen);
                }
            }
            else
            {
                header = Encoding.UTF8.GetString(data);
            }
            string[] parts = header.Split('|');
            string cmd = parts[0].ToUpperInvariant();

            PriceResponse response = new PriceResponse();

            switch (cmd)
            {
                case "PRICE":
                    response.Success = true;
                    response.PartName = parts.Length > 1 ? parts[1] : "";
                    decimal price;
                    if (parts.Length > 2 && decimal.TryParse(parts[2],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out price))
                    {
                        response.Price = price;
                    }
                    response.ImageBytes = imageBytes;
                    break;

                case "ERROR":
                    response.Success = false;
                    response.Error = parts.Length > 1 ? parts[1] : "Неизвестная ошибка";
                    break;

                case "RATELIMIT":
                    response.Success = false;
                    response.RateLimited = true;
                    response.Message = parts.Length > 1 ? parts[1] : "Превышен лимит";
                    break;

                default:
                    response.Success = false;
                    response.Error = $"Неизвестный ответ: {header}";
                    break;
            }

            ResponseReceived?.Invoke(this, response);
        }

        private void RaiseLog(string message)
        {
            LogMessage?.Invoke(this, message);
        }
    }
}
