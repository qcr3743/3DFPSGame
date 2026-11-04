using Unity.VisualScripting;
using UnityEngine;

public class PlayerPrefsAccountRepository
{
    // 계정 데이터 보관소
    private const string KeyPrefix = "Account_";

    private string GetKeyId(string id)
    {
        return KeyPrefix + id;
    }

    // 회원가입
    public void Register(string id, string password)
    {
        PlayerPrefs.SetString(GetKeyId(id), password);
        PlayerPrefs.Save();
    }

    // 아이디 중복 검사
    public bool Exists(string id)
    {
        return PlayerPrefs.HasKey(GetKeyId(id));
    }

    // 비밀번호 검증
    public bool CheckPassword(string id, string password)
    {
        return Exists(id) && PlayerPrefs.GetString(GetKeyId(id)) == password;
    }
}