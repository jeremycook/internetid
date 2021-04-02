using InternetId.Common.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace InternetId.Tests
{
    [TestClass]
    public class HasherTests
    {
        private const string rfc2898 = "rfc2898";

        private static readonly DateTimeOffset notBefore = DateTimeOffset.FromUnixTimeSeconds(1617320388);
        private static readonly DateTimeOffset notAfter1Hour = notBefore.AddHours(1);
        private static readonly DateTimeOffset notAfter10Years = notBefore.AddYears(10);

        private static readonly DateTimeOffset validNow = notBefore.AddMinutes(30);

        private static readonly byte[] salt = Convert.FromBase64String("fH6CQViPiFl9K9I2OEVSRg==");

        private static readonly Hasher validHasher = new(utcNow: validNow);
        private static readonly Hasher inactiveHasher = new(utcNow: notBefore.AddSeconds(-1));
        private static readonly Hasher expiredHasher = new(utcNow: notAfter10Years.AddSeconds(1));

        [TestMethod]
        public void Calculate_throws()
        {
            Assert.ThrowsException<ArgumentException>(() => validHasher.CalculateKey(null, salt, rfc2898, 100_000, notBefore, notAfter1Hour));
            Assert.ThrowsException<ArgumentException>(() => validHasher.CalculateKey("", salt, rfc2898, 100_000, notBefore, notAfter1Hour));
            Assert.ThrowsException<ArgumentException>(() => validHasher.CalculateKey(" ", salt, rfc2898, 100_000, notBefore, notAfter1Hour));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => validHasher.CalculateKey("123456", null, rfc2898, 100_000, notBefore, notAfter1Hour));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => validHasher.CalculateKey("123456", new byte[1], rfc2898, 100_000, notBefore, notAfter1Hour));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => validHasher.CalculateKey("123456", new byte[16], rfc2898, 100_000, notBefore, notAfter1Hour));

            Assert.ThrowsException<ArgumentNullException>(() => validHasher.CalculateKey("123456", salt, null, 100_000, notBefore, notAfter1Hour));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => validHasher.CalculateKey("123456", salt, "", 100_000, notBefore, notAfter1Hour));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => validHasher.CalculateKey("123456", salt, " ", 100_000, notBefore, notAfter1Hour));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => validHasher.CalculateKey("123456", salt, rfc2898, -1, notBefore, notAfter1Hour));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => validHasher.CalculateKey("123456", salt, rfc2898, 0, notBefore, notAfter1Hour));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => validHasher.CalculateKey("123456", salt, rfc2898, 99_999, notBefore, notAfter1Hour));
        }

        [TestMethod]
        public void Calculate_100_000_iteration_key()
        {
            byte[] key = validHasher.CalculateKey("123456", salt, rfc2898, 100_000, notBefore, notAfter1Hour);

            string base64key = Convert.ToBase64String(key);

            Assert.AreEqual("wxlLBFAMQ7qbALF6bUTmZB7zUiQrR5SMzq28qD1Eq70=", base64key);
        }

        [TestMethod]
        public void Calculate_10_000_000_iteration_key()
        {
            byte[] key = validHasher.CalculateKey("123456", salt, rfc2898, 10_000_000, notBefore, notAfter1Hour);

            string base64key = Convert.ToBase64String(key);

            Assert.AreEqual("E/LveZq0mDWiXbdp0NBBNsZMgNuK0VkX/A44vuGWsH8=", base64key);
        }

        [TestMethod]
        public void Hash_1_hour_6_letter_pin()
        {
            string hashPin = validHasher.Hash("abcdef", (long)Math.Pow(26, 6), salt, rfc2898, notBefore, notAfter1Hour);

            Assert.AreEqual(
                "{\"k\":\"jU51R+XgHuaOza3elEnSX73S2OeMOVdJ0jZ+j2p9ivg=\",\"a\":\"rfc2898\",\"s\":\"fH6CQViPiFl9K9I2OEVSRg==\",\"i\":100000,\"nb\":\"2021-04-01T23:39:48+00:00\",\"na\":\"2021-04-02T00:39:48+00:00\"}", 
                hashPin);

            Assert.AreEqual(HasherVerificationResult.Invalid, validHasher.Verify("Abcdef", hashPin));
            Assert.AreEqual(HasherVerificationResult.Valid, validHasher.Verify("abcdef", hashPin));
            Assert.AreEqual(HasherVerificationResult.Inactive, inactiveHasher.Verify("abcdef", hashPin));
            Assert.AreEqual(HasherVerificationResult.Expired, expiredHasher.Verify("abcdef", hashPin));
        }

        [TestMethod]
        public void Hash_10_year_9_alphanumeric_password()
        {
            string hashPassword = validHasher.Hash("abcd56789", (long)Math.Pow(36, 9), salt, rfc2898, notBefore, notAfter10Years);

            Assert.AreEqual(
                "{\"k\":\"NRu/iHjD0LLQ3YbrbJJ7H/0u0+zidisjMJp00AbMm2Q=\",\"a\":\"rfc2898\",\"s\":\"fH6CQViPiFl9K9I2OEVSRg==\",\"i\":100000,\"nb\":\"2021-04-01T23:39:48+00:00\",\"na\":\"2031-04-01T23:39:48+00:00\"}", 
                hashPassword);

            Assert.AreEqual(HasherVerificationResult.Invalid, validHasher.Verify("Abcd56789", hashPassword));
            Assert.AreEqual(HasherVerificationResult.Valid, validHasher.Verify("abcd56789", hashPassword));
            Assert.AreEqual(HasherVerificationResult.Inactive, inactiveHasher.Verify("abcd56789", hashPassword));
            Assert.AreEqual(HasherVerificationResult.Expired, expiredHasher.Verify("abcd56789", hashPassword));
        }
    }
}
