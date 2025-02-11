﻿using Newtonsoft.Json;
using NHM.Common;
using NHM.Common.Enums;
using NHM.MinerPlugin;
using NHM.MinerPluginToolkitV1;
using NHM.MinerPluginToolkitV1.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace XMRig
{
    public class XMRig : MinerBase, IBeforeStartMining, IDisposable
    {
        protected readonly HttpClient _httpClient = new HttpClient();

        public XMRig(string uuid) : base(uuid) { }

        public async override Task<ApiData> GetMinerStatsDataAsync()
        {
            var api = new ApiData();
            try
            {
                var result = await _httpClient.GetStringAsync($"http://127.0.0.1:{_apiPort}/1/summary");
                api.ApiResponse = result;
                var summary = JsonConvert.DeserializeObject<JsonApiResponse>(result);

                var totalSpeed = 0d;
                var perDeviceSpeedInfo = new Dictionary<string, IReadOnlyList<(AlgorithmType type, double speed)>>();
                var perDevicePowerInfo = new Dictionary<string, int>();
                // init per device sums
                foreach (var pair in _miningPairs)
                {
                    var uuid = pair.Device.UUID;
                    var currentSpeed = summary.hashrate.total.FirstOrDefault() ?? 0d;
                    totalSpeed += currentSpeed;
                    perDeviceSpeedInfo.Add(uuid, new List<(AlgorithmType type, double speed)>() { (_algorithmType, currentSpeed * (1 - DevFee * 0.01)) });
                    // no power usage info
                    perDevicePowerInfo.Add(uuid, -1);
                }

                api.AlgorithmSpeedsPerDevice = perDeviceSpeedInfo;
                api.PowerUsagePerDevice = perDevicePowerInfo;
                api.PowerUsageTotal = -1;
            }
            catch (Exception e)
            {
                Logger.Error(_logGroup, $"Error occured while getting API stats: {e.Message}");
            }

            return api;
        }

        protected override void Init()
        {
        }


        private static HashSet<string> _deleteConfigs = new HashSet<string> { "config.json" };
        private static bool IsDeleteConfigFile(string file)
        {
            foreach (var conf in _deleteConfigs)
            {
                if (file.Contains(conf)) return true;
            }
            return false;
        }
        void IBeforeStartMining.BeforeStartMining()
        {
            var binCwd = GetBinAndCwdPaths().Item2;
            var txtFiles = Directory.GetFiles(binCwd, "*.json", SearchOption.AllDirectories)
                .Where(file => IsDeleteConfigFile(file))
                .ToArray();
            foreach (var deleteFile in txtFiles)
            {
                try
                {
                    File.Delete(deleteFile);
                }
                catch (Exception e)
                {
                    Logger.Error(_logGroup, $"BeforeStartMining error while deleting file '{deleteFile}': {e.Message}");
                }
            }
        }
        private bool _disposed = false;
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                try
                {
                    _httpClient.Dispose();
                }
                catch (Exception) { }
            }
            _disposed = true;
        }
        ~XMRig()
        {
            Dispose(false);
        }
    }
}
