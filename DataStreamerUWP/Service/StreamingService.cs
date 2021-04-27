﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED AS IS WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DataStreamer.UWP
{
    /// <summary>
    /// Base class for services that stream data to Data Streamer. This base class will stream data from a supplied repository.
    /// <summary>
    public abstract class StreamingService : IStreamingService
    {
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private readonly IStreamingRepository _repository;
        private volatile int _delay = 20; // In ms

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="repository">Source of data</param>
        /// <param name="delay">Delay or interval at which data is sent in milliseconds</param>
        public StreamingService(IStreamingRepository repository, int delay = 20)
        {
            _repository = repository;
            _delay = delay;
        }

        public virtual async Task<string> Connect(IDictionary<string, object> mesage)
        {
            return await Task.FromResult(JsonSerializer.Serialize(_repository.Manifest));
        }

        public IStreamingRepository Repository => _repository;

        public virtual async Task Disconnect()
        {
            await StopData(true);
        }

        public int Delay { get { return _delay; } set {_delay = value; } }

        public virtual async Task StartData()
        {
            _cancellationToken = new CancellationTokenSource();

            Task.Run( async ()=>
            {
                while(!_cancellationToken.Token.IsCancellationRequested)
                {
                    await SendData(await _repository.GetData());

                    await Task.Delay(_delay, _cancellationToken.Token);
                }

                _cancellationToken?.Dispose();
                _cancellationToken = null;

            }).FireAndForget();

            await Task.CompletedTask;
        }

        public virtual async Task StopData(bool disconnecting = false)
        {
            _cancellationToken?.Cancel();
            await Task.CompletedTask;
        }

        protected abstract Task SendData(string data);

        public abstract Task StartRecording(string fileName, Func<string, Task> fnOnError = null);
        public abstract Task StopRecording(Func<string, Task> fnOnError = null);
        public abstract Task Reset();
        public abstract Task Ready();
        public abstract Task NotReady();
        public abstract Task UpdateManifest(Func<string, Task> fnOnError = null);
        public abstract Task OnEvent(IDictionary<string, object> message);
        public abstract Task OnCommand(string command, dynamic parms);
    }
}
