using System.Net;
using System.Text.Json;

namespace GameServer.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    public ExceptionMiddleware(RequestDelegate next,ILogger<ExceptionMiddleware> logger){_next=next;_logger=logger;}

    public async Task InvokeAsync(HttpContext ctx)
    {
        try{await _next(ctx);}
        catch(Exception ex){await Handle(ctx,ex);}
    }

    private async Task Handle(HttpContext ctx,Exception ex)
    {
        _logger.LogError(ex,"Unhandled exception: {Msg}",ex.Message);
        var(code,msg)=ex switch
        {
            UnauthorizedAccessException=>((int)HttpStatusCode.Unauthorized,"Unauthorized."),
            KeyNotFoundException=>((int)HttpStatusCode.NotFound,ex.Message),
            InvalidOperationException=>((int)HttpStatusCode.BadRequest,ex.Message),
            ArgumentException=>((int)HttpStatusCode.BadRequest,ex.Message),
            _=>((int)HttpStatusCode.InternalServerError,"An unexpected error occurred.")
        };
        ctx.Response.ContentType="application/json";
        ctx.Response.StatusCode=code;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new{success=false,message=msg,statusCode=code,timestamp=DateTime.UtcNow}));
    }
}
