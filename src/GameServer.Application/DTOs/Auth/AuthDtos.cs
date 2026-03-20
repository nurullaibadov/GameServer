namespace GameServer.Application.DTOs.Auth;
public class RegisterRequestDto { public string Username{get;set;}=""; public string Email{get;set;}=""; public string Password{get;set;}=""; public string ConfirmPassword{get;set;}=""; public string? FirstName{get;set;} public string? LastName{get;set;} public string? Country{get;set;} public DateTime? DateOfBirth{get;set;} }
public class LoginRequestDto { public string UsernameOrEmail{get;set;}=""; public string Password{get;set;}=""; public bool RememberMe{get;set;}=false; }
public class AuthResponseDto { public bool Success{get;set;} public string? Message{get;set;} public string? AccessToken{get;set;} public string? RefreshToken{get;set;} public DateTime? AccessTokenExpiry{get;set;} public UserInfoDto? User{get;set;} }
public class UserInfoDto { public Guid Id{get;set;} public string Username{get;set;}=""; public string Email{get;set;}=""; public string Role{get;set;}=""; public string? AvatarUrl{get;set;} public bool IsEmailVerified{get;set;} }
public class ForgotPasswordRequestDto { public string Email{get;set;}=""; }
public class ResetPasswordRequestDto { public string Token{get;set;}=""; public string NewPassword{get;set;}=""; public string ConfirmNewPassword{get;set;}=""; }
public class ChangePasswordRequestDto { public string CurrentPassword{get;set;}=""; public string NewPassword{get;set;}=""; public string ConfirmNewPassword{get;set;}=""; }
public class RefreshTokenRequestDto { public string RefreshToken{get;set;}=""; }
