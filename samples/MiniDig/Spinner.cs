// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigApp
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public class Spiner
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private int _msgLength = 4;
        private string _msg = string.Empty;
        private int _statusLength = 4;
        private string _status = string.Empty;
        private ConsoleColor _oldColor;
        private CancellationTokenSource _source;
        private CancellationToken _token;

        public string Message
        {
            get { return _msg; }
            set
            {
                if (value != null)
                {
                    _msgLength = _msgLength > value.Length ? _msgLength : value.Length;
                }

                _msg = value;
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                if (value != null)
                {
                    _statusLength = _statusLength > value.Length ? _statusLength : value.Length;
                }

                _status = value;
            }
        }

        public void Start()
        {
            Console.CursorVisible = false;
            _oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            _source?.Dispose();
            _source = new CancellationTokenSource();
            _token = _source.Token;
            Task.Run(Spin, _token);
        }

        public void Stop()
        {
            _source.Cancel();

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(string.Join("", Enumerable.Repeat(" ", Console.BufferWidth)));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.CursorVisible = true;
            Console.ForegroundColor = _oldColor;
        }

        private async Task Spin()
        {
            var chars = new Queue<string>(["|", "/", "-", "\\"]);

            while (true)
            {
                _token.ThrowIfCancellationRequested();
                var chr = chars.Dequeue();
                chars.Enqueue(chr);
                Console.CursorVisible = false;
                Console.ForegroundColor = ConsoleColor.Yellow;
                var msg = string.Format(
                    CultureInfo.InvariantCulture,
                    "{{{0}}} {1,-" + _msgLength + "}{2,-" + _statusLength + "}",
                    chr,
                    Message,
                    Status);

                if (msg.Length >= Console.BufferWidth)
                {
                    msg = msg.Substring(0, Console.BufferWidth - 1);
                }
                Console.Write(msg);
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.ForegroundColor = _oldColor;
                Console.CursorVisible = true;
                await Task.Delay(100, _token).ConfigureAwait(false);
            }
        }
    }
}
