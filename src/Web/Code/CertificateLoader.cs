using System;
using System.Security.Cryptography.X509Certificates;
using AzureStorage.Blob;
using Common;
using Lykke.SettingsReader.ReloadingManager;

namespace Web.Code
{
    internal static class CertificateLoader
    {
        internal static X509Certificate2 Load(string sertConnString)
        {
            var sertContainer = Environment.GetEnvironmentVariable("CertContainer");
            var sertFilename = Environment.GetEnvironmentVariable("CertFileName");
            var sertPassword = Environment.GetEnvironmentVariable("CertPassword");

            var certBlob = AzureBlobStorage.Create(ConstantReloadingManager.From(sertConnString));
            var cert = certBlob.GetAsync(sertContainer, sertFilename).GetAwaiter().GetResult().ToBytes();

            return new X509Certificate2(cert, sertPassword);
        }
    }
}
