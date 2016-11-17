// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AspnetCoreModule.TestSites.Standard
{
    public class StartupResponse
    {
        public static int SleeptimeWhileClosing = 0;
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

            app.Map("/websocketSubProtocol", subApp =>
            {
                app.UseWebSockets(new WebSocketOptions
                {
                    ReplaceFeature = true
                });

                subApp.Use(async (context, next) =>
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync("mywebsocketsubprotocol");
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

            app.Map("/GetProcessId", subApp =>
            {
                subApp.Run(context =>
                {
                    var process = Process.GetCurrentProcess();
                    return context.Response.WriteAsync(process.Id.ToString());
                });
            });
            
            app.Map("/EchoPostData", subApp =>
            {
                subApp.Run(context =>
                {
                    string responseBody = string.Empty;
                    if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                    {
                        var form = context.Request.ReadFormAsync().GetAwaiter().GetResult();
                        int counter = 0;
                        foreach (var key in form.Keys)
                        {
                            StringValues output;
                            if (form.TryGetValue(key, out output))
                            {
                                responseBody += key + "=";
                                foreach (var line in output)
                                {
                                    responseBody += line;
                                }
                                if (++counter < form.Count)
                                {
                                    responseBody += "&";
                                }
                            }
                        }
                    }
                    else
                    {
                        responseBody = "NoAction";
                    }
                    return context.Response.WriteAsync(responseBody);
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
                string[] paths = context.Request.Path.Value.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in paths)
                {
                    string action = string.Empty;
                    string parameter = string.Empty;

                    action = "DoSleep";
                    if (item.StartsWith(action))
                    {
                        /* 
                          Process "DoSleep" command here.
                          For example, if path contains "DoSleep" such as /DoSleep1000, run Thread.Sleep(1000)
                        */
                        int sleepTime = 1000;
                        if (item.Length > action.Length)
                        {
                            parameter = item.Substring(action.Length);
                            sleepTime = Convert.ToInt32(parameter);
                        }
                        Thread.Sleep(sleepTime);
                    }

                    action = "DoClosingTimeSleep";
                    if (item.StartsWith(action))
                    {
                        /* 
                          "DoClosingTimeSleep" command here.
                          For example, if path contains "DoSleepWhileClosing" such as /DoClosingTimeSleep1000, there will be 1 second sleep time while closing
                        */
                        int sleepTime = 1000;
                        if (item.Length > action.Length)
                        {
                            parameter = item.Substring(action.Length);
                            sleepTime = Convert.ToInt32(parameter);
                        }
                        SleeptimeWhileClosing = sleepTime;                        
                    }

                    action = "ExpandEnvironmentVariables";
                    if (item.StartsWith(action))
                    {
                        /* 
                          "ExpandEnvironmentVariables" command here.
                          For example, if path contains "ExpandEnvironmentVariables" such as /ExpandEnvironmentVariablesFoo, return the expanded value for the %foo% environment variable
                        */
                        if (item.Length > action.Length)
                        {
                            parameter = item.Substring(action.Length);
                            response = Environment.ExpandEnvironmentVariables("%" + parameter + "%");
                        }                        
                    }

                    action = "GetEnvironmentVariables";
                    if (item.StartsWith(action))
                    {
                        /* 
                          Process "GetEnvironmentVariables" command here.
                          For example, if path contains "DoSleep" such as /GetEnvironmentVariables, retrun the total number of available envrionment variables
                        */
                        parameter = item.Substring(action.Length);
                        response = Environment.GetEnvironmentVariables().Count.ToString();
                    }
                }
                return context.Response.WriteAsync(response);
            });
        }
    }
}
