using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeGenerator
{
    internal class Program
    {
        private static int _startNumber;
        private static int _endNumber;
        private static string _templateFilePath;
        private static string _outputFilePath;

        private static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            _Init();
            _GenerateCode(_templateFilePath);
            Console.WriteLine("Ready! Press any key to continue...");
            Console.ReadKey();
        }

        private static void _Init()
        {
            Console.WriteLine("Initialization...");
            if (!int.TryParse(ConfigurationManager.AppSettings["StartNumber"], out _startNumber))
                Console.WriteLine(string.Format("Cannot init parameter '{0}'. Set default value: '{1}'", "_startNumber", _startNumber));
            if (!int.TryParse(ConfigurationManager.AppSettings["EndNumber"], out _endNumber))
                Console.WriteLine(string.Format("Cannot init parameter '{0}'. Set default value: '{1}'", "_endNumber", _endNumber));
            _templateFilePath = ConfigurationManager.AppSettings["TemplateFilePath"];
            if (string.IsNullOrWhiteSpace(_templateFilePath))
            {
                _templateFilePath = "template.txt";
                Console.WriteLine("Invalid parameter '_templateFilePath'. Set default value: '" + _templateFilePath + "'");
            }
            _outputFilePath = ConfigurationManager.AppSettings["OutputFilePath"];
            if (string.IsNullOrWhiteSpace(_outputFilePath))
            {
                _outputFilePath = "generated_code.vhd";
                Console.WriteLine("Invalid parameter '_outputFilePath'. Set default value: '" + _outputFilePath + "'");
            }
            Console.WriteLine("Completed!");
        }

        private static void _GenerateCode(string path)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(path))
                {
                    string end = streamReader.ReadToEnd();
                    string pattern = "{(?<byteCode>byte_code)}|{(?<registerNumber>register_number((?<iter>\\+\\+)|(?<mul>\\*[0-9]*)|(?<add>\\+[0-9]*)|(?<muladd>\\*[0-9]*\\+[0-9]*)))}";
                    MatchCollection matchCollection = Regex.Matches(end, pattern);
                    using (StreamWriter streamWriter = new StreamWriter(_outputFilePath, false))
                    {
                        for (int startNumber = _startNumber; startNumber < _endNumber; ++startNumber)
                        {
                            string str = end;
                            string bin = _ConvertToBin(startNumber);
                            foreach (Match match in matchCollection)
                            {
                                if (!string.IsNullOrWhiteSpace(match.Groups["byteCode"].Value))
                                    str = str.Replace(match.Value, bin);
                                else if (!string.IsNullOrWhiteSpace(match.Groups["registerNumber"].Value) && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                                    str = _ProcessValueAndGetReplace(str, startNumber, match);
                                else if (!string.IsNullOrWhiteSpace(match.Groups["registerNumber"].Value))
                                    str = str.Replace(match.Value, string.Format("{0}", startNumber));
                            }
                            streamWriter.WriteLine(str);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static string _ProcessValueAndGetReplace(string str, int number, Match match)
        {
            int resultValue = _GetResultValue(match, number);
            return str.Replace(match.Value, string.Format("{0}", resultValue));
        }

        private static int _GetResultValue(Match match, int number)
        {
            if (!string.IsNullOrWhiteSpace(match.Groups["iter"].Value))
                return ++number;

            int result = -1;
            if (!string.IsNullOrWhiteSpace(match.Groups["mul"].Value) && int.TryParse(match.Groups["mul"].Value.Replace("*", "").Trim(), out result))
                    return number * result;
            if (!string.IsNullOrWhiteSpace(match.Groups["add"].Value) && int.TryParse(match.Groups["add"].Value.Replace("+", "").Trim(), out result))
                return number + result;
            if (!string.IsNullOrWhiteSpace(match.Groups["muladd"].Value))
            {
                string[] numbers = match.Groups["muladd"].Value.Remove(0, 1).Split('+', StringSplitOptions.RemoveEmptyEntries);
                string mul = numbers[0];
                string add = numbers[1];

                if (int.TryParse(mul?.Trim(), out int temp))
                    result = number * temp;
                if (int.TryParse(add?.Trim(), out temp))
                    result += temp;

                return result;
            }

            return number;
        }

        private static string _ConvertToBin(int number)
        {
            return Convert.ToString(number, 2).PadLeft(10, '0');
        }
    }
}
