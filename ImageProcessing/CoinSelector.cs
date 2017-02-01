using System.Timers;
using whCcTalkCommunication;

namespace ImageProcessing
{
    public enum CoinSelectorError
    {
        Undefined,
        OK,
        NoDeviceFound,
        NoConnection,
        ConnectionFailed,
        GettinCoins,
        CoinEnableError,
    }
    public struct CoinValues
    {
        public const double TRY005 = 0.05;
        public const double TRY010 = 0.10;
        public const double TRY025 = 0.25;
        public const double TRY050 = 0.50;
        public const double TRY100 = 1.00;
    }
    public class CoinSelector
    {
        private whCcTalkDeviceList _deviceOps;
        private whSearchOptions _searchOptions = whSearchOptions.SearchCcTalk;
        private whPortTypes _portTypes = whPortTypes.USB;
        private whSelectorComm _currentDevice;
        private whCoinValue[] _coinValues;
        private whSelCoinStatus[] _selCoinStates;
        private Timer _tmrPoll;
        private int[] _totalCoinCount;
        private double _totalCoin;
        private bool _tmrPoolEnabled;
        private int _deviceCount
        {
            get
            {
                return this._deviceOps.Count;
            }
        }
        public CoinSelectorError LastError { get; private set; }
        public bool ConnectionStatus
        {
            get
            {
                if (this._currentDevice != null) return this._currentDevice.IsOpen;
                else return false;
            }
        }
        public double PollingInterval
        {
            get
            {
                return this._tmrPoll.Interval;
            }
            set
            {
                this._tmrPoll.Interval = value;
            }
        }
        public bool PollingEnabled
        {
            get
            {
                return this._tmrPoolEnabled;
            }
            set
            {
                this._tmrPoll.Enabled = this._tmrPoolEnabled = value;
            }
        }
        public bool Connected { get; private set; }
        /// <summary>
        /// Gets total conin value,
        /// Sets to 0 only
        /// </summary>
        public double TotalCoin
        {
            get
            {
                return this._totalCoin;
            }
            set
            {
                this._totalCoin = 0;
                for (int i = 0; i < this._totalCoinCount.Length; i++)
                    this._totalCoinCount[i] = 0;
            }
        }
        public delegate void CoinDetectedEventHandler(object eventObject, CoinDetectedEventArgs args);
        public event CoinDetectedEventHandler OnCoinDetected;
        public CoinSelector()
        {
            //Device Operations
            this._deviceOps = new whCcTalkDeviceList();
            this._deviceOps.Options = _searchOptions;
            this._deviceOps.InDepthSearch = false;
            this._deviceOps.SearchPortType = _portTypes;

            //Polling Timer
            this._tmrPoll = new Timer();
            this._tmrPoll.Interval = 100;
            this._tmrPoll.Enabled = false;
            this._tmrPoolEnabled = false;
            this._tmrPoll.Elapsed += _tmrPoll_Elapsed;
            this._tmrPoll.Stop();
            //Coin Values
            this._coinValues = new whCoinValue[16];
            this._selCoinStates = new whSelCoinStatus[16];
            this._totalCoinCount = new int[16];
            for (int i = 0; i < this._totalCoinCount.Length; i++)
                this._totalCoinCount[i] = 0;
            this._totalCoin = 0;

            this.LastError = CoinSelectorError.OK;
            this.Connected = false;
        }

        public CoinSelectorError SearchDevices()
        {
            CoinSelectorError error = CoinSelectorError.OK;
            this._deviceOps.SearchDevices(new byte[] { 1, 2, 3, 4, 5, 6 });
            if (_deviceCount == 0) error = CoinSelectorError.NoDeviceFound;
            else
            {
                this._currentDevice = new whSelectorComm();
                this._currentDevice.Port = this._deviceOps.CcTalkDevices[0].Port;
                this._currentDevice.Address = this._deviceOps.CcTalkDevices[0].Address;
                this._currentDevice.ChecksumType = this._deviceOps.CcTalkDevices[0].ChecksumType;
                this._currentDevice.EncryptionMode = this._deviceOps.CcTalkDevices[0].EncryptionMode;
                this._currentDevice.InitPINCode(this._deviceOps.CcTalkDevices[0].GetPINCode());
            }
            this.LastError = error;
            return error;
        }

        public CoinSelectorError SetEnabledCoins(double[] values)
        {
            CoinSelectorError error = CoinSelectorError.OK;
            if (this.ConnectionStatus == false) error = CoinSelectorError.NoConnection;
            else
            {
                whSelCoinStatus[] coinStates = this._setCoinStates(values);
                if (this._currentDevice.SetCoinInhibit(coinStates) == whCcTalkErrors.Ok)
                {
                    this._selCoinStates = coinStates;
                }
                else
                    error = CoinSelectorError.CoinEnableError;
            }
            this.LastError = error;
            return error;
        }
        private whSelCoinStatus[] _setCoinStates(double[] values)
        {
            whSelCoinStatus[] coinStates = (whSelCoinStatus[])this._selCoinStates.Clone();
            for (int i = 0; i < this._selCoinStates.Length; i++)
                coinStates[i].Inhibit = false;
            foreach (double value in values)
            {
                for (int i = 0; i < this._selCoinStates.Length; i++)
                {
                    if (value == System.Math.Round(this._coinValues[i].Value, 2))
                        coinStates[i].Inhibit = true;
                }
            }
            return coinStates;
        }
        public CoinSelectorError ConnectDevice()
        {
            CoinSelectorError error = CoinSelectorError.OK;
            if (this._deviceCount == 0) error = CoinSelectorError.NoDeviceFound;
            else
            {
                if (this._currentDevice.OpenComm() != whCcTalkErrors.Ok) error = CoinSelectorError.ConnectionFailed;
                else
                {
                    if (this._currentDevice.GetCoinStates(ref _selCoinStates) != whCcTalkErrors.Ok) error = CoinSelectorError.GettinCoins;
                    if (this._currentDevice.GetCoinValues(ref _coinValues) != whCcTalkErrors.Ok) error = CoinSelectorError.GettinCoins;
                }
            }
            if (error == CoinSelectorError.OK)
                Connected = true;
            this.LastError = error;
            return error;
        }

        private void _tmrPoll_Elapsed(object sender, ElapsedEventArgs e)
        {
            int i, j, cidx, EvtCnt;
            whSelPollResponse[] PollResps = new whSelPollResponse[whSelectorComm.MaxPollEvents];

            this._tmrPoll.Enabled = false;

            this._currentDevice.PollSelector(ref PollResps, out EvtCnt);

            // Process poll response(s)
            if (EvtCnt > 0)
            {
                for (i = 0; i < EvtCnt; i++)
                {
                    // Show last poll
                    if (PollResps[i].Status == whSelPollEvent.Coin)
                    {
                        cidx = PollResps[i].CoinIndex;
                        // Show counts
                        this._totalCoinCount[cidx]++;
                        this._totalCoin = 0;
                        for (j = 0; j < this._totalCoinCount.Length; j++)
                            this._totalCoin += this._coinValues[j].Value * this._totalCoinCount[j];
                        CoinDetectedEventArgs args = new CoinDetectedEventArgs(this._totalCoin);
                        OnCoinDetected(this, args);
                    }

                }
            }
            if (this._tmrPoolEnabled)
                this._tmrPoll.Enabled = true;
        }
    }
    public class CoinDetectedEventArgs : System.EventArgs
    {
        private double _totalCoinValue;
        public double TotalCoinValue
        {
            get
            {
                return this._totalCoinValue;
            }
        }
        public CoinDetectedEventArgs(double coinValue)
        {
            this._totalCoinValue = coinValue;
        }
    }
}
