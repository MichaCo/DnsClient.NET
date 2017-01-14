////using System;
////using System.Globalization;

////namespace DnsClient
////{
////    /// <summary>
////    /// Relaxed version of <see cref="DnsName"/>. Labels don't get analyzed but characters will be.
////    /// The input will eventually get transformed into the IDN, depending on if it contains special chararcters or not.
////    /// </summary>
////    public class QueryName
////    {
////        public static readonly QueryName Root = new QueryName(".");
////        private static readonly IdnMapping _idn = new IdnMapping() { UseStd3AsciiRules = true };
////        private const char Dot = '.';
////        private const string DotStr = ".";

////        /// <summary>
////        /// Gets the validated value.
////        /// </summary>
////        public string Name { get; }

////        /// <summary>
////        /// Gets the original value used to construct the instance of <see cref="QueryName"/> initially.
////        /// </summary>
////        public string Original { get; }

////        public QueryName(string name)
////        {
////            if (name == null) throw new ArgumentNullException(nameof(name));

////            Original = name;
////            Name = Validate(name);
////        }

////        public override string ToString()
////        {
////            return Name;
////        }

////        public override bool Equals(object obj)
////        {
////            if (obj == null)
////            {
////                return false;
////            }

////            return obj.ToString().Equals(Name);
////        }

////        public override int GetHashCode()
////        {
////            return Name.GetHashCode();
////        }

////        public static implicit operator QueryName(string name) => new QueryName(name);

////        public static implicit operator string(QueryName name) => name.ToString();

////        private string Validate(string name)
////        {
////            if (name.Length > 1 && name[0] == Dot)
////            {
////                throw new ArgumentException($"'{name}' is not a legal name, found empty label.", nameof(name));
////            }

////            if (name.Length == 0 || (name.Length == 1 && name.Equals(DotStr)))
////            {
////                return DotStr;
////            }

////            string result = name;
////            bool valid = true;
////            foreach (var c in name)
////            {
////                if (!(c == '-' || c == '.' || c == '_' ||
////                    c >= 'a' && c <= 'z' ||
////                    c >= 'A' && c <= 'Z' ||
////                    c >= '0' && c <= '9'))
////                {
////                    valid = false;
////                    break;
////                }
////            }
////            if (!valid)
////            {
////                try
////                {
////                    result = _idn.GetAscii(name);
////                }
////                catch (Exception ex)
////                {
////                    throw new ArgumentException($"'{name}' is not a valid host name.", nameof(name), ex);
////                }
////            }

////            if (!result[result.Length - 1].Equals(Dot))
////            {
////                result += DotStr;
////            }

////            return result;
////        }
////    }
////}