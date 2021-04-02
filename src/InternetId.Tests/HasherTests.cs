using InternetId.Common.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace InternetId.Tests
{
    [TestClass]
    public class HasherTests
    {
        private const string rfc2898 = "rfc2898";

        private static readonly DateTimeOffset notBefore = DateTimeOffset.FromUnixTimeSeconds(1617320388);
        private static readonly DateTimeOffset notAfter1Hour = notBefore.AddHours(1);
        private static readonly DateTimeOffset notAfter10Years = notBefore.AddYears(10);
        private static readonly DateTimeOffset notAfter100Years = notBefore.AddYears(100);

        private static readonly DateTimeOffset validNow = notBefore.AddMinutes(30);

        private static readonly byte[] salt = Convert.FromBase64String("fH6CQViPiFl9K9I2OEVSRg==");

#pragma warning disable CS0618 // Type or member is obsolete
        private static readonly Hasher validHasher = Hasher.CreateTestHasher(utcNow: validNow);
        private static readonly Hasher inactiveHasher = Hasher.CreateTestHasher(utcNow: notBefore.AddSeconds(-1));
        private static readonly Hasher expiredHasher = Hasher.CreateTestHasher(utcNow: notAfter10Years.AddSeconds(1));
#pragma warning restore CS0618 // Type or member is obsolete

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
            string hashPin = validHasher.Hash("abcdef", Math.Pow(26, 6), salt, rfc2898, notBefore, notAfter1Hour);

            Assert.AreEqual(
                "{\"k\":\"jU51R+XgHuaOza3elEnSX73S2OeMOVdJ0jZ+j2p9ivg=\",\"a\":\"rfc2898\",\"s\":\"fH6CQViPiFl9K9I2OEVSRg==\",\"i\":100000,\"nb\":\"2021-04-01T23:39:48+00:00\",\"na\":\"2021-04-02T00:39:48+00:00\"}",
                hashPin);
        }

        [TestMethod]
        public void Verify_1_hour_6_letter_pin()
        {
            string hashPin = "{\"k\":\"jU51R+XgHuaOza3elEnSX73S2OeMOVdJ0jZ+j2p9ivg=\",\"a\":\"rfc2898\",\"s\":\"fH6CQViPiFl9K9I2OEVSRg==\",\"i\":100000,\"nb\":\"2021-04-01T23:39:48+00:00\",\"na\":\"2021-04-02T00:39:48+00:00\"}";

            Assert.AreEqual(HasherVerificationResult.Invalid, validHasher.Verify("Abcdef", hashPin));
            Assert.AreEqual(HasherVerificationResult.Valid, validHasher.Verify("abcdef", hashPin));
            Assert.AreEqual(HasherVerificationResult.Inactive, inactiveHasher.Verify("abcdef", hashPin));
            Assert.AreEqual(HasherVerificationResult.Expired, expiredHasher.Verify("abcdef", hashPin));
        }

        [TestMethod]
        public void Hash_100_year_9_alpha_password()
        {
            string hashPassword = validHasher.Hash("abcd56789", Math.Pow(26, 9), salt, rfc2898, notBefore, notAfter100Years);

            Assert.AreEqual(
                "{\"k\":\"gc1sZ6AgUutY7j9qY198hPBD/f+WcCLeGu080knh+vs=\",\"a\":\"rfc2898\",\"s\":\"fH6CQViPiFl9K9I2OEVSRg==\",\"i\":2400001,\"nb\":\"2021-04-01T23:39:48+00:00\",\"na\":\"2121-04-01T23:39:48+00:00\"}",
                hashPassword);
        }

        [TestMethod]
        public void Hash_10_year_9_alphanumeric_password()
        {
            string hashPassword = validHasher.Hash("abcd56789", Math.Pow(36, 9), salt, rfc2898, notBefore, notAfter10Years);

            Assert.AreEqual(
                "{\"k\":\"NRu/iHjD0LLQ3YbrbJJ7H/0u0+zidisjMJp00AbMm2Q=\",\"a\":\"rfc2898\",\"s\":\"fH6CQViPiFl9K9I2OEVSRg==\",\"i\":100000,\"nb\":\"2021-04-01T23:39:48+00:00\",\"na\":\"2031-04-01T23:39:48+00:00\"}",
                hashPassword);
        }

        [TestMethod]
        public void Verify_10_year_9_alphanumeric_password()
        {
            string hashPassword = "{\"k\":\"NRu/iHjD0LLQ3YbrbJJ7H/0u0+zidisjMJp00AbMm2Q=\",\"a\":\"rfc2898\",\"s\":\"fH6CQViPiFl9K9I2OEVSRg==\",\"i\":100000,\"nb\":\"2021-04-01T23:39:48+00:00\",\"na\":\"2031-04-01T23:39:48+00:00\"}";

            Assert.AreEqual(HasherVerificationResult.Invalid, validHasher.Verify("Abcd56789", hashPassword));
            Assert.AreEqual(HasherVerificationResult.Valid, validHasher.Verify("abcd56789", hashPassword));
            Assert.AreEqual(HasherVerificationResult.Inactive, inactiveHasher.Verify("abcd56789", hashPassword));
            Assert.AreEqual(HasherVerificationResult.Expired, expiredHasher.Verify("abcd56789", hashPassword));
        }

        [TestMethod]
        public void Hash_caches_iterations()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Hasher hasher = Hasher.CreateTestHasher(utcNow: validNow);
#pragma warning restore CS0618 // Type or member is obsolete

            var sw = Stopwatch.StartNew();
            hasher.Hash("abcd56789", Math.Pow(26, 9), salt, rfc2898, notBefore, notAfter100Years);
            var firstPass = sw.ElapsedTicks;

            // Run a couple hashes to get the jitter out
            hasher.Hash("abcd56789", Math.Pow(26, 9), salt, rfc2898, notBefore, notAfter100Years);
            hasher.Hash("abcd56789", Math.Pow(26, 9), salt, rfc2898, notBefore, notAfter100Years);

            sw.Restart();
            hasher.Hash("abcd56789", Math.Pow(26, 9), salt, rfc2898, notBefore, notAfter100Years);
            var secondPass = sw.ElapsedTicks;

            Assert.IsTrue(firstPass > secondPass, "The first pass should take longer than subsequent passes due to internal estimate caching.");
        }
    }
}
