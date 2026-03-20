namespace GameServer.Domain.Enums;

public enum UserRole { Player = 1, Moderator = 2, Admin = 3, SuperAdmin = 4 }
public enum UserStatus { Active = 1, Inactive = 2, Banned = 3, PendingVerification = 4 }
public enum GameStatus { Waiting = 1, InProgress = 2, Finished = 3, Cancelled = 4 }
public enum GameType { Solo = 1, Duo = 2, Squad = 3, Custom = 4 }
public enum MatchResult { Win = 1, Loss = 2, Draw = 3, Disconnect = 4 }
public enum TransactionType { Purchase = 1, Reward = 2, Refund = 3, AdminCredit = 4 }
