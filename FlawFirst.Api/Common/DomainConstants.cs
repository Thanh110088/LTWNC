namespace FlawFirst.Api.Common;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class SeedAccounts
{
    public const string AdminEmail = "admin@flawfirst.local";
    public const string AdminPassword = "Admin@123";
    public const string DemoUserEmail = "user@flawfirst.local";
    public const string DemoUserPassword = "User@123";
}

public static class ReportStatuses
{
    public const string Pending = "Pending";
    public const string Resolved = "Resolved";
    public const string Dismissed = "Dismissed";
}

public static class ModerationActions
{
    public const string Hide = "Hide";
    public const string Delete = "Delete";
    public const string Penalize = "Penalize";
    public const string Ignore = "Ignore";
}

public static class ReviewDisplay
{
    public const string AnonymousName = "Người dùng ẩn danh";
}
