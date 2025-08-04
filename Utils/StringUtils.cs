using UnityEngine;
using System.Collections;

public class SB
{
    static System.Text.StringBuilder sb = new System.Text.StringBuilder();
    public static string Str(params object[] str)
    {
        sb.Remove(0, sb.Length);
        for (int i = 0; i < str.Length; ++i)
        {
            sb.Append(str[i]);
        }

        sb.Replace("\\n", "\n");
		return sb.ToString();
    }

    public static string FormatStr(string fmt, params object[] str)
    {
        sb.Remove(0, sb.Length);
        sb.AppendFormat(fmt, str);
        sb.Replace("\\n", "\n");
        return sb.ToString();
    }

	/// <summary>
	/// 자동 , 찍기.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static string NumberStr(params object[] str)
	{
		sb.Remove(0, sb.Length);

		sb.Append(string.Format("{0:#,###;-#,##0;0}", str));

		return sb.ToString();
	}

    public static string FloatStr(params object[] str)
    {
        sb.Remove(0, sb.Length);

        sb.Append(string.Format("{0:#,###.#;(#,##0.#);0}", str));

        return sb.ToString();
    }
}

