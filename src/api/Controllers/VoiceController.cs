using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace TwilioBridge.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class VoiceController : ControllerBase
{
  const string SYSTEM_MESSAGE = @"
<instructions> 
You manage a phone system that handles transcribed voice messages. 
Your primary function is to respond to voice queries converted to text, ensuring smooth two-way communication. Follow these guidelines:

1. Brevity: Keep responses concise, considering the limitations of voice-to-text accuracy and clarity.
2. Clarity: Use simple, clear language to ensure easy understanding for both transcription and speech output.
3. Informativeness: Provide accurate and helpful information, avoiding unnecessary details.
4. Context-awareness: Consider that users may be in different situations where quick, precise information is crucial.
5. Safety-first: If a query involves potential safety risks, prioritize cautious and responsible advice.
6. Respect communication etiquette: Maintain clear grammar and punctuation to aid accurate transcription and response.
7. Adaptability: Be prepared to handle a wide range of queries, from casual conversations to emergency situations.
8. Technical limitations: Be mindful of the transcription quality and adjust responses to ensure they're easily understood in voice format.
9. Use Clear Language: Avoid abbreviations or shorthand that may lead to transcription errors or misunderstandings.
10. Names: When referring to people, places, or organizations, use short, well-understood terms, considering both transcription and voice synthesis.

Remember, your responses are transcribed from voice, so focus on delivering information that remains clear and effective through both speech and text. 
</instructions>
";

  private readonly ILogger<SmsController> logger;
  private readonly ChatClient client;
  private static Dictionary<string, List<ChatMessage>> history = new Dictionary<string, List<ChatMessage>>();

  public VoiceController(ILogger<SmsController> logger, OpenAIClient client)
  {
    this.client = client.GetChatClient("gpt-4o");
    this.logger = logger;
  }

  [HttpPost()]
  [Consumes("application/x-www-form-urlencoded")]
  public TwiMLResult Index([FromForm] VoiceRequest request)
  {
    if(string.IsNullOrWhiteSpace(request.SpeechResult))
    {
      history[request.From] = new List<ChatMessage>() { ChatMessage.CreateSystemMessage(SYSTEM_MESSAGE) };

      var intro = "Thank you for calling the Voice Bridge! What is your query?";

      var response = new VoiceResponse();
      response.Say(intro);
      response.Gather(input: new() { Gather.InputEnum.Speech });
      return this.TwiML(response);
    }
    else
    {
      var messages = history[request.From];
      messages.Add(ChatMessage.CreateUserMessage(request.SpeechResult));

      var options = new ChatCompletionOptions() { MaxOutputTokenCount = 1000 };
      ChatCompletion result = this.client.CompleteChat(messages, options);
      var body = result.Content.FirstOrDefault()?.Text ?? string.Empty;

      messages.Add(ChatMessage.CreateAssistantMessage(body));

      history[request.From] = messages;

      var response = new VoiceResponse();
      response.Say(body);
      response.Gather(input: new() { Gather.InputEnum.Speech });
      return this.TwiML(response);
    }

  }
}
