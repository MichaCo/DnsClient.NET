using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigApp
{
    public class Spiner
    {
        private int _maxLength = 4;
        private string _msg = string.Empty;
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
                    _maxLength = _maxLength > value.Length ? _maxLength : value.Length;
                }

                _msg = value;
            }
        }

        public void Start()
        {
            Console.CursorVisible = false;
            _oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;

            _source = new CancellationTokenSource();
            _token = _source.Token;
            Task.Run(Spin, _token);
        }

        public void Stop()
        {
            _source.Cancel();

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(Enumerable.Repeat(" ", _maxLength * 2));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.CursorVisible = true;
            Console.ForegroundColor = _oldColor;
        }

        private async Task Spin()
        {
            var chars = new Queue<string>(new[] { "|", "/", "-", "\\" });

            while (true)
            {
                _token.ThrowIfCancellationRequested();
                var chr = chars.Dequeue();
                chars.Enqueue(chr);
                Console.CursorVisible = false;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("{{{0}}} {1,-" + _maxLength + "}", chr, Message);
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.ForegroundColor = _oldColor;
                Console.CursorVisible = true;
                await Task.Delay(100, _token);
            }
        }
    }
}
