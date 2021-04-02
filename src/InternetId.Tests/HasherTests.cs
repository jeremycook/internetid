using InternetId.Common.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography;

namespace InternetId.Tests
{
    [TestClass]
    public class HasherTests
    {
        private static readonly Hasher hasher = new();
        private static readonly byte[] salt = Convert.FromBase64String("fH6CQViPiFl9K9I2OEVSRg==");
        static HasherTests()
        {
        }

        [TestMethod]
        public void Calculate_throws()
        {
            Assert.ThrowsException<ArgumentException>(() => hasher.CalculateKey(null, salt, "rfc2898", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
            Assert.ThrowsException<ArgumentException>(() => hasher.CalculateKey("", salt, "rfc2898", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
            Assert.ThrowsException<ArgumentException>(() => hasher.CalculateKey(" ", salt, "rfc2898", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => hasher.CalculateKey("123456", null, "rfc2898", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => hasher.CalculateKey("123456", new byte[1], "rfc2898", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => hasher.CalculateKey("123456", new byte[16], "rfc2898", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));

            Assert.ThrowsException<ArgumentNullException>(() => hasher.CalculateKey("123456", salt, null, 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => hasher.CalculateKey("123456", salt, "", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => hasher.CalculateKey("123456", salt, " ", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => hasher.CalculateKey("123456", salt, "rfc2898", -1, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => hasher.CalculateKey("123456", salt, "rfc2898", 0, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => hasher.CalculateKey("123456", salt, "rfc2898", 99_999, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388)));
        }

        [TestMethod]
        public void Calculate_100000_iteration_key()
        {
            byte[] key = hasher.CalculateKey("123456", salt, "rfc2898", 100_000, DateTimeOffset.FromUnixTimeSeconds(1617320388), DateTimeOffset.FromUnixTimeSeconds(1617320388));
            string base64key = Convert.ToBase64String(key);
            Assert.AreEqual("QQHxXU15oEW1jYKNMSP+OElZ9G8JxkhQJpxHFanjZ+w=", base64key);
        }

        [TestMethod]
        public void Hash_1_hour_6_letter_pin()
        {
            string hashPin = hasher.Hash("abcdef", (long)Math.Pow(26, 6), TimeSpan.FromHours(1));
        }

        [TestMethod]
        public void Hash_10_year_9_alphanumeric_password()
        {
            string hashPassword = hasher.Hash("abcd56789", (long)Math.Pow(36, 9), TimeSpan.FromDays(10 * 365));
        }
    }
}
