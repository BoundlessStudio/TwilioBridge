using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace TwilioBridge.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class SmsController : ControllerBase
{
  const string SYSTEM_MESSAGE = @"
<instructions>
You manage a text message (SMS) system.
Your primary function is to respond to SMS queries submitted from users. Follow these guidelines:

1. Brevity: Keep responses concise to respect users' time and potential character limits.
2. Clarity: Use clear, simple language to ensure easy understanding.
3. Informativeness: Provide accurate and helpful information without unnecessary details.
4. Context-awareness: Consider that users may be in various situations where quick, precise information is crucial.
5. Safety-first: If a query involves potential safety risks, prioritize cautious and responsible advice.
6. Respect SMS etiquette: Use common SMS communication practices, such as proper grammar and punctuation.
7. Adaptability: Be prepared to handle a wide range of queries, from casual conversations to emergency situations.
8. Technical limitations: Be aware of character limits (typically 160 characters per SMS) and structure messages accordingly. The max token output is limited to 150 so use follow-up messages if necessary.
9. Use Abbreviations: While abbreviations can save space, ensure they do not lead to misunderstandings.
10. Names: When referring to people, places, or organizations, use keep names short and use well understood short hand for people and places.

Remember, your responses are transmitted over SMS, so focus on delivering the most important information efficiently and effectively.
</instructions>
";

  private readonly ILogger<SmsController> logger;
  private readonly ChatClient client;
  private static Dictionary<string, List<ChatMessage>> history = new Dictionary<string, List<ChatMessage>>();

  public SmsController(ILogger<SmsController> logger, OpenAIClient client)
  {
    this.client = client.GetChatClient("gpt-4o");
    this.logger = logger;
  }

  [HttpPost()]
  [Consumes("application/x-www-form-urlencoded")]
  public TwiMLResult Index([FromForm]SmsRequest request)
  {
    var messages = history.ContainsKey(request.From) ? history[request.From] : new List<ChatMessage>() { ChatMessage.CreateSystemMessage(SYSTEM_MESSAGE) };
    messages.Add(ChatMessage.CreateUserMessage(request.Body));

    var options = new ChatCompletionOptions() { MaxOutputTokenCount = 150 };
    ChatCompletion result = this.client.CompleteChat(messages, options);
    var body = result.Content.FirstOrDefault()?.Text ?? string.Empty;

    messages.Add(ChatMessage.CreateAssistantMessage(body));

    history[request.From] = messages;

    var response = new MessagingResponse();
    response.Message(body);
    return this.TwiML(response);
  }
}