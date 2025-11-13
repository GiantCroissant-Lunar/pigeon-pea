using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using PigeonPea.DevTools.Handlers;
using PigeonPea.DevTools.Protocol;
using PigeonPea.Shared;
using Serilog;

namespace PigeonPea.DevTools.Server;

/// <summary>
/// WebSocket server for dev tools that allows external clients to connect
/// and send commands to the running game.
/// </summary>
public class DevToolsServer : IDisposable
{
    private readonly GameWorld _gameWorld;
    private readonly int _port;
    private readonly CommandHandler _commandHandler;
    private readonly ConcurrentBag<WebSocket> _connectedClients = new();
    private HttpListener? _httpListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;

    public bool IsRunning { get; private set; }

    public DevToolsServer(GameWorld gameWorld, int port = 5007)
    {
        _gameWorld = gameWorld ?? throw new ArgumentNullException(nameof(gameWorld));
        _port = port;
        _commandHandler = new CommandHandler(gameWorld);
    }

    /// <summary>
    /// Starts the WebSocket server.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            Log.Warning("DevTools server is already running");
            return;
        }

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            _httpListener.Start();

            IsRunning = true;
            Log.Information("DevTools server started on ws://127.0.0.1:{Port}/", _port);

            _listenerTask = Task.Run(() => ListenForConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start DevTools server on port {Port}", _port);
            IsRunning = false;
            throw;
        }
    }

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        Log.Information("Stopping DevTools server...");

        _cancellationTokenSource?.Cancel();

        // Close all connected clients
        foreach (var client in _connectedClients)
        {
            try
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error closing client connection");
            }
        }

        _httpListener?.Stop();
        _httpListener?.Close();

        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
        }

        IsRunning = false;
        Log.Information("DevTools server stopped");
    }

    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _httpListener != null)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(() => HandleWebSocketConnectionAsync(context), cancellationToken);
                }
                else
                {
                    // Return 400 for non-WebSocket requests
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
            {
                // Listener was stopped, this is expected
                break;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Log.Error(ex, "Error accepting connection");
            }
        }
    }

    private async Task HandleWebSocketConnectionAsync(HttpListenerContext context)
    {
        WebSocketContext? webSocketContext = null;
        WebSocket? webSocket = null;

        try
        {
            webSocketContext = await context.AcceptWebSocketAsync(null);
            webSocket = webSocketContext.WebSocket;

            _connectedClients.Add(webSocket);

            Log.Information("DevTools client connected from {RemoteEndPoint}", context.Request.RemoteEndPoint);

            // Send welcome message
            await SendEventAsync(webSocket, new CommandResultEvent
            {
                Success = true,
                Message = "Connected to PigeonPea DevTools server"
            });

            await HandleClientMessagesAsync(webSocket, _cancellationTokenSource?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling WebSocket connection");
        }
        finally
        {
            if (webSocket != null)
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error closing WebSocket");
                    }
                }

                webSocket.Dispose();
            }

            Log.Information("DevTools client disconnected");
        }
    }

    private async Task HandleClientMessagesAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleCommandAsync(webSocket, json);
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                Log.Information("Client disconnected prematurely");
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error receiving message from client");
                break;
            }
        }
    }

    private async Task HandleCommandAsync(WebSocket webSocket, string json)
    {
        try
        {
            var command = JsonSerializer.Deserialize<DevCommand>(json);

            if (command == null)
            {
                await SendEventAsync(webSocket, new CommandResultEvent
                {
                    Success = false,
                    Message = "Invalid command format"
                });
                return;
            }

            Log.Debug("Received command: {Cmd}", command.Cmd);

            var result = await _commandHandler.ExecuteCommandAsync(command);

            await SendEventAsync(webSocket, result);
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "Failed to parse command JSON: {Json}", json);
            await SendEventAsync(webSocket, new CommandResultEvent
            {
                Success = false,
                Message = $"Invalid JSON: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling command");
            await SendEventAsync(webSocket, new CommandResultEvent
            {
                Success = false,
                Message = $"Internal error: {ex.Message}"
            });
        }
    }

    private async Task SendEventAsync(WebSocket webSocket, DevEvent devEvent)
    {
        if (webSocket.State != WebSocketState.Open)
            return;

        try
        {
            var json = JsonSerializer.Serialize(devEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send event to client");
        }
    }

    /// <summary>
    /// Broadcasts an event to all connected clients.
    /// </summary>
    public async Task BroadcastEventAsync(DevEvent devEvent)
    {
        var tasks = _connectedClients
            .Where(ws => ws.State == WebSocketState.Open)
            .Select(ws => SendEventAsync(ws, devEvent));

        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _cancellationTokenSource?.Dispose();
        _httpListener?.Close();
    }
}
