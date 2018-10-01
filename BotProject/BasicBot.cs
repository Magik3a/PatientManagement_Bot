// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// See https://github.com/microsoft/botbuilder-samples for a more comprehensive list of samples.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class BasicBot : IBot
    {
        // Supported LUIS Intents
        public const string GreetingIntent = "Greeting";
        public const string CancelIntent = "Cancel";
        public const string HelpIntent = "Help";
        public const string NoneIntent = "None";
        public const string GeneralInfoIntent = "General_Info";

        // Supported Calendar LUIS Intents
        public const string CalendarAddIntent = "Calendar_Add";
        public const string CalendarFindIntent = "Calendar_Find";
        public const string CalendarEditIntent = "Calendar_Edit";

        // Supported OnDevice LUIS Intents
        public const string OnDeviceLogInIntent = "OnDevice_LogIn";

        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public static readonly string LuisConfiguration = "ClarioLUIS";

        private readonly BotServices _services;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;


        /// <summary>
        /// Initializes a new instance of the <see cref="BasicBot"/> class.
        /// </summary>
        /// <param name="botServices">Bot services.</param>
        /// <param name="conversationState">Bot conversation state.</param>
        /// <param name="userState">Bot user state.</param>
        public BasicBot(BotServices botServices, ConversationState conversationState, UserState userState, ILoggerFactory loggerFactory)
        {
            _services = botServices ?? throw new ArgumentNullException(nameof(botServices));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));

            // Verify LUIS configuration.
            if (!_services.LuisServices.ContainsKey(LuisConfiguration))
            {
                throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            }

        }


        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;

            if (activity.Type == ActivityTypes.Message)
            {
                // Perform a call to LUIS to retrieve results for the current activity message.
                var luisResults = await _services.LuisServices[LuisConfiguration].RecognizeAsync(turnContext, cancellationToken).ConfigureAwait(false);

                // If any entities were updated, treat as interruption.
                // For example, "no my name is tony" will manifest as an update of the name to be "tony".
                var topScoringIntent = luisResults?.GetTopScoringIntent();

                var topIntent = topScoringIntent.Value.intent;
                switch (topIntent)
                {
                    case GreetingIntent:
                        await turnContext.SendActivityAsync("Hello.");
                        break;
                    case GeneralInfoIntent:

                        var welcomeCard = CreateAdaptiveCardAttachment(@".\Resources\welcomeCard.json");
                        await turnContext.SendActivityAsync(CreateResponse(activity, welcomeCard)).ConfigureAwait(false);
                        break;
                    case HelpIntent:
                        await turnContext.SendActivityAsync("Let me try to provide some help.");
                        await turnContext.SendActivityAsync("I understand greetings, being asked for help, being asked for login you in Clario Admin, or being asked to cancel what I am doing.");
                        break;
                    case CancelIntent:
                        await turnContext.SendActivityAsync("I have nothing to cancel.");
                        break;
                    case OnDeviceLogInIntent:
                        var inputLogInCard = CreateAdaptiveCardAttachment(@".\Resources\inputLogInCard.json");
                        await turnContext.SendActivityAsync(CreateResponse(activity, inputLogInCard)).ConfigureAwait(false);
                        break;
                    case CalendarFindIntent:
                        await turnContext.SendActivityAsync("Searching for events in your calendar");
                        break;
                    case CalendarAddIntent:
                        await turnContext.SendActivityAsync("Add event in your calendar");
                        break;
                    case NoneIntent:
                    default:
                        // Help or no intent identified, either way, let's provide some help.
                        // to the user
                        await turnContext.SendActivityAsync("I didn't understand what you just said to me.");
                        break;
                }
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (activity.MembersAdded.Any())
                {
                    // Iterate over all new members added to the conversation.
                    foreach (var member in activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message.
                        // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                        if (member.Id != activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync("Hello, Nice to talk with you!");
                            await turnContext.SendActivityAsync("Ask for info or help if you have problems.");
                        }
                    }
                }
            }

        }

        // Create an attachment message response.
        private Activity CreateResponse(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        // Load attachment from file.
        private Attachment CreateAdaptiveCardAttachment(string cardFilePath)
        {
            var adaptiveCard = File.ReadAllText(cardFilePath);
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }
    }
}
