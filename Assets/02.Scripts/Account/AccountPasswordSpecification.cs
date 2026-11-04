using System.Text.RegularExpressions;

// 비밀번호 규칙: 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30 글자
// 어느 규칙에 걸렸는지 따로 알려 주려고 정규식을 셋으로 나눠 검사한다.
public class AccountPasswordSpecification
{
    // ^ 시작, $ 끝 → 문자열 "전체"가 패턴과 맞아야 한다. (없으면 일부만 맞아도 통과해 버린다)
    // [0-9]를 쓴 이유: C#의 \d는 아랍 숫자 등 다른 나라 숫자까지 맞기 때문이다.
    // static readonly: 패턴 해석은 한 번만 하고 계속 재사용한다.

    // 비밀번호 길이: 아무 글자(.)나 8~30번
    private static readonly Regex PasswordLengthRegex = new Regex(@"^.{8,30}$");

    // 비밀번호 글자 종류: ASCII 코드 '!'(33) ~ '~'(126) 범위 = 영문, 숫자, 기호. 공백과 한글은 범위 밖이라 걸린다.
    private static readonly Regex PasswordCharsRegex = new Regex(@"^[!-~]+$");

    // 비밀번호 조합: (?=.*X)는 "앞으로 어딘가에 X가 있다"는 조건(전방탐색)만 확인하고 위치는 옮기지 않는다.
    //  대문자, 소문자, 숫자, 기호([^A-Za-z0-9] = 영문/숫자가 아닌 글자)가 각각 하나 이상 있어야 한다.
    private static readonly Regex PasswordMixRegex =
        new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$");

    // 마지막 IsSatisfiedBy 검사에서 걸린 규칙의 안내 문구 (통과했으면 빈 문자열)
    public string ErrorMessage { get; private set; } = string.Empty;

    public bool IsSatisfiedBy(string password)
    {
        if (!PasswordLengthRegex.IsMatch(password))
        {
            ErrorMessage = "비밀번호는 8~30자로 입력해 주세요.";
            return false;
        }

        if (!PasswordCharsRegex.IsMatch(password))
        {
            ErrorMessage = "비밀번호에는 영문, 숫자, 기호만 쓸 수 있습니다.";
            return false;
        }

        if (!PasswordMixRegex.IsMatch(password))
        {
            ErrorMessage = "비밀번호는 대문자, 소문자, 숫자, 기호를 모두 포함해야 합니다.";
            return false;
        }

        ErrorMessage = string.Empty;
        return true;
    }
}
