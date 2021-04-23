using Humanizer;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;
using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace InternetId.Npgsql
{
    public static class NpgsqlConnectionBuilder
    {
        /// <summary>
        /// Create an <see cref="NpgsqlConnection"/>, including a CA certificate check 
        /// if a "ConnectionStrings:{ConnectionStringName}CaCertficiate" or 
        /// "{CONNECTION_STRING_NAME}_CA_CERTIFICATE" can be found in 
        /// <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        public static NpgsqlConnection Build(IConfiguration configuration, string connectionStringName)
        {
            string connectionString =
                configuration.GetValue<string?>("ConnectionStrings:" + connectionStringName) ??
                configuration.GetValue<string?>(connectionStringName.Underscore().ToUpperInvariant() + "_CONNECTION_STRING") ??
                throw new ArgumentException($"Could not find a connection string named '{connectionStringName}'.");

            NpgsqlConnection connection = new(connectionString);

            string connectionStringCertificateName = connectionStringName + "CaCertificate";
            string? caCertificateText =
                configuration.GetValue<string?>("ConnectionStrings:" + connectionStringCertificateName) ??
                configuration.GetValue<string?>(connectionStringCertificateName.Underscore().ToUpperInvariant());

            if (!string.IsNullOrWhiteSpace(caCertificateText))
            {
                ValidateCaCertificate(connection, caCertificateText);
            }

            return connection;
        }

        /// <summary>
        /// Configures the <see cref="NpgsqlConnection.UserCertificateValidationCallback"/> of <paramref name="connection"/>
        /// against the <paramref name="caCertificateText"/>.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static NpgsqlConnection ValidateCaCertificate(NpgsqlConnection connection, string caCertificateText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(caCertificateText);
            var caCertificate = new X509Certificate2(bytes);
            connection.UserCertificateValidationCallback = CreateUserCertificateValidationCallback(caCertificate, connection.UserCertificateValidationCallback);

            return connection;
        }

        private static RemoteCertificateValidationCallback CreateUserCertificateValidationCallback(X509Certificate2 caCert, RemoteCertificateValidationCallback? beforeUserCertificateValidationCallback = null)
        {
            return (object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
            {
                if (beforeUserCertificateValidationCallback is not null)
                {
                    if (beforeUserCertificateValidationCallback(sender, certificate, chain, sslPolicyErrors) == false)
                    {
                        return false;
                    }
                }

                if (certificate is null)
                {
                    throw new ArgumentNullException(nameof(certificate));
                }

                X509Chain caCertChain = new();
                caCertChain.ChainPolicy = new X509ChainPolicy()
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    RevocationFlag = X509RevocationFlag.EntireChain
                };
                caCertChain.ChainPolicy.ExtraStore.Add(caCert);

                X509Certificate2 serverCert = new(certificate);

                caCertChain.Build(serverCert);
                if (caCertChain.ChainStatus.Length == 0)
                {
                    // No errors
                    return true;
                }

                foreach (X509ChainStatus status in caCertChain.ChainStatus)
                {
                    // Check if we got any errors other than UntrustedRoot (which we will always get if we don't install the CA cert to the system store)
                    if (status.Status != X509ChainStatusFlags.UntrustedRoot)
                    {
                        return false;
                    }
                }

                return true;
            };
        }
    }
}
