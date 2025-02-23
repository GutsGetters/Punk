using System;
using System.Collections.Generic;
using System.Text;

namespace Punk.Parser
{
    // Тип результата парсера
    public struct ParseResult<T>
    {
        public readonly T Value;
        public readonly string RemainingInput;
        public readonly bool IsSuccess;
        public readonly string ErrorMessage;
        public readonly int ErrorPosition;

        private ParseResult(T value, string remainingInput, bool isSuccess, string errorMessage, int errorPosition)
        {
            Value = value;
            RemainingInput = remainingInput;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ErrorPosition = errorPosition;
        }

        public static ParseResult<T> Success(T value, string remainingInput)
        {
            return new ParseResult<T>(value, remainingInput, true, null, -1);
        }

        public static ParseResult<T> Failure(string errorMessage, int errorPosition)
        {
            return new ParseResult<T>(default(T), null, false, errorMessage, errorPosition);
        }
    }

    // Базовый делегат для парсеров
    public delegate ParseResult<T> Parser<T>(string input);

    public static class ParserCombinators
    {
        // Парсер, который всегда успешно возвращает значение
        public static Parser<T> Return<T>(T value)
        {
            return delegate(string input) { return ParseResult<T>.Success(value, input); };
        }

        // Парсер, который всегда завершается неудачей
        public static Parser<T> Fail<T>(string errorMessage)
        {
            return delegate(string input) { return ParseResult<T>.Failure(errorMessage, 0); };
        }

        // Парсер, который проверяет первый символ строки
        public static Parser<char> Char(Predicate<char> predicate, string errorMessage)
        {
            return delegate(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return ParseResult<char>.Failure(errorMessage, 0);
                }

                char firstChar = input[0];
                if (predicate(firstChar))
                {
                    return ParseResult<char>.Success(firstChar, input.Substring(1));
                }

                return ParseResult<char>.Failure(errorMessage, 0);
            };
        }

        // Комбинатор для выбора между двумя парсерами
        public static Parser<T> Or<T>(Parser<T> first, Parser<T> second)
        {
            return delegate(string input)
            {
                ParseResult<T> result = first(input);
                if (result.IsSuccess)
                {
                    return result;
                }

                return second(input);
            };
        }

        // Парсер для строки
        public static Parser<string> String(string expected)
        {
            return delegate(string input)
            {
                if (input.StartsWith(expected))
                {
                    return ParseResult<string>.Success(expected, input.Substring(expected.Length));
                }

                return ParseResult<string>.Failure("Expected: " + expected, 0);
            };
        }

        // Парсер для одного символа
        public static Parser<char> AnyChar()
        {
            return delegate(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return ParseResult<char>.Failure("Unexpected end of input", 0);
                }

                return ParseResult<char>.Success(input[0], input.Substring(1));
            };
        }

        // Парсер для повторяющегося шаблона (ноль или более раз)
        public static Parser<List<T>> Many<T>(Parser<T> parser)
        {
            return delegate(string input)
            {
                List<T> results = new List<T>();
                string remainingInput = input;

                while (true)
                {
                    ParseResult<T> result = parser(remainingInput);
                    if (!result.IsSuccess)
                    {
                        break;
                    }

                    results.Add(result.Value);
                    remainingInput = result.RemainingInput;
                }

                return ParseResult<List<T>>.Success(results, remainingInput);
            };
        }

        // Парсер для одного или более повторений
        public static Parser<List<T>> Many1<T>(Parser<T> parser)
        {
            return delegate(string input)
            {
                ParseResult<T> firstResult = parser(input);
                if (!firstResult.IsSuccess)
                {
                    return ParseResult<List<T>>.Failure(firstResult.ErrorMessage, firstResult.ErrorPosition);
                }

                ParseResult<List<T>> restResult = Many(parser)(firstResult.RemainingInput);
                List<T> results = new List<T>();
                results.Add(firstResult.Value);
                results.AddRange(restResult.Value);

                return ParseResult<List<T>>.Success(results, restResult.RemainingInput);
            };
        }

        // Парсер для последовательности парсеров
        public static Parser<List<T>> Sequence<T>(params Parser<T>[] parsers)
        {
            return delegate(string input)
            {
                List<T> results = new List<T>();
                string remainingInput = input;

                foreach (Parser<T> parser in parsers)
                {
                    ParseResult<T> result = parser(remainingInput);
                    if (!result.IsSuccess)
                    {
                        return ParseResult<List<T>>.Failure(result.ErrorMessage, result.ErrorPosition);
                    }

                    results.Add(result.Value);
                    remainingInput = result.RemainingInput;
                }

                return ParseResult<List<T>>.Success(results, remainingInput);
            };
        }

        // Парсер для чисел
        public static Parser<string> Number()
        {
            return delegate(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return ParseResult<string>.Failure("Expected number", 0);
                }

                int index = 0;
                while (index < input.Length && char.IsDigit(input[index]))
                {
                    index++;
                }

                if (index == 0)
                {
                    return ParseResult<string>.Failure("Expected number", 0);
                }

                return ParseResult<string>.Success(input.Substring(0, index), input.Substring(index));
            };
        }

        // Парсер для строк в кавычках (с экранированием)
        public static Parser<string> QuotedString()
        {
            return delegate(string input)
            {
                if (string.IsNullOrEmpty(input) || input[0] != '"')
                {
                    return ParseResult<string>.Failure("Expected quoted string", 0);
                }

                StringBuilder result = new StringBuilder();
                int index = 1;
                while (index < input.Length)
                {
                    char currentChar = input[index];
                    if (currentChar == '\\')
                    {
                        // Обработка экранирования
                        if (index + 1 >= input.Length)
                        {
                            return ParseResult<string>.Failure("Unexpected end of input after escape character", index);
                        }

                        char nextChar = input[index + 1];
                        result.Append(nextChar);
                        index += 2;
                    }
                    else if (currentChar == '"')
                    {
                        // Конец строки
                        return ParseResult<string>.Success(result.ToString(), input.Substring(index + 1));
                    }
                    else
                    {
                        result.Append(currentChar);
                        index++;
                    }
                }

                return ParseResult<string>.Failure("Unclosed quoted string", index);
            };
        }

        // Парсер для Hex-чисел
        public static Parser<string> HexNumber()
        {
            return delegate(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return ParseResult<string>.Failure("Expected hex number", 0);
                }

                int index = 0;
                while (index < input.Length && IsHexChar(input[index]))
                {
                    index++;
                }

                if (index == 0)
                {
                    return ParseResult<string>.Failure("Expected hex number", 0);
                }

                return ParseResult<string>.Success(input.Substring(0, index), input.Substring(index));
            };
        }

        // Вспомогательный метод для проверки Hex-символов
        private static bool IsHexChar(char c)
        {
            return char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        // Комбинатор Eat для работы с регулярными выражениями (упрощенная версия)
        public static Parser<string> Eat(Predicate<char> predicate, string errorMessage)
        {
            return delegate(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return ParseResult<string>.Failure(errorMessage, 0);
                }

                int index = 0;
                while (index < input.Length && predicate(input[index]))
                {
                    index++;
                }

                if (index == 0)
                {
                    return ParseResult<string>.Failure(errorMessage, 0);
                }

                return ParseResult<string>.Success(input.Substring(0, index), input.Substring(index));
            };
        }
    }

    public static class ParserExtensions
    {
        // Парсинг файла
        public static ParseResult<T> ParseFile<T>(Parser<T> parser, string filePath)
        {
            try
            {
                string fileContent = System.IO.File.ReadAllText(filePath);
                return parser(fileContent);
            }
            catch (System.IO.IOException ex)
            {
                return ParseResult<T>.Failure("File read error: " + ex.Message, 0);
            }
        }

        // Парсинг строки
        public static ParseResult<T> ParseString<T>(Parser<T> parser, string input)
        {
            return parser(input);
        }

        // Парсинг массива строк
        public static ParseResult<T> ParseLines<T>(Parser<T> parser, IEnumerable<string> lines)
        {
            string combinedInput = string.Join(Environment.NewLine, lines);
            return parser(combinedInput);
        }
    }
}
