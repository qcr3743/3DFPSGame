using System.Text.RegularExpressions;

// 아이디 규칙: 이메일 형식만 허용
public class AccountIdSpecification
{
    // 이메일: 아이디부분@도메인.끝
    //  [A-Za-z0-9_+-]+                : 영문, 숫자, _, +, - 한 글자 이상
    //  (\.[A-Za-z0-9_+-]+)*           : ".글자들"이 0번 이상 → 점은 맨 앞/맨 뒤/연속(..)으로 올 수 없다
    //  @                              : @ 하나
    //  [A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+ : 도메인. ".글자들"이 1번 이상 → 점이 최소 하나 있어야 한다 (a@b.c, mail.co.kr)
    private static readonly Regex EmailRegex =
        new Regex(@"^[A-Za-z0-9_+-]+(\.[A-Za-z0-9_+-]+)*@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$");

    public string ErrorMessage => "아이디는 이메일 형식으로 입력해 주세요.";

    public bool IsSatisfiedBy(string email)
    {
        return EmailRegex.IsMatch(email);
    }
}