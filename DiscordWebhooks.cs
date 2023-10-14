﻿using BattleBitAPI.Common;
using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleBitDiscordWebhooks;

[Module("Send some basic events to Discord and allow for other modules to send messages to Discord", "1.1.0")]
public class DiscordWebhooks : BattleBitModule
{
    private Queue<DiscordMessage> discordMessageQueue = new();
    private HttpClient httpClient = new HttpClient();
    public WebhookConfiguration Configuration { get; set; } = null!;

    public override void OnModulesLoaded()
    {
        if (string.IsNullOrEmpty(this.Configuration.WebhookURL))
        {
            this.Unload();
            throw new Exception("Webhook URL is not set. Please set it in the configuration file.");
        }
    }

    public override Task OnConnected()
    {
        discordMessageQueue.Enqueue(new WarningMessage("Server connected to API", Configuration.WebhookURL));
        Task.Run(() => sendChatMessagesToDiscord());
        return Task.CompletedTask;
    }

    public override Task OnDisconnected()
    {
        discordMessageQueue.Enqueue(new WarningMessage("Server disconnected from API", Configuration.WebhookURL));
        return base.OnDisconnected();
    }

    public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
    {
        discordMessageQueue.Enqueue(new ChatMessage(player.Name, player.SteamID, channel, msg));

        return Task.FromResult(true);
    }

    public override Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional)
    {
        this.discordMessageQueue.Enqueue(new WarningMessage($"{from.Name} ({from.SteamID}) reported {to.Name} ({to.SteamID}) for {reason}:{Environment.NewLine}> {additional}", Configuration.WebhookURL));
        return Task.CompletedTask;
    }

    public void SendMessage(string message, string? webhookURL = null)
    {
        if (webhookURL is not null)
        {
            Task.Run(() => sendWebhookMessage(webhookURL, message));
        }
        else
        {
            this.discordMessageQueue.Enqueue(new RawTextMessage(message));
        }
    }

    private async Task sendChatMessagesToDiscord()
    {
        do
        {
            List<DiscordMessage> messages = new();
            do
            {
                try
                {
                    while (this.discordMessageQueue.TryDequeue(out DiscordMessage? message))
                    {
                        if (message == null)
                        {
                            continue;
                        }

                        messages.Add(message);
                    }


                    if (messages.Count > 0)
                    {
                        DiscordMessage message = messages.First();
                        await sendWebhookMessage(message.WebhookURL ?? Configuration.ReportWebhook, string.Join(Environment.NewLine, messages.Select(message => message.ToString())));
                    }

                    messages.Clear();
                }
                catch (Exception ex)
                {
                    this.Logger.Error($"Failed to process message queue.", ex);
                    await Task.Delay(500);
                }
            } while (messages.Count > 0);

            await Task.Delay(500);
        } while (this.Server?.IsConnected == true);
    }

    private async Task sendWebhookMessage(string webhookUrl, string message)
    {
        bool success = false;
        while (!success)
        {
            var payload = new
            {
                content = message
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            var response = await this.httpClient.PostAsync(webhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                this.Logger.Error($"Error sending webhook message. Status Code: {response.StatusCode}");
            }

            success = response.IsSuccessStatusCode;
        }
    }

}

internal class DiscordMessage
{
    public string? WebhookURL { get; set; } = null;
}

internal class RawTextMessage : DiscordMessage
{
    public string Message { get; set; }

    public RawTextMessage(string message)
    {
        this.Message = message;
    }

    public override string ToString()
    {
        return this.Message;
    }
}

internal class ChatMessage : DiscordMessage
{
    public string PlayerName { get; set; } = string.Empty;

    public ChatMessage(string playerName, ulong steamID, ChatChannel channel, string message)
    {
        this.PlayerName = playerName;
        this.SteamID = steamID;
        this.Channel = channel;
        this.Message = message;
    }

    public ulong SteamID { get; set; }
    public ChatChannel Channel { get; set; }
    public string Message { get; set; } = string.Empty;

    public override string ToString()
    {
        return $":speech_balloon: [{this.SteamID}] {this.PlayerName}: {this.Message}";
    }
}

internal class JoinAndLeaveMessage : DiscordMessage
{
    public int PlayerCount { get; set; }

    public JoinAndLeaveMessage(int playerCount, string playerName, ulong steamID, bool joined)
    {
        this.PlayerCount = playerCount;
        this.PlayerName = playerName;
        this.SteamID = steamID;
        this.Joined = joined;
    }

    public string PlayerName { get; set; } = string.Empty;
    public ulong SteamID { get; set; }
    public bool Joined { get; set; }

    public override string ToString()
    {
        return $"{(this.Joined ? ":arrow_right:" : ":arrow_left:")} [{this.SteamID}] {this.PlayerName} {(this.Joined ? "joined" : "left")} ({this.PlayerCount} players)";
    }
}

internal class WarningMessage : DiscordMessage
{
    public WarningMessage(string message, string webhook)
    {
        this.WebhookURL = webhook;
        this.Message = message;
    }

    public bool IsError { get; set; }

    public string Message { get; set; }

    public override string ToString()
    {
        return $":warning: {this.Message}";
    }
}

public class WebhookConfiguration : ModuleConfiguration
{
    public string WebhookURL { get; set; } = string.Empty;
    public string ReportWebhook { get; set; } = string.Empty;
}