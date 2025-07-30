using System;
using System.Security.Cryptography;
using System.Text;

public static class Hash
{
    public static int GetStableHash(string input)
    {
        using (var md5 = MD5.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(bytes);

            // 任意のint型にする（最初の4バイトをint化）
            return BitConverter.ToInt32(hashBytes, 0);
        }
    }
}