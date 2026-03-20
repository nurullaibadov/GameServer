using GameServer.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace GameServer.Infrastructure.Services.Email;

public class EmailService : IEmailService
{
    private readonly string _host,_username,_password,_fromEmail,_fromName,_frontendUrl;
    private readonly int _port;

    public EmailService(IConfiguration cfg)
    {
        _host=cfg["Smtp:Host"]??"smtp.gmail.com";
        _port=int.Parse(cfg["Smtp:Port"]??"587");
        _username=cfg["Smtp:Username"]??"";
        _password=cfg["Smtp:Password"]??"";
        _fromEmail=cfg["Smtp:FromEmail"]??"";
        _fromName=cfg["Smtp:FromName"]??"GameServer";
        _frontendUrl=cfg["App:FrontendUrl"]??"http://localhost:3000";
    }

    public Task SendWelcomeEmailAsync(string to,string username,string token)=>
        SendGenericEmailAsync(to,"Welcome to GameServer - Verify Your Email",$@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
  <div style='background:linear-gradient(135deg,#667eea,#764ba2);padding:30px;border-radius:10px 10px 0 0'>
    <h1 style='color:white;margin:0'>Welcome to GameServer!</h1>
  </div>
  <div style='background:#f9f9f9;padding:30px;border-radius:0 0 10px 10px'>
    <h2>Hi {username}!</h2>
    <p>Click below to verify your email address and activate your account:</p>
    <div style='text-align:center;margin:30px 0'>
      <a href='{_frontendUrl}/verify-email?token={token}' style='background:#667eea;color:white;padding:15px 30px;border-radius:5px;text-decoration:none;font-size:16px'>Verify Email</a>
    </div>
    <p style='color:#999;font-size:12px'>This link expires in 24 hours.</p>
  </div>
</div>");

    public Task SendPasswordResetEmailAsync(string to,string username,string token)=>
        SendGenericEmailAsync(to,"Password Reset - GameServer",$@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
  <div style='background:#e74c3c;padding:30px;border-radius:10px 10px 0 0'>
    <h1 style='color:white;margin:0'>Password Reset</h1>
  </div>
  <div style='background:#f9f9f9;padding:30px;border-radius:0 0 10px 10px'>
    <h2>Hi {username}!</h2>
    <p>Click below to reset your password. Link expires in 1 hour.</p>
    <div style='text-align:center;margin:30px 0'>
      <a href='{_frontendUrl}/reset-password?token={token}' style='background:#e74c3c;color:white;padding:15px 30px;border-radius:5px;text-decoration:none;font-size:16px'>Reset Password</a>
    </div>
    <p style='color:#999;font-size:12px'>If you did not request this, ignore this email.</p>
  </div>
</div>");

    public Task SendEmailVerificationAsync(string to,string username,string token)=>SendWelcomeEmailAsync(to,username,token);

    public Task SendBanNotificationAsync(string to,string username,string reason)=>
        SendGenericEmailAsync(to,"Account Suspended - GameServer",$@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:30px'>
  <h2 style='color:#e74c3c'>Account Suspended</h2>
  <p>Hi {username}, your account has been suspended.</p>
  <p><strong>Reason:</strong> {reason}</p>
  <p>Contact support if you believe this is a mistake.</p>
</div>");

    public async Task SendGenericEmailAsync(string to,string subject,string html)
    {
        try
        {
            var msg=new MimeMessage();
            msg.From.Add(new MailboxAddress(_fromName,_fromEmail));
            msg.To.Add(new MailboxAddress("",to));
            msg.Subject=subject;
            msg.Body=new TextPart("html"){Text=html};
            using var client=new SmtpClient();
            await client.ConnectAsync(_host,_port,MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_username,_password);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }
        catch(Exception ex){ Console.WriteLine($"[Email] Failed to {to}: {ex.Message}"); }
    }
}
