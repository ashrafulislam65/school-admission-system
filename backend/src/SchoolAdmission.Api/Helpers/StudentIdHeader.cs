namespace SchoolAdmission.Api.Helpers;

public static class StudentIdHeader
{
    public const string Name = "X-Student-Id";

    // Student id mane 17 ongker jonmo nibondhon number
    public static bool TryGet(HttpRequest request, out string studentId)
    {
        studentId = request.Headers[Name].ToString().Trim();
        return studentId.Length == 17 && studentId.All(char.IsAsciiDigit);
    }
}