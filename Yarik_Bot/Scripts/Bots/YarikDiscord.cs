﻿using DSharpPlus;
using DSharpPlus.EventArgs;
using RostCunt;

namespace MainSpace
{
    public sealed class YarikDiscord
    {
        private static string? Token => Environment.GetEnvironmentVariable("YARIK_DISCORD_BOT_TOKEN");

        private DiscordClient? Client { get; set; }

        private readonly PromptGenerator _promptGenerator;
        private readonly UsersDB _usersDB;
        private readonly Platform _platform = Platform.Discord;

        public YarikDiscord(DIContainer container)
        {
            _promptGenerator = container.Resolve<PromptGenerator>();
            _usersDB = container.Resolve<UsersDB>();
        }

        public async Task Start()
        {
            Logger.Instance.LogInfo("Starting discord...", LogPlace.Discord);
            Client = new DiscordClient(GetDiscordConfig());

            Client.Ready += ClientOnReady;

            await Client.ConnectAsync();

            Client.MessageCreated += HandleMessage;
        }

        private DiscordConfiguration GetDiscordConfig()
        {
            return new DiscordConfiguration()
            {
                Token = Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                AutoReconnect = true
            };
        }

        private async Task HandleMessage(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (eventArgs.Author.IsBot)
                return;

            var message = eventArgs.Message;
            var guild = eventArgs.Guild;
            var authorId = message.Author.Id;

            if (CheckMessageCondition(eventArgs))
            {
                /// message responce
                string username =
                    guild != null ?
                    (await eventArgs.Guild.GetMemberAsync(authorId)).Nickname :
                    message.Author.Username;

                string? referencedMessage = message.ReferencedMessage?.Content;
                string responce = await _promptGenerator.GenerateAsync(message.Content, username, referencedMessage);
                await message.RespondAsync(responce);

                /// database
                bool userExistence = await _usersDB.CheckToUserExists(authorId.ToString(), _platform);
                if (userExistence)
                {
                    //await _usersDB.InsertToUsageTable(newPlatformId, 1, 1, DateTime.Now);
                }
                else
                {
                    int newUserId = await _usersDB.InsertToUserTable(DateTime.Now, DateTime.Now);
                    int newPlatformId = await _usersDB.InsertUserPlatformsTable(_platform, newUserId, authorId.ToString());
                    await _usersDB.InsertToUsageTable(newPlatformId, 1, 1, DateTime.Now);
                }



                Logger.Instance.LogDebug(message, LogPlace.Discord);
            }
        }

        private static bool CheckMessageCondition(MessageCreateEventArgs eventArgs)
        {
            var message = eventArgs.Message;

            return message.Content.ToLower().Contains("олег") || message.ReferencedMessage != null;
        }

        private Task ClientOnReady(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
