using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Grpc.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Proxy.DTO;

namespace Proxy.Hubs
{
    class PositionHubState
    {
        public MapBackend.MapBackendClient Client;
        public AsyncDuplexStreamingCall<CommandEnvelope, PositionBatch> GrpcConnection;
    }

    [PublicAPI]
    public class PositionsHub : Hub
    {
        public string ConnectionId => $"connection{Context.ConnectionId}";

        private PositionHubState State
        {
            get
            {
                if (!Context.Items.ContainsKey("state")) Context.Items.Add("state", new PositionHubState());

                return Context.Items["state"] as PositionHubState;
            }
        }

        public async IAsyncEnumerable<PositionsDto> Connect()
        {
            Console.WriteLine($"Connect user session {ConnectionId}");

            var channel = new Channel(
                host: "127.0.0.1",
                port: 5002,
                credentials: ChannelCredentials.Insecure);

            Console.WriteLine("Got response");

            var grpcClient = new MapBackend.MapBackendClient(channel);
            var conn = grpcClient.Connect();
            
            State.Client = grpcClient;
            State.GrpcConnection = conn;

            Console.WriteLine("Got response...");

            CommandEnvelope envelope = new()
            {
                CreateViewport = new CreateViewport()
                {
                    ConnectionId = ConnectionId
                }
            };

            await conn.RequestStream.WriteAsync(envelope);

            Console.WriteLine($"Connected {envelope}");

            var responseStream = State.GrpcConnection.ResponseStream;

            await foreach (var grpcResponse in responseStream.ReadAllAsync())
            {
                if (!grpcResponse.Positions.Any())
                {
                    continue;
                }

                var dtoBatch = grpcResponse
                    .Positions
                    .Select(PositionDto.MapFrom)
                    .ToArray();

                yield return new PositionsDto
                {
                    Positions = dtoBatch
                };
            }
        }

        public async Task SetViewport(double swLng, double swLat, double neLng, double neLat)
        {
            CommandEnvelope envelope = new()
            {
                UpdateViewport = new UpdateViewport
                {
                    Viewport = new Viewport
                    {
                        SouthWest = new GeoPoint(swLng, swLat),
                        NorthEast = new GeoPoint(neLng, neLat)
                    }
                }
            };

            await State.GrpcConnection.RequestStream.WriteAsync(envelope);
        }
    }
}