﻿using QuickDeploy.Common;
using QuickDeploy.Common.Messages;
using System;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace QuickDeploy.Client
{
    public class QuickDeployTcpSslClient : IQuickDeployClient
    {
        private readonly string sslHostname = "example.org";

        private readonly StreamHelper streamHelper = new StreamHelper();

        private readonly X509Certificate2 expectedServerCertificate;

        private readonly X509Certificate2 clientCertificate;

        private readonly X509Certificate2Collection clientCertificateCollection;

        private readonly string hostname;

        private readonly int port;

        public QuickDeployTcpSslClient(
            string hostname,
            int port,
            string expectedServerCertificateFilename,
            string clientCertificateFilename,
            string clientCertificatePassword)
        {
            this.hostname = hostname;
            this.port = port;

            this.expectedServerCertificate = new X509Certificate2(expectedServerCertificateFilename);
            this.clientCertificate = new X509Certificate2(clientCertificateFilename, clientCertificatePassword, X509KeyStorageFlags.Exportable);
            this.clientCertificateCollection = new X509Certificate2Collection(this.clientCertificate);
        }

        public string RemoteAddress => $"TCP {this.hostname}:{this.port}";

        public SslProtocols EnabledSslProtocols { get; set; } = SslProtocols.Tls12;

        public TResponse Call<TRequest, TResponse>(TRequest request) where TResponse : class
        {
            using (var client = new TcpClient())
            {
                client.Connect(this.hostname, this.port);

                using (var stream = new SslStream(client.GetStream(), false, this.VerifyServerCertificate))
                {
                    stream.AuthenticateAsClient(this.sslHostname, this.clientCertificateCollection, this.EnabledSslProtocols, false);

                    this.streamHelper.Send(stream, request);

                    while (true)
                    {
                        var receivedMessage = this.streamHelper.Receive(stream);

                        if (receivedMessage is StatusMessage)
                        {
                            this.HandleStatusMessage(receivedMessage as StatusMessage);
                            continue;
                        }

                        if (receivedMessage is TResponse)
                        {
                            return receivedMessage as TResponse;
                        }

                        throw new InvalidOperationException($"Unknown message type: {receivedMessage?.GetType()?.ToString() ?? "null"}");
                    }
                }
            }
        }

        public AnalyzeDirectoryResponse AnalyzeDirectory(AnalyzeDirectoryRequest analyzeDirectoryRequest)
        {
            return this.Call<AnalyzeDirectoryRequest, AnalyzeDirectoryResponse>(analyzeDirectoryRequest);
        }

        public SyncDirectoryResponse SyncDirectory(SyncDirectoryRequest syncDirectoryRequest)
        {
            return this.Call<SyncDirectoryRequest, SyncDirectoryResponse>(syncDirectoryRequest);
        }

        public SyncFileResponse SyncFile(string filename, byte[] fileContent, Credentials credentials)
        {
            var syncFileRequest = new SyncFileRequest
            {
                Credentials = credentials,
                Filename = filename,
                GzippedFile = new Zipper().Gzip(fileContent),
            };

            return this.Call<SyncFileRequest, SyncFileResponse>(syncFileRequest);
        }

        public ChangeServiceStatusResponse ChangeServiceStatus(ChangeServiceStatusRequest changeServiceStatusRequest)
        {
            return this.Call<ChangeServiceStatusRequest, ChangeServiceStatusResponse>(changeServiceStatusRequest);
        }

        public ChangeIisAppPoolStatusResponse ChangeIisAppPoolStatus(ChangeIisAppPoolStatusRequest changeIisAppPoolStatusRequest)
        {
            return this.Call<ChangeIisAppPoolStatusRequest, ChangeIisAppPoolStatusResponse>(changeIisAppPoolStatusRequest);
        }

        public ChangeServerModulesStatusResponse ChangeServerModulesStatus(ChangeServerModulesStatusRequest changeServerModulesStatusRequest)
        {
            return this.Call<ChangeServerModulesStatusRequest, ChangeServerModulesStatusResponse>(changeServerModulesStatusRequest);
        }

        public ExecuteCommandResponse ExecuteCommand(ExecuteCommandRequest executeCommandRequest)
        {
            return this.Call<ExecuteCommandRequest, ExecuteCommandResponse>(executeCommandRequest);
        }

        public ExtractZipResponse ExtractZip(ExtractZipRequest extractZipRequest)
        {
            return this.Call<ExtractZipRequest, ExtractZipResponse>(extractZipRequest);
        }

        private bool VerifyServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var other = certificate as X509Certificate2;

            return this.expectedServerCertificate.SerialNumber == other?.SerialNumber
                   && this.expectedServerCertificate.Thumbprint == other?.Thumbprint;
        }

        private void HandleStatusMessage(StatusMessage statusMessage)
        {
            if (statusMessage.Type == StatusMessageType.Error)
            {
                Trace.TraceError("[SERVER] " + statusMessage.Text);
            }
            else
            {
                Trace.WriteLine("[SERVER] " + statusMessage.Text);
            }
        }
    }
}
