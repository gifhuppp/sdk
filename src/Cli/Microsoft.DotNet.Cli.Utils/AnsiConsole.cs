// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Cli.Utils
{
    public class AnsiConsole
    {
        private const int Light = 0x08;
        private readonly bool _ansiEnabled;

        private AnsiConsole(TextWriter writer)
        {
            Writer = writer;

            OriginalForegroundColor = Console.ForegroundColor;
            _boldRecursion = ((int)OriginalForegroundColor & Light) != 0 ? 1 : 0;

            _ansiEnabled = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR"));
        }

        private int _boldRecursion;

        public static AnsiConsole GetOutput()
        {
            return new AnsiConsole(Console.Out);
        }

        public static AnsiConsole GetError()
        {
            return new AnsiConsole(Console.Error);
        }

        public TextWriter Writer { get; }

        public ConsoleColor OriginalForegroundColor { get; }

        private void SetColor(ConsoleColor color)
        {
            if (!_ansiEnabled)
            {
                return;
            }

            int c = (int)color;

            Console.ForegroundColor =
                c < 0 ? color :                                   // unknown, just use it
                _boldRecursion > 0 ? (ConsoleColor)(c | Light) :  // ensure color is light
                (ConsoleColor)(c & ~Light);                       // ensure color is dark
        }

        private void SetBold(bool bold)
        {
            if (!_ansiEnabled)
            {
                return;
            }

            _boldRecursion += bold ? 1 : -1;
            if (_boldRecursion > 1 || (_boldRecursion == 1 && !bold))
            {
                return;
            }

            // switches on _boldRecursion to handle boldness
            SetColor(Console.ForegroundColor);
        }

        public void WriteLine(string message)
        {
            Write(message);
            Writer.WriteLine();
        }


        public void Write(string message)
        {
            var escapeScan = 0;
            for (; ; )
            {
                var escapeIndex = message.IndexOf("\x1b[", escapeScan, StringComparison.Ordinal);
                if (escapeIndex == -1)
                {
                    var text = message.Substring(escapeScan);
                    Writer.Write(text);
                    break;
                }
                else
                {
                    var startIndex = escapeIndex + 2;
                    var endIndex = startIndex;
                    while (endIndex != message.Length &&
                        message[endIndex] >= 0x20 &&
                        message[endIndex] <= 0x3f)
                    {
                        endIndex += 1;
                    }

                    var text = message.Substring(escapeScan, escapeIndex - escapeScan);
                    Writer.Write(text);
                    if (endIndex == message.Length)
                    {
                        break;
                    }

                    switch (message[endIndex])
                    {
                        case 'm':
                            int value;
                            if (int.TryParse(message.Substring(startIndex, endIndex - startIndex), out value))
                            {
                                switch (value)
                                {
                                    case 1:
                                        SetBold(true);
                                        break;
                                    case 22:
                                        SetBold(false);
                                        break;
                                    case 30:
                                        SetColor(ConsoleColor.Black);
                                        break;
                                    case 31:
                                        SetColor(ConsoleColor.Red);
                                        break;
                                    case 32:
                                        SetColor(ConsoleColor.Green);
                                        break;
                                    case 33:
                                        SetColor(ConsoleColor.Yellow);
                                        break;
                                    case 34:
                                        SetColor(ConsoleColor.Blue);
                                        break;
                                    case 35:
                                        SetColor(ConsoleColor.Magenta);
                                        break;
                                    case 36:
                                        SetColor(ConsoleColor.Cyan);
                                        break;
                                    case 37:
                                        SetColor(ConsoleColor.Gray);
                                        break;
                                    case 39:
                                        Console.ForegroundColor = OriginalForegroundColor;
                                        break;
                                }
                            }
                            break;
                    }

                    escapeScan = endIndex + 1;
                }
            }
        }
    }
}
