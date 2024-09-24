using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<OpenAIClient>(sp => new(Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
builder.Services.AddControllers();

var app = builder.Build();

app.UseWelcomePage("/welcome");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();