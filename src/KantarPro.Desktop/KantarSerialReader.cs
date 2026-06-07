using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KantarPro.Desktop
{
    public class ScaleWeightReceivedEventArgs : EventArgs
    {
        public ScaleWeightReceivedEventArgs(decimal weightKg, string rawFrame)
        {
            WeightKg = weightKg;
            RawFrame = rawFrame;
        }

        public decimal WeightKg { get; private set; }
        public string RawFrame { get; private set; }
    }

    public class KantarSerialReader : IDisposable
    {
        private static readonly Regex BaykonFrameRegex = new Regex(
            "[qQ].((?:\\s*[+-]?\\d+(?:[\\.,]\\d+)?){1,3})",
            RegexOptions.Compiled);

        private readonly object _syncRoot = new object();
        private readonly StringBuilder _buffer = new StringBuilder();
        private SerialPort _port;
        private System.Timers.Timer _pollTimer;
        private System.Timers.Timer _reconnectTimer;
        private string _configuredPortName;
        private int _reconnectAttempts;
        private bool _disposed;
        private const int MaxReconnectAttempts = 5;
        private const int ReconnectIntervalMilliseconds = 2000;

        public event EventHandler<ScaleWeightReceivedEventArgs> WeightReceived;
        public event EventHandler<string> ReadError;
        public event EventHandler<string> ConnectionStatusChanged;

        public bool IsOpen
        {
            get { return _port != null && _port.IsOpen; }
        }

        public string PortName
        {
            get { return _port != null ? _port.PortName : string.Empty; }
        }

        public void Start(string portName)
        {
            Stop();

            if (string.IsNullOrWhiteSpace(portName))
            {
                return;
            }

            _configuredPortName = portName.Trim();
            _reconnectAttempts = 0;
            OpenConfiguredPort();
        }

        private void OpenConfiguredPort()
        {
            if (string.IsNullOrWhiteSpace(_configuredPortName))
            {
                return;
            }

            _port = new SerialPort(_configuredPortName, 9600, Parity.None, 8, StopBits.One)
            {
                Encoding = Encoding.ASCII,
                ReadTimeout = 500,
                NewLine = "\r\n"
            };
            _port.DataReceived += Port_DataReceived;
            _port.Open();

            _pollTimer = new System.Timers.Timer(100);
            _pollTimer.Elapsed += PollTimer_Elapsed;
            _pollTimer.AutoReset = true;
            _pollTimer.Start();
        }

        public void Stop()
        {
            _configuredPortName = null;
            StopReconnectTimer();
            ClosePort();
        }

        private void ClosePort()
        {
            lock (_syncRoot)
            {
                if (_port == null)
                {
                    _buffer.Clear();
                    return;
                }

                try
                {
                    if (_pollTimer != null)
                    {
                        _pollTimer.Elapsed -= PollTimer_Elapsed;
                        _pollTimer.Stop();
                        _pollTimer.Dispose();
                        _pollTimer = null;
                    }

                    _port.DataReceived -= Port_DataReceived;
                    if (_port.IsOpen)
                    {
                        _port.Close();
                    }
                }
                finally
                {
                    _port.Dispose();
                    _port = null;
                    _buffer.Clear();
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Stop();
        }

        public static bool TryParseWeight(string rawData, out decimal weightKg)
        {
            weightKg = 0m;
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return false;
            }

            var matches = BaykonFrameRegex.Matches(rawData);
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var numberMatches = Regex.Matches(matches[i].Groups[1].Value, "[+-]?\\d+(?:[\\.,]\\d+)?");
                if (TryParseNumberAt(numberMatches, 1, out weightKg))
                {
                    return true;
                }

                if (TryParseNumberAt(numberMatches, 0, out weightKg))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<string> GetAvailablePortNames()
        {
            return SortPortNames(SerialPort.GetPortNames());
        }

        public static IEnumerable<string> SortPortNames(IEnumerable<string> portNames)
        {
            return (portNames ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct()
                .OrderBy(GetPortNumber)
                .ThenBy(x => x);
        }

        private static int GetPortNumber(string portName)
        {
            var match = Regex.Match(portName ?? string.Empty, "^COM(\\d+)$", RegexOptions.IgnoreCase);
            int value;
            return match.Success && int.TryParse(match.Groups[1].Value, out value)
                ? value
                : int.MaxValue;
        }

        private static bool TryParseNumberAt(MatchCollection matches, int index, out decimal value)
        {
            value = 0m;
            if (matches == null || matches.Count <= index)
            {
                return false;
            }

            var text = matches[index].Value.Replace(',', '.');
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            ReadPortBuffer();
        }

        private void PollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ReadPortBuffer();
        }

        private void ReadPortBuffer()
        {
            try
            {
                string data;
                lock (_syncRoot)
                {
                    if (_port == null || !_port.IsOpen)
                    {
                        return;
                    }

                    data = _port.ReadExisting();
                    if (string.IsNullOrEmpty(data))
                    {
                        return;
                    }

                    _buffer.Append(data);
                    if (_buffer.Length > 4000)
                    {
                        _buffer.Remove(0, _buffer.Length - 2000);
                    }

                    data = _buffer.ToString();
                }

                decimal weight;
                if (TryParseWeight(data, out weight))
                {
                    OnWeightReceived(weight, data);
                }
            }
            catch (Exception ex)
            {
                OnReadError(ex.Message);
                ScheduleReconnect(ex.Message);
            }
        }

        private void ScheduleReconnect(string reason)
        {
            if (_disposed || string.IsNullOrWhiteSpace(_configuredPortName))
            {
                return;
            }

            lock (_syncRoot)
            {
                if (_reconnectTimer != null || _reconnectAttempts >= MaxReconnectAttempts)
                {
                    return;
                }
            }

            ClosePort();
            OnConnectionStatusChanged("COM yeniden bağlanma bekliyor: " + reason);

            lock (_syncRoot)
            {
                if (_reconnectTimer != null || _disposed)
                {
                    return;
                }

                _reconnectTimer = new System.Timers.Timer(ReconnectIntervalMilliseconds);
                _reconnectTimer.Elapsed += ReconnectTimer_Elapsed;
                _reconnectTimer.AutoReset = false;
                _reconnectTimer.Start();
            }
        }

        private void ReconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StopReconnectTimer();

            if (_disposed || string.IsNullOrWhiteSpace(_configuredPortName))
            {
                return;
            }

            _reconnectAttempts++;
            try
            {
                OpenConfiguredPort();
                OnConnectionStatusChanged(_configuredPortName + " yeniden bağlandı.");
                _reconnectAttempts = 0;
            }
            catch (Exception ex)
            {
                OnReadError("COM yeniden bağlanma denemesi " + _reconnectAttempts + "/" + MaxReconnectAttempts + " başarısız: " + ex.Message);
                if (_reconnectAttempts < MaxReconnectAttempts)
                {
                    ScheduleReconnect(ex.Message);
                }
                else
                {
                    OnConnectionStatusChanged("COM yeniden bağlanma denemeleri durduruldu.");
                }
            }
        }

        private void StopReconnectTimer()
        {
            lock (_syncRoot)
            {
                if (_reconnectTimer == null)
                {
                    return;
                }

                _reconnectTimer.Elapsed -= ReconnectTimer_Elapsed;
                _reconnectTimer.Stop();
                _reconnectTimer.Dispose();
                _reconnectTimer = null;
            }
        }

        private void OnWeightReceived(decimal weightKg, string rawFrame)
        {
            var handler = WeightReceived;
            if (handler != null)
            {
                handler(this, new ScaleWeightReceivedEventArgs(weightKg, rawFrame));
            }
        }

        private void OnReadError(string message)
        {
            var handler = ReadError;
            if (handler != null)
            {
                handler(this, message);
            }
        }

        private void OnConnectionStatusChanged(string message)
        {
            var handler = ConnectionStatusChanged;
            if (handler != null)
            {
                handler(this, message);
            }
        }
    }
}



