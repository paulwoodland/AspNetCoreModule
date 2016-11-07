// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AspnetCoreModule.TestSites.Standard
{
    public class StartupResponse
    {
        private async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(minLevel: LogLevel.Warning);

            app.Map("/websocket", subApp =>
            {
                app.UseWebSockets(new WebSocketOptions
                {
                    ReplaceFeature = true
                });

                subApp.Use(async (context, next) =>
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync("");
                        //var webSocket = await context.WebSockets.AcceptWebSocketAsync("mywebsocketsubprotocol");
                        await Echo(webSocket);
                    }
                    else
                    {
                        var wsScheme = context.Request.IsHttps ? "wss" : "ws";
                        var wsUrl = $"{wsScheme}://{context.Request.Host.Host}:{context.Request.Host.Port}{context.Request.Path}";
                        await context.Response.WriteAsync($"Ready to accept a WebSocket request at: {wsUrl}");
                    }
                });
            });

            app.Map("/contentlength", subApp =>
            {
                subApp.Run(context =>
                {
                    context.Response.ContentLength = 14;
                    return context.Response.WriteAsync("Content Length");
                });
            });

            app.Map("/connectionclose", subApp =>
            {
                subApp.Run(async context =>
                {
                    context.Response.Headers[HeaderNames.Connection] = "close";
                    await context.Response.WriteAsync("Connnection Close");
                    await context.Response.Body.FlushAsync(); // Bypass IIS write-behind buffering
                });
            });

            app.Map("/chunked", subApp =>
            {
                subApp.Run(async context =>
                {
                    await context.Response.WriteAsync("Chunked");
                    await context.Response.Body.FlushAsync(); // Bypass IIS write-behind buffering
                });
            });

            app.Map("/manuallychunked", subApp =>
            {
                subApp.Run(context =>
                {
                    context.Response.Headers[HeaderNames.TransferEncoding] = "chunked";
                    return context.Response.WriteAsync("10\r\nManually Chunked\r\n0\r\n\r\n");
                });
            });

            app.Map("/manuallychunkedandclose", subApp =>
            {
                subApp.Run(context =>
                {
                    context.Response.Headers[HeaderNames.Connection] = "close";
                    context.Response.Headers[HeaderNames.TransferEncoding] = "chunked";
                    return context.Response.WriteAsync("1A\r\nManually Chunked and Close\r\n0\r\n\r\n");
                });
            });

            app.Run(context =>
            {
                string response = "Running";
                var testSleep = context.Request.Headers["test-sleep"];
                if (testSleep.ToString() != string.Empty)
                {
                    int sleepTime = Convert.ToInt32(testSleep.ToString());
                    Thread.Sleep(sleepTime);
                    response += ("; test-sleep: " + testSleep.ToString());
                }
                return context.Response.WriteAsync(response);
            });
        }
    }
}
